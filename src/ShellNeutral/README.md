# Shell Neutral

The goal is to be stupdily simple.

## Variables

Any valid number is a valid variable name.

Set 0, 1, 2, 3+

## Labels

Labels can act as function calls or go to points.

If you use a `CALL` with the label, see `Function calls` for the behavior.

However, if you GOTO a label, it will behave like normal.

Labels are by number.

## Function calls

All variables set before the function call are reset to what they were after the function call.
This is excluding variable `0`

For parameters, set variables 1+ to the desired value.

## Calls

To call a compiler specific function, just call the number of the compiler function.

This repository standardizes the numbers.