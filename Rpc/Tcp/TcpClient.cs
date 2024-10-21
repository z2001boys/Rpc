using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace Rpc.Tcp
{
	public class TcpClient : ITcpClient
	{
		protected Communicator Com;

		public SocketInfoArgs GetSocketInfo()
		{
			if (Com == null)
				return null;
			return new SocketInfoArgs()
			{
				Id = Com.Id,
				Socket = Com.SocketHandle,
				Ip = Com.Ip,
				Port = Com.Port
			};
		}
		public IPAddress Ip { get; set; } = IPAddress.Parse("127.0.0.1");
		public int Port { get; set; } = 50000;
		public bool IsConnect => Com != null;
		public int ReadBufferSize { get; set; } = TcpConst.BufferSize;

		public event EventHandler<CreateCommunicatorArgs> CommunicationCreating;

		public virtual void Connect()
		{
			if (IsConnect)
				return;
			lock (this)
			{
				var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
				//外部詢問
				var createArgs = new CreateCommunicatorArgs()
				{
					Socket = socket,
					ReceiveBufferSize = ReadBufferSize
				};
				CommunicationCreating?.Invoke(this, createArgs);
				//建立一個新的ClientInfo物件，並且將此一客戶端的Socket、IP位址、Port號碼等資訊存入
				Com = createArgs.Communicator ?? new Communicator(socket, ReadBufferSize);				
				Com.Disconnected += Connection_Disconnected;
				Com.DataIn += Connection_DataIn;
				try
				{
					Com.SocketHandle.Connect(Ip, Port);
				}
				catch
				{
					Com = null;
				}

				Com?.WaitForData();

			}
		}

		private void Connection_DataIn(object sender, DataInArgs e)
		{
			DataIn?.Invoke(this, e);
		}

		private void Connection_Disconnected(object sender, EventArgs e)
		{
			var tmp = Com;
			Com = null;
			Disconnected?.Invoke(this, null);
			tmp.Dispose();


		}

		public void Disconnect()
		{
			if (IsConnect == false)
				return;
			Com.Dispose();
		}

		public void Send(byte[] data)
		{
			if (IsConnect == false)
				return;
			Com.Send(data);
		}

		public event EventHandler<DataInArgs> DataIn;
		public event EventHandler Disconnected;

		public void Dispose()
		{
			this.Disconnect();
		}
	}
}
