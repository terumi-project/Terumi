[![Discord Server](https://img.shields.io/discord/652702761665691658?label=Discord&style=flat-square)](https://discord.gg/NpDXYev)

# FULL DISCLAIMER
**TERUMI IS CURRENTLY PRE-ALPHA. THIS MEANS THAT I RESERVE THE EXCLUSIVE RIGHT TO ABSOLUTELY DESTROY ALL COMPATIBILITY DURING THE PROCESS OF MAKING TERUMI, AND YOU ARE ENTITLED TO ABSOLUTELY NO GUARENTEES.**

*i mean i'll try to not break much but imma do what i want*

# Terumi
Terumi is a programming language that transpiles to both powershell and bash, and aims to achieve the following goals:

- Completely reinvent the idea of dependencies
- Complete embrace of open source
- Enable huge shell script based codebases to be maintained easier (e.g. [msm](https://github.com/msmhq/msm), [pihole](https://github.com/pi-hole/pi-hole), [nvm](https://github.com/nvm-sh/nvm) to name a few)
- Extremely compact output

Currently, Terumi doesn't hit its goals as hard as it needs to. Only when 1.0.0 comes around should it be recommended for use in the real world.

## Using it

1. Make sure you have [.NET Core 3.0 or higher](https://dotnet.microsoft.com/download)
2. You'll also need `git`

```
git clone https://github.com/SirJosh3917/Terumi.git
cd Terumi
dotnet publish -c Release
cd src/bin/Release/netcoreapp3.0/

# use terumi
./terumi new --name "some_cool_project"
cd "some_cool_project"
../terumi compile --target bash
../terumi compile --target powershell

# install a dependency
../terumi install -p "terumi"
```

## Helping out
Terumi is extremely young and immature, and not suitable for production in the slightest. Want to see this language come into fruition and actually be usable?

- Make something with the language as is! If something's frustrating, not easy, unclear, or not implemented but necessary, leave an issue! The language can't be improved if nobody says anything about it
- [Give me money](https://www.patreon.com/sirjosh3917). If you choose to do so, make sure that it's not too early in the project for the same reasons one would not want to donate to a kickstarter. With money, I can reliably host online services, such as a REPL or more.

## Vaguely Similar Projects

- [Batsh](https://github.com/BYVoid/Batsh)  - aims to create platform independency with batch and bash
- [ShellJS](https://github.com/shelljs/shelljs) - aims to create platform independency with unix tools implemented in js
- [Bish](https://github.com/tdenniston/bish) - aims to make bash usable
- [Zsh](http://www.zsh.org/) - aims to be a better `sh`
- [Powscript](https://github.com/coderofsalvation/powscript) - aims to make bash codebases easier to read
- [BashClass](https://github.com/amirbawab/BashClass) - aims to bring classes to bash
- [Plumbum](https://plumbum.readthedocs.io/en/latest/index.html) - aims to create platform independency with unix tools implemented in python
- [Harpy](https://github.com/markpwns1/Harpy) - aims to make writing code in batch easier
- [Python](https://github.com/python/cpython) - not a shell script, but an interpreter for a very powerful language that is often used as a substitute for shell scripts

## Goals

Note: All goals are to be achieved in 1.0.0 - currently, most of what the goals specify, aren't implemented yet. If a goal is specified in the present tense, it may not be implemented yet (e.g.: `Extremely compact output`)

### Dependencies

In Terumi, dependencies are not seen as they traditionally are - they are to be seen as tools.

When a dependency is included, only the code in that dependency is visible. Dependencies that you explicitly include are known as "direct dependencies". Likewise, dependencies of a direct dependency are known as "indirect dependencies". All indirect dependencies are completely unusable - you cannot refer to them by their name, and thus cannot instantiate them or have them as fields to any variables.

In order to remedy issues with being unable to use indirect dependencies, Terumi's type system is, in a nutshell, "if it looks like a duck, it is a duck". Take the following code:

```
class Runnable
{
	run() {
		@println("I'm running!")
	}
}

contract Runner
{
	Runnable the_runner
}

do_running(Runner runner)
{
	runner.the_runner.run()
}

class Nice
{
	ctor() {
		the_runner = this
	}
	
	Nice the_runner
	
	run() {
		@println("Nice.")
	}
}

main()
{
	do_running(new Nice())
}
```

Since `Nice` looks like a `Runnable` and a `Runner`, it can be used as both.

Terumi also uses `git` for dependencies:

### Open Source (related to dependency system)

The point of open source ties in closely with the dependency system. The way Terumi does package management, is through git repositories.

*Any* git repository can be used to pull packages from.

### Enable easier maintenance

Thanks to first class `class` support, a great dependency system, and simplicity, Terumi enables huge codebases to be maintained easier.

Unit testing (if that's your thing), code reuse (classes), productivity (type system), and slimmer shell files (less to download) can be achieved simply by rewriting your entire codebase in Terumi.

### Extremely compact output

Since Terumi has all the source code available to it when it compiles, and it doesn't have the burden of having to compile to some intermediate language, it is capable of making huge decisions to wipe entire parts of a codebase out.

- Don't use a certain method in a class? It can dissapear.
- Can a conditional statement be calculated at compile time? The overhead dissapears.

Terumi is able to take complicated code, such as this
```
class Dotnet
{
	string _path

	ctor(string path) {
		_path = path
	}
	
	string path() {
		return _path
	}
}

class VersionCommand
{
	Dotnet _dotnet

	ctor(Dotnet dotnet) {
		_dotnet = dotnet
	}
	
	run() {
		@/{_dotnet.path()} --version
	}
}

main()
{
	new VersionCommand(new Dotnet("dotnet")).run()
}
```
into
```
@/dotnet --version
```
at compile time.

## Syntax

Terumi's syntax is extremely similar to other languages, allowing you to use the language without having to google every half a nanosecond for syntax specific things (looking at you, powershell and bash).

```
// packages are used to package up multiple bits of code in multiple files as
// one so you can reference all of it with a `use` statement

// any identifier since ever is always `lowercase_separated_by_underscores`, except for class names
// 

// if no package is specified, it is inferred by the folder hierarchy
package my.super_cool.package

// include all of the things in some.cool.package in your code
use some.super_cool.package

// this is a comment
/* so is this */

// any top level braces are Allman style (https://en.wikipedia.org/wiki/Indentation_style#Allman_style)
main()
{
	// no statements have semicolons - newlines are expected to end a statement
	// variable declarations
	string a_string = "hello"
	string another_string = "world"
	
	// example of string interpolation
	string interpolation = "{a_string} {another_string}"
	
	// strings can span multiple lines
	// if the first character of a string is a newline,
	// the newline isn't included
	string valid_string = "
X | O | O
--+---+--
X | X |  
--+---+--
X |   | O"
	
	// a method call prefixed with `@` is a compiler method call
	// these are important because their implementation is determined by the compiler
	@println(interpolation)
	
	// like shell scripts, terumi can execute raw commands
	// @/ represents a call to something
	
	@/type test.txt
	
	// a @/ ends at the end of a newline, and the data is treated as any regular string
	
	string file = "test.txt"
	@/type \"{file}\"
	
	// terumi supports conditionals
	
	bool condition = true
	
	if (condition) {
		@println("condition is true")
	} else {
		@println("condition is false")
	}
	
	for(number i = 0; i < 10; i++) {
		@println("iteration!")
	}
	
	number i = 0
	while(i++ < 10) {
		@println("iteration!")
	}
	
	// these are supported too
	
	if condition {
		@println("no parenthesis!")
	}
	
	for ({
		@println("initialization block")
		number i = 0;
	}; do_condition(i); {
		@println("end block")
		i++
	}) {
		@println("in for loop with i:")
	}
}

bool do_condition(number input) {
	@println("do_condition called")
	return input < 10
}

// classes are PascalCase
class AClass
{
	// a field or method is considered 'private' when it is prefixed with an underscore
	
	// fields
	string _a
	number _b

	// methods inside a class are K&R style (https://en.wikipedia.org/wiki/Indentation_style#K&R_style)

	// ctor is short for `constructor`
	ctor() {
		_a = "a"
		_b = 7
	}
	
	// if you are going to have getters and setters (which terumi doesn't recommend using), this is the preferred style:
	string a() {
		return _a
	}
	
	a(string value) {
		_a = value
	}
}
```