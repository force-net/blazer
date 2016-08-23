using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace Force.Blazer.Benchmark.MessagesDto
{
	public class LogMessage
	{
		public DateTime EventDate { get; set; }

		public string Level { get; set; }

		public string UserName { get; set; }

		public int ProcessingTime { get; set; }

		public string Message { get; set; }

		public LogMessage()
		{
		}

		public LogMessage(Random r)
		{
			EventDate = new DateTime(2016, 1, 1).AddSeconds(r.Next(60 * 60 * 24 * 365));
			Level = new[] { "DEBUG", "INFO", "WARN", "ERROR", "FATAL" }[r.Next(5)];
			if (r.Next(2) == 0) UserName = "System";
			else UserName = _words[r.Next(_words.Length)];
			ProcessingTime = r.Next(1000);
			Message = string.Join(" ", Enumerable.Range(0, r.Next(10) + 3).Select(x => _words[r.Next(_words.Length)]));
		}

		private static readonly string[] _words;

		static LogMessage()
		{
			var r = new Random(124);
			_words =
				Enumerable.Range(0, 1000)
						.Select(_ => new string(Enumerable.Range(0, r.Next(6) + 1).Select(x => (char)(r.Next(26) + 'a')).ToArray())).ToArray();
		}

		public static byte[][] Generate(int count)
		{
			// fixed seed
			var r = new Random(124);
			var l = new List<LogMessage>();
			for (var i = 0; i < count; i++)
				l.Add(new LogMessage(r));

			var l2 = new List<byte[]>();
			var s = new XmlSerializer(typeof(LogMessage));
			for (var i = 0; i < count; i++)
			{
				var ms = new MemoryStream();
				s.Serialize(ms, l[i]);
				l2.Add(ms.ToArray());
			}

			return l2.ToArray();
		}

		public static byte[] GenerateBestPattern()
		{
			var m = new LogMessage
						{
							EventDate = new DateTime(2016, 1, 1),
							Level = "DEBUGINFOWARNERRORFATAL",
							UserName = "System",
							Message = string.Join(string.Empty, _words)
						};

			var s = new XmlSerializer(typeof(LogMessage));
			var ms = new MemoryStream();
			s.Serialize(ms, m);
			return ms.ToArray();
		}
	}
}
