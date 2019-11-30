using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Terumi.Lexer;

namespace Terumi.Parser
{
	public struct ConsumedTokens
	{
		public static ConsumedTokens Default { get; } = new ConsumedTokens(default);

		public ConsumedTokens(ReadOnlyMemory<Token> tokens)
		{
			Tokens = tokens;
		}

		public ReadOnlyMemory<Token> Tokens { get; }
	}

	public enum ContextualState
	{
		Read,
		JustValue,
		FailedRead
	}

	public struct Contextual<T>
	{
		public Contextual(T value)
		{
			Tokens = new ReadOnlyMemory<Token>();
			Value = value;
			Success = ContextualState.JustValue;
		}

		public Contextual(ReadOnlyMemory<Token> tokens, T value)
		{
			Tokens = tokens;
			Value = value;
			Success = ContextualState.Read;
		}

		public ReadOnlyMemory<Token> Tokens { get; }
		public T Value { get; }
		public ContextualState Success { get; private set; }

		public static Contextual<T> Fail() => new Contextual<T> { Success = ContextualState.FailedRead };
		public static implicit operator Contextual<T>(T value) => new Contextual<T>(value);
		public static implicit operator ConsumedTokens(Contextual<T> ctx) => new ConsumedTokens(ctx.Tokens);
	}

	public class TerumiParser
	{
#region not parsing related
		public const MethodImplOptions MaxOpt = MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization;

		private readonly ReadOnlyMemory<Token> _tokens;

		private int _i;
		private Token _token;
		private readonly string _filePath;

		private Token _current
		{
			get
			{
				if (_token == null)
				{
					Debug.Assert(_i == _tokens.Length, "Token should be null at end of stream");
					Error("Didn't expect EOF so soon", _i - 1);
				}

				return _token;
			}
		}

		private TokenType _type => _current.Type;

		public TerumiParser(ReadOnlyMemory<Token> tokens, string filePath)
		{
			_tokens = tokens;
			Set(_i);
			_filePath = filePath;
		}

		private struct ContextualInit
		{
			private readonly ReadOnlyMemory<Token> _tokens;
			private readonly int _start;

			public ContextualInit(ReadOnlyMemory<Token> tokens, int start)
			{
				_tokens = tokens;
				_start = start;
			}

			public int Start => _start;

			public Contextual<T> Make<T>(T value, int current)
				=> new Contextual<T>(_tokens.Slice(_start, current - _start), value);
		}
#endregion

		public SourceFile ConsumeSourceFile(PackageLevel defaultLevel)
		{
			var ctx = Init();

			// package x.y.z at the top
			var package = ReadPackage(defaultLevel);
			var packages = new List<Contextual<Contextual<PackageLevel>>>();

			var use = ReadUse();

			while (use.Success != ContextualState.FailedRead)
			{
				packages.Add(use);
				use = ReadUse();
			}

			ConsumeAllWhitespace();

			if (AtEnd())
			{
				// no methods/classes... weird
				return new SourceFile(_filePath, Make(ctx), package.Value.Value);
			}

			var methods = new List<Method>();
			var classes = new List<Class>();

			while (!AtEnd())
			{
				if (_type == TokenType.Class)
				{
					classes.Add(ReadClass());
				}
				else
				{
					methods.Add(ReadMethod());
				}

				if (AtEnd()) break;

				ConsumeAllWhitespace();
			}

			return new SourceFile(_filePath, Make(ctx), package.Value.Value, packages.Select(x => x.Value.Value).ToList(), methods, classes);
		}

#region top of the file
		public Contextual<Contextual<PackageLevel>> ReadPackage(PackageLevel defaultLevel)
		{
			var ctx = Init();

			ConsumeAllWhitespace();
			if (_type == TokenType.Package)
			{
				NextSignificant();
				var level = PackageLevel();
				UntilNewline("package level");
				return Make(ctx, level);
			}

			return Make(ctx, defaultLevel);
		}

		public Contextual<Contextual<PackageLevel>> ReadUse()
		{
			var ctx = Init();

			ConsumeAllWhitespace();
			if (_type == TokenType.Use)
			{
				NextSignificant();
				var level = PackageLevel();
				UntilNewline("package level");
				return Make(ctx, level);
			}

			return Fail<Contextual<PackageLevel>>(ctx);
		}

		public Contextual<PackageLevel> PackageLevel()
		{
			var ctx = Init();

			// <> a.b.c <>

			// -->a.b.c <>
			ConsumeAllWhitespace();

			if (_type != TokenType.IdentifierToken)
			{
				Error("Expected an identifier at a package level");
			}

			var levels = new List<string>();

			levels.Add(_current.Value<string>());

			// -->.b.c <>
			Next();

			while (_type == TokenType.Dot)
			{
				// -->b.c
				Next();

				if (_type != TokenType.IdentifierToken)
				{
					Error("Expected an identifier at a package level");
				}

				levels.Add(_current.Value<string>());

				// -->.c
				Next();

				// next loop:
				// -->c <>
				// --><>
			}

			// --><>

			return Make(ctx, new PackageLevel(levels));
		}
#endregion

#region classes
		public Class ReadClass()
		{
			// -->class AyyLmao {
			var ctx = Init();

			if (_type != TokenType.Class)
			{
				Error("Expected 'class' keyword");
			}

			// -->AyyLmao {
			NextSignificant();

			if (_type != TokenType.IdentifierToken)
			{
				Error("Expected identifier for class name");
			}

			var name = _current.Value<string>();

			// -->{
			NextSignificant();

			if (_type != TokenType.OpenBrace)
			{
				Error("Expected open brace to signify opening of class");
			}

			// --><>} (class body)
			NextSignificant(); // read {

			var methods = new List<Method>();
			var fields = new List<Field>();

			while (_type != TokenType.CloseBrace)
			{
				if (TryField(out var field))
				{
					fields.Add(field);
				}
				else
				{
					methods.Add(ReadMethod());
				}

				ConsumeAllWhitespace();
			}

			// -->
			NextSignificant(); // read }

			return new Class(Make(ctx), name, methods, fields);
		}

		public bool TryField(out Field field)
		{
			var ctx = Init();

			// readonly keyword found, MUST be a field
			// -->readonly string a
			if (_type == TokenType.Readonly)
			{
				// -->string a
				NextSignificant();
				// must be field

				// -->
				var header = ReadTypeAndNameOptional();

				if (header.Value.Type == null)
				{
					Error("Expected type for field");
				}

				UntilNewline("readonly field");
				field = new Field(Make(ctx), header.Value.Type, header.Value.Name);

				return true;
			}
			else
			{
				// -->string a

				// -->
				var header = ReadTypeAndNameOptional();
				ConsumeWhitespace();

				// if there's no type, it's obviously a method
				if (header.Value.Type == null)
				{
					// if there isn't an open parenthesis, it's not a method
					if (_type != TokenType.OpenParen)
					{
						Error("Expected type for field");
					}

					field = default;
					Fail<Field>(ctx);
					return false;
				}

				// it's a method, there's an open paren
				if (_type == TokenType.OpenParen)
				{
					field = default;
					Fail<Field>(ctx);
					return false;
				}

				// it's a field!
				UntilNewline("field");
				field = new Field(Make(ctx), header.Value.Type, header.Value.Name);
				return true;
			}
		}
#endregion

#region methods
		public Method ReadMethod()
		{
			var ctx = Init();

			var methodHeader = ReadTypeAndNameOptional();

			ConsumeAllWhitespace();
			if (_type != TokenType.OpenParen)
			{
				Error("Expected open parenthesis to signify method parameter group");
			}

			NextSignificant(); // read (
			var parameters = ReadMethodParameterGroup();
			NextSignificant(); // read )

			var body = ReadCodeBody();

			return new Method(Make(ctx), methodHeader.Value.Type, methodHeader.Value.Name, parameters.Value, body);
		}

		public Contextual<List<MethodParameter>> ReadMethodParameterGroup()
		{
			var ctx = Init();

			if (_type != TokenType.IdentifierToken)
			{
				return Make(ctx, EmptyList<MethodParameter>.Instance);
			}

			var parameters = new List<MethodParameter>();

		LOOP:
			var read = ReadTypeAndNameOptional();

			if (read.Value.Item1 == null)
			{
				Error("Expected type and name for method parameter");
			}

			parameters.Add(new MethodParameter(read, (string)read.Value.Type, read.Value.Name));

			if (_type == TokenType.Comma)
			{
				NextSignificant();
				goto LOOP;
			}

			return Make(ctx, parameters);
		}

		public CodeBody ReadCodeBody()
		{
			var ctx = Init();

			if (_type != TokenType.OpenBrace)
			{
				// try to read one statement
				var stmt = ReadStatement();
				// single line statements don't need to end in a newline

				var stmts = new List<Statement> { stmt };

				var contextualStmts = Make(ctx, stmts);
				return new CodeBody(contextualStmts, stmts);
			}
			else
			{
				NextSignificant();

				var stmts = new List<Statement>();

				while (_type != TokenType.CloseBrace)
				{
					stmts.Add(ReadStatement());
					NextUntilNewline("Expected a newline at end of statement");
					ConsumeAllWhitespace();
				}

				// read }
				Next();

				var contextualStmts = Make(ctx, stmts);
				return new CodeBody(contextualStmts, stmts);
			}
		}
#endregion

#region statements
		public Statement ReadStatement()
		{
			var ctx = Init();
			ConsumeAllWhitespace();

#region conditionals
			var @if = ReadIf();

			if (@if != null)
			{
				return Finish(@if);
			}

			var @for = ReadFor();

			if (@for != null)
			{
				return Finish(@for);
			}

			var @while = ReadWhile();

			if (@while != null)
			{
				return Finish(@while);
			}
#endregion

			var @return = ReadReturn();

			if (@return != null)
			{
				return Finish(@return);
			}

			// expression based statements
			var a = ReadAssignmentStmt();

			if (a != null)
			{
				return Finish(a);
			}

			var decl = ReadDeclaration();

			if (decl != null)
			{
				return Finish(decl);
			}

			var ac = ReadAccessStmt();

			if (ac != null)
			{
				return Finish(ac);
			}

			var mc = ReadMethodCallStmt();

			if (mc != null)
			{
				return Finish(mc);
			}

			var inc = ReadIncrementStmt();

			if (inc != null)
			{
				return Finish(inc);
			}

			var cmd = ReadCommandStmt();

			if (cmd != null)
			{
				return Finish(cmd);
			}

			Error("Couldn't parse statement");
			return null;

			Statement Finish(Statement result)
			{
				// UntilNewline("statement");
				Make(ctx);
				return result;
			}
		}

		public Statement.If? ReadIf()
		{
			var ctx = Init();

			if (_type != TokenType.If)
			{
				Fail(ctx);
				return null;
			}

			NextSignificant();
			var comparison = ReadExpression();

			ConsumeAllWhitespace();
			var body = ReadCodeBody();

			ConsumeAllWhitespace();
			if (_type == TokenType.Else)
			{
				NextSignificant();

				var elseClause = ReadCodeBody();

				BackToFirstWhitespace();
				return new Statement.If(Make(ctx), comparison, body, elseClause);
			}

			BackToFirstWhitespace();
			return new Statement.If(Make(ctx), comparison, body, CodeBody.Empty);
		}

		public Statement.While? ReadWhile()
		{
			var ctx = Init();
			ConsumeAllWhitespace();

			if (_type == TokenType.Do)
			{
				NextSignificant();
				var statements = ReadCodeBody();
				ConsumeAllWhitespace();

				if (_type != TokenType.While)
				{
					Error("Expected 'while' at end of do while body");
				}

				NextSignificant();

				var comparison = ReadExpression();

				return new Statement.While(Make(ctx), comparison, statements, true);
			}

			if (_type == TokenType.While)
			{
				NextSignificant();
				var comparison = ReadExpression();
				ConsumeAllWhitespace();
				var statements = ReadCodeBody();

				return new Statement.While(Make(ctx), comparison, statements, false);
			}

			Fail(ctx);
			return null;
		}

		public Statement.For? ReadFor()
		{
			var ctx = Init();
			ConsumeAllWhitespace();

			if (_type != TokenType.For)
			{
				Fail(ctx);
				return null;
			}

			NextSignificant();

			if (_type != TokenType.OpenParen)
			{
				Error("Expected open parenthesis");
			}

			NextSignificant();

			var init = ReadCodeBody();

			ConsumeAllWhitespace();

			if (_type != TokenType.Semicolon)
			{
				Error("Expected semicolon");
			}

			NextSignificant();

			var comparison = ReadExpression();

			ConsumeAllWhitespace();

			if (_type != TokenType.Semicolon)
			{
				Error("Expected semicolon");
			}

			NextSignificant();

			var additional = ReadCodeBody();

			ConsumeAllWhitespace();

			if (_type != TokenType.CloseParen)
			{
				Error("Expected closing parenthesis");
			}

			NextSignificant();

			var body = ReadCodeBody();

			return new Statement.For(Make(ctx), init, comparison, additional, body);
		}

		public Statement.Return? ReadReturn()
		{
			var ctx = Init();

			if (_type == TokenType.Return)
			{
				Next();
				var value = TryReadExpression();

				return new Statement.Return(Make(ctx), value);
			}

			Fail(ctx);
			return null;
		}

		public Statement.Declaration? ReadDeclaration()
		{
			var ctx = Init();
			var declarationHeader = ReadTypeAndNameOptional(false);

			if (declarationHeader.Success == ContextualState.FailedRead)
			{
				Fail(ctx);
				return null;
			}

			ConsumeAllWhitespace();

			if (_type != TokenType.Assignment)
			{
				Fail(ctx);
				return null;
			}

			NextSignificant();

			Expression? expr;
			if (_type == TokenType.Semicolon)
			{
				expr = null;
				// no value
			}
			else
			{
				expr = ReadExpression();
			}

			return new Statement.Declaration(Make(ctx), declarationHeader.Value.Type, declarationHeader.Value.Name, expr);
		}

		public Statement.Assignment? ReadAssignmentStmt()
		{
			var ctx = Init();
			var expr = TryReadExpression();

			if (!(expr is Expression.Assignment assignment))
			{
				Fail(ctx);
				return null;
			}

			return new Statement.Assignment(Make(ctx), assignment);
		}

		public Statement.MethodCall? ReadMethodCallStmt()
		{
			var ctx = Init();
			var expr = TryReadExpression();

			if (!(expr is Expression.MethodCall methodCall))
			{
				Fail(ctx);
				return null;
			}

			return new Statement.MethodCall(Make(ctx), methodCall);
		}

		public Statement.Access? ReadAccessStmt()
		{
			var ctx = Init();
			var expr = TryReadExpression();

			if (!(expr is Expression.Access access))
			{
				Fail(ctx);
				return null;
			}

			return new Statement.Access(Make(ctx), access);
		}

		public Statement.Increment? ReadIncrementStmt()
		{
			var ctx = Init();
			var expr = TryReadExpression();

			if (!(expr is Expression.Increment increment))
			{
				Fail(ctx);
				return null;
			}

			return new Statement.Increment(Make(ctx), increment);
		}

		public Statement.Command? ReadCommandStmt()
		{
			var ctx = Init();

			if (_type != TokenType.CommandToken)
			{
				return null;
			}
			var strData = _current.Value<Lexer.StringData>();
			Next();

			return new Statement.Command(Make(ctx), UpdateStringData(strData));
		}
		#endregion

#region expressions
		public Expression ReadExpression()
		{
			var ctx = Init();

			var expr = TryReadExpression();

			if (expr == null)
			{
				Fail(ctx);
				ConsumeAllWhitespace();
				Error("Expected expression, couldn't parse one");
			}

			return expr;
		}

		public Expression? TryReadExpression()
		{
			var ctx = Init();
			var expr = Assignment();

			if (expr == null)
			{
				Fail(ctx);
				return null;
			}

			BackToFirstWhitespace();
			Make(ctx);
			return expr;
		}

#region precedence based expressions

		/*
		 * LOWEST:
		 * ^
		 * | assignment =
		 * | or ||
		 * | and &&
		 * | equality ==, not equals !=
		 * | relational < > <= >=
		 * | additive + -
		 * | multiplicative * /
		 * | exponential **
		 * | unary ! - ++ --
		 * | member acces .
		 * | value (expr) "constants, like strings" reference_by_name method_calls_too()
		 * v
		 * HIGHEST:
		 */

		public Expression? Assignment()
		{
			var ctx = Init();
			var total = Assignment_Next();
			if (total == null) return null;

			ConsumeAllWhitespace();

			if (AtEnd())
			{
				Fail(ctx);
				return total;
			}

			while (_type == TokenType.Assignment)
			{
				NextSignificant();

				var more = Assignment_Next();

				if (more == null)
				{
					Error("Expected expression after assignment, didn't get one");
				}

				total = new Expression.Assignment(Make(ctx), total, more);
				ConsumeAllWhitespace();
			}

			return total;
		}

		private Expression? Assignment_Next() => Or();

		public Expression? Or()
		{
			var ctx = Init();
			var total = Or_Next();
			if (total == null) return null;

			ConsumeAllWhitespace();

			if (AtEnd())
			{
				Fail(ctx);
				return total;
			}

			while (_type == TokenType.Or)
			{
				NextSignificant();

				var more = Or_Next();

				if (more == null)
				{
					Error("Expected expression after or, didn't get one");
				}

				total = new Expression.Binary(Make(ctx), total, TokenType.Or, more);
				ConsumeAllWhitespace();
			}

			return total;
		}

		private Expression? Or_Next() => And();

		public Expression? And()
		{
			var ctx = Init();
			var total = And_Next();
			if (total == null) return null;

			ConsumeAllWhitespace();

			if (AtEnd())
			{
				Fail(ctx);
				return total;
			}

			while (_type == TokenType.And)
			{
				NextSignificant();

				var more = And_Next();

				if (more == null)
				{
					Error("Expected expression after or, didn't get one");
				}

				total = new Expression.Binary(Make(ctx), total, TokenType.And, more);
				ConsumeAllWhitespace();
			}

			return total;
		}

		private Expression? And_Next() => Equality();

		public Expression? Equality()
		{
			var ctx = Init();
			var total = Equality_Next();
			if (total == null) return null;

			ConsumeAllWhitespace();

			if (AtEnd())
			{
				Fail(ctx);
				return total;
			}

			while (_type == TokenType.EqualTo || _type == TokenType.NotEqualTo)
			{
				var type = _type;
				NextSignificant();

				var more = Equality_Next();

				if (more == null)
				{
					Error("Expected expression after equality, didn't get one");
				}

				total = new Expression.Binary(Make(ctx), total, type, more);
				ConsumeAllWhitespace();
			}

			return total;
		}

		private Expression? Equality_Next() => Relational();

		public Expression? Relational()
		{
			var ctx = Init();
			var total = Relational_Next();
			if (total == null) return null;

			ConsumeAllWhitespace();

			if (AtEnd())
			{
				Fail(ctx);
				return total;
			}

			while (_type == TokenType.LessThan || _type == TokenType.GreaterThan
				|| _type == TokenType.LessThanOrEqualTo || _type == TokenType.GreaterThanOrEqualTo)
			{
				var type = _type;
				NextSignificant();

				var more = Relational_Next();

				if (more == null)
				{
					Error("Expected expression after relational, didn't get one");
				}

				total = new Expression.Binary(Make(ctx), total, type, more);
				ConsumeAllWhitespace();
			}

			return total;
		}

		private Expression? Relational_Next() => Additive();

		public Expression? Additive()
		{
			var ctx = Init();
			var total = Additive_Next();
			if (total == null) return null;

			ConsumeAllWhitespace();

			if (AtEnd())
			{
				Fail(ctx);
				return total;
			}

			while (_type == TokenType.Add || _type == TokenType.Subtract)
			{
				var type = _type;
				NextSignificant();

				var more = Additive_Next();

				if (more == null)
				{
					Error("Expected expression after additive, didn't get one");
				}

				total = new Expression.Binary(Make(ctx), total, type, more);
				ConsumeAllWhitespace();
			}

			return total;
		}

		private Expression? Additive_Next() => Multiplicative();

		public Expression? Multiplicative()
		{
			var ctx = Init();
			var total = Multiplicative_Next();
			if (total == null) return null;

			ConsumeAllWhitespace();

			if (AtEnd())
			{
				Fail(ctx);
				return total;
			}

			while (_type == TokenType.Multiply || _type == TokenType.Divide)
			{
				var type = _type;
				NextSignificant();

				var more = Multiplicative_Next();

				if (more == null)
				{
					Error("Expected expression after multiplicative, didn't get one");
				}

				total = new Expression.Binary(Make(ctx), total, type, more);
				ConsumeAllWhitespace();
			}

			return total;
		}

		public Expression? Multiplicative_Next() => Exponential();

		public Expression? Exponential()
		{
			var ctx = Init();
			var total = Exponential_Next();
			if (total == null) return null;

			// exponential is right assosiative, eg.

			// 2 ** 2 ** 2
			// is 2 ** (2 ** 2)
			// = 2 ** 4
			// = 16
			// aka

			//   2
			//  2^
			// 2^

			//  4
			// 2^

			// 16

			ConsumeAllWhitespace();

			if (AtEnd())
			{
				Fail(ctx);
				return total;
			}

			while (_type == TokenType.Exponent)
			{
				var type = _type;
				NextSignificant();

				// to be right associative, we just call the previous one
				var more = Exponential_Next();

				if (more == null)
				{
					Error("Expected expression after exponential, didn't get one");
				}

				total = new Expression.Binary(Make(ctx), total, type, more);
				ConsumeAllWhitespace();
			}

			return total;
		}

		private Expression? Exponential_Next() => Unary();

		public Expression? Unary()
		{
			// unary expression is negation or not
			var ctx = Init();
			var unaryType = _type;
			var isInc = _type == TokenType.Increment
				|| _type == TokenType.Decrement;

			var isUnary = _type == TokenType.Not
				|| _type == TokenType.Subtract
				|| isInc;

			if (isUnary) Next();

			var total = Unary_Next();
			if (total == null) return null;

			if (!isUnary)
			{
				if (AtEnd())
				{
					return total;
				}

				isInc = _type == TokenType.Increment
					|| _type == TokenType.Decrement;

				if (!isInc)
				{
					return total;
				}

				var t = _type;
				Next();

				return new Expression.Increment(Make(ctx), Expression.Increment.IncrementSide.Post, t, total);
			}

			if (isInc)
			{
				return new Expression.Increment(Make(ctx), Expression.Increment.IncrementSide.Pre, unaryType, total);
			}
			else
			{
				return new Expression.Unary(Make(ctx), unaryType, total);
			}
		}

		private Expression? Unary_Next() => MemberAccess();

		public Expression? MemberAccess()
		{
			var ctx = Init();
			var total = MemberAccess_Next();
			if (total == null) return null;

			ConsumeAllWhitespace();

			if (AtEnd())
			{
				Fail(ctx);
				return total;
			}

			while (_type == TokenType.Dot)
			{
				NextSignificant();
				var more = MemberAccess_Next();

				if (more == null)
				{
					Error("Expected expression after member acces, didn't get one");
				}

				total = new Expression.Access(Make(ctx), total, more);
				ConsumeAllWhitespace();
			}

			return total;
		}

		private Expression? MemberAccess_Next() => ValueExpression();

		private Expression? ValueExpression()
		{
			var parenthesized = ReadParenthesized();

			if (parenthesized != null)
			{
				return parenthesized;
			}

			var @new = ReadNew();

			if (@new != null)
			{
				return @new;
			}

			var constant = ReadConstant();

			if (constant != null)
			{
				return constant;
			}

			var methodCall = ReadMethodCall();

			if (methodCall != null)
			{
				return methodCall;
			}

			var reference = ReadReference();

			if (reference != null)
			{
				return reference;
			}

			return null;
		}
#endregion

#region non precedence based expressions
		public Expression.Parenthesized? ReadParenthesized()
		{
			var ctx = Init();
			ConsumeAllWhitespace();

			if (_type == TokenType.OpenParen)
			{
				NextSignificant();

				var expr = ReadExpression();
				ConsumeAllWhitespace();

				if (_type != TokenType.CloseParen)
				{
					Error("Expected closing parenthesis");
				}

				// consume )
				Next();

				return new Expression.Parenthesized(Make(ctx), expr);
			}

			Fail(ctx);
			return null;
		}

		public Expression.Constant? ReadConstant()
		{
			var ctx = Init();
			ConsumeAllWhitespace();

			switch (_type)
			{
				case TokenType.StringToken:
				{
					// TODO: handle interpolation stuff
					var strData = UpdateStringData(_current.Value<Lexer.StringData>());

					// skip string
					Next();
					return new Expression.Constant(Make(ctx), strData);
				}

				case TokenType.NumberToken:
				{
					var value = _current.Value<Number>();
					Next();
					return new Expression.Constant(Make(ctx), value);
				}

				case TokenType.True:
				{
					Next();
					return new Expression.Constant(Make(ctx), true);
				}

				case TokenType.False:
				{
					Next();
					return new Expression.Constant(Make(ctx), false);
				}
			}

			Fail(ctx);
			return null;
		}

		public Expression.Reference? ReadReference()
		{
			var ctx = Init();
			ConsumeAllWhitespace();

			if (_type != TokenType.IdentifierToken)
			{
				Fail(ctx);
				return null;
			}

			var name = _current.Value<string>();
			Next();

			return new Expression.Reference(Make(ctx), name);
		}

		public Expression.MethodCall? ReadMethodCall()
		{
			var ctx = Init();
			ConsumeAllWhitespace();

			var isCompilerCall = false;

			if (_type == TokenType.At)
			{
				isCompilerCall = true;
				Next();
			}

			if (_type != TokenType.IdentifierToken)
			{
				Fail(ctx);
				return null;
			}

			var methodName = _current.Value<string>();
			NextSignificant();

			if (AtEnd())
			{
				Fail(ctx);
				return null;
			}

			if (_type != TokenType.OpenParen)
			{
				Fail(ctx);
				return null;
			}

			// after this point we're basically guarenteed to have a method call group

			// read (
			NextSignificant();

			var parameters = ReadMethodCallParameters();
			ConsumeAllWhitespace();

			if (_type != TokenType.CloseParen)
			{
				Error("Expected closing parenthesis to signify end of method call");
			}

			Next(); // read )

			return new Expression.MethodCall(Make(ctx), isCompilerCall, methodName, parameters.Value);
		}

		public Expression.New? ReadNew()
		{
			var ctx = Init();
			ConsumeAllWhitespace();

			if (_type != TokenType.New)
			{
				Fail(ctx);
				return null;
			}

			NextSignificant();

			if (_type != TokenType.IdentifierToken)
			{
				Error("Expected identifier for new object, didn't get one");
			}

			// TODO: read method call expression
			var type = _current.Value<string>();

			NextSignificant();

			if (_type != TokenType.OpenParen)
			{
				Error("Expected open parenthesis for new object constructor, didn't get one");
			}

			Next();

			var exprs = ReadMethodCallParameters().Value;

			if (_type != TokenType.CloseParen)
			{
				Error("Expected a close parenthesis on a method call");
			}

			Next();

			return new Expression.New(Make(ctx), type, exprs);
		}
#endregion

		private StringData UpdateStringData(Lexer.StringData strData)
		{
			var interpolations = new List<StringData.Interpolation>();

			foreach (var interpolation in strData.Interpolations)
			{
				var parser = new TerumiParser(interpolation.Tokens.ToArray(), _filePath);
				interpolations.Add(new StringData.Interpolation(parser.ReadExpression(), interpolation.Position));
			}

			return new StringData(strData.StringValue, interpolations);
		}

		private Contextual<List<Expression>> ReadMethodCallParameters()
		{
			var ctx = Init();

			if (_type == TokenType.CloseParen)
			{
				return Make(ctx, EmptyList<Expression>.Instance);
			}

			var exprs = new List<Expression>();

			while (_type != TokenType.CloseParen)
			{
				var expr = ReadExpression();

				if (expr == null)
				{
					Error("Expected valid expression");
				}

				exprs.Add(expr);
				ConsumeAllWhitespace();

				if (_type != TokenType.Comma)
				{
					if (_type == TokenType.CloseParen)
					{
						break;
					}
					else
					{
						Error("Didn't get a comma, expected a closing parenthesis but didn't get one either.");
					}
				}

				// consume ,
				Next();
			}

			// don't consume )
			// Next();

			return Make(ctx, exprs);
		}
#endregion

#region helpers
		private Contextual<(string? Type, string Name)> ReadTypeAndNameOptional(bool error = true)
		{
			var ctx = Init();

			if (_type != TokenType.IdentifierToken)
			{
				if (error)
				{
					Error("Expected identifier");
				}
				else
				{
					return Fail<(string?, string)>(ctx);
				}
			}

			var a = _current.Value<string>();
			NextSignificant();

			if (_type != TokenType.IdentifierToken)
			{
				return Make(ctx, (default(string?), a));
			}

			var b = _current.Value<string>();
			Next();
			return Make(ctx, (a, b));
		}

		// consumption related
		[MethodImpl(MaxOpt)]
		private ContextualInit Init() => new ContextualInit(_tokens, _i);

		[MethodImpl(MaxOpt)]
		private Contextual<T> Make<T>(ContextualInit ctx, T value) => ctx.Make(value, _i);

		[MethodImpl(MaxOpt)]
		private Contextual<T> Fail<T>(ContextualInit ctx)
		{
			Set(ctx.Start);
			return Contextual<T>.Fail();
		}

		[MethodImpl(MaxOpt)]
		private void Fail(ContextualInit ctx) => Fail<object>(ctx);

		[MethodImpl(MaxOpt)]
		private ConsumedTokens Make(ContextualInit ctx) => ctx.Make(default(object), _i);

		// raw token stream helpers
		[MethodImpl(MaxOpt)]
		private void BackToFirstWhitespace()
		{
			if (_i == 0) return;

			Set(_i - 1);

			while (IsAllWhitespace(_type))
			{
				if (_i == 0) return;

				Set(_i - 1);
			}

			Next();
		}

		[MethodImpl(MaxOpt)]
		private void NextSignificant()
		{
			Next(); ConsumeAllWhitespace();
		}

		[MethodImpl(MaxOpt)]
		private void NextUntilNewline(string @for)
		{
			Next();
			if (AtEnd()) return;

			UntilNewline(@for);
		}

		[MethodImpl(MaxOpt)]
		private void UntilNewline(string @for)
		{
			ConsumeWhitespace();
			if (AtEnd()) return;

			if (_type != TokenType.Newline)
			{
				Error("Expected newline, didn't get one for " + @for);
			}

			Next();
		}

		[MethodImpl(MaxOpt)]
		private void ConsumeWhitespace()
		{
			while (IsWhitespace(_type))
			{
				Next();
				if (AtEnd()) return;
			}
		}

		[MethodImpl(MaxOpt)]
		private void ConsumeAllWhitespace()
		{
			if (AtEnd()) return;

			while (IsAllWhitespace(_type))
			{
				Next();
				if (AtEnd()) return;
			}
		}

		[MethodImpl(MaxOpt)]
		private static bool IsWhitespace(TokenType type)
			=> type == TokenType.Comment
			|| type == TokenType.Whitespace;

		[MethodImpl(MaxOpt)]
		private static bool IsAllWhitespace(TokenType type)
			=> IsWhitespace(type)
			|| type == TokenType.Newline;

		[MethodImpl(MaxOpt)]
		private void Next() => Set(_i + 1);

		[MethodImpl(MaxOpt)]
		private void Set(int i)
		{
			_i = i;

			if (AtEnd(i))
			{
				_token = null;
			}
			else
			{
				_token = _tokens.Span[i];
			}
		}

		[MethodImpl(MaxOpt)]
		private bool AtEnd(int i = -1) => (i == -1 ? _i : i) >= _tokens.Length;

		[DoesNotReturn]
		[MethodImpl(MethodImplOptions.NoInlining)]
		private void Error(string message, int index = -1, [CallerLineNumber] int lineNumber = 0)
		{
			index = index == -1 ? _i : index;

			throw new ParserException(_tokens, index, message, lineNumber);
		}
#endregion
	}

	public class ParserException : Exception
	{
		public ParserException(ReadOnlyMemory<Token> context, int index, string message, int parserLineNumber) : base(message)
		{
			Context = context;
			Index = index;
			ParserLineNumber = parserLineNumber;
		}

		public ReadOnlyMemory<Token> Context { get; }
		public int Index { get; }
		public int ParserLineNumber { get; }
	}
}