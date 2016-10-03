using System;

namespace Force.Blazer
{
	/// <summary>
	/// Header flags for Blazer archive
	/// </summary>
	[Flags]
	public enum BlazerFlags : uint
	{
		/// <summary>
		/// No flags
		/// </summary>
		None = 0,
		
#pragma warning disable 1591
		InBlockSize512 = 0,
		InBlockSize1K = 1,
		InBlockSize2K = 2,
		InBlockSize4K = 3,
		InBlockSize8K = 4,
		InBlockSize16K = 5,
		InBlockSize32K = 6,
		InBlockSize64K = 7,
		InBlockSize128K = 8,
		InBlockSize256K = 9,
		InBlockSize512K = 10,
		InBlockSize1M = 11,
		InBlockSize2M = 12,
		InBlockSize4M = 13,
		InBlockSize8M = 14,
		InBlockSize16M = 15,
		
		IncludeCrc = 256,
		IncludeHeader = 512,
		IncludeFooter = 1024,
		RespectFlush = 2048,

		// in theory, we can encrypt outer and inner two times, so, let's keep both flags
		EncryptInner = 4096,
		EncryptOuter = 8192,
		NotImplementedAddRecoveryInfo = 16384,

		NoFileInfo = 0,
		OnlyOneFile = 32768,
		MultipleFiles = 65536,
		NotImplementedMultipleIndexedFiles = OnlyOneFile | MultipleFiles,
		IncludeComment = 131072,

		Default = IncludeCrc | IncludeHeader | IncludeFooter | RespectFlush,
		DefaultStream = Default | InBlockSize64K,
		DefaultBlock = Default | InBlockSize2M,

		// all known flags for this time
		AllKnownFlags = InBlockSize16M | IncludeCrc | IncludeHeader | IncludeFooter | RespectFlush | EncryptInner | EncryptOuter | OnlyOneFile | MultipleFiles | IncludeComment | 0xf0
#pragma warning restore 1591
	}
}
