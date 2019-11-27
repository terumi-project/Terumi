using System;
using System.Diagnostics;
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
				case TokenType.IdentifierToken:	Debug.Assert(Data is string);		break;
				case TokenType.CommandToken:	Debug.Assert(Data is StringData);	break;
				case TokenType.StringToken:		Debug.Assert(Data is StringData);	break;
				case TokenType.NumberToken:		Debug.Assert(Data is Number);		break;
				case TokenType.Comment:			Debug.Assert(Data is string);		break;
				default: Debug.Assert(Data == null); break;
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