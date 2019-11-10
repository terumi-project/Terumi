using System;
using System.Collections;
using System.Collections.Generic;

namespace Terumi.SyntaxTree
{
	public class ParameterGroup : IEnumerable<Parameter>
	{
		public static ParameterGroup NoParameters { get; } = new ParameterGroup(EmptyList<Parameter>.Instance);

		public ParameterGroup(List<Parameter> parameters)
			=> Parameters = parameters;

		public List<Parameter> Parameters { get; }

		public IEnumerator<Parameter> GetEnumerator() => ((IEnumerable<Parameter>)Parameters).GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<Parameter>)Parameters).GetEnumerator();
	}
}