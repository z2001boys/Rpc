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
			this.ClientConnected += RpcServer_ClientConnected;
			this.ClientDisConnected += RpcServer_ClientDisConnected;
			_methods = Util.Util.GetOperationContractMethods(typeof(ServerContract));
			ServerHandle = serverHandle;
		}

		private void RpcServer_ClientDisConnected(object sender, SocketInfoArgs e)
		{
			var communicator = GetCommunicator(e.Id);
			lock (_commandProcesses)
			{
				var commandProcess = _commandProcesses.FirstOrDefault(x => x.Id == e.Id);
				if (commandProcess != null)
				{
					commandProcess.Dispose();
					_commandProcesses.Remove(commandProcess);
				}
			}

		}

		private List<CommandProcess<ServerContract>> _commandProcesses = new List<CommandProcess<ServerContract>>();

		public ServerContract ServerHandle { get; }

		/// <summary>
		/// 這個function是由accept thread呼叫的，所以不用擔心thread safe的問題
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void RpcServer_ClientConnected(object sender, SocketInfoArgs e)
		{
			var communicator = GetCommunicator(e.Id);
			var newCommandProcess = new CommandProcess<ServerContract>(ServerHandle, _methods, communicator);
			lock(_commandProcesses)
			{
				_commandProcesses.Add(newCommandProcess);
			}
		}

		Communicator GetCommunicator(Guid id)
		{
			return Clients.FirstOrDefault(x => x.Id == id);
		}
	}
}
