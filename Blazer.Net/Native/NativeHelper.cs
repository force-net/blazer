using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Force.Blazer.Native
{
	public static class NativeHelper
	{
		[DllImport("Kernel32.dll")]
		private static extern IntPtr LoadLibrary(string path);

		internal static bool IsNativeAvailable { get; private set; }

		private static readonly bool _isNativePossible;

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
			var assembly = Assembly.GetExecutingAssembly();
			var names = assembly.GetManifestResourceNames();
			using (var stream =
					assembly.GetManifestResourceStream("Force.Blazer.Resources.Blazer.Native." + architectureSuffix + ".dll"))
			{
				var assemblyName = assembly.GetName(false);
				var dllPath = Path.Combine(Path.GetTempPath(), assemblyName.Name + "." + assemblyName.Version.ToString(4), architectureSuffix);
				var fileName = Path.Combine(dllPath, "Blazer.Native.dll");
				if (!File.Exists(fileName))
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

		public static void SetNativeImplementation(bool isEnable)
		{
			IsNativeAvailable = isEnable && _isNativePossible;
		}
	}
}
