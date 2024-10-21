using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace Rpc.RpcHandle
{
	public class RpcClient<TServer> : Rpc.Tcp.TcpClient
	{

		ProxyProcessor _proxy;
		List<MethodInfo> _methods;
		SerializeLayer _serializeLayer;
		SocketLayer _socketLayer;

		public int ProcessTimeOutMs { get; set; } = 10000;

		private readonly ConcurrentDictionary<int, TaskCompletionSource<SocketLayer.DataReceiveArgs>> _pendingRequests =
			new ConcurrentDictionary<int, TaskCompletionSource<SocketLayer.DataReceiveArgs>>();
		//lock
		object _lock = new object();
		int _cmdCounter;
		//cancel token
		CancellationTokenSource _cts;

		public RpcClient()
		{
			_proxy = new ProxyProcessor(typeof(TServer));
			_proxy.FunctionCalled += _proxy_FunctionCalled;

			_methods = Util.Util.GetOperationContractMethods(typeof(TServer));
			_serializeLayer = new SerializeLayer(_methods);
			_socketLayer = new SocketLayer();

			this.DataIn += RpcClient_DataIn;
			_socketLayer.PackIn += _socketLayer_PackIn;

			this.Disconnected += (ss, ee) =>
			{
				_cts.Cancel();
			};
		}

		public TServer Proxy => (TServer)_proxy.GetTransparentProxy();


		public override void Connect()
		{
			_cts = new CancellationTokenSource();
			base.Connect();
		}

		private void _socketLayer_PackIn(object sender, SocketLayer.DataReceiveArgs e)
		{
			//server指令進來後
			if (e.Header.Type == MessageType.Response)
			{
				ResponseProc(e);
			}
			else if (e.Header.Type == MessageType.Request)
			{
				//client不會收到request
			}
		}

		private void ResponseProc(SocketLayer.DataReceiveArgs e)
		{
			if (_pendingRequests.TryRemove(e.Header.Id, out var tcs))
			{
				tcs.SetResult(e);
			}
		}

		private void RpcClient_DataIn(object sender, Tcp.DataInArgs e)
		{
			_socketLayer.AddData(e.Data);
		}

		int IncCounter()
		{
			lock (_lock)
			{
				var ret = _cmdCounter;
				_cmdCounter++;
				return ret;
			}
		}

		private void _proxy_FunctionCalled(object sender, ProxyFunctionCallArgs e)
		{
			if (this.IsConnect == false) throw new Exception("not connect to server");

			//get method
			var method = _methods.FirstOrDefault(x => x.Name == e.MethodName);
			PackHeader header = new PackHeader()
			{
				Type = MessageType.Request,
				Method = e.MethodName,
				Id = IncCounter()
			};
			var data = SerializeLayer.Serialize(header, e.Args);
			this.Send(data);

			//wait data back
			var waiter = WaitForDataAsync(header.Id, _cts.Token);
			var ret = waiter.Wait(ProcessTimeOutMs);

			//例外處理
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

		internal Task<SocketLayer.DataReceiveArgs> WaitForDataAsync(int key, CancellationToken cancellationToken)
		{
			var tcs = new TaskCompletionSource<SocketLayer.DataReceiveArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
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
	}
}
