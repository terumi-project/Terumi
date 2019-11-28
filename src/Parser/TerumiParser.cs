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

		public TerumiParser(ReadOnlyMemory<Token> tokens)
		{
			_tokens = tokens;
			Set(_i);
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
			// package x.y.z at the top
			var package = ReadPackage(defaultLevel);
			var packages = new List<Contextual<Contextual<PackageLevel>>>();

			var use = ReadUse();

			while (use.Success != ContextualState.FailedRead)
			{
				packages.Add(use);
				use = ReadUse();
			}

			return null;
		}

		/* top of the file */
		public Contextual<Contextual<PackageLevel>> ReadPackage(PackageLevel defaultLevel)
		{
			var ctx = Init();

			ConsumeAllWhitespace();
			if (_type == TokenType.Package)
			{
				NextSignificant();
				var level = PackageLevel();
				NextUntilNewline();
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
				NextUntilNewline();
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

			while (_type != TokenType.Dot)
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

		/* classes */

		/* methods */
		public Method ReadMethod()
		{
			var ctx = Init();

			var methodHeader = ReadTypeAndNameOptional();

			if (_type != TokenType.OpenParen)
			{
				Error("Expected open parenthesis to signify method parameter group");
			}

			var parameters = ReadMethodParameterGroup();
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
				NextUntilNewline();

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
				}

				NextSignificant();
				NextUntilNewline();

				var contextualStmts = Make(ctx, stmts);
				return new CodeBody(contextualStmts, stmts);
			}
		}

		public Statement ReadStatement()
		{
			if (_type == TokenType.StringToken)
			{
				return new Statement.Return(default, new Expression.Constant(default, _current.Value<StringData>()));
			}

			Error("Cannot parse statements at this time");
			return null;
		}

		private Contextual<(string? Type, string Name)> ReadTypeAndNameOptional()
		{
			var ctx = Init();

			if (_type != TokenType.IdentifierToken)
			{
				Error("Expected identifier");
			}

			var a = _current.Value<string>();
			NextSignificant();

			if (_type != TokenType.IdentifierToken)
			{
				NextSignificant();
				return Make(ctx, (default(string?), a));
			}

			var b = _current.Value<string>();
			NextSignificant();
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
			_i = ctx.Start;
			return Contextual<T>.Fail();
		}

		[MethodImpl(MaxOpt)]
		private ConsumedTokens Make(ContextualInit ctx) => ctx.Make(default(object), _i);

		// raw token stream helpers
		[MethodImpl(MaxOpt)]
		private void NextSignificant()
		{
			Next(); ConsumeAllWhitespace();
		}

		[MethodImpl(MaxOpt)]
		private void NextUntilNewline()
		{
			while (IsWhitespace(_type))
			{
				Next();
				if (AtEnd()) return;
			}

			if (_type != TokenType.Newline)
			{
				Error("Expected newline, didn't get one");
			}
		}

		[MethodImpl(MaxOpt)]
		private void ConsumeAllWhitespace()
		{
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
		private void Next() => Set(++_i);

		[MethodImpl(MaxOpt)]
		private void Prev() => Set(--_i);

		[MethodImpl(MaxOpt)]
		private void Set(int i) => _token = AtEnd(i) ? null : _tokens.Span[i];

		[MethodImpl(MaxOpt)]
		private bool AtEnd(int i = -1) => (i == -1 ? _i : i) == _tokens.Length - 1;

		[DoesNotReturn]
		[MethodImpl(MethodImplOptions.NoInlining)]
		private void Error(string message, int index = -1)
		{
			index = index == -1 ? _i : index;

			throw new ParserException(_tokens, index, message);
		}
	}

	public class ParserException : Exception
	{
		public ParserException(ReadOnlyMemory<Token> context, int index, string message) : base(message)
		{
			Context = context;
			Index = index;
		}

		public ReadOnlyMemory<Token> Context { get; }
		public int Index { get; }
	}
}