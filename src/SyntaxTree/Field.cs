using Terumi.Tokens;

namespace Terumi.SyntaxTree
{
	public class Field : TerumiMember
	{
		public Field(bool @readonly, IdentifierToken type, IdentifierToken name)
		{
			Name = name;
			Type = type;
			Readonly = @readonly;
		}

		public IdentifierToken Name { get; }
		public IdentifierToken Type { get; }
		public bool Readonly { get; }
	}
}