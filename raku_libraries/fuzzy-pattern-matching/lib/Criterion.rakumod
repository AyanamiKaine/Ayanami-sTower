# A criterion is a single thing that has to be true about a fact
unit class Criterion;


has &.predicate is required;

multi method Match ($value) returns Bool {
    return &!predicate($value);
}