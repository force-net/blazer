using System.IO;

namespace Force.Blazer.Exe
{
	public class NullStream : Stream
	{
		public override void Flush()
		{
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			return -1;
		}

		public override void SetLength(long value)
		{
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			return 0;
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
		}

		public override bool CanRead
		{
			get
			{
				return true;
			}
		}

		public override bool CanSeek
		{
			get
			{
				return true;
			}
		}

		public override bool CanWrite
		{
			get
			{
				return true;
			}
		}

		public override long Length
		{
			get
			{
				return -1;
			}
		}

		public override long Position
		{
			get
			{
				return -1;
			}

			set
			{
			}
		}
	}
}
