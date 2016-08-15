using System;
using System.IO;
using System.Reflection;

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

		public static void Main(string[] args)
		{
			var options = ParseArguments(args);
			if (options == null)
				return;
			if (/*options.GetNonParamOptions().Length == 0 ||*/ options.Get() == null || options.Get().Help)
			{
				Console.WriteLine(options.GenerateHeader());
				Console.WriteLine();
				Console.WriteLine(options.GenerateHelp());
				// Console.Error.WriteLine("Please, specify input file name");
				return;
			}

			try
			{
				Process(options);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine(ex.Message);
			}
		}

		private static void Process(CommandLineParser<BlazerCommandLineOptions> options)
		{
			var opt = options.Get();

			if (!opt.Stdout)
			{
				Console.WriteLine(options.GenerateHeader());
				Console.WriteLine();
			}

			if (opt.Decompress)
			{
				ProcessDecompress(options);
			}
			else if (opt.Test)
			{
				ProcessTest(options);
			}
			else
			{
				ProcessCompress(options);
			}
		}

		private static void ProcessCompress(CommandLineParser<BlazerCommandLineOptions> options)
		{
			var opt = options.Get();

			var fileName = options.GetNonParamOptions(0) ?? string.Empty;

			if (!opt.Stdin && !File.Exists(fileName))
			{
				if (fileName == string.Empty)
				{
					Console.WriteLine(options.GenerateHelp());
					return;
				}

				Console.Error.WriteLine("Source file " + fileName + " does not exist");
				return;
			}

			var archiveName = fileName + ".blz";
			var truncateOutFile = false;
			if (!opt.Stdout && File.Exists(archiveName))
			{
				if (!opt.Force)
				{
					Console.WriteLine("Archive already exists. Overwrite? (Y)es (N)o");
					var readLine = Console.ReadLine();
					if (readLine.Trim().ToLowerInvariant().IndexOf('y') != 0) return;
				}

				truncateOutFile = true;
			}

			var mode = (opt.Mode ?? "block").ToLowerInvariant();


			BlazerCompressionOptions compressionOptions = BlazerCompressionOptions.CreateStream();
			compressionOptions.Password = opt.Password;
			compressionOptions.EncryptFull = opt.EncryptFull;

			if (!opt.NoFileName)
				compressionOptions.FileInfo = BlazerFileInfo.FromFileName(fileName);

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

			if (opt.BlobOnly)
			{
				compressionOptions.IncludeCrc = false;
				compressionOptions.IncludeFooter = false;
				compressionOptions.IncludeHeader = false;
				compressionOptions.MaxBlockSize = 1 << 24;
				compressionOptions.FileInfo = null;
			}

			if (truncateOutFile)
				new FileStream(archiveName, FileMode.Truncate, FileAccess.Write).Close();

			var outStream = opt.Stdout ? Console.OpenStandardOutput() : new FileStream(archiveName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);

			Stream blazerStream = new BlazerInputStream(outStream, compressionOptions);

			using (var inFile = new StatStream(opt.Stdin ? Console.OpenStandardInput() : File.OpenRead(fileName), !opt.Stdout))
			using (var outFile = blazerStream)
			{
				inFile.CopyTo(outFile);
			}
		}

		private static void ProcessDecompress(CommandLineParser<BlazerCommandLineOptions> options)
		{
			var opt = options.Get();

			var archiveName = options.GetNonParamOptions(0) ?? string.Empty;

			if (!opt.Stdin && !File.Exists(archiveName))
			{
				if (archiveName == string.Empty)
				{
					Console.WriteLine(options.GenerateHelp());
					return;
				}

				Console.Error.WriteLine("Archive file " + archiveName + " does not exist");
				return;
			}

			Stream inStreamSource = opt.Stdin ? Console.OpenStandardInput() : File.OpenRead(archiveName);

			var decOptions = new BlazerDecompressionOptions(opt.Password) { EncyptFull = opt.EncryptFull };

			if (opt.BlobOnly)
			{
				decOptions.CompressionOptions = new BlazerCompressionOptions
				{
					IncludeCrc = false,
					IncludeFooter = false,
					IncludeHeader = false,
					FileInfo = null,
					MaxBlockSize = 1 << 24
				};

				var mode = (opt.Mode ?? "block").ToLowerInvariant();
				if (mode == "stream" || mode == "streamhigh") decOptions.SetDecoderByAlgorithm(BlazerAlgorithm.Stream);
				else if (mode == "none") decOptions.SetDecoderByAlgorithm(BlazerAlgorithm.NoCompress);
				else if (mode == "block") decOptions.SetDecoderByAlgorithm(BlazerAlgorithm.Block);
				else throw new InvalidOperationException("Unsupported mode");
			}

			var outStream = new BlazerOutputStream(inStreamSource, decOptions);

			var fileName = archiveName;
			var applyFileInfoAfterComplete = false;
			if (archiveName.EndsWith(".blz")) fileName = fileName.Substring(0, fileName.Length - 4);
			else fileName += ".unpacked";

			if (outStream.FileInfo != null && !opt.NoFileName)
			{
				fileName = outStream.FileInfo.FileName;
				applyFileInfoAfterComplete = true;
			}

			if (!opt.Stdout && File.Exists(fileName))
			{
				if (!opt.Force)
				{
					Console.WriteLine("Target " + fileName + " already exists. Overwrite? (Y)es (N)o");
					var readLine = Console.ReadLine();
					if (readLine.Trim().ToLowerInvariant().IndexOf('y') != 0) return;
				}

				new FileStream(fileName, FileMode.Truncate, FileAccess.Write).Close();
			}

			using (var inFile = outStream)
			using (var outFile = opt.Stdout ? Console.OpenStandardOutput() : new StatStream(new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read), true))
			{
				inFile.CopyTo(outFile);
			}

			if (applyFileInfoAfterComplete) outStream.FileInfo.ApplyToFile();
		}

		private static void ProcessTest(CommandLineParser<BlazerCommandLineOptions> options)
		{
			// todo: refactor. decompress method is similar
			var opt = options.Get();

			var archiveName = options.GetNonParamOptions(0) ?? string.Empty;

			if (!opt.Stdin && !File.Exists(archiveName))
			{
				if (archiveName == string.Empty)
				{
					Console.WriteLine(options.GenerateHelp());
					return;
				}

				Console.Error.WriteLine("Archive file " + archiveName + " does not exist");
				return;
			}

			Stream inStreamSource = opt.Stdin ? Console.OpenStandardInput() : File.OpenRead(archiveName);

			var decOptions = new BlazerDecompressionOptions(opt.Password) { EncyptFull = opt.EncryptFull };

			if (opt.BlobOnly)
			{
				decOptions.CompressionOptions = new BlazerCompressionOptions
				{
					IncludeCrc = false,
					IncludeFooter = false,
					IncludeHeader = false,
					FileInfo = null,
					MaxBlockSize = 1 << 24
				};

				var mode = (opt.Mode ?? "block").ToLowerInvariant();
				if (mode == "stream" || mode == "streamhigh") decOptions.SetDecoderByAlgorithm(BlazerAlgorithm.Stream);
				else if (mode == "none") decOptions.SetDecoderByAlgorithm(BlazerAlgorithm.NoCompress);
				else if (mode == "block") decOptions.SetDecoderByAlgorithm(BlazerAlgorithm.Block);
				else throw new InvalidOperationException("Unsupported mode");
			}

			var outStream = new BlazerOutputStream(inStreamSource, decOptions);


			using (var inFile = outStream)
			using (var outFile = new StatStream(new NullStream(), true))
			{
				inFile.CopyTo(outFile);
			}

			Console.WriteLine();
			Console.WriteLine("File is correct");
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
