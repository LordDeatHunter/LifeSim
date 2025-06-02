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

const lerpRGB = (color1, color2, t) => {
  const r1 = parseInt(color1.slice(1, 3), 16);
  const g1 = parseInt(color1.slice(3, 5), 16);
  const b1 = parseInt(color1.slice(5, 7), 16);

  const r2 = parseInt(color2.slice(1, 3), 16);
  const g2 = parseInt(color2.slice(3, 5), 16);
  const b2 = parseInt(color2.slice(5, 7), 16);

  const r = Math.round(lerp(r1, r2, t));
  const g = Math.round(lerp(g1, g2, t));
  const b = Math.round(lerp(b1, b2, t));

  return `#${toPaddedString(r.toString(16))}${toPaddedString(g.toString(16))}${toPaddedString(b.toString(16))}`;
};

const getOutlineColor = (entity, type) => {
  if (type !== "animal") return undefined;

  if (entity.predationInclanation === undefined) return "#ffaf2e";

  const low = "#309898";
  const mid = "#ffaf2e";
  const high = "#cb0404";

  if (entity.predationInclanation <= 0.5) {
    const t = entity.predationInclanation / 0.5;
    return lerpRGB(low, mid, t);
  } else {
    const t = (entity.predationInclanation - 0.5) / 0.5;
    return lerpRGB(mid, high, t);
  }
};

const valueToHexSegment = (color) =>
  Math.floor(color * 255)
    .toString(16)
    .padStart(2, "0");

const appendAlpha = (color, alpha) => color + valueToHexSegment(alpha);

const fractionToPercentage = (fraction, decimals = 0) =>
  (fraction * 100).toFixed(decimals);
