﻿// https://www.craftinginterpreters.com/scanning.html
namespace Terumi.Lexer
{
	public enum TokenType
	{
		IdentifierToken,
		NumberToken,
		StringToken,
		Comment,
		Whitespace,

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
		At, Dot, // @ .

		Use, Package, Class, Contract,
		True, False,
		If, Else, For, While,
		Readonly,
		This,
	}
}