using System.Collections.Generic;

namespace Terumi.Ast
{
	public class Namespace
	{
		public Namespace(IReadOnlyCollection<string> namespaceLevels)
		{
			NamespaceLevels = namespaceLevels;
		}

		public IReadOnlyCollection<string> NamespaceLevels { get; }
	}
}