using Rpc.RpcHandle;
using Rpc.Tcp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Proxies;
using System.Text;
using System.Threading.Tasks;

namespace Rpc.RpcHandle
{
	public class RpcServer<ServerContract> : TcpServer
	{
		List<MethodInfo> _methods;
		public RpcServer(ServerContract serverHandle)
		{

			_methods = Util.Util.GetOperationContractMethods(typeof(ServerContract));
			ServerHandle = serverHandle;

			this.CommunicationCreating += RpcServer_CommunicationCreating;

		}

		internal virtual void RpcServer_CommunicationCreating(object sender, CreateCommunicatorArgs args)
		{
			var com = new CommandServer<ServerContract>(ServerHandle, _methods, args.Socket, args.ReceiveBufferSize);
			args.Communicator = com;
		}

		private List<CommandServer<ServerContract>> _commandProcesses = new List<CommandServer<ServerContract>>();

		public ServerContract ServerHandle { get; }


		protected Communicator GetCommunicator(Guid id)
		{
			return Clients.FirstOrDefault(x => x.Id == id);
		}
	}
}
