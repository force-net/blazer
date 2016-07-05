using System;
using System.IO;
using System.Text;

namespace Force.Blazer.Helpers
{
	internal static class FileHeaderHelper
	{
		public static byte[] GenerateFileHeader(BlazerFileInfo info, int headerSize)
		{
			if (info.FileName == null) info.FileName = string.Empty;
			var fileNameBytes = Encoding.UTF8.GetBytes(info.FileName);
			var totalHeader = new byte[headerSize + // base header
					8 + // length
					8 + // creation time
					8 + // last write time
					2 + // attributes
					fileNameBytes.Length];

			Buffer.BlockCopy(BitConverter.GetBytes(info.Length), 0, totalHeader, headerSize, 8);
			Buffer.BlockCopy(BitConverter.GetBytes(info.CreationTimeUtc.ToFileTimeUtc()), 0, totalHeader, headerSize + 8, 8);
			Buffer.BlockCopy(BitConverter.GetBytes(info.LastWriteTimeUtc.ToFileTimeUtc()), 0, totalHeader, headerSize + 8 + 8, 8);
			totalHeader[headerSize + 8 + 8 + 8] = (byte)info.Attributes;
			totalHeader[headerSize + 8 + 8 + 8 + 1] = (byte)((int)info.Attributes >> 8);
			Buffer.BlockCopy(fileNameBytes, 0, totalHeader, headerSize + 8 + 8 + 8 + 2, fileNameBytes.Length);
			return totalHeader;
		}

		public static BlazerFileInfo ParseFileHeader(byte[] header)
		{
			const int NonNameSize = 8 + 8 + 8 + 2;
			if (header.Length < NonNameSize)
				throw new InvalidOperationException("Invalid file header");
			var fileInfo = new BlazerFileInfo();
			fileInfo.Length = BitConverter.ToInt64(header, 0);
			fileInfo.CreationTimeUtc = DateTime.FromFileTimeUtc(BitConverter.ToInt64(header, 8));
			fileInfo.LastWriteTimeUtc = DateTime.FromFileTimeUtc(BitConverter.ToInt64(header, 8 + 8));
			fileInfo.Attributes = (FileAttributes)BitConverter.ToInt16(header, 8 + 8 + 8);
			fileInfo.FileName = Encoding.UTF8.GetString(header, NonNameSize, header.Length - NonNameSize);
			return fileInfo;
		}
	}
}
