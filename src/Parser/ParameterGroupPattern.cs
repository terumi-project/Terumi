using System.Collections.Generic;

using Terumi.SyntaxTree;
using Terumi.Tokens;

namespace Terumi.Parser
{
	public class ParameterGroupPattern : IPattern<ParameterGroup>
	{
		private readonly IPattern<ParameterType> _parameterTypePattern;

		public ParameterGroupPattern(IPattern<ParameterType> parameterTypePattern)
			=> _parameterTypePattern = parameterTypePattern;

		public int TryParse(TokenStream stream, ref ParameterGroup item)
		{
			item = ParameterGroup.NoParameters;

			if (!ParameterType(ref stream, out var parameterType)) return stream;
			if (!IdentifierToken(ref stream, out var parameterName))
			{
				Log.Error($"Expected identifier while parsing parameter group, {But(stream.Top)}");
				return 0;
			}

			var parameters = new List<Parameter>();
			AddParameter();

			while (HasMoreParameters(ref stream))
			{
				if (!ParameterType(ref stream, out parameterType))
				{
					Log.Error($"Expected another parameter type while parsing parameter group, {But(stream.Top)}");
					return 0;
				}

				if (!IdentifierToken(ref stream, out parameterName))
				{
					Log.Error($"Expected another identifier while parsing parameter group, {But(stream.Top)}");
					return 0;
				}

				AddParameter();
			}

			item = new ParameterGroup(parameters);
			return stream;

			void AddParameter()
			{
				parameters.Add(new Parameter(parameterType, parameterName));
			}

			bool ParameterType(ref TokenStream stream, out ParameterType parameterType)
				=> stream.TryParse(_parameterTypePattern, out parameterType);

			static bool IdentifierToken(ref TokenStream stream, out IdentifierToken identifierToken)
				=> stream.NextNoWhitespace<IdentifierToken>(out identifierToken);

			static string But(IToken top) => $"but didn't receive one at {top.Start} Ends at {top.End}";
		}

		private static bool HasMoreParameters(ref TokenStream stream) => stream.NextChar(',');
	}
}