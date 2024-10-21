using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Rpc.Tcp
{
	public class TcpClient : ITcpClient
	{
		private Communicator _connection;

		public SocketInfoArgs GetSocketInfo()
		{
			if (_connection == null)
				return null;
			return new SocketInfoArgs()
			{
				Id = _connection.Id,
				Socket = _connection.Socket,
				Ip = _connection.Ip,
				Port = _connection.Port
			};
		}
		public IPAddress Ip { get; set; } = IPAddress.Parse("127.0.0.1");
		public int Port { get; set; } = 50000;
		public bool IsConnect => _connection != null;
		public int ReadBufferSize { get; set; } = TcpConst.BufferSize;

		public virtual void Connect()
		{
			if (IsConnect)
				return;
			lock (this)
			{
				_connection = new Communicator(ReadBufferSize);
				_connection.Disconnected += Connection_Disconnected;
				_connection.DataIn += Connection_DataIn;
				_connection.Ip = Ip.ToString();
				_connection.Port = Port;
				_connection.Socket = new System.Net.Sockets.Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
				try
				{
					_connection.Socket.Connect(Ip, Port);
				}
				catch
				{
					_connection = null;
				}

				_connection?.WaitForData();

			}
		}

		private void Connection_DataIn(object sender, DataInArgs e)
		{
			DataIn?.Invoke(this, e);
		}

		private void Connection_Disconnected(object sender, EventArgs e)
		{
			var tmp = _connection;
			_connection = null;
			Disconnected?.Invoke(this, null);
			tmp.Dispose();


		}

		public void Disconnect()
		{
			if (IsConnect == false)
				return;
			_connection.Dispose();
		}

		public void Send(byte[] data)
		{
			if (IsConnect == false)
				return;
			_connection.Send(data);
		}

		public event EventHandler<DataInArgs> DataIn;
		public event EventHandler Disconnected;

		public void Dispose()
		{
			this.Disconnect();
		}
	}
}
