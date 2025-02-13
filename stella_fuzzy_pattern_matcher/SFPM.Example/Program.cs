using SFPM;

/*
Here we define a list of facts we want to match against a list of rules.
*/

var query = new Query()
    .Add("who", "Nick")
    .Add("concept", "onHit")
    .Add("curMap", "circus")
    .Add("health", 0.66)
    .Add("nearAllies", 2)
    .Add("hitBy", "zombieClown");

/*
Here we define a list of rules that are composed out of a list of criteria.

A rule is matched when its the most specific one, a rule is rejected automatically when one criteria does not match.

If multiple rules match completly, the rule with the most criteria is automatically selected.
If multiple rules with the same number of criteria are matched a random rule is selected.
Also if a rule has a defined priority then a rule with the higher priority is selected. 
*/
List<Rule> rules = [
            new Rule([
                    new Criteria<string>("who", who => { return who == "Nick"; }),
                    new Criteria<string>("concept", concept => { return concept == "onHit"; }),
                    new Criteria<string>("hitBy", hitBy => { return hitBy == "zombieClown"; }),
                    new Criteria<string>("curMap", curMap => { return curMap == "circus"; }),
                ], ()=>{
                    Console.WriteLine("I hate circus clowns!");
                }),
            new Rule([
                    new Criteria<string>("who", who => { return who == "Nick"; }),
                    new Criteria<string>("concept", concept => { return concept == "onHit"; }),
                ], ()=>{
                    Console.WriteLine("Ouch");
                }),
            new Rule([
                    new Criteria<string>("who", who => { return who == "Nick"; }),
                    new Criteria<string>("concept", concept => { return concept == "onHit"; }),
                    new Criteria<int>("nearAllies", nearAllies => { return nearAllies > 1; }),
                ], ()=>{
                    Console.WriteLine("ow help!");
                }),
            new Rule([
                    new Criteria<string>("who", who => { return who == "Nick"; }),
                    new Criteria<string>("concept", concept => { return concept == "onHit"; }),
                    new Criteria<string>("curMap", curMap => { return curMap == "circus"; }),
                ], ()=>{
                    Console.WriteLine("This Circus Sucks!");
                }),
            new Rule([
                    new Criteria<string>("who", who => { return who == "Nick"; }),
                    new Criteria<string>("concept", concept => { return concept == "onHit"; }),
                    new Criteria<string>("hitBy", hitBy => { return hitBy == "zombieClown"; }),
                ], ()=>{
                    Console.WriteLine("Stupid Clown!");
                }),
        ];

// Should print "I hate circus clowns!"
query.Match(rules);
