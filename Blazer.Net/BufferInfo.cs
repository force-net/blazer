namespace Force.Blazer
{
	public struct BufferInfo
	{
		public byte[] Buffer;

		public int Offset;

		public int Length;

		public int Count
		{
			get
			{
				return Length - Offset;
			}
		}

		public BufferInfo(byte[] buffer, int offset, int length)
		{
			Buffer = buffer;
			Offset = offset;
			Length = length;
		}
	}
}
