grammar TerumiGrammar;

compilation_unit
	:   code_block
	;

// code
code_block
	:   code_line+
	;

code_line
	:   COMPILER_RESERVED function_call
	|   if_statement
	|   variable_declaration
	|   function_declaration
	|   function_call
	;

parameters
	:   expression (COMMA expression)*
	;

function_call
	:   NAME OPEN_PARENTHESIS parameters? CLOSE_PARENTHESIS
	;

function_declaration
	:   NAME function_call code_block
	;

if_statement
	:   'if' expression OPEN_CURLY_BRACE code_block CLOSE_CURLY_BRACE
	;

variable_declaration
	:   ('let' | NAME) NAME ASSIGNMENT expression
	;

// expressions oh boy
expression
	:	number
	|   string
	|   expression '==' expression
	|   NAME
	;

string
	:   StringLiteral
	;

number : DIGIT+ ;

// reserved keywords
COMPILER_RESERVED : '@' ;

STRING : 'string' ;
NUMBER : 'number' ;
VOID : 'void' ;

MODULE : 'namespace' ;
CLASS : 'class' ;
CONTRACT : 'contract' ;

// operators
ASSIGNMENT : '=' ;

NAME : (ALPHA| UNDERSCORE)+ DIGIT* ;
TYPE : 'let' | NAME;

OPEN_CURLY_BRACE : '{' ;
CLOSE_CURLY_BRACE : '}' ;
OPEN_PARENTHESIS : '(' ;
CLOSE_PARENTHESIS : ')' ;
COMMA : ',' ;

fragment ALPHA : (LOWERCASE | UPPERCASE) ;
fragment LOWERCASE : [a-z] ;
fragment UPPERCASE : [A-Z] ;
DIGIT : [0-9] ;
fragment UNDERSCORE : '_' ;
fragment DOT : '.' ;


fragment
SingleCharacter
	:	~['\\\r\n]
	;
// §3.10.5 String Literals
StringLiteral
	:	'"' StringCharacters? '"'
	;
fragment
StringCharacters
	:	StringCharacter+
	;
fragment
StringCharacter
	:	~["\\\r\n]
	|	EscapeSequence
	;
// §3.10.6 Escape Sequences for Character and String Literals
fragment
EscapeSequence
	:	'\\' [btnfr"'\\]
	;

WS  :  [ \t\r\n\u000C]+ -> skip
    ;