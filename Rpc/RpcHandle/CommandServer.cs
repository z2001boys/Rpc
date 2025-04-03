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
	internal class CommandServer<TServer> : CmdCommunicator
	{
		private readonly TServer _serverHandle;
		private readonly List<MethodCallInfo> _methods;
		private Threader _threader;//記得釋放
		public event EventHandler Disposing;

		public CommandServer(TServer serverHandle,
			List<MethodCallInfo> methods,
			Socket s,
			int reciverSize) : base(s, reciverSize)
		{
			this._serverHandle = serverHandle;
			this._methods = methods;
			this.PackIn += PackDataIn;
			this.Disconnected += (ss, ee) =>
			{
				Disposing?.Invoke(this, null);
				_threader.Dispose();
			};

			_threader = new Threader(1);

		}

		private void PackDataIn(object sender, DataReceiveArgs e)
		{
			if (e.Header.Type == MessageType.Request)
			{
				_threader.Start(() => ProcessCommand(e));
			}
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
			var argTypes = method.Method.GetParameters().Select(x => x.ParameterType).ToArray();
			var args = Serilaizer.Deserialize(e.Data, argTypes);

			//紀錄context
			var threadId = Thread.CurrentThread.ManagedThreadId;
			Util.Util.ContextRegiester(this);

			//invoke
			object result = null;
			string exceptionReason = "";
			try
			{
				result = method.Method.Invoke(_serverHandle, args);

				// ★加入判斷：等待非同步Task結果
				if (result is Task taskResult)
				{
					taskResult.GetAwaiter().GetResult(); //同步等待

					var returnType = method.Method.ReturnType;
					if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
					{
						var resultProperty = returnType.GetProperty("Result");
						result = resultProperty.GetValue(taskResult);
					}
					else
					{
						// Task無回傳值
						result = null;
					}
				}
			}
			catch (TargetInvocationException ex)
			{
				exceptionReason = ex.InnerException?.Message ?? ex.Message;
			}
			catch (Exception ex)
			{
				exceptionReason = ex.Message;
			}
			finally
			{
				//remove context
				Util.Util.ContextUnRegiester(this);
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

			if (method.Method.ReturnType == typeof(void) || method.Method.ReturnType == typeof(Task))
			{
				//no need to send back data
				var data = Serilaizer.Serialize(returnHeader, new object[] { responseHeader });
				this.Send(data);
				return;
			}
			else
			{
				// send back data
				var data = Serilaizer.Serialize(returnHeader, new object[] { responseHeader, result });
				this.Send(data);
			}
		}





	}
}
