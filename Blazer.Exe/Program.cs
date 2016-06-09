using System;
using System.IO;
using System.Linq;
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
			if (args.Length == 0)
			{
				Console.WriteLine("Please, specify input file name");
				return;
			}

			var isDecompress = args.Any(x => x == "-d");

			if (isDecompress)
			{
				var archiveName = args[args.Length - 1];

				if (!File.Exists(archiveName))
				{
					Console.WriteLine("Archive file " + archiveName + " does not exist");
					return;
				}

				var fileName = archiveName;
				if (archiveName.EndsWith(".blz")) fileName = fileName.Substring(0, fileName.Length - 4);
				else fileName += ".unpacked";

				if (File.Exists(fileName))
				{
					Console.WriteLine("Target already exists. Overwrite? (Y)es (N)o");
					var readLine = Console.ReadLine();
					if (readLine.Trim().ToLowerInvariant().IndexOf('y') != 0) return;
				}

				using (var inFile = new BlazerDecompressionStream(File.OpenRead(archiveName)))
				using (var outFile = File.OpenWrite(fileName))
				{
					inFile.CopyTo(outFile);
				}
			}
			else
			{
				var fileName = args[args.Length - 1];
				if (!File.Exists(fileName))
				{
					Console.WriteLine("Source file " + fileName + " does not exist");
				}

				var archiveName = args[0] + ".blz";
				if (File.Exists(archiveName))
				{
					Console.WriteLine("Archive already exists. Overwrite? (Y)es (N)o");
					var readLine = Console.ReadLine();
					if (readLine.Trim().ToLowerInvariant().IndexOf('y') != 0) return;
				}

				using (var inFile = File.OpenRead(fileName))
				using (var outFile = new BlazerBlockCompressionStream(File.OpenWrite(archiveName)))
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
	}
}
