using System;
using System.Collections.Generic;
using System.Text;

namespace Terumi.VarCode.Optimizer.Omega
{
	// these instructions are more easily compressible
	// so that targets can output more compact code
	// instead of having to assign variables to everything

	// it also more closley resembles the final output

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
		public VarExpression Value { get; set; }
	}

	public class VarReturn : VarInstruction
	{
		public VarReturn(VarExpression value)
		{
			Value = value;
		}

		public VarExpression Value { get; set; }
	}

	public class VarMethodCall : VarInstruction
	{
		public VarMethodCall(MethodCallVarExpression methodCallVarExpression)
		{
			MethodCallVarExpression = methodCallVarExpression;
		}

		public MethodCallVarExpression MethodCallVarExpression { get; set; }
	}

	public class VarIf : VarInstruction
	{
		public VarIf(VarExpression comparisonExpression, List<VarInstruction> trueBody)
		{
			ComparisonExpression = comparisonExpression;
			TrueBody = trueBody;
		}

		public List<VarInstruction> TrueBody { get; }
		public VarExpression ComparisonExpression { get; set; }
	}

	public abstract class VarExpression
	{
	}

	public class ConstantVarExpression<T> : VarExpression, IConstantVarExpression
	{
		public ConstantVarExpression(T value)
		{
			Value = value;
		}

		public T Value { get; }

		object IConstantVarExpression.Value => Value;
	}

	public class MethodCallVarExpression : VarExpression
	{
		public MethodCallVarExpression(VarCodeId methodId, List<VarExpression> parameters)
		{
			MethodId = methodId;
			Parameters = parameters;
		}

		public VarCodeId MethodId { get; }
		public List<VarExpression> Parameters { get; }
	}

	public class ReferenceVarExpression : VarExpression
	{
		public ReferenceVarExpression(VarCodeId variableId)
		{
			VariableId = variableId;
		}

		public VarCodeId VariableId { get; }
	}

	public class ParameterReferenceVarExpression : VarExpression
	{
		public ParameterReferenceVarExpression(int parameterId)
		{
			ParameterId = parameterId;
		}

		public int ParameterId { get; }
	}
}
