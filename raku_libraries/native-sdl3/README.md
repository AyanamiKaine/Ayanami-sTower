# SDL 3 Raku Wrapper

## Why?

I wanted to learn more about Raku and I am always intrested in interactive with native code.

## Symbol Names

All symbols should have the exact name, as defined in sdl3. The entire structure as well as the SDL naming convention should be the same. This makes it trivial to mirror the sdl3 examples to raku examples.

## Incremental Development

We will incrementally develop the wrapper, implementing an example one by one.

## Where the SDL3 library needs to be?

When you installed SDL3 on linux using your package manager of choice nothing needs to be done. If you want to distribute the library yourself. Always place the library in the working directory you call raku.

## To Install

```
zef install .
```

After that you can easily say

```raku
use SDL3::Rect;
```
