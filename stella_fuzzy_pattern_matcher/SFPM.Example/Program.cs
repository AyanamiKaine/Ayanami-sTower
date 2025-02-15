using SFPM;

/*
Here we define a list of facts we want to match against a list of rules.
*/

var query = new Query()
    .Add(key: "who", value: "Nick")
    .Add(key: "concept", value: "onHit")
    .Add(key: "curMap", value: "circus")
    .Add(key: "health", value: 0.66)
    .Add(key: "nearAllies", value: 2)
    .Add(key: "hitBy", value: "zombieClown");

/*
Here we define a list of rules that are composed out of a list of criteria.

A rule is matched when its the most specific one, a rule is rejected automatically when one criteria does not match.

If multiple rules match completly, the rule with the most criteria is automatically selected.
If multiple rules with the same number of criteria are matched a random rule is selected.
Also if a rule has a defined priority then a rule with the higher priority is selected. 
*/
List<Rule> rules = [
            new Rule(criterias:
            [
                    new Criteria<string>(factName: "who", predicate: who => who == "Nick"),
                    new Criteria<string>(factName: "concept", predicate: concept => concept == "onHit"),
                ], payload: ()=>{
                    Console.WriteLine(value: "Ouch");
                }),
            new Rule(criterias:
            [
                    new Criteria<string>(factName: "who", predicate: who =>  who == "Nick" ),
                    new Criteria<string>(factName: "concept", predicate: concept => concept == "onHit"),
                    new Criteria<int>(factName: "nearAllies", predicate: nearAllies => nearAllies > 5),
                ], payload: ()=>{
                    Console.WriteLine(value: "ow help!");
                }),
            new Rule(criterias:
            [
                    new Criteria<string>(factName: "who", predicate: who =>  who == "Nick" , predicateName: "IsNick"),
                    new Criteria<string>(factName: "concept", predicate: concept => concept == "onHit" ),
                    new Criteria<string>(factName: "curMap", predicate: curMap => curMap == "circus" ),
                ], payload: ()=>{
                    Console.WriteLine(value: "This Circus Sucks!");
                }),
            new Rule(criterias:
            [
                    new Criteria<string>(factName: "who", predicate: who => who == "Tom", predicateName: "IsTom"),
                    new Criteria<string>(factName: "concept", predicate: concept => concept == "onHit", predicateName: "ConceptIsOnHit"),
                    new Criteria<string>(factName: "hitBy", predicate: hitBy => hitBy == "zombieClowns", predicateName: "HitByZombieClowns"),
                ], payload: ()=>{
                    Console.WriteLine(value: "Stupid Clown!");
                }),
            new Rule(criterias:
            [
                    new Criteria<string>(factName: "who", predicate: who => who == "Nick"),
                    new Criteria<string>(factName: "concept", predicate: concept => concept == "onHit"),
                    new Criteria<string>(factName: "hitBy", predicate: hitBy => hitBy == "zombieClown"),
                    new Criteria<string>(factName: "curMap", predicate: curMap => curMap == "circus"),
                ], payload: ()=>{
                    Console.WriteLine(value: "I hate circus clowns!");
                }),
        ];

// Should print "I hate circus clowns!"
query.Match(rules: rules);
