using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Memory
{
	public static unsafe class MemoryManager
	{
		public enum Allocation
		{
			Temp,
			TempJob,
			Persistent,
		}

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

		private const int AlignmentStoreSize = 2;

#if MemoryManagerSafetyChecks
		private static readonly HashSet<MemoryBlock> RecentlyFreedBlocks = new();
		private static readonly Dictionary<Frame, MemoryBlock> AllocatedBlockByFrame = new();
		private static Frame CurrentFrame;
#endif

		public static void ResetCache()
		{
#if MemoryManagerSafetyChecks
			RecentlyFreedBlocks.Clear();
			AllocatedBlockByFrame.Clear();
			CurrentFrame = default;
#endif
		}

		public static void ProgressFrame(Frame frame)
		{
#if MemoryManagerSafetyChecks
			RecentlyFreedBlocks.Clear();
			AllocatedBlockByFrame.Clear();
			CurrentFrame = frame;
#endif
		}

		private static string PtrToString(void* ptr)
		{
			return new IntPtr(ptr).ToString();
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

			// All memory is at least 8 bytes aligned, at most uint16.max
			alignment = Math.Clamp(alignment, 8, UInt16.MaxValue);

			// Allocate extra bytes to store the offset and alignment
			var alloc = Alloc(GetRequiredSize(size, alignment));
			var ptr = HandleOffsetAndAlignmentAfterMalloc(alloc, alignment);
			var block = new MemoryBlock
			{
				Size = size,
				Alignment = alignment,
				Ptr = ptr
			};

			return block;
		}

		private static int GetRequiredSize(int requestedSize, int alignment)
		{
			return requestedSize + AlignmentStoreSize + alignment - 1;
		}

		private static void* HandleOffsetAndAlignmentAfterMalloc(void* memory, int alignment)
		{
			// Align memory address + 2, so we can use the first 2 bytes for storing the offset
			var addressToAlign = (byte*)memory + AlignmentStoreSize;
			var address = (nuint)new IntPtr(addressToAlign).ToInt64();
			var ptr = (void*)MemoryUtil.AlignUp(address, alignment);

			// Store the offset
			var offset = (ushort)((byte*)ptr - (byte*)memory);
			*((ushort*)ptr - 1) = offset;

			return ptr;
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

			var originalPtr = GetOriginalPtrFromUserPtr(memoryBlock.Ptr);
			Free(originalPtr);
			RecentlyFreedBlocks.Add(memoryBlock);
		}

		public static bool TryExpand(MemoryBlock memoryBlock, int newSize)
		{
			throw new NotImplementedException();
		}

		private static void* GetOriginalPtrFromUserPtr(void* ptr)
		{
			var offset = *((short*)ptr - 1);
			var originalPtr = (void*)((byte*)ptr - offset);
			return originalPtr;
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
}