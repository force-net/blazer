using System;
using System.Collections.Generic;
using System.Linq;

namespace Force.Blazer.Exe
{
	public class CommandLineOptions
	{
		private readonly Dictionary<string, string> _options;

		public CommandLineOptions(string[] args)
		{
			_options = ParseArgumentsInternal(args);
		}

		public bool Has(params string[] keys)
		{
			return keys.Any(key => _options.ContainsKey(key));
		}

		public string Get(params string[] keys)
		{
			return (from key in keys where _options.ContainsKey(key) select _options[key]).FirstOrDefault();
		}

		private static Dictionary<string, string> ParseArgumentsInternal(string[] args)
		{
			Action<string> throwError = e => { throw new InvalidOperationException("Invalid commandline argument " + e); };
			var dict = new Dictionary<string, string>();
			var defCounter = 0;
			string key = null;
			foreach (var arg in args)
			{
				if (arg.Length > 0)
				{
					if (arg[0] == '-')
					{
						if (arg.Length == 1) throwError(arg);
						if (arg[1] == '-')
						{
							if (arg[1] == '-' && arg.Length <= 3) throwError(arg);
							if (key != null) dict[key] = string.Empty;
							key = arg.Remove(0, 2);
						}
						else
						{
							key = arg.Remove(0, 1);
						}

						dict[key] = string.Empty;
					}
					else
					{
						if (key == null)
						{
							dict["def" + (defCounter++)] = arg;
						}
						else
						{
							dict[key] = arg;
							key = null;
						}
					}
				}
			}

			return dict;
		}

		public bool HasAny()
		{
			return _options.Count > 0;
		}
	}
}
