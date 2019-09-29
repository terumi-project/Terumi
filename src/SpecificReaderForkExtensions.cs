using System;
using Terumi.Tokens;

namespace Terumi
{
	public static class SpecificReaderForkExtensions
	{
		public static bool TryNextNonWhitespace<T>(this ReaderFork<Token> fork, out T token)
		where T : Token
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

		public static bool TryNextNonWhitespace(this ReaderFork<Token> fork, out Token token)
			=> fork.TryNextNonPredicate
			(
				TokenExtensions.IsWhitespace,
				out token
			);

		public static bool TryNextNonPredicate(this ReaderFork<Token> fork, Func<Token, bool> predicate, out Token token)
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

		public static bool TryPeekNonWhitespace<T>(this ReaderFork<Token> fork, out T token, out int peeked)
		where T : Token
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

		public static bool TryPeekNonWhitespace(this ReaderFork<Token> fork, out Token token, out int peeked)
			=> fork.TryPeekNonPredicate
			(
				TokenExtensions.IsWhitespace,
				out token,
				out peeked
			);

		public static bool TryPeekNonPredicate(this ReaderFork<Token> fork, Func<Token, bool> predicate, out Token token, out int peeked)
		{
			peeked = 1;
			while (fork.TryPeek(out var nextToken, peeked++))
			{
				if (predicate(nextToken))
				{
					continue;
				}

				peeked--;
				token = nextToken;
				return true;
			}

			peeked--;
			token = default;
			return false;
		}
	}
}
