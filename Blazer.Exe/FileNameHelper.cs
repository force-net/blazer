using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Force.Blazer.Exe.CommandLine;

namespace Force.Blazer.Exe
{
	public class FileNameHelper
	{
		public class FileOptions
		{
			public string ArchiveName { get; set; }

			public string[] SourceFiles { get; set; }
		}

		public static FileOptions ParseCompressOptions(CommandLineParser<BlazerCommandLineOptions> options)
		{
			var opt = options.Get();
			var nonParamOptions = options.GetNonParamOptions();
			if (nonParamOptions.Length == 0 && !opt.Stdout)
			{
				Console.WriteLine(options.GenerateHelp());
				return null;
			}

			var listFile = options.GetNonParamOptions().FirstOrDefault(x => x[0] == '@');
			if (listFile != null && nonParamOptions.Length != 2 - (opt.Stdout ? 1 : 0))
			{
				Console.Error.WriteLine("When list file is provided, only archive name is allowed");
				return null;
			}

			if (listFile != null && opt.Stdin)
			{
				Console.Error.WriteLine("Stdin is not compatible with list file");
				return null;
			}

			if (opt.Stdin && nonParamOptions.Length > 1)
			{
				Console.Error.WriteLine("Stdin is not compatible with multiple files");
				return null;
			}

			var archiveName = opt.Stdout ? null : nonParamOptions[0];
			string[] filesToCompress;

			if (listFile != null)
			{
				listFile = listFile.Remove(0, 1);
				if (!File.Exists(listFile))
				{
					Console.Error.WriteLine("Invalid list file");
					return null;
				}

				filesToCompress =
					File.ReadAllLines(listFile).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToArray();
			}
			else
			{
				filesToCompress = opt.Stdin ? new string[0] : nonParamOptions.Skip(1).ToArray();

				if (filesToCompress.Length == 0 && archiveName != null && !opt.Stdin) filesToCompress = new[] { archiveName };
			}

			bool hasMissingFiles;
			filesToCompress = ExpandFilesInList(filesToCompress, out hasMissingFiles);

			if (hasMissingFiles)
			{
				Console.Error.WriteLine("One or more of files to compress does not exist");
				return null;
			}

			if (archiveName != null && !archiveName.EndsWith(".blz")) archiveName += ".blz";

			return new FileOptions
						{
							ArchiveName = archiveName,
							SourceFiles = filesToCompress
						};
		}

		public static FileOptions ParseDecompressOptions(CommandLineParser<BlazerCommandLineOptions> options)
		{
			var opt = options.Get();

			var nonParamOptions = options.GetNonParamOptions();

			if (!opt.Stdin && nonParamOptions.Length == 0)
			{
				Console.Error.WriteLine("Archive name was not specified");
				return null;
			}

			var archiveName = opt.Stdin ? null : nonParamOptions[0];

			if (archiveName != null && !File.Exists(archiveName))
			{
				Console.Error.WriteLine("Archive file " + archiveName + " does not exist");
				return null;
			}

			var listFile = options.GetNonParamOptions().FirstOrDefault(x => x[0] == '@');
			if (listFile != null && nonParamOptions.Length != 2 - (opt.Stdin ? 1 : 0))
			{
				Console.Error.WriteLine("When list file is provided, only archive name is allowed");
				return null;
			}

			string[] customOutFileNames = null;

			if (listFile != null)
			{
				listFile = listFile.Remove(0, 1);
				if (!File.Exists(listFile))
				{
					Console.Error.WriteLine("Invalid list file");
					return null;
				}

				customOutFileNames = File.ReadAllLines(listFile).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToArray();
			}
			else
			{
				customOutFileNames = nonParamOptions.Skip(opt.Stdin ? 0 : 1).ToArray();
			}

			return new FileOptions
			{
				ArchiveName = archiveName,
				SourceFiles = customOutFileNames
			};
		}

		private static string[] ExpandFilesInList(string[] initialFiles, out bool hasMissingFiles)
		{
			hasMissingFiles = false;
			var l = new List<string>();
			// todo: better search + unit tests
			foreach (var s in initialFiles)
			{
				if (File.Exists(s))
					l.Add(s);
				else if (Directory.Exists(s)) l.Add(s);
				else
				{
					var asteriskIdx = s.IndexOf("*", StringComparison.InvariantCulture);
					if (asteriskIdx < 0) hasMissingFiles = true;
					else
					{
						var slashIdx = asteriskIdx > 0 ? s.LastIndexOfAny(
							new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, asteriskIdx - 1, asteriskIdx - 1) : -1;
						var dirToSearch = ".";
						if (slashIdx >= 0) dirToSearch = s.Substring(0, slashIdx);
						l.AddRange(Directory.GetFiles(dirToSearch, s.Remove(0, slashIdx + 1), SearchOption.AllDirectories));
					}
				}
			}

			return l.ToArray();
		}
	}
}
