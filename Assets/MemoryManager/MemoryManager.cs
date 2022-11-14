using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Memory
{
	public enum Allocation
	{
		// 1 Frame lifetime
		Temp,

		// 4 Frames lifetime
		TempJob,

		// Unlimited lifetime
		Persistent,
	}

	public unsafe struct MemoryBlock : IEquatable<MemoryBlock>
	{
		public void* Ptr;
		public int Size;
		public int Alignment;
		public Allocation Allocation;
		public int FrameAllocated;

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

	public static unsafe class MemoryManager
	{
		private const int AlignmentStoreSize = 2;

#if MemoryManagerSafetyChecks
		private static readonly HashSet<MemoryBlock> TempAllocations = new();
		private static readonly HashSet<MemoryBlock> TempJobAllocations = new();
		private static int CurrentFrame;
#endif

		public static void ResetCache()
		{
#if MemoryManagerSafetyChecks
			TempAllocations.Clear();
			TempJobAllocations.Clear();
			CurrentFrame = default;
#endif
		}

		public static void Init()
		{
#if MemoryManagerSafetyChecks
			ProgressFrame(0);
#endif
		}

		public static void ProgressFrame(int frame)
		{
#if MemoryManagerSafetyChecks
			CurrentFrame = frame;
#endif
		}

#if MemoryManagerSafetyChecks
		// This method needs to be called every frame.
		public static void CheckForMemoryUsagePolicy()
		{
			foreach (var tempAlloc in TempAllocations)
			{
				if (tempAlloc.FrameAllocated != CurrentFrame)
				{
					throw new Exception($"Temp Memory Leak: {tempAlloc}");
				}
			}

			foreach (var tempJobAlloc in TempJobAllocations)
			{
				// TODO: Check for TempJob allocations
			}
		}
#endif

		private static string PtrToString(void* ptr)
		{
			return new IntPtr(ptr).ToString();
		}

		public static MemoryBlock Allocate(int size, int alignment, Allocation allocation)
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
				Ptr = ptr,
				FrameAllocated = CurrentFrame,
				Allocation = allocation,
			};

#if MemoryManagerSafetyChecks
			switch (allocation)
			{
				case Allocation.Temp:
					TempAllocations.Add(block);
					break;
				case Allocation.TempJob:
					TempJobAllocations.Add(block);
					break;
				case Allocation.Persistent:
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(allocation), allocation, null);
			}
#endif
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
#endif

			if (memoryBlock.Ptr == null)
				return;

			var originalPtr = GetOriginalPtrFromUserPtr(memoryBlock.Ptr);
			Free(originalPtr);

#if MemoryManagerSafetyChecks
			switch (memoryBlock.Allocation)
			{
				case Allocation.Temp:
					TempAllocations.Remove(memoryBlock);
					break;
				case Allocation.TempJob:
					TempJobAllocations.Remove(memoryBlock);
					break;
				case Allocation.Persistent:
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
#endif
		}

		public static MemoryBlock Reallocate(MemoryBlock memoryBlock, int newSize)
		{
			var originalPtr = GetOriginalPtrFromUserPtr(memoryBlock.Ptr);
			var requiredSize = GetRequiredSize(newSize, memoryBlock.Alignment);
			var reallocPtr = Realloc(originalPtr, requiredSize);
			var ptr = HandleOffsetAndAlignmentAfterMalloc(reallocPtr, memoryBlock.Alignment);

			return new MemoryBlock
			{
				Alignment = memoryBlock.Alignment,
				Allocation = memoryBlock.Allocation,
				Ptr = ptr,
				Size = newSize,
				FrameAllocated = CurrentFrame,
			};
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

		private static void* Realloc(void* ptr, int newSize)
		{
			return Marshal.ReAllocHGlobal(new IntPtr(ptr), new IntPtr(newSize)).ToPointer();
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