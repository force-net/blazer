using System;
using System.IO;
using System.Text;

using Force.Blazer.Algorithms;
using Force.Blazer.Algorithms.Crc32C;
using Force.Blazer.Encyption;
using Force.Blazer.Helpers;

namespace Force.Blazer
{
	/// <summary>
	/// Blazer compression stream implementation
	/// </summary>
	public class BlazerInputStream : Stream
	{
		#region Stream stub

		/// <summary>
		/// Not implemented for this stream
		/// </summary>
		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Not implemented for this stream
		/// </summary>
		public override void SetLength(long value)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Not implemented for this stream
		/// </summary>
		public override int Read(byte[] buffer, int offset, int count)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Cannot read this stream
		/// </summary>
		public override bool CanRead
		{
			get
			{
				return false;
			}
		}

		/// <summary>
		/// Cannot seek this stream
		/// </summary>
		public override bool CanSeek
		{
			get
			{
				return false;
			}
		}

		/// <summary>
		/// Stream is writeable
		/// </summary>
		public override bool CanWrite
		{
			get
			{
				return true;
			}
		}

		/// <summary>
		/// Returns -1
		/// </summary>
		public override long Length
		{
			get
			{
				return -1L;
			}
		}

		/// <summary>
		/// Cannot set position in this stream
		/// </summary>
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
		private readonly BlazerFlushMode _flushMode;

		private readonly int _outBufferHeaderSize;

		private readonly byte _encoderAlgorithmId;

		private byte[] _header;

		private readonly byte[] _blockHeader;

		private readonly byte[] _innerBuffer;

		private int _innerBufferPos;

		private readonly byte[] _fileInfoHeader;

		private readonly byte[] _commentHeader;

		private readonly NullEncryptHelper _encryptHelper;

		private readonly bool _leaveStreamOpen;

		private readonly bool _isMultipleFiles;

		private bool _multipleFilesFileInfoSet;

		/// <summary>
		/// Constructs Blazer compression stream
		/// </summary>
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
			_flushMode = options.FlushMode;

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
								0x01, // version of file structure
								(byte)((((uint)flags) & 0x0f) | ((uint)_encoderAlgorithmId << 4)),
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

			_isMultipleFiles = (flags & BlazerFlags.MultipleFiles) != 0;

			if (!string.IsNullOrEmpty(options.Comment))
			{
				_commentHeader = Encoding.UTF8.GetBytes(options.Comment);
				if (_commentHeader.Length > 16 * 1048576)
					throw new InvalidOperationException("Invalid archive comment");
			}

			_blockHeader = new byte[_outBufferHeaderSize];
			_encoder.Init(_maxInBlockSize);
		}

		/// <summary>
		/// Releases the unmanaged resources used by the <see cref="T:System.IO.Stream"/> and optionally releases the managed resources.
		/// </summary>
		/// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
		protected override void Dispose(bool disposing)
		{
			ProcessAndWrite();

			if (_includeFooter)
				_innerStream.Write(new[] { (byte)BlazerBlockType.Footer, (byte)'Z', (byte)'l', (byte)'B' }, 0, 4);

			_innerStream.Flush();

			if (!_leaveStreamOpen)
				_innerStream.Dispose();
			_encoder.Dispose();
			base.Dispose(disposing);
		}

		/// <summary>
		/// Writes control data to stream 
		/// </summary>
		/// <remarks>Control data are not compressed and can be used for passing any service information while compressing data without affecting it</remarks>
		public virtual void WriteControlData(byte[] buffer, int offset, int count)
		{
			if (count > 1 << 24)
				throw new InvalidOperationException("Very big control block");

			if (count < 0)
				throw new InvalidOperationException("Cannot write zero-length data");

			if (_header != null)
			{
				WriteHeader();
			}

			// some variant of ping message
			if (count == 0)
			{
				_innerStream.Write(new byte[] { (byte)BlazerBlockType.ControlDataEmpty, 0, 0, 0 }, 0, 4);
			}
			else
			{
				WriteOuterBlock(buffer, offset, count + offset, BlazerBlockType.ControlData);
			}
		}

		/// <summary>
		/// Writes a sequence of bytes to the current stream and advances the current position within this stream by the number of bytes written.
		/// </summary>
		/// <param name="buffer">An array of bytes. This method copies <paramref name="count"/> bytes from <paramref name="buffer"/> to the current stream. </param>
		/// <param name="offset">The zero-based byte offset in <paramref name="buffer"/> at which to begin copying bytes to the current stream. </param>
		/// <param name="count">The number of bytes to be written to the current stream. </param>
		public override void Write(byte[] buffer, int offset, int count)
		{
			if (_isMultipleFiles && !_multipleFilesFileInfoSet)
				throw new InvalidOperationException("In multiple files mode first block should be file info");

			while (true)
			{
				var toWrite = Math.Min(_maxInBlockSize - _innerBufferPos, count);
				if (toWrite == 0)
					break;

				Buffer.BlockCopy(buffer, offset, _innerBuffer, _innerBufferPos, toWrite);
				_innerBufferPos += toWrite;
				count -= toWrite;
				offset += toWrite;

				if (_innerBufferPos == _maxInBlockSize)
				{
					ProcessAndWrite();
				}
			}

			if (_flushMode == BlazerFlushMode.AutoFlush)
				Flush();
			else if (_flushMode == BlazerFlushMode.RespectFlush && buffer.Length < offset + count) // smart flush will flush data, if it smaller than buffer size. otherwise we use large binary data and it not required to be flushed
				Flush();
		}

		/// <summary>
		/// In multiple files mode adds information about new file (and implies that previous is finished)
		/// </summary>
		public void WriteFileInfo(BlazerFileInfo info)
		{
			if (!_isMultipleFiles)
				throw new InvalidOperationException("Current stream options does not support this operation");
			if (info == null)
				throw new ArgumentNullException("info");
			_multipleFilesFileInfoSet = true;
			
			// write all buffered data
			ProcessAndWrite();
			var fileInfoBytes = FileHeaderHelper.GenerateFileHeader(info);
			WriteOuterBlock(fileInfoBytes, 0, fileInfoBytes.Length, BlazerBlockType.FileInfo);
		}

		/// <summary>
		/// Clears all buffers for this stream and causes any buffered data to be written to the underlying device.
		/// </summary>
		public override void Flush()
		{
			if (_flushMode != BlazerFlushMode.IgnoreFlush)
			{
				ProcessAndWrite();
				_innerStream.Flush();
			}
		}

		private void ProcessAndWrite()
		{
			if (_header != null)
				WriteHeader();

			// nothing to write
			if (_innerBufferPos == 0)
			{
				return;
			}

			var info = _encoder.Encode(_innerBuffer, 0, _innerBufferPos);
			// should not compress
			if (info.Count > _innerBufferPos)
			{
				WriteOuterBlock(_innerBuffer, 0, _innerBufferPos, 0x00);
			}
			else
			{
				WriteOuterBlock(info.Buffer, info.Offset, info.Length, (BlazerBlockType)_encoderAlgorithmId);
			}

			_innerBufferPos = 0;
		}

		private void WriteHeader()
		{
			if (_header != null)
			{
				if (_header.Length > 0)
					_innerStream.Write(_header, 0, _header.Length);

				_header = null;

				if (_commentHeader != null)
					WriteOuterBlock(_commentHeader, 0, _commentHeader.Length, BlazerBlockType.Comment);

				if (_fileInfoHeader != null)
					WriteOuterBlock(_fileInfoHeader, 0, _fileInfoHeader.Length, BlazerBlockType.FileInfo);
			}
		}

		private void WriteOuterBlock(byte[] bufferOut, int offset, int length, BlazerBlockType blockType)
		{
			if (length == 0) return;
			var o = length - 1; // -1 - we always write here at least 1 byte, so there is no point to send info about this byte

			var blockHeader = _blockHeader;

			blockHeader[0] = (byte)blockType;
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
