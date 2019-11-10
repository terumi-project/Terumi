using Terumi.Binder;

namespace Terumi.Ast
{
	public class ThisExpression : ICodeExpression
	{
		public ThisExpression(UserType type)
		{
			Type = type;
		}

		public UserType Type { get; }
	}
}