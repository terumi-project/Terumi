using System;
using System.Collections.Generic;

using Terumi.SyntaxTree;
using Terumi.Tokens;

namespace Terumi.Parser
{
	public class ParameterGroupPattern : IPattern<ParameterGroup>
	{
		private readonly IAstNotificationReceiver _astNotificationReceiver;
		private readonly IPattern<ParameterType> _parameterTypePattern;

		public ParameterGroupPattern
		(
			IAstNotificationReceiver astNotificationReceiver,
			IPattern<ParameterType> parameterTypePattern
		)
		{
			_astNotificationReceiver = astNotificationReceiver;
			_parameterTypePattern = parameterTypePattern;
		}

		public bool TryParse(ReaderFork<IToken> source, out ParameterGroup item)
		{
			var parameters = new List<Parameter>();

			using (var fork = source.Fork())
			{
				if (!_parameterTypePattern.TryParse(fork, out _))
				{
					// if there's no identifier token, we're done
					item = new ParameterGroup(Array.Empty<Parameter>());
					_astNotificationReceiver.AstCreated(source, item);
					return true;
				}
			}

			if (!_parameterTypePattern.TryParse(source, out var parameterType))
			{
				throw new Exception("Impossible to reeach ehree");
			}

			if (!source.TryNextNonWhitespace<IdentifierToken>(out var parameterName))
			{
				// TODO: exception - expected parameter
				item = default;
				return false;
			}

			AddParameter();

			while (NeedsMore(source))
			{
				if (!_parameterTypePattern.TryParse(source, out parameterType)
				|| !source.TryNextNonWhitespace<IdentifierToken>(out parameterName))
				{
					// TODO: exception - expected param type/identifier
					item = default;
					return false;
				}

				AddParameter();
			}

			item = new ParameterGroup(parameters.ToArray());
			_astNotificationReceiver.AstCreated(source, item);
			return true;

			void AddParameter()
			{
				var parameter = new Parameter(parameterType, parameterName);
				_astNotificationReceiver.AstCreated(source, parameter);
				parameters.Add(parameter);
			}
		}

		private bool NeedsMore(ReaderFork<IToken> source)
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