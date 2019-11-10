using Terumi.SyntaxTree;
using Terumi.SyntaxTree.Expressions;
using Terumi.Tokens;

namespace Terumi.Parser.Expressions
{
	public class VariableExpressionPattern : IPattern<VariableExpression>
	{
		private readonly IPattern<ParameterType> _parameterTypePattern;

		public VariableExpressionPattern(IPattern<ParameterType> parameterTypePattern)
			=> _parameterTypePattern = parameterTypePattern;

		public IPattern<Expression> ExpressionPattern { get; set; }

		public int TryParse(TokenStream stream, ref VariableExpression item)
		{
			ParameterType type = default;
			stream.TryParse(_parameterTypePattern, out type);
			return Parse(ref stream, type, ref item);
		}

		private int Parse(ref TokenStream stream, ParameterType type, ref VariableExpression item)
		{
			if (!stream.NextNoWhitespace<IdentifierToken>(out var identifier)) return 0;
			if (!stream.NextChar('=')) return 0;

			// ok, now we've assigned a variable and we should be able to deduce what we want
			// start throwing here

			if (!stream.TryParse(ExpressionPattern, out var expression))
			{
				Log.Error($"Expected valid expression when parsing variable, but didn't get one {stream.TopInfo}");
				return 0;
			}

			item = new VariableExpression(type, identifier, expression);
			return stream;
		}
	}
}