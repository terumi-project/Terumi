using Terumi.Binder;

namespace Terumi.Ast
{
	public class ThisExpression : ICodeExpression
	{
		public ThisExpression(InfoItem type)
		{
			Type = type;
		}

		public InfoItem Type { get; }
	}
}