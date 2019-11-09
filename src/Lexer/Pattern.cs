using System;

using Terumi.Tokens;

namespace Terumi.Lexer
{
	public interface IPattern
	{
		/// <returns>anything less than 1 means failure,
		/// anything greater than 0 represents the amount of bytes read
		/// and that it succeeded.</returns>
		int TryParse(Span<byte> source, LexerMetadata meta, ref IToken token);
	}
}