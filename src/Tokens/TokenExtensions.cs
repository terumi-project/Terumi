namespace Terumi.Tokens
{
	public static class TokenExtensions
	{
		public static bool IsWhitespace(this IToken token)
			=> token is WhitespaceToken
			|| token.IsNewline();

		public static bool IsChar(this IToken token, char chr)
			=> token is CharacterToken characterToken && characterToken.Character == chr;

		public static bool IsNewline(this IToken token)
			=> token.IsChar('\n');
	}
}