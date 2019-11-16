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
		public VarAssignment(VarCodeId variableId, VarExpression value)
		{
			VariableId = variableId;
			Value = value;
		}

		public VarCodeId VariableId { get; }
		public VarExpression Value { get; }
	}

	public class VarReturn : VarInstruction
	{
		public VarReturn(VarCodeId id)
		{
			Id = id;
		}

		public VarCodeId Id { get; set; }
	}

	public class VarMethodCall : VarInstruction
	{
		public VarMethodCall(VarCodeId? variableId, MethodCallVarExpression methodCallVarExpression)
		{
			MethodCallVarExpression = methodCallVarExpression;
			VariableId = variableId;
		}

		public VarCodeId? VariableId { get; set; }
		public MethodCallVarExpression MethodCallVarExpression { get; }
	}

	public class VarParameterAssignment : VarInstruction
	{
		public VarParameterAssignment(VarCodeId id, VarCodeId parameterId)
		{
			Id = id;
			ParameterId = parameterId;
		}

		public VarCodeId Id { get; }
		public VarCodeId ParameterId { get; }
	}

	public class VarIf : VarInstruction
	{
		public VarIf(VarCodeId comparisonVariable, List<VarInstruction> trueBody)
		{
			TrueBody = trueBody;
			ComparisonVariable = comparisonVariable;
		}

		public List<VarInstruction> TrueBody { get; }
		public VarCodeId ComparisonVariable { get; set; }
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
		public MethodCallVarExpression(VarCodeId methodId, List<VarCodeId> parameterVariables)
		{
			MethodId = methodId;
			ParameterVariables = parameterVariables;
		}

		public VarCodeId MethodId { get; }
		public List<VarCodeId> ParameterVariables { get; }
	}

	public class ReferenceVarExpression : VarExpression
	{
		public ReferenceVarExpression(VarCodeId variableId)
		{
			VariableId = variableId;
		}

		public VarCodeId VariableId { get; }
	}
}
