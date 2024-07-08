export class Tags {
    constructor() {
        this.tags = new Map();
    }
    addTag(tagName) {
        const tagSymbol = Symbol(tagName);
        this.tags.set(tagName, tagSymbol);
        return tagSymbol;
    }
    getTag(tagName) {
        return this.tags.get(tagName); // Return the symbol or undefined if not found
    }
}
