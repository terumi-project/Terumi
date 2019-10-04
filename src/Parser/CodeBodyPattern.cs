using System.Collections.Generic;
using Terumi.SyntaxTree;
using Terumi.SyntaxTree.Expressions;
using Terumi.Tokens;

namespace Terumi.Parser
{
	public class CodeBodyPattern : IPattern<CodeBody>
	{
		private readonly IAstNotificationReceiver _astNotificationReceiver;
		private readonly IPattern<Expression> _expressionPattern;

		public CodeBodyPattern
		(
			IAstNotificationReceiver astNotificationReceiver,
			IPattern<Expression> expressionPattern
		)
		{
			_astNotificationReceiver = astNotificationReceiver;
			_expressionPattern = expressionPattern;
		}

		public bool TryParse(ReaderFork<Token> source, out CodeBody item)
		{
			if (!(source.TryNextNonWhitespace<CharacterToken>(out var openBrace)
				&& openBrace.IsChar('{')))
			{
				// TODO: exception here, or let consumers throw an exception?
				// opting for the latter currently
				_astNotificationReceiver.Throw("code body expected {");
				item = default;
				return false;
			}

			var codeBody = new List<Expression>();
			int peeked;

			while (!source.TryPeekCharacter('}', out peeked))
			{
				if (!_expressionPattern.TryParse(source, out var expression))
				{
					// TODO: throw exception - expected expression or end of code body,
					// found neither
					_astNotificationReceiver.Throw("code body expected expression or end of code body, found neither");
					item = default;
					return false;
				}

				codeBody.Add(expression);

				if (!source.TryNextNewline())
				{
					// TODO: throw exception - expected \n to signify end of statement, found nothing
					_astNotificationReceiver.Throw("code body expected \\n to signify end of statement, did not find it.");
					item = default;
					return false;
				}
			}

			source.Advance(peeked);

			item = new CodeBody(codeBody.ToArray());
			_astNotificationReceiver.AstCreated(source, item);
			return true;
		}
	}
}