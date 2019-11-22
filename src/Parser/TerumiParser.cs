using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Terumi.Lexer;

namespace Terumi.Parser
{
	public delegate bool TryConsume<T>(ref T result);

	public struct ConsumedTokens
	{
		public static ConsumedTokens Default { get; } = new ConsumedTokens(EmptyList<Token>.Instance);

		public ConsumedTokens(List<Token> tokens)
		{
			Tokens = tokens;
		}

		public List<Token> Tokens { get; }
	}

	public class TerumiParser
	{
		private const MethodImplOptions MaxOpt = MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization;
		private readonly List<Token> _tokens;
		private int _i;

		public TerumiParser(List<Token> tokens)
		{
			_tokens = tokens;
			ConsumeWhitespace(false);
		}

		public SourceFile ConsumeSourceFile(PackageLevel defaultLevel)
		{
			var start = Current();
			if (AtEnd()) return new SourceFile(TakeTokens(start, Current()), defaultLevel, EmptyList<PackageLevel>.Instance, EmptyList<Method>.Instance);

			var packageLevel = defaultLevel;

			if (Peek().Type == TokenType.Package)
			{
				Next();

				ConsumeWhitespace();
				packageLevel = ConsumePackageLevel();

				ConsumeWhitespace();
			}

			var packages = new List<PackageLevel>();

			while (Peek().Type == TokenType.Use)
			{
				Next();
				ConsumeWhitespace();

				packages.Add(ConsumePackageLevel());
				ConsumeWhitespace();
			}

			// TODO: functions, types & whatnot

			var methods = new List<Method>();

			{
				Method method = null;

				while (true)
				{
					// TODO: types
					if (TryMethod(ref method))
					{
						methods.Add(method);
					}
					else
					{
						break;
					}
				}
			}

			return new SourceFile(TakeTokens(start, Current()), packageLevel, packages, methods);
		}

		public PackageLevel ConsumePackageLevel()
		{
			if (AtEnd()) Unsupported($"Cannot consume a package level at the end of the token list");

			if (Peek().Type != TokenType.IdentifierToken)
			{
				Unsupported($"Expected identifier type");
			}

			var levels = new List<string> { (string)Peek().Data };

			Next();
			while (Peek().Type == TokenType.Dot)
			{
				Next();

				if (Peek().Type != TokenType.IdentifierToken)
				{
					Unsupported($"Expected another identifier in namespace, got {Peek().Type}");
				}

				levels.Add((string)Peek().Data);

				Next();
			}

			return new PackageLevel(levels);
		}

		public bool TryMethod(ref Method method)
		{
			var methodStart = Current();
			if (AtEnd()) return Quit();
			if (Peek().Type != TokenType.IdentifierToken) return Quit();

			string? type = null;
			var name = Peek().Data as string;

			Next();
			// TODO: make sure there's an identifier if there's not a second one
			ConsumeWhitespace(false);

			if (Peek().Type == TokenType.IdentifierToken)
			{
				type = name;
				name = Peek().Data as string;

				Next();
				ConsumeWhitespace(false);
			}

			if (Peek().Type != TokenType.OpenParen)
			{
				Unsupported($"Expected opening parenthesis on method parameter group");
			}

			Next();

			bool expectMore = Peek().Type != TokenType.CloseParen;

			List<MethodParameter> parameters;

			if (!expectMore)
			{
				parameters = EmptyList<MethodParameter>.Instance;
			}
			else
			{
				parameters = new List<MethodParameter>(4);
			}

			while (expectMore)
			{
				var start = Current();

				Next();
				ConsumeWhitespace(false);

				if (Peek().Type != TokenType.IdentifierToken)
				{
					Unsupported($"Expected parameter type or closed parenthesis");
				}

				var paramType = Peek().Data as string;

				Next();
				ConsumeWhitespace();

				if (Peek().Type != TokenType.IdentifierToken)
				{
					Unsupported($"Expected parameter name after parameter type");
				}

				var paramName = Peek().Data as string;

				parameters.Add(new MethodParameter(TakeTokens(start, Current()), paramType, paramName));

				Next();
				ConsumeWhitespace(false);

				expectMore = Peek().Type == TokenType.Comma;
				if (!expectMore && Peek().Type != TokenType.CloseParen)
				{
					Unsupported($"Expected end of parameter list (')') because of a lack of comma, but didn't get one");
				}

				if (expectMore)
				{
					Next();
					ConsumeWhitespace(false);
				}
			}

			// consuem close paren
			Next();
			ConsumeWhitespace(false);

			ConsumeWhitespace(false);
			var body = ConsumeCodeBody();

			method = new Method(TakeTokens(methodStart, Current()), type, name, parameters, body);
			return true;

			bool Quit()
			{
				_i = methodStart;
				return false;
			}
		}

		private CodeBody ConsumeCodeBody()
		{
			var start = Current();
			if (Peek().Type != TokenType.OpenBrace)
			{
				Statement statement = null;

				if (!ConsumeStatement(ref statement))
				{
					// empty method
					return CodeBody.Empty;
				}

				return new CodeBody(TakeTokens(start, Current()), new List<Statement> { statement });
			}

			Next();
			ConsumeWhitespace(); // we expect newlines to act as an end of statement

			var statements = new List<Statement>();

			while (Peek().Type != TokenType.CloseBrace)
			{
				Statement statement = null;

				if (!ConsumeStatement(ref statement))
				{
					Unsupported($"Couldn't consume statement in code body");
				}

				statements.Add(statement);

				// statement consumed whitespace for us
			}

			// consme close brace
			Next();
			ConsumeWhitespace(false);

			return new CodeBody(TakeTokens(start, Current()), statements);
		}

		#region STATEMENTS
		private bool ConsumeStatement(ref Statement statement)
		{
			return ConsumeGeneric<Statement, Statement.Return>(ConsumeReturn, ref statement)
				|| ConsumeGeneric<Statement, Statement.Command>(ConsumeCommand, ref statement)
				|| ConsumeGeneric<Statement, Statement.Assignment>(ConsumeAssignment, ref statement)
				|| ConsumeGeneric<Statement, Statement.Increment>(ConsumeIncrement, ref statement)
				|| ConsumeGeneric<Statement, Statement.MethodCall>(ConsumeMethodCall, ref statement)
				|| ConsumeGeneric<Statement, Statement.If>(ConsumeIf, ref statement)
				|| ConsumeGeneric<Statement, Statement.While>(ConsumeWhile, ref statement)
				|| ConsumeGeneric<Statement, Statement.For>(ConsumeFor, ref statement);
		}

		private bool ConsumeReturn(ref Statement.Return @return)
		{
			if (Peek().Type != TokenType.Return) return false;
			var start = Current();
			Next(); ConsumeWhitespace();
			var expr = ConsumeExpression();
			@return = new Statement.Return(TakeTokens(start, Current()), expr);
			ConsumeWhitespace(false);
			return true;
		}

		private bool ConsumeCommand(ref Statement.Command command)
		{
			var start = Current();
			if (Peek().Type != TokenType.CommandToken) return false;
			var data = Peek().Data as StringData;
			Next();
			command = new Statement.Command(TakeTokens(start, Current()), data);
			ConsumeWhitespace(false);
			return true;
		}

		private bool ConsumeIncrement(ref Statement.Increment increment)
		{
			var start = Current();
			var expr = ConsumeIncrementExpression();
			if (!(expr is Expression.Increment incExpr)) return Quit();
			increment = new Statement.Increment(TakeTokens(start, Current()), incExpr);
			ConsumeWhitespace(false);
			return true;

			bool Quit() { _i = start; return false; }
		}

		private bool ConsumeAssignment(ref Statement.Assignment assignment)
		{
			var start = Current();
			if (Peek().Type != TokenType.IdentifierToken)
			{
				_i = start;
				return false;
				// Unsupported($"Expected identifier token when consuming variable declaration statement");
			}

			string? type = null;
			var name = Peek().Data as string;

			Next();
			var didConsume = ConsumeWhitespace(false);

			if (Peek().Type == TokenType.IdentifierToken)
			{
				if (!didConsume)
				{
					_i = start;
					return false;
					// Unsupported($"Should've consumed whitespace before another identifier");
				}

				type = name;
				name = Peek().Data as string;

				Next();
				ConsumeWhitespace(false);
			}

			if (Peek().Type != TokenType.Assignment)
			{
				_i = start;
				return false;
			}
			Next();
			ConsumeWhitespace(false);

			var value = ConsumeExpression(); // TODO

			assignment = new Statement.Assignment(TakeTokens(start, Current()), type, name, value);

			ConsumeWhitespace(false);
			return true;
		}

		private bool ConsumeMethodCall(ref Statement.MethodCall methodCall)
		{
			var start = Current();
			var isCompilerCall = false;

			if (Peek().Type == TokenType.At) { Next(); isCompilerCall = true; }

			if (Peek().Type != TokenType.IdentifierToken) { _i = start; return false; }

			var name = Peek().Data as string;
			Next();
			ConsumeWhitespace(false);

			if (AtEnd()) { _i = start; return false; }
			if (Peek().Type != TokenType.OpenParen) { _i = start; return false; }

			Next();
			ConsumeWhitespace(false);

			var exprs = new List<Expression>();

			while (Peek().Type != TokenType.CloseParen)
			{
				exprs.Add(ConsumeExpression());

				if (Peek().Type != TokenType.Comma)
				{
					// Next();
					ConsumeWhitespace(false);

					if (Peek().Type == TokenType.CloseParen) break;
					Unsupported($"Didn't get comma but didn't get closing parenthesis");
				}
				else
				{
					// consume comma
					Next();
					ConsumeWhitespace(false);
				}
			}

			// consume close paren
			Next();
			ConsumeWhitespace(false);

			methodCall = new Statement.MethodCall(TakeTokens(start, Current()), isCompilerCall, name, exprs);
			return true;
		}

		private bool ConsumeIf(ref Statement.If @if)
		{
			if (Peek().Type != TokenType.If) return false;
			var start = Current();
			Next(); ConsumeWhitespace();
			var expr = ConsumeExpression();
			var @true = ConsumeCodeBody();
			var @else = CodeBody.Empty;

			if (Peek().Type == TokenType.Else)
			{
				Next(); ConsumeWhitespace(false);
				@else = ConsumeCodeBody();
			}

			@if = new Statement.If(TakeTokens(start, Current()), expr, @true, @else);
			ConsumeWhitespace(false);
			return true;
		}

		private bool ConsumeWhile(ref Statement.While @while)
		{
			var start = Current();
			bool isDoWhile = false;
			if (Peek().Type == TokenType.Do)
			{
				isDoWhile = true;
				Next(); ConsumeWhitespace(false);
				var body = ConsumeCodeBody();
				ConsumeWhitespace(false);
				if (Peek().Type != TokenType.While) Unsupported("Expected do while to have while at the end of body");
				Next(); ConsumeWhitespace(false);
				var comparison = ConsumeExpression();

				@while = new Statement.While(TakeTokens(start, Current()), comparison, body, isDoWhile);

				ConsumeWhitespace(false);
				return true;
			}
			else if (Peek().Type == TokenType.While)
			{
				Next(); ConsumeWhitespace(false);
				var comparison = ConsumeExpression();
				ConsumeWhitespace(false);
				var body = ConsumeCodeBody();

				@while = new Statement.While(TakeTokens(start, Current()), comparison, body);
				ConsumeWhitespace(false);
				return true;
			}

			return false;
		}

		private bool ConsumeFor(ref Statement.For @for)
		{
			if (Peek().Type != TokenType.For) return false;
			var start = Current();
			Next(); ConsumeWhitespace(false);
			if (Peek().Type != TokenType.OpenParen) Unsupported("Expected open parenthesis in for");
			Next(); ConsumeWhitespace(false);
			var decl = ConsumeCodeBody();
			if (Peek().Type != TokenType.Semicolon) Unsupported("Expected semicolon at end of for declaration");
			Next(); ConsumeWhitespace(false);
			var comparison = ConsumeComparisonExpression();
			if (Peek().Type != TokenType.Semicolon) Unsupported("Expected semicolon at end of for declaration");
			Next(); ConsumeWhitespace(false);
			var inc = ConsumeCodeBody();
			if (Peek().Type != TokenType.CloseParen) Unsupported("Expected close parenthesis");
			Next(); ConsumeWhitespace(false);
			var body = ConsumeCodeBody();

			@for = new Statement.For(TakeTokens(start, Current()), decl, comparison, inc, body);
			return true;
		}
		#endregion

		#region EXPRESSIONS
		public Expression ConsumeExpression()
		{
			return ConsumeEqualityExpression();
		}

		public Expression ConsumeEqualityExpression()
		{
			var total = ConsumeComparisonExpression();
			if (total == null) return null;

			while (Peek().Type == TokenType.EqualTo
				|| Peek().Type == TokenType.NotEqualTo)
			{
				var type = Peek().Type;
				Next();
				ConsumeWhitespace(false);

				var right = ConsumeComparisonExpression();
				if (right == null)
				{
					Unsupported($"Expected right hand of addition statement ({type})");
				}

				total = new Expression.Binary(total, type, right);
				ConsumeWhitespace(false);
			}

			ConsumeWhitespace(false);
			return total;
		}

		public Expression ConsumeComparisonExpression()
		{
			var total = ConsumeAdditionExpression();
			if (total == null) return null;

			while (Peek().Type == TokenType.GreaterThan
				|| Peek().Type == TokenType.GreaterThanOrEqualTo
				|| Peek().Type == TokenType.LessThan
				|| Peek().Type == TokenType.LessThanOrEqualTo)
			{
				var type = Peek().Type;
				Next();
				ConsumeWhitespace(false);

				var right = ConsumeAdditionExpression();
				if (right == null)
				{
					Unsupported($"Expected right hand of addition statement ({type})");
				}

				total = new Expression.Binary(total, type, right);
				ConsumeWhitespace(false);
			}

			ConsumeWhitespace(false);
			return total;
		}

		public Expression ConsumeAdditionExpression()
		{
			var total = ConsumeMultiplicationExpression();
			if (total == null) return null;

			while (Peek().Type == TokenType.Add || Peek().Type == TokenType.Subtract)
			{
				var type = Peek().Type;
				Next();
				ConsumeWhitespace(false);

				var right = ConsumeMultiplicationExpression();
				if (right == null)
				{
					Unsupported($"Expected right hand of addition statement ({type})");
				}

				total = new Expression.Binary(total, type, right);
				ConsumeWhitespace(false);
			}

			ConsumeWhitespace(false);
			return total;
		}

		public Expression ConsumeMultiplicationExpression()
		{
			var total = ConsumeIncrementExpression();
			if (total == null) return null;

			while (Peek().Type == TokenType.Multiply || Peek().Type == TokenType.Divide)
			{
				var type = Peek().Type;
				Next();
				ConsumeWhitespace(false);

				var right = ConsumeIncrementExpression();
				if (right == null)
				{
					Unsupported($"Expected right hand of multiplication statement ({type})");
				}

				total = new Expression.Binary(total, type, right);
				ConsumeWhitespace(false);
			}

			ConsumeWhitespace(false);
			return total;
		}

		public Expression ConsumeIncrementExpression()
		{
			var start = Current();
			bool pre = false;
			TokenType type = TokenType.Whitespace;
			if (Peek().Type == TokenType.Increment || Peek().Type == TokenType.Decrement) { type = Peek().Type; Next(); }
			pre = type != TokenType.Whitespace;
			var primary = ConsumeAccessExpression();
			if (primary == null) return null;
			if (type == TokenType.Whitespace)
			{
				if (AtEnd()) return primary;
				if (Peek().Type == TokenType.Increment || Peek().Type == TokenType.Decrement) { type = Peek().Type; Next(); }
			}
			if (type != TokenType.Whitespace)
			{
				var side = pre ? Expression.Increment.IncrementSide.Pre : Expression.Increment.IncrementSide.Post;
				var expr = new Expression.Increment(TakeTokens(start, Current()), side, type, primary);
				ConsumeWhitespace(false);
				return expr;
			}
			ConsumeWhitespace(false);
			return primary;
		}

		public Expression ConsumeAccessExpression()
		{
			var start = Current();
			var total = ConsumePrimaryExpression();

			if (AtEnd()) return total;
			while (Peek().Type == TokenType.Dot)
			{
				Next(); ConsumeWhitespace(false);
				var action = ConsumePrimaryExpression();
				total = new Expression.Access(TakeTokens(start, Current()), total, action);
			}

			return total;
		}

		public Expression ConsumePrimaryExpression()
		{
			var start = Current();
			var primary = ConsumePrimaryExpression_Actual();
			if (primary == null) return null;
			ConsumeWhitespace(false);
			return primary;

			// access expressions are for big brains only

			/*
			ConsumeWhitespace(false);
			if (Peek().Type == TokenType.Dot)
			{
				var expr = ConsumeExpression();
				if (expr == null) Unsupported($"Expected expression after dot on expression");

				if (expr is Expression.Access access)
				{
					// unpack the access expression
				}
			}
			*/
		}

		public Expression ConsumePrimaryExpression_Actual()
		{
			var start = Current();

			switch (Peek().Type)
			{
				case TokenType.StringToken: { var data = Peek().Data; Next(); return new Expression.Constant(TakeTokens(start, Current()), data); }
				case TokenType.NumberToken: { var data = Peek().Data; Next(); return new Expression.Constant(TakeTokens(start, Current()), data); }
				case TokenType.True: { Next(); return new Expression.Constant(TakeTokens(start, Current()), true); }
				case TokenType.False: { Next(); return new Expression.Constant(TakeTokens(start, Current()), false); }
			}

			if (Peek().Type == TokenType.OpenParen)
			{
				Next();
				ConsumeWhitespace(false);
				var expr = ConsumeExpression();

				if (Peek().Type != TokenType.CloseParen)
				{
					Unsupported($"Expected closing parenthesis on expression");
				}

				// consume )
				Next();
				return expr;
			}

			Statement.MethodCall call = null;

			if (ConsumeMethodCall(ref call))
			{
				return new Expression.MethodCall(call);
			}

			// try identifier as a reference as a LAST RESORT 
			if (Peek().Type == TokenType.IdentifierToken)
			{
				var data = Peek().Data;
				Next();
				return new Expression.Reference(TakeTokens(start, Current()), data as string);
			}

			// Unsupported($"Unsupported expression");
			return null;
		}
		#endregion

		/* core */

		private bool ConsumeWhitespace(bool mustConsumeWhitespace = true)
		{
			bool didConsume = false;

			while (!AtEnd()
				&& (Peek().Type == TokenType.Whitespace
				|| Peek().Type == TokenType.Comment))
			{
				didConsume = true;
				Next();
			}

			if (mustConsumeWhitespace && !didConsume)
			{
				Unsupported($"Didn't consume necessary whitespace");
			}

			return didConsume;
		}

		private Token Peek(int amt = 0)
		{
			if (_tokens.Count <= amt + _i)
#if DEBUG
			Unsupported("No more tokens to peek from");
#else
			{
				Log.Warn("Couldn't read anymore tokens - returning 'Unknown' TokenType");
				return TokenType.Unknown;
			}
#endif
			return _tokens[amt + _i];
		}

		private void Next() => _i++;

		public bool AtEnd() => _i >= _tokens.Count - 1;

		private int Current() => _i;

		// TODO: more efficient take routine
		private ConsumedTokens TakeTokens(int start, int end)
			=> new ConsumedTokens(_tokens.Skip(start).Take(end).ToList());

		private bool ConsumeGeneric<T, T2>(TryConsume<T2> tryConsume, ref T result)
			where T2 : T
		{
			T2 instance = default;
			if (!tryConsume(ref instance)) return false;
			result = (T)instance;
			return true;
		}

		private void Unsupported(string reason)
		{
			throw new InvalidOperationException($"{reason} {(!AtEnd() ? Peek().PositionStart.ToString() : "")}");
		}
	}
}
