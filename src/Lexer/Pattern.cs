using Terumi.Tokenizer;

namespace Terumi.Lexer
{
	public interface IPattern
	{
		bool TryParse(ReaderFork<byte> source, out Token token);
	}
}