using Terumi.Ast;
using Terumi.Tokens;

namespace Terumi.Tokenizer
{
	public class CodeBodyPattern : IPattern<CodeBody>
	{
		private readonly IAstNotificationReceiver _astNotificationReceiver;

		public CodeBodyPattern(IAstNotificationReceiver astNotificationReceiver)
		{
			_astNotificationReceiver = astNotificationReceiver;
		}

		public bool TryParse(ReaderFork<Token> source, out CodeBody item)
		{
			if (!(source.TryNextNonWhitespace<CharacterToken>(out var openBrace)
				&& openBrace.IsChar('{')))
			{
				// TODO: exception here, or let consumers throw an exception?
				// opting for the latter currently
				item = default;
				return false;
			}

			// TODO: tokenize code bodies :o

			if (!(source.TryNextNonWhitespace<CharacterToken>(out var closeBrace)
				&& closeBrace.IsChar('}')))
			{
				// TODO: exception, expected close brace, got <tkn> instead.
				item = default;
				return false;
			}

			item = new CodeBody();
			_astNotificationReceiver.AstCreated(source, item);
			return true;
		}
	}
}