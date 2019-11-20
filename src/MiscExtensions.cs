using System;

namespace Terumi
{
	public static class MiscExtensions
	{
		public static string[] ExcludeLast(this string[] array)
		{
			var newArray = new string[array.Length - 1];

			Array.Copy(array, 0, newArray, 0, newArray.Length);

			return newArray;
		}
	}
}