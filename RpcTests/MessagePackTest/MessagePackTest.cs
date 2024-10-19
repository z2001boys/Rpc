using MessagePack;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RpcTests.MessagePackTest
{
	[TestClass]
	public class MessagePackTest
	{


		[TestMethod]
		public void TestMessageSerailizeString()
		{
			MessagePackSerializer.DefaultOptions = MessagePack.Resolvers.ContractlessStandardResolver.Options;

			var obj = "hello";
			var bytes = MessagePack.MessagePackSerializer.Serialize(obj);
			var obj2 = MessagePack.MessagePackSerializer.Deserialize<string>(bytes);
			Assert.AreEqual(obj, obj2);


		}
		[TestMethod]
		public void TestMessageSerailizeInt()
		{
			MessagePackSerializer.DefaultOptions = MessagePack.Resolvers.ContractlessStandardResolver.Options;
			var obj = 123;
			var bytes = MessagePack.MessagePackSerializer.Serialize(obj);
			var obj2 = MessagePack.MessagePackSerializer.Deserialize<int>(bytes);
			Assert.AreEqual(obj, obj2);
		}

	}
	[Serializable]
	public class TestClass1
	{
		public string Name { get; set; } = "hello";
		public object[] Data { get; set; } = new object[]
		{
			new TestClass2(),
			new TestClass2()
		};
	}
	[Serializable]

	public class TestClass2
	{
		public string[] Data = new string[] { "str1", "str2" };
	}

}
