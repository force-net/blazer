using System;
using System.Runtime.InteropServices;

namespace Force.Blazer.Algorithms
{
	public class StreamDecoderNative : StreamDecoder
	{
		[DllImport(@"Blazer.Native.dll", CallingConvention = CallingConvention.Cdecl)]
		private static extern int blazer_stream_decompress_block(
			byte[] bufferIn, int bufferInOffset, int bufferInLength, byte[] bufferOut, int bufferOutOffset, int bufferOutLength);

		// private GCHandle _bufferIn;
		// private GCHandle _bufferOut;

		public override int DecompressBlock(
			byte[] bufferIn, int bufferInLength, byte[] bufferOut, int idxOut, int bufferOutLength)
		{
			// with current realization, this objects does not change between calls
			// TODO: refactor
			/*if (_bufferIn == default(GCHandle))
			{
				_bufferIn = GCHandle.Alloc(bufferIn, GCHandleType.Pinned);
				_bufferOut = GCHandle.Alloc(bufferOut, GCHandleType.Pinned);
			}*/

			return blazer_stream_decompress_block(
				bufferIn, 0, bufferInLength, bufferOut, idxOut, bufferOutLength);
		}

		/*public override void Dispose()
		{
			if (_bufferIn != default(GCHandle))
			{
				_bufferIn.Free();
				_bufferOut.Free();
			}

			base.Dispose();
		}*/
	}
}
