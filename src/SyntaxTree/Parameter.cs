﻿using Terumi.Tokens;

namespace Terumi.SyntaxTree
{
	public class Parameter
	{
		public Parameter(ParameterType type, IdentifierToken name)
		{
			Type = type;
			Name = name;
		}

		public ParameterType Type { get; }
		public IdentifierToken Name { get; }
	}
}