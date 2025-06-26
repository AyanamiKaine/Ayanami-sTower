import { CircularProgressBar } from "@pixi/ui";
import { animate } from "motion";
import { Container, Sprite, Texture } from "pixi.js";

/**
 * Screen shown while loading assets. It displays a logo and a circular progress bar.
 */
export class LoadScreen extends Container {
    /** Assets bundles required by this screen */
    static assetBundles = ["preload"];

    /**
     * The PixiJS logo sprite.
     * @private
     * @type {Sprite}
     */
    pixiLogo;

    /**
     * The circular progress bar UI element.
     * @private
     * @type {CircularProgressBar}
     */
    progressBar;

    constructor() {
        super();

        this.progressBar = new CircularProgressBar({
            backgroundColor: "#3d3d3d",
            fillColor: "#e72264",
            radius: 100,
            lineWidth: 15,
            value: 20,
            backgroundAlpha: 0.5,
            fillAlpha: 0.8,
            cap: "round",
        });

        this.progressBar.x += this.progressBar.width / 2;
        this.progressBar.y += -this.progressBar.height / 2;

        this.addChild(this.progressBar);

        this.pixiLogo = new Sprite({
            texture: Texture.from("logo.svg"),
            anchor: 0.5,
            scale: 0.2,
        });
        this.addChild(this.pixiLogo);
    }

    /**
     * Updates the progress bar's value.
     * @param {number} progress - The loading progress, from 0 to 100.
     */
    onLoad(progress) {
        this.progressBar.progress = progress;
    }

    /**
     * Resize the screen, fired whenever the window size changes.
     * @param {number} width - The new width of the screen.
     * @param {number} height - The new height of the screen.
     */
    resize(width, height) {
        this.pixiLogo.position.set(width * 0.5, height * 0.5);
        this.progressBar.position.set(width * 0.5, height * 0.5);
    }

    /** Show screen with animations. */
    async show() {
        this.alpha = 1;
    }

    /** Hide screen with animations. */
    async hide() {
        const animation = animate(
            this,
            { alpha: 0 },
            {
                duration: 0.3,
                ease: "linear",
                delay: 1,
            },
        );
        await animation.finished;
    }
}
