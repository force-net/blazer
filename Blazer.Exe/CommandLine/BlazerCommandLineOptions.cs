namespace Force.Blazer.Exe.CommandLine
{
	public class BlazerCommandLineOptions
	{
		[CommandLineOption('h', "help", "Display this help")]
		public bool Help { get; set; }

		[CommandLineOption('d', "decompress", "Decompress archive")]
		public bool Decompress { get; set; }

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

		[CommandLineOption("mode", "Compression mode: none, block (default), stream, streamhigh")]
		public string Mode { get; set; }

		[CommandLineOption("blobonly", "Compress to blob (no header and footer)")]
		public bool BlobOnly { get; set; }

		[CommandLineOption('t', "test", "Test archive")]
		public bool Test { get; set; }
	}
}
