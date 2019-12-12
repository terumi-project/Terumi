using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Terumi.VarCode
{
	// represent the flattened state best

	public class Method
	{
		private static int _uniqueId = 0;

		public Method(ObjectType returns, string name, List<ObjectType> parameters)
		{
			Returns = returns;
			Name = name;
			Parameters = parameters;
		}

		public int Id { get; set; } = _uniqueId++;
		public string Name { get; }
		public List<ObjectType> Parameters { get; }
		public List<Instruction> Code { get; } = new List<Instruction>();
		public ObjectType Returns { get; }

		public bool IsEntryPoint => Name.EndsWith("##main") && Parameters.Count == 0;
	}

	public static class InstrExctsnsnsnsndasidnioasdioasinjADSJIODSAJIODIJOSA
	{
		public static int GetHighestId(this Instruction instruction)
		{
			switch (instruction)
			{
				case Instruction.New o: return o.StoreId;
				case Instruction.Return o: return o.ValueId;
				case Instruction.Load.String o: return o.Store;
				case Instruction.Load.Number o: return o.Store;
				case Instruction.Load.Boolean o: return o.Store;
				case Instruction.Load.Parameter o: return o.Store;

				case Instruction.Assign o: return Math.Max(o.Store, o.Value);
				case Instruction.GetField o: return Math.Max(o.VariableId, o.StoreId);
				case Instruction.SetField o: return Math.Max(o.VariableId, o.ValueId);

				case Instruction.Call o: return Math.Max(o.Store, o.Arguments.Max());
				case Instruction.CompilerCall o: return Math.Max(o.Store, o.Arguments.Max());

				case Instruction.If o: return Math.Max(o.Variable, o.Clause.Max(GetHighestId));
				case Instruction.While o: return Math.Max(o.Comparison, o.Clause.Max(GetHighestId));
			}

			throw new InvalidOperationException();
		}
	}

	public abstract class Instruction
	{
		public const int Nowhere = -1;

		public abstract class Load : Instruction
		{
			public class String : Load
			{
				public String(int store, string value)
				{
					Store = store;
					Value = value;
				}

				public int Store { get; }
				public string Value { get; }

				public override string ToString() => $"Load.String(store: {Store}, value: <IM LAZY>)";
			}

			public class Number : Load
			{
				public Number(int store, Terumi.Number value)
				{
					Store = store;
					Value = value;
				}

				public int Store { get; }
				public Terumi.Number Value { get; }

				public override string ToString() => $"Load.Number(store: {Store}, value: {Value.Value})";
			}

			public class Boolean : Load
			{
				public Boolean(int store, bool value)
				{
					Store = store;
					Value = value;
				}

				public int Store { get; }
				public bool Value { get; }

				public override string ToString() => $"Load.Boolean(store: {Store}, value: {Value})";
			}

			public class Parameter : Load
			{
				public Parameter(int store, int parameterNumber)
				{
					Store = store;
					ParameterNumber = parameterNumber;
				}

				public int Store { get; }
				public int ParameterNumber { get; internal set; }

				public override string ToString() => $"Load.Parameter(store: {Store}, parameterNumber: {ParameterNumber})";
			}
		}

		public class Assign : Instruction
		{
			public Assign(int store, int value)
			{
				Store = store;
				Value = value;
			}

			public int Store { get; }
			public int Value { get; }

			public override string ToString() => $"Assign(store: {Store}, value: {Value})";
		}

		public class Call : Instruction
		{
			public Call(int store, Method method, List<int> arguments)
			{
				Store = store;
				Method = method;
				Arguments = arguments;
			}

			public int Store { get; }
			public Method Method { get; }
			public List<int> Arguments { get; }

			public override string ToString() => $"Call(store: {Store}, method: '{Method.Name}', arguments: <IM LAZY>)";
		}

		public class CompilerCall : Instruction
		{
			// TODO: resolve all unresolved compiler calls into panics
			public CompilerCall(int store, Binder.CompilerMethod compilerMethod, List<int> arguments)
			{
				Store = store;
				CompilerMethod = compilerMethod;
				Arguments = arguments;
			}

			public int Store { get; }
			public Binder.CompilerMethod CompilerMethod { get; }
			public List<int> Arguments { get; }

			public override string ToString() => $"CompilerCall(store: {Store}, method: '{(CompilerMethod?.Name ?? "null")}', arguments: <IM LAZY>)";
		}

		public class SetField : Instruction
		{
			public SetField(int variableId, int fieldId, int valueId)
			{
				VariableId = variableId;
				FieldId = fieldId;
				ValueId = valueId;
			}

			public int VariableId { get; }
			public int FieldId { get; }
			public int ValueId { get; }

			public override string ToString() => $"SetField(variableId: {VariableId}, fieldId: {FieldId}, valueId: {ValueId})";
		}

		public class GetField : Instruction
		{
			public GetField(int storeId, int variableId, int fieldId)
			{
				StoreId = storeId;
				VariableId = variableId;
				FieldId = fieldId;
			}

			public int StoreId { get; }
			public int VariableId { get; }
			public int FieldId { get; }

			public override string ToString() => $"GetField(storeId: {StoreId}, variableId: {VariableId}, fieldId: {FieldId})";
		}

		public class New : Instruction
		{
			public New(int storeId)
			{
				StoreId = storeId;
			}

			public int StoreId { get; }

			public override string ToString() => $"New(storeId: {StoreId})";
		}

		public class Return : Instruction
		{
			public Return(int valueId)
			{
				ValueId = valueId;
			}

			public int ValueId { get; }

			public override string ToString() => $"Return(valueId: {ValueId})";
		}

		public class If : Instruction
		{
			public If(int variable, List<Instruction> clause)
			{
				Variable = variable;
				Clause = clause;
			}

			public int Variable { get; }
			public List<Instruction> Clause { get; }

			public override string ToString() => $"If(variable: {Variable}, clause: <IM LAZY>)";
		}

		public class While : Instruction
		{
			public While(int comparison, List<Instruction> clause)
			{
				Comparison = comparison;
				Clause = clause;
			}

			public int Comparison { get; }
			public List<Instruction> Clause { get; }

			public override string ToString() => $"While(comparison: {Comparison}, clause: <IM LAZY>)";
		}
	}
}
