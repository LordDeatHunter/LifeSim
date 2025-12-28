let populationChart = null;
let statisticsHistory = [];

const MAX_DATA_POINTS = 120;
const ONE_MINUTE_MS = 60000;

const initializeChart = () => {
  const ctx = document.getElementById('population-chart');
  if (!ctx) {
    console.error('Chart canvas not found');
    return;
  }

  populationChart = new Chart(ctx, {
    type: 'line',
    data: {
      labels: [],
      datasets: [
        {
          label: 'Animals',
          data: [],
          borderColor: '#4CAF50',
          backgroundColor: 'rgba(76, 175, 80, 0.1)',
          borderWidth: 2,
          pointRadius: 0,
          pointHoverRadius: 4,
          tension: 0.4,
          fill: true
        },
        {
          label: 'Food',
          data: [],
          borderColor: '#FFC107',
          backgroundColor: 'rgba(255, 193, 7, 0.1)',
          borderWidth: 2,
          pointRadius: 0,
          pointHoverRadius: 4,
          tension: 0.4,
          fill: true
        }
      ]
    },
    options: {
      responsive: true,
      maintainAspectRatio: true,
      interaction: {
        mode: 'index',
        intersect: false,
      },
      scales: {
        x: {
          type: 'category',
          ticks: {
            color: '#cecece',
            maxRotation: 0,
            autoSkipPadding: 20,
            callback: (value, index) => index % 10 === 0 ? this.getLabelForValue(value) : ''
          },
          grid: {
            color: '#333333'
          },
          title: {
            display: true,
            text: 'Time',
            color: '#cecece'
          }
        },
        y: {
          beginAtZero: true,
          ticks: {
            color: '#cecece',
            precision: 0
          },
          grid: {
            color: '#333333'
          },
          title: {
            display: true,
            text: 'Count',
            color: '#cecece'
          }
        }
      },
      plugins: {
        legend: {
          labels: {
            color: '#cecece',
            font: {
              family: "'Share Tech Mono', monospace"
            }
          }
        },
        tooltip: {
          backgroundColor: 'rgba(30, 30, 30, 0.95)',
          titleColor: '#cecece',
          bodyColor: '#cecece',
          borderColor: '#cecece',
          borderWidth: 1,
          titleFont: {
            family: "'Share Tech Mono', monospace"
          },
          bodyFont: {
            family: "'Share Tech Mono', monospace"
          }
        }
      }
    }
  });
};

const updateChart = (statistics) => {
  if (!populationChart) return;

  if (statistics && statistics.length > 0) {
    statisticsHistory.push(...statistics);
  }

  const oneMinuteAgo = Date.now() - ONE_MINUTE_MS;
  statisticsHistory = statisticsHistory.filter(
    s => new Date(s.timestamp).getTime() > oneMinuteAgo
  );

  if (statisticsHistory.length > MAX_DATA_POINTS) {
    statisticsHistory = statisticsHistory.slice(-MAX_DATA_POINTS);
  }

  const labels = statisticsHistory.map(s => {
    const date = new Date(s.timestamp);
    const hours = date.getHours().toString().padStart(2, '0');
    const minutes = date.getMinutes().toString().padStart(2, '0');
    const seconds = date.getSeconds().toString().padStart(2, '0');
    return `${hours}:${minutes}:${seconds}`;
  });
  const animalData = statisticsHistory.map(s => s.animalCount);
  const foodData = statisticsHistory.map(s => s.foodCount);

  populationChart.data.labels = labels;
  populationChart.data.datasets[0].data = animalData;
  populationChart.data.datasets[1].data = foodData;

  populationChart.update('none');
};

const resetChart = () => {
  statisticsHistory = [];
  if (populationChart) {
    populationChart.data.labels = [];
    populationChart.data.datasets[0].data = [];
    populationChart.data.datasets[1].data = [];
    populationChart.update();
  }
};

document.addEventListener('DOMContentLoaded', () => {
  initializeChart();
});

