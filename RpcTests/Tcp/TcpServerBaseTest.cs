using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rpc.Tcp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Rpc.Tcp.Tests
{
	[TestClass()]
	public class TcpServerBaseTest
	{
		[TestMethod()]
		public void TcpServerTest()
		{
			TcpServer server = new TcpServer("Test");
			server.Dispose();
		}

		[TestMethod()]
		public void OnLineTest()
		{
			TcpServer server = new TcpServer("Test");
			server.OnLine();
			server.Dispose();

		}

		[TestMethod()]
		public void OffLineTest()
		{
			TcpServer server = new TcpServer("Test");
			server.OnLine();
			server.OffLine();
			server.Dispose();
		}

		[TestMethod]
		public void TestConnect()
		{
			using (var server = new TcpServer("Test"))
			{
				using (var client = new TcpClient())
				{
					server.OnLine();
					client.Connect();

					Assert.IsTrue(client.IsConnect);
					var ret = SpinWait.SpinUntil(() => server.ListClient().Length > 0, 1000);
					Assert.IsTrue(ret);
					client.Disconnect();

					ret = SpinWait.SpinUntil(() => server.ListClient().Length == 0, 1000);
					Assert.IsTrue(ret);

				}
			}
		}


	}

	[TestClass()]
	public class TcpSendTest
	{
		TcpServer _server;
		TcpClient _client;

		public BlockingCollection<string> ServerQueue = new BlockingCollection<string>();
		public BlockingCollection<string> ClientQueue = new BlockingCollection<string>();

		[TestInitialize]
		public void Init()
		{
			_server = new TcpServer("Test");
			_client = new TcpClient();

			_server.DataIn += _server_DataIn;
			_client.DataIn += _client_DataIn;

			_server.OnLine();
			_client.Connect();
		}

		private void _client_DataIn(object sender, DataInArgs e)
		{
			var str = Encoding.UTF8.GetString(e.Data);
			ClientQueue.Add(str);
		}

		private void _server_DataIn(object sender, DataInArgs e)
		{
			var str = Encoding.UTF8.GetString(e.Data);
			ServerQueue.Add(str);
		}

		[TestCleanup]
		public void Clearn()
		{
			_client.Dispose();
			_server.Dispose();
		}

		[TestMethod()]
		public void SendClientSendTest()
		{
			var msg = Encoding.UTF8.GetBytes("Hello");
			_client.Send(msg);

			var ret = SpinWait.SpinUntil(() => ServerQueue.Count >= 1,1000);
			Assert.IsTrue(ret);
		}
	}
}