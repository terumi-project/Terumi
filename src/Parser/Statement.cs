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

		public class MethodCall : Statement
		{
			public MethodCall(ConsumedTokens consumed, bool isCompilerCall, string name, List<Expression> parameters)
			{
				Consumed = consumed;
				IsCompilerCall = isCompilerCall;
				Name = name;
				Parameters = parameters;
			}

			public ConsumedTokens Consumed { get; }
			public bool IsCompilerCall { get; }
			public string Name { get; }
			public List<Expression> Parameters { get; }
		}
	}
}
