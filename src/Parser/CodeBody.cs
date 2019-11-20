using System;
using System.Collections.Generic;
using System.Text;

namespace Terumi.Parser
{
	public class CodeBody
	{
		public static CodeBody Empty { get; } = new CodeBody(ConsumedTokens.Default, EmptyList<Statement>.Instance);

		public CodeBody(ConsumedTokens consumed, List<Statement> statements)
		{
			Consumed = consumed;
			Statements = statements;
		}

		public ConsumedTokens Consumed { get; }
		public List<Statement> Statements { get; }
	}
}
