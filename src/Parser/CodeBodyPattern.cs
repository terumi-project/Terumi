using System.Collections.Generic;

using Terumi.SyntaxTree;
using Terumi.SyntaxTree.Expressions;

namespace Terumi.Parser
{
	public class CodeBodyPattern : INewPattern<CodeBody>
	{
		private readonly INewPattern<Expression> _expressionPattern;

		public CodeBodyPattern(INewPattern<Expression> expressionPattern)
			=> _expressionPattern = expressionPattern;

		public int TryParse(TokenStream stream, ref CodeBody item)
		{
			if (!stream.NextChar('{'))
			{
				Log.Error($"Expected a code body to start with a {{, didn't get one {stream.TopInfo}");
				return 0;
			}

			var body = new List<Expression>();

			while (!stream.NextChar('}'))
			{
				if (!stream.TryParse(_expressionPattern, out var expression))
				{
					Log.Error($"Unable to parse expression {stream.TopInfo}");
					return 0;
				}

				body.Add(expression);

				if (!stream.NextChar('\n'))
				{
					Log.Warn($"All expression statements should end with a newline (\\n) {stream.TopInfo}");
				}
			}

			item = new CodeBody(body);
			return stream;
		}
	}
}