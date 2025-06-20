const lerp = (a, b, t) => a + (b - a) * t;

const clamp = (value, min, max) => Math.min(Math.max(value, min), max);

const toPaddedNumber = (num, padStart = 2, padEnd = 0) =>
  toPaddedString(Math.floor(num).toString(), padStart, padEnd);

const toPaddedString = (numericString, padStart = 2, padEnd = 0) =>
  numericString.padStart(padStart, "0").padEnd(padEnd, "0");

const getTimeString = (milliseconds) => {
  const seconds = milliseconds / 1000;
  const minutes = seconds / 60;
  const hours = minutes / 60;
  const days = hours / 24;

  const remainingStr = toPaddedNumber((milliseconds % 1000) / 100, 0, 1);
  const secondsStr = toPaddedNumber(seconds % 60);
  const minutesStr = toPaddedNumber(minutes % 60);
  const hoursStr = toPaddedNumber(hours % 24);
  const daysStr = toPaddedNumber(days);

  return `${daysStr}d ${hoursStr}h ${minutesStr}m ${secondsStr}.${remainingStr}s`;
};

const valueToHexSegment = (color) =>
  Math.floor(color * 255)
    .toString(16)
    .padStart(2, "0");

const appendAlpha = (color, alpha) => color + valueToHexSegment(alpha);

const fractionToPercentage = (fraction, decimals = 0) =>
  (fraction * 100).toFixed(decimals);
