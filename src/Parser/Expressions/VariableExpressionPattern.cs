using System;
using System.Collections.Generic;
using System.Text;
using Terumi.SyntaxTree;
using Terumi.SyntaxTree.Expressions;
using Terumi.Tokens;

namespace Terumi.Parser.Expressions
{
	public class VariableExpressionPattern : IPattern<VariableExpression>
	{
		private readonly IAstNotificationReceiver _astNotificationReceiver;
		private readonly IPattern<ParameterType> _parameterTypePattern;

		public VariableExpressionPattern
		(
			IAstNotificationReceiver astNotificationReceiver,
			IPattern<ParameterType> parameterTypePattern
		)
		{
			_astNotificationReceiver = astNotificationReceiver;
			_parameterTypePattern = parameterTypePattern;
		}

		public IPattern<Expression> ExpressionPattern { get; set; }

		public bool TryParse(ReaderFork<IToken> source, out VariableExpression item)
		{
			using (var fork = source.Fork())
			{
				if (_parameterTypePattern.TryParse(fork, out var type))
				{
					fork.Commit = true;
					return Parse(fork, type, out item);
				}
			}

			return Parse(source, null, out item);
		}

		private bool Parse(ReaderFork<IToken> source, ParameterType type, out VariableExpression item)
		{
			if (!source.TryNextNonWhitespace<IdentifierToken>(out var identifier))
			{
				item = default;
				return false;
			}

			if (!source.TryNextCharacter('='))
			{
				item = default;
				return false;
			}

			// ok, now we've assigned a variable and we should be able to deduce what we want
			// start throwing here

			if (!ExpressionPattern.TryParse(source, out var value))
			{
				_astNotificationReceiver.Throw("Variable assignment didn't return value.");
				item = default;
				return false;
			}

			item = new VariableExpression(type, identifier, value);
			_astNotificationReceiver.AstCreated(source, item);
			return true;
		}
	}
}
