using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Memory
{
	public unsafe class MemoryManagerTests : MonoBehaviour
	{
		private static List<MemoryManager.MemoryBlock> AllocatedBlocks = new();

		[SetUp]
		public static void SetUp()
		{
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

		[TestCase(16, 100, 96)]
		[TestCase(4, 100, 100)]
		[TestCase(8, 100, 96)]
		[TestCase(4096, 50_000, 49_152)]
		[TestCase(1024, 3000, 2048)]
		public static void AlignmentPow2(int align, int address, int output)
		{
			var result = MemoryUtil.AlignUp(address, align);
			Assert.AreEqual(output, result);
		}

#if MemoryManagerSafetyChecks

		[Test]
		public static void NegativeSizeAllocThrows()
		{
			Assert.Throws<Exception>(() => MemoryManager.Allocate(-1, 16));
		}

		[TestCase(3)]
		[TestCase(5)]
		[TestCase(100)]
		public static void NonPowerOf2AlignmentThrows(int alignment)
		{
			Assert.Throws<Exception>(() => MemoryManager.Allocate(16, alignment));
		}

		[Test]
		public static void NegativeAlignmentThrows()
		{
			Assert.Throws<Exception>(() => MemoryManager.Allocate(16, -1));
		}

		[Test]
		public static void DeallocateNegativeSizeThrows()
		{
			Assert.Throws<Exception>(() => MemoryManager.Deallocate(new MemoryManager.MemoryBlock { Size = -1 }));
		}

		[Test]
		public static void DoubleFreeThrows()
		{
			var block = MemoryManager.Allocate(100, 0);

			Assert.Throws<Exception>(() =>
			{
				MemoryManager.Deallocate(block);
				MemoryManager.Deallocate(block);
			});
		}

#endif

		[Test]
		public static void ZeroSizeReturnsNullPointer()
		{
			var memory = MemoryManager.Allocate(0, 0);
			Assert.IsTrue(memory.Ptr == null);
		}


		[Test]
		public static void DeallocateNegativeSizeWithNullPointerDoesNotThrow()
		{
			var memoryBlock = new MemoryManager.MemoryBlock
			{
				Size = -5,
				Ptr = null
			};

			Assert.DoesNotThrow(() => MemoryManager.Deallocate(memoryBlock));
		}

		[Test]
		public static void PositiveSizeReturnsNonNullPointer()
		{
			var memory = MemoryManager.Allocate(100, 0);
			Assert.IsTrue(memory.Ptr != null);
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
			var memory = MemoryManager.Allocate(requestedSize, 0);
			Assert.GreaterOrEqual(memory.Size, requestedSize);
			AllocatedBlocks.Add(memory);
		}
	}
}