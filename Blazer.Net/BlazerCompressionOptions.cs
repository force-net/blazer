using System;

using Force.Blazer.Algorithms;

namespace Force.Blazer
{
	public class BlazerCompressionOptions
	{
		public IEncoder Encoder { get; set; }

		private BlazerFlags _flags { get; set; }

		private string _password;

		public string Password
		{
			get
			{
				return _password;
			}

			set
			{
				_password = value;
				SetFlag(BlazerFlags.EncryptInner, !string.IsNullOrEmpty(_password));
			}
		}

		public bool LeaveStreamOpen { get; set; }

		public bool IncludeCrc
		{
			get
			{
				return GetFlag(BlazerFlags.IncludeCrc);
			}

			set
			{
				SetFlag(BlazerFlags.IncludeCrc, value);
			}
		}

		public bool IncludeHeader
		{
			get
			{
				return GetFlag(BlazerFlags.IncludeHeader);
			}

			set
			{
				SetFlag(BlazerFlags.IncludeHeader, value);
			}
		}

		public bool IncludeFooter
		{
			get
			{
				return GetFlag(BlazerFlags.IncludeFooter);
			}

			set
			{
				SetFlag(BlazerFlags.IncludeFooter, value);
			}
		}

		public bool RespectFlush
		{
			get
			{
				return GetFlag(BlazerFlags.RespectFlush);
			}

			set
			{
				SetFlag(BlazerFlags.RespectFlush, value);
			}
		}

		public int MaxBlockSize
		{
			get
			{
				var cnt = (int)(_flags & BlazerFlags.InBlockSize16M);
				return 1 << (cnt + 9);
			}

			set
			{
				if (value < 0)
					throw new ArgumentOutOfRangeException("value", "Block size should be positive");
				var v = value - 1;
				var cnt = 0;
				while (v > 0)
				{
					cnt++;
					v >>= 1;
				}

				cnt -= 9;
				if (cnt < 0) cnt = 0;

				if (cnt > 15) cnt = 15;
				_flags &= ~BlazerFlags.InBlockSize16M;
				_flags |= (BlazerFlags)cnt;
			}
		}

		public static int DefaultStreamBlockSize
		{
			get
			{
				return 65536;
			}
		}

		public static int DefaultBlockBlockSize
		{
			get
			{
				// 2Mb
				return 1 << 21;
			}
		}

		public static BlazerCompressionOptions CreateNoCompression()
		{
			return new BlazerCompressionOptions
						{
							Encoder = EncoderDecoderFactory.GetEncoder(BlazerAlgorithm.NoCompress),
							_flags = BlazerFlags.DefaultStream
						};
		}

		public static BlazerCompressionOptions CreateStream()
		{
			return new BlazerCompressionOptions
			{
				Encoder = EncoderDecoderFactory.GetEncoder(BlazerAlgorithm.Stream),
				_flags = BlazerFlags.DefaultStream
			};
		}

		public static BlazerCompressionOptions CreateStreamHigh()
		{
			return new BlazerCompressionOptions
			{
				Encoder = new StreamEncoderHigh(),
				_flags = BlazerFlags.DefaultStream
			};
		}

		public static BlazerCompressionOptions CreateBlock()
		{
			return new BlazerCompressionOptions
			{
				Encoder = EncoderDecoderFactory.GetEncoder(BlazerAlgorithm.Block),
				_flags = BlazerFlags.DefaultBlock
			};
		}

		public BlazerFlags GetFlags()
		{
			return _flags;
		}

		private void SetFlag(BlazerFlags flag, bool isSet)
		{
			if (isSet) _flags |= flag;
			else _flags &= ~flag;
		}

		private bool GetFlag(BlazerFlags flag)
		{
			return (_flags & flag) != 0;
		}

		public void SetEncoderByAlgorithm(BlazerAlgorithm algorithm)
		{
			Encoder = EncoderDecoderFactory.GetEncoder(algorithm);
		}

		private BlazerFileInfo _fileInfo;

		public BlazerFileInfo FileInfo
		{
			get
			{
				return _fileInfo;
			}

			set
			{
				_fileInfo = value;
				SetFlag(BlazerFlags.OnlyOneFile, _fileInfo != null);
			}
		}
	}
}
