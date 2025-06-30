import { Container, Graphics } from "pixi.js";
import { ContextMenu } from "./ContextMenu";

export class StarSystemConnectionLine extends Container {
    line;
    starSystemA;
    starSystemB;
    contextMenu;

    constructor(starSystemA, starSystemB, contextMenu) {
        super();
        this.zIndex = -1;
        this.eventMode = "static";
        // Store references to the actual star system objects
        this.starSystemA = starSystemA;
        this.starSystemB = starSystemB;

        this.line = new Graphics();
        this.addChild(this.line);

        // Initial draw
        this.redraw();
        this.on("rightclick", this.onRightClick);

        this.contextMenu = contextMenu;
    }

    redraw() {
        // Clear the previous line
        this.line.clear();

        // Get current positions from the star system objects
        const posA = [this.starSystemA.x, this.starSystemA.y];
        const posB = [this.starSystemB.x, this.starSystemB.y];

        // Draw the new line
        this.line
            .moveTo(...posA)
            .lineTo(...posB)
            .stroke({ width: 4, color: "red" });
    }

    onRightClick = (event) => {
        event.stopPropagation();
        this.contextMenu.show(event.global.x, event.global.y, menuOptions);
    };

    // Method to update the line when star systems move
    updateLine() {
        this.redraw();
    }

    // Clean up method to remove references
    destroy() {
        this.starSystemA = null;
        this.starSystemB = null;
        super.destroy();
    }
}
