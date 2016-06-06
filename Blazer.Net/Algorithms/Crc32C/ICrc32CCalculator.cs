namespace Force.Blazer.Algorithms.Crc32C
{
	public interface ICrc32CCalculator
	{
		uint Calculate(byte[] buffer, int offset, int count);
	}
}