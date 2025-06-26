import { ExtensionType } from "pixi.js";
import { Navigation } from "./navigation";

/**
 * Middleware for adding navigation functionality to a Pixi.js Application.
 * This plugin attaches a `navigation` instance to the main application object.
 *
 * @example
 * import { Application, extensions } from 'pixi.js';
 * import { CreationNavigationPlugin } from './CreationNavigationPlugin';
 *
 * extensions.add(CreationNavigationPlugin);
 *
 * const app = new Application();
 * // Now you can access app.navigation
 * app.navigation.showScreen(MyScreen);
 */
export class CreationNavigationPlugin {
    /**
     * Metadata for the Pixi.js extension system.
     * Specifies that this is an Application plugin.
     * @type {import('pixi.js').ExtensionMetadata}
     */
    static extension = ExtensionType.Application;

    /**
     * Stores the resize handler function so it can be removed later.
     * @private
     */
    static onResize = null;

    /**
     * Initializes the plugin. This method is called by the Pixi.js extension system.
     * It creates a new Navigation instance and attaches it to the application.
     * The `this` context within this method is the application instance.
     */
    static init() {
        /** @type {import('../engine').CreationEngine & { navigation: Navigation }} */
        const app = this;

        // Create and initialize the navigation manager
        app.navigation = new Navigation();
        app.navigation.init(app);

        // Create a resize handler and store it
        this.onResize = () =>
            app.navigation.resize(app.renderer.width, app.renderer.height);

        // Add the resize listener
        app.renderer.on("resize", this.onResize);

        // Trigger an initial resize
        app.navigation.resize(app.renderer.width, app.renderer.height);
    }

    /**
     * Destroys the plugin. This method is called by the Pixi.js extension system.
     * It removes the resize listener and nullifies the navigation property.
     * The `this` context within this method is the application instance.
     */
    static destroy() {
        /** @type {import('pixi.js').Application & { navigation: Navigation | null }} */
        const app = this;

        // Remove the resize listener if it exists
        if (this.onResize) {
            app.renderer.off("resize", this.onResize);
            this.onResize = null;
        }

        // Clean up the navigation property
        app.navigation = null;
    }
}
