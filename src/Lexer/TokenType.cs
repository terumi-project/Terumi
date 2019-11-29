// https://www.craftinginterpreters.com/scanning.html
namespace Terumi.Lexer
{
	public enum TokenType
	{
		Unknown,

		IdentifierToken,
		NumberToken,
		StringToken,
		CommandToken,

		Comment,
		Whitespace,
		Newline,

		Comma, // ,
		OpenParen, CloseParen, // ( )
		OpenBracket, CloseBracket, // [ ]
		OpenBrace, CloseBrace, // { }
		And, Or, // && ||
		EqualTo, Assignment, // == =
		NotEqualTo, Not, // != !
		GreaterThan, GreaterThanOrEqualTo, // > >=
		LessThan, LessThanOrEqualTo, // < <=
		Increment, Add, // ++ +
		Decrement, Subtract, // -- -
		Exponent, Multiply, // ** *
		Divide, // /
		At, Dot, // @ .
		Semicolon, // ;

		Use, Package, Class, Contract,
		True, False,
		If, Else, For, While, Do,
		Readonly,
		This,
		Return,
		Set,
		New
	}
}