// PARSE TREE GUIDELINES:
// ALL COMMENTS PRIMARILY RELATE TO ORDERING OF MEMBERS IN THIS DOCUMENT AND
// NEW NAMES GIVEN ARE NOT MEANT TO BE USED ELSEWHERE, AS THEY ONLY RELATE TO
// THE STRUCTURE OF THE DOCUMENT.
//
// The order and layout of members should be as they are parsed in a file.
//
// The ordering and layout of enums/structs should be as they are parsed in a file,
// with any "dependencies" following afterwords.
//
// e.g.
// TopLevelUnit:
// - Use <PackageLevel>
// - Package <PackageLevel>
// - Class <Class>
//
// PackageLevel: // dependency of 'Use', must be introduced here
// // 'Package' already has PackageLevel defined, it's fine
//
// Class: // dependency of 'Class'
// - Name // class Name { ... }
// - Vec<Member> { members here }
//
// Enums are used when there are different kinds of acceptable inputs in a scope.
// Class members, Statements, and Expressions are enums.
//
// Structs are used when data needs to be held, eg. Class name & member list, etc.
//
// Exceptions to the guidelines:
// - Separation of 'Code' and 'Skeleton'
//   Skeleton declarations are first to actual Code declarations.
//   This is because Skeleton declarations are rather light in their footprint,
//   and Code declarations require much more information based on statements.
//   Therefore, they are explicitly after the 'Code' comment point.

#[derive(Debug, PartialEq)]
pub enum TopLevelUnit {
	Use(PackageLevel),
	Package(PackageLevel),
	Class(Class),
	Contract(Contract),
	Function(Function),
}

// TODO: struct based on guidelines above?
#[derive(Debug, PartialEq)]
pub enum PackageLevel {
	Root(String),
	Scoped(Box<PackageLevel>, String),
}

#[derive(Debug, PartialEq)]
pub struct Class {
	pub name: String,
	pub members: Vec<ClassEntry>,
}

#[derive(Debug, PartialEq)]
pub enum ClassEntry {
	Field(Field),
	Function(Function),
}

#[derive(Debug, PartialEq)]
pub struct Field {
	pub field_type: Type,
	pub name: String,
}

#[derive(Debug, PartialEq)]
pub struct Contract {
	pub name: String,
	pub members: Vec<ContractEntry>
}

#[derive(Debug, PartialEq)]
pub enum ContractEntry {
	Field(Field), // no need to make a SkeletonField
	Function(SkeletonFunction),
}

#[derive(Debug, PartialEq)]
pub struct SkeletonFunction {
	pub return_type: Type,
	pub name: String,
	pub parameters: Vec<Parameter>,
}

// debatable if a Skeleton or Code, but seeing as Field is a skeleton,
// so are functions
#[derive(Debug, PartialEq)]
pub struct Function {
	pub return_type: Type,
	pub name: String,
	pub parameters: Vec<Parameter>,
	pub statements: Vec<Statement>,
}

// Code // ====================================================================

// TODO: exceptions in guidelines for this
#[derive(Debug, PartialEq)]
pub enum Type {
	Void,
	String,
	Number,
	Bool,

	// when the user makes a type, like a class or contract
	User(String),

	Qualified(PackageLevel, Box<Type>),

	//       Type    <Different, Types>
	Generics(String, Vec<Type>),
}

#[derive(Debug, PartialEq)]
pub struct Parameter {
	pub parameter_type: Type,
	pub name: String,
}

#[derive(Debug, PartialEq)]
pub enum Statement {
	Number(i32)
}

// Tests

lalrpop_mod!(pub terumi);

// types
#[test]
fn type_parses_string() {
	let type_string = terumi::TypeParser::new().parse("string").unwrap();
	assert_eq!(type_string, Type::String);
}

#[test]
fn type_parses_number() {
	let type_string = terumi::TypeParser::new().parse("number").unwrap();
	assert_eq!(type_string, Type::Number);
}

#[test]
fn type_parses_bool() {
	let type_string = terumi::TypeParser::new().parse("bool").unwrap();
	assert_eq!(type_string, Type::Bool);
}

#[test]
fn type_parses_user_type() {
	let type_string = terumi::TypeParser::new().parse("JsonParser").unwrap();
	assert_eq!(type_string, Type::User("JsonParser".to_string()));
}

#[test]
fn type_parses_single_generic() {
	let type_string = terumi::TypeParser::new().parse("SingleGeneric<string>").unwrap();
	assert_eq!(type_string, Type::Generics("SingleGeneric".to_string(), vec![Type::String]));
}

#[test]
fn type_parses_two_generics() {
	let type_string = terumi::TypeParser::new().parse("TwoGenerics<string, number>").unwrap();
	assert_eq!(type_string, Type::Generics("TwoGenerics".to_string(), vec![Type::String, Type::Number]));
}

#[test]
fn type_parses_lots_of_generics() {
	let type_string = terumi::TypeParser::new().parse("A<B<C, D, E>, F<G<H>>, I<string, number, J>>").unwrap();
	assert_eq!(type_string, Type::Generics(
		"A".to_string(),
		vec![
			Type::Generics(
				"B".to_string(),
				vec![
					Type::User("C".to_string()),
					Type::User("D".to_string()),
					Type::User("E".to_string()),
				]
			),
			Type::Generics(
				"F".to_string(),
				vec![
					Type::Generics("G".to_string(), vec![Type::User("H".to_string())])
				]
			),
			Type::Generics(
				"I".to_string(),
				vec![
					Type::String,
					Type::Number,
					Type::User("J".to_string())
				]
			)
		]
	));
}

#[test]
fn fully_qualified_type() {
	let type_string = terumi::TypeParser::new().parse("fully.qualified::Type").unwrap();
	assert_eq!(type_string, Type::Qualified(
		PackageLevel::Scoped(
			Box::new(PackageLevel::Root("fully".to_string())),
			"qualified".to_string()
		),
		Box::new(Type::User("Type".to_string()))
	));
}

// parameters

#[test]
fn parameter_parses() {
	let result = terumi::ParameterParser::new().parse("SomeType parameter_name").unwrap();
	assert_eq!(result, Parameter {
		parameter_type: Type::User("SomeType".to_string()),
		name: "parameter_name".to_string(),
	})
}

#[test]
fn sample_generic_parameter() {
	let result = terumi::ParameterParser::new().parse("Some<Generic, Parameter, string> name").unwrap();
	assert_eq!(result, Parameter {
		parameter_type: Type::Generics("Some".to_string(), vec![
			Type::User("Generic".to_string()),
			Type::User("Parameter".to_string()),
			Type::String,
		]),
		name: "name".to_string(),
	})
}

// functions

#[test]
fn function_declaration_no_return_type_no_parameters_no_code() {
	let result = terumi::FunctionDeclarationParser::new().parse("no_return_type_no_parameters_no_code() { }").unwrap();
	assert_eq!(result, Function {
		return_type: Type::Void,
		name: "no_return_type_no_parameters_no_code".to_string(),
		parameters: vec![],
		statements: vec![]
	})
}

#[test]
fn function_declaration_no_return_type_one_parameter_no_code() {
	let result = terumi::FunctionDeclarationParser::new().parse("no_return_type_one_parameter_no_code(string some_parameter) { }").unwrap();
	assert_eq!(result, Function {
		return_type: Type::Void,
		name: "no_return_type_one_parameter_no_code".to_string(),
		parameters: vec![Parameter { parameter_type: Type::String, name: "some_parameter".to_string()}],
		statements: vec![]
	})
}

#[test]
fn function_declaration_no_return_type_plural_parameters_no_code() {
	let result = terumi::FunctionDeclarationParser::new().parse("no_return_type_plural_parameters_no_code(string some_parameter, number another, And<Another> one) { }").unwrap();
	assert_eq!(result, Function {
		return_type: Type::Void,
		name: "no_return_type_plural_parameters_no_code".to_string(),
		parameters: vec![
			Parameter { parameter_type: Type::String, name: "some_parameter".to_string() },
			Parameter { parameter_type: Type::Number, name: "another".to_string() },
			Parameter { parameter_type: Type::Generics("And".to_string(), vec![Type::User("Another".to_string())]), name: "one".to_string() }
		],
		statements: vec![]
	})
}

#[test]
fn function_declaration_has_return_type() {
	let result = terumi::FunctionDeclarationParser::new().parse("string func() { }").unwrap();
	assert_eq!(result, Function {
		return_type: Type::String,
		name: "func".to_string(),
		parameters: vec![],
		statements: vec![],
	})
}

// contracts

#[test]
fn contract_nothing() {
	let result = match terumi::TopLevelUnitParser::new().parse("contract Contract { }").unwrap() {
		TopLevelUnit::Contract(contract) => contract,
		_ => panic!("Expected contract")
	};
	assert_eq!(result, Contract {
		name: "Contract".to_string(),
		members: vec![]
	})
}

#[test]
fn contract_field() {
	let result = match terumi::TopLevelUnitParser::new().parse("contract Contract {
    string string_field
}").unwrap() {
		TopLevelUnit::Contract(contract) => contract,
		_ => panic!("Expected contract")
	};
	assert_eq!(result, Contract {
		name: "Contract".to_string(),
		members: vec![
			ContractEntry::Field(Field {
				field_type: Type::String,
				name: "string_field".to_string()
			})
		]
	})
}

#[test]
fn contract_fields() {
	let result = match terumi::TopLevelUnitParser::new().parse("contract Contract {
	string string_field
	A<Type> _jaisod
}").unwrap() {
		TopLevelUnit::Contract(contract) => contract,
		_ => panic!("Expected contract")
	};
	assert_eq!(result, Contract {
		name: "Contract".to_string(),
		members: vec![
			ContractEntry::Field(Field {
				field_type: Type::String,
				name: "string_field".to_string()
			}),
			ContractEntry::Field(Field {
				field_type: Type::Generics("A".to_string(), vec![
					Type::User("Type".to_string())
				]),
				name: "_jaisod".to_string()
			}),
		]
	})
}

#[test]
fn contract_function() {
	let result = match terumi::TopLevelUnitParser::new().parse("contract Contract {
	hello_world()
}").unwrap() {
		TopLevelUnit::Contract(contract) => contract,
		_ => panic!("Expected contract")
	};
	assert_eq!(result, Contract {
		name: "Contract".to_string(),
		members: vec![
			ContractEntry::Function(SkeletonFunction {
				return_type: Type::Void,
				name: "hello_world".to_string(),
				parameters: vec![],
			}),
		]
	})
}

#[test]
fn complex_contract() {
	let result = match terumi::TopLevelUnitParser::new().parse("contract Contract {
	number some_field
	string with_return_type(number parameter)
}").unwrap() {
		TopLevelUnit::Contract(contract) => contract,
		_ => panic!("Expected contract")
	};
	assert_eq!(result, Contract {
		name: "Contract".to_string(),
		members: vec![
			ContractEntry::Field(Field {
				field_type: Type::Number,
				name: "some_field".to_string()
			}),
			ContractEntry::Function(SkeletonFunction {
				return_type: Type::String,
				name: "with_return_type".to_string(),
				parameters: vec![Parameter {
					parameter_type: Type::Number,
					name: "parameter".to_string()
				}],
			}),
		]
	})
}

// classes

#[test]
fn class_simple() {
	let result = match terumi::TopLevelUnitParser::new().parse("class Class { }").unwrap() {
		TopLevelUnit::Class(class) => class,
		_ => panic!("Expected class")
	};
	assert_eq!(result, Class {
		name: "Class".to_string(),
		members: vec![],
	})
}

#[test]
fn class_fields() {
	let result = match terumi::TopLevelUnitParser::new().parse("class Class {
	string class_field
	string _private_field
}").unwrap() {
		TopLevelUnit::Class(class) => class,
		_ => panic!("Expected class")
	};
	assert_eq!(result, Class {
		name: "Class".to_string(),
		members: vec![
			ClassEntry::Field(Field {
				field_type: Type::String,
				name: "class_field".to_string()
			}),
			ClassEntry::Field(Field {
				field_type: Type::String,
				name: "_private_field".to_string()
			}),
		],
	})
}

#[test]
fn class_functions() {
	let result = match terumi::TopLevelUnitParser::new().parse("class Class {
	number function_with_body() {
	}
}").unwrap() {
		TopLevelUnit::Class(class) => class,
		_ => panic!("Expected class")
	};
	assert_eq!(result, Class {
		name: "Class".to_string(),
		members: vec![
			ClassEntry::Function(Function {
				return_type: Type::Number,
				name: "function_with_body".to_string(),
				parameters: vec![],
				statements: vec![],
			}),
		],
	})
}

// use/package

#[test]
fn parse_use_and_packages() {
	let result = terumi::CompilationUnitParser::new().parse("use some_package
use name.space

package my_package").unwrap();
	assert_eq!(result, vec![
		TopLevelUnit::Use(PackageLevel::Root("some_package".to_string())),
		TopLevelUnit::Use(PackageLevel::Scoped(Box::new(PackageLevel::Root("name".to_string())), "space".to_string())),
		TopLevelUnit::Package(PackageLevel::Root("my_package".to_string())),
	])
}