using System;
using System.Diagnostics;
using System.IO;

namespace Force.Blazer.Exe
{
	public class StatStream : Stream
	{
		private readonly Stream _baseStream;

		private long _totalHandled;

		private readonly bool _doStats;

		private readonly long _totalLength;

		private int prevPcnt = -1;

		private readonly Stopwatch _sw = new Stopwatch();

		public string Prefix { get; set; }

		public StatStream(Stream baseStream, bool doStats = false)
		{
			_baseStream = baseStream;
			_doStats = doStats;
			if (doStats)
			{
				_totalLength = baseStream.CanSeek ? baseStream.Length : -1;
				_sw.Start();
			}
		}

		public override void Close()
		{
			_baseStream.Close();
			DoStats();
		}

		public override void Flush()
		{
			_baseStream.Flush();
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			return _baseStream.Seek(offset, origin);
		}

		public override void SetLength(long value)
		{
			_baseStream.SetLength(value);
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			var readed = _baseStream.Read(buffer, offset, count);
			_totalHandled += readed;
			if (_doStats)
			{
				DoStats();
			}

			return readed;
		}

		private void DoStats()
		{
			var elapsed = _sw.ElapsedMilliseconds;
			if (elapsed == 0)
				return;
			if (_totalLength > 0)
			{
				var pcnt = 10000 * _totalHandled / _totalLength;
				var pcntAdj = (int)(pcnt / 100);
				if (pcntAdj != prevPcnt && pcnt > 0)
				{
					var eta = (10000 * elapsed / pcnt) - elapsed;
					Console.Write(
						"\r{3}{0,3}% ETA: {1} {2}MB/s  ",
						pcntAdj,
						TimeSpan.FromMilliseconds(eta).ToString(@"hh\:mm\:ss"),
						_totalHandled / 1048 / elapsed,
						Prefix);
					prevPcnt = pcntAdj;
				}
			}
			else
			{
				Console.Write("\r{2}Processed {0,3}MB {1}MB/s  ", _totalHandled / 1048576, _totalHandled / 1048 / elapsed, Prefix);
			}
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			_baseStream.Write(buffer, offset, count);
			_totalHandled += count;
			if (_doStats)
			{
				DoStats();
			}
		}

		public override bool CanRead
		{
			get
			{
				return _baseStream.CanRead;
			}
		}

		public override bool CanSeek
		{
			get
			{
				return _baseStream.CanSeek;
			}
		}

		public override bool CanWrite
		{
			get
			{
				return _baseStream.CanWrite;
			}
		}

		public override long Length
		{
			get
			{
				return _baseStream.Length;
			}
		}

		public override long Position
		{
			get
			{
				return _baseStream.Position;
			}

			set
			{
				_baseStream.Position = value;
			}
		}
	}
}
