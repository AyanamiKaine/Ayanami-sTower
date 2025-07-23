NAME
====

Language::Stella - an ECS language first.

SYNOPSIS
========

```raku
use Language::Stella;
```

DESCRIPTION
===========

`Language::Stella` is an ECS first-class language. The main idea to model a domain is using ECS. Each program has its own ECS world. There are two main objectives we want to achieve the first one is having an ECS language. The second one is being able to sandbox the language in itself so it can run arbitrary code.

We want to be able to just share programs via code that can be directly executed. To correctly achieve this we need permissions based sandboxing, so we can restrict by default the usage of system APIs that could do harm to the user. This includes the usage of memory and CPU time. When a sandbox runs a program it should do no harm to the underlying program that executes it and the computer itself.

AUTHOR
======

Ayanami Kaine <personal@ayanamikaine.com>

COPYRIGHT AND LICENSE
=====================

Copyright 2025 Ayanami Kaine

This library is free software; you can redistribute it and/or modify it under the Artistic License 2.0.

