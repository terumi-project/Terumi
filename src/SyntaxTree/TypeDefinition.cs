namespace Terumi.SyntaxTree
{
	public enum TypeDefinitionType
	{
		Class,
		Contract
	}

	public static class TypeDefinitionTypeExtensions
	{
		public static Tokens.Keyword ToKeyword(this TypeDefinitionType typeDefinitionType)
			=> typeDefinitionType switch
			{
				TypeDefinitionType.Class => Tokens.Keyword.Class,
				TypeDefinitionType.Contract => Tokens.Keyword.Contract,
			};
	}

	public class TypeDefinition : CompilerUnitItem
	{
		public TypeDefinition(string identifier, TypeDefinitionType type, TerumiMember[] members)
		{
			Identifier = identifier;
			Members = members;
			Type = type;
		}

		public TypeDefinitionType Type { get; }

		public string Identifier { get; }

		public TerumiMember[] Members { get; }

		public override string ToString()
			=> $"{Type} {Identifier}";
	}
}