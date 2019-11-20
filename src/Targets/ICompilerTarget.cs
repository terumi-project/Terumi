/*
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;

namespace Terumi.Targets
{
	public interface ICompilerTarget
	{
		CompilerMethod? MatchMethod(string name, params IType[] parameters);

		void Write(IndentedTextWriter writer, VarCodeStore store);
	}

	public enum CompilerOperators
	{
		Not,
		Equals,
		NotEquals,
		LessThan,
		GreaterThan,
		LessThanOrEqualTo,
		GreaterThanOrEqualTo
	}

	public static class CompilerTargetExtensions
	{
		public static CompilerMethod Operator(this ICompilerTarget target, CompilerOperators @operator, params IType[] parameters)
		{
			var compilerMethod = target.MatchMethod($"op_{@operator}", parameters);

			if (compilerMethod == null)
			{
				throw new NotSupportedException($"Incomplete compiler target '{target.GetType().FullName}' - doesn't implement operator {@operator}.");
			}

			return compilerMethod;
		}
	}
}
*/