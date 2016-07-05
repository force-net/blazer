namespace Force.Blazer.Algorithms.Crc32C
{
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

		uint ICrc32CCalculator.Calculate(byte[] buffer, int offset, int count)
		{
			uint crcLocal = uint.MaxValue;

			uint[] table = _table;
			while (count >= 16)
			{
				crcLocal = table[(15 * 256) + ((crcLocal ^ buffer[offset]) & 0xff)]
					^ table[(14 * 256) + (((crcLocal >> 8) ^ buffer[offset + 1]) & 0xff)]
					^ table[(13 * 256) + (((crcLocal >> 16) ^ buffer[offset + 2]) & 0xff)]
					^ table[(12 * 256) + (((crcLocal >> 24) ^ buffer[offset + 3]) & 0xff)]
					^ table[(11 * 256) + buffer[offset + 4]]
					^ table[(10 * 256) + buffer[offset + 5]]
					^ table[(9 * 256) + buffer[offset + 6]]
					^ table[(8 * 256) + buffer[offset + 7]]
					^ table[(7 * 256) + buffer[offset + 8]]
					^ table[(6 * 256) + buffer[offset + 9]]
					^ table[(5 * 256) + buffer[offset + 10]]
					^ table[(4 * 256) + buffer[offset + 11]]
					^ table[(3 * 256) + buffer[offset + 12]]
					^ table[(2 * 256) + buffer[offset + 13]]
					^ table[(1 * 256) + buffer[offset + 14]]
					^ table[(0 * 256) + buffer[offset + 15]];
				offset += 16;
				count -= 16;
			}

			while (--count >= 0)
				crcLocal = table[(crcLocal ^ buffer[offset++]) & 0xff] ^ crcLocal >> 8;
			return crcLocal ^ uint.MaxValue;
		}
	}
}