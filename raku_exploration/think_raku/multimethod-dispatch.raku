# You can use multi-method-dispatch.   

multi sub fact(0) 
{ 
    return  1 
}

multi fibonacci(1) 
{ 
    return 1 
}
# The intresting thing is that we not only can dispatch based on the arity but also
# on a condition, but of course this is also possible to define conditions for normal
# functions.
multi sub fact(Int $n where $n > 0) 
{ 
    $n * fact $n - 1; 
}
