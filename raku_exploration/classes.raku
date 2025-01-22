class Dog {
    has Str $.name; # Public attribute

    method bark {
        say "Woof! My name is $!name";
    }

    method rename (Str $new-name) {
        $!name = $new-name
    }
}

my Dog $spot .= new(name => "Spot");
$spot.bark;        # Output: Woof! My name is Spot
$spot.rename("Buddy");
$spot.bark;        # Output: Woof! My name is Buddy

