namespace Terumi.Tokens
{
	public static class TokenExtensions
	{
		public static bool IsWhitespace(this Token token)
			=> token is WhitespaceToken
			|| token.IsNewline();

		public static bool IsChar(this Token token, char chr)
			=> token is CharacterToken characterToken && characterToken.Character == chr;

		public static bool IsNewline(this Token token)
			=> token.IsChar('\n');
	}
}