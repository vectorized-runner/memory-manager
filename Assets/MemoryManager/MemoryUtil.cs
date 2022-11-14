namespace Memory
{
	public class MemoryUtil
	{
		public static int AlignUp(int address, int alignment)
		{
			return address & ~(alignment - 1);
		}
	}
}