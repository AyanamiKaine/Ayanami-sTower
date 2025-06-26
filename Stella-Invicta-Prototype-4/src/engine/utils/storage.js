/**
 * Simple local storage utility that can safely get/set number, boolean and object values too
 * not only string as in plain localStorage.
 */
class StorageWrapper {
  /** Get a string value from storage */
  getString(key) {
    return localStorage.getItem(key) ?? undefined;
  }

  /** Set a string value to storage */
  setString(key, value) {
    localStorage.setItem(key, value);
  }

  /** Get a number value from storage or undefined if value can't be converted */
  getNumber(key) {
    const str = this.getString(key) ?? undefined;
    const value = Number(str);
    return isNaN(value) ? null : value;
  }

  /** Set a number value to storage */
  setNumber(key, value) {
    this.setString(key, String(value));
  }

  /** Get a boolean value from storage or undefined if value can't be converted */
  getBool(key) {
    const bool = localStorage.getItem(key);
    return bool ? Boolean(bool.toLowerCase()) : undefined;
  }

  /** Set a boolean value to storage */
  setBool(key, value) {
    localStorage.setItem(key, String(value));
  }

  /** Get an object value from storage or undefined if value can't be parsed */
  getObject(key) {
    const str = this.getString(key);
    if (!str) return undefined;
    try {
      return JSON.parse(str);
    } catch (e) {
      console.warn(e);
      return undefined;
    }
  }

  /** Set an object value to storage */
setObject(key, value) {
    this.setString(key, JSON.stringify(value));
  }
}

export const storage = new StorageWrapper();
