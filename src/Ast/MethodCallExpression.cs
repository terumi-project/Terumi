using System.Collections.Generic;

using Terumi.Binder;

namespace Terumi.Ast
{
	public class MethodCallExpression : CodeStatement, ICodeExpression
	{
		public MethodCallExpression(IMethod callingMethod, List<ICodeExpression> parameters)
		{
			CallingMethod = callingMethod;
			Parameters = parameters;
		}

		public IMethod CallingMethod { get; }
		public List<ICodeExpression> Parameters { get; }

		public IType Type => CallingMethod.ReturnType;
	}
}