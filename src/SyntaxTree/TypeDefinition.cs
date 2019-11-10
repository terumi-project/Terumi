namespace Terumi.SyntaxTree
{
	public class TypeDefinition : CompilerUnitItem
	{
		public TypeDefinition(string identifier)
			=> Identifier = identifier;

		public string Identifier { get; }
	}
}