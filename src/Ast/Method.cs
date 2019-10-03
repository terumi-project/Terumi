namespace Terumi.Ast
{
	public class Method : Member
	{
		// TODO: code body
		public Method(MethodDefinition methodDefinition)
		{
			MethodDefinition = methodDefinition;
		}

		public MethodDefinition MethodDefinition { get; }
	}
}