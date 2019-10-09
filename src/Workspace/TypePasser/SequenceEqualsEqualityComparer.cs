using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace Terumi.Workspace.TypePasser
{

	public class SequenceEqualsEqualityComparer<T> : IEqualityComparer<ICollection<T>>
	{
		public static SequenceEqualsEqualityComparer<T> Instance { get; } = new SequenceEqualsEqualityComparer<T>();

		public bool Equals([AllowNull] ICollection<T> x, [AllowNull] ICollection<T> y) => x.SequenceEqual(y);
		public int GetHashCode([DisallowNull] ICollection<T> obj) => obj.GetHashCode();
	}
}
