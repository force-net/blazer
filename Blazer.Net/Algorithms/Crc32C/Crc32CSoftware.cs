namespace Force.Blazer.Algorithms.Crc32C
{
	/// <summary>
	/// Software managed Crc32C calculator
	/// </summary>
	public class Crc32CSoftware : ICrc32CCalculator
	{
		private static readonly uint[] _table = new uint[16 * 256];

		static Crc32CSoftware()
		{
			const uint Poly = 0x82f63b78;
			for (uint i = 0; i < 256; i++)
			{
				uint res = i;
				for (int t = 0; t < 16; t++)
				{
					for (int k = 0; k < 8; k++) res = (res & 1) == 1 ? Poly ^ (res >> 1) : (res >> 1);
					_table[(t * 256) + i] = res;
				}
			}
		}

		uint ICrc32CCalculator.Calculate(uint crc, byte[] input, int offset, int length)
		{
			uint crcLocal = uint.MaxValue ^ crc;

			uint[] table = _table;
			while (length >= 16)
			{
				var a = table[(3 * 256) + input[offset + 12]]
					^ table[(2 * 256) + input[offset + 13]]
					^ table[(1 * 256) + input[offset + 14]]
					^ table[(0 * 256) + input[offset + 15]];

				var b = table[(7 * 256) + input[offset + 8]]
					^ table[(6 * 256) + input[offset + 9]]
					^ table[(5 * 256) + input[offset + 10]]
					^ table[(4 * 256) + input[offset + 11]];

				var c = table[(11 * 256) + input[offset + 4]]
					^ table[(10 * 256) + input[offset + 5]]
					^ table[(9 * 256) + input[offset + 6]]
					^ table[(8 * 256) + input[offset + 7]];

				var d = table[(15 * 256) + ((crcLocal ^ input[offset]) & 0xff)]
					^ table[(14 * 256) + (((crcLocal >> 8) ^ input[offset + 1]) & 0xff)]
					^ table[(13 * 256) + (((crcLocal >> 16) ^ input[offset + 2]) & 0xff)]
					^ table[(12 * 256) + (((crcLocal >> 24) ^ input[offset + 3]) & 0xff)];

				crcLocal = d ^ c ^ b ^ a;
				offset += 16;
				length -= 16;
			}

			while (--length >= 0)
				crcLocal = table[(crcLocal ^ input[offset++]) & 0xff] ^ crcLocal >> 8;

			return crcLocal ^ uint.MaxValue;

		}
	}
}