import { ExtensionType } from "pixi.js";

import { resize } from "./resize";

/**
 * Middleware for Application's resize functionality.
 *
 * Adds the following methods to Application:
 * * Application#resizeTo
 * * Application#resize
 * * Application#queueResize
 * * Application#cancelResize
 * * Application#resizeOptions
 */
export class CreationResizePlugin {
    /** @ignore */
    static extension = ExtensionType.Application;

    static _resizeId;
    static _resizeTo;
    static _cancelResize;

    /**
     * Initialize the plugin with scope of application instance
     * @param {object} [options] - See application options
     */
    static init(options) {
        const app = this;

        Object.defineProperty(
            app,
            "resizeTo",
            /**
             * The HTML element or window to automatically resize the
             * renderer's view element to match width and height.
             */
            {
                set(dom) {
                    globalThis.removeEventListener("resize", app.queueResize);
                    this._resizeTo = dom;
                    if (dom) {
                        globalThis.addEventListener("resize", app.queueResize);
                        app.resize();
                    }
                },
                get() {
                    return this._resizeTo;
                },
            },
        );

        /**
         * Resize is throttled, so it's safe to call this multiple times per frame and it'll
         * only be called once.
         */
        app.queueResize = () => {
            if (!this._resizeTo) {
                return;
            }

            this._cancelResize();

            // Throttle resize events per raf
            this._resizeId = requestAnimationFrame(() => app.resize());
        };

        /**
         * Execute an immediate resize on the renderer, this is not
         * throttled and can be expensive to call many times in a row.
         * Will resize only if `resizeTo` property is set.
         */
        app.resize = () => {
            if (!this._resizeTo) {
                return;
            }

            // clear queue resize
            this._cancelResize();

            let canvasWidth;
            let canvasHeight;

            // Resize to the window
            if (this._resizeTo === globalThis.window) {
                canvasWidth = globalThis.innerWidth;
                canvasHeight = globalThis.innerHeight;
            }
            // Resize to other HTML entities
            else {
                const { clientWidth, clientHeight } = this._resizeTo;

                canvasWidth = clientWidth;
                canvasHeight = clientHeight;
            }

            const { width, height } = resize(
                canvasWidth,
                canvasHeight,
                app.resizeOptions.minWidth,
                app.resizeOptions.minHeight,
                app.resizeOptions.letterbox,
            );

            app.renderer.canvas.style.width = `${canvasWidth}px`;
            app.renderer.canvas.style.height = `${canvasHeight}px`;
            window.scrollTo(0, 0);

            app.renderer.resize(width, height);
        };

        this._cancelResize = () => {
            if (this._resizeId) {
                cancelAnimationFrame(this._resizeId);
                this._resizeId = null;
            }
        };
        this._resizeId = null;
        this._resizeTo = null;
        app.resizeOptions = {
            minWidth: 768,
            minHeight: 1024,
            letterbox: true,
            ...options.resizeOptions,
        };
        app.resizeTo = options.resizeTo || null;
    }

    /**
     * Clean up the ticker, scoped to application
     */
    static destroy() {
        const app = this;

        globalThis.removeEventListener("resize", app.queueResize);
        this._cancelResize();
        this._cancelResize = null;
        app.queueResize = null;
        app.resizeTo = null;
        app.resize = null;
    }
}
