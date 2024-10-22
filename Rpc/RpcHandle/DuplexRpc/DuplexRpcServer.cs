using Rpc.Tcp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Rpc.RpcHandle.DuplexRpc
{
	public class DuplexRpcServer<TServer, TServerCallBack> : RpcServer<TServer>
	{


		public DuplexRpcServer(TServer serverHandle) : base(serverHandle)
		{

		}

		internal override void RpcServer_CommunicationCreating(object sender, CreateCommunicatorArgs args)
		{
			var com = new CommandDuplex<TServer, TServerCallBack>(ServerHandle, ServerMethods, args.Socket, args.ReceiveBufferSize);
			com.ProcessTimeOutMs = _processTimeoutMs;
			args.Communicator = com;
		}

		private int _processTimeoutMs = 1000;
		public int ProcessTimeoutMs
		{
			get => _processTimeoutMs;
			set
			{
				foreach (var com in Clients)
				{
					Util.Util.SetPropertyValue(com, "ProcessTimeOutMs", value);
				}
			}
		}

		public IContext[] GetAllContext()
		{
			return Clients.Select(x => (IContext)x).ToArray();
		}

	}
}
