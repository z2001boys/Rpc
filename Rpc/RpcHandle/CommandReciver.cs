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
using static Rpc.RpcHandle.Serilaizer;
using static Rpc.RpcHandle.CmdCommunicator;
using System.Net.Sockets;

namespace Rpc.RpcHandle
{
	internal class CommandReciver<TServer> : CmdCommunicator, IDisposable
	{
		private readonly TServer _serverHandle;
		private readonly List<MethodInfo> _methods;						
		private Threader _threader;//記得釋放
		public event EventHandler Disposing;

		public CommandReciver(TServer serverHandle, List<MethodInfo> methods,
			Socket s, int reciverSize) : base(s, reciverSize)
		{
			this._serverHandle = serverHandle;
			this._methods = methods;

			this.PackIn += (ss,ee) => ProcessCommand(ee);

			_threader = new Threader(1);

		}


		internal virtual void ProcessCommand(DataReceiveArgs e)
		{
			var method = _methods.FirstOrDefault(x => x.Name == e.Header.Method);
			if (method == null)
			{
				//error hanlding
				return;
			}

			//get all arg's type
			var argTypes = method.GetParameters().Select(x => x.ParameterType).ToArray();
			var args = Serilaizer.Deserialize(e.Data, argTypes);

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
			if (method.ReturnType == typeof(void))
			{
				//no need to send back data
				var data = Serilaizer.Serialize(returnHeader, new object[] { responseHeader });
				this.Send(data);
				return;
			}
			else
			{
				var data = Serilaizer.Serialize(returnHeader, new object[] { responseHeader, result });
				this.Send(data);
			}
		}


		public override void Dispose()
		{
			Disposing?.Invoke(this, null);
			_threader.Dispose();
			base.Dispose();
		}


	}
}
