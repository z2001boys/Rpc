using Rpc.Tcp;
using Rpc.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Rpc.RpcHandle.DuplexRpc
{
	public class DuplexRpcClient<TServer, TServerCallBack> : RpcClient<TServer>
		where TServer : class
		where TServerCallBack : class
	{
		public DuplexRpcClient(TServerCallBack callBackHandle)
		{
			CallBackHandle = callBackHandle;
			CallBackMethods = Util.Util.BuildMethodInfo(typeof(TServerCallBack));
		}

		internal override void OnCommunicationCreating(object sender, CreateCommunicatorArgs args)
		{
			var com = new CommandDuplex<TServerCallBack, TServer>(CallBackHandle, CallBackMethods, args.Socket, args.ReceiveBufferSize);
			com.ProcessTimeOutMs = this.ProcessTimeOutMs;
			args.Communicator = com;
		}

		//internal override void FunctionCall(object sender, ProxyFunctionCallArgs e)
		//{
		//	if (this.IsConnect == false)
		//		throw new Exception("Not connected");
		//	if (this.Com == null)
		//		throw new Exception("Communicator not created");
		//	if (Com is CommandDuplex<TServerCallBack, TServer> duplexCom)
		//	{
		//		duplexCom.Call(e);
		//	}
		//}

		public TServerCallBack CallBackHandle { get; }
		public List<MethodCallInfo> CallBackMethods { get; }
	}
}
