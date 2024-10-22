using Rpc.RpcHandle;
using Rpc.Tcp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Rpc.Util
{
	internal static class Util
	{

		internal static Dictionary<int, Communicator> ContexCatcher = new Dictionary<int, Communicator>();

		internal static List<MethodInfo> GetOperationContractMethods(Type targetType)
		{
			var ret = GetMethods(targetType).Where(m => m.GetCustomAttribute<OperationContractAttribute>() != null).ToList();
			return ret;
		}

		internal static IEnumerable<MethodInfo> GetMethods(Type type)
		{
			foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Static |
												   BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
			{
				yield return method;
			}
			if (type.IsInterface)
			{
				foreach (var iface in type.GetInterfaces())
				{
					foreach (var method in GetMethods(iface))
					{
						yield return method;
					}
				}
			}
		}

		public static T BytesToStruct<T>(byte[] data, int offset) where T : struct
		{
			T obj = default(T);
			int size = Marshal.SizeOf(obj);
			IntPtr ptr = Marshal.AllocHGlobal(size);

			try
			{
				Marshal.Copy(data, offset, ptr, size);
				obj = Marshal.PtrToStructure<T>(ptr);
			}
			finally
			{
				Marshal.FreeHGlobal(ptr);
			}


			return obj;
		}

		public static byte[] StructToBytes<T>(T structObj) where T : struct
		{
			int size = Marshal.SizeOf(structObj);
			byte[] arr = new byte[size];
			IntPtr ptr = Marshal.AllocHGlobal(size);

			try
			{
				Marshal.StructureToPtr(structObj, ptr, true);
				Marshal.Copy(ptr, arr, 0, size);
			}
			finally
			{
				Marshal.FreeHGlobal(ptr);
			}

			return arr;
		}

		public static void SetPropertyValue(object obj, string propertyName, object value)
		{
			if (obj == null) return;
			// 取得物件的類型
			Type type = obj.GetType();

			// 取得屬性
			PropertyInfo property = type.GetProperty(propertyName);

			if (property != null && property.CanWrite)
			{
				// 設定屬性的值
				property.SetValue(obj, value);
			}
			else
			{
				Console.WriteLine($"屬性 {propertyName} 不存在或不可寫入。");
			}
		}

		internal static void ContextRegiester<TServer>(CommandServer<TServer> commandServer)
		{
			var threadId = Thread.CurrentThread.ManagedThreadId;
			lock (ContexCatcher)
				if (ContexCatcher.ContainsKey(threadId))
				{
					ContexCatcher[threadId] = commandServer;
				}
				else
				{
					ContexCatcher.Add(threadId, commandServer);
				}
		}

		internal static void ContextUnRegiester<TServer>(CommandServer<TServer> commandServer)
		{
			var threadId = Thread.CurrentThread.ManagedThreadId;
			lock (ContexCatcher)
				if (ContexCatcher.ContainsKey(threadId))
				{
					ContexCatcher.Remove(threadId);
				}
		}
	}

	public static class Helper
	{
		public static IContext GetCurrentContext()
		{
			var threadId = Thread.CurrentThread.ManagedThreadId;
			if (Util.ContexCatcher.ContainsKey(threadId))
			{
				return (IContext)Util.ContexCatcher[threadId];
			}
			else
			{
				return null;
			}

		}
	}


}
