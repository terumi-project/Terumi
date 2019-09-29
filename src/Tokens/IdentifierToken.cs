namespace Terumi.Tokens
{
	public enum IdentifierCase
	{
		PascalCase,
		SnakeCase
	}

	public class IdentifierToken : Token
	{
		public IdentifierToken(string identifier, IdentifierCase identifierCase)
		{
			if (identifier == "h")
			{
				System.Console.WriteLine("h?");
			}
			Identifier = identifier;
			IdentifierCase = identifierCase;
		}

		public string Identifier { get; }
		public IdentifierCase IdentifierCase { get; }

		public override string ToString() => $"IdentifierToken - \"{Identifier}\", in {IdentifierCase}";
	}
}