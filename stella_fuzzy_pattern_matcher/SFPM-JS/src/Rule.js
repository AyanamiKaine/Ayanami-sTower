// src/Rule.js

export class Rule {
    /**
     * @param {import('./Criteria').Criteria[]} criterias
     * @param {(...data) => void} payload
     * @param {string} [name='']
     * @param {number} [priority=0]
     */
    constructor(criterias, payload, name = "", priority = 0) {
        this.criterias = criterias;
        this.payload = payload;
        this.name = name;
        this.priority = priority;
    }

    get criteriaCount() {
        return this.criterias.length;
    }

    /**
     * @param {import('./FactSource').FactSource} facts
     * @returns {{isTrue: boolean, matchedCriteriaCount: number}}
     */
    evaluate(facts) {
        if (this.criteriaCount === 0) {
            return { isTrue: true, matchedCriteriaCount: 0 };
        }

        for (const criteria of this.criterias) {
            if (!criteria.evaluate(facts)) {
                return { isTrue: false, matchedCriteriaCount: 0 };
            }
        }

        return { isTrue: true, matchedCriteriaCount: this.criteriaCount };
    }

    executePayload(...data) {
        this.payload(...data);
    }
}
