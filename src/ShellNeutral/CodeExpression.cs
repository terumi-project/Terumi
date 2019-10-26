using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Terumi.ShellNeutral
{
	public class CodeExpression
	{
		public CodeExpression(IEnumerable<CodeExpression> expressions)
		{
			IsConcatenationExpression = true;
			Expressions = expressions.ToArray();
		}

		public CodeExpression(CodeExpression[] expressions)
		{
			IsConcatenationExpression = true;
			Expressions = expressions;
		}

		public CodeExpression(string value)
		{
			IsString = true;
			StringValue = value;
		}

		public CodeExpression(BigInteger value)
		{
			IsNumber = true;
			NumberValue = value;
		}

		public CodeExpression(CodeExpression variableName)
		{
			IsVariable = true;
			Variable = variableName;
		}

		public bool IsConcatenationExpression { get; }
		public CodeExpression Variable { get; }
		public CodeExpression[] Expressions { get; }
		public bool IsString { get; }
		public string StringValue { get; }
		public bool IsVariable { get; }
		public bool IsNumber { get; }
		public BigInteger NumberValue { get; }
	}
}