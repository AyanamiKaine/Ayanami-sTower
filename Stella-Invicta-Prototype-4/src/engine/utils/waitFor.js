/** Pause the code for a certain amount of time, in seconds */
export function waitFor(delayInSecs = 1) {
  return new Promise((resolve) => {
    setTimeout(() => resolve(), delayInSecs * 1000);
  });
}
