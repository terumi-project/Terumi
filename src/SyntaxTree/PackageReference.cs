using System;
using System.Linq;

namespace Terumi.SyntaxTree
{
	public enum PackageAction
	{
		Namespace,
		Using
	}

	public class PackageReference : CompilerUnitItem, IEquatable<PackageReference>
	{
		public PackageReference(PackageAction action, string[] levels)
		{
			Action = action;
			Levels = levels;
		}

		public PackageAction Action { get; }
		public PackageLevel Levels { get; }

		public bool Equals(PackageReference other)
			=> Action == other.Action
			&& LevelEquals(other);

		public static bool operator ==(PackageReference a, PackageReference b)
			=> a.Equals(b);

		public static bool operator !=(PackageReference a, PackageReference b)
			=> !(a == b);

		public override string ToString()
			=> Levels.Aggregate((a, b) => $"{a}.{b}");

		internal bool LevelEquals(PackageReference level)
			=> Levels.SequenceEqual(level.Levels);
	}
}