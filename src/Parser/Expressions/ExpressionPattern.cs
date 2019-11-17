using System;
using System.Numerics;

using Terumi.SyntaxTree.Expressions;

namespace Terumi.Parser.Expressions
{
	public class ExpressionPattern : IPattern<Expression>
	{
		private readonly IPattern<MethodCall> _methodCallPattern;
		private readonly IPattern<ReturnExpression> _returnPattern;
		private readonly IPattern<AccessExpression> _accessPattern;
		private readonly IPattern<Terumi.Ast.ConstantLiteralExpression<BigInteger>> _numericPattern;
		private readonly IPattern<Terumi.Ast.ConstantLiteralExpression<string>> _stringPattern;
		private readonly IPattern<ThisExpression> _thisPattern;
		private readonly IPattern<ReferenceExpression> _referencePattern;
		private readonly IPattern<Terumi.Ast.ConstantLiteralExpression<bool>> _booleanPattern;
		private readonly IPattern<VariableExpression> _variablePattern;
		private readonly IPattern<IfExpression> _ifPattern;
		private readonly Func<ExpressionPattern, Expression, IPattern<ComparisonExpression>> _comparisonPattern;

		public ExpressionPattern
		(
			IPattern<MethodCall> methodCallPattern,
			IPattern<ReturnExpression> returnPattern,
			IPattern<AccessExpression> accessPattern,
			IPattern<Terumi.Ast.ConstantLiteralExpression<BigInteger>> numericPattern,
			IPattern<Terumi.Ast.ConstantLiteralExpression<string>> stringPattern,
			IPattern<ThisExpression> thisPattern,
			IPattern<ReferenceExpression> referencePattern,
			IPattern<Terumi.Ast.ConstantLiteralExpression<bool>> booleanPattern,
			IPattern<VariableExpression> variablePattern,
			IPattern<IfExpression> ifPattern,
			Func<ExpressionPattern, Expression, IPattern<ComparisonExpression>> comparisonPattern
		)
		{
			_methodCallPattern = methodCallPattern;
			_returnPattern = returnPattern;
			_accessPattern = accessPattern;
			_numericPattern = numericPattern;
			_stringPattern = stringPattern;
			_thisPattern = thisPattern;
			_referencePattern = referencePattern;
			_booleanPattern = booleanPattern;
			_variablePattern = variablePattern;
			_ifPattern = ifPattern;
			_comparisonPattern = comparisonPattern;
		}

		public int TryParse(TokenStream stream, ref Expression item)
		{
			// first try to parse programming constructs/patterns
			if (TryParse(ref stream, _returnPattern, ref item)
				|| TryParse(ref stream, _ifPattern, ref item))
			{
				return stream;
			}
			// then parse out things that can evaluate to something
			else if (TryParse(ref stream, _methodCallPattern, ref item)
				|| TryParse(ref stream, _numericPattern, ref item)
				|| TryParse(ref stream, _stringPattern, ref item)
				|| TryParse(ref stream, _thisPattern, ref item)
				|| TryParse(ref stream, _booleanPattern, ref item)
				|| TryParse(ref stream, _variablePattern, ref item)
				|| TryParse(ref stream, _referencePattern, ref item))
			{
				var comparison = _comparisonPattern(this, item);

				// don't really care if it fails
				TryParse(ref stream, comparison, ref item);

				return stream;
			}

			return 0;
		}

		private bool TryParse<TExpression>
		(
			ref TokenStream stream,
			IPattern<TExpression> pattern,
			ref Expression expression
		)
			where TExpression : Expression
		{
			if (stream.TryParse(pattern, out var tExpr))
			{
				expression = TryDeeperExpressionParse(ref stream, tExpr);
				return true;
			}

			return false;
		}

		// || TryParse(source, _accessPattern, out item);

		// this will give me a structure that looks like this
		//
		// expr().expr().expr().expr().expr()
		// |___________|      |      |      |
		//       |____________|      |      |
		//              |____________|      |
		//                     |____________|
		//
		// which will be elegant for figuring out
		// stuff about type safety
		private Expression TryDeeperExpressionParse(ref TokenStream stream, Expression start)
		{
			var totalExpression = start;

			while (stream.TryParse(_accessPattern, out var expr))
			{
				var expression = expr.Access;

				totalExpression = new AccessExpression
				{
					// Access = expression
					// Predecessor = totalExpression

					// because we parse "in reverse" (expression -> deeper -> access expression -> ...
					//     so the results come in reverse)
					// these two are reversed
					Predecessor = expression,
					Access = totalExpression
				};
			}

			return totalExpression;
		}
	}
}