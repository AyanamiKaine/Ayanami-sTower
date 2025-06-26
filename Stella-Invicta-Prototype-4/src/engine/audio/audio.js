import { sound } from "@pixi/sound";
import { animate } from "motion";

/**
 * Handles background music (BGM), playing only one audio file in a loop at a time.
 * It fades out the current music before playing a new one. It also provides
 * volume control specifically for the BGM.
 */
export class BGM {
    /**
     * The alias of the current music being played.
     * @type {string | undefined}
     */
    currentAlias;

    /**
     * The current Sound instance being played.
     * @type {import('@pixi/sound').Sound | undefined}
     */
    current;

    /**
     * The volume for the background music.
     * @private
     */
    #volume = 1;

    /**
     * Plays a background music track. If another track is playing, it will be faded out and stopped.
     * @param {string} alias - The alias of the sound to play.
     * @param {import('@pixi/sound').PlayOptions} [options] - Optional settings for playback.
     */
    async play(alias, options) {
        // Do nothing if the requested music is already playing.
        if (this.currentAlias === alias) return;

        // Fade out and then stop the current music if it exists.
        if (this.current) {
            const current = this.current;
            animate(
                current,
                { volume: 0 },
                { duration: 1, ease: "linear" },
            ).then(() => {
                current.stop();
            });
        }

        // Find the new sound instance.
        this.current = sound.find(alias);

        // Play and fade in the new music.
        this.currentAlias = alias;
        this.current.play({ loop: true, ...options });
        this.current.volume = 0;
        animate(
            this.current,
            { volume: this.#volume },
            { duration: 1, ease: "linear" },
        );
    }

    /**
     * Gets the background music volume.
     * @returns {number} The current volume.
     */
    getVolume() {
        return this.#volume;
    }

    /**
     * Sets the background music volume.
     * @param {number} v - The new volume level (0 to 1).
     */
    setVolume(v) {
        this.#volume = v;
        if (this.current) {
            this.current.volume = this.#volume;
        }
    }
}

/**
 * Handles short sound special effects (SFX).
 * This class primarily exists to provide its own volume control, separate from BGM.
 * Note: The volume control only affects newly played sounds, not instances that are already playing.
 * This is generally acceptable for short-lived sound effects.
 */
export class SFX {
    /**
     * The volume scale for new sound effect instances.
     * @private
     */
    #volume = 1;

    /**
     * Plays a one-shot sound effect.
     * @param {string} alias - The alias of the sound to play.
     * @param {import('@pixi/sound').PlayOptions} [options] - Optional settings for playback.
     */
    play(alias, options) {
        const baseVolume = options?.volume ?? 1;
        const volume = this.#volume * baseVolume;
        sound.play(alias, { ...options, volume });
    }

    /**
     * Gets the sound effects volume.
     * @returns {number} The current volume.
     */
    getVolume() {
        return this.#volume;
    }

    /**
     * Sets the sound effects volume. Does not affect instances that are currently playing.
     * @param {number} v - The new volume level (0 to 1).
     */
    setVolume(v) {
        this.#volume = v;
    }
}
