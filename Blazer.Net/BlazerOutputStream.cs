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

		private byte[] _innerBuffer;

		private byte[] _decodedBuffer;

		private int _decodedBufferOffset;

		private int _decodedBufferLength;

		private byte _algorithmId;

		private bool _shouldHaveFileInfo;

		private NullDecryptHelper _decryptHelper;

		private BlazerFileInfo _fileInfo;

		public BlazerFileInfo FileInfo
		{
			get
			{
				return _fileInfo;
			}
		}

		private readonly bool _leaveStreamOpen;

		private Action<byte[], int, int> _controlDataCallback { get; set; }

		public BlazerOutputStream(Stream innerStream, BlazerDecompressionOptions options = null)
		{
			options = options ?? BlazerDecompressionOptions.CreateDefault();
			_leaveStreamOpen = options.LeaveStreamOpen;
			_innerStream = innerStream;
			if (!_innerStream.CanRead)
				throw new InvalidOperationException("Base stream is invalid");

			var password = options.Password;
			_controlDataCallback = options.ControlDataCallback ?? ((b, o, c) => { });

			if (options.EncyptFull)
			{
				if (string.IsNullOrEmpty(options.Password))
					throw new InvalidOperationException("Encryption flag was set, but password is missing.");
				_innerStream = DecryptHelper.ConvertStreamToDecyptionStream(innerStream, options.Password);
				// no more password for this
				password = null;
			}

			if (options.CompressionOptions != null)
			{
				var decoder = options.Decoder;
				if (decoder == null)
				{
					if (options.CompressionOptions.Encoder == null)
						throw new InvalidOperationException("Missing decoder information");
					options.SetDecoderByAlgorithm(options.CompressionOptions.Encoder.GetAlgorithmId());
					decoder = options.Decoder;
				}

				InitByFlags(options.CompressionOptions.GetFlags(), decoder, password);
			}
			else
			{
				InitByOptions(password);
			}
		}

		private void InitByFlags(BlazerFlags flags, IDecoder decoder, string password)
		{
			if ((flags & BlazerFlags.IncludeHeader) != 0)
				throw new InvalidOperationException("Flags cannot contains IncludeHeader flags");

			_decoder = decoder;
			_maxUncompressedBlockSize = 1 << ((((int)flags) & 15) + 9);
			_innerBuffer = new byte[_maxUncompressedBlockSize];
			_decoder.Init(_maxUncompressedBlockSize);
			_algorithmId = (byte)_decoder.GetAlgorithmId();
			_includeCrc = (flags & BlazerFlags.IncludeCrc) != 0;
			_includeFooter = (flags & BlazerFlags.IncludeFooter) != 0;
			if (!string.IsNullOrEmpty(password))
			{
				_decryptHelper = new DecryptHelper(password);
				var encHeader = new byte[_decryptHelper.GetHeaderLength()];
				if (!EnsureRead(encHeader, 0, encHeader.Length))
					throw new InvalidOperationException("Missing encryption header");
				_decryptHelper.Init(encHeader, _maxUncompressedBlockSize);
			}
			else
			{
				_decryptHelper = new NullDecryptHelper();
			}
		}

		private void InitByOptions(string password = null)
		{
			if (!_innerStream.CanRead)
				throw new InvalidOperationException("Base stream is invalid");
			if (!string.IsNullOrEmpty(password))
				_decryptHelper = new DecryptHelper(password);
			else
				_decryptHelper = new NullDecryptHelper();

			ReadAndValidateHeader();

			// todo: refactor this
			if (_shouldHaveFileInfo)
			{
				var fInfo = GetNextChunk(_innerBuffer);
				if (_encodingType != 0xfd)
					throw new InvalidOperationException("Invalid file info header");

				_fileInfo = FileHeaderHelper.ParseFileHeader(fInfo.Buffer, fInfo.Offset, fInfo.Count);
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (!_leaveStreamOpen)
				_innerStream.Dispose();
			_decoder.Dispose();
			base.Dispose(disposing);
		}

		private bool _isFinished;

		public override int Read(byte[] buffer, int offset, int count)
		{
			if (_decodedBufferOffset == _decodedBufferLength)
			{
				if (_isFinished) return 0;
				var info = GetNextChunk(_innerBuffer);
				if (info.Count == 0)
				{
					if (_encodingType == 0xf0)
					{
						_controlDataCallback(new byte[0], 0, 0);
						return Read(buffer, offset, count);
					}

					_isFinished = true;
					return 0;
				}

				if (_encodingType != 0 && _encodingType != _algorithmId)
				{
					if (_encodingType == 0xf1)
					{
						_controlDataCallback(info.Buffer, info.Offset, info.Count);
						return Read(buffer, offset, count);
					}

					throw new InvalidOperationException("Invalid header");
				}

				var decoded = _decoder.Decode(info.Buffer, info.Offset, info.Length, _encodingType != 0);
				_decodedBuffer = decoded.Buffer;
				_decodedBufferOffset = decoded.Offset;
				_decodedBufferLength = decoded.Length;
			}

			var toReturn = Math.Min(count, _decodedBufferLength - _decodedBufferOffset);
			Buffer.BlockCopy(_decodedBuffer, _decodedBufferOffset, buffer, offset, toReturn);
			_decodedBufferOffset += toReturn;
			return toReturn;
		}

		private void ReadAndValidateHeader()
		{
			var buf = new byte[8];
			if (!EnsureRead(buf, 0, 8))
				throw new InvalidOperationException("Invalid input stream");
			if (buf[0] != 'b' || buf[1] != 'L' || buf[2] != 'z')
				throw new InvalidOperationException("This is not Blazer archive");
			if (buf[3] != 0x00)
				throw new InvalidOperationException("Stream was created in new version of Blazer library");
			BlazerFlags flags = (BlazerFlags)(buf[4] | ((uint)buf[5] << 8) | ((uint)buf[6] << 16) | ((uint)buf[7] << 24));

			if ((flags & (~BlazerFlags.AllKnownFlags)) != 0)
				throw new InvalidOperationException("Invalid flag combination. Try to use newer version of Blazer");

			_decoder = EncoderDecoderFactory.GetDecoder((BlazerAlgorithm)((((uint)flags) >> 4) & 15));
			_maxUncompressedBlockSize = 1 << ((((int)flags) & 15) + 9);
			_algorithmId = (byte)_decoder.GetAlgorithmId();
			_includeCrc = (flags & BlazerFlags.IncludeCrc) != 0;
			_includeFooter = (flags & BlazerFlags.IncludeFooter) != 0;
			_shouldHaveFileInfo = (flags & BlazerFlags.OnlyOneFile) != 0;
			if ((flags & BlazerFlags.EncryptInner) != 0)
			{
				if (!(_decryptHelper is DecryptHelper)) throw new InvalidOperationException("Stream is encrypted, but password is not provided");
				var encHeader = new byte[_decryptHelper.GetHeaderLength()];
				if (!EnsureRead(encHeader, 0, encHeader.Length)) throw new InvalidOperationException("Missing encryption header");
				_decryptHelper.Init(encHeader, _maxUncompressedBlockSize);
			}
			else
			{
				if (_decryptHelper is DecryptHelper)
					throw new InvalidOperationException("Stream is not encrypted");
			}

			_decoder.Init(_maxUncompressedBlockSize);

			_maxUncompressedBlockSize = _decryptHelper.AdjustLength(_maxUncompressedBlockSize);

			_innerBuffer = new byte[_maxUncompressedBlockSize];

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

		private int GetNextChunkHeader()
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

			if (_encodingType == 0xf0)
			{
				return 0;
			}

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

		private BufferInfo GetNextChunk(byte[] inBuffer)
		{
			var inLength = GetNextChunkHeader();
			if (inLength == 0) return new BufferInfo(null, 0, 0);

			var origInLength = inLength;

			inLength = _decryptHelper.AdjustLength(inLength);

			if (!EnsureRead(inBuffer, 0, inLength))
				throw new InvalidOperationException("Invalid block data");

			var info = _decryptHelper.Decrypt(inBuffer, 0, inLength);

			if (_includeCrc)
			{
				var realCrc = Crc32C.Calculate(inBuffer, 0, inLength);

				if (realCrc != _passedCrc)
					throw new InvalidOperationException("Invalid CRC32C data in passed block. It seems, data error is occured");
			}

			info.Length = origInLength + info.Offset;

			return info;
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
