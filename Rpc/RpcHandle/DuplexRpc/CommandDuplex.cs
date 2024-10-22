using Rpc.Tcp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Rpc.RpcHandle.DuplexRpc
{
	internal class CommandDuplex<TServer, TCallBack> : CommandServer<TServer>, IContext
	{

		CommandClient<TCallBack> _client;

		public CommandDuplex(TServer serverHandle,
			List<MethodInfo> methodInfo,
			Socket s,
			int receiverSize) : base(serverHandle, methodInfo, s, receiverSize)
		{
			_client = new CommandClient<TCallBack>(s, receiverSize);
			this.PackIn += (ss, ee) => _client.OnPackIn(ss, ee);
		}

		public int ProcessTimeOutMs
		{
			get => _client.ProcessTimeOutMs;
			set => _client.ProcessTimeOutMs = value;
		}

		public TContract GetContract<TContract>()
		{
			return _client.GetContract<TContract>();
		}
	}
}
