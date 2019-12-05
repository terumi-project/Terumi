using System;
using System.Collections.Generic;
using System.Text;

using Body = System.Collections.Generic.List<Terumi.Flattening.Instruction>;

namespace Terumi.Flattening
{
	public class Class
	{
		public Class(string name, Binder.Class boundClass)
		{
			Name = name;
			BoundClass = boundClass;
		}

		public string Name { get; }
		public List<TypedPair> Fields { get; set; } = new List<TypedPair>();
		public Binder.Class BoundClass { get; }
	}

	public class Method
	{
		public Method(string name, Class? owner, Binder.Method? boundMethod)
		{
			Name = name;
			Owner = owner;
			BoundMethod = boundMethod;
		}

		public string Name { get; }
		public Class? Owner { get; }
		public List<TypedPair> Parameters { get; set; } = new List<TypedPair>();
		public Body Body { get; set; } = new Body();
		public Binder.Method? BoundMethod { get; }
	}

	public class TypedPair
	{
		public TypedPair(ObjectType type, string name)
		{
			Type = type;
			Name = name;
		}

		public ObjectType Type { get; }
		public string Name { get; }
	}

	// variable scope is local

	public abstract class Instruction
	{
		public class New : Instruction
		{
			public New(string assignTo)
			{
				AssignTo = assignTo;
			}

			public string AssignTo { get; }
		}

		public class LoadConstant : Instruction
		{
			public LoadConstant(string assignTo, object objectValue)
			{
				AssignTo = assignTo;
				ObjectValue = objectValue;
			}

			public string AssignTo { get; }
			public object ObjectValue { get; }
		}

		public class Assignment : Instruction
		{
			public Assignment(string variableName, string variableValue)
			{
				VariableName = variableName;
				VariableValue = variableValue;
			}

			public string VariableName { get; }
			public string VariableValue { get; }
		}

		public class Reference : Instruction
		{
			public Reference(string resultVariableName, int methodParameterIndex)
			{
				ResultVariableName = resultVariableName;
				MethodParameterIndex = methodParameterIndex;
			}

			public string ResultVariableName { get; }
			public int MethodParameterIndex { get; }
		}

		public class Dereference : Instruction
		{
			public Dereference(string resultVariableName, string targetVariableName, string targetFieldName)
			{
				ResultVariableName = resultVariableName;
				TargetVariableName = targetVariableName;
				TargetFieldName = targetFieldName;
			}

			public string ResultVariableName { get; }
			public string TargetVariableName { get; }
			public string TargetFieldName { get; }
		}

		public class SetField : Instruction
		{
			public SetField(string targetVariableName, string targetFieldName, string newValue)
			{
				TargetVariableName = targetVariableName;
				TargetFieldName = targetFieldName;
				NewValue = newValue;
			}

			public string TargetVariableName { get; }
			public string TargetFieldName { get; }
			public string NewValue { get; }
		}

		public class If : Instruction
		{
			public If(string comparisonVariable, Body trueClause, Body elseClause)
			{
				ComparisonVariable = comparisonVariable;
				TrueClause = trueClause;
				ElseClause = elseClause;
			}

			public string ComparisonVariable { get; }
			public Body TrueClause { get; }
			public Body ElseClause { get; }
		}

		public class While : Instruction
		{
			public While(string comparisonVariable, List<Instruction> body)
			{
				ComparisonVariable = comparisonVariable;
				Body = body;
			}

			public string ComparisonVariable { get; }
			public List<Instruction> Body { get; }
		}

		public class MethodCall : Instruction
		{
			public MethodCall(string? resultVariable, List<string> parameters, Method calling, string? instance = null)
			{
				ResultVariable = resultVariable;
				Parameters = parameters;
				Calling = calling;
				Instance = instance;
			}

			public string? ResultVariable { get; }
			public List<string> Parameters { get; }
			public Method Calling { get; }
			public string? Instance { get; }
		}

		public class CompilerCall : Instruction
		{
			public CompilerCall(string? resultVariable, List<string> parameters, string calling)
			{
				ResultVariable = resultVariable;
				Parameters = parameters;
				Calling = calling;
			}

			public string ResultVariable { get; }
			public List<string> Parameters { get; }
			public string Calling { get; }
		}

		public class Return : Instruction
		{
			public Return(string? returnVariable)
			{
				ReturnVariable = returnVariable;
			}

			public string? ReturnVariable { get; }
		}
	}
}
