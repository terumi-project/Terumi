using System.Collections.Generic;
using System.Linq;

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

		public override string ToString()
			=> $"{ReturnType} {Name}({(Parameters.Select(x => x.CompilationTypeName).Aggregate((a, b) => $"{a}, {b}"))})";
	}
}