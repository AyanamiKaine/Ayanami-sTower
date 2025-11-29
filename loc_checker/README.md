# LocChecker

Really simple lines of code checker for programming language files, by default it complains if a file is bigger than 1000 LOC.

## Installation

If [available in Hex](https://hex.pm/docs/publish), the package can be installed
by adding `loc_checker` to your list of dependencies in `mix.exs`:

```elixir
def deps do
  [
    {:loc_checker, "~> 0.1.0"}
  ]
end
```

Documentation can be generated with [ExDoc](https://github.com/elixir-lang/ex_doc)
and published on [HexDocs](https://hexdocs.pm). Once published, the docs can
be found at <https://hexdocs.pm/loc_checker>.

## Build Script File

```bash
mix escript.build
```

### Run

```bash
escript loc_checker <folderPath>
```
