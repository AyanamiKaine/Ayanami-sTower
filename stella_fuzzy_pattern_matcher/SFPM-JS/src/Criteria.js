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

const operatorToString = (opSymbol) => Object.keys(Operator).find(key => Operator[key] === opSymbol);
const stringToOperator = (opString) => Operator[opString];


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

        //console.log(
        //    `[DEBUG] Evaluating Criteria: FactName='${this.factName}', Expected='${this.expectedValue}', //Actual='${actualValue}'`
        //);

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

    /**
       * Serializes the Criteria instance to a JSON-compatible object.
       * Throws an error if the operator is 'Predicate' as functions cannot be serialized.
       * @returns {{factName: string, expectedValue: any, operator: string}}
       */
    toJSON() {
        if (this.operator === Operator.Predicate) {
            throw new Error("Criteria with a Predicate operator cannot be serialized to JSON.");
        }

        return {
            factName: this.factName,
            expectedValue: this.expectedValue,
            operator: operatorToString(this.operator)
        };
    }

    /**
     * Deserializes a JSON object or string into a Criteria instance.
     * Throws an error if the operator is 'Predicate'.
     * @param {string|object} json - The JSON string or object to deserialize.
     * @returns {Criteria} A new instance of the Criteria class.
     */
    static fromJSON(json) {
        const data = typeof json === 'string' ? JSON.parse(json) : json;

        const operator = stringToOperator(data.operator);
        if (!operator) {
            throw new Error(`Unknown operator found during deserialization: '${data.operator}'`);
        }

        if (operator === Operator.Predicate) {
            throw new Error("Cannot deserialize a Criteria with a Predicate operator.");
        }

        return new Criteria(data.factName, data.expectedValue, operator);
    }
}
