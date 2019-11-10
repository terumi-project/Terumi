using System;

using Terumi.Tokens;

namespace Terumi
{
	public static class SpecificReaderForkExtensions
	{
		public static bool TryNextNewline(this ReaderFork<IToken> fork)
			=> fork.TryNextNonPredicate(tkn => tkn is WhitespaceToken, out var tkn)
			&& tkn is CharacterToken characterToken
			&& characterToken.Character == '\n';

		public static bool TryNextCharacter(this ReaderFork<IToken> fork, char character)
			=> fork.TryNextNonWhitespace<CharacterToken>(out var characterToken)
			&& characterToken.Character == character;

		public static bool TryNextNonWhitespace<T>(this ReaderFork<IToken> fork, out T token)
		where T : IToken
		{
			if (fork.TryNextNonWhitespace(out var rawToken)
				&& rawToken is T genericToken)
			{
				token = genericToken;
				return true;
			}

			token = default;
			return false;
		}

		public static bool TryNextNonWhitespace(this ReaderFork<IToken> fork, out IToken token)
			=> fork.TryNextNonPredicate
			(
				TokenExtensions.IsWhitespace,
				out token
			);

		public static bool TryNextNonPredicate(this ReaderFork<IToken> fork, Func<IToken, bool> predicate, out IToken token)
		{
			while (fork.TryNext(out var nextToken))
			{
				if (predicate(nextToken))
				{
					continue;
				}

				token = nextToken;
				return true;
			}

			token = default;
			return false;
		}

		public static bool TryPeekCharacter(this ReaderFork<IToken> fork, char character, out int peeked)
			=> fork.TryPeekNonWhitespace<CharacterToken>(out var characterToken, out peeked)
			&& characterToken.Character == character;

		public static bool TryPeekNonWhitespace<T>(this ReaderFork<IToken> fork, out T token, out int peeked)
		where T : IToken
		{
			if (fork.TryPeekNonWhitespace(out var rawToken, out peeked)
				&& rawToken is T genericToken)
			{
				token = genericToken;
				return true;
			}

			token = default;
			return false;
		}

		public static bool TryPeekNonWhitespace(this ReaderFork<IToken> fork, out IToken token, out int peeked)
			=> fork.TryPeekNonPredicate
			(
				TokenExtensions.IsWhitespace,
				out token,
				out peeked
			);

		public static bool TryPeekNonPredicate(this ReaderFork<IToken> source, Func<IToken, bool> predicate, out IToken token, out int peeked)
		{
			using var fork = source.Fork();

			peeked = 0;
			while (fork.TryNext(out var nextToken))
			{
				peeked++;
				if (predicate(nextToken))
				{
					continue;
				}

				token = nextToken;
				return true;
			}

			token = default;
			return false;
		}
	}
}