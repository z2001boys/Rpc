using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Rpc.RpcHandle
{
	internal class Serilaizer
	{
		private readonly List<MethodInfo> methods;

		public Serilaizer(List<MethodInfo> methods)
		{
			this.methods = methods;
		}



		public static byte[] Serialize(PackHeader header, object[] args)
		{
			var eachData = new List<byte[]>();
			for (int i = 0; i < args.Length; i++)
			{
				var dataByte = MessagePack.MessagePackSerializer.Serialize(args[i], MessagePack.Resolvers.ContractlessStandardResolver.Options);
				eachData.Add(dataByte);
			}
			//calculate total length
			var dataHeaderSize = Marshal.SizeOf(typeof(DataHeader));
			var pureDataLength = eachData.Sum(x => x.Length + dataHeaderSize);
			var totalLength = Marshal.SizeOf(typeof(PackHeader)) + pureDataLength;
			header.Length = totalLength;
			var totalData = new byte[totalLength];
			var offset = 0;

			//write header
			var headerData = Util.Util.StructToBytes(header);
			Array.Copy(headerData, 0, totalData, offset, headerData.Length);
			offset += headerData.Length;
			//write each data
			for (int i = 0; i < eachData.Count; i++)
			{
				var dataHeader = new DataHeader() { Length = eachData[i].Length };
				var dataHeaderData = Util.Util.StructToBytes(dataHeader);
				Array.Copy(dataHeaderData, 0, totalData, offset, dataHeaderData.Length);
				offset += dataHeaderData.Length;
				Array.Copy(eachData[i], 0, totalData, offset, eachData[i].Length);
				offset += eachData[i].Length;
			}


			return totalData;

		}

		public static object[] Deserialize(byte[] data, Type[] dataType)
		{
			var offset = Marshal.SizeOf(typeof(PackHeader));
			var ret = new List<object>();
			while (offset < data.Length)
			{
				var header = Util.Util.BytesToStruct<DataHeader>(data, offset);
				offset += Marshal.SizeOf(typeof(DataHeader));
				var eachData = new byte[header.Length];
				Array.Copy(data, offset, eachData, 0, header.Length);
				offset += header.Length;
				var obj = MessagePack.MessagePackSerializer.Deserialize(dataType[ret.Count], eachData, MessagePack.Resolvers.ContractlessStandardResolver.Options);
				ret.Add(obj);
			}
			return ret.ToArray();

		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		public struct DataHeader
		{
			public int Length;
		}		

	}
}
