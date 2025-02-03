# A rule is a list of criterions that is matched against a query.
# Each rules has an associated response, this is simply the thing that happens when a rule is matched.
unit class Rule;


multi method Match (@criterions) {
    for @criterions -> $criterion {
        say $criterion; 
    }
}