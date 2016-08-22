using System;
using System.Runtime.InteropServices;

namespace Force.Blazer.Algorithms
{
	/// <summary>
	/// Native implementation of decoder of Stream version of Blazer algorithm
	/// </summary>
	/// <remarks>Stream version is good for 'live' streamss, slightly slower than Block, but support stream flushing without
	/// losing compression rate and has very fast decoder</remarks>
	public class StreamDecoderNative : StreamDecoder
	{
		[DllImport(@"Blazer.Native.dll", CallingConvention = CallingConvention.Cdecl)]
		private static extern int blazer_stream_decompress_block(
			byte[] bufferIn, int bufferInOffset, int bufferInLength, byte[] bufferOut, int bufferOutOffset, int bufferOutLength);

		/// <summary>
		/// Initializes decoder with information about maximum uncompressed block size
		/// </summary>
		public override void Init(int maxUncompressedBlockSize)
		{
			// +8 for better copying speed. allow dummy copy by 8 bytes 
			base.Init(maxUncompressedBlockSize + 8);
		}

		/// <summary>
		/// Decompresses block of data
		/// </summary>
		public override int DecompressBlock(
			byte[] bufferIn, int bufferInOffset, int bufferInLength, byte[] bufferOut, int bufferOutOffset, int bufferOutLength)
		{
			var cnt = blazer_stream_decompress_block(
				bufferIn, bufferInOffset, bufferInLength, bufferOut, bufferOutOffset, bufferOutLength);
			if (cnt < 0)
				throw new InvalidOperationException("Invalid compressed data");
			return cnt;
		}
	}
}
