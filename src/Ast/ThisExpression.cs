using Terumi.Binder;

namespace Terumi.Ast
{
	public class ThisExpression : ICodeExpression
	{
		public ThisExpression(UserType type)
		{
			Type = type;
		}

		public IType Type { get; }
	}
}