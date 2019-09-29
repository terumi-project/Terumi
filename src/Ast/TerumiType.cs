namespace Terumi.Ast
{
	public abstract class TerumiType : CompilerUnitItem
	{
		public abstract TerumiMember[] Members { get; }
	}
}