using System.IO;

namespace Force.Blazer
{
	/// <summary>
	/// Compression stream with 'stream' algorithm realization. It is good for transferring messages by chunks, but can be used for any data.
	/// </summary>
	public class BlazerStreamCompressionStream : BlazerBaseCompressionStream
	{
		public BlazerStreamCompressionStream(Stream innerStream)
			: base(innerStream, BlazerAlgorithm.Stream, BlazerFlags.DefaultStream)
		{
		}

		public BlazerStreamCompressionStream(Stream innerStream, BlazerFlags flags = BlazerFlags.DefaultStream, string password = null)
			: base(innerStream, BlazerAlgorithm.Stream, flags, password)
		{
		}
	}
}
