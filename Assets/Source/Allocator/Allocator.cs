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
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="blocks">Number of 8 byte blocks of available memory.</param>
        public Allocator(int blocks)
        {
            size = blocks * sizeof(long);
            ptr = Marshal.AllocHGlobal(size);

            for (int i = 0; i < blocks; i++)
            {
                *((long*)ptr.ToPointer() + i) = 0;
            }
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

            while (offset < this.size)
            {
                Block* block = (Block*)GetPtrAt(offset);

                if (!block->InUse && block->Size <= size)
                {
                    return offset;
                }

                offset += sizeof(Block) + block->Size;
            }

            throw new OutOfMemoryException();
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
    }
}
