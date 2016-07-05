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

		private readonly byte[] _fileInfoHeader;

		private readonly NullEncryptHelper _encryptHelper;

		private readonly bool _leaveStreamOpen;

		public BlazerInputStream(Stream innerStream, BlazerCompressionOptions options)
		{
			if (innerStream == null)
				throw new ArgumentNullException("innerStream");

			_innerStream = innerStream;
			if (!_innerStream.CanWrite)
				throw new InvalidOperationException("Base stream is invalid");

			var flags = options.GetFlags();

			_leaveStreamOpen = options.LeaveStreamOpen;

			_includeCrc = (flags & BlazerFlags.IncludeCrc) != 0;
			_includeHeader = (flags & BlazerFlags.IncludeHeader) != 0;
			_includeFooter = (flags & BlazerFlags.IncludeFooter) != 0;
			_respectFlush = (flags & BlazerFlags.RespectFlush) != 0;

			_maxInBlockSize = 1 << ((((int)flags) & 15) + 9);
			_outBufferHeaderSize = _includeCrc ? 8 : 4;
			_encoder = options.Encoder;
			_encoderAlgorithmId = (byte)_encoder.GetAlgorithmId();
			if (_encoderAlgorithmId > 15)
				throw new InvalidOperationException("Invalid encoder algorithm");

			Action<byte[], int, byte> writeOuterBlock = WriteOuterBlock;

			if (!string.IsNullOrEmpty(options.Password))
			{
				flags |= BlazerFlags.EncryptInner;
				_encryptHelper = new EncryptHelper(options.Password);
				writeOuterBlock = WriteOuterBlockEncrypted;
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
				_fileInfoHeader = FileHeaderHelper.GenerateFileHeader(options.FileInfo, _outBufferHeaderSize);
			}

			_encoder.Init(_maxInBlockSize, _outBufferHeaderSize, writeOuterBlock);
		}

		protected override void Dispose(bool disposing)
		{
			_encoder.CompressAndWrite();
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
			_encoder.Write(buffer, offset, count);
		}

		public override void Flush()
		{
			if (_respectFlush)
			{
				_encoder.CompressAndWrite();
				_innerStream.Flush();
			}
		}

		private void WriteOuterBlock(byte[] bufferOut, int length, byte blockType)
		{
			if (_header != null)
			{
				if (_header.Length > 0)
					_innerStream.Write(_header, 0, _header.Length);

				_header = null;

				if (_fileInfoHeader != null)
					WriteOuterBlock(_fileInfoHeader, _fileInfoHeader.Length, 0xfd);
			}

			if (length == _outBufferHeaderSize) return;
			var o = length - _outBufferHeaderSize - 1; // -1 - we always write here at least 1 byte, so there is no point to send info about this byte
			
			bufferOut[0] = blockType;
			bufferOut[1] = (byte)o;
			bufferOut[2] = (byte)(o >> 8);
			bufferOut[3] = (byte)(o >> 16);

			if (_includeCrc)
			{
				var crc = Crc32C.Calculate(bufferOut, _outBufferHeaderSize, length - _outBufferHeaderSize);
				bufferOut[4] = (byte)crc;
				bufferOut[5] = (byte)(crc >> 8);
				bufferOut[6] = (byte)(crc >> 16);
				bufferOut[7] = (byte)(crc >> 24);
			}

			_innerStream.Write(bufferOut, 0, length);
		}

		private void WriteOuterBlockEncrypted(byte[] bufferOut, int length, byte blockType)
		{
			if (_header != null)
			{
				if (_header.Length > 0)
					_innerStream.Write(_header, 0, _header.Length);

				_header = null;

				if (_fileInfoHeader != null)
					WriteOuterBlockEncrypted(_fileInfoHeader, _fileInfoHeader.Length, 0xfd);
			}

			if (length == _outBufferHeaderSize) return;
			var o = length - _outBufferHeaderSize - 1; // -1 - we always write here at least 1 byte, so there is no point to send info about this byte

			bufferOut[0] = blockType;
			bufferOut[1] = (byte)o;
			bufferOut[2] = (byte)(o >> 8);
			bufferOut[3] = (byte)(o >> 16);

			var targetBuffer = _encryptHelper.Encrypt(bufferOut, _outBufferHeaderSize, length - _outBufferHeaderSize);
			var targetLength = targetBuffer.Length;

			if (_includeCrc)
			{
				var crc = Crc32C.Calculate(targetBuffer, 0, targetLength);
				bufferOut[4] = (byte)crc;
				bufferOut[5] = (byte)(crc >> 8);
				bufferOut[6] = (byte)(crc >> 16);
				bufferOut[7] = (byte)(crc >> 24);
			}

			_innerStream.Write(bufferOut, 0, _outBufferHeaderSize);
			_innerStream.Write(targetBuffer, 0, targetLength);
		}
	}
}
