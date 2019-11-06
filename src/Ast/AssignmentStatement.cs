using System;
using System.Collections.Generic;
using System.Text;

namespace Terumi.Ast
{
	public class AssignmentStatement : CodeStatement
	{
		public AssignmentStatement(VariableAssignment variableAssignment)
		{
			VariableAssignment = variableAssignment;
		}

		public VariableAssignment VariableAssignment { get; }
	}
}
