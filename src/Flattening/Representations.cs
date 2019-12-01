using System;
using System.Collections.Generic;
using System.Text;

using Body = System.Collections.Generic.List<Terumi.Flattening.Instruction>;

namespace Terumi.Flattening
{
	public enum Type
	{
		Void,
		String,
		Number,
		Boolean,

		// blanket 'object' type, because in the deobjectification step
		// everything becomes a single object anyways
		Object,
	}

	public class Class
	{
		public Class(string name)
		{
			Name = name;
		}

		public string Name { get; }
		public List<TypedPair> Fields { get; set; } = new List<TypedPair>();
	}

	public class Method
	{
		public Method(string name, Class? owner = null)
		{
			Name = name;
			Owner = owner;
		}

		public string Name { get; }
		public Class? Owner { get; }
		public List<TypedPair> Parameters { get; set; } = new List<TypedPair>();
		public Body Body { get; set; } = new Body();
	}

	public class TypedPair
	{
		public TypedPair(Type type, string name)
		{
			Type = type;
			Name = name;
		}

		public Type Type { get; }
		public string Name { get; }
	}

	// variable scope is local

	public abstract class Instruction
	{
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
			public MethodCall(string? resultVariable, List<string> parameters, Method calling)
			{
				ResultVariable = resultVariable;
				Parameters = parameters;
				Calling = calling;
			}

			public string? ResultVariable { get; }
			public List<string> Parameters { get; }
			public Method Calling { get; }
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
	}
}
