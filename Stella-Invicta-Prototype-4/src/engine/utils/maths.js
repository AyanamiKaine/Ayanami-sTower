/** Get the distance between a and b points */
export function getDistance(ax, ay, bx = 0, by = 0) {
  const dx = bx - ax;
  const dy = by - ay;
  return Math.sqrt(dx * dx + dy * dy);
}

/** Linear interpolation */
export function lerp(a, b, t) {
  return (1 - t) * a + t * b;
}

/** Clamp a number to minimum and maximum values */
export function clamp(v, min = 0, max = 1) {
  if (min > max) [min, max] = [max, min];
  return v < min ? min : v > max ? max : v;
}
