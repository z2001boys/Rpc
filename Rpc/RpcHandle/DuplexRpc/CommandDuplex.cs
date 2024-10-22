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
	/// <summary>
	/// a internal class for duplex command
	/// </summary>
	/// <typeparam name="TServer"></typeparam>
	/// <typeparam name="TCallBack"></typeparam>
	internal class CommandDuplex<TServer, TCallBack> : CommandServer<TServer>, IContext
	{

		CommandClient<TCallBack> _client;

		/// <summary>
		/// command process for duplex
		/// </summary>
		/// <param name="serverHandle">server's handle</param>
		/// <param name="methodInfo">methods</param>
		/// <param name="so">new socket</param>
		/// <param name="receiverSize">socket receive size</param>
		public CommandDuplex(
			TServer serverHandle,
			List<MethodInfo> methodInfo,
			Socket so,
			int receiverSize) : base(serverHandle, methodInfo, so, receiverSize)
		{
			_client = new CommandClient<TCallBack>(so, receiverSize);
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
