using Terumi.Binder;

namespace Terumi.Ast
{
	public class ThisExpression : ICodeExpression
	{
		public ThisExpression(IType type)
		{
			Type = type;
		}

		public IType Type { get; }
	}
}