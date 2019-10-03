using System.Collections.Generic;

namespace Terumi.Ast
{
	public class Contract : CompilationNode, ICompilationType
	{
		public Contract(string name, IReadOnlyCollection<MemberDefinition> members, Namespace @namespace)
		{
			Namespace = @namespace;
			Name = name;
			Members = members;
		}

		public Namespace Namespace { get; }
		public string Name { get; }
		public IReadOnlyCollection<MemberDefinition> Members { get; }
	}
}