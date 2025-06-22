// src/FactSource.js

export class FactSource {
    getFact(factName) {
        throw new Error("getFact() must be implemented by a subclass.");
    }

    setFact(factName, value) {
        throw new Error("setFact() must be implemented by a subclass.");
    }
}

export class DictionaryFactSource extends FactSource {
    /**
     * @param {Map<string, any>} data
     */
    constructor(data) {
        super();
        this._data = data;
    }

    getFact(factName) {
        return this._data.get(factName);
    }

    setFact(factName, value) {
        this._data.set(factName, value);
    }
}
