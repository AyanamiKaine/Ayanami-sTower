import { Assets, BigPool, Container } from "pixi.js";

/**
 * The Navigation class is responsible for managing screens and popups within a Pixi.js application.
 * It handles the lifecycle of screens, including loading assets, showing, hiding, resizing, and updating.
 *
 * It assumes the existence of an `AppScreen` base class that your individual screens will extend.
 * This class should have methods like `prepare`, `show`, `hide`, `update`, `resize`, etc.
 */
export class Navigation {
    /** * Reference to the main application engine.
     * @type {import('../engine').CreationEngine}
     */
    app;

    /** * The main container for all screens managed by this navigation instance.
     * @type {Container}
     */
    container = new Container();

    /** * The current width of the application view.
     * @type {number}
     */
    width = 0;

    /** * The current height of the application view.
     * @type {number}
     */
    height = 0;

    /** * A persistent background screen that stays visible behind all other screens.
     * @type {import('./AppScreen').AppScreen | undefined}
     */
    background;

    /** * The currently active and visible screen.
     * @type {import('./AppScreen').AppScreen | undefined}
     */
    currentScreen;

    /** * The currently active and visible popup, displayed on top of the current screen.
     * @type {import('./AppScreen').AppScreen | undefined}
     */
    currentPopup;

    /**
     * Initializes the Navigation with a reference to the main application.
     * @param {import('../engine').CreationEngine} app - The main application instance.
     */
    init(app) {
        this.app = app;
    }

    /**
     * Sets and displays a default background screen.
     * @param {new () => import('./AppScreen').AppScreen} ctor - The constructor of the screen to be used as the background.
     */
    setBackground(ctor) {
        this.background = new ctor();
        this.#addAndShowScreen(this.background);
    }

    /**
     * A private method to add a screen to the stage and run its lifecycle methods.
     * @private
     * @param {import('./AppScreen').AppScreen} screen - The screen instance to add and show.
     */
    async #addAndShowScreen(screen) {
        // Add the main container to the stage if it's not already there.
        if (!this.container.parent) {
            this.app.stage.addChild(this.container);
        }

        // Add the new screen to the container.
        this.container.addChild(screen);

        // Call the screen's prepare method if it exists.
        if (screen.prepare) {
            screen.prepare();
        }

        // Call the screen's resize method if it exists, triggering an initial resize.
        if (screen.resize) {
            screen.resize(this.width, this.height);
        }

        // Add the screen's update method to the application's ticker.
        if (screen.update) {
            this.app.ticker.add(screen.update, screen);
        }

        // Call the screen's show method to animate its appearance.
        if (screen.show) {
            screen.interactiveChildren = false; // Disable interaction during transition
            await screen.show();
            screen.interactiveChildren = true; // Re-enable interaction after transition
        }
    }

    /**
     * A private method to hide and remove a screen from the stage.
     * @private
     * @param {import('./AppScreen').AppScreen} screen - The screen instance to hide and remove.
     */
    async #hideAndRemoveScreen(screen) {
        // Disable interaction on the screen.
        screen.interactiveChildren = false;

        // Call the screen's hide method to animate its disappearance.
        if (screen.hide) {
            await screen.hide();
        }

        // Remove the screen's update method from the ticker.
        if (screen.update) {
            this.app.ticker.remove(screen.update, screen);
        }

        // Remove the screen from its parent container.
        if (screen.parent) {
            screen.parent.removeChild(screen);
        }

        // Call the screen's reset method for cleanup, allowing it to be reused by an object pool.
        if (screen.reset) {
            screen.reset();
        }
    }

    /**
     * Hides the current screen and shows a new one.
     * @param {new () => import('./AppScreen').AppScreen & { assetBundles?: string[] }} ctor - The constructor of the new screen to show.
     */
    async showScreen(ctor) {
        // Block interaction on the current screen during the transition.
        if (this.currentScreen) {
            this.currentScreen.interactiveChildren = false;
        }

        // Load assets for the new screen, if any are defined.
        if (ctor.assetBundles) {
            await Assets.loadBundle(ctor.assetBundles, (progress) => {
                // Forward loading progress to the current screen if it has a handler.
                if (this.currentScreen?.onLoad) {
                    this.currentScreen.onLoad(progress * 100);
                }
            });
        }

        // Ensure the onLoad handler hits 100%
        if (this.currentScreen?.onLoad) {
            this.currentScreen.onLoad(100);
        }

        // Hide and remove the old screen.
        if (this.currentScreen) {
            await this.#hideAndRemoveScreen(this.currentScreen);
        }

        // Get a new screen instance from the pool (or create one) and show it.
        this.currentScreen = BigPool.get(ctor);
        await this.#addAndShowScreen(this.currentScreen);
    }

    /**
     * Resizes all active screens (background, current screen, popup).
     * @param {number} width - The new viewport width.
     * @param {number} height - The new viewport height.
     */
    resize(width, height) {
        this.width = width;
        this.height = height;
        this.background?.resize?.(width, height);
        this.currentScreen?.resize?.(width, height);
        this.currentPopup?.resize?.(width, height);
    }

    /**
     * Displays a popup over the current screen.
     * @param {new () => import('./AppScreen').AppScreen} ctor - The constructor for the popup screen.
     */
    async presentPopup(ctor) {
        // Pause the current screen and disable interaction.
        if (this.currentScreen) {
            this.currentScreen.interactiveChildren = false;
            await this.currentScreen.pause?.();
        }

        // Remove any existing popup.
        if (this.currentPopup) {
            await this.#hideAndRemoveScreen(this.currentPopup);
        }

        // Create and show the new popup.
        this.currentPopup = new ctor();
        await this.#addAndShowScreen(this.currentPopup);
    }

    /**
     * Hides and removes the current popup.
     */
    async dismissPopup() {
        if (!this.currentPopup) return;

        const popup = this.currentPopup;
        this.currentPopup = undefined;

        await this.#hideAndRemoveScreen(popup);

        // Resume the main screen.
        if (this.currentScreen) {
            this.currentScreen.interactiveChildren = true;
            this.currentScreen.resume?.();
        }
    }

    /**
     * Calls the blur method on all active screens.
     */
    blur() {
        this.background?.blur?.();
        this.currentScreen?.blur?.();
        this.currentPopup?.blur?.();
    }

    /**
     * Calls the focus method on all active screens.
     */
    focus() {
        this.background?.focus?.();
        this.currentScreen?.focus?.();
        this.currentPopup?.focus?.();
    }
}
