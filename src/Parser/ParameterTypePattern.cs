﻿using Terumi.SyntaxTree;
using Terumi.Tokens;

namespace Terumi.Parser
{
	public class ParameterTypePattern : IPattern<ParameterType>
	{
		private readonly IAstNotificationReceiver _astNotificationReceiver;

		public ParameterTypePattern(IAstNotificationReceiver astNotificationReceiver)
		{
			_astNotificationReceiver = astNotificationReceiver;
		}

		public bool TryParse(ReaderFork<IToken> source, out ParameterType item)
		{
			if (!source.TryNextNonWhitespace<IdentifierToken>(out var identifier)
				) // || identifier.IdentifierCase != IdentifierCase.PascalCase)
				  // we don't want to specifically test for PascalCase because SnakeCase counts as type (number, string, etc)
			{
				item = default;
				return false;
			}

			if (HasBrackets(source))
			{
				item = new ParameterType(identifier, true);
			}
			else
			{
				item = new ParameterType(identifier, false);
			}

			while (HasBrackets(source))
			{
				item = new ParameterType(item, true);
			}

			_astNotificationReceiver.AstCreated(source, item);

			return true;
		}

		private static bool HasBrackets(ReaderFork<IToken> source)
		{
			if (source.TryPeekNonWhitespace<CharacterToken>(out var openBracket, out var peeked)
				&& openBracket.Character == '[')
			{
				source.Advance(peeked);

				if (!source.TryNextNonWhitespace<CharacterToken>(out var closeBracket)
					|| closeBracket.Character != ']')
				{
					// TODO: throw exception, expecting close bracket (or commas)
					return false;
				}

				return true;
			}

			return false;
		}
	}
}