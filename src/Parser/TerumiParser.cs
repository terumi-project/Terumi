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
			if (Peek().Type != TokenType.IdentifierToken) return false;

			string? type = null;
			var name = Peek().Data as string;

			Next();
			ConsumeWhitespace();

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

			ConsumeWhitespace(false);
			var body = ConsumeCodeBody();

			method = new Method(TakeTokens(methodStart, Current()), type, name, parameters, body);
			return true;
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

				ConsumeWhitespace();
			}

			return new CodeBody(TakeTokens(start, Current()), statements);
		}

		private bool ConsumeStatement(ref Statement statement)
		{
			return ConsumeGeneric<Statement, Statement.Assignment>(ConsumeAssignment, ref statement);
		}

		private bool ConsumeAssignment(ref Statement.Assignment assignment)
		{
			var start = Current();
			if (Peek().Type != TokenType.IdentifierToken)
			{
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
				return false;
			}

			object value = null; // TODO

			assignment = new Statement.Assignment(TakeTokens(start, Current()), type, name, value);
			return true;
		}

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
