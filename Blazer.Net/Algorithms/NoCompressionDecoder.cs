using System;

namespace Force.Blazer.Algorithms
{
	public class NoCompressionDecoder : IDecoder
	{
		private byte[] _innerBuffer;

		private int _innerBufferPos;

		private int _innerBufferLen;

		private Func<byte[], Tuple<int, bool, bool>> _needNewBlock;

		public int Read(byte[] buffer, int offset, int count)
		{
			if (_innerBufferPos == _innerBufferLen)
			{
				var res = _needNewBlock(_innerBuffer);
				if (!res.Item3) return 0;
				_innerBufferPos = 0;
				_innerBufferLen = res.Item1;
			}

			count = Math.Min(_innerBufferLen - _innerBufferPos, count);
			Buffer.BlockCopy(_innerBuffer, _innerBufferPos, buffer, offset, count);
			_innerBufferPos += count;
			return count;
		}

		public void Init(int maxUncompressedBlockSize, Func<byte[], Tuple<int, bool, bool>> getNextBlock)
		{
			_innerBuffer = new byte[maxUncompressedBlockSize];
			_needNewBlock = getNextBlock;
		}

		public BlazerAlgorithm GetAlgorithmId()
		{
			return BlazerAlgorithm.NoCompress;
		}

		public void Dispose()
		{
		}
	}
}
