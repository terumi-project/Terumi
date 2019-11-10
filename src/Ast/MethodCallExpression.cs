using System.Collections.Generic;

using Terumi.Binder;

namespace Terumi.Ast
{
	public class MethodCallExpression : CodeStatement, ICodeExpression
	{
		public MethodCallExpression(ICodeExpression entity, IMethod callingMethod, IReadOnlyCollection<ICodeExpression> parameters)
		{
			Entity = entity;
			CallingMethod = callingMethod;
			Parameters = parameters;
		}

		public ICodeExpression Entity { get; }
		public IMethod CallingMethod { get; }
		public IReadOnlyCollection<ICodeExpression> Parameters { get; }

		public IType Type => CallingMethod.ReturnType;
	}
}