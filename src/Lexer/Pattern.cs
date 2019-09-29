using Terumi.Tokenizer;

namespace Terumi.Lexer
{
	public interface IPattern
	{
		bool TryParse(ReaderFork source, out Token token);
	}
}