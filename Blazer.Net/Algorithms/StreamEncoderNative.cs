using System.Runtime.InteropServices;

namespace Force.Blazer.Algorithms
{
	public class StreamEncoderNative : StreamEncoder
	{
		[DllImport(@"Blazer.Native.dll", CallingConvention = CallingConvention.Cdecl)]
		private static extern int blazer_stream_compress_block(
			byte[] bufferIn, int bufferInOffset, int bufferInLength, int globalOffset, byte[] bufferOut, int bufferOutOffset, int[] hashArr);

		/*private GCHandle _bufferInHandle;
		private GCHandle _bufferOutHandle;
		private GCHandle _hashArrHandle;

		public override void Init(int maxInBlockSize, int additionalHeaderSizeForOut, Action<byte[], int, bool> onBlockPrepared)
		{
			base.Init(maxInBlockSize, additionalHeaderSizeForOut, onBlockPrepared);
			_bufferInHandle = GCHandle.Alloc(_bufferIn, GCHandleType.Pinned);
			_bufferOutHandle = GCHandle.Alloc(_bufferOut, GCHandleType.Pinned);
			_hashArrHandle = GCHandle.Alloc(_hashArr, GCHandleType.Pinned);
		}*/

		protected override int CompressBlock(
			byte[] bufferIn,
			int bufferInOffset,
			int bufferInLength,
			int bufferInShift,
			byte[] bufferOut,
			int bufferOutOffset)
		{
			return blazer_stream_compress_block(
				bufferIn,
				bufferInOffset,
				bufferInLength,
				bufferInShift,
				bufferOut,
				bufferOutOffset,
				_hashArr);
		}

		/*public override void Dispose()
		{
			_bufferInHandle.Free();
			_bufferOutHandle.Free();
			_hashArrHandle.Free();
			base.Dispose();
		}*/
	}
}
