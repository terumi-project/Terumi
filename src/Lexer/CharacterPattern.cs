using Terumi.Tokenizer;

namespace Terumi.Lexer
{
	public class CharacterPattern : IPattern
	{
		private readonly char _character;

		public CharacterPattern(char character)
			=> _character = character;

		public bool TryParse(ReaderFork source, out Token token)
		{
			var position = source.Position;

			if (source.TryNext(out var value)
			&& value == _character)
			{
				token = new CharacterToken(_character, position);
				return true;
			}

			token = default;
			return false;
		}
	}
}