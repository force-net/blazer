using System;
using System.Diagnostics.CodeAnalysis;

namespace Force.Blazer.Algorithms
{
	/// <summary>
	/// Encoder of Stream version of Blazer algorithm
	/// </summary>
	/// <remarks>Stream version is good for 'live' streamss, slightly slower than Block, but support stream flushing without
	/// losing compression rate and has very fast decoder</remarks>
	public class StreamEncoder : IEncoder
	{
		private const int HASH_TABLE_BITS = 16;
		private const int HASH_TABLE_LEN = (1 << HASH_TABLE_BITS) - 1;
		internal const int MAX_BACK_REF = (1 << 16) + 256;
		private const int MIN_SEQ_LEN = 4;
		
		// carefully selected random number
		private const uint MUL = 1527631329;

		/// <summary>
		/// Size to shift big data
		/// </summary>
		protected const int SIZE_SHIFT = 1000000000;

		/// <summary>
		/// Hash array to store dictionary between iterations
		/// </summary>
		[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1304:NonPrivateReadonlyFieldsMustBeginWithUpperCaseLetter", Justification = "Reviewed. Suppression is OK here.")]
		protected readonly int[] _hashArr = new int[HASH_TABLE_LEN + 1];

		/// <summary>
		/// Buffer to store inbound data between iterations
		/// </summary>
		[SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:FieldsMustBePrivate", Justification = "Reviewed. Suppression is OK here.")]
		protected byte[] _bufferIn;

		/// <summary>
		/// Buffer to temporary store compressed data
		/// </summary>
		[SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:FieldsMustBePrivate", Justification = "Reviewed. Suppression is OK here.")]
		protected byte[] _bufferOut;

		private int _innerBufferSize;

		private int _maxInBlockSize;

		private int _bufferInPosFact;

		private int _bufferInLength;

		private int _shiftValue;

		private int _bufferOutIdx;

		/// <summary>
		/// Returns internal hash array
		/// </summary>
		public int[] HashArr
		{
			get
			{
				return _hashArr;
			}
		}

		/// <summary>
		/// Returns additional size for inner buffers. Can be used to store some data or for optimiations
		/// </summary>
		/// <returns>Size in bytes</returns>
		public virtual int GetAdditionalInSize()
		{
			return 0;
		}

		/// <summary>
		/// Initializes encoder with information about maximum uncompressed block size
		/// </summary>
		public virtual void Init(int maxInBlockSize)
		{
			_maxInBlockSize = maxInBlockSize;
			_innerBufferSize = MAX_BACK_REF + maxInBlockSize;
			_bufferIn = new byte[_innerBufferSize + 1 + GetAdditionalInSize()];
			_bufferOut = new byte[maxInBlockSize + (maxInBlockSize >> 8) + 3 + GetAdditionalInSize()];
			_bufferOutIdx = 0;
		}

		/// <summary>
		/// Encodes given buffer
		/// </summary>
		public BufferInfo Encode(byte[] buffer, int offset, int length)
		{
			var count = length - offset;
			
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

			var cnt = CompressBlock(
					_bufferIn, _bufferInPosFact, _bufferInLength + _bufferInPosFact, _shiftValue, _bufferOut, _bufferOutIdx);
			_bufferInPosFact += _bufferInLength;
			_bufferInLength = 0;
			_bufferOutIdx = 0;

			if (_shiftValue >= 2 * SIZE_SHIFT)
			{
				ShiftHashtable();
				_shiftValue -= SIZE_SHIFT;
			}

			return new BufferInfo(_bufferOut, _bufferOutIdx, cnt);
		}

		/// <summary>
		/// Returns algorithm id
		/// </summary>
		public BlazerAlgorithm GetAlgorithmId()
		{
			return BlazerAlgorithm.Stream;
		}

		/// <summary>
		/// Compresses block of data. See <see cref="CompressBlockExternal"/> for details
		/// </summary>
		public virtual int CompressBlock(
			byte[] bufferIn,
			int bufferInOffset,
			int bufferInLength,
			int bufferInShift,
			byte[] bufferOut,
			int bufferOutOffset)
		{
			return CompressBlockExternal(bufferIn, bufferInOffset, bufferInLength, bufferInShift, bufferOut, bufferOutOffset, _hashArr);
		}

		/// <summary>
		/// Compresses independent block of data
		/// </summary>
		/// <param name="bufferIn">In buffer</param>
		/// <returns>Compressed array</returns>
		public static byte[] CompressData(byte[] bufferIn)
		{
			var hashArr = new int[HASH_TABLE_LEN + 1];
			var outBuffer = new byte[bufferIn.Length + (bufferIn.Length >> 8) + 3];
			var cnt = CompressBlockExternal(bufferIn, 0, bufferIn.Length, 0, outBuffer, 0, hashArr);
			Array.Resize(ref outBuffer, cnt);
			return outBuffer;
		}

		/// <summary>
		/// Compresses block of data, can be used independently for byte arrays
		/// </summary>
		/// <param name="bufferIn">In buffer</param>
		/// <param name="bufferInOffset">In buffer offset</param>
		/// <param name="bufferInLength">In buffer right offset (offset + count)</param>
		/// <param name="bufferInShift">Additional relative offset for data in hash array</param>
		/// <param name="bufferOut">Out buffer, should be enough size</param>
		/// <param name="bufferOutOffset">Out buffer offset</param>
		/// <param name="hashArr">Hash array with data. Should be same for consecutive blocks of data</param>
		/// <returns>Bytes count of compressed data</returns>
		public static int CompressBlockExternal(byte[] bufferIn, int bufferInOffset, int bufferInLength, int bufferInShift, byte[] bufferOut, int bufferOutOffset, int[] hashArr)
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
			}
			else
			{
				idxIn = bufferInLength;
			}

			var iterMax = bufferInLength - 1;

			while (idxIn < iterMax)
			{
				byte elemP0 = bufferIn[idxIn];

				mulEl = (mulEl << 8) | elemP0;
				var hashKey = (mulEl * MUL) >> (32 - HASH_TABLE_BITS);
				var hashVal = hashArr[hashKey] - globalOfs;
				hashArr[hashKey] = idxIn + globalOfs;
				var backRef = idxIn - hashVal;
				var isBig = backRef < 257 ? 0 : 1;
				if (hashVal > 0
					&& backRef < MAX_BACK_REF
					&& ((isBig == 0 || bufferIn[hashVal + 1] == bufferIn[idxIn + 1])
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

					int seqLen = idxIn - cntLit - lastProcessedIdxIn - MIN_SEQ_LEN + 3/* - isBig*/;

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

		/// <summary>
		/// Shifts hashtable data
		/// </summary>
		/// <remarks>Use this method to periodically shift positions in array. It is required for streams longer than 2Gb</remarks>
		protected virtual void ShiftHashtable()
		{
			for (var i = 0; i < HASH_TABLE_LEN + 1; i++)
				_hashArr[i] = Math.Max(0, _hashArr[i] - SIZE_SHIFT);
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		/// <filterpriority>2</filterpriority>
		public virtual void Dispose()
		{
		}
	}
}
