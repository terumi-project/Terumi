using Terumi.Tokens;

namespace Terumi.Tokenizer
{
	public interface IAstNotificationReceiver
	{
		void AstCreated<T>(ReaderFork<Token> fork, T ast);

		void DebugPrint(ReaderFork<Token> fork);
	}
}