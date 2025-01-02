# Universal Standard Library

## Problem

When implementing a new programming language, one thing is always missing. A standard library that makes common things easy. For example trimming strings, checking if a string contains a substring, getting a substring from a string, etc.

### Remarks

All those features can be implemented in any sophisticated programming language. This is not the problem. The problem lies in reinventing the wheel. What if we could take a great and vast implemented standard library found in C# and dotnet and "simply" use it in our language? Not all features could simply be turned into a method call that can be exposed over an ffi some need runtime features but intrestingly enough using the AOT compiler for dotnet makes it possible to compiler native shared libraries that can be used by any C-FFI compatible interface.

## Solution

Implementing a C-FFI compabtible shared library that can be opened by any language with a working C-FFI.