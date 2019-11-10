using System.Collections.Generic;

using Terumi.SyntaxTree;
using Terumi.Tokens;

namespace Terumi.Parser
{
	public class FieldPattern : IPattern<Field>
	{
		public int TryParse(TokenStream stream, ref Field item)
		{
			var keywords = new List<Keyword>();

			while (stream.NextNoWhitespace<KeywordToken>(out var keyword))
			{
				// TODO: verify keywords
				// TODO: ensure whitespace between keywords
				keywords.Add(keyword.Keyword);
			}

			// dunoo if we're at an end of file
			if (!stream.NextNoWhitespace<IdentifierToken>(out var type)) return 0;

			// we might be dealing with a method, so this one failing is ok
			if (!stream.NextNoWhitespace<IdentifierToken>(out var name)) return 0;

			// let's get a newline
			if (!stream.NextChar('\n')) return 0;

			// we have a field
			item = new Field(keywords.Contains(Keyword.Readonly), type, name);
			return stream;
		}
	}
}