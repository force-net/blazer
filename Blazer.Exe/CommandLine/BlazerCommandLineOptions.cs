namespace Force.Blazer.Exe.CommandLine
{
	[CommandLineDescription("Usage: Blazer.exe [options] [archiveName.blz] sourceFile|@fileList")]
	public class BlazerCommandLineOptions
	{
		[CommandLineOption('h', "help", "Display this help")]
		public bool Help { get; set; }

		[CommandLineOption('d', "decompress", "Decompress archive")]
		public bool Decompress { get; set; }

		[CommandLineOption('l', "list", "List content of archive")]
		public bool List { get; set; }

		[CommandLineOption('t', "test", "Test archive")]
		public bool Test { get; set; }

		[CommandLineOption('f', "force", "Overwrite target files without confirmation")]
		public bool Force { get; set; }

		[CommandLineOption("stdin", "Read data from stdin")]
		public bool Stdin { get; set; }

		[CommandLineOption("stdout", "Write data to stdout")]
		public bool Stdout { get; set; }

		[CommandLineOption('p', "password", "Archive password")]
		public string Password { get; set; }

		[CommandLineOption("encyptfull", "Encrypt archive fully (this key required on decompress)")]
		public bool EncryptFull { get; set; }

		[CommandLineOption("nofilename", "Do not (re)store file name")]
		public bool NoFileName { get; set; }

		[CommandLineOption("nopathname", "Do not (re)store information about paths")]
		public bool NoPathName { get; set; }

		[CommandLineOption("mode", "Compression mode: none, block (default), stream, streamhigh")]
		public string Mode { get; set; }

		[CommandLineOption("maxblocksize", "Specifies maximum size of data chunk")]
		public string MaxBlockSize { get; set; }

		[CommandLineOption("dataarray", "Compress to solid array with 4-bytes length prefix")]
		public bool DataArray { get; set; }

		[CommandLineOption("comment", "Add comment to archive")]
		public string Comment { get; set; }
	}
}
