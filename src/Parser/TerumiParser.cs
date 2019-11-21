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
			return ConsumeGeneric<Statement, Statement.Assignment>(ConsumeAssignment, ref statement)
				|| ConsumeGeneric<Statement, Statement.MethodCall>(ConsumeMethodCall, ref statement);
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

			object value = null; // TODO

			ConsumeWhitespace(false);
			assignment = new Statement.Assignment(TakeTokens(start, Current()), type, name, value);
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
			}

			// consume close paren
			Next();
			ConsumeWhitespace(false);

			methodCall = new Statement.MethodCall(TakeTokens(start, Current()), isCompilerCall, name, exprs);
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
			}

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
			}

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
			}

			return total;
		}

		public Expression ConsumeMultiplicationExpression()
		{
			var total = ConsumePrimaryExpression();
			if (total == null) return null;

			while (Peek().Type == TokenType.Multiply || Peek().Type == TokenType.Divide)
			{
				var type = Peek().Type;
				Next();
				ConsumeWhitespace(false);

				var right = ConsumePrimaryExpression();
				if (right == null)
				{
					Unsupported($"Expected right hand of multiplication statement ({type})");
				}

				total = new Expression.Binary(total, type, right);
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
				case TokenType.IdentifierToken: { var data = Peek().Data; Next(); return new Expression.Reference(TakeTokens(start, Current()), data as string); }
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

				return expr;
			}

			Statement.MethodCall call = null;

			if (ConsumeMethodCall(ref call))
			{
				return new Expression.MethodCall(call);
			}

			Unsupported($"Unsupported expression");
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
			if (_tokens.Count <= amt + _i) Unsupported("No more tokens to peek from");
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
