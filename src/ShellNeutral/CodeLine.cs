using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Terumi.ShellNeutral
{

	public class CodeLine
	{
		public static CodeLine Pop { get; } = new CodeLine();

		public CodeLine()
		{
			IsPop = true;
		}

		public CodeLine(BigInteger compilerFunctionId)
		{
			IsCompilerFunctionCall = true;
			Variable = compilerFunctionId;
		}

		public CodeLine(BigInteger labelId, bool isGoto, bool isDecl)
		{
			LabelId = labelId;

			if (isDecl)
			{
				IsLabel = true;
			}
			else
			{
				if (isGoto)
				{
					IsGoto = true;
				}
				else
				{
					IsCall = true;
				}
			}
		}

		public CodeLine(BigInteger variable, CodeExpression expression)
		{
			IsSetLine = true;
			Variable = variable;
			Expression = expression;
		}

		public bool IsSetLine { get; }
		public bool IsCompilerFunctionCall { get; }
		public BigInteger Variable { get; }
		public CodeExpression Expression { get; }
		public bool IsLabel { get; }
		public BigInteger LabelId { get; }
		public bool IsGoto { get; }
		public bool IsCall { get; }
		public bool IsPop { get; }
	}
}
