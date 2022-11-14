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
		throw new NotImplementedException();
	}

	public static void Deallocate(MemoryBlock memoryBlock)
	{
		throw new NotImplementedException();
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