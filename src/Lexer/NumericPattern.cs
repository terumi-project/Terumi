using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Terumi.Tokens;

namespace Terumi.Lexer
{
	public class NumericPattern : IPattern
	{
		public bool TryParse(ReaderFork<byte> source, out Token token)
		{
			var number = new BigInteger(0);
			bool consumedNumber = false;

			while (source.TryPeek(out var value))
			{
				if (!(value >= '0' && value <= '9'))
				{
					break;
				}

				consumedNumber = true;

				number *= 10;
				number += (int)value - '0';

				source.Advance(1);
			}

			if (consumedNumber)
			{
				token = new NumericToken(number);
				return true;
			}

			token = default;
			return false;
		}
	}
}
