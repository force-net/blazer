namespace Force.Blazer.Algorithms.Crc32C
{
	public class Crc32CSoftware : ICrc32CCalculator
	{
		private static readonly uint[][] _table;

		static Crc32CSoftware()
		{
			const uint Poly = 0x82f63b78;
			_table = new uint[16][];
			for (var i = 0; i < 16; i++) _table[i] = new uint[256];

			for (int i = 0; i < 256; i++) 
			{
				uint res = (uint)i;
				for (int t = 0; t < 16; t++) 
				{
					for (int k = 0; k < 8; k++) res = (res & 1) == 1 ? Poly ^ (res >> 1) : (res >> 1);
						_table[t][i] = res;
				}
			}
		}

		uint ICrc32CCalculator.Calculate(byte[] buffer, int offset, int count)
		{
			ulong crc = 4294967295U;

			while (count >= 16)
			{
				crc = _table[15][(crc ^ buffer[offset]) & 0xff]
					^ _table[14][((crc >> 8) ^ buffer[offset + 1]) & 0xff]
					^ _table[13][((crc >> 16) ^ buffer[offset + 2]) & 0xff]
					^ _table[12][((crc >> 24) ^ buffer[offset + 3]) & 0xff]
					^ _table[11][((crc >> 32) ^ buffer[offset + 4]) & 0xff]
					^ _table[10][((crc >> 40) ^ buffer[offset + 5]) & 0xff]
					^ _table[9][((crc >> 48) ^ buffer[offset + 6]) & 0xff]
					^ _table[8][((crc >> 56) ^ buffer[offset + 7]) & 0xff]
					^ _table[7][buffer[offset + 8]]
					^ _table[6][buffer[offset + 9]]
					^ _table[5][buffer[offset + 10]]
					^ _table[4][buffer[offset + 11]]
					^ _table[3][buffer[offset + 12]]
					^ _table[2][buffer[offset + 13]]
					^ _table[1][buffer[offset + 14]]
					^ _table[0][buffer[offset + 15]];
				offset += 16;
				count -= 16;
			}

			while (--count >= 0)
				crc = _table[0][(crc ^ buffer[offset++]) & 0xff] ^ crc >> 8;
			return (uint)(crc ^ uint.MaxValue);
		}
	}
}