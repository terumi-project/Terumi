# Terumi Compiler
The compiler for Terumi is slightly complicated. There are multiple stages.

## Stages of compilation

1. Lexing

Lexing converts the stream of characters in the code into individual `Token`s, which makes the next stage's job easier. `Token`s maintain a reference to their characters for error messages. This stage usually catches simple typo errors, eg. `class Si[erComputer`.

2. Parsing

Parsing converts a stream of `Token`s into *loose data structures*. A series of tokens, as such:
```
'if' | whitespace | open parenthesis | 'true' | closing parenthesis | whitespace | at | identifier 'println' | open parenthesis | string "It is true!" | close parenthesis
```
into a data structure which will verify that the given tokens make sense
```
if:
    - comparison: 'true'
    - true clause:
        - method call:
            - name: '@println'
            - args: ["It is true!"]
```
This stage usually catches errors where tokens are in an invalid order, eg. `if (while ==) }{`

3. Binding

Binding is where, internally, the code links itself together. Method calls are linked to their definitions, variable references linked to their declarations, etc. etc.
This stage usually catches error where stuff doesn't exist, eg. a call `help_em()` where no method called `help_em()` is defined

The compiler has complete awareness of the code at this stage, Binding. Now, the compiler breaks down the code into simpler and simpler pieces that it can put through a shell script.

If a compiler call is unable to be resolved, it is left as 'null' in wait for optimization step down the line.

4. Deobjectification

Deobjectification is where the code maps all fields of classes into one large object (for inheritence) and all individual methods into methods which break into other methods, given the object passed in. At a high level, it looks like this:
```
class MyClass
{
    string _thing
    
    ctor (string value) {
        _thing = value
    }
    
    print_thing(number extra) {
        @println("MyClass ({to_string(extra)}): {_thing}")
    }
}

class AnotherClass
{
    string _thang
    
    ctor (string value) {
        _thing = value
    }
    
    print_thing(number extra) {
        @println("AnotherClass ({to_string(extra)}): {_thang}")
    }
}

do_thing(MyClass instance)
{
    instance.print_thing(7)
}

main()
{
    do_thing(new MyClass("a"))
    do_thing(new AnotherClass("b"))
}
```
into, essentially,
```
class GlobalData
{
    string type
    string a
}

void_print_thing_number(GlobalData global, number extra)
{
    if (global.type == "MyClass") {
        MyClass_print_thing(global, extra)
    } else if (global.type == "AnotherClass") {
        AnotherClass_print_thing(global, extra)
    } else {
        @panic("Runtime exception: didn't expect an object of type {global.type} to be passed to void_print_thing_number")
    }
}

MyClass_print_thing(GlobalData global, number extra)
{
    @println("MyClass ({to_string(extra)}): {global.a}")
}

AnotherClass_print_thing(GlobalData global, number extra)
{
    @println("AnotherClass ({to_string(extra)}): {global.a}")
}

do_thing(GlobalData global)
{
    void_print_thing_number(global, 7)
}

main()
{
    GlobalData data = new GlobalData()
    data.type = "MyClass"
    data.a = "a"
    do_thing(data)
    
    data = new GlobalData()
    data.type = "AnotherClass"
    data.a = "b"
    do_thing(data)
}
```
***Why?***

Shell scripts don't have an idea of "classes". Terumi's type system introduces additional complexity, as anything that "looks like" a given type can be used as said given type. Since `AnotherClass` looks exactly like `MyClass`, it can also be used in `do_thing`.

All fields and data are unified under one class object. Unlike actual programming languages, like C# or Java, where fields are allocated enough space to store a reference or a value type (primitive in Java), in shell scripts, classes are simple numbers where there are variables set on that specific number to point to "a field". An example can be shown here:

```bash
# the 'gc' is really just a variable
gc=a

# 'object_one' is really just 'a'
object_one="$gc"

# gc is incremented for more objects
gc="$gc""a"

# to store a value of '7' into field 'thing' on 'object_one',
# we actually set a variable named 'athing' to '7'.
declare -g "$object_one""thing"=$((7))
```

Thus, it makes sense to have a "global object" as shown above.


The `type` variable on the GlobalData is to support breaking into the implementation of a specific method for a given class. This allows Terumi to call methods on an object, and use the implementation of the object that is necessary.

All fields are merged in the "global object", to support referring to a given object as another object and requesting the given object for fields. All similarly named fields are first merged to do this. To save fields in this global object, fields of similar types are also merged and referred to, even if they're not similar by name. This can be shown here:
```
class Thing
{
    string cool
    number radical
}

class Other
{
    string business
    string attire
    number height
    number width
}
```
into
```
class GlobalObject
{
    string a // 'cool', 'business'
    string b // 'attire'
    number c // 'radical', 'height'
    number d // 'width'
}
```

**This deobjectification step makes the next step significantly easier.**

All garbage on this step is cleaned up in the 'optimization' step down the line.

5. Varcode translation

Terumi's 'Varcode' exists to speed up the implementation of target languages in Terumi. It appears as a register-inspired languages, with extremely limited concepts:

- Declaring fields on variables
- Getting fields on variables
- Setting variables
- Getting variables
- Declaring methods
- Calling methods
- 'If'
- 'While'

There are no gotos as to prevent the use of them at all. Gotos are unsupported in most languages, and the overhead of a state machine is too much to bear.

With varcode, everything becomes easier, because there is no concept of parameters - only identifiers. A method call may look similar to the following:
```
0 = "Hello, World!"
1 = @println(0)
```
Looping constructs do as well:
```
0 = 0 // set 0 to 0
1 = 10 // set 1 to 10
2 = 0 < 1 // check if var 0 < var 1, aka: 0 < 10?
while(2) { // while the variable 2 is true, aka: true
    3 = 1 // set var 3 to the value 1
    0 = 0 + 3 // set var 0 equal to the adding the variable 0 with 3, aka: 0 + 1, so set the variable 0 to 1
    2 = 0 < 1 // check if var 0 is < var 1, aka: 1 < 10?
}
```
Since expressions are not nested, this makes optimization extremely easy to perform.

6. Optimization

At the core of Terumi, is optimization. Optimization is what allows Terumi users to write relatively wasteful code, and allows Terumi itself to generate garbage, but it'll all be optimized away. Giant classes and method calls turn into a single method call, thanks to optimization. Optimization is the water Terumi programmers drink and optimization is the air Terumi programmers breathe. Without optimizations, your shell scripts would be bloated at least 10 times in size.

Various, numerous, huge amounts of optimizations are to be performed.

- Constant inlining
- Method call inlining
- Compiler method call execution
- Control flow reductions
- more lol

Once all of the optimizations are done, the output varcode is garbage free and small. Then, there's the next step

7. Treeification (Treecode)

Treeification is where var code turns back into a tree. While compiler targets may stop at optimization and write out the resulting varcode, for a shell script, varcode is wasteful as most of varcode can be translated into "set this variable to that", and then "use this variable you just set and never use it again".

By treeifiying varcode, more complicated constructs are created and must be dealt with, but the primary goal of inlining useless variable declarations is achieved with treeification.

## Code generation

Compilation ends at code generation. Usually, a given compiler target will take in Varcode or Treecode and produce some kind of language output - powershell, bash, or whatever fork of Terumi you're using may have more compiler targets.

# Current state of things

1. Lexing - [x]
2. Parsing - [x]
3. Binding - [x]
4. Deobjectification - [ ] <-- WIP
5. Varcode translation - [ ]
6. Optimization - [ ]
7. Treeification - [ ]
Varcode Codegen: [x]
Treecode Codegen: [ ]

Currently, output is being produced from binding straight into a messy varcode translation step, and then to codegen.