using System;
using System.Collections.Generic;
using System.Text;

namespace Terumi.Parser
{
	public class Method
	{
		public Method(ConsumedTokens consumed, string? type, string name, List<MethodParameter> parameters, CodeBody code)
		{
			Type = type;
			Name = name;
			Parameters = parameters;
			Code = code;
			Consumed = consumed;
		}

		public string? Type { get; }
		public string Name { get; }
		public List<MethodParameter> Parameters { get; }
		public CodeBody Code { get; }
		public ConsumedTokens Consumed { get; }
	}
}
