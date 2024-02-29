/*
- [a, &b] — a is captured by value, and b
is by reference.
- [] — A lambda that doesn’t use any variable from the surrounding scope. These
lambdas don’t have any internal state and can be implicitly cast to ordinary function pointers.
- [&] — Captures all variables that are used in the lambda body by reference.
- [=] — Captures all variables that are used in the lambda body by value.
- [this] — Captures the this pointer by value.
- [&, a] — Captures all variables by reference, except a, which is captured by value.
- [=, &b] — Captures all variables by value, except b, which is captured by reference
*/
int main (int argc, char *argv[]) {
    
    auto adder = [](int x) { return x + 1;};


    int num { 0 };
    num = adder(num);

    return 0;
}
