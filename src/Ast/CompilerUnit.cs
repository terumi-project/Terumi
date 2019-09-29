namespace Terumi.Ast
{
	public class CompilerUnit
	{
		public CompilerUnit(TerumiType[] terumiTypes)
		{
			TerumiTypes = terumiTypes;
		}

		public TerumiType[] TerumiTypes { get; }
	}
}