pub mod parser_tree;

#[macro_use] extern crate lalrpop_util;

lalrpop_mod!(pub terumi);

fn main() {
	let result = terumi::CompilationUnitParser::new().parse("
use some.cool.thing
package oh.i.see

fdecl() { 1 2 3 4 5 }
fdecl() 1 use ok . boom
fdecl() { 1 2 }
void void() {
	1
}

string teststr(string oper, number k) {
	2
}

class SomeClas {
	ctor() {
	}

	string something_cool

	does_stuff() {
		2
	}

	bool can_do_stuff() {
		4
	}
}

contract SomeClasContract {
	string something_cool
	does_stuff()
	bool can_do_stuff()
}
").unwrap();

	for element in result.iter() {
		println!();
		println!("{:?}", element);
	}
}
