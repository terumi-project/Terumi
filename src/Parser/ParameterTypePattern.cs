using Terumi.SyntaxTree;
using Terumi.Tokens;

namespace Terumi.Parser
{
	public class ParameterTypePattern : IPattern<ParameterType>
	{
		public int TryParse(TokenStream stream, ref ParameterType item)
		{
			if (!stream.NextNoWhitespace<IdentifierToken>(out var identifier)) return 0;

			var hasBrackets = stream.Parse(HasBrackets);
			item = new ParameterType(identifier, hasBrackets);

			while (stream.Parse(HasBrackets))
			{
				item = new ParameterType(item, true);
			}

			return stream;
		}

		private static int HasBrackets(TokenStream stream)
		{
			if (stream.NextChar('[')) return 0;
			if (stream.NextChar(']')) return 0;
			return stream;
		}
	}
}