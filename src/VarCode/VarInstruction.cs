using System;
using System.Collections.Generic;
using System.Text;

namespace Terumi.VarCode
{
	// unoptimized, simple instructions to use in the creation of a VarTree

	public abstract class VarInstruction
	{
	}

	public class VarAssignment : VarInstruction
	{
		public VarAssignment(int variableId, VarExpression value)
		{
			VariableId = variableId;
			Value = value;
		}

		public int VariableId { get; }
		public VarExpression Value { get; }
	}

	public class VarReturn : VarInstruction
	{
		public VarReturn(int id)
		{
			Id = id;
		}

		public int Id { get; }
	}

	public class VarMethodCall : VarInstruction
	{
		public VarMethodCall(int? variableId, MethodCallVarExpression methodCallVarExpression)
		{
			MethodCallVarExpression = methodCallVarExpression;
			VariableId = variableId;
		}

		public int? VariableId { get; }
		public MethodCallVarExpression MethodCallVarExpression { get; }
	}

	public abstract class VarExpression
	{
	}

	public class ConstantVarExpression<T> : VarExpression
	{
		public ConstantVarExpression(T value)
		{
			Value = value;
		}

		public T Value { get; }
	}

	public class MethodCallVarExpression : VarExpression
	{
		public MethodCallVarExpression(int methodId, params int[] parameterVariables)
		{
			MethodId = methodId;
			ParameterVariables = parameterVariables;
		}

		public int MethodId { get; }
		public int[] ParameterVariables { get; }
	}
}
