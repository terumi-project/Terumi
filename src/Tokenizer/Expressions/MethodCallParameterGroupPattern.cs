using System;
using System.Collections.Generic;
using System.Text;
using Terumi.Ast.Expressions;
using Terumi.Tokens;

namespace Terumi.Tokenizer.Expressions
{
	public class MethodCallParameterGroupPattern : IPattern<MethodCallParameterGroup>
	{
		private readonly IAstNotificationReceiver _astNotificationReceiver;

		public MethodCallParameterGroupPattern(IAstNotificationReceiver astNotificationReceiver)
		{
			_astNotificationReceiver = astNotificationReceiver;
		}

		public IPattern<Expression> ExpressionPattern { get; set; }

		public bool TryParse(ReaderFork<Token> source, out MethodCallParameterGroup item)
		{
			if (ExpressionPattern == null)
			{
				throw new Exception("Must set ExpressionPattern.");
			}

			var expressions = new List<Expression>();

			while (ExpressionPattern.TryParse(source, out var expression))
			{
				expressions.Add(expression);

				if (!source.TryPeekCharacter(',', out var peeked))
				{
					break;
				}

				source.Advance(peeked);
			}

			item = new MethodCallParameterGroup(expressions.ToArray());
			_astNotificationReceiver.AstCreated(source, item);
			return true;
		}
	}
}
