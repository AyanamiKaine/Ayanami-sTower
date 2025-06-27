import { FancyButton } from "@pixi/ui";
import { animate } from "motion";
import { Container, Graphics, Point, Rectangle } from "pixi.js";

import { engine } from "../../getEngine";
import { PausePopup } from "../../popups/PausePopup.";
import { SettingsPopup } from "../../popups/SettingsPopup";
import { ContextMenu } from "../../ui/ContextMenu";
import { Game } from "../../../game/game";
import { StarSystem } from "../../ui/StarSystem";
import { position3D } from "../../../game/mixins/Position3D";
import { StarSystemConnectionLine } from "../../ui/StarSystemConnectionLine";
import { SelectionManager } from "../../ui/SelectionManager";
import { getRandomSystemName } from "../../../game/data/Utility";

/**
 * The main screen of the application, responsible for displaying the primary game view,
 * handling user input for navigation like panning and zooming, and managing UI elements
 * like buttons and context menus.
 */
export class MainScreen extends Container {
    /** Assets bundles required by this screen */
    static assetBundles = ["main"];

    // --- Connection State ---
    isConnecting = false;
    connectionSourceStar = null;

    connectionLines = new Map();

    // Add selection-related properties
    selectionManager;
    isGroupDragging = false;
    groupDragStart = { x: 0, y: 0 };
    groupDragOffset = { x: 0, y: 0 };

    constructor() {
        super();
        /** @type {Game} */
        this.game = new Game();
        /** @type {Container} */
        this.mainContainer = new Container();
        this.addChild(this.mainContainer);

        this.mainContainer.sortableChildren = true;

        let grid = new Graphics()
            .moveTo(0, 0)
            .lineTo(0, 10000)
            .stroke({ color: "003f90", width: 2 });

        grid.moveTo(0, 0)
            .lineTo(0, -10000)
            .stroke({ color: "003f90", width: 2 });

        grid.moveTo(0, 0)
            .lineTo(10000, 0)
            .stroke({ color: "003f90", width: 2 });

        grid.moveTo(0, 0)
            .lineTo(-10000, 0)
            .stroke({ color: "003f90", width: 2 });

        grid.zIndex = -2;

        this.mainContainer.addChild(grid);

        this.mainContainer.hitArea = new Rectangle(
            -10000,
            -10000,
            20000,
            20000,
        );

        this.selectionManager = new SelectionManager(this.mainContainer);
        this.mainContainer.addChild(this.selectionManager);

        // Add keyboard event listeners for Ctrl key
        this.setupKeyboardListeners();

        this.mainContainer.eventMode = "static";

        this.mainContainer.on("wheel", this.onWheelScroll);
        this.mainContainer.on("pointerdown", this.onPointerDown);
        this.mainContainer.on("pointerup", this.onPointerUp);
        this.mainContainer.on("pointerupoutside", this.onPointerUp);
        this.mainContainer.on("pointermove", this.onPointerMove);

        // Right-click event to show the context menu
        this.mainContainer.on("rightclick", (event) => {
            event.preventDefault();
            const menuOptions = [
                {
                    label: "Add Star System",
                    action: "add-circle",
                    callback: () => {
                        const worldPosition = this.mainContainer.toLocal(
                            event.global,
                        );
                        const starEntity = this.game.createEntity(
                            getRandomSystemName(),
                        );
                        starEntity.with(position3D, {
                            x: worldPosition.x,
                            y: worldPosition.y,
                            z: 0,
                        });

                        // Pass `this` (the MainScreen instance) as the fourth argument
                        const starSystemView = new StarSystem(
                            starEntity,
                            this.contextMenu,
                            this.mainContainer,
                            this,
                        );

                        starSystemView.position.copyFrom(worldPosition);
                        this.mainContainer.addChild(starSystemView);
                    },
                    icon: "🌣",
                },
            ];

            this.contextMenu.show(event.global.x, event.global.y, menuOptions);
        });

        this.mainContainer.on("pointerdown", () => {
            this.contextMenu.hide();
        });
        /** @private */
        this.paused = false;
        /** @private */
        this.contextMenu;
        /** @private */
        this.ZOOM_FACTOR = 1.1;
        /** @private */
        this.MIN_ZOOM = 0.2;
        /** @private */
        this.MAX_ZOOM = 5.0;
        /** @private */
        this.isPanning = false;
        /** @private */
        this.lastPanPosition = new Point();

        this.contextMenu = new ContextMenu();
        this.addChild(this.contextMenu);
    }

    setupMainContextMenu() {
        this.mainContainer.on("rightclick", (event) => {
            event.preventDefault();
            const menuOptions = [];

            // Show selection-specific options if items are selected
            if (this.selectionManager.hasSelection()) {
                menuOptions.push({
                    label: `Delete Selected (${this.selectionManager.getSelectedCount()})`,
                    action: "delete-selected",
                    callback: () => {
                        this.selectionManager.deleteSelectedItems();
                    },
                    icon: "🗑️",
                });

                menuOptions.push({
                    label: "Clear Selection",
                    action: "clear-selection",
                    callback: () => {
                        this.selectionManager.clearSelection();
                    },
                    icon: "✖️",
                });

                menuOptions.push({ action: "separator", label: "" });
            }

            // Regular "Add Star System" option
            menuOptions.push({
                label: "Add Star System",
                action: "add-circle",
                callback: () => {
                    const worldPosition = this.mainContainer.toLocal(
                        event.global,
                    );
                    const starEntity = this.game.createEntity(
                        getRandomSystemName(),
                    );
                    starEntity.with(position3D, {
                        x: worldPosition.x,
                        y: worldPosition.y,
                        z: 0,
                    });

                    const starSystemView = new StarSystem(
                        starEntity,
                        this.contextMenu,
                        this.mainContainer,
                        this,
                    );

                    starSystemView.position.copyFrom(worldPosition);
                    this.mainContainer.addChild(starSystemView);
                },
                icon: "🌟",
            });

            // Show select all option
            menuOptions.push({
                label: "Select All",
                action: "select-all",
                callback: () => {
                    this.selectAllStarSystems();
                },
                icon: "🔘",
            });

            this.contextMenu.show(event.global.x, event.global.y, menuOptions);
        });
    }

    setupKeyboardListeners() {
        // Track Ctrl key state
        document.addEventListener("keydown", (event) => {
            if (event.ctrlKey || event.metaKey) {
                this.selectionManager.setCtrlPressed(true);
            }

            // Handle keyboard shortcuts
            if (event.key === "Delete" || event.key === "Backspace") {
                if (this.selectionManager.hasSelection()) {
                    this.selectionManager.deleteSelectedItems();
                    event.preventDefault();
                }
            }

            // Ctrl+A to select all
            if ((event.ctrlKey || event.metaKey) && event.key === "a") {
                this.selectAllStarSystems();
                event.preventDefault();
            }

            // Escape to clear selection
            if (event.key === "Escape") {
                this.selectionManager.clearSelection();
            }
        });

        document.addEventListener("keyup", (event) => {
            if (!event.ctrlKey && !event.metaKey) {
                this.selectionManager.setCtrlPressed(false);
            }
        });
    }

    selectAllStarSystems() {
        const starSystems = this.mainContainer.children.filter(
            (child) => child.constructor.name === "StarSystem",
        );

        this.selectionManager.clearSelection();
        for (const starSystem of starSystems) {
            this.selectionManager.addToSelection(starSystem);
        }
    }

    /**
     * @param {import('pixi.js').FederatedPointerEvent} event
     */
    onPointerDown = (event) => {
        // Handle panning with left mouse button + Ctrl key
        if (event.button === 0 && event.ctrlKey) {
            this.isPanning = true;
            this.lastPanPosition.copyFrom(event.global);
            this.mainContainer.cursor = "move";
            return; // Stop processing other click events
        }

        if (this.isConnecting && event.target instanceof Graphics) {
            // We need to find the StarSystem that owns the clicked graphics object.
            let targetStar = event.target;
            while (targetStar.parent && !(targetStar instanceof StarSystem)) {
                targetStar = targetStar.parent;
            }

            if (
                targetStar instanceof StarSystem &&
                targetStar !== this.connectionSourceStar
            ) {
                // Successfully found a target star, create the relationship in the data model
                this.game.addSymmetricRelationship(
                    this.connectionSourceStar.entity,
                    targetStar.entity,
                    { type: "connectedTo" },
                );

                // Create the connection line with star system references
                const connectionLine = new StarSystemConnectionLine(
                    this.connectionSourceStar,
                    targetStar,
                );

                this.mainContainer.addChild(connectionLine);

                // Store the connection line for updates
                const connectionKey = this.getConnectionKey(
                    this.connectionSourceStar,
                    targetStar,
                );
                this.connectionLines.set(connectionKey, connectionLine);

                // Register this connection line with both star systems
                this.connectionSourceStar.addConnectionLine(connectionLine);
                targetStar.addConnectionLine(connectionLine);
            }
            // Reset the connection state regardless of success
            this.isConnecting = false;
            this.connectionSourceStar = null;
            this.mainContainer.cursor = "default";
            return; // Exit after handling the connection click
        } else {
            this.isConnecting = false;
            this.connectionSourceStar = null;
            this.mainContainer.cursor = "default";
        }

        if (event.button === 0) {
            const worldPosition = this.mainContainer.toLocal(event.global);

            // Check if clicking on a star system
            const clickedStarSystem = this.getStarSystemAt(event.target);

            if (clickedStarSystem) {
                // Handle clicking on a star system
                if (event.ctrlKey || event.metaKey) {
                    // Toggle selection with Ctrl
                    this.selectionManager.toggleSelection(clickedStarSystem);
                } else if (
                    !this.selectionManager.selectedItems.has(clickedStarSystem)
                ) {
                    // Select single item if not already selected
                    this.selectionManager.clearSelection();
                    this.selectionManager.addToSelection(clickedStarSystem);
                }

                // Start group dragging if we have selected items
                if (this.selectionManager.hasSelection()) {
                    this.isGroupDragging = true;
                    this.groupDragStart = {
                        x: event.global.x,
                        y: event.global.y,
                    };
                    this.mainContainer.cursor = "move";
                }
            } else {
                // Start selection rectangle if not clicking on a star system
                if (!event.ctrlKey && !event.metaKey) {
                    this.selectionManager.clearSelection();
                }
                this.selectionManager.startSelection(worldPosition);
            }

            // Hide context menu
            this.contextMenu.hide();
            return;
        }

        if (
            event.button === 0 &&
            event.ctrlKey &&
            !this.selectionManager.isSelecting
        ) {
            this.isPanning = true;
            this.lastPanPosition.copyFrom(event.global);
            this.mainContainer.cursor = "move";
            return;
        }
    };

    getStarSystemAt(target) {
        let current = target;
        while (current) {
            if (current.constructor.name === "StarSystem") {
                return current;
            }
            current = current.parent;
        }
        return null;
    }

    /**
     * Creates a configurable grid pattern using PIXI.Graphics.
     *
     * This improved function allows you to define the total width and height of the grid,
     * the size of each individual cell, and the visual style of the grid lines.
     *
     * @param {PIXI.Graphics} graphics - The PIXI.Graphics instance to draw the grid on. This should be cleared beforehand if you're redrawing.
     * @param {object} config - The configuration object for the grid.
     * @param {number} config.width - The total width of the grid (e.g., 800).
     * @param {number} config.height - The total height of the grid (e.g., 600).
     * @param {number} config.cellSize - The size of each square cell in the grid (e.g., 50).
     * @param {number} [config.lineColor=0xcccccc] - (Optional) The color of the grid lines in hexadecimal format. Defaults to light gray.
     * @param {number} [config.lineWidth=1] - (Optional) The thickness of the grid lines in pixels. Defaults to 1.
     * @returns {PIXI.Graphics} The same Graphics object, now with the grid drawn onto it.
     */
    buildGrid(graphics, config) {
        // --- Parameter Validation & Defaulting ---
        // Use default values for optional parameters if they aren't provided.
        const width = config.width;
        const height = config.height;
        const cellSize = config.cellSize;
        const lineColor = config.lineColor ?? 0xcccccc; // Nullish coalescing operator (??) for modern, clean defaults
        const lineWidth = config.lineWidth ?? 1;

        // A simple check to prevent infinite loops if cellSize is 0 or negative.
        if (cellSize <= 0) {
            console.error("cellSize must be a positive number.");
            return graphics;
        }

        // Draw the vertical lines
        // We calculate the number of lines needed based on the total width and cell size.
        // We loop from x=0 to x=width, incrementing by cellSize each time.
        for (let x = 0; x <= width; x += cellSize) {
            graphics.moveTo(x, 0).lineTo(x, height);
        }

        // Draw the horizontal lines
        // We loop from y=0 to y=height, incrementing by cellSize each time.
        for (let y = 0; y <= height; y += cellSize) {
            graphics.moveTo(0, y).lineTo(width, y);
        }

        return graphics;
    }

    getConnectionKey(starA, starB) {
        const idA = starA.entity.id;
        const idB = starB.entity.id;
        // Create a consistent key regardless of order
        return idA < idB ? `${idA}-${idB}` : `${idB}-${idA}`;
    }

    // Method to remove a connection line
    removeConnectionLine(starA, starB) {
        const connectionKey = this.getConnectionKey(starA, starB);
        const connectionLine = this.connectionLines.get(connectionKey);

        if (connectionLine) {
            this.mainContainer.removeChild(connectionLine);
            connectionLine.destroy();
            this.connectionLines.delete(connectionKey);
        }
    }

    startConnectionMode(sourceStar) {
        this.isConnecting = true;
        this.connectionSourceStar = sourceStar;
        this.mainContainer.cursor = "crosshair";
        this.contextMenu.hide();
    }

    /**
     * @param {import('pixi.js').FederatedPointerEvent} event
     */
    onPointerUp = (event) => {
        // End selection rectangle
        if (this.selectionManager.isSelecting) {
            this.selectionManager.endSelection();
            return;
        }

        // End group dragging
        if (this.isGroupDragging && event.button === 0) {
            this.isGroupDragging = false;
            this.mainContainer.cursor = "default";
            return;
        }

        // Stop panning
        if (event.button === 0 && this.isPanning) {
            this.isPanning = false;
            this.mainContainer.cursor = "default";
        }
    };

    /**
     * @param {import('pixi.js').FederatedPointerEvent} event
     */
    onPointerMove = (event) => {
        // Handle selection rectangle
        if (this.selectionManager.isSelecting) {
            const worldPosition = this.mainContainer.toLocal(event.global);
            this.selectionManager.updateSelection(worldPosition);
            return;
        }

        // Handle group dragging
        if (this.isGroupDragging && this.selectionManager.hasSelection()) {
            const deltaX = event.global.x - this.groupDragStart.x;
            const deltaY = event.global.y - this.groupDragStart.y;

            // Convert to world coordinates
            const worldDeltaX = deltaX / this.mainContainer.scale.x;
            const worldDeltaY = deltaY / this.mainContainer.scale.y;

            this.selectionManager.moveSelectedItems(worldDeltaX, worldDeltaY);

            this.groupDragStart = { x: event.global.x, y: event.global.y };
            return;
        }

        // Move the container if panning is active
        if (this.isPanning) {
            const dx = event.global.x - this.lastPanPosition.x;
            const dy = event.global.y - this.lastPanPosition.y;

            this.mainContainer.x += dx;
            this.mainContainer.y += dy;

            this.lastPanPosition.copyFrom(event.global);
        }
    };

    /**
     * Handles zooming the main container based on the mouse wheel.
     * @param {import('pixi.js').FederatedWheelEvent} event The federated wheel event from PixiJS.
     */
    onWheelScroll = (event) => {
        event.preventDefault();

        const scroll = event.deltaY;
        if (scroll === 0) return;

        // Calculate the new scale
        const zoomDirection =
            scroll < 0 ? this.ZOOM_FACTOR : 1 / this.ZOOM_FACTOR;
        const currentScale = this.mainContainer.scale.x;
        let newScale = currentScale * zoomDirection;
        newScale = Math.max(this.MIN_ZOOM, Math.min(newScale, this.MAX_ZOOM));

        if (newScale === currentScale) return;

        // Get the mouse's position in the world before the zoom.
        const mousePositionInWorld = this.mainContainer.toLocal(event.global);

        // Apply the new scale.
        this.mainContainer.scale.set(newScale);

        // Calculate the new position to keep the point under the cursor stationary.
        const newPosX = event.global.x - mousePositionInWorld.x * newScale;
        const newPosY = event.global.y - mousePositionInWorld.y * newScale;

        this.mainContainer.position.set(newPosX, newPosY);
    };

    /** Prepare the screen just before showing */
    prepare() {}

    /**
     * Update the screen
     * @param {import('pixi.js').Ticker} time
     */
    update(_time) {
        if (this.paused) return;
    }

    /** Pause gameplay - automatically fired when a popup is presented */
    async pause() {
        this.mainContainer.interactiveChildren = false;
        this.paused = true;
    }

    /** Resume gameplay */
    async resume() {
        this.mainContainer.interactiveChildren = true;
        this.paused = false;
    }

    /** Fully reset */
    reset() {}

    /**
     * Resize the screen, fired whenever window size changes
     * @param {number} width
     * @param {number} height
     */
    resize(width, height) {
        const centerX = width * 0.5;
        const centerY = height * 0.5;

        // Only set initial position if it hasn't been panned/zoomed yet
        if (this.mainContainer.x === 0 && this.mainContainer.y === 0) {
            this.mainContainer.x = centerX;
            this.mainContainer.y = centerY;
        }

        this.pauseButton.x = 30;
        this.pauseButton.y = 30;
        this.settingsButton.x = width - 30;
        this.settingsButton.y = 30;
    }

    /** Show screen with animations */
    async show() {
        const elementsToAnimate = [this.pauseButton, this.settingsButton];

        let finalPromise;
        for (const element of elementsToAnimate) {
            element.alpha = 0;
            finalPromise = animate(
                element,
                { alpha: 1 },
                { duration: 0.3, delay: 0.75, ease: "backOut" },
            );
        }

        if (finalPromise) {
            await finalPromise.finished;
        }
    }

    /** Hide screen with animations */
    async hide() {}

    /** Auto pause the app when window go out of focus */
    blur() {}
}
