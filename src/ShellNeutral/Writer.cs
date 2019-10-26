using System.Collections.Generic;
using System.Numerics;

namespace Terumi.ShellNeutral
{
	public class Writer
	{
		public List<CodeLine> Code { get; } = new List<CodeLine>();

		public Writer WriteLine(CodeLine line)
		{
			Code.Add(line);
			return this;
		}

		public Writer Place(BigInteger label)
			=> WriteLine(new CodeLine(label, false, true));

		public Writer Goto(BigInteger label)
			=> WriteLine(new CodeLine(label, true, false));

		public Writer CallLabel(BigInteger label)
			=> WriteLine(new CodeLine(label, false, false));

		public Writer CallCompiler(string compilerFunctionId)
			=> WriteLine(new CodeLine(compilerFunctionId));

		public Writer Set(CodeExpression variable, CodeExpression expression)
			=> WriteLine(new CodeLine(variable, expression));

		public Writer Pop()
			=> WriteLine(CodeLine.Pop);
	}
}