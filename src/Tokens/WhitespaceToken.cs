namespace Terumi.Tokens
{
	public class WhitespaceToken : Token
	{
		public WhitespaceToken(int start, int end)
		{
			Start = start;
			End = end;
		}

		public int Start { get; }
		public int End { get; }
	}
}