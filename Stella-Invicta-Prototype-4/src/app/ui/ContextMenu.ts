import { Container, Graphics, Text } from 'pixi.js';
import { animate } from 'motion';
import { engine } from '../getEngine';

// Define the structure for a menu item
export interface ContextMenuOption {
    label: string;
    action: string | 'separator';
    callback?: () => void;
    icon?: string; // Optional icon
}

export class ContextMenu extends Container {
    private background: Graphics;
    private shadow: Graphics;
    private innerBorder: Graphics;
    private menuWidth = 200;
    private itemHeight = 32;
    private padding = 8;

    constructor() {
        super();

        // Create the graphical elements once
        this.shadow = new Graphics();
        this.background = new Graphics();
        this.innerBorder = new Graphics();

        this.addChild(this.shadow);
        this.addChild(this.background);
        this.addChild(this.innerBorder);

        this.eventMode = 'static';
        this.visible = false; // Initially hidden
    }

    /**
     * Displays and animates the context menu at a specific position.
     * @param x - The horizontal position (top-left).
     * @param y - The vertical position (top-left).
     * @param options - An array of menu item definitions.
     */
    public async show(x: number, y: number, options: ContextMenuOption[]) {
        if (this.visible) {
            // If it's already visible, hide it instantly before showing the new one
            this.alpha = 0;
            this.visible = false;
        }

        // Clear any previous items
        this.removeChildren();
        this.addChild(this.shadow, this.background, this.innerBorder);

        this.buildMenu(options);
        this.drawGraphics();
        this.adjustPosition(x, y);

        // Make it visible and start the entrance animation
        this.visible = true;
        this.alpha = 0;
        this.scale.set(0.8);

        await animate(
            this,
            { alpha: 1, scale: 1 },
            { duration: 0.15, easing: 'ease-out' }
        );
    }

    /**
     * Hides the context menu with a fade-out animation.
     */
    public async hide() {
        if (!this.visible) return;

        await animate(
            this,
            { alpha: 0, scale: 0.8 },
            { duration: 0.1, easing: 'ease-in' }
        );
        this.visible = false;
    }

    /**
     * Constructs the individual menu items from the options array.
     */
    private buildMenu(options: ContextMenuOption[]) {
        let currentY = this.padding;

        options.forEach((option) => {
            if (option.action === 'separator') {
                const separator = new Graphics();
                separator.moveTo(this.padding * 2, 0);
                separator.lineTo(this.menuWidth - this.padding * 2, 0);
                separator.stroke({ width: 1, color: 0xe0e0e0 });
                separator.y = currentY + this.itemHeight / 2;
                this.addChild(separator);
                currentY += this.itemHeight;
                return;
            }

            const itemContainer = this.createMenuItem(option);
            itemContainer.y = currentY;
            this.addChild(itemContainer);

            currentY += this.itemHeight;
        });

        // Update the total height of the menu
        this.height = currentY + this.padding;
    }

    /**
     * Creates a single interactive menu item.
     */
    private createMenuItem(option: ContextMenuOption): Container {
        const container = new Container();

        // Background for hover and click effects
        const itemBg = new Graphics();
        container.addChild(itemBg);

        // Menu item text
        const labelText = option.icon ? `${option.icon} ${option.label}` : option.label;
        const text = new Text({
            text: labelText,
            style: {
                fontFamily: 'Arial',
                fontSize: 14,
                fill: 0x333333,
                align: 'left',
            }
        });
        text.anchor.set(0, 0.5);
        text.x = this.padding * 2;
        text.y = this.itemHeight / 2;
        container.addChild(text);

        // Make the container interactive
        container.eventMode = 'static';
        container.cursor = 'pointer';

        // Hover effects
        container.on('pointerenter', () => {
            itemBg.clear()
                .roundRect(this.padding / 2, 2, this.menuWidth - this.padding, this.itemHeight - 4, 4)
                .fill({ color: 0x4CAF50, alpha: 0.1 });
        });

        container.on('pointerleave', () => {
            itemBg.clear();
        });

        // Click handler
        container.on('pointerdown', (e) => {
            e.stopPropagation(); // Prevent the click from bubbling up to the main screen
            itemBg.clear()
                .roundRect(this.padding / 2, 2, this.menuWidth - this.padding, this.itemHeight - 4, 4)
                .fill({ color: 0x4CAF50, alpha: 0.2 });

            // Execute the action and hide the menu
            if (option.callback) {
                option.callback();
            }
            this.hide();
        });

        return container;
    }

    /**
     * Draws the main graphical elements (shadow, background, borders).
     */
    private drawGraphics() {
        const menuHeight = this.children.filter(c => c instanceof Container && c.children.length > 0).length * this.itemHeight + this.padding * 6;

        // Shadow
        this.shadow.clear()
            .roundRect(4, 4, this.menuWidth, menuHeight, 8)
            .fill({ color: 0x000000, alpha: 0.15 });

        // Main background
        this.background.clear()
            .roundRect(0, 0, this.menuWidth, menuHeight, 6)
            .fill(0xffffff)
            .stroke({ width: 1, color: 0xc0c0c0 });

        // Subtle inner border for a highlight effect
        this.innerBorder.clear()
            .roundRect(1, 1, this.menuWidth - 2, menuHeight - 2, 5)
            .stroke({ width: 1, color: 0xffffff, alpha: 0.8 });
    }

    /**
     * Adjusts the menu's final position to ensure it stays within the screen bounds.
     */
    private adjustPosition(x: number, y: number) {
        const screen = engine().screen;
        let finalX = x;
        let finalY = y;

        // If the menu goes off the right edge, flip it to the left of the cursor
        if (x + this.width > screen.width) {
            finalX = x - this.width;
        }

        // If the menu goes off the bottom edge, flip it to be above the cursor
        if (y + this.height > screen.height) {
            finalY = y - this.height;
        }

        // Ensure it doesn't go off the top-left of the screen either
        if (finalX < 0) finalX = 0;
        if (finalY < 0) finalY = 0;

        this.position.set(finalX, finalY);
    }
}