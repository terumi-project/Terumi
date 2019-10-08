using System;
using System.Collections.Generic;
using System.Text;
using Terumi.Ast;
using Terumi.SyntaxTree;

namespace Terumi.Binder
{
	public class ClassMethodBinder
	{
		private readonly MethodDefinition _methodDefinition;

		public ClassMethodBinder(MethodDefinition methodDefinition)
		{
			_methodDefinition = methodDefinition;
		}

		public Method Bind(CodeBody? codeBody)
		{
			if (codeBody == null)
			{

			}
		}
	}
}
