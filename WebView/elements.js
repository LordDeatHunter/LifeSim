const reigniteLife = () => {
  fetch("http://localhost:5000/api/reignite_life");
};

let canvas;
let ctx;
let reigniteLifeButton;
let viewerCountHeader;
let elapsedTimeHeader;
let animalFoodTypeDisplays;
let entityCategoryDisplays;

document.addEventListener("DOMContentLoaded", () => {
  canvas = document.getElementById("view");
  ctx = canvas.getContext("2d");
  viewerCountHeader = document.getElementById("viewer-count-text");
  elapsedTimeHeader = document.getElementById("elapsed-time-display");
  animalFoodTypeDisplays = document.querySelectorAll(".animal-type-display");
  entityCategoryDisplays = document.querySelectorAll(
    ".entity-category-display",
  );

  reigniteLifeButton = document.getElementById("reignite-life-button");
  reigniteLifeButton.addEventListener("click", reigniteLife);

  requestAnimationFrame(render);
});
