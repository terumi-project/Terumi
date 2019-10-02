using System;
using System.Collections.Generic;
using System.Text;
using Terumi.Ast.Expressions;
using Terumi.Tokens;

namespace Terumi.Tokenizer.Expressions
{
	public class ReturnExpressionPattern : IPattern<ReturnExpression>
	{
		private readonly IAstNotificationReceiver _astNotificationReceiver;

		public ReturnExpressionPattern(IAstNotificationReceiver astNotificationReceiver)
		{
			_astNotificationReceiver = astNotificationReceiver;
		}

		public IPattern<Expression> ExpressionPattern { get; set; }

		public bool TryParse(ReaderFork<Token> source, out ReturnExpression item)
		{
			if (ExpressionPattern == null)
			{
				throw new Exception("Must set ExpressionPattern.");
			}

			// if wwe don't have the return keyword this isn't a return expression
			if (!(source.TryNextNonWhitespace<KeywordToken>(out var keywordToken)
				&& keywordToken.Keyword == Keyword.Return))
			{
				item = default;
				return false;
			}

			if (!ExpressionPattern.TryParse(source, out var expression))
			{
				// TODO: exception - expected expression, didn't get one.
				_astNotificationReceiver.Throw("return expression - expected expression, didn't find one.");
				item = default;
				return false;
			}

			var returnExpression = new ReturnExpression(expression);
			_astNotificationReceiver.AstCreated(source, returnExpression);
			item = returnExpression;
			return true;
		}
	}
}
