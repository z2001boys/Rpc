using Rpc.RpcHandle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Rpc.RpcHandle
{
	internal class SocketLayer
	{
		private byte[] _currentData = null;
		PackHeader _currentHeader = new PackHeader();
		private readonly DataPipe _pipe = new DataPipe();
		static readonly int SizeOfHeader = Marshal.SizeOf<PackHeader>();

		public SocketLayer()
		{

		}

		internal void AddData(byte[] data)
		{
			_pipe.Add(data);
			while (true)
				if (_currentData == null)
				{
					//嘗試拿出header bytea
					var canTakeHeader = _pipe.TryTake(SizeOfHeader, out var headerByte);
					if (canTakeHeader == false) return;//不能拿出
					_currentHeader = ExtractHeader(headerByte, 0);
					_currentData = new byte[_currentHeader.Length];
					//把header複製回去
					Array.Copy(headerByte, _currentData, headerByte.Length);
				}
				else
				{
					//已經有header嘗試拿出資料
					var canExtractData = _pipe.TryTake(_currentData, SizeOfHeader);
					if (canExtractData == false) return;//資料還沒滿，不能送出													

					PackIn?.Invoke(this, new DataReceiveArgs()
					{
						Header = _currentHeader,
						Data = _currentData
					});
					//釋放資料
					_currentData = null;
				}
		}

		internal event EventHandler<DataReceiveArgs> PackIn;

		public static PackHeader ExtractHeader(byte[] eData, int offset)
		{
			var headerSize = Marshal.SizeOf(typeof(PackHeader));
			byte[] headerData = new byte[headerSize];
			Array.Copy(eData, offset, headerData, 0, headerSize);
			var header = Util.Util.BytesToStruct<PackHeader>(headerData, 0);
			return header;
		}

		internal class DataReceiveArgs : EventArgs
		{
			public PackHeader Header { get; set; }
			public byte[] Data { get; set; }
		}

	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
	internal struct PackHeader
	{
		public MessageType Type;
		public int Id;
		public int Length;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
		public string Method;
	}

	public class ResponseHeader
	{
		public bool Success { get; set; }
		public string Exception { get; set; }
	}

	internal enum MessageType
	{
		Request,
		Response,
	}
}
