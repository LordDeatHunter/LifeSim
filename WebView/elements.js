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

document.addEventListener("DOMContentLoaded", () => {
  canvas = document.getElementById("view");
  ctx = canvas.getContext("2d");
  ctx.imageSmoothingEnabled = false;
  viewerCountHeader = document.getElementById("viewer-count-text");
  elapsedTimeHeader = document.getElementById("elapsed-time-display");
  animalFoodTypeDisplays = document.querySelectorAll(".animal-type-display");
  entityCategoryDisplays = document.querySelectorAll(
    ".entity-category-display",
  );
  reignitionCounter = document.getElementById("reignition-counter");

  reigniteLifeButton = document.getElementById("reignite-life-button");
  reigniteLifeButton.addEventListener("click", reigniteLife);

  requestAnimationFrame(render);
});
