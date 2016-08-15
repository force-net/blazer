using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Force.Blazer.Exe.CommandLine
{
	public class CommandLineParser<T> where T : new()
	{
		private T _options;

		private List<string> _nonKeyOptions;

		public CommandLineParser(string[] args)
		{
			ParseArgumentsInternal(args);
		}

		private void ParseArgumentsInternal(string[] args)
		{
			var t = new T();

			var knownOptions =
				typeof(T).GetProperties()
						.Select(x => new Tuple<PropertyInfo, CommandLineOptionAttribute>(x, (CommandLineOptionAttribute)x.GetCustomAttributes(typeof(CommandLineOptionAttribute), true).FirstOrDefault()))
						.Where(x => x.Item2 != null)
						.ToArray();

			var boolOptions =
				knownOptions.Where(x => x.Item1.PropertyType == typeof(bool) || x.Item1.PropertyType == typeof(bool?)).ToArray();

			Action<string> throwError = e => { throw new InvalidOperationException("Invalid commandline argument " + e); };
			var dict = new Dictionary<string, string>();
			var list = new List<string>();
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
						if (boolOptions.Any(x => x.Item2.ShortKey.ToString(CultureInfo.InvariantCulture) == key || x.Item2.LongKey == key)) key = null;
						if (key == null)
						{
							list.Add(arg);
						}
						else
						{
							dict[key] = arg;
							key = null;
						}
					}
				}
			}

			_nonKeyOptions = list;

			foreach (var v in dict)
			{
				var option = knownOptions.FirstOrDefault(
					x => x.Item2.ShortKey.ToString(CultureInfo.InvariantCulture) == v.Key || x.Item2.LongKey == v.Key);
				if (option != null)
				{
					if (boolOptions.Contains(option))
					{
						option.Item1.SetValue(t, true, null);
					}
					else
					{
						var converted = new TypeConverter().ConvertTo(v.Value, option.Item1.PropertyType);
						option.Item1.SetValue(t, converted, null);
					}
				}
			}

			_options = t;
		}

		public T Get()
		{
			return _options;
		}

		public string[] GetNonParamOptions()
		{
			return _nonKeyOptions.ToArray();
		}

		public string GetNonParamOptions(int idx)
		{
			if (_nonKeyOptions.Count <= idx) return null;
			return _nonKeyOptions[idx];
		}

		public string GenerateHelp()
		{
			var options = typeof(T).GetProperties()
						.Select(x => (CommandLineOptionAttribute)x.GetCustomAttributes(typeof(CommandLineOptionAttribute), true).FirstOrDefault())
						.Where(x => x != null)
						.ToArray();

			var maxParamLen = 0;

			foreach (var option in options)
			{
				var cnt = 0;
				if (option.ShortKey != default(char) && option.LongKey != null) cnt = 6 + option.LongKey.Length;
				else if (option.ShortKey != default(char)) cnt = 2;
				else cnt = 2 + option.LongKey.Length;
				maxParamLen = Math.Max(maxParamLen, cnt);
			}

			var b = new StringBuilder();
			foreach (var option in options)
			{
				b.Append("\t");
				string str;
				if (option.ShortKey != default(char) && option.LongKey != null)
				{
					str = string.Format("-{0}, --{1}", option.ShortKey, option.LongKey);
				}
				else if (option.ShortKey != default(char))
				{
					str = string.Format("-{0}", option.ShortKey);
				}
				else
				{
					str = string.Format("--{0}", option.LongKey);
				}

				b.Append(str);
				b.Append(new string(' ', maxParamLen - str.Length));
				b.Append("\t");
				b.Append(option.Description);
				b.AppendLine();
			}

			return b.ToString();
		}

		public string GenerateHeader()
		{
			var title = (AssemblyTitleAttribute)Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), true).First();
			var copy = (AssemblyCopyrightAttribute)Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCopyrightAttribute), true).First();
			var version = (AssemblyFileVersionAttribute)Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyFileVersionAttribute), true).First();

			return string.Format("{0} {2}  {1}", title.Title, copy.Copyright, version.Version);
		}
	}
}
