// From a very good answer about pseudo random numbers on stack overflow
// https://stackoverflow.com/a/47593316
function xmur3(str) {
  let h = 1779033703 ^ str.length;

  for (let i = 0; i < str.length; i++) {
    h = Math.imul(h ^ str.charCodeAt(i), 3432918353);
    h = (h << 13) | (h >>> 19);
  }

  return () => {
    h = Math.imul(h ^ (h >>> 16), 2246822507);
    h = Math.imul(h ^ (h >>> 13), 3266489909);

    return (h ^= h >>> 16) >>> 0;
  };
}

function mulberry32(a) {
  return () => {
    let t = (a += 0x6d2b79f5)

    t = Math.imul(t ^ (t >>> 15), t | 1);
    t ^= t + Math.imul(t ^ (t >>> 7), t | 61);

    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}

const HASH_CHARSET =
  "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

/**
 * Creates a seeded random function similar to Math.random() based on given seed hash
 * @param seed - The hash string, can be anything
 * @returns Function that can be used instead Math.random
 */
export function randomSeeded(seed) {
  return mulberry32(xmur3(seed)());
}

/**
 * Returns a random color
 * @param random - The random function to be used (defaults to Math.random)
 */
export function randomColor(random = Math.random) {
  const r = Math.floor(0xff * random());
  const g = Math.floor(0xff * random());
  const b = Math.floor(0xff * random());
  return (r << 16) | (g << 8) | b;
}

/**
 * Returns a random number within a range
 * @param min - lowest number (inclusive)
 * @param max - highest number (exclusive)
 * @param random - The random function to be used (defaults to Math.random)
 */
export function randomRange(
  min,
  max,
  random = Math.random,
){
  const a = Math.min(min, max);
  const b = Math.max(min, max);

  const v = a + (b - a) * random();

  return v;
}

/**
 * Returns a random item from an object or array
 * @param arr - array to be selected
 * @param random - The random function to be used (defaults to Math.random)
 */
export function randomItem(obj, random = Math.random) {
  if (Array.isArray(obj)) {
    return obj[Math.floor(random() * obj.length)];
  }

  const keys = Object.keys(obj);
  const key = keys[Math.floor(random() * keys.length)];
  return obj[key];
}

/**
 * Returns a random boolean.
 * @param weight - The chance of true value, between 0 and 1
 * @param random - The random function to be used (defaults to Math.random)
 * @returns
 */
export function randomBool(weight = 0.5, random = Math.random) {
  return random() < weight;
}

/**
 * Random shuffle an array in place, without cloning it
 * @param array - The array that will be shuffled
 * @param random - The random function to be used (defaults to Math.random)
 * @returns
 */
export function randomShuffle(array, random = Math.random) {
  let currentIndex = array.length;
  let temporaryValue;
  let randomIndex;

  while (currentIndex !== 0) {
    randomIndex = Math.floor(random() * currentIndex);
    currentIndex -= 1;
    temporaryValue = array[currentIndex];
    array[currentIndex] = array[randomIndex];
    array[randomIndex] = temporaryValue;
  }

  return array;
}

/**
 * Return a random string hash - not guaranteed to be unique
 * @param length - The length of the hash
 * @param random - The random function to be used (defaults to Math.random)
 * @returns
 */
export function randomHash(
  length,
  random = Math.random,
  charset = HASH_CHARSET,
) {
  const charsetLength = charset.length;
  let result = "";

  for (let i = 0; i < length; i++) {
    result += charset.charAt(Math.floor(random() * charsetLength));
  }

  return result;
}

/**
 * Returns a random number within a range.
 *
 * @param min - The minimum value (inclusive).
 * @param max - The maximum value (exclusive).
 */
export function randomFloat(min, max, random = Math.random) {
  return random() * (max - min) + min;
}

/**
 * Returns a random integer within a range.
 *
 * @param min - The minimum value (inclusive).
 * @param max - The minimum value (inclusive).
 * @param random - The random function to be used (defaults to Math.random)
 */
export function randomInt(min, max, random = Math.random) {
  // This function will return 4 if float result is 3.5 because of +1. Should return 3 instead?
  return Math.floor(random() * (max - min + 1)) + min;
}
