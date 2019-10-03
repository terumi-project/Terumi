using Terumi.Tokens;

namespace Terumi.SyntaxTree
{
	public class ParameterType
	{
		public ParameterType(IdentifierToken typeName, bool array)
		{
			TypeName = typeName;
			Array = array;
		}

		public ParameterType(ParameterType innerParameterType, bool array)
		{
			HasInnerParameterType = true;
			InnerParameterType = innerParameterType;
			Array = array;
		}

		public IdentifierToken TypeName { get; }
		public bool Array { get; }

		public bool HasInnerParameterType { get; }
		public ParameterType InnerParameterType { get; }
	}
}
