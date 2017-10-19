using System;

namespace Force.Blazer.Algorithms
{
	/// <summary>
	/// Slow, but with higher compression reate version of <see cref="StreamEncoder"/> of Blazer algorithm. Fully compatible with it.
	/// </summary>
	/// <remarks>Incompleted. Can be improved in future. But stable</remarks>
	public class StreamEncoderHigh : StreamEncoder
	{
		private const int HASH_TABLE_BITS = 16;

		/// <summary>
		/// Length of Hash Array - 1
		/// </summary>
		protected const int HASH_TABLE_LEN = (1 << HASH_TABLE_BITS) - 1;

		/// <summary>
		/// Count of internal hash arrays
		/// </summary>
		public const int HASHARR_CNT = 32;

		private const int MIN_SEQ_LEN = 4;

		// carefully selected random number
		private const uint MUL = 1527631329;

		private int[][] _hashArr2;

		private int[] _hashArrPos;

		/// <summary>
		/// Returns internal hash array
		/// </summary>
		public int[][] HashArr2
		{
			get
			{
				return _hashArr2;
			}
		}

		/// <summary>
		/// Returns position in internal hash array
		/// </summary>
		public int[] HashArrPos
		{
			get
			{
				return _hashArrPos;
			}
		}

		/// <summary>
		/// Initializes encoder with information about maximum uncompressed block size
		/// </summary>
		public override void Init(int maxInBlockSize)
		{
			base.Init(maxInBlockSize);
			_hashArr2 = new int[HASHARR_CNT][];
			for (var i = 0; i < HASHARR_CNT; i++)
				_hashArr2[i] = new int[HASH_TABLE_LEN + 1];
			_hashArrPos = new int[HASH_TABLE_LEN + 1];
		}

		/// <summary>
		/// Shifts hashtable data
		/// </summary>
		/// <remarks>Use this method to periodically shift positions in array. It is required for streams longer than 2Gb</remarks>
		protected override void ShiftHashtable()
		{
			for (var i = 0; i < HASHARR_CNT; i++)
				for (var k = 0; k < HASH_TABLE_LEN + 1; k++)
					_hashArr2[i][k] = Math.Max(0, _hashArr2[i][k] - SIZE_SHIFT);

			for (var k = 0; k < HASH_TABLE_LEN + 1; k++) _hashArrPos[k] = _hashArrPos[k] & 0xffff;
		}

		/// <summary>
		/// Compresses block of data. See <see cref="StreamEncoder.CompressBlockExternal"/> for details
		/// </summary>
		public override int CompressBlock(
			byte[] bufferIn,
			int bufferInOffset,
			int bufferInLength,
			int bufferInShift,
			byte[] bufferOut,
			int bufferOutOffset)
		{
			return CompressBlockHighExternal(bufferIn, bufferInOffset, bufferInLength, bufferInShift, bufferOut, bufferOutOffset, _hashArr2, _hashArrPos);
		}

		private static int FindMaxSequence(byte[] bufferIn, int iterMax, int a, int b, int minValToCompare)
		{
			if (a + minValToCompare >= iterMax) return -1;
			if (bufferIn[a + minValToCompare] != bufferIn[b + minValToCompare]) return -1;
			var origA = a;
			while (a < iterMax && bufferIn[a] == bufferIn[b])
			{
				a++;
				b++;
			}

			return a - origA;
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
		/// <param name="hashArr">Hash arrays with data. Should be same for consecutive blocks of data</param>
		/// /// <param name="hashArrPos">Position in hash arrays. Should be same for consecutive blocks of data</param>
		/// <returns>Bytes count of compressed data</returns>
		public static int CompressBlockHighExternal(byte[] bufferIn, int bufferInOffset, int bufferInLength, int bufferInShift, byte[] bufferOut, int bufferOutOffset, int[][] hashArr, int[] hashArrPos)
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
			var cntToCheck = -1;

			while (idxIn < iterMax)
			{
				byte elemP0 = bufferIn[idxIn];

				mulEl = (mulEl << 8) | elemP0;
				var hashKey = (mulEl * MUL) >> (32 - HASH_TABLE_BITS);
				int hashVal = 0;

				var hashArrPo = cntToCheck >= 0 ? cntToCheck : hashArrPos[hashKey];
				var min = Math.Max(0, hashArrPo - HASHARR_CNT);
				var cnt = 0;
				var checkCnt = 3;
				for (var i = hashArrPo - 1; i >= min; i--)
				{
					var hashValLocal = hashArr[i & (HASHARR_CNT - 1)][hashKey] - globalOfs;
					int backRefLocal = idxIn - hashValLocal;
					if (backRefLocal < MAX_BACK_REF)
					{
						var checkCntLocal = FindMaxSequence(bufferIn, bufferInLength, idxIn - 3, hashValLocal - 3, checkCnt);
						var cntLocal = checkCntLocal + (backRefLocal < 257 ? 1 : 0);
						if (cntLocal > cnt)
						{
							cnt = cntLocal;
							checkCnt = checkCntLocal;
							hashVal = hashValLocal;
						}
					}
					else
						break;
				}

				if (cnt >= 4)
				{
					var hashKeyNext = ((mulEl << 8 | bufferIn[idxIn + 1]) * MUL) >> (32 - HASH_TABLE_BITS);
					var minNext = Math.Max(0, hashArrPos[hashKeyNext] - HASHARR_CNT);
					for (var i = hashArrPos[hashKeyNext] - 1; i >= minNext; i--)
					{
						var hashValLocal = hashArr[i & (HASHARR_CNT - 1)][hashKeyNext] - globalOfs;
						int backRefLocal = idxIn - hashValLocal;
						if (backRefLocal < MAX_BACK_REF)
						{
							var cntLocal = FindMaxSequence(bufferIn, bufferInLength, idxIn + 1 - 3, hashValLocal - 3, cnt - 1) + (backRefLocal < 257 ? 1 : 0);
							if (cntLocal > cnt)
							{
								checkCnt = 0;
								cntToCheck = hashArrPos[hashKeyNext];
								break;
							}
						}
						else break;
					}
				}

				// var hashVal = hashArr[hashArrPos[hashKey] & (HASHARR_CNT - 1)][hashKey] - globalOfs;
				hashArr[(hashArrPos[hashKey]++) & (HASHARR_CNT - 1)][hashKey] = idxIn + globalOfs;
				// var isBig = backRef < 257 ? 0 : 1;
				if (checkCnt >= 4)
				{
					cntToCheck = -1;
					var backRef = idxIn - hashVal;
					cntLit = idxIn - lastProcessedIdxIn;

					checkCnt -= 3;
					if (checkCnt + idxIn > iterMax)
					{
						// last block no requirement to update hashes
						idxIn += checkCnt;
					}
					else
					{
						while (checkCnt-- > 0)
						{
							// hashVal++;
							idxIn++;

							elemP0 = bufferIn[idxIn];
							mulEl = (mulEl << 8) | elemP0;
							hashKey = (mulEl * MUL) >> (32 - HASH_TABLE_BITS);
							hashArr[(hashArrPos[hashKey]++) & (HASHARR_CNT - 1)][hashKey] = idxIn + globalOfs;
						}
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
						hashArr[(hashArrPos[hashKey]++) & (HASHARR_CNT - 1)][hashKey] = idxIn - 2 + globalOfs;

						mulEl = (mulEl << 8) | bufferIn[idxIn - 1];
						hashKey = (mulEl * MUL) >> (32 - HASH_TABLE_BITS);
						hashArr[(hashArrPos[hashKey]++) & (HASHARR_CNT - 1)][hashKey] = idxIn - 1 + globalOfs;
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
	}
}
