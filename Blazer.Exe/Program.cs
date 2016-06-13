using System;
using System.IO;
using System.Reflection;

namespace Force.Blazer.Exe
{
	public class Program
	{
		static Program()
		{
			AppDomain.CurrentDomain.AssemblyResolve += CurrentDomainOnAssemblyResolve;
		}

		public static void Main(string[] args)
		{
			var options = ParseArguments(args);
			if (options == null)
				return;
			if (!options.HasAny())
			{
				Console.Error.WriteLine("Please, specify input file name");
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

		private static void Process(CommandLineOptions options)
		{
			var isDecompress = options.Has("d", "decompress");
			var isForce = options.Has("f", "force");
			var isStdIn = options.Has("stdin");
			var isStdOut = options.Has("stdout");
			var password = options.Get("p", "password");

			if (isDecompress)
			{
				var archiveName = options.Get("def0") ?? options.Get("d", "decompress") ?? string.Empty;

				if (!isStdIn && !File.Exists(archiveName))
				{
					Console.Error.WriteLine("Archive file " + archiveName + " does not exist");
					return;
				}

				var fileName = archiveName;
				if (archiveName.EndsWith(".blz")) fileName = fileName.Substring(0, fileName.Length - 4);
				else fileName += ".unpacked";

				if (!isStdOut && File.Exists(fileName))
				{
					if (!isForce)
					{
						Console.WriteLine("Target already exists. Overwrite? (Y)es (N)o");
						var readLine = Console.ReadLine();
						if (readLine.Trim().ToLowerInvariant().IndexOf('y') != 0) return;
					}

					new FileStream(fileName, FileMode.Truncate, FileAccess.Write).Close();
				}

				using (var inFile = new BlazerDecompressionStream(isStdIn ? Console.OpenStandardInput() : File.OpenRead(archiveName), password))
				using (var outFile = isStdOut ? Console.OpenStandardOutput() : new StatStream(new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read), true))
				{
					inFile.CopyTo(outFile);
				}
			}
			else
			{
				var fileName = options.Get("def0") ?? string.Empty;
				if (!isStdIn && !File.Exists(fileName))
				{
					Console.Error.WriteLine("Source file " + fileName + " does not exist");
					return;
				}

				var archiveName = fileName + ".blz";
				if (!isStdOut && File.Exists(archiveName))
				{
					if (!isForce)
					{
						Console.WriteLine("Archive already exists. Overwrite? (Y)es (N)o");
						var readLine = Console.ReadLine();
						if (readLine.Trim().ToLowerInvariant().IndexOf('y') != 0) return;
					}

					new FileStream(archiveName, FileMode.Truncate, FileAccess.Write).Close();
				}

				var mode = (options.Get("mode") ?? "block").ToLowerInvariant();

				var outStream = isStdOut ? Console.OpenStandardOutput() : new FileStream(archiveName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
				Stream blazerStream;
				if (mode == "none") blazerStream = new BlazerNoCompressionStream(outStream, password: password);
				else if (mode == "stream") blazerStream = new BlazerStreamCompressionStream(outStream, password: password);
				else if (mode == "block") blazerStream = new BlazerBlockCompressionStream(outStream, password: password);
				else throw new InvalidOperationException("Invalid compression mode");

				using (var inFile = isStdIn ? Console.OpenStandardInput() : new StatStream(File.OpenRead(fileName), true))
				using (var outFile = blazerStream)
				{
					inFile.CopyTo(outFile);
				}
			}
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

		private static CommandLineOptions ParseArguments(string[] args)
		{
			try
			{
				return new CommandLineOptions(args);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine(ex.Message);
				return null;
			}
		}
	}
}
