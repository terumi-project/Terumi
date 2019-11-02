using System;

namespace Terumi.SyntaxTree
{
	public class TypeDefinition : CompilerUnitItem
	{
		public TypeDefinition(string identifier, Method method)
		{
			Identifier = identifier;
			Method = method;
		}

		public string Identifier { get; }

		public Method Method { get; }
	}
}