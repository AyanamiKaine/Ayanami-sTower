sub add(Int $a, Int $b) {
    return $a + $b;
}

# What in gods name is $a, why not just a?

# $ is called a sigil, and in Raku, it indicates a scalar variable
# $ (Scalar): Holds a single value. This value can be a number, string, object, or even a code reference

# Other types are :
#@ (Array): Represents an ordered list of values.
#% (Hash): Represents a collection of key-value pairs (like a dictionary).
#& (Code): Represents a subroutine or function

say add(2, 8)