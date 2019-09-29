using System;
using System.Collections.Generic;
using System.Text;

namespace Terumi.Tokens
{
	public static class TokenExtensions
	{
		public static bool IsWhitespace(this Token token)
			=> token is WhitespaceToken
			|| token.IsNewline();

		public static bool IsNewline(this Token token)
			=> token is CharacterToken characterToken && characterToken.Character == '\n';
	}
}
