using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Rpc.RpcHandle
{
	/// <summary>
	/// 簡單代理的介面
	/// </summary>
	public interface ISimpleProxy
	{
		/// <summary>
		/// function call args
		/// </summary>
		event EventHandler<ProxyFunctionCallArgs> FunctionCalled;
		/// <summary>
		/// 取得透明代理
		/// </summary>
		/// <returns></returns>
		object GetProxy();
	}

	/// <summary>
	/// 類別呼叫方法
	/// </summary>
	public class ProxyFunctionCallArgs
	{
		/// <summary>
		/// 參數
		/// </summary>
		public object[] Args { get; set; }
		/// <summary>
		/// 方法名稱
		/// </summary>
		public string MethodName { get; set; }
		/// <summary>
		/// 回傳物件
		/// </summary>
		public object ReturnObject { get; set; }
	}


	/// <summary>
	/// 使用 DispatchProxy 建立一個假的透明代理物件
	/// </summary>
	public class FakeSimpleProxy<T> : DispatchProxy, ISimpleProxy where T : class
	{
		public event EventHandler<ProxyFunctionCallArgs> FunctionCalled;


		public FakeSimpleProxy() : base()
		{

		}

		private T _proxyInstance;

		/// <summary>
		/// 回傳透明代理物件
		/// </summary>
		public object GetProxy()
		{
			if (_proxyInstance == null)
			{
				_proxyInstance = Create<T, FakeSimpleProxy<T>>();
				((FakeSimpleProxy<T>)(object)_proxyInstance).FunctionCalled = this.FunctionCalled;
			}
			return _proxyInstance;
		}

		/// <summary>
		/// 攔截所有方法呼叫，並觸發事件
		/// </summary>
		protected override object Invoke(MethodInfo targetMethod, object[] args)
		{
			var eventArgs = new ProxyFunctionCallArgs
			{
				MethodName = targetMethod.Name,
				Args = args,
				ReturnObject = GetDefaultReturn(targetMethod)
			};

			FunctionCalled?.Invoke(this, eventArgs);

			return eventArgs.ReturnObject;
		}

		/// <summary>
		/// 提供預設的回傳值
		/// </summary>
		private object GetDefaultReturn(MethodInfo method)
		{
			if (method.ReturnType != typeof(void))
				return Activator.CreateInstance(method.ReturnType);

			return null;
		}
	}
}
