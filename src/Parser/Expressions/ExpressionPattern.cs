using System.Collections.Generic;
using System.Numerics;

using Terumi.SyntaxTree.Expressions;
using Terumi.Tokens;

namespace Terumi.Parser.Expressions
{
	public class ExpressionPattern : INewPattern<Expression>
	{
		private readonly INewPattern<MethodCall> _methodCallPattern;
		private readonly INewPattern<ReturnExpression> _returnPattern;
		private readonly INewPattern<AccessExpression> _accessPattern;
		private readonly INewPattern<Terumi.Ast.ConstantLiteralExpression<BigInteger>> _numericPattern;
		private readonly INewPattern<Terumi.Ast.ConstantLiteralExpression<string>> _stringPattern;
		private readonly INewPattern<ThisExpression> _thisPattern;
		private readonly INewPattern<ReferenceExpression> _referencePattern;
		private readonly INewPattern<Terumi.Ast.ConstantLiteralExpression<bool>> _booleanPattern;
		private readonly INewPattern<VariableExpression> _variablePattern;

		public ExpressionPattern
		(
			INewPattern<MethodCall> methodCallPattern,
			INewPattern<ReturnExpression> returnPattern,
			INewPattern<AccessExpression> accessPattern,
			INewPattern<Terumi.Ast.ConstantLiteralExpression<BigInteger>> numericPattern,
			INewPattern<Terumi.Ast.ConstantLiteralExpression<string>> stringPattern,
			INewPattern<ThisExpression> thisPattern,
			INewPattern<ReferenceExpression> referencePattern,
			INewPattern<Terumi.Ast.ConstantLiteralExpression<bool>> booleanPattern,
			INewPattern<VariableExpression> variablePattern
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
		}

		public int TryParse(TokenStream stream, ref Expression item)
		{
			if (TryParse(ref stream, _methodCallPattern, ref item)
			|| TryParse(ref stream, _returnPattern, ref item)
			|| TryParse(ref stream, _numericPattern, ref item)
			|| TryParse(ref stream, _stringPattern, ref item)
			|| TryParse(ref stream, _thisPattern, ref item)
			|| TryParse(ref stream, _booleanPattern, ref item)
			|| TryParse(ref stream, _variablePattern, ref item)
			|| TryParse(ref stream, _referencePattern, ref item))
			{
				return stream;
			}

			return 0;
		}

		private bool TryParse<TExpression>
		(
			ref TokenStream stream,
			INewPattern<TExpression> pattern,
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