using System.Collections.Generic;

using Terumi.Ast;
using Terumi.Tokens;

namespace Terumi.Tokenizer
{
	public class PackageLevelPattern : IPattern<PackageLevel>
	{
		private readonly IAstNotificationReceiver _astNotificationReceiver;

		public PackageLevelPattern(IAstNotificationReceiver astNotificationReceiver)
		{
			_astNotificationReceiver = astNotificationReceiver;
		}

		public bool TryParse(ReaderFork<Token> source, out PackageLevel item)
		{
			if (!source.TryNextNonWhitespace<KeywordToken>(out var keywordToken))
			{
				item = default;
				return false;
			}

			if (keywordToken.Keyword == Keyword.Using)
			{
				if (!TryParseLevels(source, out var levels))
				{
					// TODO: exception - expected namespace levels
					item = default;
					return false;
				}

				item = new PackageLevel(PackageAction.Using, levels.ToArray());
				_astNotificationReceiver.AstCreated(source, item);
				return true;
			}
			else if (keywordToken.Keyword == Keyword.Namespace)
			{
				if (!TryParseLevels(source, out var levels))
				{
					// TODO: exception - expected namespace levels
					item = default;
					return false;
				}

				item = new PackageLevel(PackageAction.Namespace, levels.ToArray());
				_astNotificationReceiver.AstCreated(source, item);
				return true;
			}

			item = default;
			return false;
		}

		private bool TryParseLevels(ReaderFork<Token> source, out List<string> levels)
		{
			levels = new List<string>();

			do
			{
				if (!source.TryNextNonWhitespace<IdentifierToken>(out var identifier))
				{
					// TODO: exception - expected identifier
					return false;
				}

				if (identifier.IdentifierCase != IdentifierCase.SnakeCase)
				{
					// TODO: exception - namespaces must be in snakecase
					return false;
				}

				levels.Add(identifier.Identifier);
			}
			while (TryParseAccessLevel(source));

			return true;
		}

		private bool TryParseAccessLevel(ReaderFork<Token> source)
		{
			if (!(source.TryPeekNonWhitespace<CharacterToken>(out var characterToken, out int peeked)
				&& characterToken.Character == '.'))
			{
				return false;
			}

			source.Advance(peeked);
			return true;
		}
	}
}