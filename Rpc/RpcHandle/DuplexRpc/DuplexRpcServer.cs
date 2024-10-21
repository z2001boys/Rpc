using Rpc.Tcp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Rpc.RpcHandle.DuplexRpc
{
	public class DuplexRpcServer<TServer,TServerCallBack> : RpcServer<TServer>
	{		

		//List<CommandSender<TServerCallBack>> _commandSender;

		public DuplexRpcServer(TServer serverHandle) : base(serverHandle)
		{
			this.ClientConnected += DuplexClientConnect;
			this.ClientDisConnected += DuplexClientDisConnect;
		}

		private void DuplexClientDisConnect(object sender, SocketInfoArgs e)
		{
			
		}

		private void DuplexClientConnect(object sender, SocketInfoArgs e)
		{
			//var newSender = new CommandSender<TServerCallBack>();
			//
			//
			//lock (newSender)
			//{
			//	_commandSender.Add(newSender);
			//}
		}
	}
}
