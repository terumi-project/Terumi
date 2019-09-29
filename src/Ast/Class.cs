namespace Terumi.Ast
{
	public class Class : TerumiType
	{
		public Class(TerumiMember[] members)
		{
			Members = members;
		}

		public override TerumiMember[] Members { get; }
	}
}