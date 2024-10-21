using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Rpc.Tcp
{
	/// <summary>
	/// 二代tcp server
	/// </summary>
	public class TcpServer : ITcpServer
	{
		private Socket _mainSocket;
		/// <summary>
		/// 已經連線的client
		/// </summary>
		protected List<Communicator> Clients = new List<Communicator>();
		/// <summary>
		/// 接收資料的buffer大小
		/// 越小越快，但是會增加cpu使用率
		/// </summary>
		public int ReceiveBufferSize { get; set; } = TcpConst.BufferSize;
		private Thread _litsenerThread;

		/// <summary>
		/// 建構子
		/// </summary>
		public TcpServer(string name = "UnNamed")
		{
			Name = name;
		}

		private void LitsenerWork()
		{
			IsOnline = true;
			while (true)
			{
				try
				{
					var newSocket = this._mainSocket.Accept();
					EndPoint endPoint = newSocket.RemoteEndPoint;
					//建立一個新的ClientInfo物件，並且將此一客戶端的Socket、IP位址、Port號碼等資訊存入
					Communicator socketInfo = new Communicator(ReceiveBufferSize)
					{
						Socket = newSocket,
						Ip = ((IPEndPoint)endPoint).Address.ToString(),
						Port = ((IPEndPoint)endPoint).Port,
					};
					//設定ClientInfo物件的DataIn事件處理函式
					socketInfo.DataIn += (sender, args) => DataIn?.Invoke(sender, args);
					//設定ClientInfo物件的Disconnected事件處理函式
					socketInfo.Disconnected += ClientInfo_Disconnected;
					//將此一客戶端的資訊存入Clients集合中
					lock (Clients)
						Clients.Add(socketInfo);
					//引發ClientConnected事件
					ClientConnected?.Invoke(this, new SocketInfoArgs()
					{
						Socket = socketInfo.Socket,
						Id = socketInfo.Id,
						Ip = socketInfo.Ip,
						Port = socketInfo.Port
					});
					//開始接收資料
					socketInfo.WaitForData();
				}
				catch
				{
					//just return
					return;
				}


			}
		}


		/// <summary>
		/// 是否上線
		/// </summary>
		public bool IsOnline
		{
			get;
			private set;
		}

		public IPEndPoint[] ListClient()
		{
			return Clients.Select(c => new IPEndPoint(IPAddress.Parse(c.Ip), c.Port)).ToArray();
		}

		/// <summary>
		/// port
		/// </summary>
		public int Port { get; set; } = 50000;
		public string Name { get; } = "UnNamed";

		public void OnLine()
		{
			lock (this)
			{
				if (IsOnline)
				{
					return;
				}

				//建立socket
				_mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
				//建立本機監聽位址及埠號，IPAddress.Any表示監聽所有的介面。
				_mainSocket.Bind(new IPEndPoint(IPAddress.Any, Port));
				// 啟動監聽
				//backlog=4 參數會指定可在佇列中等候接收的輸入連接數。若要決定可指定的最大連接數，除非同時間的連線非常的大，否則值4應該很夠用。
				_mainSocket.Listen(10);
				//將這個Socket使用keep-alive來保持長連線
				//KeepAlive函數參數說明: onOff:是否開啟Keep-Alive(開 1/ 關 0) , 
				//keepAliveTime:當開啟keep-Alive後經過多久時間(ms)開啟偵測
				//keepAliveInterval: 多久偵測一次(ms)
				_mainSocket.IOControl(IOControlCode.KeepAliveValues, KeepAlive(1, 1000, 1000), null);
				_litsenerThread = new Thread(LitsenerWork);
				_litsenerThread.Start();
				SpinWait.SpinUntil(() => IsOnline = true);//等待執行續啟動
				Log?.Invoke(this, $"Server is online at port {Port}");
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="onOff"></param>
		/// <param name="keepAliveTime"></param>
		/// <param name="keepAliveInterval"></param>
		/// <returns></returns>
		private byte[] KeepAlive(int onOff, int keepAliveTime, int keepAliveInterval)
		{
			byte[] buffer = new byte[12];
			BitConverter.GetBytes(onOff).CopyTo(buffer, 0);
			BitConverter.GetBytes(keepAliveTime).CopyTo(buffer, 4);
			BitConverter.GetBytes(keepAliveInterval).CopyTo(buffer, 8);
			return buffer;
		}


		/// <summary>
		/// 物件斷線時的處理
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ClientInfo_Disconnected(object sender, EventArgs e)
		{
			var client = (Communicator)sender;
			lock (Clients) Clients.Remove(client);
			ClientDisConnected?.Invoke(this, new SocketInfoArgs()
			{
				Socket = client.Socket,
				Id = client.Id,
				Ip = client.Ip,
				Port = client.Port
			});
			client.Dispose();
		}

		public void OffLine()
		{

			if (!IsOnline)
			{
				return;
			}

			lock (Clients)
			{
				foreach (var client in Clients)
				{
					client.Dispose();
				}
			}

			this._mainSocket.Close();
			var ret = this._litsenerThread.Join(1000);
			if (ret == false)
			{
				this._litsenerThread.Abort();
			}

			this.IsOnline = false;

		}

		public void Send(IPEndPoint remoteEndPoint, byte[] data)
		{
			var client = Clients.First(c => c.Ip == remoteEndPoint.Address.ToString() && c.Port == remoteEndPoint.Port);
			client.Send(data);
		}

		public void Send(Guid id, byte[] data)
		{
			var client = Clients.First(c => c.Id == id);
			client.Send(data);
		}


		public event EventHandler<SocketInfoArgs> ClientConnected;
		public event EventHandler<SocketInfoArgs> ClientDisConnected;
		public event EventHandler<DataInArgs> DataIn;
		public event EventHandler<string> Log;

		/// <summary>
		/// 
		/// </summary>
		public void Dispose()
		{
			this.OffLine();
		}
	}

	/// <summary>
	/// info of socket
	/// </summary>
	public class SocketInfoArgs
	{
		public Socket Socket;
		public Guid Id;
		public string Ip;
		public int Port;
	}


	/// <summary>
	/// "一次性"使用的socket資料
	/// </summary>
	public class Communicator : IDisposable
	{
		/// <summary>
		/// socket實體
		/// </summary>
		public Socket Socket;
		/// <summary>
		/// ip
		/// </summary>
		public string Ip;
		/// <summary>
		/// port
		/// </summary>
		public int Port;
		/// <summary>
		/// 資料輸入
		/// </summary>
		public event EventHandler<DataInArgs> DataIn;
		/// <summary>
		/// 斷線事件
		/// </summary>
		public event EventHandler Disconnected;
		/// <summary>
		/// 該物件id
		/// </summary>
		public Guid Id = Guid.NewGuid();
		public bool Finalized { get; internal set; }

		private readonly Thread _workThread;
		private readonly byte[] _buffer;

		/// <summary>
		/// 建構子
		/// </summary>
		/// <param name="receiveBufferSize">預設buffer size</param>
		public Communicator(int receiveBufferSize)
		{
			_buffer = new byte[receiveBufferSize];
			_workThread = new Thread(ThreadJob);
		}

		private void ThreadJob()
		{
			while (true)
			{
				try
				{
					var length = Socket.Receive(_buffer);
					if (length == 0)
					{
						Finalized = true;
						//發出中斷後訊號，因為不用及時處理，所以用Task
						Task.Factory.StartNew(() =>
							Disconnected?.Invoke(this, null));
						return;
					}
					else
					{
						var args = new DataInArgs
						{
							Id = this.Id,
							RemoteEndPoint = (IPEndPoint)Socket.RemoteEndPoint,
							Data = new byte[length]
						};
						Array.Copy(_buffer, args.Data, length);
						DataIn?.Invoke(this, args);
					}
				}
				catch
				{
					Finalized = true;
					//有任何異常，當作斷線處理
					Task.Factory.StartNew(() => Disconnected?.Invoke(this, null));
					return;
				}


			}
		}

		/// <summary>
		/// 開始等待資料
		/// </summary>
		public void WaitForData()
		{
			_workThread.Start();
		}

		/// <summary>
		/// 透過socket送出資料
		/// </summary>
		/// <param name="data"></param>
		public void Send(byte[] data)
		{
			if (Finalized == false)
				Socket.Send(data);
		}

		/// <summary>
		/// 釋放物件
		/// </summary>
		public void Dispose()
		{
			//shutdown and dispose if not finalized
			if (!Finalized)
			{
				Socket.Shutdown(SocketShutdown.Both);
				Socket.Dispose();
			}
			var ret = _workThread?.Join(1000);
			if (ret == false)
			{
				_workThread.Abort();
			}
		}
	}
}
