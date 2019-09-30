namespace Terumi.Ast
{
	public class CompilerUnit
	{
		public CompilerUnit(CompilerUnitItem[] compilerUnitItem)
		{
			CompilerUnitItems = compilerUnitItem;
		}

		public CompilerUnitItem[] CompilerUnitItems { get; }
	}
}