using System.Collections.Generic;
using System.Text;

// https://www.craftinginterpreters.com/scanning.html
namespace Terumi.Lexer
{
	public class StringData
	{
		public class Interpolation
		{
			public Interpolation(int position, List<Token> tokens)
			{
				Position = position;
				Tokens = tokens;
			}

			public int Position { get; }
			public List<Token> Tokens { get; }
		}

		public StringData(StringBuilder stringValue, List<Interpolation> interpolations)
		{
			StringValue = stringValue;
			Interpolations = interpolations;
		}

		public StringBuilder StringValue { get; }
		public List<Interpolation> Interpolations { get; }
	}
}