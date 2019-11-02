using System.Collections.Generic;

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
		private readonly IPattern<StringLiteralExpression> _stringPattern;
		private readonly IPattern<ThisExpression> _thisPattern;
		private readonly IPattern<ReferenceExpression> _referencePattern;

		public ExpressionPattern
		(
			IPattern<MethodCall> methodCallPattern,
			IPattern<ReturnExpression> returnPattern,
			IPattern<AccessExpression> accessPattern,
			IPattern<NumericLiteralExpression> numericPattern,
			IPattern<StringLiteralExpression> stringPattern,
			IPattern<ThisExpression> thisPattern,
			IPattern<ReferenceExpression> referencePattern
		)
		{
			_methodCallPattern = methodCallPattern;
			_returnPattern = returnPattern;
			_accessPattern = accessPattern;
			_numericPattern = numericPattern;
			_stringPattern = stringPattern;
			_thisPattern = thisPattern;
			_referencePattern = referencePattern;
		}

		public bool TryParse(ReaderFork<Token> source, out Expression item)
			=> TryParse(source, _methodCallPattern, out item)
			|| TryParse(source, _returnPattern, out item)
			|| TryParse(source, _numericPattern, out item)
			|| TryParse(source, _stringPattern, out item)
			|| TryParse(source, _thisPattern, out item)
			|| TryParse(source, _referencePattern, out item);

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
		private Expression TryDeeperExpressionParse
		(
			ReaderFork<Token> source,
			Expression start
		)
		{
			var totalExpression = start;

			foreach (var expression in ContinueParseDeeper(source))
			{
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

		private IEnumerable<Expression> ContinueParseDeeper(ReaderFork<Token> source)
		{
			while (TryParseToTExpression(source, _accessPattern, out var expr))
			{
				yield return expr.Access;
			}
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