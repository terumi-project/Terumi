using System.Collections.Generic;

using Terumi.Binder;

namespace Terumi.Ast
{
	public class MethodCallExpression : CodeStatement, ICodeExpression
	{
		public MethodCallExpression(ICodeExpression entity, MethodBind callingMethod, IReadOnlyCollection<ICodeExpression> parameters)
		{
			Entity = entity;
			CallingMethod = callingMethod;
			Parameters = parameters;
		}

		public ICodeExpression Entity { get; }
		public MethodBind CallingMethod { get; }
		public IReadOnlyCollection<ICodeExpression> Parameters { get; }

		public InfoItem Type => CallingMethod.ReturnType;
	}
}