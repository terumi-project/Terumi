use std::path::Path;
use crate::lexer::Token;

//# Defines parser structures.

pub enum StructureComposition<'a> {
  /// Used to denote that this structure is real and came from source code.
  Real(RealStructureComposition<'a>),

  /// Used to denote that this structure is imaginary and came from the
  /// compiler.
  Pseudo(),
}

/// Represents a slice of a stream of tokens grabbed from a file.
pub struct RealStructureComposition<'a> {
  parent: &'a [Token<'a>],
  tokens: &'a [Token<'a>],
}

pub struct ParsedFile<'a> {
  composition: StructureComposition<'a>,
  
  path: &'a Path,

  package: PackageLevel<'a>,
  classes: Vec<Class<'a>>,
  contracts: Vec<Contract<'a>>,
  functions: Vec<Function<'a>>,
}

pub struct PackageLevel<'a> {
  composition: StructureComposition<'a>,
}

pub struct Class<'a> {
  composition: StructureComposition<'a>,
  
  name: &'a [u8],

  fields: Vec<Field<'a>>,
  functions: Vec<Function<'a>>,
}

pub struct Contract<'a> {
  composition: StructureComposition<'a>,
  
  name: &'a [u8],

  fields: Vec<Field<'a>>,
  functions: Vec<FunctionDeclaration<'a>>,
}

pub enum Type<'a> {
  UserClass(&'a Class<'a>),
  UserContract(&'a Contract<'a>),
  /// The primary type whose generics are to be filled, with a list of the generic
  /// definitions to fill them with.
  /// 
  /// This may look like the following in a complicated structure:
  /// Generics
  /// (
  ///     Class<T1, T2, T3>
  ///     [
  ///         number,
  ///         string,
  ///         Generics
  ///         (
  ///             Class<T1, T2>
  ///             [
  ///                 number,
  ///                 string
  ///             ]
  ///         )
  ///     ]
  /// )
  Generics(&'a Type<'a>, Vec<&'a Type<'a>>),
  String,
  Number,
  Bool,
}

pub struct Field<'a> {
  composition: StructureComposition<'a>,

  name: &'a [u8],
  value_type: Type<'a>,
}

pub struct FunctionDeclaration<'a> {
  composition: StructureComposition<'a>,
  
  name: &'a [u8],
  return_type: Type<'a>,
  parameters: Vec<Parameter<'a>>
}

pub struct Parameter<'a> {
  composition: StructureComposition<'a>,

  name: &'a [u8],
  value_type: Type<'a>,
}

pub struct Function<'a> {
  composition: StructureComposition<'a>,
  declaration: FunctionDeclaration<'a>,
  code: Vec<Statement<'a>>
}

pub enum Statement<'a> {
  NoStatementsYet(StructureComposition<'a>)
}