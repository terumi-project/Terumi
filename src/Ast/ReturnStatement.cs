namespace Terumi.Ast
{
	public class ReturnStatement : CodeStatement
	{
		public ReturnStatement(ICodeExpression returnOn)
		{
			ReturnOn = returnOn;
		}

		public ICodeExpression ReturnOn { get; }
	}
}