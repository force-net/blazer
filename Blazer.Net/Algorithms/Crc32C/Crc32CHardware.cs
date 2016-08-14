using System;
using System.Runtime.InteropServices;

using Force.Blazer.Native;

namespace Force.Blazer.Algorithms.Crc32C
{
	/// <summary>
	/// Native (hardware if available) Crc32C calculator. Do not use it directly, instead of use <see cref="Crc32C"/> class
	/// </summary>
	public class Crc32CHardware : ICrc32CCalculator
	{
		[DllImport(@"Blazer.Native.dll", CallingConvention = CallingConvention.Cdecl)]
		private static extern uint crc32c_append(uint crc, IntPtr buffer, int length);

		/// <summary>
		/// Constructor, will throw exception if it impossible to use hardware implementation
		/// </summary>
		public Crc32CHardware()
		{
			if (!NativeHelper.IsNativeAvailable)
				throw new InvalidOperationException("You have no right for hardware implementation");
		}

		uint ICrc32CCalculator.Calculate(byte[] buffer, int offset, int count)
		{
			var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
			var res = crc32c_append(0, handle.AddrOfPinnedObject() + offset, count);
			handle.Free();
			return res;
		}
	}
}