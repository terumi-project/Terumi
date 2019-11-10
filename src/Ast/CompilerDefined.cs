using System;
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

		// TODO: delete this
		[Obsolete("Please use the newer MatchMethod method")]
		public static MethodBind? MatchMethod(string name, params UserType[] parameters)
		{
			foreach (var func in CompilerFunctions)
			{
				if (func.Name == name)
				{
					if (func.Parameters.Length != parameters.Length) continue;

					bool flag = false;
					for(var i = 0; i < parameters.Length; i++)
					{
						if (parameters[i].Name != func.Parameters[i].Name)
						{
							flag = true;
							break;
						}
					}

					if (flag) continue;

					return new MethodBind
					{
						Name = func.Name,
						Parameters = func.Parameters.Select((i, x) => new MethodBind.Parameter
						{
							Type = new UserType { IsCompilerDefined = true, Name = i.Name },
							Name = $"k{x}"
						}).ToList()
					};
				}
			}

			return null;
		}

		public static CompilerMethod? MatchMethod(string name, params IType[] parameters)
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

		public static CompilerMethod[] CompilerFunctions { get; } = new CompilerMethod[]
		{
			New(Void, "println", String),
			New(Void, "println", Number),
			New(Void, "println", Boolean),
			New(String, "concat", String, String),
			New(Number, "add", Number, Number)
		};

		private static CompilerMethod New(IType returnType, string name, params IType[] parameters)
			=> new CompilerMethod
			{
				Name = name,
				ReturnType = returnType,
				Parameters = parameters
			};
	}
}