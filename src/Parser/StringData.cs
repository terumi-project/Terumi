using System;
using System.Collections.Generic;
using System.Text;

namespace Terumi.Parser
{
	public class StringData
	{
		public class Interpolation
		{
			public Interpolation(Expression expression, int insert)
			{
				Expression = expression;
				Insert = insert;
			}

			public Expression Expression { get; }
			public int Insert { get; }
		}

		public StringData(StringBuilder value, List<Interpolation> interpolations)
		{
			Value = value;
			Interpolations = interpolations;
		}

		public StringBuilder Value { get; }
		public List<Interpolation> Interpolations { get; }
	}
}
