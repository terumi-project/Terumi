using System;
using System.Collections.Generic;
using System.Text;

namespace Terumi.Parser
{
	public abstract class Statement
	{
		public class Assignment : Statement
		{
			public Assignment(ConsumedTokens consumedTokens, string? type, string name, object value)
			{
				ConsumedTokens = consumedTokens;
				Type = type;
				Name = name;
				Value = value;
			}

			public ConsumedTokens ConsumedTokens { get; }
			public string Type { get; }
			public string Name { get; }
			public object Value { get; }
		}
	}
}
