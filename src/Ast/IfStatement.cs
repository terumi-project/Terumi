using System;
using System.Collections.Generic;
using Terumi.Binder;
using Terumi.SyntaxTree.Expressions;

namespace Terumi.Ast
{
	public class IfStatement : CodeStatement, ICodeExpression
	{
		public IfStatement(ICodeExpression comparison, List<CodeStatement> expressions, List<CodeStatement> falseStatements)
		{
			if (comparison.Type != CompilerDefined.Boolean) throw new ArgumentException($"Cannot instantiate IfStatement with an expression that isn't a boolean");

			Comparison = comparison;
			Statements = expressions;
			FalseStatements = falseStatements;
		}

		public ICodeExpression Comparison { get; set; }
		public List<CodeStatement> Statements { get; }
		public List<CodeStatement> FalseStatements { get; }

		// boolean
		public IType Type => Comparison.Type;
	}
}
