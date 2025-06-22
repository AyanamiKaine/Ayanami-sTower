// src/Query.js

import { DictionaryFactSource } from "./FactSource.js";
import { match } from "./RuleMatcher.js";

export class Query {
    /**
     * @param {Map<string, any>} factData
     */
    constructor(factData = new Map()) {
        // Default to an empty map
        this._factSource = new DictionaryFactSource(factData);
    }

    /**
     * @param {string} key
     * @param {any} value
     * @returns {this}
     */
    add(key, value) {
        this._factSource.setFact(key, value);
        return this; // Return this for chaining, like in the C# example
    }

    /**
     * @param {import('./Rule').Rule[]} rules
     */
    match(rules) {
        match(rules, this._factSource);
    }
}
