using Rpc.RpcHandle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Rpc.Util
{
	internal static class Util
	{
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
	}
}
