using System.Collections.Generic;

namespace Terumi.Parser
{
	public class Class
	{
		public Class(ConsumedTokens consumed, string name, List<Method> methods)
		{
			Consumed = consumed;
			Name = name;
			Methods = methods;
		}

		public ConsumedTokens Consumed { get; }
		public string Name { get; }
		public List<Method> Methods { get; }
	}
}