using System.Collections.Generic;

using Terumi.Binder;

namespace Terumi.Ast
{
	public class MethodCallExpression : CodeStatement, ICodeExpression
	{
		public MethodCallExpression(ICodeExpression entity, InfoItem.Method callingMethod, IReadOnlyCollection<ICodeExpression> parameters)
		{
			Entity = entity;
			CallingMethod = callingMethod;
			Parameters = parameters;
		}

		public ICodeExpression Entity { get; }
		public InfoItem.Method CallingMethod { get; }
		public IReadOnlyCollection<ICodeExpression> Parameters { get; }

		public InfoItem Type => CallingMethod.ReturnType;
	}
}