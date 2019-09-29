namespace Terumi.Ast
{
	public class CompilerUnit
	{
		public CompilerUnit(CompilerUnitItem[] compilerUnitItem)
		{
			CompilerUnitItem = compilerUnitItem;
		}

		public CompilerUnitItem[] CompilerUnitItem { get; }
	}
}