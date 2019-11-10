using Terumi.SyntaxTree.Expressions;
using Terumi.Tokens;

namespace Terumi.Parser.Expressions
{
	public class ReferenceExpressionPattern : INewPattern<ReferenceExpression>
	{
		public int TryParse(TokenStream stream, ref ReferenceExpression item)
		{
			if (!stream.NextNoWhitespace<IdentifierToken>(out var identifier)) return 0;

			item = new ReferenceExpression(identifier);
			return stream;
		}
	}
}