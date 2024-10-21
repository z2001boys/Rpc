using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rpc.RpcHandle
{
	public class DataPipe : IDisposable
	{
		private List<byte[]> _datas = new List<byte[]>();
		public int TotalLength { get; private set; } = 0;
		private int _offset = 0;
		/// <summary>
		/// 加入一筆新資料
		/// </summary>
		/// <param name="data"></param>
		public void Add(byte[] data)
		{
			if (data.Length == 0)
				return;
			lock (this)
			{
				_datas.Add(data);
				TotalLength += data.Length;
			}
		}

		/// <summary>
		/// 嘗試提取指定數量
		/// </summary>
		/// <param name="reqLength"></param>
		/// <param name="outputData"></param>
		/// <returns></returns>
		public bool TryTake(int reqLength, out byte[] outputData)
		{
			lock (this)
			{
				if (reqLength == 0)
				{
					outputData = new byte[0];
					return true;
				}

				if (reqLength > TotalLength)
				{
					outputData = null;
					return false;
				}

				outputData = new byte[reqLength];

				var copiedSize = 0;
				while (copiedSize < reqLength)
				{
					var remindSize = reqLength - copiedSize;
					var canCopySize = _datas[0].Length - _offset;
					if (canCopySize >= remindSize)
					{
						Array.Copy(_datas[0], _offset, outputData, copiedSize, remindSize);
						TotalLength -= remindSize;
						copiedSize += remindSize;
						_offset += remindSize;
					}
					else
					{
						Array.Copy(_datas[0], _offset, outputData, copiedSize, canCopySize);
						TotalLength -= canCopySize;
						copiedSize += canCopySize;
						_offset += canCopySize;
					}

					if (_offset == _datas[0].Length)
					{
						_offset = 0;
						_datas.RemoveAt(0);
					}

				}

				return true;
			}
		}

		/// <summary>
		/// 嘗試提取指定數量
		/// </summary>
		/// <param name="outputData"></param>
		/// <param name="outputOffset"></param>
		/// <returns></returns>
		public bool TryTake(byte[] outputData, int outputOffset = 0)
		{
			lock (this)
			{
				int reqLength = outputData.Length - outputOffset;
				if (TotalLength == 0)
				{
					outputData = new byte[0];
					return true;
				}

				if (reqLength > TotalLength)
				{
					outputData = null;
					return false;
				}


				var copiedSize = 0;
				while (copiedSize < reqLength)
				{
					var remindSize = reqLength - copiedSize;
					var canCopySize = _datas[0].Length - _offset;
					if (canCopySize >= remindSize)
					{
						Array.Copy(_datas[0], _offset, outputData, copiedSize + outputOffset, remindSize);
						TotalLength -= remindSize;
						copiedSize += remindSize;
						_offset += remindSize;
					}
					else
					{
						Array.Copy(_datas[0], _offset, outputData, copiedSize + outputOffset, canCopySize);
						TotalLength -= canCopySize;
						copiedSize += canCopySize;
						_offset += canCopySize;
					}

					if (_offset == _datas[0].Length)
					{
						_offset = 0;
						_datas.RemoveAt(0);
					}

				}

				return true;
			}
		}

		/// <summary>
		/// 釋放階段
		/// </summary>
		public void Dispose()
		{
			lock (this)
			{
				_datas.Clear();
				TotalLength = 0;
				_offset = 0;
			}
		}
	}

}
