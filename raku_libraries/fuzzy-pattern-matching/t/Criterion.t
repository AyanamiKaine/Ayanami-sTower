use Test;
use lib 'lib'; 

use Criterion; 


#The last statement of a lambda is the implicit return value of the block
my $criterion-hello-world = Criterion.new(predicate => -> $x --> Bool { $x eq "Hello, World" });

# ok , Condition, FailMessage 
ok $criterion-hello-world, "Criterion-hello-world class creation";
ok $criterion-hello-world.Match("Hello, World"), "Criterion predicate match";
ok $criterion-hello-world.Match("Hello") == False, "Criterion predicate does NOT match";

my $criterion-range = Criterion.new(predicate => -> $x --> Bool { $x > 50});

ok $criterion-range, "Criterion class creation";
ok $criterion-range.Match(100), "Criterion predicate match";
ok $criterion-range.Match(20) == False, "Criterion predicate does NOT match";

done-testing;  # optional with 'plan'
