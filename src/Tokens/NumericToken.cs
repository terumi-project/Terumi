using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Terumi.Tokens
{
	public class NumericToken : Token
	{
		public NumericToken(BigInteger number)
		{
			Number = number;
		}

		public BigInteger Number { get; }
	}
}
