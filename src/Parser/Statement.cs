namespace Terumi.Parser
{
	public abstract class Statement
	{
		protected Statement(ConsumedTokens consumed) => Consumed = consumed;
		public ConsumedTokens Consumed { get; }

		public class Assignment : Statement
		{
			public Assignment(ConsumedTokens consumed, string? type, string name, Expression value) : base(consumed)
			{
				Type = type;
				Name = name;
				Value = value;
			}

			public string Type { get; }
			public string Name { get; }
			public Expression Value { get; }
		}

		public class MethodCall : Statement
		{
			public MethodCall(ConsumedTokens consumed, Expression.MethodCall methodCall) : base(consumed)
			{
				MethodCallExpression = methodCall;
			}

			public Expression.MethodCall MethodCallExpression { get; }
		}

		public class Command : Statement
		{
			public Command(ConsumedTokens consumed, StringData @string) : base(consumed)
			{
				String = @string;
			}

			public StringData String { get; }
		}

		public class If : Statement
		{
			public If(ConsumedTokens consumed, Expression comparison, CodeBody ifClause, CodeBody elseClause) : base(consumed)
			{
				Comparison = comparison;
				IfClause = ifClause;
				ElseClause = elseClause;
			}

			public Expression Comparison { get; }
			public CodeBody IfClause { get; }
			public CodeBody ElseClause { get; }
		}

		public class While : Statement
		{
			public While(ConsumedTokens consumed, Expression comparison, CodeBody statements, bool isDoWhile = false) : base(consumed)
			{
				Comparison = comparison;
				Statements = statements;
				IsDoWhile = isDoWhile;
			}

			public Expression Comparison { get; }
			public CodeBody Statements { get; }
			public bool IsDoWhile { get; }
		}

		public class For : Statement
		{
			public For(ConsumedTokens consumed, CodeBody declaration, Expression comparison, CodeBody end, CodeBody statements) : base(consumed)
			{
				Declaration = declaration;
				Comparison = comparison;
				End = end;
				Statements = statements;
			}

			public CodeBody Declaration { get; }
			public Expression Comparison { get; }
			public CodeBody End { get; }
			public CodeBody Statements { get; }
		}

		public class Increment : Statement
		{
			public Increment(ConsumedTokens consumed, Expression.Increment incExpr) : base(consumed)
			{
				IncrementExpression = incExpr;
			}

			public Expression.Increment IncrementExpression { get; }
		}

		public class Return : Statement
		{
			public Return(ConsumedTokens consumed, Expression expr) : base(consumed)
			{
				Expression = expr;
			}

			public Expression Expression { get; }
		}

		public class Access : Statement
		{
			public Access(ConsumedTokens consumed, Expression.Access access) : base(consumed)
			{
				AccessExpression = access;
			}

			public Expression.Access AccessExpression { get; }
		}
	}
}
