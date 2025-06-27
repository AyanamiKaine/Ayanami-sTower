import { Container, Graphics } from "pixi.js";

export class StarSystemConnectionLine extends Container {
    line;
    // We want to draw a line between the first star system
    // to the second one
    constructor(starSystemPositionA, starSystemPositionB) {
        super();
        this.zIndex = -1;

        this.line = new Graphics();

        this.line
            .moveTo(...starSystemPositionA) // Start point
            .lineTo(...starSystemPositionB) // end point
            .stroke({ width: 4, color: "red" });

        this.addChild(this.line);
    }
}
