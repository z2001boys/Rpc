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

		//cancel token
		CancellationTokenSource _cts;

		public RpcClient()
		{
			_proxy = new ProxyProcessor(typeof(TServer));
			_proxy.FunctionCalled += FunctionCall;
			//掛上行為
			this.CommunicationCreating += (ss, ee) =>
			{
				var com = new CommandSender<TServer>(ee.Socket, ee.ReceiveBufferSize);
				com.ProcessTimeOutMs = this.ProcessTimeOutMs;
				ee.Communicator = com;
			};

			this.Disconnected += (ss, ee) =>
			{
				_cts.Cancel();
			};
		}

		private void FunctionCall(object sender, ProxyFunctionCallArgs e)
		{
			if(this.IsConnect==false)
				throw new Exception("Not connected");
			if(this.Com==null)
				throw new Exception("Communicator not created");
			if(Com is CommandSender<TServer> senderCom)
			{
				senderCom.Call(e);
			}


		}

		public TServer Proxy => (TServer)_proxy.GetTransparentProxy();

		private int _timeOutMs = 1000;
		public int ProcessTimeOutMs
		{
			get => _timeOutMs;
			set
			{
				if (value == _timeOutMs) return;
				_timeOutMs = value;
				if (Com is CommandSender<TServer> senderCom)
				{
					senderCom.ProcessTimeOutMs = value;
				}
			}
		}

		public override void Connect()
		{
			_cts = new CancellationTokenSource();			
			base.Connect();
		}



	}
}
