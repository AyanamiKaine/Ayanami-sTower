import { sound } from "@pixi/sound";
import { ExtensionType } from "pixi.js";
import { BGM, SFX } from "./audio";

/**
 * Middleware for adding audio functionality to a Pixi.js Application.
 * This plugin attaches an `audio` object to the main application instance,
 * which provides access to background music (BGM) and sound effects (SFX) controllers,
 * as well as master volume controls.
 *
 * @example
 * import { Application, extensions } from 'pixi.js';
 * import { CreationAudioPlugin } from './CreationAudioPlugin';
 *
 * extensions.add(CreationAudioPlugin);
 *
 * const app = new Application();
 * // Now you can access app.audio
 * app.audio.bgm.play('music-loop');
 * app.audio.sfx.play('click-sound');
 * app.audio.setMasterVolume(0.5);
 */
export class CreationAudioPlugin {
    /**
     * Metadata for the Pixi.js extension system.
     * Specifies that this is an Application plugin.
     * @type {import('pixi.js').ExtensionMetadata}
     */
    static extension = ExtensionType.Application;

    /**
     * Initializes the plugin. This method is called by the Pixi.js extension system.
     * It creates and attaches the audio management object to the application.
     * The `this` context within this method is the application instance.
     */
    static init() {
        /** @type {import('pixi.js').Application & { audio: any }} */
        const app = this;

        app.audio = {
            bgm: new BGM(),
            sfx: new SFX(),
            /**
             * Gets the master volume for all sounds.
             * @returns {number} The current master volume (0 to 1).
             */
            getMasterVolume: () => sound.volumeAll,
            /**
             * Sets the master volume for all sounds.
             * Also mutes or unmutes all sounds based on the volume level.
             * @param {number} volume - The desired volume level (0 to 1).
             */
            setMasterVolume: (volume) => {
                sound.volumeAll = volume;
                if (!volume) {
                    sound.muteAll();
                } else {
                    sound.unmuteAll();
                }
            },
        };
    }

    /**
     * Destroys the plugin. This method is called by the Pixi.js extension system.
     * It nullifies the audio property on the application instance.
     * The `this` context within this method is the application instance.
     */
    static destroy() {
        /** @type {import('pixi.js').Application & { audio: any }} */
        const app = this;
        app.audio = null;
    }
}
