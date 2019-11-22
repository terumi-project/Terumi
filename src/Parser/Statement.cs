using System;
using System.Collections.Generic;
using System.Text;
using Terumi.Lexer;

namespace Terumi.Parser
{
	public abstract class Statement
	{
		public class Assignment : Statement
		{
			public Assignment(ConsumedTokens consumedTokens, string? type, string name, Expression value)
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

		public class Command : Statement
		{
			public Command(ConsumedTokens consumed, StringData @string)
			{
				Consumed = consumed;
				String = @string;
			}

			public ConsumedTokens Consumed { get; }
			public StringData String { get; }
		}

		public class If : Statement
		{
			public If(ConsumedTokens consumed, Expression comparison, CodeBody ifClause, CodeBody elseClause)
			{
				Consumed = consumed;
				Comparison = comparison;
				IfClause = ifClause;
				ElseClause = elseClause;
			}

			public ConsumedTokens Consumed { get; }
			public Expression Comparison { get; }
			public CodeBody IfClause { get; }
			public CodeBody ElseClause { get; }
		}

		public class While : Statement
		{
			public While(ConsumedTokens consumed, Expression comparison, CodeBody statements, bool isDoWhile = false)
			{
				Consumed = consumed;
				Comparison = comparison;
				Statements = statements;
				IsDoWhile = isDoWhile;
			}

			public ConsumedTokens Consumed { get; }
			public Expression Comparison { get; }
			public CodeBody Statements { get; }
			public bool IsDoWhile { get; }
		}

		public class For : Statement
		{
			public For(ConsumedTokens consumed, CodeBody declaration, Expression comparison, CodeBody end, CodeBody statements)
			{
				Consumed = consumed;
				Declaration = declaration;
				Comparison = comparison;
				End = end;
				Statements = statements;
			}

			public ConsumedTokens Consumed { get; }
			public CodeBody Declaration { get; }
			public Expression Comparison { get; }
			public CodeBody End { get; }
			public CodeBody Statements { get; }
		}

		public class Increment : Statement
		{
			public Increment(ConsumedTokens consumed, Expression.Increment incExpr)
			{
				Consumed = consumed;
				IncrementExpression = incExpr;
			}

			public ConsumedTokens Consumed { get; }
			public Expression.Increment IncrementExpression { get; }
		}

		public class Return : Statement
		{
			public Return(ConsumedTokens consumed, Expression expr)
			{
				Consumed = consumed;
				Expression = expr;
			}

			public ConsumedTokens Consumed { get; }
			public Expression Expression { get; }
		}

		public class Access : Statement
		{
			public Access(ConsumedTokens consumed, Expression.Access access)
			{
				Consumed = consumed;
				AccessExpression = access;
			}

			public ConsumedTokens Consumed { get; }
			public Expression.Access AccessExpression { get; }
		}
	}
}
