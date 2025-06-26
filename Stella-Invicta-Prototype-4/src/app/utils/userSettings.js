import { storage } from "../../engine/utils/storage";
import { engine } from "../getEngine";

// Keys for saved items in storage
const KEY_VOLUME_MASTER = "volume-master";
const KEY_VOLUME_BGM = "volume-bgm";
const KEY_VOLUME_SFX = "volume-sfx";

/**
 * Persistent user settings of volumes.
 */
class UserSettings {
  init() {
    engine().audio.setMasterVolume(this.getMasterVolume());
    engine().audio.bgm.setVolume(this.getBgmVolume());
    engine().audio.sfx.setVolume(this.getSfxVolume());
  }

  /** Get overall sound volume */
  getMasterVolume() {
    return storage.getNumber(KEY_VOLUME_MASTER) ?? 0.5;
  }

  /** Set overall sound volume */
  setMasterVolume(value) {
    engine().audio.setMasterVolume(value);
    storage.setNumber(KEY_VOLUME_MASTER, value);
  }

  /** Get background music volume */
  getBgmVolume() {
    return storage.getNumber(KEY_VOLUME_BGM) ?? 1;
  }

  /** Set background music volume */
  setBgmVolume(value) {
    engine().audio.bgm.setVolume(value);
    storage.setNumber(KEY_VOLUME_BGM, value);
  }

  /** Get sound effects volume */
getSfxVolume() {
    return storage.getNumber(KEY_VOLUME_SFX) ?? 1;
  }

  /** Set sound effects volume */
setSfxVolume(value) {
    engine().audio.sfx.setVolume(value);
    storage.setNumber(KEY_VOLUME_SFX, value);
  }
}

/** SHared user settings instance */
export const userSettings = new UserSettings();
