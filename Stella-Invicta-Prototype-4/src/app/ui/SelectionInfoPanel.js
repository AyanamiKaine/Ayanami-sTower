import { Container, Graphics, Text } from "pixi.js";

export class SelectionInfoPanel extends Container {
    constructor() {
        super();

        this.background = new Graphics();
        this.addChild(this.background);

        this.text = new Text({
            text: "",
            style: {
                fontFamily: "Arial",
                fontSize: 14,
                fill: 0xffffff,
                align: "left",
            },
        });
        this.text.x = 10;
        this.text.y = 5;
        this.addChild(this.text);

        this.visible = false;
    }

    updateSelection(selectedItems) {
        const count = selectedItems.size;

        if (count === 0) {
            this.visible = false;
            return;
        }

        let infoText = `Selected: ${count} star system${count > 1 ? "s" : ""}`;

        if (count === 1) {
            const item = Array.from(selectedItems)[0];
            if (item.entity && item.entity.name) {
                infoText += `\nName: ${item.entity.name}`;
            }
        }

        infoText += "\nHotkeys: Del=Delete, Ctrl+A=Select All, Esc=Clear";

        this.text.text = infoText;

        // Update background
        const padding = 10;
        const textBounds = this.text.getBounds();

        this.background.clear();
        this.background
            .roundRect(
                0,
                0,
                textBounds.width + padding * 2,
                textBounds.height + padding,
                5,
            )
            .fill({ color: 0x000000, alpha: 0.8 })
            .stroke({ color: 0x4169e1, width: 2, alpha: 0.8 });

        this.visible = true;
    }

    hide() {
        this.visible = false;
    }
}
