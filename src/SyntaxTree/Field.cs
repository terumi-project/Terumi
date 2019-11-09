using Terumi.Tokens;

namespace Terumi.SyntaxTree
{
	public class Field : ITerumiMember
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