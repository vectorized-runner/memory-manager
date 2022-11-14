using System;
using System.Runtime.InteropServices;

public static unsafe class MemoryManager
{
	public struct MemoryBlock
	{
		public void* Ptr;
		public int Size;
		public int Alignment;
	}

	public static MemoryBlock Allocate(int size, int alignment)
	{
#if MemoryManagerSafetyChecks
		if (size < 0)
			throw new Exception($"Negative Size isn't supported: '{size}'");
		if (alignment < 0)
			throw new Exception($"Negative Alignment isn't supported: '{alignment}'");
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
#endif
		
		if (memoryBlock.Ptr == null)
			return;

		Free(memoryBlock.Ptr);
	}

	public static bool TryExpand(MemoryBlock memoryBlock, int newSize)
	{
		throw new NotImplementedException();
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