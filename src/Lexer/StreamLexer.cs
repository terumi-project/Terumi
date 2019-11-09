using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Terumi.Tokens;

namespace Terumi.Lexer
{
	public struct LexerMetadata
	{
		public int Line;
		public int Column;
		public int BinaryOffset;
		public string File;

		public string ToInfo() => $"on line {Line}, column {Column} (binary offset {BinaryOffset}) in file {File}.";
	}

	public class StreamLexer
	{
		private readonly IPattern[] _patterns;

		public StreamLexer(IEnumerable<IPattern> patterns)
			=> _patterns = patterns.ToArray();

		/// <param name="filename">Used purely as metadata.</param>
		public IEnumerable<Token> ParseTokens(Memory<byte> source, string filename)
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

				Token token = default;
				foreach (var pattern in _patterns)
				{
					var result = pattern.TryParse(source.Span, meta, ref token);

					// TODO: check that token isn't an ExceptionToken

					// trust that the pattern doesn't return anything less than 1 ever
					if (result == 0)
					{
						continue;
					}

					// update lexer metadata based on what was consumed
					var consumed = source.Slice(0, result);
					source = source.Slice(result);

					meta.BinaryOffset += result;

					for (var i = 0; i < consumed.Length; i++)
					{
						if (consumed.Span[i] == '\n')
						{
							meta.Line++;
							meta.Column = 1;
						}
						else
						{
							meta.Column++;
						}
					}

					yield return token;
					goto next; // goes to beginning of while loop
				}

				throw new Exception($"Unlexable character discovered {meta.ToInfo()}");
			}
		}
	}
}