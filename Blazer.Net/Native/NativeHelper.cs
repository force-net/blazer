using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Force.Blazer.Native
{
	/// <summary>
	/// Helper for native implementation ofr Blazer algorithms
	/// </summary>
	public static class NativeHelper
	{
		// check tests if changed
		private const string NativeSuffix = "0.8.3.9";

#if NETCORE
		private const string ResourcePrefix = "Blazer.Net.Resources.Blazer.Native.";
#else
		private const string ResourcePrefix = "Force.Blazer.Resources.Blazer.Native.";
#endif

		private static readonly bool _isNativePossible;

		[DllImport("Kernel32.dll")]
		private static extern IntPtr LoadLibrary(string path);

		/// <summary>
		/// Returns is native library is available for usage
		/// </summary>
		public static bool IsNativeAvailable { get; private set; }

		static NativeHelper()
		{
			_isNativePossible = Init();
			IsNativeAvailable = _isNativePossible;
		}

		private static bool Init()
		{
			try
			{
				InitInternal();
				return true;
			}
			catch (Exception) // will use software realization
			{
				return false;
			}
		}

		private static void InitInternal()
		{
			var architectureSuffix = IntPtr.Size == 8 ? "x64" : "x86";
#if NETCORE
			var assembly = typeof(NativeHelper).GetTypeInfo().Assembly;
#else
			var assembly = typeof(NativeHelper).Assembly;
#endif
			using (var stream =
					assembly.GetManifestResourceStream(ResourcePrefix + architectureSuffix + ".dll"))
			{
				if (stream == null)
					throw new InvalidOperationException("Missing resource stream");

#if NETCORE
				var assemblyName = assembly.GetName();
#else
				var assemblyName = assembly.GetName(false);
#endif				
				
				var dllPath = Path.Combine(Path.GetTempPath(), assemblyName.Name + "." + NativeSuffix, architectureSuffix);
				
				var fileName = Path.Combine(dllPath, "Blazer.Native.dll");
				if (!File.Exists(fileName) || new FileInfo(fileName).Length == 0)
				{
					Directory.CreateDirectory(dllPath);
					using (var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite, 16386/*, FileOptions.DeleteOnClose*/))
					{
						stream.CopyTo(fs);
					}
				}

				if (LoadLibrary(fileName) == IntPtr.Zero)
					throw new InvalidOperationException("Unexpected error in dll loading");
			}
		}

		/// <summary>
		/// Sets native implementation is enabled.
		/// </summary>
		/// <remarks>Native implementation can be turned off manually. If current environment does not support native implementation, software will be used anyway</remarks>
		public static void SetNativeImplementation(bool isEnable)
		{
			IsNativeAvailable = isEnable && _isNativePossible;
		}
	}
}
