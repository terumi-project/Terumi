namespace Terumi.Tokens
{
	public class StringToken : Token
	{
		public StringToken(string @string) => String = @string;

		public string String { get; }
	}
}
