using Rpc.Tcp;
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
	internal class CommandClient<TCmd> : CmdCommunicator, IContext
	{
		public int ProcessTimeOutMs { get; set; } = 10000;


		CancellationTokenSource _canceller = new CancellationTokenSource();

		//for wait data
		private readonly ConcurrentDictionary<int, TaskCompletionSource<CmdCommunicator.DataReceiveArgs>> _pendingRequests =
			new ConcurrentDictionary<int, TaskCompletionSource<CmdCommunicator.DataReceiveArgs>>();

		List<MethodInfo> _methods;
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

		internal ProxyProcessor ProxyHandle { get; private set; }
		public TCmd Proxy => (TCmd)ProxyHandle.GetTransparentProxy();


		public CommandClient(Socket s, int receiveBufferSize) : base(s, receiveBufferSize)
		{
			_methods = Util.Util.GetOperationContractMethods(typeof(TCmd));
			ProxyHandle = new ProxyProcessor(typeof(TCmd));
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
			if (Finalized == true) throw new Exception("not connect to server");

			//get method
			var method = _methods.FirstOrDefault(x => x.Name == e.MethodName);
			PackHeader header = new PackHeader()
			{
				Type = MessageType.Request,
				Method = e.MethodName,
				Id = IncCounter()
			};
			var data = Serilaizer.Serialize(header, e.Args);

			//wait data back
			//you should start wait before send 
			var waiter = WaitForDataAsync(header.Id, _canceller.Token);
			var ret = waiter.Wait(ProcessTimeOutMs);
			//send data
			this.Send(data);

			

			//excpetion process
			if (_canceller.IsCancellationRequested)
			{
				//disconnect procedure
				throw new Exception("Connection is closed");
			}
			if (ret == false || waiter.Result.Data.Length == 0)
			{
				//timeout
				throw new TimeoutException("Remote command timeout");
			}

			var result = new ResponseResult(method, waiter.Result.Data);
			if (result.Success == false)
			{
				throw new Exception(result.ErrorReason);
			}


			e.ReturnObject = result.Result;
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
			return (TContract)ProxyHandle.GetTransparentProxy();
		}
	}
}
