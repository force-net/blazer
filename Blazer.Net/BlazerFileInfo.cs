using System;
using System.IO;

namespace Force.Blazer
{
	public class BlazerFileInfo
	{
		public string FileName { get; set; }

		public DateTime CreationTimeUtc { get; set; }

		public DateTime LastWriteTimeUtc { get; set; }

		public long Length { get; set; }

		public FileAttributes Attributes { get; set; }

		public static BlazerFileInfo FromFileInfo(FileInfo info, string relativeFileName = null)
		{
			var bfi = new BlazerFileInfo();
			bfi.FileName = relativeFileName ?? info.Name;
			bfi.Attributes = info.Attributes;
			bfi.CreationTimeUtc = info.CreationTimeUtc;
			bfi.LastWriteTimeUtc = info.LastWriteTimeUtc;
			bfi.Length = info.Length;
			return bfi;
		}

		public static BlazerFileInfo FromFileName(string fileName)
		{
			return FromFileInfo(new FileInfo(fileName), fileName);
		}

		public void ApplyToFile()
		{
			if (!File.Exists(FileName))
				return;
			File.SetAttributes(FileName, Attributes);
			File.SetCreationTimeUtc(FileName, CreationTimeUtc);
			File.SetLastWriteTimeUtc(FileName, CreationTimeUtc);
		}
	}
}
