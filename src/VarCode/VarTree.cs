using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Terumi.VarCode
{
	public class VarTree
	{
		private int _counter;

		public List<VarInstruction> Code { get; set; } = new List<VarInstruction>();

		public int Push(string value)
			=> AppendInstruction(id => new VarAssignment(id, new ConstantVarExpression<string>(value)));

		public int Push(BigInteger value)
			=> AppendInstruction(id => new VarAssignment(id, new ConstantVarExpression<BigInteger>(value)));

		public int Push(bool value)
			=> AppendInstruction(id => new VarAssignment(id, new ConstantVarExpression<bool>(value)));

		/// <seealso cref="Call(int, int[])"/>
		public void Execute(int methodId, params int[] vars)
			=> Code.Add(new VarMethodCall(null, new MethodCallVarExpression(methodId, vars)));

		/// <seealso cref="Execute(int, int[])"
		public int Call(int methodId, params int[] vars)
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
