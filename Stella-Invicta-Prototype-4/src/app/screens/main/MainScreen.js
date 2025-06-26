import { FancyButton } from "@pixi/ui";
import { animate } from "motion";
import { Container, Point, Rectangle } from "pixi.js";

import { engine } from "../../getEngine";
import { PausePopup } from "../../popups/PausePopup.";
import { SettingsPopup } from "../../popups/SettingsPopup";
import { ContextMenu } from "../../ui/ContextMenu";
import { Game } from "../../../game/game";
import { StarSystem } from "../../ui/StarSystem";

/**
 * The main screen of the application, responsible for displaying the primary game view,
 * handling user input for navigation like panning and zooming, and managing UI elements
 * like buttons and context menus.
 */
export class MainScreen extends Container {
    /** Assets bundles required by this screen */
    static assetBundles = ["main"];

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

        // Hide context menu on left click
        if (event.button === 0) {
            this.contextMenu.hide();
        }
    };

    /**
     * @param {import('pixi.js').FederatedPointerEvent} event
     */
    onPointerUp = (event) => {
        // Stop panning if the left mouse button is released and we were panning
        if (event.button === 0 && this.isPanning) {
            this.isPanning = false;
            this.mainContainer.cursor = "default";
        }
    };

    /**
     * @param {import('pixi.js').FederatedPointerEvent} event
     */
    onPointerMove = (event) => {
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

    constructor() {
        super();
        /** @type {Game} */
        this.game = new Game();
        /** @type {Container} */
        this.mainContainer = new Container();
        this.addChild(this.mainContainer);
        this.mainContainer.hitArea = new Rectangle(
            -10000,
            -10000,
            20000,
            20000,
        );

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
                        const star = new StarSystem(
                            this.game.createEntity(""), // 1. Pass a placeholder entity object
                            this.contextMenu, // 2. Pass the context menu
                            this.mainContainer, // 3. Pass the main container as the stage
                        );
                        const localPosition = this.mainContainer.toLocal(
                            event.global,
                        );
                        star.position.set(localPosition.x, localPosition.y);
                        this.mainContainer.addChild(star);
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

        const buttonAnimations = {
            hover: { props: { scale: { x: 1.1, y: 1.1 } }, duration: 100 },
            pressed: { props: { scale: { x: 0.9, y: 0.9 } }, duration: 100 },
        };

        /** @private */
        this.pauseButton = new FancyButton({
            defaultView: "icon-pause.png",
            anchor: 0.5,
            animations: buttonAnimations,
        });
        this.pauseButton.onPress.connect(() =>
            engine().navigation.presentPopup(PausePopup),
        );
        this.addChild(this.pauseButton);

        /** @private */
        this.settingsButton = new FancyButton({
            defaultView: "icon-settings.png",
            anchor: 0.5,
            animations: buttonAnimations,
        });
        this.settingsButton.onPress.connect(() =>
            engine().navigation.presentPopup(SettingsPopup),
        );
        this.addChild(this.settingsButton);

        this.contextMenu = new ContextMenu();
        this.addChild(this.contextMenu);
    }

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
    blur() {
        if (!engine().navigation.currentPopup) {
            engine().navigation.presentPopup(PausePopup);
        }
    }
}
