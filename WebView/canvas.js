let camX = 0;
let camY = 0;
let isPanning = false;
let lastPanX = 0;
let lastPanY = 0;

document.addEventListener("DOMContentLoaded", () => {
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
});