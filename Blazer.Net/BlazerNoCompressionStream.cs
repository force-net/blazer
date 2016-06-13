using System.IO;

namespace Force.Blazer
{
	/// <summary>
	/// Compressions stream which not compress data. Can be used as a stub or for integrity checks of data (Crc32C is included)
	/// </summary>
	public class BlazerNoCompressionStream : BlazerBaseCompressionStream
	{
		public BlazerNoCompressionStream(Stream innerStream)
			: base(innerStream, BlazerAlgorithm.NoCompress, BlazerFlags.DefaultStream)
		{
		}

		public BlazerNoCompressionStream(Stream innerStream, BlazerFlags flags = BlazerFlags.DefaultStream, string password = null)
			: base(innerStream, BlazerAlgorithm.NoCompress, flags, password)
		{
		}
	}
}
