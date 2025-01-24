# In Raku functions are first class citizens.

# Notice the & in the argument,
sub call-twice(&function)
{
    function();
    function();
}

sub say-hello-world()
{
    say "Hello, World!";
}

call-twice &say-hello-world;


# Lambdas
call-twice sub { say "Hello, World!"};

my $greet = sub { say "Hello World!"; };
$greet();

# Functions returning functions 
sub create-func ($person) { return sub { say "Hello $person!"}};
my $tom = create-func "Tom";
$tom()