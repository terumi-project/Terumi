using System;
using Terumi.SyntaxTree;
using Terumi.Tokens;

namespace Terumi.Parser
{
	public class ParameterTypePattern : INewPattern<ParameterType>
	{
		private readonly IAstNotificationReceiver _astNotificationReceiver;

		public ParameterTypePattern(IAstNotificationReceiver astNotificationReceiver)
		{
			_astNotificationReceiver = astNotificationReceiver;
		}

		public int TryParse(Span<IToken> source, ref ParameterType item)
		{
			int read;
			if (0 == (read = source.NextNoWhitespace<IdentifierToken>(out var identifier))) return 0;

			var hasBrackets = HasBrackets(source.Slice(read)).IncButCmp(ref read);
			item = new ParameterType(identifier, hasBrackets != 0);

			while (HasBrackets(source.Slice(read)).IncButCmp(ref read) != 0)
			{
				item = new ParameterType(item, true);
			}

			_astNotificationReceiver.AstCreated(source, item);
			return read;
		}

		private static int HasBrackets(Span<IToken> source)
		{
			int read;
			if (0 == (read = source.NextChar('['))) return 0;
			if (0 == source.Slice(read).NextChar(']').IncButCmp(ref read)) return 0;

			return read;
		}
	}
}