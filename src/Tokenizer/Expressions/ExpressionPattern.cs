using Terumi.Ast.Expressions;
using Terumi.Tokens;

namespace Terumi.Tokenizer.Expressions
{
	public class ExpressionPattern : IPattern<Expression>
	{
		private readonly IPattern<MethodCall> _methodCallPattern;

		public ExpressionPattern
		(
			IPattern<MethodCall> methodCallPattern
		)
		{
			_methodCallPattern = methodCallPattern;
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
			item = default;
			return false;
		}
	}
}
