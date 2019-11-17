using System;
using System.Collections.Generic;
using System.Text;
using Terumi.Targets;

namespace Terumi.SyntaxTree.Expressions
{
	public enum ComparisonOperator
	{
		Equals,
		NotEquals,
		LessThan,
		GreaterThan,
		LessThanOrEqualTo,
		GreaterThanOrEqualTo
	}

	public static class ComparisonHelpers
	{
		public static CompilerOperators ToCompilerOp(this ComparisonOperator comparisonOp)
			=> comparisonOp switch
			{
				ComparisonOperator.Equals => CompilerOperators.Equals,
				ComparisonOperator.NotEquals => CompilerOperators.NotEquals,
				ComparisonOperator.LessThan => CompilerOperators.LessThan,
				ComparisonOperator.GreaterThan => CompilerOperators.GreaterThan,
				ComparisonOperator.LessThanOrEqualTo => CompilerOperators.LessThanOrEqualTo,
				ComparisonOperator.GreaterThanOrEqualTo => CompilerOperators.GreaterThanOrEqualTo
			};
	}

	public class ComparisonExpression : Expression
	{
		public ComparisonExpression(Expression left, ComparisonOperator comparison, Expression right)
		{
			Left = left;
			Comparison = comparison;
			Right = right;
		}

		public Expression Left { get; }
		public ComparisonOperator Comparison { get; }
		public Expression Right { get; }
	}
}
