using System.Collections.Generic;

namespace Terumi.Binder
{
	public class CodeBody
	{
		public static CodeBody None { get; } = new CodeBody(EmptyList<Statement>.Instance);

		public CodeBody(List<Statement> statements)
		{
			Statements = statements;
		}

		public List<Statement> Statements { get; }
	}

	public abstract class Statement
	{
		protected Statement(Parser.Statement fromParser) => FromParser = fromParser;

		public Parser.Statement FromParser { get; }

		public class Declaration : Statement
		{
			public Declaration(Parser.Statement.Declaration fromParser, IType type, string name, Expression value) : base(fromParser)
			{
				FromParser = fromParser;
				Type = type;
				Name = name;
				Value = value;
			}

			new public Parser.Statement.Declaration FromParser { get; }
			public IType Type { get; }
			public string Name { get; }
			public Expression Value { get; }
		}

		public class Assignment : Statement
		{
			public Assignment(Parser.Statement.Assignment fromParser, Expression.Assignment assignment) : base(fromParser)
			{
				FromParser = fromParser;
				AssignmentExpression = assignment;
			}

			new public Parser.Statement.Assignment FromParser { get; }
			public Expression.Assignment AssignmentExpression { get; }
		}

		public class MethodCall : Statement
		{
			public MethodCall(Parser.Statement.MethodCall fromParser, Expression.MethodCall methodCall) : base(fromParser)
			{
				FromParser = fromParser;
				MethodCallExpression = methodCall;
			}

			new public Parser.Statement.MethodCall FromParser { get; }
			public Expression.MethodCall MethodCallExpression { get; }
		}

		public class Command : Statement
		{
			// TODO: string interpolation data
			public Command(Parser.Statement.Command fromParser, StringData stringData) : base(fromParser)
			{
				FromParser = fromParser;
				StringData = stringData;
			}

			new public Parser.Statement.Command FromParser { get; }
			public StringData StringData { get; }
		}

		public class If : Statement
		{
			public If(Parser.Statement.If fromParser, Expression comparison, CodeBody trueClause, CodeBody elseClause) : base(fromParser)
			{
				FromParser = fromParser;
				Comparison = comparison;
				TrueClause = trueClause;
				ElseClause = elseClause;
			}

			new public Parser.Statement.If FromParser { get; }
			public Expression Comparison { get; }
			public CodeBody TrueClause { get; }
			public CodeBody ElseClause { get; }
		}

		public class While : Statement
		{
			public While(Parser.Statement.While fromParser, bool isDoWhile, Expression comparison, CodeBody body) : base(fromParser)
			{
				FromParser = fromParser;
				IsDoWhile = isDoWhile;
				Comparison = comparison;
				Body = body;
			}

			new public Parser.Statement.While FromParser { get; }
			public bool IsDoWhile { get; }
			public Expression Comparison { get; }
			public CodeBody Body { get; }
		}

		public class For : Statement
		{
			public For(Parser.Statement.For fromParser, CodeBody initialization, Expression comparison, CodeBody end, CodeBody code) : base(fromParser)
			{
				FromParser = fromParser;
				Initialization = initialization;
				Comparison = comparison;
				End = end;
				Code = code;
			}

			new public Parser.Statement.For FromParser { get; }
			public CodeBody Initialization { get; }
			public Expression Comparison { get; }
			public CodeBody End { get; }
			public CodeBody Code { get; }
		}

		public class Increment : Statement
		{
			public Increment(Parser.Statement.Increment fromParser, Expression.Increment expression) : base(fromParser)
			{
				FromParser = fromParser;
				Expression = expression;
			}

			new public Parser.Statement.Increment FromParser { get; }
			public Expression.Increment Expression { get; }
		}

		public class Return : Statement
		{
			public Return(Parser.Statement.Return fromParser, Expression? value) : base(fromParser)
			{
				FromParser = fromParser;
				Value = value;
			}

			new public Parser.Statement.Return FromParser { get; }
			public Expression? Value { get; }
		}

		public class Access : Statement
		{
			public Access(Parser.Statement.Access fromParser, Expression.Access expression) : base(fromParser)
			{
				FromParser = fromParser;
				Expression = expression;
			}

			new public Parser.Statement.Access FromParser { get; }
			public Expression.Access Expression { get; }
		}
	}

	public abstract class Expression
	{
		protected Expression(Parser.Expression fromParser) => FromParser = fromParser;

		public Parser.Expression FromParser { get; }
		public abstract IType Type { get; }

		public class Assignment : Expression
		{
			public Assignment(Parser.Expression.Assignment fromParser, Expression left, Expression right) : base(fromParser)
			{
				FromParser = fromParser;
				Left = left;
				Right = right;
			}

			public Parser.Expression.Assignment FromParser { get; }
			public override IType Type => Left.Type; // the right will be casted to the left
			public Expression Left { get; }
			public Expression Right { get; }
		}

		public class Constant : Expression
		{
			public Constant(Parser.Expression.Constant fromParser, object value) : base(fromParser)
			{
				FromParser = fromParser;
				Value = value;
			}

			new public Parser.Expression.Constant FromParser { get; }

			// TODO: verify that it can only be one of these three
			public override IType Type => (Value is StringData ? BuiltinType.String : (Value is Number ? BuiltinType.Number : BuiltinType.Boolean));

			public object Value { get; }
		}

		public abstract class Reference : Expression
		{
			protected Reference(Parser.Expression.Reference fromParser) : base(fromParser)
			{
				FromParser = fromParser;
			}

			new public Parser.Expression.Reference FromParser { get; }

			public class Parameter : Reference
			{
				public Parameter(Parser.Expression.Reference fromParser, MethodParameter parameter) : base(fromParser)
				{
					MethodParameter = parameter;
				}

				public override IType Type => MethodParameter.Type;
				public MethodParameter MethodParameter { get; }
			}

			public class Variable : Reference
			{
				public Variable(Parser.Expression.Reference fromParser, Statement.Declaration declaration) : base(fromParser)
				{
					Declaration = declaration;
				}

				public override IType Type => Declaration.Type;
				public Statement.Declaration Declaration { get; }
			}

			public class Field : Reference
			{
				public Field(Parser.Expression.Reference fromParser, Binder.Field field) : base(fromParser)
				{
					FieldDeclaration = field;
				}

				public override IType Type => FieldDeclaration.Type;
				public Binder.Field FieldDeclaration { get; }
			}
		}

		public class Access : Expression
		{
			public Access(Parser.Expression.Access fromParser, Expression left, Expression right) : base(fromParser)
			{
				FromParser = fromParser;
				Left = left;
				Right = right;
			}

			new public Parser.Expression.Access FromParser { get; }
			public override IType Type => Right.Type;
			public Expression Left { get; }
			public Expression Right { get; }
		}

		public class MethodCall : Expression
		{
			public MethodCall(Parser.Expression.MethodCall fromParser, IMethod calling, List<Expression> parameters) : base(fromParser)
			{
				FromParser = fromParser;
				Calling = calling;
				Parameters = parameters;
			}

			new public Parser.Expression.MethodCall FromParser { get; }
			public override IType Type => Calling.ReturnType;
			public IMethod Calling { get; }
			public List<Expression> Parameters { get; }
		}

		// works as a comparison as well
		public class Binary : Expression
		{
			// TODO: binary enum
			public Binary(Parser.Expression.Binary fromParser, Expression left, BinaryExpression @operator, Expression right) : base(fromParser)
			{
				FromParser = fromParser;
				Left = left;
				Operator = @operator;
				Right = right;
			}

			new public Parser.Expression.Binary FromParser { get; }

			// TODO: verify that both sides are the same type
			public override IType Type => Operator.IsComparisonOperator() ? BuiltinType.Boolean : Left.Type;

			public Expression Left { get; }
			public BinaryExpression Operator { get; }
			public Expression Right { get; }
		}

		public class Parenthesized : Expression
		{
			public Parenthesized(Parser.Expression.Parenthesized fromParser, Expression inner) : base(fromParser)
			{
				FromParser = fromParser;
				Inner = inner;
			}

			new public Parser.Expression.Parenthesized FromParser { get; }
			public override IType Type => Inner.Type;
			public Expression Inner { get; }
		}

		public class Increment : Expression
		{
			// TODO: other fields
			public Increment(Parser.Expression.Increment fromParser, Expression expression, IncrementType incrementType) : base(fromParser)
			{
				FromParser = fromParser;
				Expression = expression;
				IncrementType = incrementType;
			}

			new public Parser.Expression.Increment FromParser { get; }
			public override IType Type => Expression.Type; // should be 'Number' anyways
			public Expression Expression { get; }
			public IncrementType IncrementType { get; }
		}

		public class New : Expression
		{
			public New(Parser.Expression.New fromParser, IType target, IMethod constructor, List<Expression> parameters) : base(fromParser)
			{
				Target = target;
				Constructor = constructor;
				Parameters = parameters;
			}

			public override IType Type => Target;
			public IType Target { get; }
			public IMethod Constructor { get; }
			public List<Expression> Parameters { get; }
		}
	}
}