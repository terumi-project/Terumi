# FULL DISCLAIMER
**TERUMI IS CURRENTLY PRE-ALPHA. THIS MEANS THAT I RESERVE THE EXCLUSIVE RIGHT TO ABSOLUTELY DESTROY ALL COMPATIBILITY DURING THE PROCESS OF MAKING TERUMI, AND YOU ARE ENTITLED TO ABSOLUTELY NO GUARENTEES.**

# This Branch
This branch is `terumi-rewrite`. This is the rewrite of the compiler, written in pure Terumi. However, the compiler used is the C# compiler (with minor modifications, as can be seen in this branch). This means that the resulting code will not perfectly model the ideas Terumi has in mind, however, it'll be a start. With a compiler written in Terumi, the compiler can begin to be boostrapped to itself once it makes significant progress.

# Terumi [![Discord Server](https://img.shields.io/discord/652702761665691658?label=Discord&style=flat-square)](https://discord.gg/NpDXYev)
```
main()
{
	@println("Hello, World!")
}
```

Terumi is a programming language designed to replace massive shell script projects.

- Compiles to Powershell and Bash (and C). No interpreters (Python, Ruby) necessary.
- First class `class` support, unlike shell scripts.
- Strong yet flexible type system, similar to TypeScript.
- Stupidly simple syntax, similar to C.
- Hassle-free package manager, consciously designed last.
- Versionless, content-focused dependencies.

# Get Terumi

1. Make sure you have [.NET Core 3.0 or higher](https://dotnet.microsoft.com/download)
2. Make sure you have [git](https://git-scm.com/book/en/v2/Getting-Started-Installing-Git)

```shell
git clone https://github.com/terumi-project/Terumi.git
cd Terumi
dotnet publish -c Release
cd src/bin/Release/netcoreapp3.0/

# windows
terumi new --name "project_name"
cd project_name
..\terumi compile --target powershell

# linux
dotnet terumi.dll new --name "project_name"
cd project_name
dotnet "../terumi.dll" compile --target bash
```

# Example

*[Click here to read more](#about-the-terumi-language)*

```
class Printer
{
	print(string data)
		@println(data)
}

class MaliciousPrinter
{
	print(string data)
		@println("Stole '{data}'!")
}

print_secure_data(Printer printer)
	printer.print("Secure Data")

main()
	print_secure_data(new MaliciousPrinter())
```

*Note: braces shouldn't omitted so often in a real codebase.*

# Help Out
Terumi is extremely young, immature, and not production ready. Terumi needs your help.

- **Make something with Terumi!** Submit issues as you find bugs or see that something is incomplete.
- [Give me money](https://www.patreon.com/sirjosh3917). I like money. I can host Terumi REPLs with money.

## Who uses Terumi

Nobody, yet. Be the first and [let us know you're using it.](https://github.com/terumi-project/Terumi/issues/new/choose)

# Who should use Terumi

If you're the author of a large shell script project, you are the consumer of which this programming language markets towards.

A few examples of "large shell script projects" are the following:

- [msm](https://github.com/msmhq/msm)
- [pihole](https://github.com/pi-hole/pi-hole)
- [nvm](https://github.com/nvm-sh/nvm)

# About the Terumi Language

The claims Terumi makes are all backed thanks to its **type system**. This is a strong selling point for Terumi, as it features the perfect balance between dynamic and strongly typed, and enables the other parts of Terumi (dependency system) to flourish.

## Type System

The Terumi type system is simple: *if it looks like a duck, it is a duck.*. Your code won't need to even think about being unit testable or modular, because it simply *just is*.

This is some extremely modular code.
```
class ProgramInfo
{
	string get_program_name()
		return "Cool Program"
}

print_program_info()
	print_program_info(new ProgramInfo())

print_program_info(ProgramInfo info)
{
	@println("- - - {info.get_program_name()} - - -")
}
```

Initially, it looks tightly coupled to `ProgramInfo`. However, *if it looks like a duck, it is a duck.*. You can pass in `any` object to `print_program_info(ProgramInfo)`, as long as the first parameter *looks like* a `ProgramInfo`. Thus, the following compiles:

```
class OtherProgramInfo
{
	string _name

	ctor(string name) _name = name
	string get_program_name() return _name
}

main()
{
	print_program_info(new OtherProgramInfo("Super Cool Program"))
}
```

Because `OtherProgramInfo` *looks* like a `ProgramInfo`, it can be passed in. It has the same public methods and public fields.

## Dependency System

The package manager was designed to be an *after thought*, but dependencies were prioritized. This conscious decision allows the language to have arguably the best dependency system - or as it should be named, the *tool* system. Terumi handles dependencies in a fundamentally different way, which prefers thinking about dependencies moreso as "tools to obtain a goal" rather than "hunks of reusable code".

A Terumi project has a `config.toml` file. This file needs to only contain a list of dependencies that the project requires, and allows them to be specified using a `git` repo, or a `file path`. If a project does not have any dependencies, it can be safely omitted. Often times there is a `name = ""` field in the configuration file, but this is primarily used for the package manager, and can be omitted.

```
# this is the entire config.toml file
[[libs]]
git_url = "https://github.com/terumi-project/terumi_std"
branch = "master"
commit = "725a0c5ee2c2c7f2d92cce09be5b4292db6e3b44"

[[libs]]
path = "some/path/to/another/cool_project"
```

Terumi will resolve dependencies by fetching them at the specified git url, using the branch and commit as specified. If the branch and commit aren't specified. Terumi will refuse to fetch these a given dependency.

1. Dependency Scopes: How Terumi handles dependencies of a dependency.
2. Type System: How Terumi handles types in dependencies of a dependency.
3. Content Based: How Terumi handles having multiple "versions" of the same dependency.

1. Dependency Scopes

There are 3 "scopes of code" in the compiler:
- The Project: Your code.
- Immediate Dependencies: The dependencies your code depends on.
- Indirect Dependencies: All the dependencies that every dependency depends on.

`The Project` will only see the code, and can only use the code of `Immediate Dependencies`.

2. Type System

In a language such as Java, dependency scopes like Terumi simply wouldn't be possible. What if a method in an immediate dependency references an indirect dependency? Terumi handles this, by not handling it. The Type System does.

You cannot `new` objects that are not visible. You cannot declare variables with types that are not visible. The objects that are visible are the objects within the current namespace, and the objects in namespaces included with `use`.

As a result, dependencies should be thought of moreso as *tools*.

3. Content Based

Thanks to the scopes of dependencies, they become content based. Version conflicts no longer exist.

If you wish to try include two versions of a dependency into your project expecting it to work, it won't. There is a preferred way to overcome this: "wrapper packages". A wrapper package is a package that includes inner packages. The inner packages require a specific version of a dependency, and provides version specific classes. The wrapper packages includes the inner packages, and provides a version agnostic way to use the package.

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
