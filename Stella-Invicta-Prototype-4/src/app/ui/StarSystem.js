import { Graphics, Text, Point, Container } from "pixi.js";
import { randomColor } from "../../engine/utils/random";
import { engine } from "../getEngine";
import { getResolution } from "../../engine/utils/getResolution";

// Best practice: Define a consistent style for your labels
const labelStyle = {
    fontFamily: "Arial",
    fontSize: 14,
    fill: 0xffffff,
    align: "center",
    stroke: { color: 0x000000, width: 2, join: "round" },
    resolution: getResolution() * 2,
};

export class StarSystem extends Container {
    // <-- Change #1: Inherit from Container
    entity;
    contextMenu;
    stage;
    mainScreen;
    isSelected = false;
    // --- Child Components ---
    circle; // <-- We'll store a reference to the circle graphic
    label; // <-- And a reference to the text label

    // --- Dragging Properties ---
    isDragging = false;
    dragOffset = new Point();

    connectionLines = new Set();

    constructor(entity, contextMenu, stage, mainScreen) {
        super(); // <-- Important: Call the Container's constructor

        this.entity = entity;
        this.contextMenu = contextMenu;
        this.stage = stage;
        this.mainScreen = mainScreen;
        // --- Change #2: Create child objects ---
        // Create the circle graphic
        this.circle = new Graphics();
        this.circle.circle(0, 0, 15);
        this.circle.fill(randomColor());
        this.circle.stroke({ width: 2, color: 0xffffff, alpha: 0.8 });

        // Create the text label
        this.label = new Text({
            text: this.entity.name || "",
            style: labelStyle,
        });
        this.label.anchor.set(0.5); // Center the text's origin
        this.label.y = -30; // Position it above the circle

        // Add the children to this container
        this.addChild(this.circle);
        this.addChild(this.label);
        // --- End of Change #2 ---

        // Make the whole container interactive
        this.eventMode = "static";
        this.cursor = "pointer";

        // The hit area now needs to be set on the circle itself,
        // so only clicking the graphic part triggers interaction.
        this.circle.eventMode = "static";
        this.circle.cursor = "pointer";

        // Add event listeners TO THE CIRCLE
        this.circle.on("pointerdown", this.onDragStart);
        this.on("rightclick", this.onRightClick); // Right-click can be on the container
    }

    addConnectionLine(connectionLine) {
        this.connectionLines.add(connectionLine);
    }

    // Add method to remove a connection line
    removeConnectionLine(connectionLine) {
        this.connectionLines.delete(connectionLine);
    }

    // This is the new method to handle renaming
    rename(newName) {
        if (!newName) return;
        this.entity.name = newName;
        this.label.text = newName;
    }

    onDragStart = (event) => {
        if (event.button !== 0) return;

        // Check if this star system is part of a selection
        const mainScreen = this.mainScreen;
        if (mainScreen && mainScreen.selectionManager.selectedItems.has(this)) {
            // Let the main screen handle group dragging
            return;
        }

        // Handle individual dragging (existing code)
        this.isDragging = true;
        this.dragOffset = this.toLocal(event.global);

        if (this.parent) {
            this.parent.sortableChildren = true;
            this.zIndex = 100;
        }

        this.stage.on("pointermove", this.onDragMove);
        this.stage.on("pointerup", this.onDragEnd);
        this.stage.on("pointerupoutside", this.onDragEnd);
    };

    onDragEnd = (event) => {
        if (!this.isDragging || event.button !== 0) return;

        this.isDragging = false;
        this.alpha = 1;
        this.zIndex = 0;

        if (this.parent) {
            this.parent.sortableChildren = true;
        }

        this.stage.off("pointermove", this.onDragMove);
        this.stage.off("pointerup", this.onDragEnd);
        this.stage.off("pointerupoutside", this.onDragEnd);
    };

    onDragMove = (event) => {
        if (this.isDragging && this.parent) {
            const newPosition = this.parent.toLocal(event.global);
            this.x = newPosition.x - this.dragOffset.x;
            this.y = newPosition.y - this.dragOffset.y;

            if (this.entity.setPosition) {
                this.entity.setPosition({ x: this.x, y: this.y });
            }

            this.updateConnectionLines();
        }
    };

    updateConnectionLines() {
        for (const connectionLine of this.connectionLines) {
            if (connectionLine && connectionLine.redraw) {
                connectionLine.redraw();
            }
        }
    }

    onRightClick = (event) => {
        event.stopPropagation();
        const menuOptions = [];

        // Check if this star system is selected
        const isSelected =
            this.mainScreen.selectionManager.selectedItems.has(this);
        const selectedCount =
            this.mainScreen.selectionManager.getSelectedCount();

        if (isSelected && selectedCount > 1) {
            // Show group operations
            menuOptions.push({
                label: `Move Selected (${selectedCount})`,
                action: "move-group",
                icon: "↔️",
                callback: () => {
                    // This could open a dialog for precise positioning
                    alert(`Moving ${selectedCount} selected star systems`);
                },
            });

            menuOptions.push({
                label: `Delete Selected (${selectedCount})`,
                action: "delete-group",
                icon: "🗑️",
                callback: () => {
                    if (
                        confirm(
                            `Delete ${selectedCount} selected star systems?`,
                        )
                    ) {
                        this.mainScreen.selectionManager.deleteSelectedItems();
                    }
                },
            });

            menuOptions.push({ action: "separator", label: "" });
        }

        // Individual operations
        menuOptions.push({
            label: "Rename",
            action: "rename",
            icon: "✎",
            callback: () => {
                const newName = prompt(
                    "Enter a new name for the star system:",
                    this.entity.name,
                );
                this.rename(newName);
            },
        });

        menuOptions.push({
            label: "Connect To...",
            action: "connect",
            icon: "↔️",
            callback: () => {
                this.mainScreen.startConnectionMode(this);
            },
        });

        menuOptions.push({
            label: "Get Info",
            action: "info",
            icon: "ℹ️",
            callback: () =>
                alert(
                    `Entity ID: ${this.entity.id}, Name: ${this.entity.name}`,
                ),
        });

        if (!isSelected || selectedCount === 1) {
            menuOptions.push({ action: "separator", label: "" });

            menuOptions.push({
                label: "Delete",
                action: "delete",
                icon: "🗑️",
                callback: () => {
                    if (confirm("Delete this star system?")) {
                        this.destroy();
                    }
                },
            });
        }

        // Selection operations
        menuOptions.push({ action: "separator", label: "" });

        if (isSelected) {
            menuOptions.push({
                label: "Deselect",
                action: "deselect",
                icon: "◯",
                callback: () => {
                    this.mainScreen.selectionManager.removeFromSelection(this);
                },
            });
        } else {
            menuOptions.push({
                label: "Select",
                action: "select",
                icon: "◉",
                callback: () => {
                    this.mainScreen.selectionManager.addToSelection(this);
                },
            });
        }

        this.contextMenu.show(event.global.x, event.global.y, menuOptions);
    };

    destroy() {
        // Remove all connection lines before destroying
        for (const connectionLine of this.connectionLines) {
            if (connectionLine && !connectionLine.destroyed) {
                // Remove this star system's reference from the connection line
                if (connectionLine.starSystemA === this) {
                    connectionLine.starSystemA = null;
                } else if (connectionLine.starSystemB === this) {
                    connectionLine.starSystemB = null;
                }

                // If both references are null, destroy the connection line
                if (
                    !connectionLine.starSystemA ||
                    !connectionLine.starSystemB
                ) {
                    connectionLine.destroy();
                }
            }
        }

        this.connectionLines.clear();
        super.destroy();
    }

    setSelected(selected) {
        this.isSelected = selected;
    }
}
