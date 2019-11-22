using System.Collections.Generic;
using Terumi.Lexer;

namespace Terumi.Parser
{
	public abstract class Expression
	{
		protected Expression(ConsumedTokens consumed) => Consumed = consumed;
		public ConsumedTokens Consumed { get; }

		public class Constant : Expression
		{
			public Constant(ConsumedTokens consumed, object value) : base(consumed)
			{
				Value = value;
			}

			public object Value { get; }
		}

		public class Reference : Expression
		{
			public Reference(ConsumedTokens consumed, string referenceName) : base(consumed)
			{
				ReferenceName = referenceName;
			}

			public string ReferenceName { get; }
		}

		public class Access : Expression
		{
			public Access(ConsumedTokens consumed, Expression left, Expression right) : base(consumed)
			{
				Left = left;
				Right = right;
			}

			public Expression Left { get; }
			public Expression Right { get; }
		}

		public class MethodCall : Expression
		{
			public MethodCall(ConsumedTokens consumed, bool isCompilerCall, string name, List<Expression> parameters) : base(consumed)
			{
				IsCompilerCall = isCompilerCall;
				Name = name;
				Parameters = parameters;
			}

			public bool IsCompilerCall { get; }
			public string Name { get; }
			public List<Expression> Parameters { get; }
		}

		// works as a comparison as well
		public class Binary : Expression
		{
			public Binary(ConsumedTokens consumed, Expression left, TokenType @operator, Expression right) : base(consumed)
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
			public Parenthesized(ConsumedTokens consumed, Expression inner) : base(consumed)
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

			public Increment(ConsumedTokens consumed, IncrementSide side, TokenType type, Expression expression) : base(consumed)
			{
				Side = side;
				Type = type;
				Expression = expression;
			}

			public IncrementSide Side { get; }
			public TokenType Type { get; }
			public Expression Expression { get; }
		}
	}
}