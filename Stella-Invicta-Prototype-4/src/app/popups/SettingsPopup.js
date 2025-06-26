import { List } from "@pixi/ui";
import { animate } from "motion";
import { BlurFilter, Container, Sprite, Texture } from "pixi.js";

import { engine } from "../getEngine";
import { Button } from "../ui/Button";
import { Label } from "../ui/Label";
import { RoundedBox } from "../ui/RoundedBox";
import { VolumeSlider } from "../ui/VolumeSlider";
import { userSettings } from "../utils/userSettings";

/**
 * A popup screen that allows the user to change audio settings.
 * It includes sliders for master, background music, and sound effects volume.
 */
export class SettingsPopup extends Container {
    /**
     * The dark semi-transparent background covering the current screen.
     * @private
     * @type {Sprite}
     */
    bg;

    /**
     * Container for the popup UI components.
     * @private
     * @type {Container}
     */
    panel;

    /**
     * The popup title label.
     * @private
     * @type {import("../ui/Label").Label}
     */
    title;

    /**
     * Button that closes the popup.
     * @private
     * @type {Button}
     */
    doneButton;

    /**
     * The panel background.
     * @private
     * @type {RoundedBox}
     */
    panelBase;

    /**
     * The build version label.
     * @private
     * @type {import("../ui/Label").Label}
     */
    versionLabel;

    /**
     * Layout that organizes the UI components.
     * @private
     * @type {List}
     */
    layout;

    /**
     * Slider that changes the master volume.
     * @private
     * @type {VolumeSlider}
     */
    masterSlider;

    /**
     * Slider that changes background music volume.
     * @private
     * @type {VolumeSlider}
     */
    bgmSlider;

    /**
     * Slider that changes sound effects volume.
     * @private
     * @type {VolumeSlider}
     */
    sfxSlider;

    constructor() {
        super();

        this.bg = new Sprite(Texture.WHITE);
        this.bg.tint = 0x0;
        this.bg.interactive = true;
        this.addChild(this.bg);

        this.panel = new Container();
        this.addChild(this.panel);

        this.panelBase = new RoundedBox({ height: 425 });
        this.panel.addChild(this.panelBase);

        this.title = new Label({
            text: "Settings",
            style: {
                fill: 0xec1561,
                fontSize: 50,
            },
        });
        this.title.y = -this.panelBase.boxHeight * 0.5 + 60;
        this.panel.addChild(this.title);

        this.doneButton = new Button({ text: "OK" });
        this.doneButton.y = this.panelBase.boxHeight * 0.5 - 78;
        this.doneButton.onPress.connect(() =>
            engine().navigation.dismissPopup(),
        );
        this.panel.addChild(this.doneButton);

        // APP_VERSION is assumed to be a global variable injected by a build tool like Webpack or Vite.
        this.versionLabel = new Label({
            text: `Version ${typeof APP_VERSION !== "undefined" ? APP_VERSION : "dev"}`,
            style: {
                fill: 0xffffff,
                fontSize: 12,
            },
        });
        this.versionLabel.alpha = 0.5;
        this.versionLabel.y = this.panelBase.boxHeight * 0.5 - 15;
        this.panel.addChild(this.versionLabel);

        this.layout = new List({ type: "vertical", elementsMargin: 4 });
        this.layout.x = -140;
        this.layout.y = -80;
        this.panel.addChild(this.layout);

        this.masterSlider = new VolumeSlider("Master Volume");
        this.masterSlider.onUpdate.connect((v) => {
            userSettings.setMasterVolume(v / 100);
        });
        this.layout.addChild(this.masterSlider);

        this.bgmSlider = new VolumeSlider("BGM Volume");
        this.bgmSlider.onUpdate.connect((v) => {
            userSettings.setBgmVolume(v / 100);
        });
        this.layout.addChild(this.bgmSlider);

        this.sfxSlider = new VolumeSlider("SFX Volume");
        this.sfxSlider.onUpdate.connect((v) => {
            userSettings.setSfxVolume(v / 100);
        });
        this.layout.addChild(this.sfxSlider);
    }

    /**
     * Resize the popup, fired whenever window size changes.
     * @param {number} width - The new width of the screen.
     * @param {number} height - The new height of the screen.
     */
    resize(width, height) {
        this.bg.width = width;
        this.bg.height = height;
        this.panel.x = width * 0.5;
        this.panel.y = height * 0.5;
    }

    /** Set things up just before showing the popup. */
    prepare() {
        this.masterSlider.value = userSettings.getMasterVolume() * 100;
        this.bgmSlider.value = userSettings.getBgmVolume() * 100;
        this.sfxSlider.value = userSettings.getSfxVolume() * 100;
    }

    /** Present the popup, animated. */
    async show() {
        const currentEngine = engine();
        if (currentEngine.navigation.currentScreen) {
            currentEngine.navigation.currentScreen.filters = [
                new BlurFilter({ strength: 4 }),
            ];
        }

        this.bg.alpha = 0;
        this.panel.pivot.y = -400;
        animate(this.bg, { alpha: 0.8 }, { duration: 0.2, ease: "linear" });
        const animation = animate(
            this.panel.pivot,
            { y: 0 },
            { duration: 0.3, ease: "backOut" },
        );
        await animation.finished;
    }

    /** Dismiss the popup, animated. */
    async hide() {
        const currentEngine = engine();
        if (currentEngine.navigation.currentScreen) {
            currentEngine.navigation.currentScreen.filters = [];
        }
        animate(this.bg, { alpha: 0 }, { duration: 0.2, ease: "linear" });
        const animation = animate(
            this.panel.pivot,
            { y: -500 },
            {
                duration: 0.3,
                ease: "backIn",
            },
        );
        await animation.finished;
    }
}
