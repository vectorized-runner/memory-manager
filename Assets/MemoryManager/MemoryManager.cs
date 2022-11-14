using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

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

		public static int AlignUp(int value, int alignment)
		{
			// TODO: Implement
			throw new NotImplementedException();
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

			const int offsetSize = 2;
			// Allocate extra bytes to store the offset and alignment
			var maxExtraBytes = offsetSize + alignment - 1;
			var allocSize = size + maxExtraBytes;
			var alloc = Alloc(allocSize);

			// Align the pointer
			var ptr = MemoryUtil.AlignUp((nint)((int*)alloc + offsetSize), alignment);
			// Store the offset
			*((UInt16*)ptr - 1) = (UInt16)((uint*)ptr - (uint*)alloc);
			

			// var ptr = (void*)(alloc + )

			var block = new MemoryBlock
			{
				Size = size,
				Alignment = alignment,
				Ptr = alloc
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

			UInt16 offset = *((UInt16*)memoryBlock.Ptr - 1);
			var p = (void*)((UInt16*)memoryBlock.Ptr - offset);

			Free(p);
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
}