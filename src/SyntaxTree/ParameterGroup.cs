using System;
using System.Collections;
using System.Collections.Generic;

namespace Terumi.SyntaxTree
{
	public class ParameterGroup : IEnumerable<Parameter>
	{
		public static ParameterGroup NoParameters { get; } = new ParameterGroup(Array.Empty<Parameter>());

		public ParameterGroup(List<Parameter> parameters) : this(parameters.ToArray())
		{
		}

		public ParameterGroup(Parameter[] parameters)
			=> Parameters = parameters;

		public Parameter[] Parameters { get; }

		public IEnumerator<Parameter> GetEnumerator() => ((IEnumerable<Parameter>)Parameters).GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<Parameter>)Parameters).GetEnumerator();
	}
}