using Rpc.Tcp;
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
	public class RpcClient<TServer> : Rpc.Tcp.TcpClient where TServer : class
	{


		//cancel token
		CancellationTokenSource _cts;

		public RpcClient()
		{
			//掛上行為
			this.CommunicationCreating += OnCommunicationCreating;

			this.Disconnected += (ss, ee) =>
			{
				_cts.Cancel();
			};
		}

		internal virtual void OnCommunicationCreating(object sender, CreateCommunicatorArgs args)
		{
			var com = new CommandClient<TServer>(args.Socket, args.ReceiveBufferSize);
			com.ProcessTimeOutMs = this.ProcessTimeOutMs;
			args.Communicator = com;
		}

		//internal virtual void FunctionCall(object sender, ProxyFunctionCallArgs e)
		//{
		//	if (this.IsConnect == false)
		//		throw new Exception("Not connected");
		//	if (this.Com == null)
		//		throw new Exception("Communicator not created");
		//	if (Com is CommandClient<TServer> senderCom)
		//	{
		//		senderCom.Call(e);
		//	}
		//}

		public TServer Proxy
		{
			get
			{
				if (Com == null)
					throw new Exception("Communicator not created");
				if (Com is IContext ic)
					return ic.GetContract<TServer>();
				throw new Exception("Communicator not support IContext");
			}
		}


		private int _timeOutMs = 1000;
		public int ProcessTimeOutMs
		{
			get => _timeOutMs;
			set
			{
				if (value == _timeOutMs) return;
				_timeOutMs = value;
				//set property by reflection to com

				Util.Util.SetPropertyValue(Com, "ProcessTimeOutMs", value);

			}
		}

		public override void Connect()
		{
			_cts = new CancellationTokenSource();
			base.Connect();
		}



	}
}
