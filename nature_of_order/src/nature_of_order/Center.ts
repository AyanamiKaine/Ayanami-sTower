import { Vector3D } from "./Vector3D.js";

interface Message {
	type: string; // or enum for more structured message types
	// ... other properties
}

interface CenterOptions {
	parentCenter?: Center; // Optional parent center
	position?: Vector3D; // Optional initial position
	name?: string;
}

export class Center {
	parentCenter: Center | undefined;
	childCenters: Center[];

	position: Vector3D;
	tags: symbol[];
	name: string;

	constructor(options: CenterOptions = {}) {
		this.parentCenter = options.parentCenter;
		this.childCenters = [];
		this.position = options.position ?? new Vector3D(); // Default to 0,0,0 if not provided
		this.tags = [];
		this.name = options.name ?? "";
	}
	/**
	 * Gets the global position of this center in 3D space.
	 *
	 * If this center has a parent, the global position is calculated by recursively adding
	 * its local position to the parent's global position.
	 * If this center is the root (no parent), its local position is considered the global position.
	 *
	 * @returns {Vector3D} The global position of the center.
	 */
	get globalPosition(): Vector3D {
		if (this.parentCenter) {
			// Recursive call to get parent's global position, then add this center's local position
			return this.parentCenter.globalPosition.add(this.position); // Note the change here
		} else {
			// If no parent, this center's position is its global position
			return this.position;
		}
	}
	/**
	 * Gets the number of child centers.
	 *
	 * @returns {number} The number of child centers.
	 */
	get size(): number {
		return this.childCenters.length;
	}
	/**
	 * Adds a new center as a child of this center.
	 *
	 * @param {Center} center - The center to add as a child.
	 */
	private addCenter(center: Center): void {
		center.parentCenter = this;
		this.childCenters.push(center);
	}
	/**
	 * creates a new Center that is part of the current center
	 * It automatically adds the center as a child and defines its parent
	 *
	 * @returns {Center} the created center
	 */
	createCenter(name: string = ""): Center {
		const center = new Center({ parentCenter: this, name: name }); // Set this as the parent
		this.childCenters.push(center);
		return center;
	}

	private addTag(symbol: symbol): void {
		this.tags.push(symbol);
	}

	receiveMessage(message: Message): void {
		// This is now an empty method that can be overridden
	}

	sendMessage(center: Center, message: Message): void {
		center.receiveMessage(message);
	}

	*[Symbol.iterator](): Iterator<Center> {
		// TypeScript-specific way to define an iterator
		for (const center of this.childCenters) {
			yield center;
		}
	}
}
