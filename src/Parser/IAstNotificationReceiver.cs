using System;
using Terumi.Tokens;

namespace Terumi.Parser
{
	public interface IAstNotificationReceiver
	{
		void AstCreated<T>(TokenStream source, T ast) => AstCreated(default(ReaderFork<IToken>), ast);

		void AstCreated<T>(ReaderFork<IToken> fork, T ast);

		void DebugPrint(ReaderFork<IToken> fork);

		void Throw(string msg);
	}
}