using System;
using System.IO;

namespace Force.Blazer
{
	/// <summary>
	/// Information about compressed file
	/// </summary>
	public class BlazerFileInfo
	{
		/// <summary>
		/// File name with path, or without it
		/// </summary>
		public string FileName { get; set; }

		/// <summary>
		/// File creation time in UTC
		/// </summary>
		public DateTime CreationTimeUtc { get; set; }

		/// <summary>
		/// Last write time in UTC
		/// </summary>
		public DateTime LastWriteTimeUtc { get; set; }

		/// <summary>
		/// File length
		/// </summary>
		/// <remarks>This length is just for information. Real compressed data can have another lengh</remarks>
		public long Length { get; set; }

		/// <summary>
		/// File attributes
		/// </summary>
		public FileAttributes Attributes { get; set; }

		/// <summary>
		/// Creates file info from <see cref="FileInfo"/> with optional custom relative name
		/// </summary>
		/// <remarks>If no relative name is passed, file name without path is used as file name. Otherwise, this relative name</remarks>
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

		/// <summary>
		/// Creates file info from file name
		/// </summary>
		public static BlazerFileInfo FromFileName(string fileName)
		{
			return FromFileInfo(new FileInfo(fileName), fileName);
		}

		/// <summary>
		/// Apply this file info to real file (set attributes and time)
		/// </summary>
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
