using System;
using System.Diagnostics.CodeAnalysis;

namespace Force.Blazer.Algorithms
{
	public class StreamEncoder : IEncoder
	{
		private const int HASH_TABLE_BITS = 16;
		private const int HASH_TABLE_LEN = (1 << HASH_TABLE_BITS) - 1;
		public const int MAX_BACK_REF = (1 << 16) + 256;
		private const int MIN_SEQ_LEN = 4;
		
		// carefully selected random number
		private const uint MUL = 1527631329;

		private const int SIZE_SHIFT = 1000000000;

		[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1304:NonPrivateReadonlyFieldsMustBeginWithUpperCaseLetter", Justification = "Reviewed. Suppression is OK here.")]
		protected readonly int[] _hashArr = new int[HASH_TABLE_LEN + 1];

		[SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:FieldsMustBePrivate", Justification = "Reviewed. Suppression is OK here.")]
		protected byte[] _bufferIn;

		[SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:FieldsMustBePrivate", Justification = "Reviewed. Suppression is OK here.")]
		protected byte[] _bufferOut;

		private int _innerBufferSize;

		private int _maxInBlockSize;

		private int _bufferInPosFact;

		private int _bufferInLength;

		private int _shiftValue;

		private int _bufferOutIdx;

		private int _bufferOutHeaderSize;

		private Action<byte[], int, bool> _onBlockPrepared;

		public virtual void Init(int maxInBlockSize, int additionalHeaderSizeForOut, Action<byte[], int, bool> onBlockPrepared)
		{
			_maxInBlockSize = maxInBlockSize;
			_innerBufferSize = MAX_BACK_REF + maxInBlockSize;
			_bufferIn = new byte[_innerBufferSize + 1];
			_bufferOut = new byte[maxInBlockSize + (maxInBlockSize >> 8) + 3 + additionalHeaderSizeForOut];
			_bufferOutIdx = additionalHeaderSizeForOut;
			_bufferOutHeaderSize = additionalHeaderSizeForOut;
			_onBlockPrepared = onBlockPrepared;
		}

		public void Write(byte[] buffer, int offset, int count)
		{
			while (count > 0)
			{
				// shift
				if (_innerBufferSize - _bufferInPosFact < _maxInBlockSize)
				{
					var srcOffset = _bufferInPosFact - MAX_BACK_REF;
					Buffer.BlockCopy(_bufferIn, srcOffset, _bufferIn, 0, _bufferInLength + MAX_BACK_REF);
					_bufferInPosFact = MAX_BACK_REF;
					_shiftValue += srcOffset;
				}

				// copying minimal count to set MAX_BLOCK = MAX_IN_BLOCK_SIZE
				var toCopy = Math.Min(count, _maxInBlockSize - _bufferInLength);
				Buffer.BlockCopy(buffer, offset, _bufferIn, _bufferInLength + _bufferInPosFact, toCopy);
				_bufferInLength += toCopy;

				if (_bufferInLength >= _maxInBlockSize)
				{
					CompressAndWrite();
					if (_shiftValue >= 2 * SIZE_SHIFT)
					{
						ShiftHashtable();
					}
				}

				count -= toCopy;
				offset += toCopy;
			}
		}

		public void CompressAndWrite()
		{
			// nothing to do
			if (_bufferInLength == 0) return;

			var cnt = CompressBlock(_bufferIn, _bufferInPosFact, _bufferInLength + _bufferInPosFact, _shiftValue, _bufferOut, _bufferOutIdx, _hashArr);
			if (cnt - _bufferOutIdx >= _bufferInLength)
			{
				Buffer.BlockCopy(_bufferIn, _bufferInPosFact, _bufferOut, _bufferOutIdx, _bufferInLength);
				_bufferOutIdx += _bufferInLength;
				_onBlockPrepared(_bufferOut, _bufferOutIdx, false);
			}
			else
			{
				_bufferOutIdx = cnt;
				_onBlockPrepared(_bufferOut, _bufferOutIdx, true);
			}

			_bufferInPosFact += _bufferInLength;
			_bufferInLength = 0;
			_bufferOutIdx = _bufferOutHeaderSize;
		}

		public BlazerAlgorithm GetAlgorithmId()
		{
			return BlazerAlgorithm.Stream;
		}

		public virtual int CompressBlock(byte[] bufferIn, int bufferInOffset, int bufferInLength, int bufferInShift, byte[] bufferOut, int bufferOutOffset, int[] hashArr)
		{
			var idxOut = bufferOutOffset;
			int cntLit;

			uint mulEl = 0;

			var idxIn = bufferInOffset;
			var lastProcessedIdxIn = idxIn + 3;
			var globalOfs = bufferInShift;
			if (bufferInLength - idxIn > 3)
			{
				mulEl = (uint)(bufferIn[idxIn++] << 16 | bufferIn[idxIn++] << 8 | bufferIn[idxIn++]);
				// hashArr[(mulEl * MUL) >> (32 - HASH_TABLE_BITS)] = idxIn - 1 + globalOfs;
			}
			else
			{
				idxIn = bufferInLength;
			}

			while (idxIn < bufferInLength)
			{
				byte elemP0 = bufferIn[idxIn];

				mulEl = (mulEl << 8) | elemP0;
				var hashKey = (mulEl * MUL) >> (32 - HASH_TABLE_BITS);
				var hashVal = hashArr[hashKey] - globalOfs;
				hashArr[hashKey] = idxIn + globalOfs;
				var backRef = idxIn - hashVal;
				if (hashVal > 0
					&& backRef < MAX_BACK_REF
					&& ((/*idxIn != lastProcessedIdxIn*/ backRef < 257 || bufferIn[hashVal + 1] == bufferIn[idxIn + 1])
						&& mulEl == (uint)((bufferIn[hashVal - 3] << 24) | (bufferIn[hashVal - 2] << 16) | (bufferIn[hashVal - 1] << 8) | bufferIn[hashVal - 0])))
				{
					cntLit = idxIn - lastProcessedIdxIn;

					hashVal++;
					idxIn++;

					while (idxIn < bufferInLength)
					{
						elemP0 = bufferIn[idxIn];
						mulEl = (mulEl << 8) | elemP0;
						hashKey = (mulEl * MUL) >> (32 - HASH_TABLE_BITS);
						hashArr[hashKey] = idxIn + globalOfs;

						if (bufferIn[hashVal] == elemP0)
						{
							hashVal++;
							idxIn++;
						}
						else break;
					}

					int seqLen = idxIn - cntLit - lastProcessedIdxIn - MIN_SEQ_LEN + 3;

					#region Write Back Ref

					if (backRef >= 256 + 1)
					{
						backRef -= 256 + 1;
						bufferOut[idxOut++] = (byte)(((Math.Min(cntLit, 7) << 4) | Math.Min(seqLen, 15)) + 128);

						bufferOut[idxOut++] = (byte)backRef;
						bufferOut[idxOut++] = (byte)(backRef >> 8);
					}
					else
					{
						bufferOut[idxOut++] = (byte)((Math.Min(cntLit, 7) << 4) | Math.Min(seqLen, 15));

						// 1 is always min, should not write it
						bufferOut[idxOut++] = (byte)(backRef - 1);
					}

					#endregion

					#region Write Cnt

					if (cntLit >= 7)
					{
						var c = cntLit - 7;
						if (c < 253) bufferOut[idxOut++] = (byte)c;
						else if (c < 253 + 256)
						{
							bufferOut[idxOut++] = 253;
							bufferOut[idxOut++] = (byte)(c - 253);
						}
						else if (c < 253 + (256 * 256))
						{
							bufferOut[idxOut++] = 254;
							c -= 253 + 256;
							bufferOut[idxOut++] = (byte)c;
							bufferOut[idxOut++] = (byte)(c >> 8);
						}
						else
						{
							bufferOut[idxOut++] = 255;
							c -= 253 + (256 * 256);
							bufferOut[idxOut++] = (byte)c;
							bufferOut[idxOut++] = (byte)(c >> 8);
							bufferOut[idxOut++] = (byte)(c >> 16);
							bufferOut[idxOut++] = (byte)(c >> 24);
						}
					}

					if (seqLen >= 15)
					{
						var c = seqLen - 15;
						if (c < 253) bufferOut[idxOut++] = (byte)c;
						else if (c < 253 + 256)
						{
							bufferOut[idxOut++] = 253;
							bufferOut[idxOut++] = (byte)(c - 253);
						}
						else if (c < 253 + (256 * 256))
						{
							bufferOut[idxOut++] = 254;
							c -= 253 + 256;
							bufferOut[idxOut++] = (byte)c;
							bufferOut[idxOut++] = (byte)(c >> 8);
						}
						else
						{
							bufferOut[idxOut++] = 255;
							c -= 253 + (256 * 256);
							bufferOut[idxOut++] = (byte)c;
							bufferOut[idxOut++] = (byte)(c >> 8);
							bufferOut[idxOut++] = (byte)(c >> 16);
							bufferOut[idxOut++] = (byte)(c >> 24);
						}
					}

					#endregion

					#region Copy Lit

					if (cntLit >= 8)
					{
						Buffer.BlockCopy(bufferIn, lastProcessedIdxIn - 3, bufferOut, idxOut, cntLit);
						idxOut += cntLit;
					}
					else
					{
						var start = lastProcessedIdxIn - 3;
						while (--cntLit >= 0)
						{
							bufferOut[idxOut++] = bufferIn[start++];
						}
					}

					#endregion

					idxIn += 3;
					lastProcessedIdxIn = idxIn;

					#region Write Hash

					if (idxIn < bufferInLength)
					{
						mulEl = (mulEl << 8) | bufferIn[idxIn - 2];
						hashKey = (mulEl * MUL) >> (32 - HASH_TABLE_BITS);
						hashArr[hashKey] = idxIn - 2 + globalOfs;

						mulEl = (mulEl << 8) | bufferIn[idxIn - 1];
						hashKey = (mulEl * MUL) >> (32 - HASH_TABLE_BITS);
						hashArr[hashKey] = idxIn - 1 + globalOfs;
					}

					#endregion

					continue;
				}

				idxIn++;
			}

			cntLit = bufferInLength - lastProcessedIdxIn + 3;
			idxIn = bufferInLength;

			#region Write Final cntLit

			if (cntLit > 0)
			{
				bufferOut[idxOut++] = (byte)(Math.Min(127, cntLit) + 128);
				bufferOut[idxOut++] = 0xff;
				bufferOut[idxOut++] = 0xff;

				if (cntLit >= 127)
				{
					var c = cntLit - 127;
					if (c < 253) bufferOut[idxOut++] = (byte)c;
					else if (c < 253 + 256)
					{
						bufferOut[idxOut++] = 253;
						bufferOut[idxOut++] = (byte)(c - 253);
					}
					else if (c < 253 + (256 * 256))
					{
						bufferOut[idxOut++] = 254;
						c -= 253 + 256;
						bufferOut[idxOut++] = (byte)c;
						bufferOut[idxOut++] = (byte)(c >> 8);
					}
					else
					{
						bufferOut[idxOut++] = 255;
						c -= 253 + (256 * 256);
						bufferOut[idxOut++] = (byte)c;
						bufferOut[idxOut++] = (byte)(c >> 8);
						bufferOut[idxOut++] = (byte)(c >> 16);
						bufferOut[idxOut++] = (byte)(c >> 24);
					}
				}

				while (cntLit > 0)
				{
					bufferOut[idxOut++] = bufferIn[idxIn - cntLit];
					cntLit--;
				}
			}

			#endregion

			return idxOut;
		}

		public void ShiftHashtable()
		{
			_shiftValue -= SIZE_SHIFT;
			for (var i = 0; i < HASH_TABLE_LEN; i++)
				_hashArr[i] = Math.Min(0, _hashArr[i] - SIZE_SHIFT);
		}

		public virtual void Dispose()
		{
		}
	}
}
