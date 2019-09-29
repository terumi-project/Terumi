using Terumi.Tokens;

namespace Terumi.Ast
{
	public class Parameter
	{
		public Parameter(IdentifierToken type, IdentifierToken name)
		{
			Type = type;
			Name = name;
		}

		public IdentifierToken Type { get; }
		public IdentifierToken Name { get; }
	}
}