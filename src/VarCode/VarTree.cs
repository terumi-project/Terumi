using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Terumi.VarCode
{
	public class VarTree
	{
		private readonly List<List<VarInstruction>> _backlog = new List<List<VarInstruction>>();
		private readonly List<VarCodeId> _comparisonBacklog = new List<VarCodeId>();

		public List<VarInstruction> Code { get; set; } = new List<VarInstruction>();
		public VarCodeId Counter { get; set; }

		public void BeginIf(VarCodeId comparisonVariable)
		{
			_backlog.Add(Code);
			_comparisonBacklog.Add(comparisonVariable);
			Code = new List<VarInstruction>();
		}

		public void EndIf()
		{
			var @if = new VarIf(_comparisonBacklog[^1], Code);
			_comparisonBacklog.RemoveAt(_comparisonBacklog.Count - 1);
			Code = _backlog[^1];
			_backlog.RemoveAt(_backlog.Count - 1);
			Code.Add(@if);
		}

		public VarCodeId GetParameter(VarCodeId parameterId)
			=> AppendInstruction(id => new VarParameterAssignment(id, parameterId));

		public VarCodeId Push(string value)
			=> AppendInstruction(id => new VarAssignment(id, new ConstantVarExpression<string>(value)));

		public VarCodeId Push(BigInteger value)
			=> AppendInstruction(id => new VarAssignment(id, new ConstantVarExpression<BigInteger>(value)));

		public VarCodeId Push(bool value)
			=> AppendInstruction(id => new VarAssignment(id, new ConstantVarExpression<bool>(value)));

		/// <seealso cref="Call(VarCodeId, List{VarCodeId})"/>
		public void Execute(VarCodeId methodId, List<VarCodeId> vars)
			=> Code.Add(new VarMethodCall(null, new MethodCallVarExpression(methodId, vars)));

		/// <seealso cref="Execute(VarCodeId, List{VarCodeId})"
		public VarCodeId Call(VarCodeId methodId, List<VarCodeId> vars)
			=> AppendInstruction(id => new VarMethodCall(id, new MethodCallVarExpression(methodId, vars)));

		public void Return(VarCodeId variable)
			=> Code.Add(new VarReturn(variable));

		private VarCodeId AppendInstruction(Func<VarCodeId, VarInstruction> append)
		{
			var id = Counter++;
			Code.Add(append(id));
			return id;
		}
	}
}
