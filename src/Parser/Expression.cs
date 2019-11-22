using Terumi.Lexer;

namespace Terumi.Parser
{
	public abstract class Expression
	{
		public class Constant : Expression
		{
			public Constant(ConsumedTokens consumed, object value)
			{
				Consumed = consumed;
				Value = value;
			}

			public ConsumedTokens Consumed { get; }
			public object Value { get; }
		}

		public class Reference : Expression
		{
			public Reference(ConsumedTokens consumed, string referenceName)
			{
				Consumed = consumed;
				ReferenceName = referenceName;
			}

			public ConsumedTokens Consumed { get; }
			public string ReferenceName { get; }
		}

		public class Access : Expression
		{
			public Access(ConsumedTokens consumed, Expression left, Expression right)
			{
				Consumed = consumed;
				Left = left;
				Right = right;
			}

			public ConsumedTokens Consumed { get; }
			public Expression Left { get; }
			public Expression Right { get; }
		}

		public class MethodCall : Expression
		{
			// TODO: easier way to copy and paste
			public MethodCall(Statement.MethodCall methodCall)
			{
				MethodCallStatement = methodCall;
			}

			public Statement.MethodCall MethodCallStatement { get; }
		}

		// works as a comparison as well
		public class Binary : Expression
		{
			public Binary(Expression left, TokenType @operator, Expression right)
			{
				Left = left;
				Operator = @operator;
				Right = right;
			}

			public Expression Left { get; }
			public TokenType Operator { get; }
			public Expression Right { get; }
		}

		public class Parenthesized : Expression
		{
			public Parenthesized(Expression inner)
			{
				Inner = inner;
			}

			public Expression Inner { get; }
		}

		public class Increment : Expression
		{
			public enum IncrementSide
			{
				Pre,
				Post
			}

			public Increment(ConsumedTokens consumed, IncrementSide side, TokenType type, Expression expression)
			{
				Consumed = consumed;
				Side = side;
				Type = type;
				Expression = expression;
			}

			public ConsumedTokens Consumed { get; }
			public IncrementSide Side { get; }
			public TokenType Type { get; }
			public Expression Expression { get; }
		}
	}
}