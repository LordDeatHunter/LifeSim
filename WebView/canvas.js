// TODO: Get these values from the backend
const WORLD_WIDTH = 2048;
const WORLD_HEIGHT = 2048;

let camX = 0;
let camY = 0;
let isPanning = false;
let lastPanX = 0;
let lastPanY = 0;
let camScale = 0.5;

const clampCam = () => {
  const scaledWidth = WORLD_WIDTH * camScale;
  const scaledHeight = WORLD_HEIGHT * camScale;

  camX =
    scaledWidth <= canvas.width
      ? (canvas.width - scaledWidth) / 2
      : clamp(camX, canvas.width - scaledWidth, 0);

  camY =
    scaledHeight <= canvas.height
      ? (canvas.height - scaledHeight) / 2
      : clamp(camY, canvas.height - scaledHeight, 0);
};

document.addEventListener("DOMContentLoaded", () => {
  canvas.addEventListener("mousedown", (e) => {
    if (e.button !== 0) return;

    isPanning = true;
    lastPanX = e.clientX;
    lastPanY = e.clientY;
  });

  canvas.addEventListener("mousemove", (e) => {
    if (!isPanning) return;

    const dx = e.clientX - lastPanX;
    const dy = e.clientY - lastPanY;

    lastPanX = e.clientX;
    lastPanY = e.clientY;

    camX += dx;
    camY += dy;

    clampCam();
  });

  canvas.addEventListener("wheel", (e) => {
    e.preventDefault();

    const rect = canvas.getBoundingClientRect();
    const mx = e.clientX - rect.left;
    const my = e.clientY - rect.top;

    const worldX = (mx - camX) / camScale;
    const worldY = (my - camY) / camScale;

    const zoomFactor = 1 + e.deltaY * -0.002;
    camScale = clamp(camScale * zoomFactor, 0.5, 4);

    camX = mx - worldX * camScale;
    camY = my - worldY * camScale;

    clampCam();
  });
});

document.addEventListener("mouseup", () => (isPanning = false));
