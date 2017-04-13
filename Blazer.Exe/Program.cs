using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

using Force.Blazer.Algorithms;
using Force.Blazer.Exe.CommandLine;

namespace Force.Blazer.Exe
{
	public class Program
	{
		static Program()
		{
#if !DEBUG
			AppDomain.CurrentDomain.AssemblyResolve += CurrentDomainOnAssemblyResolve;
#endif
		}

		private static string GetBlazerLibraryVersion()
		{
			return "library: "
					+ ((AssemblyFileVersionAttribute)
						typeof(BlazerInputStream).Assembly.GetCustomAttributes(typeof(AssemblyFileVersionAttribute), true).First()).Version;
		}

		public static int Main(string[] args)
		{
			var options = ParseArguments(args);
			if (options == null)
				return 0;
			if (/*options.GetNonParamOptions().Length == 0 ||*/ options.Get() == null || options.Get().Help || !options.HasAnyOptions())
			{
				Console.WriteLine(options.GenerateHeader(GetBlazerLibraryVersion()));
				Console.WriteLine();
				Console.WriteLine(options.GenerateHelp());
				// Console.Error.WriteLine("Please, specify input file name");
				return 0;
			}

			try
			{
				return Process(options);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine(ex.Message);
				return 1;
			}
		}

		private static int Process(CommandLineParser<BlazerCommandLineOptions> options)
		{
			var opt = options.Get();

			if (!opt.Stdout)
			{
				Console.WriteLine(options.GenerateHeader(GetBlazerLibraryVersion()));
				Console.WriteLine();
			}

			if (opt.Decompress)
			{
				return ProcessDecompress(options);
			}
			else if (opt.Test)
			{
				return ProcessTest(options);
			}
			else if (opt.List)
			{
				return ProcessList(options);
			}
			else
			{
				return ProcessCompress(options);
			}
		}

		private static int ProcessCompress(CommandLineParser<BlazerCommandLineOptions> options)
		{
			var opt = options.Get();

			var fileOptions = FileNameHelper.ParseCompressOptions(options);
			if (fileOptions == null) return 1;

			var truncateOutFile = false;

			if (!opt.Stdout && File.Exists(fileOptions.ArchiveName))
			{
				if (!opt.Force)
				{
					// cannot ask question here
					if (opt.Stdin)
					{
						Console.Error.WriteLine("Archive already exists. Please, specify -f option to override it");
						return 1;
					}

					Console.WriteLine("Archive already exists. Overwrite? (Y)es (N)o");
					var readLine = Console.ReadLine();
					if (readLine.Trim().ToLowerInvariant().IndexOf('y') != 0) return 1;
				}

				truncateOutFile = true;
			}

			var mode = (opt.Mode ?? "block").ToLowerInvariant();

			BlazerCompressionOptions compressionOptions = BlazerCompressionOptions.CreateStream();
			compressionOptions.Password = opt.Password;
			compressionOptions.EncryptFull = opt.EncryptFull;
			compressionOptions.Comment = opt.Comment;

			if (!opt.NoFileName && !opt.Stdin)
			{
				if (fileOptions.SourceFiles.Length == 1) compressionOptions.FileInfo = BlazerFileInfo.FromFileName(fileOptions.SourceFiles[0], false);
				else compressionOptions.MultipleFiles = true;
			}
			else
			{
				if (fileOptions.SourceFiles.Length > 1)
				{
					Console.Error.WriteLine("No File Name option cannot be used with multiple files");
					return 1;
				}
			}

			if (mode == "none")
				compressionOptions.SetEncoderByAlgorithm(BlazerAlgorithm.NoCompress);
			else if (mode == "stream")
				compressionOptions.SetEncoderByAlgorithm(BlazerAlgorithm.Stream);
			else if (mode == "streamhigh")
				compressionOptions.Encoder = new StreamEncoderHigh();
			else if (mode == "block")
			{
				compressionOptions.SetEncoderByAlgorithm(BlazerAlgorithm.Block);
				compressionOptions.MaxBlockSize = BlazerCompressionOptions.DefaultBlockBlockSize;
			}
			else throw new InvalidOperationException("Invalid compression mode");

			if (!string.IsNullOrEmpty(opt.MaxBlockSize))
			{
				if (opt.MaxBlockSize.All(char.IsDigit)) compressionOptions.MaxBlockSize = Convert.ToInt32(opt.MaxBlockSize);
				else
				{
					BlazerFlags flagsBlockSize;
					if (Enum.TryParse("InBlockSize" + opt.MaxBlockSize, true, out flagsBlockSize))
						compressionOptions.SetMaxBlockSizeFromFlags(flagsBlockSize);
				}
			}

			if (truncateOutFile)
				new FileStream(fileOptions.ArchiveName, FileMode.Truncate, FileAccess.Write).Close();

			var outStream = opt.Stdout ? Console.OpenStandardOutput() : new FileStream(fileOptions.ArchiveName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);

			if (opt.DataArray)
			{
				byte[] sourceData;
				using (var sourceStream = opt.Stdin ? Console.OpenStandardInput() : File.OpenRead(fileOptions.SourceFiles[0]))
				{
					var tmpOutStream = new MemoryStream();
					sourceStream.CopyTo(tmpOutStream);
					sourceData = tmpOutStream.ToArray();
				}

				var encoder = compressionOptions.Encoder;
				encoder.Init(sourceData.Length);
				var res = encoder.Encode(sourceData, 0, sourceData.Length);
				outStream.Write(new[] { (byte)sourceData.Length, (byte)(sourceData.Length >> 8), (byte)(sourceData.Length >> 16), (byte)(sourceData.Length >> 24) }, 0, 4);
				outStream.Write(res.Buffer, res.Offset, res.Count);
				outStream.Close();
			}
			else
			{
				using (var outFile = new BlazerInputStream(outStream, compressionOptions))
				{
					if (opt.Stdin)
					{
						using (var inFile = new StatStream(Console.OpenStandardInput(), !opt.Stdout)) inFile.CopyTo(outFile);
					}
					else
					{
						foreach (var fileName in fileOptions.SourceFiles)
						{
							if (fileOptions.SourceFiles.Length > 1)
							{
								var blazerFileInfo = BlazerFileInfo.FromFileName(fileName, !opt.NoPathName);
								outFile.WriteFileInfo(blazerFileInfo);
								if ((blazerFileInfo.Attributes & FileAttributes.Directory) != 0)
									continue;
							}

							using (var inFile = new StatStream(File.OpenRead(fileName), !opt.Stdout))
							{
								inFile.CopyTo(outFile);
							}
						}
					}
				}
			}
			
			return 0;
		}

		private static int ProcessDecompress(CommandLineParser<BlazerCommandLineOptions> options)
		{
			var opt = options.Get();

			var fileOptions = FileNameHelper.ParseDecompressOptions(options);

			if (fileOptions == null) return 1;

			Regex[] customOutFileNames = fileOptions.SourceFiles
					.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim())
					.Select(x => new Regex(new string(x.SelectMany(y => char.IsLetterOrDigit(y) ? new[] { y } : (y == '*' ? new[] { '.', '*' } : new[] { '\\', y })).ToArray())))
					.ToArray();

			if (customOutFileNames.Length == 0)
				customOutFileNames = new[] { new Regex(".*") };

			Stream inStreamSource = opt.Stdin ? Console.OpenStandardInput() : File.OpenRead(fileOptions.ArchiveName);

			var decOptions = new BlazerDecompressionOptions(opt.Password) { EncyptFull = opt.EncryptFull };

			BlazerOutputStream outBlazerStream = null;
			Stream outStream;
			BlazerFileInfo prevFile = null;
			Stream[] outFile = { null };
			bool forceAllOverride = false;
			bool forceAllSkip = opt.Stdin && !opt.Force; // nothing to do, just skip
			decOptions.FileInfoCallback = fInfo =>
				{
					if (opt.Stdout)
						return;

					if (prevFile != null)
					{
						if (outFile[0] != null)
						{
							outFile[0].Flush();
							outFile[0].Close();
							outFile[0] = null;
						}

						prevFile.ApplyToFile();
						prevFile = null;
					}

					var fInfoFileName = fInfo.FileName;
					if (customOutFileNames != null && !customOutFileNames.Any(y => y.IsMatch(fInfoFileName)))
						return;

					prevFile = fInfo;
					if ((fInfo.Attributes & FileAttributes.Directory) != 0)
					{
						if (!opt.NoPathName && !Directory.Exists(fInfoFileName)) Directory.CreateDirectory(fInfoFileName);
					}
					else
					{
						if (opt.NoPathName) fInfoFileName = Path.GetFileName(fInfoFileName);

						if (File.Exists(fInfoFileName))
						{
							if (forceAllSkip)
								return;
							if (!opt.Force && !forceAllOverride)
							{
								Console.WriteLine("Target " + fInfoFileName + " already exists. Overwrite? (Y)es (N)o (A)ll (S)kip all existing");
								var readLine = Console.ReadLine().Trim().ToLowerInvariant();

								forceAllOverride = readLine.IndexOf("a", StringComparison.Ordinal) >= 0;
								forceAllSkip = readLine.IndexOf("s", StringComparison.Ordinal) >= 0;
								if (!forceAllOverride && readLine.IndexOf("y", StringComparison.Ordinal) < 0)
									return;
							}

							new FileStream(fInfoFileName, FileMode.Truncate, FileAccess.Write).Close();
						}

						var directoryName = Path.GetDirectoryName(fInfoFileName);
						if (!string.IsNullOrEmpty(directoryName)) Directory.CreateDirectory(directoryName);
						outFile[0] = new StatStream(new FileStream(fInfoFileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read), true);
						var statStream = outFile[0] as StatStream;
						Console.WriteLine();
						statStream.Prefix = "Extracting " + fInfo.FileName + "  ";
					}
				};

			if (opt.DataArray)
			{
				var mode = (opt.Mode ?? "block").ToLowerInvariant();
				if (mode == "stream" || mode == "streamhigh") decOptions.SetDecoderByAlgorithm(BlazerAlgorithm.Stream);
				else if (mode == "none") decOptions.SetDecoderByAlgorithm(BlazerAlgorithm.NoCompress);
				else if (mode == "block") decOptions.SetDecoderByAlgorithm(BlazerAlgorithm.Block);
				else throw new InvalidOperationException("Unsupported mode");
				var ms = new MemoryStream();
				inStreamSource.CopyTo(ms);
				var comprArray = ms.ToArray();
				var uncomprLength = comprArray[0] | (comprArray[1] << 8) | (comprArray[2] << 16) | (comprArray[3] << 24);
				var decoder = decOptions.Decoder;
				decoder.Init(uncomprLength);
				var decoded = decoder.Decode(comprArray, 4, comprArray.Length, true);
				outStream = new MemoryStream(decoded.Buffer, decoded.Offset, decoded.Count);
			}
			else
			{
				outBlazerStream = new BlazerOutputStream(inStreamSource, decOptions);
				outStream = outBlazerStream;
			}

			var fileName = Path.GetFileName(fileOptions.ArchiveName ?? "dummy");
			if (fileName.EndsWith(".blz")) fileName = fileName.Substring(0, fileName.Length - 5);
			else fileName += (fileName.EndsWith(".") ? string.Empty : ".") + "unpacked";

			if (outBlazerStream != null && outBlazerStream.FileInfo != null && !opt.NoFileName)
			{
				fileName = outBlazerStream.FileInfo.FileName;
			}

			// nothing to do
			if (!outBlazerStream.HaveMultipleFiles && !customOutFileNames.Any(y => y.IsMatch(fileName)))
				return 0;

			if (opt.Stdout) outFile[0] = Console.OpenStandardOutput();
			// we haven't received an file info from callback
			if (outFile[0] == null && !outBlazerStream.HaveMultipleFiles)
				outFile[0] = new StatStream(new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read), true);

			using (var inFile = outStream)
			{
				var buf = new byte[81920];
				int cnt = 1;
				while (cnt > 0)
				{
					cnt = inFile.Read(buf, 0, buf.Length);
					if (outFile[0] != null)
						outFile[0].Write(buf, 0, cnt);
				}
			}

			if (prevFile != null)
			{
				outFile[0].Flush();
				outFile[0].Close();
				prevFile.ApplyToFile();
			}

			return 0;
		}

		private static int ProcessTest(CommandLineParser<BlazerCommandLineOptions> options)
		{
			// todo: refactor. decompress method is similar
			var opt = options.Get();

			var fileOptions = FileNameHelper.ParseDecompressOptions(options);

			if (fileOptions == null) return 1;

			Stream inStreamSource = opt.Stdin ? Console.OpenStandardInput() : File.OpenRead(fileOptions.ArchiveName);

			var decOptions = new BlazerDecompressionOptions(opt.Password) { EncyptFull = opt.EncryptFull };

			Stream outStream;

			if (opt.DataArray)
			{
				var mode = (opt.Mode ?? "block").ToLowerInvariant();
				if (mode == "stream" || mode == "streamhigh") decOptions.SetDecoderByAlgorithm(BlazerAlgorithm.Stream);
				else if (mode == "none") decOptions.SetDecoderByAlgorithm(BlazerAlgorithm.NoCompress);
				else if (mode == "block") decOptions.SetDecoderByAlgorithm(BlazerAlgorithm.Block);
				else throw new InvalidOperationException("Unsupported mode");
				var ms = new MemoryStream();
				inStreamSource.CopyTo(ms);
				var comprArray = ms.ToArray();
				var uncomprLength = comprArray[0] | (comprArray[1] << 8) | (comprArray[2] << 16) | (comprArray[3] << 24);
				var decoder = decOptions.Decoder;
				decoder.Init(uncomprLength);
				// if we do not fail - all ok
				var decoded = decoder.Decode(comprArray, 4, comprArray.Length, true);
				outStream = new MemoryStream(decoded.Buffer, decoded.Offset, decoded.Count);
			}
			else
			{
				outStream = new BlazerOutputStream(inStreamSource, decOptions);
			}

			using (var inFile = outStream)
			using (var outFile = new StatStream(new NullStream(), true))
			{
				inFile.CopyTo(outFile);
			}

			Console.WriteLine();
			Console.WriteLine("File is correct");

			return 0;
		}

		private static int ProcessList(CommandLineParser<BlazerCommandLineOptions> options)
		{
			// todo: refactor. decompress method is similar
			var opt = options.Get();

			var fileOptions = FileNameHelper.ParseDecompressOptions(options);

			if (fileOptions == null) return 1;

			Stream inStreamSource = opt.Stdin ? Console.OpenStandardInput() : File.OpenRead(fileOptions.ArchiveName);

			var decOptions = new BlazerDecompressionOptions(opt.Password) { EncyptFull = opt.EncryptFull };
			StringBuilder header = new StringBuilder();
			header.AppendLine("   Date      Time    Attr         Size  Name");
			header.AppendLine("------------------- ----- ------------  ------------------------");
			bool[] headerWritten = { false };
			decOptions.FileInfoCallback = fi =>
				{
					var s = string.Format(
						"{0:yyyy-MM-dd} {1:HH:mm:ss} {2}{3}{4}{5}{6} {7,12}  {8}",
						fi.CreationTimeUtc.ToLocalTime(),
						fi.CreationTimeUtc.ToLocalTime(),
						(fi.Attributes & FileAttributes.Directory) != 0 ? "D" : ".",
						(fi.Attributes & FileAttributes.ReadOnly) != 0 ? "R" : ".",
						(fi.Attributes & FileAttributes.Hidden) != 0 ? "H" : ".",
						(fi.Attributes & FileAttributes.System) != 0 ? "S" : ".",
						(fi.Attributes & FileAttributes.Archive) != 0 ? "A" : ".",
						fi.Length,
						fi.FileName);
					if (!headerWritten[0]) header.AppendLine(s);
					else Console.WriteLine(s);
				};

			if (opt.DataArray)
			{
				Console.WriteLine("Data array does not contain file info");
				return 1;
			}

			// format is simplified 7z output
			using (var outStream = new BlazerOutputStream(inStreamSource, decOptions))
			{
				Console.WriteLine("Listing archive: " + (opt.Stdin ? "stdin" : fileOptions.ArchiveName));
				Console.WriteLine("Method: " + outStream.Algorithm);
				Console.WriteLine("Max block size: " + outStream.MaxUncompressedBlockSize);
				if (outStream.Comment != null)
				{
					Console.WriteLine("Comment: " + outStream.Comment);
				}

				if (outStream.FileInfo == null && !outStream.HaveMultipleFiles)
				{
					Console.WriteLine("Missing file information in archive, using generated data");
				}

				Console.WriteLine();
				Console.Write(header);
				headerWritten[0] = true;
				var fi = outStream.FileInfo;
				// showing dummy file info to allow extraction through far or something other
				if (fi == null && !outStream.HaveMultipleFiles)
				{
					var fileName = Path.GetFileName(fileOptions.ArchiveName ?? "dummy");
					if (fileName.EndsWith(".blz")) fileName = fileName.Substring(0, fileName.Length - 5);
					else fileName += (fileName.EndsWith(".") ? string.Empty : ".") + "unpacked";

					decOptions.FileInfoCallback(new BlazerFileInfo
													{
														FileName = fileName
													});
					// return 1;
				}
				else
				{
					// while we read, we will write info
					if (outStream.HaveMultipleFiles)
						outStream.CopyTo(new NullStream());
				}

				Console.WriteLine("------------------- ----- ------------  ------------------------");
				// todo: think about total line
				//                                  4854         1018  2 files, 1 folders
			}

			return 0;
		}

		private static Assembly CurrentDomainOnAssemblyResolve(object sender, ResolveEventArgs args)
		{
			if (!args.Name.StartsWith("Blazer.Net")) return null;
			byte[] assemblyBytes;
			using (var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("Force.Blazer.Exe.Resources.Blazer.Net.dll"))
			{
				var ms = new MemoryStream();
				s.CopyTo(ms);
				assemblyBytes = ms.ToArray();
			}

			return Assembly.Load(assemblyBytes);
		}

		private static CommandLineParser<BlazerCommandLineOptions> ParseArguments(string[] args)
		{
			try
			{
				return new CommandLineParser<BlazerCommandLineOptions>(args);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine(ex.Message);
				return null;
			}
		}
	}
}
