using System;
using System.IO;

namespace Force.Blazer.Helpers
{
	/// <summary>
	/// File Helper class
	/// </summary>
	public static class BlazerFileHelper
	{
		/// <summary>
		/// Helper method for convenient read multiple files from blazer archive and writing result to stream
		/// </summary>
		/// <typeparam name="T">Out streams type</typeparam>
		/// <param name="blazerStream">Input blazer stream</param>
		/// <param name="directoryCallback">Callback on new directory arrival</param>
		/// <param name="fileStartCallback">Callback on new file arrival, method should return stream for writing or null to skip it</param>
		/// <param name="fileEndCallback">Callback on file end (called only for files, not for directories)</param>
		public static void ReadFilesFromStream<T>(
			BlazerOutputStream blazerStream,
			Action<BlazerFileInfo> directoryCallback,
			Func<BlazerFileInfo, T> fileStartCallback,
			Action<BlazerFileInfo, T> fileEndCallback)
			where T : Stream
		{
			if (directoryCallback == null) directoryCallback = info => { };
			if (fileStartCallback == null) fileStartCallback = info => null;
			if (fileEndCallback == null) fileEndCallback = (info, arg2) => { };

			// we've missed original event, so, emulate it for client manually
			if (!blazerStream.HaveMultipleFiles)
			{
				if ((blazerStream.FileInfo.Attributes & FileAttributes.Directory) != 0) 
					directoryCallback(blazerStream.FileInfo);
				else
				{
					var s = fileStartCallback(blazerStream.FileInfo);
					if (s != null)
						blazerStream.CopyTo(s);
					fileEndCallback(blazerStream.FileInfo, s);
				}

				return;
			}

			T[] outFile = new T[1];
			BlazerFileInfo prevFile = null;

			blazerStream.FileInfoCallback = fInfo =>
			{
				if (prevFile != null)
				{
					if (fileEndCallback != null) fileEndCallback(prevFile, outFile[0]);
					prevFile = null;
				}

				prevFile = fInfo;
				if ((fInfo.Attributes & FileAttributes.Directory) != 0)
				{
					directoryCallback(fInfo);
				}
				else
				{
					outFile[0] = fileStartCallback(fInfo);
				}
			};

			var buf = new byte[blazerStream.MaxUncompressedBlockSize];
			while (true)
			{
				var cnt = blazerStream.Read(buf, 0, buf.Length);
				if (cnt == 0)
					break;
				if (outFile[0] != null)
					outFile[0].Write(buf, 0, cnt);
			}

			if (prevFile != null && ((prevFile.Attributes & FileAttributes.Directory) == 0) && outFile[0] != null)
			{
				fileEndCallback(prevFile, outFile[0]);
			}
		}
	}
}
