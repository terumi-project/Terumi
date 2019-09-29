namespace Terumi.Tokens
{
	public static class TokenExtensions
	{
		public static bool IsWhitespace(this Token token)
			=> token is WhitespaceToken
			|| token.IsNewline();

		public static bool IsNewline(this Token token)
			=> token is CharacterToken characterToken && characterToken.Character == '\n';

		public static bool IsIdentifier(this Token token, IdentifierCase @case, out IdentifierToken identifierToken)
		{
			if (token is IdentifierToken castIdentifierToken)
			{
				identifierToken = castIdentifierToken;
				return identifierToken.IdentifierCase == @case;
			}

			identifierToken = default;
			return false;
		}
	}
}