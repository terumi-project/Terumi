using System;
using System.Collections.Generic;
using System.Text;
using Terumi.Ast;
using Terumi.Tokens;

namespace Terumi.Tokenizer
{
	public class ParameterGroupPattern : IPattern<ParameterGroup>
	{
		private readonly IAstNotificationReceiver _astNotificationReceiver;

		public ParameterGroupPattern(IAstNotificationReceiver astNotificationReceiver)
		{
			_astNotificationReceiver = astNotificationReceiver;
		}

		public bool TryParse(ReaderFork<Token> source, out ParameterGroup item)
		{
			var parameters = new List<Parameter>();

			if (!source.TryPeekNonWhitespace<IdentifierToken>(out _, out _))
			{
				// if there's no identifier token, we're done
				item = new ParameterGroup(Array.Empty<Parameter>());
				_astNotificationReceiver.AstCreated(source, item);
				return true;
			}

			do
			{
				if (!source.TryNextNonWhitespace<IdentifierToken>(out var type))
				{
					item = new ParameterGroup(parameters.ToArray());
					_astNotificationReceiver.AstCreated(source, item);
					return true;
				}

				if (!source.TryNextNonWhitespace<IdentifierToken>(out var name))
				{
					// TODO: exception, must specify name of token
					item = default;
					return false;
				}

				var parameter = new Parameter(type, name);
				_astNotificationReceiver.AstCreated(source, parameter);
				parameters.Add(parameter);
			}
			while (NeedsMore(source));

			item = new ParameterGroup(parameters.ToArray());
			_astNotificationReceiver.AstCreated(source, item);
			return true;
		}

		private bool NeedsMore(ReaderFork<Token> source)
		{
			if (source.TryPeekNonWhitespace<CharacterToken>(out var characterToken, out var peeked)
				&& characterToken.IsChar(','))
			{
				source.Advance(peeked);
				return true;
			}

			return false;
		}
	}
}
