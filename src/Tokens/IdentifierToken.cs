using Terumi.Lexer;

namespace Terumi.Tokens
{
	public enum IdentifierCase
	{
		PascalCase,
		SnakeCase
	}

	public class IdentifierToken : Token
	{
		public IdentifierToken(LexerMetadata meta, string identifier, IdentifierCase identifierCase)
		{
			Start = meta;
			Identifier = identifier;
			IdentifierCase = identifierCase;
		}

		public override LexerMetadata Start { get; protected set; }
		public override LexerMetadata End { get; set; }

		public string Identifier { get; }
		public IdentifierCase IdentifierCase { get; }

		public override string ToString() => $"IdentifierToken - \"{Identifier}\", in {IdentifierCase}";
	}
}