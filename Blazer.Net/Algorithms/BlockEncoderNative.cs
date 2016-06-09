using System.Runtime.InteropServices;

namespace Force.Blazer.Algorithms
{
	public class BlockEncoderNative : BlockEncoder
	{
		[DllImport(@"Blazer.Native.dll", CallingConvention = CallingConvention.Cdecl)]
		private static extern int blazer_block_compress_block(
			byte[] bufferIn, int bufferInOffset, int bufferInLength, byte[] bufferOut, int bufferOutOffset);

		// private GCHandle _bufferInHandle;
		// private GCHandle _bufferOutHandle;

		/*public override void Init(int maxInBlockSize, int additionalHeaderSizeForOut, Action<byte[], int, bool> onBlockPrepared)
		{
			base.Init(maxInBlockSize, additionalHeaderSizeForOut, onBlockPrepared);
			_bufferInHandle = GCHandle.Alloc(_bufferIn, GCHandleType.Pinned);
			_bufferOutHandle = GCHandle.Alloc(_bufferOut, GCHandleType.Pinned);
			_bufferInPtr = _bufferInHandle.AddrOfPinnedObject();
			_bufferOutPtr = _bufferOutHandle.AddrOfPinnedObject();
		}*/

		public override int CompressBlock(
			byte[] bufferIn,
			int bufferInOffset,
			int bufferInCount,
			byte[] bufferOut,
			int bufferOutOffset)
		{
			return blazer_block_compress_block(
				bufferIn,
				bufferInOffset,
				bufferInCount,
				_bufferOut,
				bufferOutOffset);
		}

		/*public override void Dispose()
		{
			_bufferInHandle.Free();
			_bufferOutHandle.Free();
			base.Dispose();
		}*/
	}
}
