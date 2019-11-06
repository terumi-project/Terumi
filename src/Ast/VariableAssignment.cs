using System;
using System.Collections.Generic;
using System.Text;
using Terumi.Binder;

namespace Terumi.Ast
{
	public class VariableAssignment : ICodeExpression
	{
		public VariableAssignment(string variableName, ICodeExpression value)
		{
			VariableName = variableName;
			Value = value;
		}

		public InfoItem Type => Value.Type;

		public string VariableName { get; }
		public ICodeExpression Value { get; }
	}
}
