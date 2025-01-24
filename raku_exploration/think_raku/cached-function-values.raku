use experimental :cached;

sub nth-prime(Int:D $x where * > 0) is cached {
    say "Calculating {$x}th prime";
    return (2..*).grep(*.is-prime)[$x - 1];
}

say nth-prime(43); # The function gets only executed once
say nth-prime(43); # automatically returns its cached value 191
say nth-prime(43); # automatically returns its cached value 191
