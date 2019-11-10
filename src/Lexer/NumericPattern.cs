using System;
using System.Numerics;
using System.Text;

using Terumi.Tokens;

namespace Terumi.Lexer
{
	public class NumericPattern : IPattern
	{
		public int TryParse(Span<byte> source, LexerMetadata meta, ref IToken token)
		{
			var end = 0;

			for (; end < source.Length; end++)
			{
				var value = source[end];

				if (
					// if it starts with -
					(end == 0 && value == '-')

					// or the char goes from 0 to 9
					|| (value >= '0' && value <= '9'))
				{
					// it is valid
					continue;
				}

				// invalid char
				break;
			}

			if (end == 0)
			{
				return 0;
			}

			var number = BigInteger.Parse(Encoding.UTF8.GetString(source.Slice(0, end)));

			token = new NumericToken(meta, number);
			return end;
		}
	}
}