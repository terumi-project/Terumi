using Terumi.Tokens;

namespace Terumi.SyntaxTree
{
	public class Method : TerumiMember
	{
		public Method(IdentifierToken type, IdentifierToken identifier, ParameterGroup parameters, CodeBody? body)
		{
			Type = type;
			Identifier = identifier;
			Parameters = parameters;
			Body = body;
		}

		public IdentifierToken Type { get; }
		public IdentifierToken Identifier { get; }
		public ParameterGroup Parameters { get; }
		public CodeBody? Body { get; }
	}
}