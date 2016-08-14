namespace Force.Blazer.Algorithms.Crc32C
{
	/// <summary>
	/// Interface for Crc32 calculators
	/// </summary>
	public interface ICrc32CCalculator
	{
		/// <summary>
		/// Calculates Crc32C data for buffer
		/// </summary>
		uint Calculate(byte[] buffer, int offset, int count);
	}
}