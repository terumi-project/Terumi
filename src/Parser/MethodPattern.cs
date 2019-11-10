using Terumi.SyntaxTree;
using Terumi.Tokens;

namespace Terumi.Parser
{
	public class MethodPattern : IPattern<Method>
	{
		private readonly IPattern<ParameterGroup> _parameterPattern;
		private readonly IPattern<CodeBody>? _codeBodyPattern;

		public MethodPattern
		(
			IPattern<ParameterGroup> parameterPattern,
			IPattern<CodeBody>? codeBodyPattern
		)
		{
			_parameterPattern = parameterPattern;
			_codeBodyPattern = codeBodyPattern;
		}

		public int TryParse(TokenStream stream, ref Method item)
		{
			// we might have 'my_method() { ... }' or 'number my_method() { ... }', so we need to handle that

			if (!stream.NextNoWhitespace<IdentifierToken>(out var identifierOrType)) return 0;

			if (stream.NextChar('('))
			{
				return ParameterParsingStage(ref stream, null, identifierOrType, ref item);
			}
			else if (stream.NextNoWhitespace<IdentifierToken>(out var identifier))
			{
				if (!stream.NextChar('('))
				{
					Log.Error($"Expected opening parenthesis on method definition, but didn't get one {stream.TopInfo}");
					return 0;
				}

				return ParameterParsingStage(ref stream, identifierOrType, identifier, ref item);
			}

			Log.Warn($"Unable to parse method {stream.TopInfo}");
			return 0;
		}

		private int ParameterParsingStage(ref TokenStream stream, IdentifierToken? type, IdentifierToken name, ref Method method)
		{
			if (!stream.TryParse(_parameterPattern, out var parameterGroup))
			{
				Log.Error($"Unable to parse method parameters, {stream.TopInfo}");
				return 0;
			}

			if (!stream.NextChar(')'))
			{
				Log.Error($"Expected closing parenthesis on parameter group, didn't get one {stream.TopInfo}");
				return 0;
			}

			var result = GetCodeBody(ref stream, out var body);
			method = new Method(type, name, parameterGroup, body);
			return result;
		}

		private int GetCodeBody(ref TokenStream stream, out CodeBody? body)
		{
			if (_codeBodyPattern == null)
			{
				// contracts don't have bodies
				body = null;

				if (!stream.NextChar('\n'))
				{
					Log.Error($"You must have a newline to end a method in a contract {stream.TopInfo}");
					return 0;
				}
			}
			else
			{
				if (!stream.TryParse(_codeBodyPattern, out body))
				{
					Log.Error($"Couldn't parse code body {stream.TopInfo}");
					return 0;
				}
			}

			return stream;
		}
	}
}