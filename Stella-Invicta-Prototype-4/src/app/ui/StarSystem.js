import { Graphics, FederatedPointerEvent, Point, Container } from "pixi.js";
import { randomColor } from "../../engine/utils/random";

export class StarSystem extends Graphics {
    entity;
    contextMenu;
    stage; // Reference to the main stage/container for global events

    // --- Dragging Properties ---
    isDragging = false;
    dragOffset = new Point();

    constructor(entity, contextMenu, stage) {
        super();
        this.entity = entity;
        this.contextMenu = contextMenu;
        this.stage = stage; // Store the reference to the main container

        // Draw the object itself
        this.circle(0, 0, 30);
        this.fill(randomColor());
        this.stroke({ width: 2, color: 0xffffff, alpha: 0.8 });

        // Make it interactive
        this.eventMode = "static";
        this.cursor = "pointer";

        // Add event listeners
        this.on("rightclick", this.onRightClick);
        this.on("pointerdown", this.onDragStart);
        this.on("pointerup", this.onDragEnd);
        this.on("pointerupoutside", this.onDragEnd);
        this.on("pointermove", this.onDragMove);
    }

    onDragStart = (event) => {
        if (event.button !== 0) return;

        this.isDragging = true;
        this.dragOffset = this.toLocal(event.global);
        this.alpha = 0.7;

        if (this.parent) {
            this.parent.sortableChildren = true;
            this.zIndex = 100;
        }

        // --- THE FIX ---
        // Add the move and up listeners to the main stage, not this object.
        // This ensures they fire even if the cursor leaves this object.
        this.stage.on("pointermove", this.onDragMove);
        this.stage.on("pointerup", this.onDragEnd);
        this.stage.on("pointerupoutside", this.onDragEnd);
        // --- End of Fix ---
    };

    onDragEnd = (event) => {
        // This handler might be called for a different object if not dragging,
        // so we check if this specific instance is being dragged.
        if (!this.isDragging || event.button !== 0) return;

        this.isDragging = false;
        this.alpha = 1;
        this.zIndex = 0;

        if (this.parent) {
            this.parent.sortableChildren = true;
        }

        // --- THE FIX ---
        // CRITICAL: Remove the global listeners to prevent memory leaks
        // and unintended behavior on other objects.
        this.stage.off("pointermove", this.onDragMove);
        this.stage.off("pointerup", this.onDragEnd);
        this.stage.off("pointerupoutside", this.onDragEnd);
        // --- End of Fix ---
    };

    onDragMove = (event) => {
        if (this.isDragging && this.parent) {
            const newPosition = this.parent.toLocal(event.global);
            this.x = newPosition.x - this.dragOffset.x;
            this.y = newPosition.y - this.dragOffset.y;

            if (this.entity.setPosition) {
                this.entity.setPosition({ x: this.x, y: this.y });
            }
        }
    };

    // --- onRightClick method remains the same ---
    onRightClick = (event) => {
        event.stopPropagation();

        const menuOptions = [];

        menuOptions.push({
            label: "Rename",
            action: "rename",
            icon: "✎",
            callback: () => {
                console.log("rename function to be implemented");
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

        menuOptions.push({ action: "separator", label: "" });

        menuOptions.push({
            label: "Delete",
            action: "delete",
            icon: "🗑️",
            callback: () => this.destroy(),
        });

        this.contextMenu.show(event.global.x, event.global.y, menuOptions);
    };
}
