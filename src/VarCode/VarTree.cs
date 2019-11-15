using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Terumi.VarCode
{
	public class VarTree
	{
		private int _counter;

		private readonly List<List<VarInstruction>> _backlog = new List<List<VarInstruction>>();
		private readonly List<int> _comparisonBacklog = new List<int>();

		public List<VarInstruction> Code { get; set; } = new List<VarInstruction>();

		public void BeginIf(int comparisonVariable)
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

		public int GetParameter(int parameterId)
			=> AppendInstruction(id => new VarParameterAssignment(id, parameterId));

		public int Push(string value)
			=> AppendInstruction(id => new VarAssignment(id, new ConstantVarExpression<string>(value)));

		public int Push(BigInteger value)
			=> AppendInstruction(id => new VarAssignment(id, new ConstantVarExpression<BigInteger>(value)));

		public int Push(bool value)
			=> AppendInstruction(id => new VarAssignment(id, new ConstantVarExpression<bool>(value)));

		/// <seealso cref="Call(int, int[])"/>
		public void Execute(int methodId, List<int> vars)
			=> Code.Add(new VarMethodCall(null, new MethodCallVarExpression(methodId, vars)));

		/// <seealso cref="Execute(int, int[])"
		public int Call(int methodId, List<int> vars)
			=> AppendInstruction(id => new VarMethodCall(id, new MethodCallVarExpression(methodId, vars)));

		public void Return(int variable)
			=> Code.Add(new VarReturn(variable));

		private int AppendInstruction(Func<int, VarInstruction> append)
		{
			var id = _counter++;
			Code.Add(append(id));
			return id;
		}
	}
}
