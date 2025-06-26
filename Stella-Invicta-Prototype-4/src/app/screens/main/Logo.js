import { Sprite, Texture } from "pixi.js";

import {
    randomBool,
    randomFloat,
    randomInt,
} from "../../../engine/utils/random";

/**
 * Enum for representing cardinal directions.
 * @readonly
 * @enum {number}
 */
export const DIRECTION = {
    NE: 0,
    NW: 1,
    SE: 2,
    SW: 3,
};

/**
 * Represents a bouncing logo sprite with its own movement properties.
 */
export class Logo extends Sprite {
    /**
     * The current movement direction of the logo.
     * @type {DIRECTION}
     */
    direction;

    /**
     * The movement speed of the logo.
     * @type {number}
     */
    speed;

    /** The x-coordinate of the left edge of the sprite. */
    get left() {
        return -this.width * 0.5;
    }

    /** The x-coordinate of the right edge of the sprite. */
    get right() {
        return this.width * 0.5;
    }

    /** The y-coordinate of the top edge of the sprite. */
    get top() {
        return -this.height * 0.5;
    }

    /** The y-coordinate of the bottom edge of the sprite. */
    get bottom() {
        return this.height * 0.5;
    }

    constructor() {
        const tex = randomBool() ? "logo.svg" : "logo-white.svg";
        super({ texture: Texture.from(tex), anchor: 0.5, scale: 0.25 });

        /**
         * The initial direction of the logo, chosen randomly.
         */
        this.direction = randomInt(0, 3);

        /**
         * The initial speed of the logo, chosen randomly.
         */
        this.speed = randomFloat(1, 6);
    }
}
