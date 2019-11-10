using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Terumi
{
	public struct PackageLevel : IEquatable<PackageLevel>, IEnumerable<string>
	{
		// ctors
		public PackageLevel(List<string> levels)
			: this(levels.ToArray())
		{
		}

		public PackageLevel(string[] levels)
			: this(new ReadOnlyMemory<string>(levels))
		{
		}

		public PackageLevel(ReadOnlyMemory<string> levels)
		{
			Levels = levels;

			// a.b.c

			Length = levels.Length - 1;

			for (var i = 0; i < levels.Length; i++)
			{
				Length += i;
			}
		}

		// props
		public ReadOnlyMemory<string> Levels { get; }

		public int Length { get; }

		// indexer
		public string this[int index] => Levels.Span[index];

		// enumerators
		public PackageLevelEnumerator GetEnumerator() => new PackageLevelEnumerator(Levels);

		IEnumerator<string> IEnumerable<string>.GetEnumerator() => GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		// object methods
		public override bool Equals(object obj) => obj is PackageLevel level && Equals(level);

		public override int GetHashCode() => HashCode.Combine(Levels);

		public override string ToString()
		{
			var strb = new StringBuilder(Length);

			for (var i = 0; i < Levels.Length; i++)
			{
				strb.Append(Levels.Span[i]);

				if (i < Levels.Length - 1)
				{
					strb.Append('.');
				}
			}

			return strb.ToString();
		}

		// not auto generated - value based equality method
		public bool Equals([AllowNull] PackageLevel other)
		{
			if (other.Levels.Length != Levels.Length) return false;

			var a = Levels.Span;
			var b = other.Levels.Span;

			for (var i = 0; i < Levels.Length; i++)
			{
				if (!a[i].Equals(b[i])) return false;
			}

			return true;
		}

		// operators
		public static bool operator ==(PackageLevel left, PackageLevel right) => left.Equals(right);

		public static bool operator !=(PackageLevel left, PackageLevel right) => !(left == right);

		// implicit operators
		public static implicit operator PackageLevel(List<string> levels) => new PackageLevel(levels);

		public static implicit operator PackageLevel(string[] levels) => new PackageLevel(levels);

		public static implicit operator PackageLevel(Memory<string> levels) => new PackageLevel(levels);

		public static implicit operator PackageLevel(ReadOnlyMemory<string> levels) => new PackageLevel(levels);

		public static implicit operator string[](PackageLevel levels) => levels.Levels.ToArray();

		// enumerator for enumerability
		public struct PackageLevelEnumerator : IEnumerator<string>
		{
			private ReadOnlyMemory<string> _levels;
			private int _i;

			public PackageLevelEnumerator(ReadOnlyMemory<string> levels)
			{
				_levels = levels;
				_i = 0;
			}

			public string Current => _levels.Span[_i];
			object IEnumerator.Current => Current;

			public bool MoveNext() => ++_i < _levels.Length;

			public void Reset() => _i = 0;

			public void Dispose()
			{
			}
		}
	}
}