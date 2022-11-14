namespace Memory
{
	public class MemoryUtil
	{
		public static nint AlignUp(nint address, int alignment)
		{
			return address & ~(alignment - 1);
		}
	}
}