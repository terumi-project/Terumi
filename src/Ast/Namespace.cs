using System.Collections.Generic;
using System.Linq;

namespace Terumi.Ast
{
	public class Namespace
	{
		public Namespace(IReadOnlyCollection<string> namespaceLevels)
		{
			NamespaceLevels = namespaceLevels;
		}

		public IReadOnlyCollection<string> NamespaceLevels { get; }

		public override bool Equals(object obj)
		{
			return obj is Namespace @namespace
				&& @namespace.NamespaceLevels.SequenceEqual(NamespaceLevels);
		}

		public override string ToString()
			=> NamespaceLevels.Aggregate((a, b) => $"{a}.{b}");
	}
}