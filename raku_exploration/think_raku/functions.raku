# Functions in raku are called sub routines. 

say round 20.5, 1; 
# Most of the time you can 
# ommit the parenthesises, 
# this is done for readability.

# See for your self what is better to read?
say(round(20, 1));

# some functions have method equivalents so we can say
# That does not mean that raku has Universal Function Call Syntax (UFCS) 
say 20.2.round(1);
# But we could turn it into one using &
sub is-even(Int $n) { $n %% 2 }
say 4.&is-even;  # True

# Call by value

# Call by refrence

sub add-side-effect-free(Int $a, Int $b) {
    return $a + $b;
}
# What if we want instead add the number b to the variable a and mutate it?
# We must say "is rb" because variables cannot be mutated by default in functions
sub add-mutate(Int $number-to-mutate is rw, Int $number-to-add) 
{
    $number-to-mutate += $number-to-add;
}

my $number = 0;

add-mutate($number, 5);

say $number; # $number is now 5