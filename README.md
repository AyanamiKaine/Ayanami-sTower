## Monorepo of Ayanami

## Short Summary

-   The main idea behind this monorepo is the creation of software capital.
-   Applications dont refrence other applications (no inter-dependency between them).
-   Applications have longer english like names.
-   Packages have short prefix names.
-   Packages are cohesive bundeling of similar components.
-   Components (files in packages) are prefixed with the package name.
-   A flat package dependency hierarchy is preferred
-   If one component from one package is refrenced we implicity say we depend on the complet package and not just the one component (You either depend on the package or you dont depend on the package)
-   Reuse is encouraged.

## Getting submodules

You should run

```bash
git submodule update --init --recursive
```

## Notes

For some intresting notes about various aspects of things i did in the repo, why and what my opinions on them are see the notes folder. See the "OnAI" note for opinions on LLM usage in the repo.

## Why a Monorepo?

To facicilate reuse and creation of cross application libraries. Not only for reuse between same languages but also for reuse of patterns across languages.

## C# Defaults

see:

-   Directory.Build.props
-   Directory.Packages.props
-   CSharpy Formater
