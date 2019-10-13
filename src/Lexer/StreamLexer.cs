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

		public IEnumerable<Token> ParseTokens(Stream source)
		{
			using var reader = new BinaryReader(source, Encoding.UTF8, true);

			var readerHead = new ReaderHead<byte>(reader.ReadBytes);

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