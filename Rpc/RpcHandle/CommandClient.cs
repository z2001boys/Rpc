using Rpc.Tcp;
using Rpc.Util;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Rpc.RpcHandle
{
	internal class CommandClient<TCmd> : CmdCommunicator, IContext where TCmd : class
	{
		public int ProcessTimeOutMs { get; set; } = 10000;


		CancellationTokenSource _canceller = new CancellationTokenSource();

		//for wait data
		private readonly ConcurrentDictionary<int, TaskCompletionSource<CmdCommunicator.DataReceiveArgs>> _pendingRequests =
			new ConcurrentDictionary<int, TaskCompletionSource<CmdCommunicator.DataReceiveArgs>>();

		List<MethodCallInfo> _methods;
		private object _lock = new object();
		private int _cmdCounter;

		int IncCounter()
		{
			lock (_lock)
			{
				var ret = _cmdCounter;
				_cmdCounter++;
				if (_cmdCounter == 10000)
					_cmdCounter = 0;
				return ret;
			}
		}

		internal FakeSimpleProxy<TCmd> ProxyHandle { get; private set; }
		public TCmd Proxy => (TCmd)ProxyHandle.GetProxy();


		public CommandClient(Socket s, int receiveBufferSize) : base(s, receiveBufferSize)
		{
			_methods = Util.Util.BuildMethodInfo(typeof(TCmd));
			ProxyHandle = new FakeSimpleProxy<TCmd>();
			ProxyHandle.FunctionCalled += Call;
			base.PackIn += OnPackIn;
			this.Disconnected += CommandClient_Disconnected;
		}

		private void CommandClient_Disconnected(object sender, EventArgs e)
		{
			_canceller.Cancel();
			ProxyHandle.FunctionCalled -= Call;
		}

		internal virtual void OnPackIn(object sender, DataReceiveArgs e)
		{
			if (e.Header.Type == MessageType.Response)
			{
				ResponseProc(e);
			}
		}

		internal void ResponseProc(CmdCommunicator.DataReceiveArgs e)
		{
			if (_pendingRequests.TryRemove(e.Header.Id, out var tcs))
			{
				tcs.SetResult(e);
			}
		}

		private void Call(object sender, ProxyFunctionCallArgs e)
		{
			var method = _methods.FirstOrDefault(x => x.Name == e.MethodName);
			var returnType = method.Method.ReturnType;

			if (typeof(Task).IsAssignableFrom(returnType))
			{
				// 若為Task或Task<T>，將其包裝並回傳給外部
				e.ReturnObject = WrapAsTask(returnType, e.MethodName, e.Args, ProcessTimeOutMs);
			}
			else
			{
				// 同步方法繼續使用Wait (這種方法同步是可接受的，因為它非async)
				try
				{
					var task = CallAsync(e.MethodName, e.Args, ProcessTimeOutMs);
					task.Wait(ProcessTimeOutMs);
					e.ReturnObject = task.Result;
				}
				catch (Exception ex)
				{
					// 這裡可以處理異常
					if (ex.InnerException != null)
					{
						throw ex.InnerException;
					}
					else
					{
						throw ex;
					}
				}


			}
		}

		private object WrapAsTask(Type returnType, string methodName, object[] args, int timeoutMs)
		{
			if (returnType == typeof(Task))
			{
				return CallAsync(methodName, args, timeoutMs);
			}
			else if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
			{
				var innerType = returnType.GetGenericArguments()[0];

				return this.GetType()  // 改為你的類別名稱
					.GetMethod(nameof(WrapAsGenericTask), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
					.MakeGenericMethod(innerType)
					.Invoke(this, new object[] { methodName, args, timeoutMs });
			}

			throw new InvalidOperationException("Invalid return type");
		}

		private async Task<T> WrapAsGenericTask<T>(string methodName, object[] args, int timeoutMs)
		{
			var result = await CallAsync(methodName, args, timeoutMs);
			return (T)result;
		}

		private async Task<object> CallAsync(string methodName, object[] args, int timeoutMs)
		{
			if (Finalized)
				throw new Exception("not connect to server");

			var method = _methods.FirstOrDefault(x => x.Name == methodName);

			if (method == null)
				throw new Exception($"Method '{methodName}' not found.");

			PackHeader header = new PackHeader()
			{
				Type = MessageType.Request,
				Method = methodName,
				Id = IncCounter()
			};

			var data = Serilaizer.Serialize(header, args);

			var timeout = method.TimeoutMs >= 0 ? method.TimeoutMs : timeoutMs;

			var waiter = WaitForDataAsync(header.Id, _canceller.Token);

			this.Send(data);

			var completedTask = await Task.WhenAny(waiter, Task.Delay(timeout));
			if (completedTask != waiter)
				throw new TimeoutException("Remote command timeout");

			var responseData = waiter.Result;

			if (_canceller.IsCancellationRequested)
				throw new Exception("Connection is closed");

			if (responseData.Data.Length == 0)
				throw new TimeoutException("Remote command timeout");

			var result = new ResponseResult(method.Method, responseData.Data);

			if (!result.Success)
				throw new Exception(result.ErrorReason);

			return result.Result;
		}

		internal Task<CmdCommunicator.DataReceiveArgs> WaitForDataAsync(int key, CancellationToken cancellationToken)
		{
			var tcs = new TaskCompletionSource<CmdCommunicator.DataReceiveArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
			if (!_pendingRequests.TryAdd(key, tcs))
			{
				throw new InvalidOperationException($"Request with key {key} is already pending.");
			}

			cancellationToken.Register(() =>
			{
				// If cancellation occurs, try to remove the request and set the task as cancelled
				if (_pendingRequests.TryRemove(key, out var pendingTcs))
				{
					pendingTcs.TrySetCanceled();
				}
			});

			return tcs.Task;
		}

		public TContract GetContract<TContract>()
		{
			return (TContract)ProxyHandle.GetProxy();
		}
	}
}
