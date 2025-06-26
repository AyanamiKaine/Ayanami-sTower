export function getResolution() {
  let resolution = Math.max(window.devicePixelRatio, 2);

  if (resolution % 1 !== 0) {
    resolution = 2;
  }

  return resolution;
}
