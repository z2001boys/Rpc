using System;
using System.Collections.Generic;
using System.Linq;
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
}
