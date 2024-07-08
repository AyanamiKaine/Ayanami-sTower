import { Vector3D } from "./Vector3D.js";
export class Center {
	constructor(options = {}) {
		this.parentCenter = options.parentCenter;
		this.childCenters = [];
		/**
		 * The idea of a position might sound a bit esoteric.
		 * Think of a position in relation to another position of a center as a degree of relatedness and wholeness.
		 *
		 * The position of a continent center on a earth center is closer than the position of a mars center.
		 *
		 * Its not about actual distance, its about related distance. Things that are less related are farther away.
		 *
		 * Most confusion comes from the assumption that x its not related to y in any way.
		 * Like assuming there is no connection of a brain cell and the solar system. Nothing in this world is separated from it.
		 *
		 * With position we introduce a scale, as a center has many centers itself, but each has not the same distance to each other.
		 *
		 * The key is to see the world as a whole, not just one part. As a part always exists in the whole.
		 *
		 */
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
	get globalPosition() {
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
	get size() {
		return this.childCenters.length;
	}
	/**
	 * Adds a new center as a child of this center.
	 *
	 * @param {Center} center - The center to add as a child.
	 */
	addCenter(center) {
		center.parentCenter = this;
		this.childCenters.push(center);
	}
	/**
	 * creates a new Center that is part of the current center
	 * It automatically adds the center as a child and defines its parent
	 *
	 * @returns {Center} the created center
	 */
	createCenter(name = "") {
		const center = new Center({ parentCenter: this, name: name }); // Set this as the parent
		this.childCenters.push(center);
		return center;
	}
	addTag(symbol) {
		this.tags.push(symbol);
	}
	receiveMessage(message) {
		// This is now an empty method that can be overridden
	}
	sendMessage(center, message) {
		center.receiveMessage(message);
	}
	*[Symbol.iterator]() {
		// TypeScript-specific way to define an iterator
		for (const center of this.childCenters) {
			yield center;
		}
	}
}
