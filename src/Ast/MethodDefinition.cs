using System.Collections.Generic;

namespace Terumi.Ast
{
	public class MethodDefinition : MemberDefinition
	{
		public MethodDefinition(string name, ICompilationType returnType, IReadOnlyCollection<ICompilationType> parameters)
		{
			Parameters = parameters;
			ReturnType = returnType;
			Name = name;
		}

		public IReadOnlyCollection<ICompilationType> Parameters { get; }
		public ICompilationType ReturnType { get; }
		public string Name { get; }
	}
}