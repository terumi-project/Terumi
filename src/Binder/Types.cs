using System;
using System.Collections.Generic;
using System.Text;

namespace Terumi.Binder
{
	public interface IType
	{
		string TypeName { get; }
	}

	public class BuiltinType : IType
	{
		public static IType Void { get; } = new BuiltinType("void");
		public static IType String { get; } = new BuiltinType("string");
		public static IType Number { get; } = new BuiltinType("number");
		public static IType Boolean { get; } = new BuiltinType("bool");

		private BuiltinType(string name)
		{
			TypeName = name;
		}

		public string TypeName { get; }
	}

	public interface IMethod
	{
		IType ReturnType { get; }

		string Name { get; }

		List<MethodParameter> Parameters { get; }
	}

	public class CompilerMethod : IMethod
	{
		public CompilerMethod(IType returnType, string name, List<MethodParameter> parameters)
		{
			ReturnType = returnType;
			Name = name;
			Parameters = parameters;
		}

		public IType ReturnType { get; }
		public string Name { get; }
		public List<MethodParameter> Parameters { get; }
	}
}
