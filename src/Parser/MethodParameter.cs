using System;
using System.Collections.Generic;
using System.Text;

namespace Terumi.Parser
{

	public class MethodParameter
	{
		public MethodParameter(ConsumedTokens consumed, string type, string name)
		{
			Type = type;
			Name = name;
			Consumed = consumed;
		}

		public string Type { get; }
		public string Name { get; }
		public ConsumedTokens Consumed { get; }
	}
}
