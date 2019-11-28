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

			ConsumeAllWhitespace();

			if (AtEnd())
			{
				// no methods/classes... weird
				return null;
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

			return null;
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

			return new Class(Make(ctx), name, null, null);
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
				UntilNewline("statement");

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

				// read }
				// go to newline
				NextUntilNewline("end of code body");

				var contextualStmts = Make(ctx, stmts);
				return new CodeBody(contextualStmts, stmts);
			}
		}
#endregion

#region statements
		public Statement ReadStatement()
		{
			if (_type == TokenType.StringToken)
			{
				return new Statement.Return(default, new Expression.Constant(default, _current.Value<Lexer.StringData>()));
			}

			Error("Cannot parse statements at this time");
			return null;
		}
#endregion

#region expressions
#endregion

#region helpers
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
		private ConsumedTokens Make(ContextualInit ctx) => ctx.Make(default(object), _i);

		// raw token stream helpers
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