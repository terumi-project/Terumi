using System;
using System.Collections.Generic;

using Terumi.SyntaxTree;
using Terumi.Tokens;

namespace Terumi.Parser
{
	public class ParameterGroupPattern : INewPattern<ParameterGroup>
	{
		private readonly IAstNotificationReceiver _astNotificationReceiver;
		private readonly INewPattern<ParameterType> _parameterTypePattern;

		public ParameterGroupPattern
		(
			IAstNotificationReceiver astNotificationReceiver,
			INewPattern<ParameterType> parameterTypePattern
		)
		{
			_astNotificationReceiver = astNotificationReceiver;
			_parameterTypePattern = parameterTypePattern;
		}

		public int TryParse(Span<IToken> source, ref ParameterGroup item)
		{
			int read;
			item = ParameterGroup.NoParameters;
			ParameterType parameterType = default;

			if (0 == (read = _parameterTypePattern.TryParse(source, ref parameterType))) return ParserConstants.ParseNothingButSuccess;
			if (0 == (source.Slice(read).NextNoWhitespace<IdentifierToken>(out var parameterName).IncButCmp(ref read)))
			{
				source.Slice(read).NextNoWhitespace(out var errToken);
				Log.Error($"Expected identifier while parsing parameter group, but didn't receive one at {errToken.Start} Ends at {errToken.End}");
				return 0;
			}

			var parameters = new List<Parameter>();
			AddParameter(source);

			while (0 != HasMoreParameters(source.Slice(read)).IncButCmp(ref read))
			{
				if (0 == (_parameterTypePattern.TryParse(source.Slice(read), ref parameterType).IncButCmp(ref read)))
				{
					source.Slice(read).NextNoWhitespace(out var errToken);
					Log.Error($"Expected another parameter type while parsing parameter group, but didn't receive one at {errToken.Start} Ends at {errToken.End}");
					return 0;
				}

				if (0 == (source.Slice(read).NextNoWhitespace<IdentifierToken>(out parameterName).IncButCmp(ref read)))
				{
					source.Slice(read).NextNoWhitespace(out var errToken);
					Log.Error($"Expected another identifier while parsing parameter group, but didn't receive one at {errToken.Start} Ends at {errToken.End}");
					return 0;
				}

				AddParameter(source);
			}

			item = new ParameterGroup(parameters);
			_astNotificationReceiver.AstCreated(source, item);
			return read;

			void AddParameter(Span<IToken> bypassCsharpCrud)
			{
				var parameter = new Parameter(parameterType, parameterName);
				_astNotificationReceiver.AstCreated(bypassCsharpCrud.Slice(read), parameter);
				parameters.Add(parameter);
			}
		}

		private static int HasMoreParameters(Span<IToken> source) => source.NextChar(',');

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