namespace RawRabbit.Common
{
	public class Truncator
	{
		public static void Truncate(ref string name)
		{
			if (name.Length > 254)
			{
				name = string.Concat("...", name.Substring(name.Length - 250));
			}
		}
	}
}
