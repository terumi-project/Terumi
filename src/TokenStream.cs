using System;

using Terumi.Parser;
using Terumi.Tokens;

namespace Terumi
{
	public delegate int Parse(TokenStream stream);

	public ref struct TokenStream
	{
		private readonly ReadOnlySpan<IToken> _tokens;

		private int _read;

		public TokenStream(ReadOnlySpan<IToken> tokens)
		{
			_tokens = tokens;
			_read = 0;
		}

		public ReadOnlySpan<IToken> Tokens => _tokens.Slice(_read);
		public int Read => _read;
		public IToken Top => _tokens[_read];
		public string TopInfo => $"{Top.Start} Ends {Top.End}";

		public bool NextKeyword(Keyword keyword) => NextNoWhitespace<KeywordToken>(out var token) && token.Keyword == keyword;

		public bool NextChar(char character) => 0 != Tokens.NextChar(character).IncButCmp(ref _read);

		public bool NextChars(string chars)
		{
			var read = Tokens.NextChar(chars[0]);
			if (0 == read) return false;

			for (var i = 1; i < chars.Length; i++)
			{
				if (!(Tokens.Next((read - 1) + i, out var next)
					&& next is CharacterToken charToken
					&& charToken.Character == chars[i]))
				{
					return false;
				}
			}

			_read += read + chars.Length - 1;
			return true;
		}

		public bool NextNoWhitespace<T>(out T token) where T : IToken => 0 != Tokens.NextNoWhitespace<T>(out token).IncButCmp(ref _read);

		public bool NextNoWhitespace(out IToken token) => 0 != Tokens.NextNoWhitespace(out token).IncButCmp(ref _read);

		public bool PeekNextNoWhitespace(out IToken token) => 0 != Tokens.NextNoWhitespace(out token);

		public TokenStream Child() => new TokenStream(Tokens);

		public bool TryParse<T>(IPattern<T> tryParse, out T token)
		{
			token = default;

			var read = tryParse.TryParse(Child(), ref token);

			if (read > 0)
			{
				_read += read;
			}

			return read != 0;
		}

		public bool Parse(Parse predicate)
		{
			var read = predicate(Child());

			if (read > 0)
			{
				_read += read;
			}

			return read != 0;
		}

		public static implicit operator TokenStream(Span<IToken> tokens) => new TokenStream(tokens);

		public static implicit operator TokenStream(ReadOnlySpan<IToken> tokens) => new TokenStream(tokens);

		// if we return TokenStream, we want to return -1 if we've parsed nothing to represent success
		public static implicit operator int(TokenStream stream) => stream.Read == 0 ? ParserConstants.ParseNothingButSuccess : stream.Read;
	}
}