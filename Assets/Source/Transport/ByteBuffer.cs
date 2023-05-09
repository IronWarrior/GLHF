namespace GLHF.Transport
{
    public unsafe class ByteBuffer
    {
        public int Position { get; set; }
        public readonly byte[] Data;

        private const int defaultCapacity = 1024;

        public ByteBuffer(int capacity=defaultCapacity)
        {
            Data = new byte[capacity];

            Position = 0;
        }

        public ByteBuffer(byte[] data)
        {
            Data = data;

            Position = 0;
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

        public void Put<T>(T[] values) where T : unmanaged
        {
            for (int i = 0; i < values.Length; i++)
            {
                Put(values[i]);
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

        public T[] Get<T>(int count) where T : unmanaged
        {
            T[] values = new T[count];

            for (int i = 0; i < count; i++)
            {
                values[i] = Get<T>();
            }

            return values;
        }
    }
}
