﻿using System;
using System.Text;

using Force.Blazer.Algorithms;

namespace Force.Blazer
{
	/// <summary>
	/// Options for compression
	/// </summary>
	public class BlazerCompressionOptions
	{
		/// <summary>
		/// Encoder (realization of compression algorithm)
		/// </summary>
		public IEncoder Encoder { get; set; }

		private BlazerFlags _flags { get; set; }

		/// <summary>
		/// Password for encrypting data
		/// </summary>
		public string Password 
		{ 
			get
			{
				return PasswordRaw == null ? null : Encoding.UTF8.GetString(PasswordRaw);
			} 

			set
			{
				PasswordRaw = string.IsNullOrEmpty(value) ? null : Encoding.UTF8.GetBytes(value);
			}
		}

		/// <summary>
		/// Password for encrypting data (raw binary variant)
		/// </summary>
		public byte[] PasswordRaw { get; set; }

		/// <summary>
		/// Encrypt full flag. Fully encypted streams does not reveal any information about inner data (blazer header is also encypted)
		/// </summary>
		/// <remarks>Flush can be unsupported with this mode</remarks>
		public bool EncryptFull { get; set; }

		/// <summary>
		/// Leave inner stream open after closing blazer stream
		/// </summary>
		public bool LeaveStreamOpen { get; set; }

		/// <summary>
		/// Add Crc data to stream. If data are damaged, error will occure
		/// </summary>
		/// <remarks>Blazer uses Crc32C checksum algorithm</remarks>
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

		/// <summary>
		/// Add header to stream. Stream without header requires manual flags set on decompression
		/// </summary>
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

		/// <summary>
		/// Add footer to stream. Footer allows to check on decompression that stream is correct
		/// </summary>
		/// <remarks>If on decompression stream is not seekable, footer will be validated only after decompressing data. If stream is seekable, before it.</remarks>
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

		/// <summary>
		/// Respect <see cref="System.IO.Stream.Flush"/> command. 
		/// </summary>
		/// <remarks>If it set, every flush will compress current block of data and Flush it into inner stream. Otherwise, flush commands are ignored</remarks>
		public BlazerFlushMode FlushMode { get; set; }

		/// <summary>
		/// Maximum block size to compress. Larger blocks require more memory, but can produce higher compression
		/// </summary>
		/// <remarks>Currently, block sizes from 512 bytes to 16Mb are supported</remarks>
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

		/// <summary>
		/// Sets max block size from flags
		/// </summary>
		public void SetMaxBlockSizeFromFlags(BlazerFlags sizeFlags)
		{
			_flags &= ~BlazerFlags.InBlockSize16M;
			_flags |= sizeFlags & BlazerFlags.InBlockSize16M;
		}

		/// <summary>
		/// Gets default block size for Stream algorithm
		/// </summary>
		public static int DefaultStreamBlockSize
		{
			get
			{
				return 65536;
			}
		}

		/// <summary>
		/// Gets default block size for Block algorithm
		/// </summary>
		public static int DefaultBlockBlockSize
		{
			get
			{
				// 2Mb
				return 1 << 21;
			}
		}

		/// <summary>
		/// Creates default options for no compression algorithm
		/// </summary>
		public static BlazerCompressionOptions CreateNoCompression()
		{
			return new BlazerCompressionOptions
						{
							Encoder = EncoderDecoderFactory.GetEncoder(BlazerAlgorithm.NoCompress),
							_flags = BlazerFlags.DefaultStream
						};
		}

		/// <summary>
		/// Creates default options for Stream algorithm
		/// </summary>
		public static BlazerCompressionOptions CreateStream()
		{
			return new BlazerCompressionOptions
			{
				Encoder = EncoderDecoderFactory.GetEncoder(BlazerAlgorithm.Stream),
				_flags = BlazerFlags.DefaultStream,
				FlushMode = BlazerFlushMode.RespectFlush
			};
		}

		/// <summary>
		/// Creates default options for Stream algorithm with high compression
		/// </summary>
		public static BlazerCompressionOptions CreateStreamHigh()
		{
			return new BlazerCompressionOptions
			{
				Encoder = new StreamEncoderHigh(),
				_flags = BlazerFlags.DefaultStream,
				FlushMode = BlazerFlushMode.RespectFlush
			};
		}

		/// <summary>
		/// Creates default options for Block algorithm
		/// </summary>
		public static BlazerCompressionOptions CreateBlock()
		{
			return new BlazerCompressionOptions
			{
				Encoder = EncoderDecoderFactory.GetEncoder(BlazerAlgorithm.Block),
				_flags = BlazerFlags.DefaultBlock
			};
		}

		/// <summary>
		/// Returns bit flags for current settings
		/// </summary>
		public BlazerFlags GetFlags()
		{
			var flags = _flags;
			if (PasswordRaw != null && PasswordRaw.Length != 0)
			{
				flags |= EncryptFull ? BlazerFlags.EncryptOuter : BlazerFlags.EncryptInner;
			}

			if (FlushMode != BlazerFlushMode.IgnoreFlush)
				flags |= BlazerFlags.RespectFlush;

			return flags;
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

		/// <summary>
		/// Instantiates default encoder for specified algoritm
		/// </summary>
		public void SetEncoderByAlgorithm(BlazerAlgorithm algorithm)
		{
			Encoder = EncoderDecoderFactory.GetEncoder(algorithm);
		}

		private BlazerFileInfo _fileInfo;

		private byte[] _commentRaw;

		/// <summary>
		/// Gets or sets information about encoded file
		/// </summary>
		public BlazerFileInfo FileInfo
		{
			get
			{
				return _fileInfo;
			}

			set
			{
				if (GetFlag(BlazerFlags.MultipleFiles) && value != null)
					throw new InvalidOperationException("Do not set FileInfo in multiple files mode mode");
				_fileInfo = value;
				SetFlag(BlazerFlags.OnlyOneFile, _fileInfo != null);
			}
		}

		/// <summary>
		/// Marks, that archive can contains multiple file, pass file info through <see cref="BlazerInputStream.WriteFileInfo"/>
		/// </summary>
		public bool MultipleFiles
		{
			get
			{
				return GetFlag(BlazerFlags.MultipleFiles);
			}

			set
			{
				if (GetFlag(BlazerFlags.OnlyOneFile) && value)
					throw new InvalidOperationException("Do not set FileInfo in this mode");
				SetFlag(BlazerFlags.MultipleFiles, value);
			}
		}

		/// <summary>
		/// Gets or sets archive comment
		/// </summary>
		public string Comment
		{
			get
			{
				return CommentRaw == null ? null : Encoding.UTF8.GetString(CommentRaw);
			}

			set
			{
				CommentRaw = value == null ? null : Encoding.UTF8.GetBytes(value);
			}
		}

		/// <summary>
		/// Gets or sets archive raw (binary) comment. Useful for adding some info for technical streams
		/// </summary>
		public byte[] CommentRaw
		{
			get
			{
				return _commentRaw;
			}

			set
			{
				_commentRaw = value;
				SetFlag(BlazerFlags.IncludeComment, value != null && value.Length > 0);
			}
		}
	}
}
