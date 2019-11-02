using System;
using System.Text;
using Terumi.Tokens;

namespace Terumi.Lexer
{
	public class StringPattern : IPattern
	{
		public bool TryParse(ReaderFork<byte> source, out Token token)
		{
			if (!source.TryPeek(out var quote) || quote != '"')
			{
				token = default;
				return false;
			}

			source.Advance(1);

			var strb = new StringBuilder();

			// keep reading chars until end

			bool couldNext;
			while ((couldNext = source.TryNext(out var current)) && current != '"')
			{
				if (current == '\r')
				{
					continue;
				}

				if (strb.Length == 0 && current == '\n')
				{
					// we don't want to append a \n if it's the first character in the string
				}

				if (current == '\\')
				{
					if (!source.TryNext(out var next))
					{
						throw new Exception("Expected to escape character - escaped nothing.");
					}

					switch (next)
					{
						case (byte)'n': strb.Append('\n'); break;
						case (byte)'t': strb.Append('\t'); break;
						case (byte)'\\': strb.Append('\\'); break;

						default: throw new Exception("Expected a valid escape sequence - none found.");
					}

					continue;
				}

				strb.Append((char)current);
			}

			if (!couldNext)
			{
				throw new Exception("String didn't end.");
			}

			token = new StringToken(strb.ToString());
			return true;
		}
	}
}
