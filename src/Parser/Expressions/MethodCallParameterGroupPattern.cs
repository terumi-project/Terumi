using System;
using System.Collections.Generic;

using Terumi.SyntaxTree.Expressions;
using Terumi.Tokens;

namespace Terumi.Parser.Expressions
{
	public class MethodCallParameterGroupPattern : IPattern<MethodCallParameterGroup>
	{
		private readonly IAstNotificationReceiver _astNotificationReceiver;

		public MethodCallParameterGroupPattern(IAstNotificationReceiver astNotificationReceiver)
			=> _astNotificationReceiver = astNotificationReceiver;

		public IPattern<Expression> ExpressionPattern { get; set; }

		public bool TryParse(ReaderFork<Token> source, out MethodCallParameterGroup item)
		{
			if (ExpressionPattern == null)
			{
				throw new Exception("Must set ExpressionPattern.");
			}

			var expressions = new List<Expression>();
			bool couldParse;

			do
			{
				using var fork = source.Fork();

				couldParse = ExpressionPattern.TryParse(fork, out var expression);

				if (couldParse)
				{
					fork.Commit = true;

					expressions.Add(expression);

					if (!fork.TryPeekCharacter(',', out var peeked))
					{
						break;
					}

					fork.Advance(peeked);
				}
			}
			while (couldParse);

			item = new MethodCallParameterGroup(expressions.ToArray());
			_astNotificationReceiver.AstCreated(source, item);
			return true;
		}
	}
}