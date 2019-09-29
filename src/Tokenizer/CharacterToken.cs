namespace Terumi.Tokenizer
{
	public class CharacterToken : Token
	{
		public CharacterToken(char character, int position)
		{
			Character = character;
			Position = position;
		}

		public char Character { get; }
		public int Position { get; }
	}
}