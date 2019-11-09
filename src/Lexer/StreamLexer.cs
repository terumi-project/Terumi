using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Terumi.Tokens;

namespace Terumi.Lexer
{
	public class StreamLexer
	{
		private readonly IPattern[] _patterns;

		public StreamLexer(IEnumerable<IPattern> patterns)
			=> _patterns = patterns.ToArray();

		public IEnumerable<Token> ParseTokens(Memory<byte> source)
		{
			int currentPos = 0;
			var readerHead = new ReaderHead<byte>((bytes) =>
			{
				var returnBytes = source.Slice(currentPos, bytes);
				currentPos += bytes;
				return returnBytes.ToArray();
			});

			while (true)
			{
				// make sure there are items
				using (var fork = readerHead.Fork())
				{
					if (!fork.TryNext(out _))
					{
						// if there's nothing left, exit
						break;
					}
				}

				var hasCommit = false;

				foreach (var pattern in _patterns)
				{
					using var fork = readerHead.Fork();

					if (!pattern.TryParse(fork, out var token))
					{
						continue;
					}

					hasCommit = fork.Commit = true;
					yield return token;
					break;
				}

				if (!hasCommit)
				{
					throw new Exception("Unrecognized sequence beginning at " + readerHead.Position);
				}
			}
		}
	}
}