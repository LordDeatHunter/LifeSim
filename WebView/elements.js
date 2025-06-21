const reigniteLife = () => {
  fetch(`${API_ENDPOINT}/reignite-life`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify({ chaos: reignitionChaosSlider.value }),
  });
};

let canvas;
let ctx;
let reigniteLifeButton;
let viewerCountHeader;
let elapsedTimeHeader;
let animalFoodTypeDisplays;
let entityCategoryDisplays;
let reignitionCounter;
let reignitionChaosSlider;
let reignitionChaosLabel;

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

  reignitionChaosSlider = document.getElementById("reignition-chaos-slider");
  reignitionChaosLabel = document.getElementById("reignition-chaos-label");

  reignitionChaosSlider.addEventListener("input", () => {
    const value = parseFloat(reignitionChaosSlider.value).toFixed(2);
    reignitionChaosLabel.innerText = `💥 Chaos: ${value}`;
  });

  reignitionCounter = document.getElementById("reignition-counter");

  reigniteLifeButton = document.getElementById("reignite-life-button");
  reigniteLifeButton.addEventListener("click", reigniteLife);

  requestAnimationFrame(render);
});
