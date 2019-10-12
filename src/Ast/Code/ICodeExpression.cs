using Terumi.Workspace.TypePasser;

namespace Terumi.Ast.Code
{
	public interface ICodeExpression
	{
		public InfoItem Type { get; }
	}
}