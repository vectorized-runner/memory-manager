using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public static unsafe class MemoryManager
{
	public struct MemoryBlock : IEquatable<MemoryBlock>
	{
		public void* Ptr;
		public int Size;
		public int Alignment;

		public bool Equals(MemoryBlock other)
		{
			return Ptr == other.Ptr;
		}

		public override bool Equals(object obj)
		{
			return obj is MemoryBlock other && Equals(other);
		}

		public override int GetHashCode()
		{
			return unchecked((int)(long)Ptr);
		}
	}

	public struct Frame : IEquatable<Frame>
	{
		public long Value;

		public bool Equals(Frame other)
		{
			return Value == other.Value;
		}

		public override bool Equals(object obj)
		{
			return obj is Frame other && Equals(other);
		}

		public override int GetHashCode()
		{
			return Value.GetHashCode();
		}
	}

#if MemoryManagerSafetyChecks
	private static readonly HashSet<MemoryBlock> RecentlyFreedBlocks = new();
#endif

	public static void ResetCache()
	{
#if MemoryManagerSafetyChecks
		RecentlyFreedBlocks.Clear();
#endif
	}

	public static void ProgressFrame()
	{
#if MemoryManagerSafetyChecks
		RecentlyFreedBlocks.Clear();
#endif
	}

	public static MemoryBlock Allocate(int size, int alignment)
	{
#if MemoryManagerSafetyChecks
		if (size < 0)
			throw new Exception($"Negative Size isn't supported: '{size}'");
		if (alignment < 0)
			throw new Exception($"Negative Alignment isn't supported: '{alignment}'");
		if (!IsPow2(alignment))
			throw new Exception("Alignment must be a power of 2.");
#endif

		if (size <= 0)
			return new MemoryBlock();

		var ptr = Alloc(size);
		var block = new MemoryBlock
		{
			Size = size,
			Alignment = alignment,
			Ptr = ptr
		};

		return block;
	}

	public static void Deallocate(MemoryBlock memoryBlock)
	{
#if MemoryManagerSafetyChecks
		if (memoryBlock.Size < 0)
			throw new Exception("Attempting to Free Memory Block with Negative size.");

		if (RecentlyFreedBlocks.Contains(memoryBlock))
		{
			throw new Exception("Attempting to Double-Free memory block.");
		}
#endif

		if (memoryBlock.Ptr == null)
			return;

		Free(memoryBlock.Ptr);
		RecentlyFreedBlocks.Add(memoryBlock);
	}

	public static bool TryExpand(MemoryBlock memoryBlock, int newSize)
	{
		throw new NotImplementedException();
	}

	/// <summary>
	/// https://stackoverflow.com/questions/600293/how-to-check-if-a-number-is-a-power-of-2
	/// </summary>
	/// <param name="x"></param>
	/// <returns></returns>
	public static bool IsPow2(int x)
	{
		return (x & (x - 1)) == 0;
	}

	private static void* Alloc(int size)
	{
		return Marshal.AllocHGlobal(size).ToPointer();
	}

	private static void Free(void* ptr)
	{
		Marshal.FreeHGlobal(new IntPtr(ptr));
	}
}