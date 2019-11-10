using Terumi.SyntaxTree.Expressions;
using Terumi.Tokens;

namespace Terumi.Parser.Expressions
{
	public class MethodCallPattern : INewPattern<MethodCall>
	{
		private readonly INewPattern<MethodCallParameterGroup> _pattern;

		public MethodCallPattern(INewPattern<MethodCallParameterGroup> pattern)
			=> _pattern = pattern;

		public int TryParse(TokenStream stream, ref MethodCall item)
		{
			var isCompilerCall = stream.NextChar('@');

			if (!stream.NextNoWhitespace<IdentifierToken>(out var identifier)) return 0;
			if (identifier.IdentifierCase != IdentifierCase.SnakeCase) return 0;
			if (!stream.NextChar('(')) return 0;

			if (!stream.TryParse(_pattern, out var methodCallParameterGroup))
			{
				Log.Error($"While parsing method call, attempted to parse method call parameter group but failed {stream.TopInfo}");
				return 0;
			}

			if (!stream.NextChar(')'))
			{
				Log.Error($"No closing paranthesis on method call {stream.TopInfo}");
				return 0;
			}

			item = new MethodCall(isCompilerCall, identifier, methodCallParameterGroup);
			return stream;
		}
	}
}