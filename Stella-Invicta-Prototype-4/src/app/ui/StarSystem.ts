import { Graphics, FederatedPointerEvent, Point } from 'pixi.js';
import { randomColor } from '../../engine/utils/random';
import type { ContextMenu, ContextMenuOption } from './ContextMenu';

export class StarSystem extends Graphics {
    private contextMenu: ContextMenu;

    // --- Dragging Properties ---
    private isDragging = false;
    private dragOffset = new Point();

    constructor(contextMenu: ContextMenu) {
        super();
        this.contextMenu = contextMenu;

        // Draw the object itself
        this.circle(0, 0, 30);
        this.fill(randomColor());
        this.stroke({ width: 2, color: 0xffffff, alpha: 0.8 });

        // Make it interactive
        this.eventMode = 'static';
        this.cursor = 'pointer';

        // Add event listeners
        this.on('rightclick', this.onRightClick);
        this.on('pointerdown', this.onDragStart);
        this.on('pointerup', this.onDragEnd);
        this.on('pointerupoutside', this.onDragEnd);
        this.on('pointermove', this.onDragMove);
    }

    // --- Drag and Drop Handlers ---

    private onDragStart = (event: FederatedPointerEvent) => {
        // We only want to drag with the left mouse button
        if (event.button !== 0) return;

        this.isDragging = true;

        // Store the offset between the object's origin and the click point.
        // This prevents the object from "jumping" to the cursor's position.
        this.dragOffset = this.toLocal(event.global);

        // Give some visual feedback that the object is being dragged
        this.alpha = 0.7;

        // --- THE FIX ---
        // To bring the object to the front, we give it a high zIndex...
        this.zIndex = -10;

        // ...and then we must tell the parent container to sort its children.
        // We also need to ensure the parent is sortable.
        if (this.parent) {
            this.parent.sortableChildren = true;
        }
    }

    private onDragEnd = (event: FederatedPointerEvent) => {
        // We only care about the left mouse button being released
        if (event.button !== 0) return;

        this.isDragging = false;

        // Reset visual feedback
        this.alpha = 1;
    }

    private onDragMove = (event: FederatedPointerEvent) => {
        if (this.isDragging) {
            // Get the new position of the mouse in the parent's coordinate system
            const newPosition = this.parent.toLocal(event.global);

            // Update the object's position, taking the initial offset into account
            this.x = newPosition.x - this.dragOffset.x;
            this.y = newPosition.y - this.dragOffset.y;
        }
    }

    // --- Context Menu Handler (no changes needed here) ---

    private onRightClick = (event: FederatedPointerEvent) => {
        event.stopPropagation();

        const menuOptions: ContextMenuOption[] = [
            {
                label: 'Change Color',
                action: 'change-color',
                callback: () => {
                    this.tint = randomColor();
                },
                icon: '🎨'
            },
            {
                label: 'Get Info',
                action: 'get-info',
                callback: () => {
                    alert(`Star System at X: ${this.x.toFixed(0)}, Y: ${this.y.toFixed(0)}`);
                },
                icon: 'ℹ️'
            },
            {
                action: 'separator', label: ''
            },
            {
                label: 'Delete',
                action: 'delete',
                callback: () => {
                    this.destroy();
                },
                icon: '🗑️'
            }
        ];

        this.contextMenu.show(event.global.x, event.global.y, menuOptions);
    }
}