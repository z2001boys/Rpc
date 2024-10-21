using Rpc.Tcp;
using Rpc.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Rpc.RpcHandle.SerializeLayer;
using static Rpc.RpcHandle.SocketLayer;

namespace Rpc.RpcHandle
{
	internal class CommandProcess<TServer> : IDisposable
	{
		private readonly TServer _serverHandle;
		private readonly List<MethodInfo> _methods;
		private readonly Communicator _com;
		private SerializeLayer _serializeLayer;
		private SocketLayer _socketLayer;
		private Threader _threader;//記得釋放

		public Guid Id => _com.Id;

		public CommandProcess(TServer serverHandle, List<MethodInfo> methods, Communicator com)
		{
			this._serverHandle = serverHandle;
			this._methods = methods;
			this._com = com;

			_serializeLayer = new SerializeLayer(methods);
			_socketLayer = new SocketLayer();

			//tcp的資料進入，後續由socket layer處理
			com.DataIn += Com_DataIn;
			//socket layer的資料進入，後續由serialize layer處理
			_socketLayer.PackIn += PackIn;

			_threader = new Threader(3);

		}

		private void PackIn(object sender, SocketLayer.DataReceiveArgs e)
		{
			_threader.Start(() => ProcessCommand(e));
		}

		private void ProcessCommand(DataReceiveArgs e)
		{
			var method = _methods.FirstOrDefault(x => x.Name == e.Header.Method);
			if (method == null)
			{
				//error hanlding
				return;
			}

			//get all arg's type
			var argTypes = method.GetParameters().Select(x => x.ParameterType).ToArray();
			var args = SerializeLayer.Deserialize(e.Data, argTypes);

			//invoke
			object result = null;
			string exceptionReason = "";
			try
			{
				result = method.Invoke(_serverHandle, args);
			}
			catch (Exception ex)
			{
				exceptionReason = ex.Message;
			}

			//send back
			PackHeader returnHeader = new PackHeader()
			{
				Type = MessageType.Response,
				Id = e.Header.Id,
				Method = e.Header.Method,
			};
			var responseHeader = new ResponseHeader()
			{
				Success = exceptionReason == "",
				Exception = exceptionReason
			};
			if(method.ReturnType == typeof(void))
			{
				//no need to send back data
				var data = SerializeLayer.Serialize(returnHeader, new object[] { responseHeader });
				_com.Send(data);
				return;
			}
			else
			{
				var data = SerializeLayer.Serialize(returnHeader, new object[] { responseHeader, result });
				_com.Send(data);
			}
			

		}

		private void Com_DataIn(object sender, DataInArgs e)
		{
			_socketLayer.AddData(e.Data);
		}

		public void Dispose()
		{
			
		}

		
	}
}
