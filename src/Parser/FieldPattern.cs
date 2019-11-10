using System.Collections.Generic;

using Terumi.SyntaxTree;
using Terumi.Tokens;

namespace Terumi.Parser
{
	public class FieldPattern : ILegacyPattern<Field>
	{
		private readonly IAstNotificationReceiver _astNotificationReceiver;

		public FieldPattern(IAstNotificationReceiver astNotificationReceiver)
			=> _astNotificationReceiver = astNotificationReceiver;

		public bool TryParse(ReaderFork<IToken> source, out Field item)
		{
			var keywords = new List<KeywordToken>();
			int peeked;

			// it's entirely possible for a field to not have any keywords
			while (source.TryPeekNonWhitespace<KeywordToken>(out var keywordToken, out peeked))
			{
				keywords.Add(keywordToken);
				source.Advance(peeked);
			}

			// if there's an identifier, we're going to try to get another identifier
			if (!source.TryPeekNonWhitespace<IdentifierToken>(out var type, out peeked))
			{
				item = default;
				return false;
			}

			source.Advance(peeked);

			// if we can't, that's fine, it might be a method
			if (!source.TryNextNonWhitespace<IdentifierToken>(out var name))
			{
				item = default;
				return false;
			}

			// try to get a newline to mark the end of the field
			if (!(source.TryNextNonPredicate(tkn => tkn is WhitespaceToken, out var tkn)
				&& tkn.IsNewline()))
			{
				// TODO: exception - expected newline to mark end of field, got <tkn> instead
				item = default;
				return false;
			}

			// so now we must have a field
			// let's make sure the keywords make sense
			bool isReadonly = false;

			foreach (var keyword in keywords)
			{
				if (keyword.Keyword == Keyword.Readonly)
				{
					isReadonly = true;
				}
				else
				{
					// TODO: exception - unexpected keyword <...> when (defining field?)
				}
			}

			item = new Field(isReadonly, type, name);
			_astNotificationReceiver.AstCreated(source, item);
			return true;
		}
	}
}