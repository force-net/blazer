using System;

namespace Force.Blazer.Exe.CommandLine
{
	[AttributeUsage(AttributeTargets.Property)]
	public class CommandLineOptionAttribute : Attribute
	{
		public char ShortKey { get; set; }

		public string LongKey { get; set; }

		public string Description { get; set; }

		public CommandLineOptionAttribute(char shortKey, string longKey, string description)
		{
			ShortKey = shortKey;
			LongKey = longKey;
			Description = description;
		}

		public CommandLineOptionAttribute(string longKey, string description)
		{
			LongKey = longKey;
			Description = description;
		}
	}
}
