using System;
using System.Runtime.InteropServices;

namespace Force.Blazer.Algorithms
{
	public class StreamDecoderNative : StreamDecoder
	{
		[DllImport(@"Blazer.Native.dll", CallingConvention = CallingConvention.Cdecl)]
		private static extern int blazer_v1_decompress_block(
			IntPtr bufferIn, int bufferInOffset, int bufferInLength, IntPtr bufferOut, int bufferOutOffset, int bufferOutLength);

		private GCHandle _bufferIn;

		private GCHandle _bufferOut;

		public override int DecompressBlock(
			byte[] bufferIn, int bufferInLength, byte[] bufferOut, int idxOut, int bufferOutLength)
		{
			// with current realization, this objects does not change between calls
			// TODO: refactor
			if (_bufferIn == default(GCHandle))
			{
				_bufferIn = GCHandle.Alloc(bufferIn, GCHandleType.Pinned);
				_bufferOut = GCHandle.Alloc(bufferOut, GCHandleType.Pinned);
			}

			return blazer_v1_decompress_block(
				_bufferIn.AddrOfPinnedObject(), 0, bufferInLength, _bufferOut.AddrOfPinnedObject(), idxOut, bufferOutLength);
		}

		public override void Dispose()
		{
			if (_bufferIn != default(GCHandle))
			{
				_bufferIn.Free();
				_bufferOut.Free();
			}

			base.Dispose();
		}
	}
}
