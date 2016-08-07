using System;
using System.IO;

using Force.Blazer.Algorithms;
using Force.Blazer.Algorithms.Crc32C;
using Force.Blazer.Encyption;
using Force.Blazer.Helpers;

namespace Force.Blazer
{
	/// <summary>
	/// Base version of blazer compression stream. You can use it in advanced scenarios.
	/// </summary>
	public class BlazerInputStream : Stream
	{
		#region Stream stub

		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new NotSupportedException();
		}

		public override void SetLength(long value)
		{
			throw new NotSupportedException();
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			throw new NotSupportedException();
		}

		public override bool CanRead
		{
			get
			{
				return false;
			}
		}

		public override bool CanSeek
		{
			get
			{
				return false;
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
				return -1L;
			}
		}

		public override long Position
		{
			get
			{
				return -1L;
			}

			set
			{
				throw new NotSupportedException();
			}
		}

		#endregion

		private readonly Stream _innerStream;

		private readonly IEncoder _encoder;

		private readonly int _maxInBlockSize;

		private readonly bool _includeCrc;
		private readonly bool _includeHeader;
		private readonly bool _includeFooter;
		private readonly bool _respectFlush;

		private readonly int _outBufferHeaderSize;

		private readonly byte _encoderAlgorithmId;

		private byte[] _header;

		private readonly byte[] _blockHeader;

		private readonly byte[] _innerBuffer;

		private int _innerBufferPos;

		private readonly byte[] _fileInfoHeader;

		private readonly NullEncryptHelper _encryptHelper;

		private readonly bool _leaveStreamOpen;

		public BlazerInputStream(Stream innerStream, BlazerCompressionOptions options)
		{
			if (innerStream == null)
				throw new ArgumentNullException("innerStream");

			if (!innerStream.CanWrite)
				throw new InvalidOperationException("Base stream is invalid");

			_innerStream = innerStream;

			var flags = options.GetFlags();

			var encyptOuter = (flags & BlazerFlags.EncryptOuter) != 0;
			if (encyptOuter)
			{
				if (string.IsNullOrEmpty(options.Password))
					throw new InvalidOperationException("Encryption flag was set, but password is missing.");
				_innerStream = EncryptHelper.ConvertStreamToEncyptionStream(innerStream, options.Password);
			}

			_leaveStreamOpen = options.LeaveStreamOpen;

			_includeCrc = (flags & BlazerFlags.IncludeCrc) != 0;
			_includeHeader = (flags & BlazerFlags.IncludeHeader) != 0;
			_includeFooter = (flags & BlazerFlags.IncludeFooter) != 0;
			_respectFlush = (flags & BlazerFlags.RespectFlush) != 0;

			_maxInBlockSize = 1 << ((((int)flags) & 15) + 9);
			_innerBuffer = new byte[_maxInBlockSize];
			_outBufferHeaderSize = _includeCrc ? 8 : 4;
			_encoder = options.Encoder;
			_encoderAlgorithmId = (byte)_encoder.GetAlgorithmId();
			if (_encoderAlgorithmId > 15)
				throw new InvalidOperationException("Invalid encoder algorithm");

			if (!string.IsNullOrEmpty(options.Password) && !encyptOuter)
			{
				flags |= BlazerFlags.EncryptInner;
				_encryptHelper = new EncryptHelper(options.Password, _maxInBlockSize);
			}
			else
			{
				_encryptHelper = new NullEncryptHelper();
			}

			if (_includeHeader)
			{
				_header = new byte[]
							{
								(byte)'b', (byte)'L', (byte)'z',
								0x00, // version of file structure
								(byte)((((uint)flags) & 0xff) | ((uint)_encoderAlgorithmId << 4)),
								(byte)(((uint)flags >> 8) & 0xff),
								(byte)(((uint)flags >> 16) & 0xff),
								(byte)(((uint)flags >> 24) & 0xff)
							};
			}
			else
			{
				_header = new byte[0];
			}

			_header = _encryptHelper.AppendHeader(_header);
			if (options.FileInfo != null)
			{
				_fileInfoHeader = FileHeaderHelper.GenerateFileHeader(options.FileInfo);
			}

			_blockHeader = new byte[_outBufferHeaderSize];
			_encoder.Init(_maxInBlockSize);
		}

		protected override void Dispose(bool disposing)
		{
			ProcessAndWrite();
			// zero-length file
			if (_header != null)
			{
				if (_header.Length > 0)
					_innerStream.Write(_header, 0, _header.Length);
				_header = null;
			}

			if (_includeFooter)
				_innerStream.Write(new byte[] { 0xff, (byte)'Z', (byte)'l', (byte)'B' }, 0, 4);

			_innerStream.Flush();

			if (!_leaveStreamOpen)
				_innerStream.Dispose();
			_encoder.Dispose();
			base.Dispose(disposing);
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			while (true)
			{
				if (_innerBufferPos == _maxInBlockSize)
				{
					ProcessAndWrite();
				}

				var toWrite = Math.Min(_maxInBlockSize - _innerBufferPos, count);
				if (toWrite == 0)
					break;

				Buffer.BlockCopy(buffer, offset, _innerBuffer, _innerBufferPos, toWrite);
				_innerBufferPos += toWrite;
				count -= toWrite;
				offset += toWrite;
			}
		}

		public override void Flush()
		{
			if (_respectFlush)
			{
				ProcessAndWrite();
				_innerStream.Flush();
			}
		}

		private void ProcessAndWrite()
		{
			// nothing to write
			if (_innerBufferPos == 0)
				return;

			var info = _encoder.Encode(_innerBuffer, 0, _innerBufferPos);
			// should not compress
			if (info.Count > _innerBufferPos)
			{
				WriteOuterBlock(_innerBuffer, 0, _innerBufferPos, 0x00);
			}
			else
			{
				WriteOuterBlock(info.Buffer, info.Offset, info.Length, _encoderAlgorithmId);
			}

			_innerBufferPos = 0;
		}

		private void WriteOuterBlock(byte[] bufferOut, int offset, int length, byte blockType)
		{
			if (_header != null)
			{
				if (_header.Length > 0)
					_innerStream.Write(_header, 0, _header.Length);

				_header = null;

				if (_fileInfoHeader != null)
					WriteOuterBlock(_fileInfoHeader, 0, _fileInfoHeader.Length, 0xfd);
			}

			if (length == 0) return;
			var o = length - 1; // -1 - we always write here at least 1 byte, so there is no point to send info about this byte

			var blockHeader = _blockHeader;

			blockHeader[0] = blockType;
			blockHeader[1] = (byte)o;
			blockHeader[2] = (byte)(o >> 8);
			blockHeader[3] = (byte)(o >> 16);

			var targetBuffer = _encryptHelper.Encrypt(bufferOut, offset, length);

			if (_includeCrc)
			{
				var crc = Crc32C.Calculate(targetBuffer.Buffer, targetBuffer.Offset, targetBuffer.Count);
				blockHeader[4] = (byte)crc;
				blockHeader[5] = (byte)(crc >> 8);
				blockHeader[6] = (byte)(crc >> 16);
				blockHeader[7] = (byte)(crc >> 24);
			}

			_innerStream.Write(blockHeader, 0, _outBufferHeaderSize);
			_innerStream.Write(targetBuffer.Buffer, targetBuffer.Offset, targetBuffer.Count);
		}
	}
}
