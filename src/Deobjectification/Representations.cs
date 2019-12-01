using System.Collections.Generic;
using System.Text;

namespace Terumi.Deobjectification
{
	public enum ObjectType
	{
		Void,
		String,
		Number,
		Boolean,
		GlobalObject,
	}

	public static class DeobjectificationConstants
	{
		public const string GlobalObjectType = "obj<>_type_";

		public static string UniqueName(Method method)
		{
			var unique = new StringBuilder();

			unique.Append('<')
					.Append('<')
					.Append(method.UniqueThing)
					.Append('>')
				.Append(method.InClass ?? "")
				.Append('>');

			unique.Append('_')
				.Append(method.ReturnType)
				.Append('_')
				.Append(method.Name)
				.Append('_');

			foreach (var p in method.Parameters)
			{
				unique.Append(p)
					.Append('_');
			}

			return unique.ToString();
		}

		public static string MethodParameter(Method method, int parameterIndex) => $"<{UniqueName(method)}>_parameter_{parameterIndex}_";

		public static string MethodVariableName(Method method, string newVarName) => $"<{UniqueName(method)}>_variable_{newVarName}_";
	}

	public class GlobalObjectInfo
	{
		public Dictionary<string, ObjectType> Fields { get; } = new Dictionary<string, ObjectType>();
		public Dictionary<Binder.Class, string> Types { get; } = new Dictionary<Binder.Class, string>();
	}

	public class Method
	{
		public Method(string uniqueThing, string name, ObjectType returnType, List<ObjectType> parameters, string? inClass = null)
		{
			Name = name;
			Parameters = parameters;
			Instructions = new List<Instruction>();
			ReturnType = returnType;
			InClass = inClass;
			UniqueThing = uniqueThing;
		}

		public string Name { get; }
		public ObjectType ReturnType { get; }
		public List<ObjectType> Parameters { get; }
		public List<Instruction> Instructions { get; }
		public string? InClass { get; }
		public string UniqueThing { get; }
	}

	// variable scope is global

	public abstract class Instruction
	{
		public class Constant : Instruction
		{
			public Constant(object value)
			{
				Value = value;
			}

			public object Value { get; }
		}

		public class Assignment : Instruction
		{
			public Assignment(string variableName, Instruction value)
			{
				VariableName = variableName;
				Value = value;
			}

			public string VariableName { get; }
			public Instruction Value { get; }
		}

		public class Reference : Instruction
		{
			public Reference(Instruction? reference, string access)
			{
				ReferenceI = reference;
				Access = access;
			}

			public Instruction? ReferenceI { get; }
			public string Access { get; }
		}

		public class MethodCall : Instruction
		{
			public MethodCall(Method calling, List<Instruction> arguments)
			{
				Calling = calling;
				Arguments = arguments;
			}

			public Method Calling { get; }
			public List<Instruction> Arguments { get; }
		}

		public class CompilerCall : Instruction
		{
			public CompilerCall(string methodName, List<Instruction> arguments)
			{
				MethodName = methodName;
				Arguments = arguments;
			}

			public string MethodName { get; }
			public List<Instruction> Arguments { get; }
		}

		public class Return : Instruction
		{
			public Return(Instruction? value = null)
			{
				Value = value;
			}

			public Instruction? Value { get; }
		}

		public class While : Instruction
		{
			public While(Instruction condition, List<Instruction> code)
			{
				Condition = condition;
				Code = code;
			}

			public Instruction Condition { get; }
			public List<Instruction> Code { get; }
		}

		public class If : Instruction
		{
			public If(Instruction condition, List<Instruction> clause)
			{
				Condition = condition;
				Clause = clause;
			}

			public Instruction Condition { get; }
			public List<Instruction> Clause { get; }
		}

		public class New : Instruction
		{
			public New(string objectType)
			{
				ObjectType = objectType;
			}

			public string ObjectType { get; }
		}
	}
}
