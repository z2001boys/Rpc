using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
using System.Text;
using System.Threading.Tasks;

namespace Rpc.RpcHandle
{
	internal class ProxyProcessor : RealProxy, ISimpleProxy
	{
		public ProxyProcessor(Type target) : base(target)
		{
		}

		public event EventHandler<ProxyFunctionCallArgs> FunctionCalled;
		public object GetProxy()
		{
			return this.GetTransparentProxy();
		}


		public override IMessage Invoke(IMessage msg)
		{
			IMethodCallMessage methodCall = (IMethodCallMessage)msg;

			var args = new ProxyFunctionCallArgs()
			{
				Args = methodCall.Args,
				MethodName = methodCall.MethodName
			};

			FunctionCalled?.Invoke(this, args);

			return new ReturnMessage(args.ReturnObject, methodCall.Args, methodCall.ArgCount, methodCall.LogicalCallContext, methodCall);
		}


	}
}
