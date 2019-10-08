using System;
using System.Collections.Generic;
using System.Text;
using Terumi.Ast;
using Terumi.Ast.Code;
using Terumi.SyntaxTree.Expressions;

namespace Terumi.Binder
{
	public class ClassMethodBinder
	{
		private readonly SyntaxTree.TypeDefinition _typeDefinition;
		private readonly MethodDefinition _methodDefinition;

		public ClassMethodBinder(SyntaxTree.TypeDefinition typeDefinition, MethodDefinition methodDefinition)
		{
			_typeDefinition = typeDefinition;
			_methodDefinition = methodDefinition;
		}

		public Method Bind(SyntaxTree.CodeBody? codeBody)
		{
			if (codeBody == null)
			{
				return new Method(_methodDefinition, new CodeBlock(Array.Empty<CodeStatement>()));
			}

			var statements = new List<CodeStatement>();

			var translator = new ExpressionBinder(_methodDefinition, _typeDefinition);

			foreach(var expression in codeBody.Expressions)
			{
				statements.Add(translator.Bind(expression));
			}

			return new Method(_methodDefinition, new CodeBlock(statements.AsReadOnly()));
		}
	}
}
