let socket;
const webURL = "ws://localhost:5000/ws";

const lerpDuration = 300;
const lerp = (a, b, t) => a + (b - a) * t;

let prevEntities;
let entities;
let lastUpdate;

let recentlySpawned;
let recentlyDespawned;

const resetState = () => {
  prevEntities = {};
  entities = {
    animal: {},
    food: {},
  };
  lastUpdate = performance.now();

  recentlySpawned = {
    animal: {},
    food: {},
  };
  recentlyDespawned = {
    animal: {},
    food: {},
  };
};

const applyDiff = (cache, diffs, type) => {
  const { added = {}, updated = {}, removed = [] } = diffs;

  Object.entries(added).forEach(([id, dto]) => {
    cache[type][id] = dto;
    recentlySpawned[type][id] = 0;
  });

  Object.entries(updated)
    .filter(([id]) => cache[type][id])
    .forEach(([id, changes]) => Object.assign(cache[type][id], changes));

  removed.forEach((id) => {
    if (cache[type][id]) {
      recentlyDespawned[type][id] = { entity: cache[type][id], opacity: 1 };
      delete cache[type][id];
      delete recentlySpawned[type][id];
    }
  });
};

let createConnection = () => {
  resetState();
  socket = new WebSocket(webURL);

  socket.onmessage = (event) => {
    const { animals, foods, activeClients, timeFromStart, reignitions } =
      JSON.parse(event.data);
    lastUpdate = performance.now();

    prevEntities = JSON.parse(JSON.stringify(entities));

    applyDiff(entities, animals, "animal");
    applyDiff(entities, foods, "food");

    let animalCounts = {
      HERBIVORE: 0,
      CARNIVORE: 0,
      OMNIVORE: 0,
    };

    for (const animal of Object.values(entities["animal"])) {
      if (animal.predationInclanation <= 0.01) {
        animal.foodType = "HERBIVORE";
      } else if (animal.predationInclanation <= 0.66) {
        animal.foodType = "OMNIVORE";
      } else {
        animal.foodType = "CARNIVORE";
      }
      animalCounts[animal.foodType] += 1;
    }

    const animalCount = Object.keys(entities["animal"]).length;
    const foodCount = Object.keys(entities["food"]).length;

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

    elapsedTimeHeader.innerText = `Elapsed time: ${getTimeString(
      timeFromStart,
    )}`;
    reignitionCounter.innerText = `Reignition count: ${reignitions}`;

    reigniteLifeButton.disabled = animalCount > 0;
  };
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

  for (const type of ["animal", "food"]) {
    for (const id in entities[type]) {
      const curr = entities[type][id];
      const prev = id in prevEntities[type] ? prevEntities[type][id] : curr;

      let opacity = 1;

      if (recentlySpawned[type][id] !== undefined) {
        opacity = recentlySpawned[type][id];
        recentlySpawned[type][id] += 0.03 * t;
        if (recentlySpawned[type][id] > 1) {
          delete recentlySpawned[type][id];
        }
      }

      const x = lerp(prev.x, curr.x, t);
      const y = lerp(prev.y, curr.y, t);
      const color = appendAlpha(curr.color, opacity);
      const size = curr.size;

      let outline;
      if (type === "animal") {
        outline = appendAlpha(getOutlineColor(curr, type), opacity);
      }

      renderEntity({ x, y }, size, outline ?? color, outline);
    }
  }

  for (const type of ["animal", "food"]) {
    for (const id in recentlyDespawned[type]) {
      const { entity, opacity } = recentlyDespawned[type][id];

      if (opacity <= 0) {
        delete recentlyDespawned[type][id];
        continue;
      }

      const { x, y, size } = entity;
      const color = appendAlpha(entity.color, opacity);

      recentlyDespawned[type][id].opacity -= 0.03 * t;

      let outline;
      if (type === "animal") {
        outline = appendAlpha(getOutlineColor(entity, type), opacity);
      }
      renderEntity({ x, y }, size, outline ?? color, outline);
    }
  }

  requestAnimationFrame(render);
};

const MAX_TILT_DEG = 30;
const PERSPECTIVE = 300;

document.addEventListener("DOMContentLoaded", () => {
  document.querySelectorAll(".logo").forEach((logo) => {
    const el = logo.querySelector("span");

    logo.style.perspective = `${PERSPECTIVE}px`;

    logo.addEventListener("mousemove", (e) => {
      const { left, top, width, height } = logo.getBoundingClientRect();
      const normalizedMousePos = {
        x: (e.clientX - left) / width - 0.5,
        y: (e.clientY - top) / height - 0.5,
      };
      const rotation = {
        x: -normalizedMousePos.y * MAX_TILT_DEG * 2,
        y: normalizedMousePos.x * MAX_TILT_DEG * 2,
      };

      el.style.transform = `
        perspective(${PERSPECTIVE}px)
        rotateX(${rotation.x}deg)
        rotateY(${rotation.y}deg)
      `;
    });

    logo.addEventListener("mouseleave", () => {
      el.style.transform = `perspective(${PERSPECTIVE}px) rotateX(0deg) rotateY(0deg)`;
    });
  });
});

createConnection();

document.addEventListener("visibilitychange", () => {
  if (document.hidden) {
    socket.close();
  } else {
    createConnection();
  }
});
