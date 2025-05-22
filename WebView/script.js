const socket = new WebSocket("ws://localhost:5000/ws");

const lerpDuration = 100;
const lerp = (a, b, t) => a + (b - a) * t;

let prevEntities = {};
let entities = {};
let lastUpdate = performance.now();

let recentlySpawned = {};
let recentlyDespawned = {};

socket.onmessage = (event) => {
  const { animals, foods, activeClients, timeFromStart, reignitions } =
    JSON.parse(event.data);
  lastUpdate = performance.now();

  const animalCount = animals.length;
  const foodCount = foods.length;
  let animalCounts = {
    HERBIVORE: 0,
    CARNIVORE: 0,
    OMNIVORE: 0,
  };

  for (const animal of animals) {
    animalCounts[animal.foodType] += 1;
  }

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
  const animalPercent = fractionToPercentage(animalCount / total);
  const foodPercent = fractionToPercentage(foodCount / total);

  let animalCountsPercentage = {};

  if (animalCount === 0) {
    animalCountsPercentage = animalCounts;
  } else {
    for (let type in animalCounts) {
      animalCountsPercentage[type] = fractionToPercentage(
        animalCounts[type] / animalCount,
        1,
      );
    }
  }

  viewerCountHeader.innerText =
    activeClients === 1
      ? `${activeClients} viewer`
      : `${activeClients} viewers`;

  entityCategoryDisplays[0].innerText = `Animals: ${animalCount}\n${animalPercent}% `;
  entityCategoryDisplays[1].innerText = `Food: ${foodCount}\n${foodPercent}%`;

  animalFoodTypeDisplays[0].innerText = `Herbivores: ${animalCounts["HERBIVORE"]}\n${animalCountsPercentage["HERBIVORE"]}%`;
  animalFoodTypeDisplays[1].innerText = `Carnivores: ${animalCounts["CARNIVORE"]}\n${animalCountsPercentage["CARNIVORE"]}%`;
  animalFoodTypeDisplays[2].innerText = `Omnivores: ${animalCounts["OMNIVORE"]}\n${animalCountsPercentage["OMNIVORE"]}%`;

  elapsedTimeHeader.innerText = `Elapsed time: ${getTimeString(timeFromStart)}`;
  reignitionCounter.innerText = `Reignition count: ${reignitions}`;

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
    const color = appendAlpha(curr.color, opacity);
    const size = curr.size;

    let outline = getOutlineColor(curr);

    renderEntity({ x, y }, size, outline ?? color, outline);
  }

  for (const id in recentlyDespawned) {
    const { entity, opacity } = recentlyDespawned[id];

    if (opacity <= 0) {
      delete recentlyDespawned[id];
      continue;
    }

    const { x, y, size } = entity;
    const color = appendAlpha(entity.color, opacity);

    recentlyDespawned[id].opacity -= 0.03 * t;

    let outline;
    if (entity.type === "animal") {
      outline = appendAlpha(getOutlineColor(entity), opacity);
    }
    renderEntity({ x, y }, size, outline ?? color, outline);
  }

  requestAnimationFrame(render);
};
