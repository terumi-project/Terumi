using Terumi.SyntaxTree.Expressions;
using Terumi.Tokens;

namespace Terumi.Parser.Expressions
{
	public class ExpressionPattern : IPattern<Expression>
	{
		private readonly IPattern<MethodCall> _methodCallPattern;
		private readonly IPattern<ReturnExpression> _returnPattern;
		private readonly IPattern<AccessExpression> _accessPattern;
		private readonly IPattern<NumericLiteralExpression> _numericPattern;

		public ExpressionPattern
		(
			IPattern<MethodCall> methodCallPattern,
			IPattern<ReturnExpression> returnPattern,
			IPattern<AccessExpression> accessPattern,
			IPattern<NumericLiteralExpression> numericPattern
		)
		{
			_methodCallPattern = methodCallPattern;
			_returnPattern = returnPattern;
			_accessPattern = accessPattern;
			_numericPattern = numericPattern;
		}

		public bool TryParse(ReaderFork<Token> source, out Expression item)
			=> TryParse(source, _methodCallPattern, out item)
			|| TryParse(source, _returnPattern, out item)
			|| TryParse(source, _numericPattern, out item);
			// || TryParse(source, _accessPattern, out item);

		private Expression TryDeeperExpressionParse
		(
			ReaderFork<Token> source,
			Expression start
		)
		{
			if (TryParseToTExpression(source, _accessPattern, out var accessExpression))
			{
				accessExpression.Predecessor = start;
				return TryDeeperExpressionParse(source, accessExpression);
			}

			return start;
		}

		private bool TryParse<TExpresion>
		(
			ReaderFork<Token> source,
			IPattern<TExpresion> pattern,
			out Expression expression
		)
			where TExpresion : Expression
		{
			if (TryParseToTExpression(source, pattern, out var tExpr))
			{
				expression = TryDeeperExpressionParse(source, tExpr);
				return true;
			}

			expression = default;
			return false;
		}

		private static bool TryParseToTExpression<TExpression>
		(
			ReaderFork<Token> source,
			IPattern<TExpression> pattern,
			out TExpression expression
		)
			where TExpression : Expression
		{
			using var fork = source.Fork();

			if (pattern.TryParse(fork, out expression))
			{
				fork.Commit = true;
				return true;
			}

			expression = default;
			return false;
		}
	}
}
