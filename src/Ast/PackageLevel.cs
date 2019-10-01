using System;
using System.Linq;

namespace Terumi.Ast
{
	public enum PackageAction
	{
		Namespace,
		Using
	}

	public class PackageLevel : CompilerUnitItem, IEquatable<PackageLevel>
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
			&& LevelEquals(other);

		public static bool operator ==(PackageLevel a, PackageLevel b)
			=> a.Equals(b);

		public static bool operator !=(PackageLevel a, PackageLevel b)
			=> !(a == b);

		public override string ToString()
			=> Levels.Aggregate((a, b) => $"{a}.{b}");

		internal bool LevelEquals(PackageLevel level)
			=> Levels.SequenceEqual(level.Levels);
	}
}