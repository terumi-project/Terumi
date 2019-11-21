using System;
using System.Numerics;

// https://www.craftinginterpreters.com/scanning.html
namespace Terumi.Lexer
{
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
				case TokenType.IdentifierToken when !(Data is string): throw new InvalidOperationException($"Cannot have identifier token without string data");
				case TokenType.IdentifierToken: break;

				case TokenType.StringToken when !(Data is StringData): throw new InvalidOperationException($"Must pass in a StringData for a string token");
				case TokenType.StringToken: break;

				case TokenType.Comment when !(Data is string): throw new InvalidOperationException($"Must pass in a string for a comment token");
				case TokenType.Comment: break;

				case TokenType.NumberToken when !(Data is Number): throw new InvalidOperationException($"Must pass in Number for a number token");
				case TokenType.NumberToken: break;

				case TokenType.CommandToken when !(Data is StringData): throw new InvalidOperationException("Must pass in StringData to command token");
				case TokenType.CommandToken: break;

				default: if (Data != null) throw new InvalidOperationException($"Token type {Type} is not allowed to have data"); break;
			}
#endif
		}

		public TokenType Type { get; }
		public LexerMetadata PositionStart { get; }
		public LexerMetadata PositionEnd { get; }
		public object? Data { get; }

		public override string ToString() => $"Token '{Type}' beginning {PositionStart} ending {PositionEnd} with data: {PositionEnd}";
	}
}