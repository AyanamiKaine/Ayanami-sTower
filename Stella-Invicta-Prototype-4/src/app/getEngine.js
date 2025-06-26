let instance = null;

/**
 * Get the main application engine
 * This is a simple way to access the engine instance from anywhere in the app
 */
export function engine() {
  return instance;
}

export function setEngine(app) {
  instance = app;
}
