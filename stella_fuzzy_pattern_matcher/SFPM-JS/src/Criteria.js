// src/Criteria.js

/**
 * @enum {Symbol}
 */
export const Operator = Object.freeze({
    Equal: Symbol("Equal"),
    GreaterThan: Symbol("GreaterThan"),
    LessThan: Symbol("LessThan"),
    GreaterThanOrEqual: Symbol("GreaterThanOrEqual"),
    LessThanOrEqual: Symbol("LessThanOrEqual"),
    NotEqual: Symbol("NotEqual"),
    Predicate: Symbol("Predicate"),
});

export class Criteria {
    /**
     * @param {string} factName
     * @param {any} expectedValue
     * @param {Operator} operator
     */
    constructor(factName, expectedValue, operator) {
        this.factName = factName;
        this.expectedValue = expectedValue;
        this.operator = operator;
        this.predicate = null;

        if (operator === Operator.Predicate) {
            if (typeof expectedValue !== "function") {
                throw new Error(
                    "For the Predicate operator, expectedValue must be a function."
                );
            }
            this.predicate = expectedValue;
        }
    }

    /**
     * @param {import('./FactSource').FactSource} facts
     * @returns {boolean}
     */
    evaluate(facts) {
        const actualValue = facts.getFact(this.factName);

        if (actualValue === undefined) {
            return false;
        }

        if (this.operator === Operator.Predicate) {
            return this.predicate(actualValue);
        }

        switch (this.operator) {
            case Operator.Equal:
                return actualValue === this.expectedValue;
            case Operator.NotEqual:
                return actualValue !== this.expectedValue;
            case Operator.GreaterThan:
                return actualValue > this.expectedValue;
            case Operator.LessThan:
                return actualValue < this.expectedValue;
            case Operator.GreaterThanOrEqual:
                return actualValue >= this.expectedValue;
            case Operator.LessThanOrEqual:
                return actualValue <= this.expectedValue;
            default:
                return false;
        }
    }
}
