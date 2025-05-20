const socket = new WebSocket("ws://localhost:5000/ws");

let canvas;
let ctx;
let reigniteLifeButton;

const lerpDuration = 100;
const lerp = (a, b, t) => a + (b - a) * t;

let prevEntities = {};
let entities = {};
let lastUpdate = performance.now();

let recentlySpawned = {};
let recentlyDespawned = {};

const toPaddedNumber = (num, padStart = 2, padEnd = 0) =>
  Math.floor(num).toString().padStart(padStart, "0").padEnd(padEnd, "0");

const getTimeString = (milliseconds) => {
  const seconds = milliseconds / 1000;
  const minutes = seconds / 60;
  const hours = minutes / 60;
  const days = hours / 24;

  const remainingStr = toPaddedNumber((milliseconds % 1000) / 100, 0, 3);
  const secondsStr = toPaddedNumber(seconds % 60);
  const minutesStr = toPaddedNumber(minutes % 60);
  const hoursStr = toPaddedNumber(hours % 60);
  const daysStr = toPaddedNumber(days % 24);

  return `${daysStr}d ${hoursStr}h ${minutesStr}m ${secondsStr}s ${remainingStr}ms`;
};

socket.onmessage = (event) => {
  const { animals, foods, activeClients, timeFromStart } = JSON.parse(
    event.data,
  );
  lastUpdate = performance.now();

  const animalCount = animals.length;
  const foodCount = foods.length;

  document.querySelector("h3").innerText =
    activeClients === 1
      ? `${activeClients} viewer`
      : `${activeClients} viewers`;

  prevEntities = { ...entities };
  entities = {};
  for (const entity of [...animals, ...foods]) {
    entities[entity.id] = entity;
  }

  for (const id in prevEntities) {
    if (id in entities) continue;
    recentlyDespawned[id] = { entity: prevEntities[id], opacity: 1 };
  }

  for (const id in entities) {
    if (id in prevEntities) continue;
    recentlySpawned[id] = 0;
  }

  const total = animalCount + foodCount;
  const animalPercent = ((animalCount / total) * 100).toFixed();
  const foodPercent = ((foodCount / total) * 100).toFixed();
  document.querySelector("h2:nth-of-type(1)").innerText =
    `Animals: ${animalCount}\n${animalPercent}%`;
  document.querySelector("h2:nth-of-type(2)").innerText =
    `Food: ${foodCount}\n${foodPercent}%`;
  document.querySelector("h1").innerText =
    `Elapsed time: ${getTimeString(timeFromStart)}`;

  reigniteLifeButton.disabled = animalCount > 0;
};

const renderEntity = (position, diameter, color, outline) => {
  ctx.beginPath();
  ctx.arc(position.x, position.y, diameter / 2, 0, Math.PI * 2);
  ctx.fillStyle = color;
  ctx.fill();

  if (!outline) return;
  ctx.lineWidth = 2;
  ctx.strokeStyle = outline;
  ctx.stroke();
};

const render = () => {
  const now = performance.now();
  ctx.clearRect(0, 0, canvas.width, canvas.height);

  const t = Math.min((now - lastUpdate) / lerpDuration, 1);

  for (const id in entities) {
    const curr = entities[id];
    const prev = id in prevEntities ? prevEntities[id] : curr;

    let opacity = 1;

    if (recentlySpawned[id] !== undefined) {
      opacity = recentlySpawned[id];
      recentlySpawned[id] += 0.03 * t;
      if (recentlySpawned[id] > 1) {
        delete recentlySpawned[id];
      }
    }

    const x = lerp(prev.x, curr.x, t);
    const y = lerp(prev.y, curr.y, t);
    const color =
      curr.color +
      Math.floor(opacity * 255)
        .toString(16)
        .padStart(2, "0");
    const size = curr.size;

    let outline;
    if (curr.type === "Animal") {
      switch (curr.foodType) {
        case 1:
          outline = "#00FF00";
          break;
        case 2:
          outline = "#FF0000";
          break;
        default:
          outline = "#FFFFFF";
      }
    }

    renderEntity({ x, y }, size, color, outline);
  }

  for (const id in recentlyDespawned) {
    const { entity, opacity } = recentlyDespawned[id];

    if (opacity <= 0) {
      delete recentlyDespawned[id];
      continue;
    }

    const { x, y, size } = entity;
    const alpha = Math.floor(opacity * 255)
      .toString(16)
      .padStart(2, "0");
    const color = entity.color + alpha;

    recentlyDespawned[id].opacity -= 0.03 * t;

    let outline;
    if (entity.type === "Animal") {
      switch (entity.foodType) {
        case 1:
          outline = "#00FF00";
          break;
        case 2:
          outline = "#FF0000";
          break;
        default:
          outline = "#FFFFFF";
      }
      outline += alpha;
    }

    renderEntity({ x, y }, size, color, outline);
  }

  requestAnimationFrame(render);
};

document.addEventListener("DOMContentLoaded", () => {
  canvas = document.getElementById("view");
  ctx = canvas.getContext("2d");
  reigniteLifeButton = document.getElementById("reignite-life-button");
  reigniteLifeButton.addEventListener("click", reigniteLife);

  requestAnimationFrame(render);
});

const reigniteLife = () => {
  fetch("http://localhost:5000/api/reignite_life");
};
