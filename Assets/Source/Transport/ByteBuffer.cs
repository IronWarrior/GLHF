namespace GLHF.Transport
{
    public unsafe class ByteBuffer
    {
        public int Position { get; set; }
        public readonly byte[] Data;

        private const int defaultCapacity = 1024;

        public ByteBuffer()
        {
            Data = new byte[defaultCapacity];
        }

        public ByteBuffer(int capacity)
        {
            Data = new byte[capacity];

            Position = 0;
        }

        public ByteBuffer(byte[] data)
        {
            this.Data = data;
        }

        public void Put<T>(T value) where T : unmanaged
        {
            byte* p = (byte*)&value;

            for (int i = 0; i < sizeof(T); i++)
            {
                Data[Position] = *(p + i);
                Position++;
            }
        }

        public T Get<T>() where T : unmanaged
        {
            fixed (byte* p = Data)
            {
                T value =  *(T*)(p + Position);
                Position += sizeof(T);

                return value;
            }
        }
    }
}
