let currencyDisplay;
let betAmountElement;
let betStatusDiv;
let betStatusList;

const getCurrency = async () =>
  fetch("http://localhost:5000/api/currency", {
    method: "GET",
    headers: {
      "Content-Type": "application/json",
    },
    credentials: "include",
  });

const setCurrencyDisplay = (currency) => {
  if (currencyDisplay) {
    currencyDisplay.textContent = `🪙${currency}`;
  } else {
    console.error("Currency display element not found.");
  }
};

const placeBet = async (betType) => {
  const amount = parseInt(betAmountElement.value, 10);
  if (isNaN(amount) || amount <= 0 || !Number.isSafeInteger(amount)) {
    console.error("Invalid bet amount.");
    return;
  }

  fetch("http://localhost:5000/api/bet", {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify({ betType, amount }),
    credentials: "include",
  })
    .then((response) => response.json())
    .then((data) => data.currency)
    .then((currency) => {
      setCurrencyDisplay(currency);
      const betMsg = document.createElement("div");
      betMsg.classList.add("bet-status");
      betMsg.classList.add(`bet-${betType}`);
      betMsg.textContent = `You bet ${amount} on ${betType}. Waiting 30s...`;
      betStatusList.insertBefore(betMsg, betStatusList.firstChild);
      betAmountElement.value = "";

      let time = 30;
      const interval = setInterval(() => {
        betMsg.textContent = `You bet ${amount} on ${betType}. Waiting ${--time}s...`;
        if (time < 0) {
          betMsg.remove();
          updateCurrencyDisplay();
          clearInterval(interval);
        }
      }, 1000);
    })
    .catch((error) => {
      console.error("Error placing bet:", error);
    });
};

const updateCurrencyDisplay = () => {
  getCurrency()
    .then((response) => response.json())
    .then((data) => data.currency)
    .then(setCurrencyDisplay)
    .catch((error) => {
      console.error("Error fetching currency:", error);
    });
};

document.addEventListener("DOMContentLoaded", () => {
  currencyDisplay = document.getElementById("currency-display");
  betAmountElement = document.getElementById("bet-amount");
  betStatusDiv = document.getElementById("bet-statuses");
  betStatusList = document.getElementById("bet-status-list");

  updateCurrencyDisplay();

  document.getElementById("bet-increase").onclick = () => {
    placeBet("increase");
  };

  document.getElementById("bet-decrease").onclick = () => {
    placeBet("decrease");
  };
});
