mod lexer;
mod parser;
use parser::parser::*;
use lexer::*;

fn main() {
  println!("Tokens: ");
    
  let tokens: Vec<Token<'_>> = Lexer::from_string("package ok

use thing

class Testing
{
  ok() {
    new okay::Test().ok()
  }
}").collect();

  let parser = Parser::new(&(tokens[..]));

  println!("Hello, World!");
}