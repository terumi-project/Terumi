using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Terumi.ShellNeutral
{

	public class CodeExpression
	{
		public CodeExpression(IEnumerable<CodeExpression> expressions)
		{
			IsArray = true;
			Expressions = expressions.ToArray();
		}

		public CodeExpression(CodeExpression[] expressions)
		{
			IsArray = true;
			Expressions = expressions;
		}

		public CodeExpression(string value)
		{
			IsString = true;
			StringValue = value;
		}

		public CodeExpression(BigInteger value, bool isVariable)
		{
			if (isVariable)
			{
				IsVariable = true;
			}
			else
			{
				IsNumber = true;
			}

			NumberValue = value;
		}

		public bool IsArray { get; }
		public CodeExpression[] Expressions { get; }
		public bool IsString { get; }
		public string StringValue { get; }
		public bool IsVariable { get; }
		public bool IsNumber { get; }
		public BigInteger NumberValue { get; }
	}
}
