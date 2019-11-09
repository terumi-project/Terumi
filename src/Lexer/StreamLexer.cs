using System;
using System.Collections.Generic;
using System.Linq;

using Terumi.Tokens;

namespace Terumi.Lexer
{
	public class StreamLexer
	{
		private readonly IPattern[] _patterns;

		public StreamLexer(IEnumerable<IPattern> patterns)
			=> _patterns = patterns.ToArray();

		/// <param name="filename">Used purely as metadata.</param>
		public IEnumerable<IToken> ParseTokens(Memory<byte> source, string filename)
		{
			var meta = new LexerMetadata { Line = 1, Column = 1, File = filename };

			while (true)
			{
			next: // jumped to after a successful yield <token>

				// make sure there are items
				if (source.Length == 0)
				{
					yield break;
				}

				IToken token = default;
				foreach (var pattern in _patterns)
				{
					var result = pattern.TryParse(source.Span, meta, ref token);

					// trust that the pattern doesn't return anything less than 1 ever
					if (result == 0)
					{
						continue;
					}

					meta = meta.FromConsumed(source.Slice(0, result).Span);
					source = source.Slice(result);

					token.End = meta;

					yield return token;
					goto next; // goes to beginning of while loop
				}

				throw new LexingException($"Unlexable character discovered: {source.Span[0]:X2} {meta}");
			}
		}
	}
}