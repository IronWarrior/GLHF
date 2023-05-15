using NUnit.Framework;
using System;

namespace GLHF.Tests
{
    public unsafe class TestsAllocator
    {
        private Allocator allocator;

        [SetUp]
        public void Setup()
        {
            allocator = new Allocator(1024);
        }

        [TearDown]
        public void TearDown()
        {
            allocator = null;
        }

        [Test]
        public void CalculateAllocatedMemory_ReturnsCorrectSize()
        {
            allocator.Allocate(100);

            int allocatedMemory = allocator.CalculateAllocatedMemory();

            int expectedSize = 100 + sizeof(Allocator.Block);
            Assert.AreEqual(expectedSize, allocatedMemory, "The calculated allocated memory is incorrect.");
        }

        [Test]
        public void Release_ValidPointer_MarksBlockAsNotInUse()
        {
            int size = 16;
            byte* ptr = allocator.Allocate(size);
            var block = (Allocator.Block*)(ptr - sizeof(Allocator.Block));

            Assert.IsTrue(block->InUse);

            allocator.Release(ptr);
            
            Assert.IsFalse(block->InUse);
        }

        [Test]
        public void CopyFrom_AnotherAllocator_CopiesDataCorrectly()
        {
            var sourceAllocator = new Allocator(1024);
            byte[] data = new byte[50];
            new Random().NextBytes(data);

            sourceAllocator.Allocate(data);
            allocator.CopyFrom(sourceAllocator);

            CollectionAssert.AreEqual(sourceAllocator.ToByteArray(true), allocator.ToByteArray(true));
        }

        [Test]
        public void Constructor_AllocatorInitialized_MemoryIsZeroedOut()
        {
            int size = 100;
            Allocator allocator = new Allocator(size);

            byte[] result = allocator.ToByteArray(false);

            CollectionAssert.AreEqual(new byte[size * 8], result);
        }
    }
}
