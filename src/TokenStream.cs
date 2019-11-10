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

		public bool NextChar(char character) => 0 != Tokens.NextChar(character).IncButCmp(ref _read);
		public bool NextNoWhitespace<T>(out T token) where T : IToken => 0 != Tokens.NextNoWhitespace<T>(out token).IncButCmp(ref _read);
		public bool NextNoWhitespace(out IToken token) => 0 != Tokens.NextNoWhitespace(out token).IncButCmp(ref _read);

		public TokenStream Child() => new TokenStream(Tokens);

		public bool TryParse<T>(INewPattern<T> tryParse, out T token)
		{
			token = default;

			var read = tryParse.TryParse(Child(), ref token);

			_read += read;
			return read != 0;
		}

		public bool Parse(Parse predicate)
		{
			var read = predicate(Child());

			_read += read;
			return read != 0;
		}

		public static implicit operator TokenStream(Span<IToken> tokens) => new TokenStream(tokens);
		public static implicit operator TokenStream(ReadOnlySpan<IToken> tokens) => new TokenStream(tokens);
		public static implicit operator int(TokenStream stream) => stream.Read;
	}
}
