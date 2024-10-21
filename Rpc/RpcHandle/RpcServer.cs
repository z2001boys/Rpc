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

			this.CommunicationCreating += (ss, ee) =>
			{
				var com = new CommandReciver<ServerContract>(serverHandle, _methods, ee.Socket, ee.ReceiveBufferSize);
				ee.Communicator = com;
			};

		}



		private List<CommandReciver<ServerContract>> _commandProcesses = new List<CommandReciver<ServerContract>>();

		public ServerContract ServerHandle { get; }


		protected Communicator GetCommunicator(Guid id)
		{
			return Clients.FirstOrDefault(x => x.Id == id);
		}
	}
}
