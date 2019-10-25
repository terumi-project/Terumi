# Shell Neutral

The goal of Shell Neutral is to be a dynamically typed mess that can perfectly emulate any necessary behavior with its simple constructs

# DISCLAIMER

Anything not mentioned in this readme is intended to not be used. An example of such:

```
*"B" = "C"
*"A" = *"B"

*"D" = &"A"
// what is D?
```

There are empty holes in this language because it is not designed to be a widely used thing, it's just internally used for representing the shell neutral target all Terumi code compiles into.

## Shell Neutral pseudo representation

The pseudo representation will make sense once you read the rest of the document.

```
// this is a comment.

// this is the reference keyword.
// this is used to make it explicitly known that the given expression is a reference expression.
// it must always be used to differentiate an expression from a variable
*

// this is the dereference operator.
// this will get the value of a variable.
&

// this is a string
"STR"

// number
1234
```

# Operators

Shell Neutral doesn't have many operators. Thus, it is extremely simple.

- Label Operator
- Goto Operator
- Call Operator
- Set Operator
- Pop Operator

## Label Operator

```
// this is a label.
// labels can only be defined by numbers
:1234
```

## Goto Operator

```
GOTO :1234

*"A" = "B"

:1234

// *"A" never gets set
```

## Call Operator

The call operator has two different contexts.

- Label context: `:`
- Compiler context: `@`

Example using both:


```
:0 // MAIN

*1 = 7
CALL :1

*1 = &-1
CALL @2 // pretend 2 is a compiler call to println

POP

:1 // ADD ONE

// variable 1 is the parameter
// set 2 to one

*2 = 1

// call the compiler function for add.
// (the number specified here may not be accurate)

CALL @1

// now *-1 is equal to the result.
// negative numbers aren't modified in the context of calling labels
// we're OK here

POP
```

### Label Context

Calling a will ensure that once the caller returns, no state has changed. The only state that will change are variables who are negative numbers.

If you want to make a function that returns a value, you can call labels to achieve this.

### Compiler Context

If you call a compiler defined function, it behaves in the exact same way that a call to label does.

However, the implementation of the function is compiler defined. It may do something specific to that language, it may not.

## Set Operator

The set operator is simple.

Refer to a variable and then set it to the value specified.

```
*"VARIABLE NAME" = "THE VALUE TO SET IT TO"
```

## Pop Operator

Exits the current scope.

All programs begin with the `main` scopes - labels do not count as scopes.

The only time a new scope is entered is when the `CALL` operator is used.

`POP` will pop off a scope context. If there are no more scope contexts, the program will exit.

# Expressions

An expression can either be three things:

- A string
- A number
- A variable
- A dereference
- A concatenation

Strings, numbers, and dereference expressions are "value type" expressions. These kind of expresisons means that there are no pointers to them.

Dereference and concatenation expressions are considered "computation" expressions. There is some kind of computation going on behind the scenes when you execute them.

Variable expressions are "reference type" expressions. These kind of expressions means that the result is always going to be a reference.

A concatenation expression is represented interanally as an array of expressions.

Concatenation expressions evaluate into a string as soon as they are declared.

With expressions defined as loosely as we do, we can represent every programming paradigm possible.

## Strings

Strings are strings. You know them, love them, and we have a different definition for them.

Strings are a series of characters, spanning from the first open quote to the last open quote.

If you want to put a quote in your string, prefix it with a backslash. If you want to put a backslash in your string, put a backslash.

Double backslashes don't count as an error.

Some examples of strings:

```
"Hello, World!"
->
Hello, World!
```

```
"Multi line
spanning string!"
->
Multi line
spanning string!
```

```
"An\ Example\ O\f The \\! \"\"\\\""
->
An\ Example\ O\f The \! ""\"
```

```
"Recursion String says:
\"Recursion String says:
\\\"Recursion String says:
\\\\\"Recursion String says:
\\\\\"
\\\"
\"
"
->
Recursion String says:
"Recursion String says:
\"Recursion String says:
\\"Recursion String says:
\\"
\"
"
```

## Numbers

We don't have decimals. If you want them, multiple numbers big enough to the precision you require. Any system that implements ShellNeutral must be able to support infinitely big numbers.

Prefix your number with a minus if it is to be negative.

Example numbers:

```
-078905143287590132789053127815093278503918759013790581328970513273892017893213578257312052378571328017850215738081735298790351208971352290758728059
->
-78905143287590132789053127815093278503918759013790581328970513273892017893213578257312052378571328017850215738081735298790351208971352290758728059
```

## Variables

Variables are peculiar compared to what they are in existing languages.

Variables are just computed references to expressions. "Computed" because using a concatenation expression as the name of a variable is perfectly legal.

As per the representation spec, all variable names will be prefixed with a `*` to show that they are variable names and not expressions.

Using the derefernece operator `&` will automatically assume that the expression given is a variable and try to lookup its value.

Dereferencing without a known value will throw a runtime exception.

Simple variable examples:

```
// the variable 1 now has the string value "Hello, World!"
*1 = "Hello, World!"

// the variable A now has the string value "Hello, World!"
*"A" = "Hello, World!"

// let's set 2 to the value of 1
*2 = &*1

// this is also perfectly valid syntax for the purposes of this pseudo representation
*2 = &1
```

More complicated examples with concatenation expressions:

```
// declare some variables for starters
*1 = 2
*2 = 3
*3 = 1

// we can dereference any one of these variables forever
*"TOTAL" = &&&&&&&&&&&&&&1

// TOTAL equals 3 now

// let's assign something to a concatenation expression
*[1, 2, 3] = "A"

// not bad! let's use it somewhere
*1 = &[1, 2, 3]

// so now the variable 1 equals the string value "A"
// now here's where stuff comes into play
*1 = "A"
*2 = "B"
*3 = "C"

// concatening a variable will just evaluate into its name.
*"EXPR" = [*1, &2, *3]

// this is equivalent:
*"EXPR" = "1B3"

```