using System;

namespace Force.Blazer.Exe.CommandLine
{
	[AttributeUsage(AttributeTargets.Class)]
	public class CommandLineDescriptionAttribute : Attribute
	{
		public string Description { get; set; }

		public CommandLineDescriptionAttribute(string description)
		{
			Description = description;
		}
	}
}
