using System.Collections.Generic;
using Terumi.Lexer;

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

	public class MethodParameter
	{
		public MethodParameter(ConsumedTokens consumed, string type, string name)
		{
			Type = type;
			Name = name;
			Consumed = consumed;
		}

		public string Type { get; }
		public string Name { get; }
		public ConsumedTokens Consumed { get; }
	}

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

	public abstract class Statement
	{
		protected Statement(ConsumedTokens consumed) => Consumed = consumed;
		public ConsumedTokens Consumed { get; }

		public class Declaration : Statement
		{
			public Declaration(ConsumedTokens consumed, string? type, string name, Expression? value) : base(consumed)
			{
				Type = type;
				Name = name;
				Value = value;
			}

			public string? Type { get; }
			public string Name { get; }
			public Expression Value { get; }
		}

		public class Assignment : Statement
		{
			public Assignment(ConsumedTokens consumed, Expression.Assignment assignment) : base(consumed)
			{
				Expression = assignment;
			}

			public Expression.Assignment Expression { get; }
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
			public Return(ConsumedTokens consumed, Expression? expr) : base(consumed)
			{
				Expression = expr;
			}

			public Expression? Expression { get; }
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

	public abstract class Expression
	{
		protected Expression(ConsumedTokens consumed) => Consumed = consumed;
		public ConsumedTokens Consumed { get; }

		public class Assignment : Expression
		{
			public Assignment(ConsumedTokens consumed, Expression reference, Expression right) : base(consumed)
			{
				Left = reference;
				Right = right;
			}

			public Expression Left { get; }
			public Expression Right { get; }
		}

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
				Arguments = parameters;
			}

			public bool IsCompilerCall { get; }
			public string Name { get; }
			public List<Expression> Arguments { get; }
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

		public class Unary : Expression
		{
			public Unary(ConsumedTokens consumed, TokenType @operator, Expression operand) : base(consumed)
			{
				Operator = @operator;
				Operand = operand;
			}

			public TokenType Operator { get; }
			public Expression Operand { get; }
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

		public class New : Expression
		{
			public New(ConsumedTokens consumed, string type, List<Expression> expressions) : base(consumed)
			{
				Type = type;
				Expressions = expressions;
			}

			public string Type { get; }
			public List<Expression> Expressions { get; }
		}
	}
}