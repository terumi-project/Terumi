using System;
using System.Collections.Generic;
using System.Text;

namespace Terumi.Ast.Code
{
	public class CodeBlock
	{
		public CodeBlock(IReadOnlyCollection<CodeStatement> statements)
		{
			Statements = statements;
		}

		public IReadOnlyCollection<CodeStatement> Statements { get; }
	}
}
