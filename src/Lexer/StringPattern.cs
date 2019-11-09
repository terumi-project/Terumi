using System;
using System.Text;
using Terumi.Tokens;

namespace Terumi.Lexer
{
	public class StringPattern : IPattern
	{
		public int TryParse(Span<byte> source, LexerMetadata meta, ref IToken token)
		{
			if (source[0] != '"')
			{
				return 0;
			}

			var strb = new StringBuilder();

			var stringEnded = false;
			const int initialValue = 1;
			int i = initialValue;

			for (; i < source.Length; i++)
			{
				var current = source[i];

				// ignore all '\r's
				if (current == '\r') continue;

				// if the string starts with a '\n', we want to ignore that
/*
string so = "
that multiline strings ignore the first newline"
*/
				if (i == initialValue && current == '\n')
				{
					continue;
				}

				if (current == '\\')
				{
					if (i + 1 < source.Length)
					{
						throw new Exception("TODO: better exception - file ended on a backslash in the middle of a string");
					}

					var next = source[i++];

					switch (next)
					{
						case (byte)'n': strb.Append('\n'); continue;
						case (byte)'t': strb.Append('\t'); continue;
						case (byte)'\\': strb.Append('\\'); continue;
						default: throw new Exception("TODO: better exceptions - Unexpected escape sequence '\\" + next + "'.");
					}
				}

				if (current == '"')
				{
					stringEnded = true;
					break;
				}

				// TODO: could optimize this if we needed to, but EH
				strb.Append(current);
			}

			if (!stringEnded)
			{
				throw new Exception("TODO: better str exceptions - string didn't end");
			}

			token = new StringToken(meta, strb.ToString());
			return i;
		}
	}
}
