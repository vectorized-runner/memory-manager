using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;


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

	public static void Deallocate(MemoryBlock memoryBlock, int alignment)
	{
		throw new NotImplementedException();
	}

	public static bool TryExpand(MemoryBlock memoryBlock, int newSize)
	{
		throw new NotImplementedException();
	}
}