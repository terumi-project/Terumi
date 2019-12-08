using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

// https://www.craftinginterpreters.com/scanning.html
namespace Terumi.Lexer
{
	public ref struct TerumiLexer
	{
		private SourceTraverser _source;
		private readonly string _fileName;

		// allow for consumers to use these fields instead of allocation a Token
		public TokenType TokenType;
		public LexerMetadata Start;
		public LexerMetadata End;
		public object? Data;

		// for detailed errors
		public bool WasError;
		public LexerMetadata ErrorLocation;
		public string ErrorMessage;

		public TerumiLexer(ReadOnlySpan<char> source, string fileName)
		{
			// don't want to copy
			_source = new SourceTraverser(source, fileName);
			_fileName = fileName;

			TokenType = TokenType.Unknown;
			Start = default;
			End = default;
			Data = default;

			WasError = default;
			ErrorLocation = default;
			ErrorMessage = default;
		}

		public ReadOnlySpan<char> Source => _source.Source;

		public List<Token> ConsumeTokens()
		{
			var tokens = new List<Token>();

			while (NextToken())
			{
				tokens.Add(new Token(this.TokenType, Start, End, Data));
			}

			return tokens;
		}

		public bool NextToken()
		{
			Start = _source.Metadata;

			switch (_source.Peek())
			{
				// note: tightly coupled to the cases in WhiteSpace.IsWhitespace
				case ' ':
				case '\r':
				case '\t': return Whitespace();

				case '\n': return Newline();

				case '.': return Char(TokenType.Dot);
				case ',': return Char(TokenType.Comma);
				case '(': return Char(TokenType.OpenParen);
				case ')': return Char(TokenType.CloseParen);
				case '[': return Char(TokenType.OpenBracket);
				case ']': return Char(TokenType.CloseBracket);
				case '{': return Char(TokenType.OpenBrace);
				case '}': return Char(TokenType.CloseBrace);
				case ';': return Char(TokenType.Semicolon);
				case '=': return IsNext('=') ? Char(TokenType.EqualTo)				: Char(TokenType.Assignment);
				case '!': return IsNext('=') ? Char(TokenType.NotEqualTo)			: Char(TokenType.Not);
				case '>': return IsNext('=') ? Char(TokenType.GreaterThanOrEqualTo)	: Char(TokenType.GreaterThan);
				case '<': return IsNext('=') ? Char(TokenType.LessThanOrEqualTo)	: Char(TokenType.LessThan);
				case '+': return IsNext('+') ? Char(TokenType.Increment)			: Char(TokenType.Add);
				case '-': return IsNext('-') ? Char(TokenType.Decrement)			: Char(TokenType.Subtract);
				case '*': return IsNext('*') ? Char(TokenType.Exponent)				: Char(TokenType.Multiply);

				case '@': return IsNext('/', false) ? Command() : Char(TokenType.At);

				case '"': return String();

				case '/': return IsNext('/', false)
					? SinglelineComment() // //
					: (IsNext('*', false)
						? MultilineComment() // /*
						: Char(TokenType.Divide)); // /

				case SourceTraverser.InvalidCharacter: return false;

				default:
				{
					if (TryNumber())
					{
						return true;
					}

					// try keywords before identifiers
					// things like `new_top` shouldn't be counted as keyword `New` identifier `_top`
					if (TryKeyword("contract", TokenType.Contract)
					 || TryKeyword("readonly", TokenType.Readonly)
					 || TryKeyword("package", TokenType.Package)
					 || TryKeyword("return", TokenType.Return)
					 || TryKeyword("class", TokenType.Class)
					 || TryKeyword("false", TokenType.False)
					 || TryKeyword("while", TokenType.While)
					 || TryKeyword("true", TokenType.True)
					 || TryKeyword("else", TokenType.Else)
					 || TryKeyword("this", TokenType.This)
					 || TryKeyword("for", TokenType.For)
					 || TryKeyword("use", TokenType.Use)
					 || TryKeyword("set", TokenType.Set)
					 || TryKeyword("new", TokenType.New)
					 || TryKeyword("if", TokenType.If)
					 || TryKeyword("do", TokenType.Do))
					{
						return true;
					}

					if (TryIdentifier())
					{
						return true;
					}
				}
				break;
			}

			WasError = true;
			ErrorLocation = _source.Metadata;
			ErrorMessage = "Unrecognized character";
			return false;
		}

		// case ...
		private bool Char(TokenType type)
		{
			_source.Advance();

			TokenType = type;
			End = _source.Metadata;
			Data = null;

			return true;
		}

		private bool IsNext(char c, bool advance = true)
		{
			var result = _source.Peek(1) == c;

			if (advance && result)
			{
				_source.Advance();
			}

			return result;
		}

		// default:
		private bool TryKeyword(string compare, TokenType result)
		{
			if (!_source.TryExact(compare, c => !IsIdentifierNonStart(c)))
			{
				return false;
			}

			TokenType = result;
			End = _source.Metadata;
			Data = null;

			return true;
		}

		private bool TryNumber()
		{
			if (!IsNumber(_source.Peek())
				&& _source.Peek() != '-')
			{
				return false;
			}

			int end = 1;

			while (IsNumber(_source.Peek(end)))
			{
				end++;
			}

			var number = _source.Extract(end);

			TokenType = TokenType.NumberToken;
			End = _source.Metadata;
			Data = new Number(BigInteger.Parse(number));

			return true;

			static bool IsNumber(char c) => c <= '9' && c >= '0';
		}

		private bool TryIdentifier()
		{
			if (!IsIdentifierStart(_source.Peek()))
			{
				return false;
			}

			int end = 1;

			while (IsIdentifierNonStart(_source.Peek(end)))
			{
				end++;
			}

			var identifier = _source.Extract(end);

			TokenType = TokenType.IdentifierToken;
			End = _source.Metadata;
			Data = new string(identifier);

			return true;
		}

		private static bool IsIdentifierStart(char c) => (c <= 'Z' && c >= 'A') || (c <= 'z' && c >= 'a') || c == '_';
		private static bool IsIdentifierNonStart(char c) => IsIdentifierStart(c) || (c <= '9' && c >= '0');

		// special cases
		private bool SinglelineComment()
		{
			Debug.Assert(_source.Peek() == '/', "First character is a /");
			Debug.Assert(_source.Peek(1) == '/', "Second character is a /");

			int end = 2;
			char peek = _source.Peek(end);

			while (peek != '\n' && peek != SourceTraverser.InvalidCharacter)
			{
				end++;
				peek = _source.Peek(end);
			}

			var rawData = _source.Extract(end);
			var commentData = rawData.Slice(2, rawData.Length - (1 + 2));

#if DEBUG
			if (peek == '\n')
			{
				Debug.Assert(rawData[0] == '/', "First two characters of raw data should be forward slashes");
				Debug.Assert(rawData[1] == '/', "First two characters of raw data should be forward slashes");
				Debug.Assert(rawData[^1] == '\n', "Last character in raw data should be a newline");

				if (commentData.Length != 0)
				{
					Debug.Assert(commentData[0] != '/', "First character of comment data isn't a foward slash");
					Debug.Assert(commentData[^1] != '\n', "Last character of comment data isn't a newline");
				}
			}
#endif

			TokenType = TokenType.Comment;
			End = _source.Metadata;
			Data = new string(commentData);

			return true;
		}

		private bool MultilineComment()
		{
			Debug.Assert(_source.Peek() == '/', "First character is a /");
			Debug.Assert(_source.Peek(1) == '*', "Second character is a *");

			// we want to support /*/
			int length = 1;
			char peek = _source.Peek(length);

			while (true)
			{
				if (peek == SourceTraverser.InvalidCharacter)
				{
					break;
				}

				if (peek == '*' && _source.Peek(length + 1) == '/')
				{
					length++;
					break;
				}

				length++;
			}

			var rawData = _source.Extract(length);
			ReadOnlySpan<char> commentData;

			if (rawData.Length == 3)
			{
				commentData = string.Empty;
			}
			else
			{
				commentData = rawData.Slice(2, rawData.Length - (2 + 2));
			}

#if DEBUG
			Debug.Assert(rawData[0] == '/');
			Debug.Assert(rawData[1] == '*');

			// we might not end wit ha */
			// Debug.Assert(rawData[^1] == '/');
			// Debug.Assert(rawData[^2] == '*');
#endif

			TokenType = TokenType.Comment;
			End = _source.Metadata;
			Data = commentData.Length == 0 ? string.Empty : new string(commentData);

			return true;
		}

		private bool String()
		{
			Debug.Assert(_source.Peek() == '"', "First character of string should be a \"");
			_source.Advance();

			// if the very first character of the string is a newline, ignore it

			// any sane operating system
			if (_source.Peek() == '\n')
			{
				_source.Advance();
			}
			// windows
			else if (_source.Peek() == '\r' && _source.Peek(1) == '\n')
			{
				_source.Advance(2);
			}

			return InnerString(c => c == '"');
		}

		private bool Command()
		{
			Debug.Assert(_source.Peek() == '@', "First character should be @");
			Debug.Assert(_source.Peek(1) == '/', "Second character should be /");
			_source.Advance(2);

			// go right behind the \r and or \n so that the parser recognizes the statement as ending
			var success = InnerString(c => c == '\r' || c == '\n', false);

			TokenType = TokenType.CommandToken;
			return success;
		}

		private bool InnerString(Predicate<char> isEnd, bool advanceAtEnd = true)
		{
			var interpolations = new List<StringData.Interpolation>();
			var strb = new StringBuilder();

			char current;

			// will get overwritten with interpolations
			var startCopy = Start;

			while (!isEnd(current = _source.Peek()))
			{
				switch (current)
				{
					default:
					{
						strb.Append(current);
					}
					break;

					case '{':
					{
						_source.Advance();

						var tokens = new List<Token>();

						char c;
						while ((current = _source.Peek()) != '}')
						{
							if (current == SourceTraverser.InvalidCharacter)
							{
								_source.Advance();

								WasError = true;
								ErrorLocation = _source.Metadata;
								ErrorMessage = "Reached EOF before interpolation ended";
								return false;
							}

							if (!NextToken())
							{
								Debug.Assert(WasError, "There must've been an error if a token couldn't be consumed");

								// assume all the error stuff was already set
								return false;
							}

							tokens.Add(new Token(this.TokenType, Start, End, Data));
						}

						interpolations.Add(new StringData.Interpolation(strb.Length, tokens));
					}
					break;

					case SourceTraverser.InvalidCharacter:
					{
						WasError = true;
						ErrorLocation = _source.Metadata;
						ErrorMessage = "Reached EOF without a closing quote";
					}
					return false;

					case '\\':
					{
						var next = _source.Peek(1);

						switch (next)
						{
							case '\\': strb.Append('\\'); break;
							case '"': strb.Append('"'); break;
							case '{': strb.Append('{'); break;
							case '}': strb.Append('}'); break;

							// TODO: warn that you don't need to escape these
							case 'n': strb.Append('\n'); break;
							case 't': strb.Append('\t'); break;

							default:
							{
								// the advances bellow won't occur, so we'll advance it manually
								// to the invalid escaped char
								_source.Advance();

								WasError = true;
								ErrorLocation = _source.Metadata;
								ErrorMessage = "Invalid escape character";
							}
							return false;
						}

						_source.Advance();
					}
					break;
				}

				_source.Advance();
			}

			// consume last char
			if (advanceAtEnd)
			{
				_source.Advance();
			}

			TokenType = TokenType.StringToken;
			Start = startCopy;
			End = _source.Metadata;
			Data = new StringData(strb, interpolations);
			return true;
		}

		// whitespace
		private bool Whitespace()
		{
			if (!IsWhitespace(_source.Peek()))
			{
				return false;
			}

			do
			{
				_source.Advance();
			}
			while (IsWhitespace(_source.Peek()));

			TokenType = TokenType.Whitespace;
			End = _source.Metadata;
			Data = null;
			return true;

			// note: tightly coupled to the cases in the NextToken switch
			static bool IsWhitespace(char c) => c == '\r' || c == '\t' || c == ' ';
		}

		private bool Newline()
		{
			if (_source.Peek() != '\n')
			{
				return false;
			}

			_source.Advance();

			TokenType = TokenType.Newline;
			End = _source.Metadata;
			Data = null;
			return true;
		}
	}

	public ref struct SourceTraverser
	{
		public const MethodImplOptions MaxOpt = MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization;

		// https://github.com/dotnet/roslyn/blob/master/src/Compilers/CSharp/Portable/Parser/SlidingTextWindow.cs#L25-L37
		public const char InvalidCharacter = char.MaxValue;

		// how many columns a sincle tab will account f or
		public const int TabSize = 4;

		private readonly ReadOnlySpan<char> _source;
		private readonly string _fileName;

		private int _line;
		private int _column;
		private int _offset;

		public SourceTraverser(ReadOnlySpan<char> source, string fileName)
		{
			_source = source;
			_fileName = fileName;

			_line = 1;
			_column = 1;
			_offset = 0;
		}

		public ReadOnlySpan<char> Source => _source;

		public LexerMetadata Metadata => new LexerMetadata { BinaryOffset = _offset, Line = _line, Column = _column, File = _fileName };

		/// <summary>
		/// Attempts to consume the string specified at the current position.
		/// </summary>
		[MethodImpl(MaxOpt)]
		public bool TryExact(ReadOnlySpan<char> compare, Predicate<char> trailing)
		{
			if (_offset + compare.Length + 1 >= _source.Length)
			{
				return false;
			}

			ReadOnlySpan<char> source = _source.Slice(_offset, compare.Length + 1);

			if (compare[0] != source[0])
			{
				return false;
			}

			if (!trailing(source[^1]))
			{
				return false;
			}

			for (int i = 1; i < compare.Length; i++)
			{
				if (compare[i] != source[i])
				{
					return false;
				}
			}

			Advance(compare.Length);

			return true;
		}

		[MethodImpl(MaxOpt)]
		public ReadOnlySpan<char> Extract(int amount)
		{
			Debug.Assert(_offset + amount < _source.Length, $"Cannot extract {amount} chars from source - not long enough.");

			var extraction = _source.Slice(_offset, amount);

			Advance(amount);

			return extraction;
		}

		[MethodImpl(MaxOpt)]
		public char Peek(int amount = 0)
		{
			var target = _offset + amount;

			if (target >= _source.Length)
			{
				return InvalidCharacter;
			}

			return _source[target];
		}

		[MethodImpl(MaxOpt)]
		public void Advance(int amount)
		{
			for (int i = 0; i < amount; i++)
			{
				Advance();
			}
		}

		[MethodImpl(MaxOpt)]
		public void Advance()
		{
			var current = Peek();

			if (current != InvalidCharacter)
			{
				switch (current)
				{
					case '\r': break;

					case '\t':
					{
						_column += TabSize;
					}
					break;

					case '\n':
					{
						_line++;
						_column = 1;
					}
					break;

					default:
					{
						_column++;
					}
					break;
				}
			}

			_offset++;
		}
	}
}