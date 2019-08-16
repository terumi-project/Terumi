grammar TerumiGrammar;

compilation_unit : hello_message+ ;

hello_message : HELLO IDENTIFIER ;

HELLO : 'HELLO' ;

IDENTIFIER : IDENTIFIER_BASE IDENTIFIER_ALL+ ;
IDENTIFIER_BASE : [a-zA-Z_] ;
IDENTIFIER_ALL : IDENTIFIER_BASE | [0-9] ;

WS : [ \r\n\t]+ -> skip ;