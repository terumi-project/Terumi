using System;
using System.Collections.Generic;
using System.Text;

namespace Terumi.VarCode
{
	// a simpler representation of what the Binder provides
	// this is to make translation from the binder to varcode easier, as this only contains varcode concepts

	public class Method
	{
		public Method(Binder.Class? context, Binder.Method fromBinder, string name, List<Binder.MethodParameter> parameters)
		{
			Name = name;
			Parameters = parameters;
			Context = context;
			FromBinder = fromBinder;
		}

		public int Id { get; set; }
		public string Name { get; }
		public List<Binder.MethodParameter> Parameters { get; }
		public List<Instruction> Code { get; } = new List<Instruction>();
		public Binder.Class? Context { get; }
		public Binder.Method FromBinder { get; }
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
			}

			public class Parameter : Load
			{
				public Parameter(int store, int parameterNumber)
				{
					Store = store;
					ParameterNumber = parameterNumber;
				}

				public int Store { get; }
				public int ParameterNumber { get; }
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
		}

		public class CompilerCall : Instruction
		{
			public CompilerCall(int store, Binder.CompilerMethod compilerMethod, List<int> arguments)
			{
				Store = store;
				CompilerMethod = compilerMethod;
				Arguments = arguments;
			}

			public int Store { get; }
			public Binder.CompilerMethod CompilerMethod { get; }
			public List<int> Arguments { get; }
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
		}

		public class New : Instruction
		{
			public New(int storeId)
			{
				StoreId = storeId;
			}

			public int StoreId { get; }
		}

		public class Return : Instruction
		{
			public Return(int valueId)
			{
				ValueId = valueId;
			}

			public int ValueId { get; }
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
		}
	}
}
