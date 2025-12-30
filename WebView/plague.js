let plaguePinPosition = null;
let plagueRadius = 50;

// Elements
let plagueSizeSlider;
let plagueSizeLabel;
let plagueCostDisplay;
let startPlagueButton;

const calculatePlagueCost = (radius) => {
  const area = Math.PI * radius * radius;
  return Math.floor(area * 0.5 * 0.01);
};

const updatePlagueCost = () => {
  const cost = calculatePlagueCost(plagueRadius);
  if (plagueCostDisplay) {
    plagueCostDisplay.innerText = cost;
  }
};

const canvasToWorldCoords = (canvasX, canvasY) => {
  const rect = canvas.getBoundingClientRect();
  const x = canvasX - rect.left;
  const y = canvasY - rect.top;

  const worldX = (x - camX) / camScale;
  const worldY = (y - camY) / camScale;

  return { x: worldX, y: worldY };
};

const worldToCanvasCoords = (worldX, worldY) => {
  return {
    x: worldX * camScale + camX,
    y: worldY * camScale + camY
  };
};

const renderPlaguePreview = () => {
  if (!plaguePinPosition) return;

  const canvasPos = worldToCanvasCoords(plaguePinPosition.x, plaguePinPosition.y);
  const radiusOnCanvas = plagueRadius * camScale;

  ctx.save();
  ctx.globalAlpha = 0.3;
  ctx.fillStyle = '#8B008B';
  ctx.beginPath();
  ctx.arc(canvasPos.x, canvasPos.y, radiusOnCanvas, 0, Math.PI * 2);
  ctx.fill();

  ctx.globalAlpha = 0.8;
  ctx.strokeStyle = '#8B008B';
  ctx.lineWidth = 2;
  ctx.stroke();

  ctx.globalAlpha = 1;
  ctx.fillStyle = '#FF00FF';
  ctx.beginPath();
  ctx.arc(canvasPos.x, canvasPos.y, 5, 0, Math.PI * 2);
  ctx.fill();

  ctx.restore();
};

const handleCanvasClickForPlague = (e) => {
  // Only handle right-click (button 2)
  if (e.button !== 2) return;

  e.preventDefault();

  const worldCoords = canvasToWorldCoords(e.clientX, e.clientY);

  worldCoords.x = clamp(worldCoords.x, 0, 2048);
  worldCoords.y = clamp(worldCoords.y, 0, 2048);

  plaguePinPosition = worldCoords;
};

const startPlague = async () => {
  if (!plaguePinPosition) {
    return;
  }

  const cost = calculatePlagueCost(plagueRadius);

  try {
    const response = await fetch(`${API_ENDPOINT}/start-plague`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      credentials: 'include',
      body: JSON.stringify({
        x: plaguePinPosition.x,
        y: plaguePinPosition.y,
        radius: plagueRadius
      })
    });

    const data = await response.json();

    if (response.ok) {
      plaguePinPosition = null;

      if (balanceAmountDisplay) {
        balanceAmountDisplay.innerText = data.balance;
      }
    }
  } catch (error) {
    console.error('Error starting plague:', error);
  }
};

document.addEventListener('DOMContentLoaded', () => {
  plagueSizeSlider = document.getElementById('plague-size-slider');
  plagueSizeLabel = document.getElementById('plague-size-label');
  plagueCostDisplay = document.getElementById('plague-cost');
  startPlagueButton = document.getElementById('start-plague-button');

  if (plagueSizeSlider) {
    plagueSizeSlider.addEventListener('input', (e) => {
      plagueRadius = parseFloat(e.target.value);
      plagueSizeLabel.innerText = `🦠 Size: ${plagueRadius}`;
      updatePlagueCost();
    });
  }

  if (startPlagueButton) {
    startPlagueButton.addEventListener('click', startPlague);
  }

  canvas.addEventListener('mousedown', handleCanvasClickForPlague);

  canvas.addEventListener('contextmenu', (e) => {
    e.preventDefault();
  });

  updatePlagueCost();
});

