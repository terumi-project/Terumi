using Terumi.Lexer;

namespace Terumi.Tokens
{
	public enum IdentifierCase
	{
		PascalCase,
		SnakeCase
	}

	public class IdentifierToken : IToken
	{
		public IdentifierToken(LexerMetadata meta, string identifier, IdentifierCase identifierCase)
		{
			Start = meta;
			Identifier = identifier;
			IdentifierCase = identifierCase;
		}

		public LexerMetadata Start { get; }
		public LexerMetadata End { get; set; }

		public string Identifier { get; }
		public IdentifierCase IdentifierCase { get; }

		public override string ToString() => $"IdentifierToken - \"{Identifier}\", in {IdentifierCase}";

		public static implicit operator string(IdentifierToken token) => token?.Identifier;
	}
}