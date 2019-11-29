using System.Numerics;

// https://www.craftinginterpreters.com/scanning.html
namespace Terumi
{
	// used to wrap around BigInteger
	// to prevent `using System.Numerics;`
	public class Number
	{
		public Number(BigInteger number)
		{
			Value = number;
		}

		public BigInteger Value { get; }
	}
}