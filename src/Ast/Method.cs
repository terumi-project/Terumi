using Terumi.Ast.Code;

namespace Terumi.Ast
{
	public class Method : Member
	{
		// TODO: code body
		public Method(MethodDefinition methodDefinition, CodeBlock codeBlock)
		{
			MethodDefinition = methodDefinition;
			CodeBlock = codeBlock;
		}

		public MethodDefinition MethodDefinition { get; }
		public CodeBlock CodeBlock { get; }
	}
}