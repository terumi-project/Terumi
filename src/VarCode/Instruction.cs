using System;
using System.Collections.Generic;
using System.Text;

namespace Terumi.VarCode
{
	public class InstructionMethod
	{
		public InstructionMethod(int id, List<int> parameters, InstructionBody code)
		{
			Parameters = parameters;
			Code = code;
			Id = id;
		}

		public List<int> Parameters { get; }
		public InstructionBody Code { get; }
		public int Id { get; }
	}

	public class InstructionBody
	{
		public InstructionBody(List<Instruction> instructions)
		{
			Instructions = instructions;
		}

		public List<Instruction> Instructions { get; }
	}

	public abstract class Instruction
	{
		public abstract class Assignment : Instruction
		{
			public class Constant : Assignment
			{
				public Constant(object value, int storeId)
				{
					Value = value;
					StoreId = storeId;
				}

				public object Value { get; }
				public int StoreId { get; }
			}

			public class Reference : Assignment
			{
				public Reference(int id, int valueId)
				{
					Id = id;
					ValueId = valueId;
				}

				public int Id { get; }
				public int ValueId { get; }
			}

			public class New : Assignment
			{
				public New(int storeId)
				{
					StoreId = storeId;
				}

				public int StoreId { get; }
			}
		}

		public class SetField : Instruction
		{
			public SetField(int id, int fieldName, int value)
			{
				Id = id;
				FieldName = fieldName;
				Value = value;
			}

			public int Id { get; }
			public int FieldName { get; }
			public int Value { get; }
		}

		public class GetField : Instruction
		{
			public GetField(int id, int fieldName, int storeValue)
			{
				Id = id;
				FieldName = fieldName;
				StoreValue = storeValue;
			}

			public int Id { get; }
			public int FieldName { get; }
			public int StoreValue { get; }
		}

		public class MethodCall : Instruction
		{
			public MethodCall(int result, int method, List<int> parameters)
			{
				Result = result;
				Method = method;
				Parameters = parameters;
			}

			public int Result { get; }
			public int Method { get; }
			public List<int> Parameters { get; }
		}

		public class While : Instruction
		{
			public While(InstructionBody comparison, InstructionBody run, int compareVar)
			{
				Comparison = comparison;
				Run = run;
				CompareVar = compareVar;
			}

			public InstructionBody Comparison { get; }
			public InstructionBody Run { get; }
			public int CompareVar { get; }
		}

		public class If : Instruction
		{
			public If(int compareVar, InstructionBody clause)
			{
				CompareVar = compareVar;
				Clause = clause;
			}

			public int CompareVar { get; }
			public InstructionBody Clause { get; }
		}

		public class Return : Instruction
		{
			public Return(int returnValueId)
			{
				ReturnValueId = returnValueId;
			}

			public int ReturnValueId { get; }
		}
	}
}
