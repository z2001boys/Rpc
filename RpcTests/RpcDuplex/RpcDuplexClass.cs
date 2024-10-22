using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace RpcTests.RpcDuplex
{
	[TestClass]
	public class RpcDuplexClass
	{
		[TestMethod]
		public void DuplexTest()
		{
			int clientCallCount = 0;
			var serverContract = new TestContractImpl();
			var clientContract = new TestContractCallbackImpl();
			clientContract.OnAddEvent = (e) =>
			{
				clientCallCount++;
			};
			var server = new Rpc.RpcHandle.DuplexRpc.DuplexRpcServer<TestContract, TestContractCallback>(serverContract);
			var client = new Rpc.RpcHandle.DuplexRpc.DuplexRpcClient<TestContract, TestContractCallback>(clientContract);
			client.ProcessTimeOutMs = 9999999;
			server.OnLine();
			client.Connect();

			var ret = client.Proxy.Add(1, 2);
			Assert.IsTrue(3 == ret);

			Assert.IsTrue(clientCallCount == 1);

			server.OffLine();

		}

	}

	public interface TestContract
	{
		[OperationContract]
		int Add(int a, int b);
	}

	public interface TestContractCallback
	{
		[OperationContract]
		void OnAdd(int result);
	}

	public class TestContractImpl : TestContract
	{
		public int Add(int a, int b)
		{
			var callBackContract = Rpc.Util.Helper.GetCurrentContext().GetContract<TestContractCallback>();
			callBackContract.OnAdd(a + b);
			return a + b;
		}
	}

	public class TestContractCallbackImpl : TestContractCallback
	{
		public Action<int> OnAddEvent;
		public void OnAdd(int result)
		{
			OnAddEvent?.Invoke(result);
		}
	}

}
