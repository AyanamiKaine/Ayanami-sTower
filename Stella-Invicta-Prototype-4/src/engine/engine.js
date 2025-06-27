import { sound } from "@pixi/sound";
import { Application, Assets, extensions, ResizePlugin } from "pixi.js";
import "pixi.js/app";

// eslint-disable-next-line @typescript-eslint/ban-ts-comment
// @ts-ignore - This is a dynamically generated file by AssetPack
import manifest from "../manifest.json";

import { CreationAudioPlugin } from "./audio/AudioPlugin";
import { CreationNavigationPlugin } from "./navigation/NavigationPlugin";
import { CreationResizePlugin } from "./resize/ResizePlugin";
import { getResolution } from "./utils/getResolution";

extensions.remove(ResizePlugin);
extensions.add(CreationResizePlugin);
extensions.add(CreationAudioPlugin);
extensions.add(CreationNavigationPlugin);

/**
 * The main creation engine class.
 *
 * This is a lightweight wrapper around the PixiJS Application class.
 * It provides a few additional features such as:
 * - Navigation manager
 * - Audio manager
 * - Resize handling
 * - Visibility change handling (pause/resume sounds)
 *
 * It also initializes the PixiJS application and loads any assets in the `preload` bundle.
 */
export class CreationEngine extends Application {
    
    _wheelListener = null;

    /** Initialize the application */
    async init(opts) {
        opts.resizeTo ??= window;
        opts.resolution ??= getResolution();
        opts.autoDensity = true;
        opts.antialias = true;
        opts.powerPreference = "high-performance";
        opts.premultipliedAlpha = true;
        await super.init(opts);

        // Append the application canvas to the document body
        document.getElementById("pixi-container").appendChild(this.canvas);
        // Add a visibility listener, so the app can pause sounds and screens
        document.addEventListener("visibilitychange", this.visibilityChange);

        document.addEventListener("contextmenu", (e) => {
            e.preventDefault();
        });

        this._wheelListener = (event) => {
            // If the ctrlKey is pressed, prevent the default browser zoom action.
            if (event.ctrlKey) {
                event.preventDefault();
            }
        };
        
        window.addEventListener("wheel", this._wheelListener, {
            passive: false,
        });

        // Init PixiJS assets with this asset manifest
        await Assets.init({ manifest, basePath: "assets" });
        await Assets.loadBundle("preload");

        // List all existing bundles names
        const allBundles = manifest.bundles.map((item) => item.name);
        // Start up background loading of all bundles
        Assets.backgroundLoadBundle(allBundles);
    }

    destroy(rendererDestroyOptions = false, options = false) {
        document.removeEventListener("visibilitychange", this.visibilityChange);
        super.destroy(rendererDestroyOptions, options);
    }

    /** Fire when document visibility changes - lose or regain focus */
    visibilityChange = () => {
        if (document.hidden) {
            sound.pauseAll();
            this.navigation.blur();
        } else {
            sound.resumeAll();
            this.navigation.focus();
        }
    };
}
