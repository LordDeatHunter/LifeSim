const toPaddedNumber = (num, padStart = 2, padEnd = 0) =>
  Math.floor(num).toString().padStart(padStart, "0").padEnd(padEnd, "0");

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

const getOutlineColor = (entity, type) => {
  if (type !== "animal") return undefined;
  switch (entity.foodType) {
    case "HERBIVORE":
      return "#309898";
    case "CARNIVORE":
      return "#cb0404";
    default:
      return "#ffaf2e";
  }
};

const valueToHexSegment = (color) =>
  Math.floor(color * 255)
    .toString(16)
    .padStart(2, "0");

const appendAlpha = (color, alpha) => color + valueToHexSegment(alpha);

const fractionToPercentage = (fraction, decimals = 0) =>
  (fraction * 100).toFixed(decimals);
