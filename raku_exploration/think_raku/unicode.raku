# Unicode is completly valid for anything!

# As strings!
my $name = "🔥";
say "My name is " ~ $name; 

# And as symbols
sub prefix:<Σ>(\x) { x.sum }

# Now we can write Σ @[1, 2, 3, 4, 5] instead of @[1, 2, 3, 4, 5].sum
say Σ @[1, 2, 3, 4, 5];

# We could write pi or π
say π;
say "Does π equal pi? {π == pi}";