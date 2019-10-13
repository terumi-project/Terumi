using System;

using Terumi.SyntaxTree.Expressions;
using Terumi.Tokens;

namespace Terumi.Parser.Expressions
{
	public class AccessExpressionPattern : IPattern<AccessExpression>
	{
		private readonly IAstNotificationReceiver _astNotificationReceiver;

		public AccessExpressionPattern
		(
			IAstNotificationReceiver astNotificationReceiver
		)
			=> _astNotificationReceiver = astNotificationReceiver;

		public IPattern<Expression> ExpressionPattern { get; set; }

		public bool TryParse(ReaderFork<Token> source, out AccessExpression item)
		{
			if (ExpressionPattern == null)
			{
				throw new ArgumentException("Set ExpressionPattern in " + nameof(AccessExpressionPattern));
			}

			if (!source.TryNextCharacter('.'))
			{
				item = default;
				return false;
			}

			if (!ExpressionPattern.TryParse(source, out var accessExpression))
			{
				// TODO: exception
				_astNotificationReceiver.Throw("Expected expression after dot, didn't get that.");
				item = default;
				return false;
			}

			item = new AccessExpression { Access = accessExpression };
			_astNotificationReceiver.AstCreated(source, item);
			return true;
		}
	}
}