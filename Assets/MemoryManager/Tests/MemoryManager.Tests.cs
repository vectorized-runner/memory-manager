using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Memory
{
	public unsafe class MemoryManagerTests : MonoBehaviour
	{
		private static List<MemoryBlock> AllocatedBlocks = new();

		[SetUp]
		public static void SetUp()
		{
			MemoryManager.Init();
			Debug.Assert(AllocatedBlocks.Count == 0);
		}

		[TearDown]
		public static void TearDown()
		{
			foreach (var block in AllocatedBlocks)
			{
				MemoryManager.Deallocate(block);
			}

			AllocatedBlocks.Clear();
			MemoryManager.ResetCache();
		}

#if MemoryManagerSafetyChecks

		[Test]
		public static void NegativeSizeAllocThrows()
		{
			Assert.Throws<Exception>(() => MemoryManager.Allocate(-1, 16, Allocation.Temp));
		}

		[TestCase(3)]
		[TestCase(5)]
		[TestCase(100)]
		public static void NonPowerOf2AlignmentThrows(int alignment)
		{
			Assert.Throws<Exception>(() => MemoryManager.Allocate(16, alignment, Allocation.Temp));
		}

		[Test]
		public static void TempMemoryThrowsIfNotFreed()
		{
			var memory = MemoryManager.Allocate(100, 8, Allocation.Temp);
			MemoryManager.ProgressFrame(1);
			AllocatedBlocks.Add(memory);
			Assert.Throws<Exception>(MemoryManager.CheckForMemoryUsagePolicy);
		}

		[Test]
		public static void NegativeAlignmentThrows()
		{
			Assert.Throws<Exception>(() => MemoryManager.Allocate(16, -1, Allocation.Temp));
		}

		[Test]
		public static void DeallocateNegativeSizeThrows()
		{
			Assert.Throws<Exception>(() => MemoryManager.Deallocate(new MemoryBlock { Size = -1 }));
		}

#endif

		[Test]
		public static void ZeroSizeReturnsNullPointer()
		{
			var memory = MemoryManager.Allocate(0, 0, Allocation.Temp);
			Assert.IsTrue(memory.Ptr == null);
		}

		[Test]
		public static void DeallocateNegativeSizeWithNullPointerThrows()
		{
			var memoryBlock = new MemoryBlock
			{
				Size = -5,
				Ptr = null
			};

			Assert.Throws<Exception>(() => MemoryManager.Deallocate(memoryBlock));
		}

		[Test]
		public static void PositiveSizeReturnsNonNullPointer()
		{
			var memory = MemoryManager.Allocate(100, 0, Allocation.Temp);
			Assert.IsTrue(memory.Ptr != null);
			AllocatedBlocks.Add(memory);
		}

		[TestCase(100, 200)]
		[TestCase(1, 2)]
		[TestCase(20, 50)]
		[TestCase(1, 999)]
		public static void ReallocGreaterSizeWorks(int initialSize, int newSize)
		{
			var alignment = 16;
			var alloc = MemoryManager.Allocate(initialSize, alignment, Allocation.Temp);
			var realloc = MemoryManager.Reallocate(alloc, newSize);
			
			Assert.AreEqual(newSize, realloc.Size);
			Assert.AreEqual(alignment, realloc.Alignment);
			Assert.IsTrue(realloc.Ptr != null);
			
			AllocatedBlocks.Add(realloc);
		}

		[TestCase(1)]
		[TestCase(4)]
		[TestCase(100)]
		[TestCase(2000)]
		[TestCase(10_000)]
		public static void CanWriteToAllocatedMemory(int arraySize)
		{
			var align = 16;
			var size = sizeof(int) * arraySize;
			var memory = MemoryManager.Allocate(size, align, Allocation.Temp);
			var span = new Span<int>(memory.Ptr, arraySize);
			
			for (int i = 0; i < arraySize; i++)
			{
				span[i] = i;
			}
			
			AllocatedBlocks.Add(memory);
		}
		
		[TestCase(4)]
		[TestCase(8)]
		[TestCase(16)]
		[TestCase(128)]
		[TestCase(1024)]
		[TestCase(4096)]
		public static void TestAlignmentRequested(int alignment)
		{
			var memory = MemoryManager.Allocate(100, alignment, Allocation.Temp);
			Assert.Zero(new IntPtr(memory.Ptr).ToInt64() % alignment);
			AllocatedBlocks.Add(memory);
		}

		[TestCase(1)]
		[TestCase(4)]
		[TestCase(16)]
		[TestCase(100)]
		[TestCase(400)]
		[TestCase(16_000)]
		[TestCase(200_000)]
		[TestCase(1_000_000)]
		public static void TestAllocationSizeAtLeastRequested(int requestedSize)
		{
			var memory = MemoryManager.Allocate(requestedSize, 0, Allocation.Temp);
			Assert.GreaterOrEqual(memory.Size, requestedSize);
			AllocatedBlocks.Add(memory);
		}
	}
}