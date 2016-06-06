using System;

namespace Force.Blazer.Algorithms
{
	public class BlockDecoder : IDecoder
	{
		// should be equal with BlockEncoder
		private const int HASH_TABLE_BITS = 16;
		private const int HASH_TABLE_LEN = (1 << HASH_TABLE_BITS) - 1;
		private const int MIN_SEQ_LEN = 4;
		// carefully selected random number
		private const uint Mul = 1527631329;

		private byte[] _innerBuffer;

		private int _innerBufferPos;

		private int _innerBufferLen;

		private Func<bool> _getNewBlock;

		private int _maxUncompressedBlockSize;

		public int Read(byte[] buffer, int offset, int count)
		{
			if (_innerBufferPos == _innerBufferLen) if (!_getNewBlock()) return 0;

			count = Math.Min(_innerBufferLen - _innerBufferPos, count);
			Buffer.BlockCopy(_innerBuffer, _innerBufferPos, buffer, offset, count);
			_innerBufferPos += count;
			return count;
		}

		public void ProcessBlock(byte[] inBuffer, int length, bool isCompressed)
		{
			_innerBufferPos = 0;
			if (!isCompressed)
			{
				Buffer.BlockCopy(inBuffer, 0, _innerBuffer, 0, length);
				_innerBufferLen = length;
			}
			else
			{
				_innerBufferLen = DecompressBlock(inBuffer, 0, length, _innerBuffer, 0, _maxUncompressedBlockSize);
			}
		}

		public void Init(int maxUncompressedBlockSize, Func<bool> getNextBlock)
		{
			_innerBuffer = new byte[maxUncompressedBlockSize];
			_maxUncompressedBlockSize = maxUncompressedBlockSize;
			_getNewBlock = getNextBlock;
		}

		public BlazerAlgorithm GetAlgorithmId()
		{
			return BlazerAlgorithm.Block;
		}

		public virtual int DecompressBlock(byte[] bufferIn, int bufferInOffset, int bufferInLength, byte[] bufferOut, int bufferOutOffset, int bufferOutLength)
		{
			var hashArr = new int[HASH_TABLE_LEN + 1];
			var idxIn = bufferInOffset;
			var idxOut = bufferOutOffset;
			uint mulEl = 0;

			while (idxIn < bufferInLength)
			{
				var elem = bufferIn[idxIn];

				// if (idxOut >= 389874 - 800 && idxOut <= 389874)
				//	Console.WriteLine(idxOut);

				var litCnt = (elem >> 4) & 7;
				var litCntOrig = litCnt;
				var seqCnt = (elem & 0xf) + MIN_SEQ_LEN;
				var backRef = 0;
				var hashIdx = -1;

				if (elem >= 128)
				{
					hashIdx = bufferIn[idxIn + 1] | (bufferIn[idxIn + 2] << 8);
					idxIn += 3;
					if (hashIdx == 0xffff)
					{
						seqCnt = 0;
						litCnt = elem - 128;
						litCntOrig = litCnt == 127 ? 7 : 0;
					}
				}
				else
				{
					backRef = bufferIn[idxIn + 1] + 1;
					idxIn += 2;
				}

				if (litCntOrig == 7)
				{
					var litCntR = bufferIn[idxIn++];
					if (litCntR < 253) litCnt += litCntR;
					else if (litCntR == 253)
						litCnt += 253 + bufferIn[idxIn++];
					else if (litCntR == 254)
						litCnt += 253 + 256 + bufferIn[idxIn++] + (bufferIn[idxIn++] << 8);
					else
						litCnt += 253 + (256 * 256) + bufferIn[idxIn++] + (bufferIn[idxIn++] << 8) + (bufferIn[idxIn++] << 16) + (bufferIn[idxIn++] << 24);
				}

				if (seqCnt == 15 + 4)
				{
					var seqCntR = bufferIn[idxIn++];
					if (seqCntR < 253) seqCnt += seqCntR;
					else if (seqCntR == 253)
						seqCnt += 253 + bufferIn[idxIn++];
					else if (seqCntR == 254)
						seqCnt += 253 + 256 + bufferIn[idxIn++] + (bufferIn[idxIn++] << 8);
					else
						seqCnt += 253 + (256 * 256) + bufferIn[idxIn++] + (bufferIn[idxIn++] << 8) + (bufferIn[idxIn++] << 16) + (bufferIn[idxIn++] << 24);
				}

				var maxOutLength = idxOut + litCnt + seqCnt;
				if (maxOutLength > bufferOutLength)
				{
					throw new InvalidOperationException("Very small inner buffer. Invalid configuration or stream.");
				}

				while (--litCnt >= 0)
				{
					var v = bufferIn[idxIn++];
					mulEl = (mulEl << 8) | v;
					var hashKey = (mulEl * Mul) >> (32 - HASH_TABLE_BITS);
					hashArr[hashKey] = idxOut;
					bufferOut[idxOut++] = v;
				}

				var inRepIdx = hashIdx >= 0 ? hashArr[hashIdx] - 3 : idxOut - backRef;

				while (--seqCnt >= 0)
				{
					var v = bufferOut[inRepIdx++];
					mulEl = (mulEl << 8) | v;

					hashArr[(mulEl * Mul) >> (32 - HASH_TABLE_BITS)] = idxOut;

					bufferOut[idxOut++] = v;
				}
			}

			return idxOut;
		}

		public virtual void Dispose()
		{
		}
	}
}
