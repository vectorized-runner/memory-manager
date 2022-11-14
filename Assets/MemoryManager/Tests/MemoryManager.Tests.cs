using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public unsafe class MemoryManagerTests : MonoBehaviour
{
	private static List<MemoryManager.MemoryBlock> AllocatedBlocks = new();

	[SetUp]
	private static void SetUp()
	{
		Debug.Assert(AllocatedBlocks.Count == 0);
	}

	[TearDown]
	private static void TearDown()
	{
		foreach (var block in AllocatedBlocks)
		{
			MemoryManager.Deallocate(block);
		}
	}

	[Test]
	public static void NegativeSizeAllocThrows()
	{
		Assert.Throws<Exception>(() => MemoryManager.Allocate(-1, 16));
	}

	[Test]
	public static void NegativeAlignmentThrows()
	{
		Assert.Throws<Exception>(() => MemoryManager.Allocate(16, -1));
	}

	[Test]
	public static void ZeroSizeReturnsNullPointer()
	{
		var memory = MemoryManager.Allocate(0, 0);
		Assert.IsTrue(memory.Ptr == null);
	}

	[Test]
	public static void DeallocateZeroSizeDoesNotThrow()
	{
		
	}

	[Test]
	public static void DeallocateNegativeSizeWithNullPointerDoesNotThrow()
	{
		
	}
	
	// TODO: Test allocating more than int.maxvalue

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