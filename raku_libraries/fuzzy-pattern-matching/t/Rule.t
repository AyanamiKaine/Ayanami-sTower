use Test;
use lib 'lib'; 

use Rule; 

# ok , Condition, FailMessage 
ok Rule.new(), "Rule class creation";

done-testing;  # optional with 'plan'
