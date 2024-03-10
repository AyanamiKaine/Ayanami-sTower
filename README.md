## Monorepo of Ayanami

## Short Summary
- The main idea behind this monorepo is the creation of software capital.
- Applications dont refrence other applications (no inter-dependency between them).
- Applications have longer english like names.
- Packages have short prefix names, like svc (Svelte Components).
- Packages are cohesive bundeling of similar components.
- Components (files in packages) are prefixed with the package name.
- A flat package dependency hierarchy is preferred
- If one component from one package is refrenced we implicity say we depend on the complet package and not just the one component (You either depend on the package or you dont depend on the package)
- Reuse is encouraged.

## Why a Monorepo?

To facicilate reuse and creation of cross application libraries. Not only for reuse between same languages but also for reuse of patterns across languages.

## Why are Tests for components and their implementation side by side?

A test for an interface and a desired outcome(behavior) (Black Box Testing) are highly cohesive with interface itself not only shows a test if a interface works as expected it shows how it can be used. This is valuable for people that use the interface. They form a cohesive logical unit so they should be physical cohesive too (i.e. next to each other).

## Long Term Goal
- Having Bazel as the general build tool for everything in this repo (Now this is not true as many apps need other build tools like npm, visual studio, etc.).
    - And/Or Using Cmake for all C++ modules, more testing is required.
- Having a large colletion of reusable components that are shared by many applications.
- Each new application increases software capital bit by bit.

## Package Prefixes Explained
- SV    = Svelte
- CS    = CSharp
- JS    = JavaScript
- CPP   = C++
- PY    = Python 
