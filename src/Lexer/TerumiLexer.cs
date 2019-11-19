using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Terumi.Lexer
{
	// https://www.craftinginterpreters.com/scanning.html

	public enum TokenType
	{
		StringToken,
		Comment,

		Comma, // ,
		OpenParen, CloseParen, // ( )
		OpenBracket, CloseBracket, // [ ]
		OpenBrace, CloseBrace, // { }
		EqualTo, Assignment, // == =
		NotEqualTo, Not, // != !
		GreaterThan, GreaterThanOrEqualTo, // > >=
		LessThan, LessThanOrEqualTo, // < <=
		Increment, Add, // ++ +
		Decrement, Subtract, // -- -
		Exponent, Multiply, // ** *
		Divide, // /
	}

	public class Token
	{
		public Token
		(
			TokenType type,
			LexerMetadata positionStart,
			LexerMetadata positionEnd,

			// i don't really like this whole 'object' thing but
			// generics don't really help us out here
			object? data
		)
		{
			Type = type;
			PositionStart = positionStart;
			PositionEnd = positionEnd;
			Data = data;

			// some preventative measures since we have a yucky object
#if DEBUG
			switch (Type)
			{
				case TokenType.StringToken when !(Data is StringData): throw new InvalidOperationException($"Must pass in a StringData for a string token");
				case TokenType.Comment when !(Data is string): throw new InvalidOperationException($"Must pass in a string for a comment token");
				default: if (Data != null) throw new InvalidOperationException($"Token type {Type} is not allowed to have data"); break;
			}
#endif
		}

		public TokenType Type { get; }
		public LexerMetadata PositionStart { get; }
		public LexerMetadata PositionEnd { get; }
		public object? Data { get; }
	}

	public class StringData
	{
	}

	public ref struct TerumiLexer
	{
		private const MethodImplOptions MaxOpt = MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization;
		public const int TabSize = 4;

		private Span<byte> _source;
		private readonly string _file;
		private int _line;
		private int _column;
		private int _offset;

		public LexerMetadata Metadata => new LexerMetadata { BinaryOffset = _offset, Line = _line, Column = _column, File = _file };

		public TerumiLexer(string file, Span<byte> source)
		{
			_source = source;
			_line = 1;
			_column = 1;
			_offset = 0;
			_file = file;
		}

		[MethodImpl(MaxOpt)]
		public Token NextToken()
		{
			var now = Metadata;

			switch (Next())
			{
				case (byte)'(': return Char(TokenType.OpenParen, now);
				case (byte)')': return Char(TokenType.CloseParen, now);
				case (byte)'[': return Char(TokenType.OpenBracket, now);
				case (byte)']': return Char(TokenType.CloseBracket, now);
				case (byte)'{': return Char(TokenType.OpenBrace, now);
				case (byte)'}': return Char(TokenType.CloseBrace, now);
				case (byte)'=': return IsNext((byte)'=') ? Char(TokenType.EqualTo, now) : Char(TokenType.Assignment, now);
				case (byte)'!': return IsNext((byte)'=') ? Char(TokenType.NotEqualTo, now) : Char(TokenType.Not, now);
				case (byte)'>': return IsNext((byte)'=') ? Char(TokenType.GreaterThanOrEqualTo, now) : Char(TokenType.GreaterThan, now);
				case (byte)'<': return IsNext((byte)'=') ? Char(TokenType.LessThanOrEqualTo, now) : Char(TokenType.LessThan, now);
				case (byte)'+': return IsNext((byte)'+') ? Char(TokenType.Increment, now) : Char(TokenType.Add, now);
				case (byte)'-': return IsNext((byte)'-') ? Char(TokenType.Decrement, now) : Char(TokenType.Subtract, now);
				case (byte)'*': return IsNext((byte)'*') ? Char(TokenType.Exponent, now) : (IsNext((byte)'/') ? MultilineComment() : Char(TokenType.Multiply, now));
				case (byte)'/': return IsNext((byte)'/') ? SinglelineComment() : Char(TokenType.Divide, now);
				case (byte)'"': return String();
			}

			Unsupported();
			return null;
		}

		[MethodImpl(MaxOpt)]
		private Token Char(TokenType type, LexerMetadata start) => new Token(type, start, Metadata, null);

		[MethodImpl(MaxOpt)]
		private bool IsNext(byte b)
		{
			if (Peek() == b) { Next(); return true; }
			return false;
		}

		/* comments */

		[MethodImpl(MaxOpt)]
		public Token MultilineComment()
		{
			var now = Metadata;
			var capture = _source;
			bool hitEnd = false;

			// we want to capture */
			_source = _source.Slice(1);

			while (!AtEnd() && !(hitEnd = EndOfMultilineComment())) Next();

			if (!hitEnd)
			{
				Unsupported();
			}

			var commentData = capture.Slice(0, _offset - now.BinaryOffset);
			var stringData = Encoding.UTF8.GetString(commentData);

			return new Token(TokenType.Comment, now, Metadata, stringData);
		}

		// modifies state to put us at the end of the comment
		[MethodImpl(MaxOpt)]
		private bool EndOfMultilineComment()
		{
			byte b = (byte)'\0';

			if (TryPeek(ref b) && b == (byte)'*'
				&& TryPeek(ref b, 1) && b == (byte)'/')
			{
				_source = _source.Slice(2);
				return true;
			}

			return false;
		}

		/* singleline noise */

		[MethodImpl(MaxOpt)]
		public Token SinglelineComment()
		{
			var now = Metadata;
			var capture = _source;
			bool hitEnd = false;

			_source = _source.Slice(1);

			while (!AtEnd() && !(hitEnd = Peek() == (byte)'\n')) Next();

			if (!hitEnd)
			{
				Unsupported();
			}

			var commentData = capture.Slice(0, _offset - now.BinaryOffset);
			var stringData = Encoding.UTF8.GetString(commentData);

			return new Token(TokenType.Comment, now, Metadata, stringData);
		}

		/* string */

		[MethodImpl(MaxOpt)]
		public Token String()
		{
			var now = Metadata;
			var strb = new StringBuilder();

			// TODO:

			return new Token(TokenType.StringToken, now, Metadata, new StringData());
		}

		/* basic */

		[MethodImpl(MaxOpt)]
		public byte Next()
		{
			var b = Peek();

			_offset++;
			switch (b)
			{
				case (byte)'\n':
				{
					_line++;
					_column = 1;
				}
				break;

				case (byte)'\t':
				{
					_column += TabSize;
				}
				break;
			}

			_source = _source.Slice(1);
			return b;
		}

		[MethodImpl(MaxOpt)]
		public byte Peek(int amt = 1)
		{
			if (AtEnd(amt)) Unsupported();
			return _source[amt - 1];
		}

		[MethodImpl(MaxOpt)]
		public bool TryPeek(ref byte result, int amt = 0)
		{
			if (AtEnd(amt)) return false;
			result = _source[amt];
			return true;
		}

		[MethodImpl(MaxOpt)]
		private bool AtEnd(int amt = 0) => _source.Length <= amt;

		[MethodImpl(MethodImplOptions.NoInlining)]
		private void Unsupported() => throw new InvalidOperationException();
	}
}
