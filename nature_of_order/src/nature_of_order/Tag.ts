export class Tags {
	private tags: Map<string, symbol>; // Type the map to store string keys and symbol values

	constructor() {
		this.tags = new Map();
	}

	addTag(tagName: string): symbol {
		const tagSymbol = Symbol(tagName);
		this.tags.set(tagName, tagSymbol);
		return tagSymbol;
	}

	getTag(tagName: string): symbol | undefined {
		return this.tags.get(tagName); // Return the symbol or undefined if not found
	}
}
