# TLDR

- The package manager will create a ./lib folder in your fennel project and will put there all needed libraries
- Packages will only be local to the project itself
- No Global Packages. (For Now)

# How to Use

- `fennel_pkg install PACKAGE_NAME`
    - installs the package in the working directory.


# TODO

- Create a flutter GUI

# Requirements

- (or (>= Lua 5.1) (= luajit))

# Notes

It would be really nice if the automatic inclusion of a dependency on a package would mean installing it automatically if not found.
We could:
    - A: Can each lua file and search for requires
    - B: Define a .package json file where we need to write our dependencies (probably a better choice)
