using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Terumi
{
	public struct PackageLevel : IEquatable<PackageLevel>
	{
		public PackageLevel(List<string> levels)
			: this(levels.ToArray())
		{
		}

		public PackageLevel(string[] levels)
			: this(new ReadOnlyMemory<string>(levels))
		{
		}

		public PackageLevel(ReadOnlyMemory<string> levels)
			=> Levels = levels;

		public ReadOnlyMemory<string> Levels { get; }

		public string this[int index] => Levels.Span[index];

		public override bool Equals(object obj) => obj is PackageLevel level && Equals(level);
		public override int GetHashCode() => HashCode.Combine(Levels);
		public bool Equals([AllowNull] PackageLevel other)
		{
			if (other.Levels.Length != Levels.Length) return false;

			var a = Levels.Span;
			var b = other.Levels.Span;

			for(var i = 0; i < Levels.Length; i++)
			{
				if (!a[i].Equals(b[i])) return false;
			}

			return true;
		}

		public static bool operator ==(PackageLevel left, PackageLevel right) => left.Equals(right);
		public static bool operator !=(PackageLevel left, PackageLevel right) => !(left == right);

		public static implicit operator PackageLevel(List<string> levels) => new PackageLevel(levels);
		public static implicit operator PackageLevel(string[] levels) => new PackageLevel(levels);
		public static implicit operator PackageLevel(Memory<string> levels) => new PackageLevel(levels);
		public static implicit operator PackageLevel(ReadOnlyMemory<string> levels) => new PackageLevel(levels);
	}
}
