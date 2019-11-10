using System.Collections.Generic;

namespace Terumi
{
	public static class EmptyList<T>
	{
		public static List<T> Instance { get; } = new List<T>(0);
	}
}