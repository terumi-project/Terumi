using System;
using System.Collections.Generic;
using System.Linq;

using Terumi.Binder;
using Terumi.Targets;

namespace Terumi.Ast
{
	public static class CompilerDefined
	{
		public static IType Void { get; } = new CompilerType { Name = "void" };
		public static IType String { get; } = new CompilerType { Name = "string" };
		public static IType Number { get; } = new CompilerType { Name = "number" };
		public static IType Boolean { get; } = new CompilerType { Name = "bool" };

		// TODO: CompilerDefined as an instance that will generate code according to the target language stuff

		public static CompilerMethod[] CompilerFunctions(ICompilerMethods target)
			=> new CompilerMethod[]
		{
			New(args => target.supports_string(args[0]),
				Boolean, "supports",
				P(String, "feature")),

			New(args => target.println_string(args[0]),
				Void, "println",
				P(String,  "value")),

			New(args => target.println_number(args[0]),
				Void, "println",
				P(Number,  "value")),

			New(args => target.println_bool(args[0]),
				Void, "println",
				P(Boolean, "value")),

			New(args => target.concat_string_string(args[0], args[1]),
				String, "concat",
				P(String,  "a"),
				P(String, "b")),

			New(args => target.add_number_number(args[0], args[1]),
				Number, "add",
				P(Number,  "a"),
				P(Number, "b"))
		};

		private static ParameterBind P(IType type, string name)
			=> new ParameterBind { Type = type, Name = name };

		private static CompilerMethod New(Func<List<string>, string> generate, IType returnType, string name, params ParameterBind[] parameters)
			=> new CompilerMethod
			{
				Name = name,
				ReturnType = returnType,
				Parameters = new List<ParameterBind>(parameters),
				Generate = generate
			};

		public static CompilerMethod? MatchMethod(ICompilerMethods target, string name, params ParameterBind[] parameters)
		{
			foreach (var func in CompilerFunctions(target))
			{
				if (func.Name == name
					&& func.Parameters.SequenceEqual(parameters))
				{
					return func;
				}
			}

			return null;
		}
	}
}