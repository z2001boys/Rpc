using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static Rpc.RpcHandle.Serilaizer;

namespace Rpc.RpcHandle
{
	public class ResponseResult
	{
		public bool Success { get; }
		public object Result { get; }
		public string ErrorReason { get; }
		public ResponseResult(MethodInfo info, byte[] data)
		{
			int offset = Marshal.SizeOf(typeof(PackHeader));
			int dataHeaderSize = Marshal.SizeOf(typeof(DataHeader));
			//get first data header
			var dataHeader = Util.Util.BytesToStruct<DataHeader>(data, offset);
			offset += dataHeaderSize;
			//extract response header with message pack
			var responseHeaderByte = new byte[dataHeader.Length];
			Array.Copy(data, offset, responseHeaderByte, 0, dataHeader.Length);
			var responseHeader = MessagePack.MessagePackSerializer.Deserialize<ResponseHeader>(responseHeaderByte,
					MessagePack.Resolvers.ContractlessStandardResolver.Options);

			//check the data is exist	
			Success = responseHeader.Success;
			if (responseHeader.Success == false)
			{
				ErrorReason = responseHeader.Exception;
				return;
			}

			//extract data
			offset += dataHeader.Length;
			if (info.ReturnType == typeof(void))
			{
				Result = null;
				return;
			}


			var solveType = info.ReturnType;
			//如果是task
			if (solveType.IsGenericType && solveType.GetGenericTypeDefinition() == typeof(Task<>))
			{
				solveType = solveType.GetGenericArguments()[0];
			}

			dataHeader = Util.Util.BytesToStruct<DataHeader>(data, offset);
			offset += dataHeaderSize;
			var eachData = new byte[dataHeader.Length];
			Array.Copy(data, offset, eachData, 0, dataHeader.Length);
			Result = MessagePack.MessagePackSerializer.Deserialize(solveType, eachData, MessagePack.Resolvers.ContractlessStandardResolver.Options);

		}
	}
}
