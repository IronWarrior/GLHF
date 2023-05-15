using System;
using System.Runtime.InteropServices;

namespace GLHF
{
    public unsafe class Allocator
    {
        public byte* Head => (byte*)ptr.ToPointer();

        private readonly IntPtr ptr;
        private readonly int size;

        public struct Block
        {
            public int Size;            
            public bool InUse;

            public static byte* Data(Block* block)
            {
                return ((byte*)block) + sizeof(Block);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="blocks">Number of 8 byte blocks of available memory.</param>
        public Allocator(int blocks)
        {
            size = blocks * sizeof(long);
            ptr = Marshal.AllocHGlobal(size);

            WriteToZeroes();
        }

        /// <summary>
        /// Deep copies an existing allocator.
        /// </summary>
        /// <param name="allocator"></param>
        public Allocator(Allocator allocator)
        {
            size = allocator.size;
            ptr = Marshal.AllocHGlobal(size);

            CopyFrom(allocator);
        }

        public Allocator(int blocks, byte[] data) : this(blocks)
        {
            CopyFrom(data);
        }

        ~Allocator()
        {
            Marshal.FreeHGlobal(ptr);
        }

        private byte* GetPtrAt(int offset)
        {
            return (byte*)ptr.ToPointer() + offset;
        }

        private int FindFirstFit(int size)
        {
            int offset = 0;

            while (offset + size < this.size)
            {
                Block* block = (Block*)GetPtrAt(offset);

                // Blocks of size zero have not been allocated and can be used
                // regardless of requested size.
                if (!block->InUse && (block->Size == 0 || block->Size >= size))
                {
                    return offset;
                }

                offset += sizeof(Block) + block->Size;
            }

            throw new OutOfMemoryException();
        }

        public int CalculateAllocatedMemory()
        {
            int offset = 0;

            while (offset < size)
            {
                Block* block = (Block*)GetPtrAt(offset);

                // Blocks of size zero have not been allocated, indicating
                // the end of allocated memory.
                if (block->Size == 0)
                {
                    return offset;
                }

                offset += sizeof(Block) + block->Size;
            }

            return size;
        }

        private void WriteToZeroes()
        {
            for (int i = 0; i < size / sizeof(long); i++)
            {
                *((long*)ptr.ToPointer() + i) = 0;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="size">Size of the allocated memory in bytes.</param>
        /// <returns></returns>
        public byte* Allocate(int size)
        {
            int offset = FindFirstFit(size);

            Block* blockHead = (Block*)GetPtrAt(offset);
            blockHead->InUse = true;
            blockHead->Size = size;

            byte* dataPtr = GetPtrAt(offset + sizeof(Block));

            return dataPtr;
        }

        public byte* Allocate(byte[] data)
        {
            byte* ptr = Allocate(data.Length);

            for (int i = 0; i < data.Length; i++)
            {
                byte* b = (ptr + i);
                *b = data[i];
            }

            return ptr;
        }

        public void Release(byte* dataPtr)
        {
            Block* block = (Block*)(dataPtr - sizeof(Block));
            block->InUse = false;
        }

        public Block* Next(Block* current)
        {
            return (Block*)((byte*)current + sizeof(Block) + current->Size);
        }

        public bool NextInUse(Block* current, out Block* next)
        {
            while (current->Size != 0)
            {
                current = Next(current);

                if (current->InUse)
                {
                    next = current;
                    return true;
                }
            }

            next = null;
            return false;
        }

        public long Checksum()
        {
            long checksum = 0;

            long* head = (long*)ptr.ToPointer();

            for (int i = 0; i < size / sizeof(long); i++)
            {
                long value = *(head + i);

                checksum ^= value;
            }

            return checksum;
        }

        public void CopyFrom(Allocator source)
        {
            if (source.size != size)
                throw new Exception();

            Buffer.MemoryCopy(source.ptr.ToPointer(), ptr.ToPointer(), size, size);
        }

        public void CopyFrom(byte[] data)
        {
            if (data.Length > size)
                throw new Exception("Allocator does not have sufficient memory to copy byte[] data.");

            for (int i = 0; i < data.Length; i++)
            {
                byte* b = (Head + i);
                *b = data[i];
            }
        }

        public byte[] ToByteArray(bool trimmed)
        {
            int size;

            if (trimmed)
                size = CalculateAllocatedMemory();
            else
                size = this.size;

            byte[] data = new byte[size];
            Marshal.Copy(ptr, data, 0, size);

            return data;
        }
    }
}
