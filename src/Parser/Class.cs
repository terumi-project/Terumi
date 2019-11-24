using System.Collections.Generic;

namespace Terumi.Parser
{
	public class Class
	{
		public Class(ConsumedTokens consumed, string name, List<Method> methods, List<Field> fields)
		{
			Consumed = consumed;
			Name = name;
			Methods = methods;
			Fields = fields;
		}

		public ConsumedTokens Consumed { get; }
		public string Name { get; }
		public List<Method> Methods { get; }
		public List<Field> Fields { get; }
	}

	public class Field
	{
		public Field(ConsumedTokens consumed, string type, string name)
		{
			Consumed = consumed;
			Type = type;
			Name = name;
		}

		public ConsumedTokens Consumed { get; }
		public string Type { get; }
		public string Name { get; }
	}
}