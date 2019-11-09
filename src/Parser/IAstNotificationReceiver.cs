using Terumi.Tokens;

namespace Terumi.Parser
{
	public interface IAstNotificationReceiver
	{
		void AstCreated<T>(ReaderFork<IToken> fork, T ast);

		void DebugPrint(ReaderFork<IToken> fork);

		void Throw(string msg);
	}
}