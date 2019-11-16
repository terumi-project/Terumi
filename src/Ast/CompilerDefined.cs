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

		public static CompilerMethod? MatchMethod(ICompilerTarget target, string name, params ParameterBind[] parameters)
		{
			return target.MatchMethod(name, parameters.Select(x => x.Type).ToArray());
		}
	}
}