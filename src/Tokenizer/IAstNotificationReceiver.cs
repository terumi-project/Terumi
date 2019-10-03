using Terumi.Tokens;

namespace Terumi.Parser
{
	public interface IAstNotificationReceiver
	{
		void AstCreated<T>(ReaderFork<Token> fork, T ast);

		void DebugPrint(ReaderFork<Token> fork);

		void Throw(string msg);
	}
}