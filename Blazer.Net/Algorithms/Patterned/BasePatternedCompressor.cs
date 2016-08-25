using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Force.Blazer.Algorithms.Patterned
{
	/// <summary>
	/// Base implementation of Patterned Compressor/Decompressor
	/// </summary>
	public abstract class BasePatternedCompressor : IPatternedCompressor
	{
		/// <summary>
		/// Calculates max compressed buffer size for specified uncompressed data length
		/// </summary>
		public abstract int CalculateMaxCompressedBufferLength(int uncompressedLength);

		/// <summary>
		/// Initializes internal hash array
		/// </summary>
		protected abstract void InitHashArray();

		/// <summary>
		/// Restores internal hash array
		/// </summary>
		protected abstract void RestoreHashArray();

		/// <summary>
		/// Returns algorithm id
		/// </summary>
		protected abstract byte GetAlgorithmId();

		/// <summary>
		/// Inner buffer
		/// </summary>
		[SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:FieldsMustBePrivate", Justification = "Reviewed. Suppression is OK here.")]
		protected byte[] _innerBuffer;

		/// <summary>
		/// Length of pattern
		/// </summary>
		[SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:FieldsMustBePrivate", Justification = "Reviewed. Suppression is OK here.")]
		protected int _patternLength;

		/// <summary>
		/// Compress block of data
		/// </summary>
		protected abstract int CompressBlock(int countIn, byte[] bufferOut, int offsetOut);

		/// <summary>
		/// Prepares pattern. Should be called only once for one pattern
		/// </summary>
		public void PreparePattern(byte[] pattern, int offset, int count)
		{
			if (count <= 0)
				throw new InvalidOperationException("Invalid pattern length");
			var patternLength = count;
			_innerBuffer = new byte[patternLength * 2];
			var outerBuffer = new byte[CalculateMaxCompressedBufferLength(patternLength) + 1];

			Buffer.BlockCopy(pattern, offset, _innerBuffer, 0, patternLength);
			CompressBlock(patternLength, outerBuffer, 0);
			_patternLength = patternLength;
			InitHashArray();
		}

		/// <summary>
		/// Prepares pattern. Should be called only once for one pattern
		/// </summary>
		public void PreparePattern(byte[] pattern)
		{
			PreparePattern(pattern, 0, pattern.Length);
		}

		private int _isWorking;

		/// <summary>
		/// Encodes data with prepared pattern
		/// </summary>
		public int EncodeWithPattern(byte[] bufferIn, int offsetIn, int countIn, byte[] bufferOut, int offsetOut)
		{
			if (Interlocked.CompareExchange(ref _isWorking, 1, 0) != 0)
				throw new InvalidOperationException("Method is not thread safe ");

			try
			{
				var totalInLength = countIn + _patternLength;
				if (totalInLength > _innerBuffer.Length)
				{
					var oldInnerBuffer = _innerBuffer;
					_innerBuffer = new byte[totalInLength + 128];
					Buffer.BlockCopy(oldInnerBuffer, 0, _innerBuffer, 0, _patternLength);
				}

				Buffer.BlockCopy(bufferIn, offsetIn, _innerBuffer, _patternLength, countIn);

				if (bufferOut.Length - offsetOut < CalculateMaxCompressedBufferLength(countIn))
					throw new InvalidOperationException("Out buffer too small");

				var len = CompressBlock(countIn, bufferOut, offsetOut);
				var writtenCnt = len - offsetOut - 1;
				writtenCnt >>= 16;
				var lenCnt = 0;
				while (writtenCnt > 0)
				{
					lenCnt++;
					writtenCnt >>= 1;
				}

				// writing some metainfo to out buffer, to allow decode and validate data
				bufferOut[offsetOut] = (byte)(lenCnt | (GetAlgorithmId() << 4));

				RestoreHashArray();
				return len - offsetOut;
			}
			finally
			{
				Interlocked.Exchange(ref _isWorking, 0);
			}
		}

		/// <summary>
		/// Decompress block of data
		/// </summary>
		protected abstract int DecompressBlock(byte[] bufferIn, int offsetIn, int countIn);

		/// <summary>
		/// Decodes data with prepared pattern
		/// </summary>
		public int DecodeWithPattern(byte[] bufferIn, int offsetIn, int countIn, byte[] bufferOut, int offsetOut)
		{
			if (Interlocked.CompareExchange(ref _isWorking, 1, 0) != 0)
				throw new InvalidOperationException("Method is not thread safe ");

			try
			{
				var algFlag = bufferIn[offsetIn];
				var algorithm = algFlag >> 4;
				if (algorithm != GetAlgorithmId())
					throw new InvalidOperationException("Encoded data is not patterned data");
				var maxOutLength = ((algFlag + 1) & 0xf) << 16;

				var totalOutLength = _patternLength + maxOutLength;
				if (totalOutLength > _innerBuffer.Length)
				{
					var oldInnerBuffer = _innerBuffer;
					_innerBuffer = new byte[totalOutLength + 128];
					Buffer.BlockCopy(oldInnerBuffer, 0, _innerBuffer, 0, _patternLength);
				}

				var res = DecompressBlock(bufferIn, offsetIn, countIn);
				if (offsetOut + res - _patternLength > bufferOut.Length)
					throw new InvalidOperationException("Out buffer too small");
				Buffer.BlockCopy(_innerBuffer, _patternLength, bufferOut, offsetOut, res - _patternLength);
				return res - _patternLength;
			}
			finally
			{
				Interlocked.Exchange(ref _isWorking, 0);
			}
		}

		/// <summary>
		/// Encodes data with prepared pattern
		/// </summary>
		public byte[] EncodeWithPattern(byte[] buffer, int offset, int count)
		{
			var bufferOut = new byte[CalculateMaxCompressedBufferLength(count)];
			var cnt = EncodeWithPattern(buffer, offset, count, bufferOut, 0);
			Array.Resize(ref bufferOut, cnt);
			return bufferOut;
		}

		/// <summary>
		/// Encodes data with prepared pattern
		/// </summary>
		public byte[] EncodeWithPattern(byte[] buffer)
		{
			return EncodeWithPattern(buffer, 0, buffer.Length);
		}

		/// <summary>
		/// Decodes data with prepared pattern
		/// </summary>
		public byte[] DecodeWithPattern(byte[] buffer, int offset, int count)
		{
			var algFlag = buffer[offset];
			var maxOutLength = ((algFlag + 1) & 0xf) << 16;

			var bufferOut = new byte[maxOutLength];
			var cnt = DecodeWithPattern(buffer, offset, count, bufferOut, 0);
			Array.Resize(ref bufferOut, cnt);
			return bufferOut;
		}

		/// <summary>
		/// Decodes data with prepared pattern
		/// </summary>
		public byte[] DecodeWithPattern(byte[] buffer)
		{
			return DecodeWithPattern(buffer, 0, buffer.Length);
		}
	}
}
