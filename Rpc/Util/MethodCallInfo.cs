using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Rpc.Util
{
	/// <summary>
	/// a method call information
	/// </summary>
	public class MethodCallInfo
	{
		public MethodInfo Method;

		public MethodCallInfo(MethodInfo method)
		{
			Method = method;
			var timeoutAttr = method.GetCustomAttribute<TimeoutForMethod>();
			if (timeoutAttr != null)
			{
				TimeoutMs = timeoutAttr.TimeoutMs;
			}
		}

		/// <summary>
		/// timeout of this method
		/// keeps -1 for system timeout
		/// </summary>
		public int TimeoutMs { get; set; } = -1;

		public string Name => Method.Name;
	}

	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
	public class TimeoutForMethod : Attribute
	{
		public TimeoutForMethod(int timeoutMs)
		{
			TimeoutMs = timeoutMs;
		}

		public int TimeoutMs { get; }
	}
}
