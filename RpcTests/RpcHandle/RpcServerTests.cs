using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rpc.RpcHandle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Rpc.RpcHandle.Tests
{
	[TestClass()]
	public class RpcServerTests
	{
		[TestMethod()]
		public void RpcServerTest()
		{
			var server = new RpcServer<ITestConract>(new TestContract());
			server.OnLine();
			var client = new RpcClient<ITestConract>();
			client.Connect();

			var testData = new TestClass1();

			client.ProcessTimeOutMs = 9999999;
			client.Proxy.TestNoReturn(testData);
			client.Proxy.TestWithReturn(testData);
			client.Proxy.TestSendEnum(TestEnum.A);

			server.Dispose();
			client.Dispose();
		}

		[TestMethod()]
		[DataRow(2)]
		[DataRow(5)]
		public void TestWithMultiClient(int clientNum)
		{
			var server = new RpcServer<ITestConract>(new TestContract());
			server.OnLine();
			var clients = new List<RpcClient<ITestConract>>();
			for (int i = 0; i < clientNum; i++)
			{
				var client = new RpcClient<ITestConract>();
				client.Connect();
				clients.Add(client);
			}

			var testData = new TestClass1();

			//test multi client process at same time
			Parallel.ForEach(clients, (client) =>
			{
				client.ProcessTimeOutMs = 9999999;
				for (int i = 0; i < 100; i++)
				{
					client.Proxy.TestNoReturn(testData);
					client.Proxy.TestWithReturn(testData);
				}
				
			});

			server.Dispose();
			foreach (var client in clients)
			{
				client.Dispose();
			}


		}

	}

	public interface ITestConract
	{
		[OperationContract]
		void TestNoReturn(TestClass1 data);
		[OperationContract]
		bool TestWithReturn(TestClass1 data);
		[OperationContract]
		void TestSendEnum(TestEnum data);
	}

	public enum TestEnum
	{
		A,B
	}

	public class TestContract : ITestConract
	{
		public void TestNoReturn(TestClass1 data)
		{
			Console.WriteLine(data.StringData);
		}

		public void TestSendEnum(TestEnum data)
		{
			//do nothing
		}

		public bool TestWithReturn(TestClass1 data)
		{
			Console.WriteLine(data.StringData);
			return true;
		}


	}

	public class TestClass1
	{
		public string StringData = "";
		public int IntData = 0;
		public double DoubleData = 1;
		public double[] DoubleArray = new double[] { 1, 2, 3, 4, 5 };
		public byte[] DataByte = new byte[10];
		public TestClass2 TestClass2 = new TestClass2();
		public TestClass2[] TestClass2Array = new TestClass2[] { new TestClass2(), new TestClass2() };
	}

	public class TestClass2
	{
		public string StringData = "";
		public int IntData = 0;
		public double DoubleData = 1;
		public double[] DoubleArray = new double[] { 1, 2, 3, 4, 5 };
	}

}