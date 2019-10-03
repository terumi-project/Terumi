using System.Collections.Generic;

namespace Terumi.Ast
{
	public class Class : CompilationNode, ICompilationType
	{
		public Class(string name, IReadOnlyCollection<Member> members, Namespace @namespace)
		{
			Namespace = @namespace;
			Name = name;
			Members = members;
		}

		public Namespace Namespace { get; }
		public string Name { get; }
		public IReadOnlyCollection<Member> Members { get; }
	}
}