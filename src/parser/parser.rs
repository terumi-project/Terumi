use crate::parser::structures::ParsedFile;
use crate::lexer::Token;

pub struct Parser<'a> {
  tokens: &'a [Token<'a>]
}

impl<'a> Parser<'a> {
  pub fn new(tokens: &'a [Token<'a>]) -> Parser<'a> {
    Parser {
      tokens: tokens
    }
  }

  pub fn parse(&self) -> ParsedFile<'a> {
    panic!("TODO: implement");
  }
}