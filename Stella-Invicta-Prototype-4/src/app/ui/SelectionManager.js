import { Container, Graphics, Rectangle } from "pixi.js";

export class SelectionManager extends Container {
    constructor(mainContainer) {
        super();

        this.mainContainer = mainContainer;
        this.selectedItems = new Set();
        this.isSelecting = false;
        this.selectionStart = { x: 0, y: 0 };
        this.selectionEnd = { x: 0, y: 0 };

        // Visual selection rectangle
        this.selectionRect = new Graphics();
        this.selectionRect.zIndex = 1000; // Make sure it's on top
        this.addChild(this.selectionRect);

        // Selection rectangle style
        this.selectionStyle = {
            fillColor: 0x4169e1,
            fillAlpha: 0.2,
            strokeColor: 0x4169e1,
            strokeWidth: 2,
            strokeAlpha: 0.8,
        };
    }

    startSelection(worldPosition) {
        this.isSelecting = true;
        this.selectionStart = { x: worldPosition.x, y: worldPosition.y };
        this.selectionEnd = { x: worldPosition.x, y: worldPosition.y };

        // Clear previous selection if not holding Ctrl
        if (!this.isCtrlPressed) {
            this.clearSelection();
        }

        this.updateSelectionRect();
    }

    updateSelection(worldPosition) {
        if (!this.isSelecting) return;

        this.selectionEnd = { x: worldPosition.x, y: worldPosition.y };
        this.updateSelectionRect();

        // Find items within selection rectangle
        this.selectItemsInRect();
    }

    endSelection() {
        this.isSelecting = false;
        this.selectionRect.clear();
    }

    updateSelectionRect() {
        const x = Math.min(this.selectionStart.x, this.selectionEnd.x);
        const y = Math.min(this.selectionStart.y, this.selectionEnd.y);
        const width = Math.abs(this.selectionEnd.x - this.selectionStart.x);
        const height = Math.abs(this.selectionEnd.y - this.selectionStart.y);

        this.selectionRect.clear();

        if (width > 0 && height > 0) {
            this.selectionRect
                .rect(x, y, width, height)
                .fill({
                    color: this.selectionStyle.fillColor,
                    alpha: this.selectionStyle.fillAlpha,
                })
                .stroke({
                    color: this.selectionStyle.strokeColor,
                    width: this.selectionStyle.strokeWidth,
                    alpha: this.selectionStyle.strokeAlpha,
                });
        }
    }
    selectItemsInRect() {
        const selectionBounds = this.getSelectionBounds();

        // Find all StarSystem instances in mainContainer
        const starSystems = this.mainContainer.children.filter(
            (child) => child.constructor.name === "StarSystem",
        );

        for (const starSystem of starSystems) {
            // Use the star system's position and a reasonable radius instead of getBounds()
            // since getBounds() can return global coordinates which might not match our world coordinates
            const starX = starSystem.x;
            const starY = starSystem.y;
            const starRadius = 20; // Approximate radius of star system (15 + some padding)

            // Create a simple bounds rectangle for the star system
            const starBounds = {
                x: starX - starRadius,
                y: starY - starRadius,
                width: starRadius * 2,
                height: starRadius * 2,
            };

            if (this.boundsIntersect(selectionBounds, starBounds)) {
                this.addToSelection(starSystem);
            } else if (!this.isCtrlPressed) {
                // Remove from selection if not in rectangle and not holding Ctrl
                this.removeFromSelection(starSystem);
            }
        }
    }

    getSelectionBounds() {
        const x = Math.min(this.selectionStart.x, this.selectionEnd.x);
        const y = Math.min(this.selectionStart.y, this.selectionEnd.y);
        const width = Math.abs(this.selectionEnd.x - this.selectionStart.x);
        const height = Math.abs(this.selectionEnd.y - this.selectionStart.y);

        return new Rectangle(x, y, width, height);
    }

    boundsIntersect(rect1, rect2) {
        return !(
            rect1.x + rect1.width < rect2.x ||
            rect2.x + rect2.width < rect1.x ||
            rect1.y + rect1.height < rect2.y ||
            rect2.y + rect2.height < rect1.y
        );
    }

    addToSelection(item) {
        if (!this.selectedItems.has(item)) {
            this.selectedItems.add(item);
            this.highlightItem(item, true);
        }
    }

    removeFromSelection(item) {
        if (this.selectedItems.has(item)) {
            this.selectedItems.delete(item);
            this.highlightItem(item, false);
        }
    }

    toggleSelection(item) {
        if (this.selectedItems.has(item)) {
            this.removeFromSelection(item);
        } else {
            this.addToSelection(item);
        }
    }

    clearSelection() {
        for (const item of this.selectedItems) {
            this.highlightItem(item, false);
        }
        this.selectedItems.clear();
    }

    highlightItem(item, isSelected) {
        if (item && item.circle) {
            if (isSelected) {
                // Add selection highlight
                item.circle.tint = 0xffff00; // Yellow tint
                item.alpha = 0.9;
            } else {
                // Remove selection highlight
                item.circle.tint = 0xffffff; // White (no tint)
                item.alpha = 1.0;
            }
        }
    }

    // Group operations
    moveSelectedItems(deltaX, deltaY) {
        for (const item of this.selectedItems) {
            item.x += deltaX;
            item.y += deltaY;

            // Update entity position if it exists
            if (item.entity && item.entity.setPosition) {
                item.entity.setPosition({ x: item.x, y: item.y });
            }

            // Update connection lines
            if (item.updateConnectionLines) {
                item.updateConnectionLines();
            }
        }
    }

    deleteSelectedItems() {
        const itemsToDelete = Array.from(this.selectedItems);

        for (const item of itemsToDelete) {
            // Remove from selection first
            this.removeFromSelection(item);

            // Remove from parent container
            if (item.parent) {
                item.parent.removeChild(item);
            }

            // Destroy the item
            item.destroy();
        }
    }

    getSelectedCount() {
        return this.selectedItems.size;
    }

    hasSelection() {
        return this.selectedItems.size > 0;
    }

    getSelectedItems() {
        return Array.from(this.selectedItems);
    }

    // Utility method to set Ctrl key state
    setCtrlPressed(pressed) {
        this.isCtrlPressed = pressed;
    }
}
