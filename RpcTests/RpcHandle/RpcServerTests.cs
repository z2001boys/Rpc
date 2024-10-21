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

			server.Dispose();
			client.Dispose();

		}
	}

	public interface ITestConract
	{
		[OperationContract]
		void TestNoReturn(TestClass1 data);
		[OperationContract]
		bool TestWithReturn(TestClass1 data);
	}

	public class TestContract : ITestConract
	{
		public void TestNoReturn(TestClass1 data)
		{
			Console.WriteLine(data.StringData);
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