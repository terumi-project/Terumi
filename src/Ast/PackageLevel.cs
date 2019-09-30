using System.Linq;

namespace Terumi.Ast
{
	public enum PackageAction
	{
		Namespace,
		Using
	}

	public class PackageLevel : CompilerUnitItem, System.IEquatable<PackageLevel>
	{
		public PackageLevel(PackageAction action, string[] levels)
		{
			Action = action;
			Levels = levels;
		}

		public PackageAction Action { get; }
		public string[] Levels { get; }

		public bool Equals(PackageLevel other)
			=> Action == other.Action
			&& Levels.SequenceEqual(Levels);
	}
}