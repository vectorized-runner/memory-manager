namespace Memory
{
	public class MemoryUtil
	{
		public static nuint AlignUp(nuint address, int alignment)
		{
			return (address + (nuint)alignment - 1) & ~((nuint)alignment - 1);
		}
	}
}