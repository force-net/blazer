using System;
using System.IO;
using System.Text;

namespace Force.Blazer.Helpers
{
	internal static class FileHeaderHelper
	{
		private static readonly DateTime MinFileTime = new DateTime(1601, 1, 1);

		public static byte[] GenerateFileHeader(BlazerFileInfo info)
		{
			if (info.FileName == null) info.FileName = string.Empty;
			if (info.CreationTimeUtc < MinFileTime)
				info.CreationTimeUtc = MinFileTime;
			if (info.LastWriteTimeUtc < MinFileTime)
				info.LastWriteTimeUtc = MinFileTime;

			var fileNameBytes = Encoding.UTF8.GetBytes(info.FileName);
			var totalHeader = new byte[8 + // length
					8 + // creation time
					8 + // last write time
					2 + // attributes
					fileNameBytes.Length];

			Buffer.BlockCopy(BitConverter.GetBytes(info.Length), 0, totalHeader, 0, 8);
			Buffer.BlockCopy(BitConverter.GetBytes(info.CreationTimeUtc.ToFileTimeUtc()), 0, totalHeader, 0 + 8, 8);
			Buffer.BlockCopy(BitConverter.GetBytes(info.LastWriteTimeUtc.ToFileTimeUtc()), 0, totalHeader, 0 + 8 + 8, 8);
			totalHeader[0 + 8 + 8 + 8] = (byte)info.Attributes;
			totalHeader[0 + 8 + 8 + 8 + 1] = (byte)((int)info.Attributes >> 8);
			Buffer.BlockCopy(fileNameBytes, 0, totalHeader, 0 + 8 + 8 + 8 + 2, fileNameBytes.Length);
			return totalHeader;
		}

		public static BlazerFileInfo ParseFileHeader(byte[] header, int offset, int count)
		{
			const int NonNameSize = 8 + 8 + 8 + 2;
			if (count < NonNameSize)
				throw new InvalidOperationException("Invalid file header");
			var fileInfo = new BlazerFileInfo();
			fileInfo.Length = BitConverter.ToInt64(header, offset);
			fileInfo.CreationTimeUtc = DateTime.FromFileTimeUtc(BitConverter.ToInt64(header, 8 + offset));
			fileInfo.LastWriteTimeUtc = DateTime.FromFileTimeUtc(BitConverter.ToInt64(header, 8 + 8 + offset));
			fileInfo.Attributes = (FileAttributes)BitConverter.ToInt16(header, 8 + 8 + 8 + offset);
			fileInfo.FileName = Encoding.UTF8.GetString(header, NonNameSize + offset, count - NonNameSize);
			return fileInfo;
		}
	}
}
