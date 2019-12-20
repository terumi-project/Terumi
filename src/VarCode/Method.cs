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
				case Instruction.New o: return o.Store;
				case Instruction.Return o: return o.ValueId;
				case Instruction.Load.String o: return o.Store;
				case Instruction.Load.Number o: return o.Store;
				case Instruction.Load.Boolean o: return o.Store;
				case Instruction.Load.Parameter o: return o.Store;

				case Instruction.Assign o: return Math.Max(o.Store, o.Value);
				case Instruction.GetField o: return Math.Max(o.VariableId, o.Store);
				case Instruction.SetField o: return Math.Max(o.VariableId, o.ValueId);

				case Instruction.Call o: return Math.Max(o.Store, o.Arguments.MaybeMax());
				case Instruction.CompilerCall o: return Math.Max(o.Store, o.Arguments.MaybeMax());

				case Instruction.If o: return Math.Max(o.ComparisonId, o.Clause.MaybeMax(GetHighestId));
				case Instruction.While o: return Math.Max(o.ComparisonId, o.Clause.MaybeMax(GetHighestId));
			}

			throw new InvalidOperationException();
		}
	}

	public interface IResultInstruction
	{
		int Store { get; }
	}

	public interface IClauseInstruction
	{
		int ComparisonId { get; }

		List<Instruction> Clause { get; }
	}

	public abstract class Instruction
	{
		public const int Nowhere = -1;

		public virtual IEnumerable<int> GetUsedVariables()
		{
			yield break;
		}

		public abstract class Load : Instruction
		{
			public abstract int Store { get; }

			public class String : Load, IResultInstruction
			{
				public String(int store, string value)
				{
					Store = store;
					Value = value;
				}

				public override int Store { get; }
				public string Value { get; }

				public override string ToString() => $"Load.String(store: {Store}, value: <IM LAZY>)";
			}

			public class Number : Load, IResultInstruction
			{
				public Number(int store, Terumi.Number value)
				{
					Store = store;
					Value = value;
				}

				public override int Store { get; }
				public Terumi.Number Value { get; }

				public override string ToString() => $"Load.Number(store: {Store}, value: {Value.Value})";
			}

			public class Boolean : Load, IResultInstruction
			{
				public Boolean(int store, bool value)
				{
					Store = store;
					Value = value;
				}

				public override int Store { get; }
				public bool Value { get; }

				public override string ToString() => $"Load.Boolean(store: {Store}, value: {Value})";
			}

			public class Parameter : Load, IResultInstruction
			{
				public Parameter(int store, int parameterNumber)
				{
					Store = store;
					ParameterNumber = parameterNumber;
				}

				public override int Store { get; }
				public int ParameterNumber { get; internal set; }

				public override string ToString() => $"Load.Parameter(store: {Store}, parameterNumber: {ParameterNumber})";
			}
		}

		public class Assign : Instruction, IResultInstruction
		{
			public Assign(int store, int value)
			{
				Store = store;
				Value = value;
			}

			public int Store { get; }
			public int Value { get; }

			public override IEnumerable<int> GetUsedVariables()
			{
				yield return Value;
			}

			public override string ToString() => $"Assign(store: {Store}, value: {Value})";
		}

		public class Call : Instruction, IResultInstruction
		{
			public Call(int store, Method method, List<int> arguments)
			{
				Store = store;
				Method = method;
				Arguments = arguments;
			}

			public int Store { get; internal set; }
			public Method Method { get; }
			public List<int> Arguments { get; internal set; }

			public override IEnumerable<int> GetUsedVariables() => Arguments;

			public override string ToString() => $"Call(store: {Store}, method: '{Method.Name}', arguments: <IM LAZY>)";
		}

		public class CompilerCall : Instruction, IResultInstruction
		{
			// TODO: resolve all unresolved compiler calls into panics
			public CompilerCall(int store, Binder.CompilerMethod compilerMethod, List<int> arguments)
			{
				Store = store;
				CompilerMethod = compilerMethod;
				Arguments = arguments;
			}

			public int Store { get; internal set; }
			public Binder.CompilerMethod CompilerMethod { get; }
			public List<int> Arguments { get; }

			public override IEnumerable<int> GetUsedVariables() => Arguments;

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
			public int ValueId { get; internal set; }

			public override IEnumerable<int> GetUsedVariables()
			{
				yield return ValueId;
			}

			public override string ToString() => $"SetField(variableId: {VariableId}, fieldId: {FieldId}, valueId: {ValueId})";
		}

		public class GetField : Instruction, IResultInstruction
		{
			public GetField(int storeId, int variableId, int fieldId)
			{
				Store = storeId;
				VariableId = variableId;
				FieldId = fieldId;
			}

			public int Store { get; }
			public int VariableId { get; internal set; }
			public int FieldId { get; }

			public override IEnumerable<int> GetUsedVariables()
			{
				yield return VariableId;
			}

			public override string ToString() => $"GetField(storeId: {Store}, variableId: {VariableId}, fieldId: {FieldId})";
		}

		public class New : Instruction, IResultInstruction
		{
			public New(int storeId)
			{
				Store = storeId;
			}

			public int Store { get; }

			public override string ToString() => $"New(storeId: {Store})";
		}

		public class Return : Instruction
		{
			public Return(int valueId)
			{
				ValueId = valueId;
			}

			public int ValueId { get; internal set; }

			public override IEnumerable<int> GetUsedVariables()
			{
				yield return ValueId;
			}

			public override string ToString() => $"Return(valueId: {ValueId})";
		}

		public class If : Instruction, IClauseInstruction
		{
			public If(int variable, List<Instruction> clause)
			{
				ComparisonId = variable;
				Clause = clause;
			}

			public int ComparisonId { get; internal set; }
			public List<Instruction> Clause { get; }

			public override IEnumerable<int> GetUsedVariables()
			{
				yield return ComparisonId;
			}

			public override string ToString() => $"If(variable: {ComparisonId}, clause: <IM LAZY>)";
		}

		public class While : Instruction, IClauseInstruction
		{
			public While(int comparison, List<Instruction> clause)
			{
				ComparisonId = comparison;
				Clause = clause;
			}

			public int ComparisonId { get; internal set; }
			public List<Instruction> Clause { get; }

			public override IEnumerable<int> GetUsedVariables()
			{
				yield return ComparisonId;
			}

			public override string ToString() => $"While(comparison: {ComparisonId}, clause: <IM LAZY>)";
		}
	}
}
