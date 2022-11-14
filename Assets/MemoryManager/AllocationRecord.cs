using System;

namespace Memory
{
	public struct AllocationRecord : IEquatable<AllocationRecord>
	{
		public MemoryAddress MemoryAddress;
		public Allocation Allocation;
		public int Frame;

		public bool Equals(AllocationRecord other)
		{
			return MemoryAddress.Equals(other.MemoryAddress) && Allocation == other.Allocation && Frame == other.Frame;
		}

		public override bool Equals(object obj)
		{
			return obj is AllocationRecord other && Equals(other);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(MemoryAddress, (int)Allocation, Frame);
		}
	}
}