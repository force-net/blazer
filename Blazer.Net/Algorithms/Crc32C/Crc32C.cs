using Force.Blazer.Native;

namespace Force.Blazer.Algorithms.Crc32C
{
	/// <summary>
	/// Crc32C (Castagnoli) checksum implementation
	/// </summary>
	public static class Crc32C
	{
		private static readonly ICrc32CCalculator _calculator;

		static Crc32C()
		{
			_calculator = NativeHelper.IsNativeAvailable ? (ICrc32CCalculator)new Crc32CHardware() : new Crc32CSoftware();
		}

		/// <summary>
		/// Calculates Crc32C data of given buffer
		/// </summary>
		public static uint Calculate(byte[] buffer, int offset, int count)
		{
			return Calculate(0, buffer, offset, count);
		}

		/// <summary>
		/// Calculates Crc32C data of given buffer, updates existing crc
		/// </summary>
		public static uint Calculate(uint currentCrc, byte[] buffer)
		{
			return Calculate(currentCrc, buffer, 0, buffer.Length);
		}

		/// <summary>
		/// Calculates Crc32C data of given buffer, updates existing crc
		/// </summary>
		public static uint Calculate(uint currentCrc, byte[] buffer, int offset, int count)
		{
			return _calculator.Calculate(currentCrc, buffer, offset, count);
		}

		/// <summary>
		/// Calculates Crc32C data of given buffer
		/// </summary>
		public static uint Calculate(byte[] buffer)
		{
			return Calculate(0, buffer, 0, buffer.Length);
		}
	}
}
