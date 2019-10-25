using System.Numerics;

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
			Number = compilerFunctionId;
		}

		public CodeLine(BigInteger labelId, bool isGoto, bool isDecl)
		{
			Number = labelId;

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

		public CodeLine(CodeExpression variable, CodeExpression expression)
		{
			IsSetLine = true;
			Variable = variable;
			Expression = expression;
		}

		public bool IsSetLine { get; }
		public bool IsCompilerFunctionCall { get; }
		public CodeExpression Variable { get; }
		public CodeExpression Expression { get; }
		public bool IsLabel { get; }
		public BigInteger Number { get; }
		public bool IsGoto { get; }
		public bool IsCall { get; }
		public bool IsPop { get; }
	}
}