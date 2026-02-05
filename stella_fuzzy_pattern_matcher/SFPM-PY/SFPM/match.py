def match(facts = {}, rules = None, *payload_args):
    currentHighestScore = 0
    matched_rules = []

    rules.sort(key= lambda rule:  rule.count())

    for rule in rules:
        matched, matched_amount = rule.evaluate(facts)

        if matched and matched_amount > currentHighestScore:
            matched_rules.clear()
            currentHighestScore = matched_amount
            matched_rules.append(rule)


    matched_rules_amount = len(matched_rules)

    if matched_rules_amount == 0:
        return
    elif matched_rules_amount == 1:
        matched_rules[0].execute(*payload_args)
    else:
        matched_rules.sort(key= lambda rule: rule.priority, reverse=True)
        matched_rules[0].execute(*payload_args)
        # Here we need to sort the rules with the same amount of criteria by priority and pick the one with the highest if multiple rules have the same pick by random.