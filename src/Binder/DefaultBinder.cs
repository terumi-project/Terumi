using Terumi.Ast;
using Terumi.SyntaxTree;

namespace Terumi.Binder
{
	public static class DefaultBinder
	{
		public static CompilationUnit BindToAst(CompilerUnit compilerUnit)
		{
			var binder = new CompilationUnitBinder(compilerUnit);

			return binder.Bind();
		}
	}
}