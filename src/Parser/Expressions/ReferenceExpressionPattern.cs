using System;
using System.Collections.Generic;
using System.Text;
using Terumi.SyntaxTree.Expressions;
using Terumi.Tokens;

namespace Terumi.Parser.Expressions
{
	public class ReferenceExpressionPattern : IPattern<ReferenceExpression>
	{
		private readonly IAstNotificationReceiver _astNotificationReceiver;

		public ReferenceExpressionPattern(IAstNotificationReceiver astNotificationReceiver)
		{
			_astNotificationReceiver = astNotificationReceiver;
		}

		public bool TryParse(ReaderFork<IToken> source, out ReferenceExpression item)
		{
			if (!source.TryNextNonWhitespace<IdentifierToken>(out var identifier))
			{
				// a reference should just be a name - the name would be a field or a parameter or whatnot
				item = default;
				return false;
			}

			item = new ReferenceExpression(identifier.Identifier);
			_astNotificationReceiver.AstCreated(source, item);
			return true;
		}
	}
}
