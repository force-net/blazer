using System;

namespace Force.Blazer
{
	[Flags]
	public enum BlazerFlags : uint
	{
		None = 0,
		
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
		NotImplementedMultipleFiles = 65536,

		Default = IncludeCrc | IncludeHeader | IncludeFooter | RespectFlush,
		DefaultStream = Default | InBlockSize64K,
		DefaultBlock = Default | InBlockSize2M
	}
}
