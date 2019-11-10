using System;
using System.Collections.Generic;
using System.Linq;

using Terumi.Binder;

namespace Terumi.Ast
{
	public static class CompilerDefined
	{
		public static IType Void { get; } = new CompilerType { Name = "void" };
		public static IType String { get; } = new CompilerType { Name = "string" };
		public static IType Number { get; } = new CompilerType { Name = "number" };
		public static IType Boolean { get; } = new CompilerType { Name = "bool" };

		// TODO: CompilerDefined as an instance that will generate code according to the target language stuff

		public static CompilerMethod[] CompilerFunctions { get; } = new CompilerMethod[]
		{
			New(Void, "println", P(String, "value")),
			New(Void, "println", P(String, "value")),
			New(Void, "println", P(String, "value")),
			New(String, "concat", P(String, "a"), P(String, "b")),
			New(Number, "add", P(Number, "a"), P(Number, "b"))
		};

		private static ParameterBind P(IType type, string name)
			=> new ParameterBind { Type = type, Name = name };

		private static CompilerMethod New(IType returnType, string name, params ParameterBind[] parameters)
			=> new CompilerMethod
			{
				Name = name,
				ReturnType = returnType,
				Parameters = new List<ParameterBind>(parameters)
			};

		// TODO: delete this
		[Obsolete("Please use the newer MatchMethod method")]
		public static MethodBind? MatchMethod(string name, params IType[] parameters)
		{
			foreach (var func in CompilerFunctions)
			{
				if (func.Name == name)
				{
					if (func.Parameters.Count != parameters.Length) continue;

					var f = false;
					for (var i = 0; i < parameters.Length; i++)
						if (func.Parameters[i].Type != parameters[i])
						{
							f = true;
							break;
						}
					if (f) continue;

					return new MethodBind
					{
						Name = func.Name,
						Parameters = func.Parameters.Select((i, x) => new ParameterBind
						{
							Type = new UserType { IsCompilerDefined = true, Name = i.Name },
							Name = $"k{x}"
						}).ToList()
					};
				}
			}

			return null;
		}

		public static CompilerMethod? MatchMethod(string name, params ParameterBind[] parameters)
		{
			foreach (var func in CompilerFunctions)
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