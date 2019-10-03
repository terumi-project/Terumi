namespace Terumi.SyntaxTree
{
	public class TerumiTypeBody
	{
		public TerumiTypeBody(TerumiMember[] members)
		{
			Members = members;
		}

		public TerumiMember[] Members { get; }
	}
}