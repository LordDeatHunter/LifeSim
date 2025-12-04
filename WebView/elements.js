const reigniteLife = () => {
  fetch(`${API_ENDPOINT}/reignite-life`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify({ chaos: reignitionChaosSlider.value }),
    credentials: "include",
  }).then(() => updateBalanceDisplay());
};

let canvas;
let ctx;
let reigniteLifeButton;
let viewerCountHeader;
let totalSimulationTimeHeader;
let currentLifeDurationHeader;
let longestLifeDurationHeader;
let animalFoodTypeDisplays;
let entityCategoryDisplays;
let reignitionCounter;
let reignitionChaosSlider;
let reignitionChaosLabel;
let reignitionCost;

const calculateReignitionCost = (value) => Math.floor(25 + 975 * value);

document.addEventListener("DOMContentLoaded", () => {
  canvas = document.getElementById("view");
  ctx = canvas.getContext("2d");
  ctx.imageSmoothingEnabled = false;
  viewerCountHeader = document.getElementById("viewer-count-text");
  totalSimulationTimeHeader = document.getElementById("total-simulation-time-display");
  currentLifeDurationHeader = document.getElementById("current-life-duration-display");
  longestLifeDurationHeader = document.getElementById("longest-life-duration-display");
  animalFoodTypeDisplays = document.querySelectorAll(".animal-type-display");
  entityCategoryDisplays = document.querySelectorAll(
    ".entity-category-display",
  );

  reignitionChaosSlider = document.getElementById("reignition-chaos-slider");
  reignitionChaosLabel = document.getElementById("reignition-chaos-label");
  reignitionCost = document.getElementById("reignition-cost");
  reignitionCost.innerText = calculateReignitionCost(
    reignitionChaosSlider.value,
  ).toString();

  reignitionChaosSlider.addEventListener("input", () => {
    const value = parseFloat(reignitionChaosSlider.value).toFixed(2);
    reignitionChaosLabel.innerText = `💥 Chaos: ${value}`;
    reignitionCost.innerText = calculateReignitionCost(value).toString();
  });

  reignitionCounter = document.getElementById("reignition-counter");

  reigniteLifeButton = document.getElementById("reignite-life-button");
  reigniteLifeButton.addEventListener("click", reigniteLife);

  requestAnimationFrame(render);
});
