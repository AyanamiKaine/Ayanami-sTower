use Test;
use lib 'lib'; 

use FPM; 

# ok , Condition, FailMessage 
ok FPM.new(), "FPM class creation";

done-testing;  # optional with 'plan'
