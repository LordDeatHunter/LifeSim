const reigniteLife = () => {
  fetch(`${API_ENDPOINT}/reignite-life`, { method: "POST" });
};

let canvas;
let ctx;
let reigniteLifeButton;
let viewerCountHeader;
let elapsedTimeHeader;
let animalFoodTypeDisplays;
let entityCategoryDisplays;
let reignitionCounter;

let camX = 0;
let camY = 0;
let isPanning = false;
let lastPanX = 0;
let lastPanY = 0;

document.addEventListener("DOMContentLoaded", () => {
  canvas = document.getElementById("view");
  ctx = canvas.getContext("2d");
  viewerCountHeader = document.getElementById("viewer-count-text");
  elapsedTimeHeader = document.getElementById("elapsed-time-display");
  animalFoodTypeDisplays = document.querySelectorAll(".animal-type-display");
  entityCategoryDisplays = document.querySelectorAll(
    ".entity-category-display",
  );
  reignitionCounter = document.getElementById("reignition-counter");

  reigniteLifeButton = document.getElementById("reignite-life-button");
  reigniteLifeButton.addEventListener("click", reigniteLife);

  canvas.addEventListener("mousedown", (e) => {
    isPanning = true;
    lastPanX = e.clientX;
    lastPanY = e.clientY;
  });

  canvas.addEventListener("mousemove", (e) => {
    if (!isPanning) return;

    const dx = e.clientX - lastPanX;
    const dy = e.clientY - lastPanY;
    camX = clamp(camX + dx, -canvas.width, 0);
    camY = clamp(camY + dy, -canvas.height, 0);
    lastPanX = e.clientX;
    lastPanY = e.clientY;
  });

  canvas.addEventListener("mouseup", () => {
    isPanning = false;
  });

  canvas.addEventListener("mouseleave", () => {
    isPanning = false;
  });

  requestAnimationFrame(render);
});
