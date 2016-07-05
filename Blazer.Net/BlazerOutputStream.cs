using System;
using System.IO;

using Force.Blazer.Algorithms;
using Force.Blazer.Algorithms.Crc32C;
using Force.Blazer.Encyption;
using Force.Blazer.Helpers;

namespace Force.Blazer
{
	public class BlazerOutputStream : Stream
	{
		#region Stream stub

		public override void Flush()
		{
			throw new NotSupportedException();
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new NotSupportedException();
		}

		public override void SetLength(long value)
		{
			throw new NotSupportedException();
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			throw new NotSupportedException();
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
				return false;
			}
		}

		public override bool CanWrite
		{
			get
			{
				return false;
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

		private IDecoder _decoder;

		private bool _includeCrc;
		private bool _includeFooter;

		private int _maxUncompressedBlockSize;

		private byte _algorithmId;

		private bool _shouldHaveFileInfo;

		private readonly NullDecryptHelper _decryptHelper;

		private BlazerFileInfo _fileInfo;

		public BlazerFileInfo FileInfo
		{
			get
			{
				return _fileInfo;
			}
		}

		public BlazerOutputStream(Stream innerStream, IDecoder decoder, BlazerFlags flags, string password = null)
		{
			_innerStream = innerStream;
			if (!_innerStream.CanRead)
				throw new InvalidOperationException("Base stream is invalid");

			if ((flags & BlazerFlags.IncludeHeader) != 0)
				throw new InvalidOperationException("Flags cannot contains IncludeHeader flags");

			_decoder = decoder;
			_maxUncompressedBlockSize = 1 << ((((int)flags) & 15) + 9);
			_decoder.Init(_maxUncompressedBlockSize, GetNextChunk);
			_algorithmId = (byte)_decoder.GetAlgorithmId();
			_includeCrc = (flags & BlazerFlags.IncludeCrc) != 0;
			_includeFooter = (flags & BlazerFlags.IncludeFooter) != 0;
			if (!string.IsNullOrEmpty(password))
			{
				_decryptHelper = new DecryptHelper(password);
				var encHeader = new byte[_decryptHelper.GetHeaderLength()];
				if (!EnsureRead(encHeader, 0, encHeader.Length))
					throw new InvalidOperationException("Missing encryption header");
				_decryptHelper.Init(encHeader);
			}
		}

		public BlazerOutputStream(Stream innerStream, BlazerAlgorithm algorithm, BlazerFlags flags, string password = null)
			: this(innerStream, EncoderDecoderFactory.GetDecoder(algorithm), flags, password)
		{
		}

		public BlazerOutputStream(Stream innerStream, string password = null)
		{
			_innerStream = innerStream;
			_innerStream = innerStream;
			if (!_innerStream.CanRead)
				throw new InvalidOperationException("Base stream is invalid");
			if (!string.IsNullOrEmpty(password))
				_decryptHelper = new DecryptHelper(password);
			ReadAndValidateHeader();

			// todo: refactor this
			if (_shouldHaveFileInfo)
			{
				var inLength = GetNextChunkHeader(false);
				if (inLength == 0)
					throw new InvalidOperationException("Missing file info");

				var inLengthOrig = inLength;
				if (_decryptHelper != null)
					inLength = _decryptHelper.AdjustLength(inLength);

				if (_encodingType != 0xfd)
					throw new InvalidOperationException("Invalid header data");

				var fileBuf = new byte[inLength];

				if (!EnsureRead(fileBuf, 0, inLength))
					throw new InvalidOperationException("Invalid block data");

				if (_includeCrc)
				{
					var realCrc = Crc32C.Calculate(fileBuf, 0, inLength);

					if (realCrc != _passedCrc)
						throw new InvalidOperationException("Invalid CRC32C data in passed block. It seems, data error is occured.");
				}

				if (_decryptHelper != null)
				{
					var decrypted = _decryptHelper.Decrypt(fileBuf, 0, inLength);
					Buffer.BlockCopy(decrypted, 0, fileBuf, 0, inLengthOrig);
				}

				_fileInfo = FileHeaderHelper.ParseFileHeader(fileBuf);
			}
		}

		protected override void Dispose(bool disposing)
		{
			_innerStream.Dispose();
			_decoder.Dispose();
			base.Dispose(disposing);
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			return _decoder.Read(buffer, offset, count);
		}

		private void ReadAndValidateHeader()
		{
			var buf = new byte[8];
			if (!EnsureRead(buf, 0, 8))
				throw new InvalidOperationException("Invalid Stream");
			if (buf[0] != 'b' || buf[1] != 'L' || buf[2] != 'z')
				throw new InvalidOperationException("This is not blazer archive");
			if (buf[3] != 0x00)
				throw new InvalidOperationException("Stream is created in new version of archiver. Please, update library.");
			BlazerFlags flags = (BlazerFlags)(buf[4] | ((uint)buf[5] << 8) | ((uint)buf[6] << 16) | ((uint)buf[7] << 24));

			_decoder = EncoderDecoderFactory.GetDecoder((BlazerAlgorithm)((((uint)flags) >> 4) & 15));
			_maxUncompressedBlockSize = 1 << ((((int)flags) & 15) + 9);
			Func<byte[], Tuple<int, byte, bool>> nextChunk = GetNextChunk;
			_algorithmId = (byte)_decoder.GetAlgorithmId();
			_includeCrc = (flags & BlazerFlags.IncludeCrc) != 0;
			_includeFooter = (flags & BlazerFlags.IncludeFooter) != 0;
			_shouldHaveFileInfo = (flags & BlazerFlags.OnlyOneFile) != 0;
			if ((flags & BlazerFlags.EncryptInner) != 0)
			{
				if (_decryptHelper == null)
					throw new InvalidOperationException("Stream is encrypted.");
				var encHeader = new byte[_decryptHelper.GetHeaderLength()];
				if (!EnsureRead(encHeader, 0, encHeader.Length))
					throw new InvalidOperationException("Missing encryption header");
				_decryptHelper.Init(encHeader);
				nextChunk = GetNextChunkEncrypted;
			}

			_decoder.Init(_maxUncompressedBlockSize, nextChunk);

			if (_includeFooter && _innerStream.CanSeek)
			{
				var position = _innerStream.Seek(0, SeekOrigin.Current);
				_innerStream.Seek(-4, SeekOrigin.End);
				if (!EnsureRead(buf, 0, 4))
					throw new Exception("Footer is missing");
				ValidateFooter(buf);
				_innerStream.Seek(position, SeekOrigin.Begin);
			}
		}

		private void ValidateFooter(byte[] footer)
		{
			if (footer[0] != 0xff || footer[1] != (byte)'Z' || footer[2] != (byte)'l' || footer[3] != (byte)'B')
				throw new InvalidOperationException("Invalid footer. Possible stream was truncated");
		}

		private readonly byte[] _sizeBlock = new byte[4];

		private byte _encodingType;

		private uint _passedCrc;

		private int GetNextChunkHeader(bool validateEncodingType)
		{
			// end of stream
			if (!EnsureRead(_sizeBlock, 0, 4)) return 0;

			_encodingType = _sizeBlock[0];

			// empty footer
			if (_encodingType == 0xff)
			{
				ValidateFooter(_sizeBlock);
				return 0;
			}

			if (_encodingType != 0 && _encodingType != _algorithmId && validateEncodingType)
				throw new InvalidOperationException("Invalid header");

			var inLength = ((_sizeBlock[1] << 0) | (_sizeBlock[2] << 8) | _sizeBlock[3] << 16) + 1;
			if (inLength > _maxUncompressedBlockSize)
				throw new InvalidOperationException("Invalid block size");

			if (_includeCrc)
			{
				if (!EnsureRead(_sizeBlock, 0, 4))
					throw new InvalidOperationException("Missing CRC32C in stream");
				_passedCrc = ((uint)_sizeBlock[0] << 0) | (uint)_sizeBlock[1] << 8 | (uint)_sizeBlock[2] << 16 | (uint)_sizeBlock[3] << 24;
			}

			return inLength;
		}

		private Tuple<int, byte, bool> GetNextChunk(byte[] inBuffer)
		{
			var inLength = GetNextChunkHeader(true);
			if (inLength == 0) return new Tuple<int, byte, bool>(0, 0x00, false);

			if (!EnsureRead(inBuffer, 0, inLength))
				throw new InvalidOperationException("Invalid block data");

			if (_includeCrc)
			{
				var realCrc = Crc32C.Calculate(inBuffer, 0, inLength);

				if (realCrc != _passedCrc)
					throw new InvalidOperationException("Invalid CRC32C data in passed block. It seems, data error is occured.");
			}

			return new Tuple<int, byte, bool>(inLength, _encodingType, true);
		}

		private Tuple<int, byte, bool> GetNextChunkEncrypted(byte[] inBuffer)
		{
			var inLength = GetNextChunkHeader(true);
			if (inLength == 0) return new Tuple<int, byte, bool>(0, 0x00, false);

			var inLengthOrig = inLength;
			inLength = _decryptHelper.AdjustLength(inLength);

			if (!EnsureRead(inBuffer, 0, inLength))
				throw new InvalidOperationException("Invalid block data");

			if (_includeCrc)
			{
				var realCrc = Crc32C.Calculate(inBuffer, 0, inLength);

				if (realCrc != _passedCrc)
					throw new InvalidOperationException("Invalid CRC32C data in passed block. It seems, data error is occured.");
			}

			var decrypted = _decryptHelper.Decrypt(inBuffer, 0, inLength);
			Buffer.BlockCopy(decrypted, 0, inBuffer, 0, inLengthOrig);

			return new Tuple<int, byte, bool>(inLengthOrig, _encodingType, true);
		}

		private bool EnsureRead(byte[] buffer, int offset, int size)
		{
			var sizeOrig = size;
			while (true)
			{
				var cnt = _innerStream.Read(buffer, offset, size);
				if (cnt == 0)
				{
					if (size == sizeOrig) return false;
					throw new InvalidOperationException("Invalid data");
				}

				size -= cnt;
				if (size == 0) return true;
				offset += cnt;
			}
		}
	}
}
