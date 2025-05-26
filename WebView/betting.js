let currencyDisplay;
let betAmountElement;
let betStatusDiv;
let betStatusList;

const createBetStatusElement = ({ id, betType, amount, status, expiresAt }) => {
  const betMsg = document.createElement("div");
  betMsg.classList.add("bet-status");
  betMsg.classList.add(`bet-${betType}`);
  betMsg.classList.add(`bet-${status.toLowerCase()}`);
  betMsg.id = `bet-${id}`;

  if (status !== "Pending") {
    betMsg.textContent = `You bet ${amount} on ${betType}. Outcome: ${status}.`;
    return betMsg;
  }

  const remainingSeconds = Math.ceil((new Date(expiresAt) - new Date()) / 1000);
  betMsg.textContent = `You bet ${amount} on ${betType}. Waiting ${remainingSeconds}s...`;

  let time = remainingSeconds;
  const interval = setInterval(() => {
    betMsg.textContent = `You bet ${amount} on ${betType}. Waiting ${--time}s...`;
    if (time < 0) {
      updateCurrencyDisplay();
      updateBet(id);
      clearInterval(interval);
    }
  }, 1000);
  return betMsg;
};

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

  fetch("http://localhost:5000/api/place-bet", {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify({ betType, amount }),
    credentials: "include",
  })
    .then((response) => response.json())
    .then((json) => {
      const { currency, bet } = json;

      setCurrencyDisplay(currency);

      const betMsg = createBetStatusElement(bet);

      betStatusList.insertBefore(betMsg, betStatusList.firstChild);
      betAmountElement.value = "";
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

const updateBet = (id) => {
  fetch(`http://localhost:5000/api/bet/${id}`, {
    method: "GET",
    headers: {
      "Content-Type": "application/json",
    },
    credentials: "include",
  })
    .then((response) => response.json())
    .then((data) => {
      const { id, amount, betType, status } = data;

      const betMsg = document.getElementById(`bet-${id}`);
      if (betMsg) {
        betMsg.textContent = `You bet ${amount} on ${betType}. Outcome: ${status}.`;
        betMsg.classList.add(`bet-${status.toLowerCase()}`);
      } else {
        console.error("Bet message element not found.");
      }
    })
    .catch((error) => {
      console.error("Error updating bet:", error);
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

  fetch("http://localhost:5000/api/bets", {
    method: "GET",
    headers: {
      "Content-Type": "application/json",
    },
    credentials: "include",
  })
    .then((response) => response.json())
    .then((data) => {
      data.forEach((item) => {
        const betMsg = createBetStatusElement(item);

        betStatusList.insertBefore(betMsg, betStatusList.firstChild);
      });
    })
    .catch((error) => {
      console.error("Error fetching bets:", error);
    });
});
