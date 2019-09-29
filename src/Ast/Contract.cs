namespace Terumi.Ast
{
	public class Contract : TerumiType
	{
		public Contract(TerumiMember[] members)
		{
			Members = members;
		}

		public override TerumiMember[] Members { get; }
	}
}