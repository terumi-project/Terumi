using Terumi.Ast.Expressions;
using Terumi.Tokens;

namespace Terumi.Tokenizer.Expressions
{
	public class ExpressionPattern : IPattern<Expression>
	{
		private readonly IPattern<MethodCall> _methodCallPattern;
		private readonly IPattern<ReturnExpression> _returnPattern;

		public ExpressionPattern
		(
			IPattern<MethodCall> methodCallPattern,
			IPattern<ReturnExpression> returnPattern
		)
		{
			_methodCallPattern = methodCallPattern;
			_returnPattern = returnPattern;
		}

		public bool TryParse(ReaderFork<Token> source, out Expression item)
		{
			using var fork1 = source.Fork();

			if (_methodCallPattern.TryParse(fork1, out var methodCall))
			{
				fork1.Commit = true;
				item = methodCall;
				return true;
			}

			using var fork2 = source.Fork();

			if (_returnPattern.TryParse(fork2, out var returnExpression))
			{
				fork2.Commit = true;
				item = returnExpression;
				return true;
			}

			item = default;
			return false;
		}
	}
}
