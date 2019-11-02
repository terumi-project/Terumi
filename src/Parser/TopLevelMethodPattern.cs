using Terumi.SyntaxTree;
using Terumi.Tokens;

namespace Terumi.Parser
{
	public class TopLevelMethodPattern : IPattern<TypeDefinition>
	{
		private readonly IAstNotificationReceiver _astNotificationReceiver;
		private readonly IPattern<Method> _methodPattern;

		public TopLevelMethodPattern
		(
			IAstNotificationReceiver astNotificationReceiver,
			IPattern<Method> methodPattern
		)
		{
			_astNotificationReceiver = astNotificationReceiver;
			_methodPattern = methodPattern;
		}

		public bool TryParse(ReaderFork<Token> source, out TypeDefinition item)
		{
			if (_methodPattern.TryParse(source, out var method))
			{
				item = new TypeDefinition(method.Identifier.Identifier, method);

				_astNotificationReceiver.AstCreated(source, item);
				return true;
			}

			item = default;
			return false;
		}
	}
}