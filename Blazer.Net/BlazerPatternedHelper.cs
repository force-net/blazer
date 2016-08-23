using Force.Blazer.Algorithms.Patterned;

namespace Force.Blazer
{
	/// <summary>
	/// Helper for patterned compression
	/// </summary>
	public static class BlazerPatternedHelper
	{
		/// <summary>
		/// Creates Stream algorithm Patterned compressor. Good on compression, fast on decompression
		/// </summary>
		/// <remarks>Do not use pattern data more than 64Kb. It is useless for this type</remarks>
		public static IPatternedCompressor CreateStream()
		{
			return new StreamPatternedCompressor();
		}

		/// <summary>
		/// Creates Stream algorithm Patterned compressor with high encoding. Slow on compression, fast on decompression
		/// </summary>
		/// <remarks>Compression is very slow. Use only when needed</remarks>
		public static IPatternedCompressor CreateStreamHigh()
		{
			return new StreamHighPatternedCompressor();
		}

		/// <summary>
		/// Creates Block algorithm Patterned compressor. Good on compression, Good on decompression
		/// </summary>
		/// <remarks>Use this algorithm whan pattern is big (greater than 64Kb)</remarks>
		public static IPatternedCompressor CreateBlock()
		{
			return new BlockPatternedCompressor();
		}

		/// <summary>
		/// Creates Patterned compressor and init it with pattern. Algorithm is selected by pattern size
		/// </summary>
		public static IPatternedCompressor CreateFromPatternAuto(byte[] pattern)
		{
			return CreateFromPatternAuto(pattern, 0, pattern.Length);
		}

		/// <summary>
		/// Creates Patterned compressor and init it with pattern. Algorithm is selected by pattern size
		/// </summary>
		public static IPatternedCompressor CreateFromPatternAuto(byte[] pattern, int offset, int count)
		{
			IPatternedCompressor c;
			if (count < 65536) c = CreateStream();
			else c = CreateBlock();

			c.PreparePattern(pattern, offset, count);

			return c;
		}
	}
}
