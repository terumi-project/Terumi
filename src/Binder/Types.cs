using System;
using System.Collections.Generic;
using System.Text;

namespace Terumi.Binder
{
	public interface IType
	{
		string TypeName { get; }

		List<Field> Fields { get; }

		List<IMethod> Methods { get; }
	}

	public class Field
	{
		public Field(IType type, string name)
		{
			Type = type;
			Name = name;
		}

		public IType Type { get; }
		public string Name { get; }
	}

	public sealed class BuiltinType : IType
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

		public List<Field> Fields => EmptyList<Field>.Instance;

		public List<IMethod> Methods => EmptyList<IMethod>.Instance;
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

	//

	public class Class : IType
	{
		public Class(Parser.Class fromParser, string name, List<IMethod> methods, List<Field> fields)
		{
			FromParser = fromParser;
			Name = name;
			Methods = methods;
			Fields = fields;
		}

		public Parser.Class FromParser { get; }
		public string Name { get; }
		public List<IMethod> Methods { get; }
		public List<Field> Fields { get; }

		string IType.TypeName => Name;
	}

	public class Method : IMethod
	{
		public Method(IType returnType, string name, List<MethodParameter> parameters, CodeBody body)
		{
			ReturnType = returnType;
			Name = name;
			Parameters = parameters;
			Body = body;

			foreach (var parameter in parameters)
			{
				parameter.Claim(this);
			}
		}

		public IType ReturnType { get; }
		public string Name { get; }
		public List<MethodParameter> Parameters { get; }
		public CodeBody Body { get; }
	}

	public class MethodParameter
	{
		private IMethod? _method;

		public MethodParameter(IType type, string name)
		{
			Type = type;
			Name = name;
		}

		public IMethod Method { get => _method ?? throw new System.InvalidOperationException($"This MethodParameter has not been passed into a Method yet - cannot get the method"); }
		public IType Type { get; }
		public string Name { get; }

		/// <summary>
		/// Used to set the Method property on this parameter to link back to the method this parameter can be found in
		/// </summary>
		/// <param name="claimer"></param>
		public void Claim(IMethod claimer)
		{
			if (_method != null)
			{
				throw new System.InvalidOperationException("This MethodParameter has already been claimed by a method!");
			}

			_method = claimer;
		}

		[System.Obsolete("Using this is a code smell", false)]
		public bool IsClaimed => _method == null;
	}
}
