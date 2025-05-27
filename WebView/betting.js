const API_ENDPOINT = "http://localhost:5000/api";

let balanceDisplay;
let betAmountElement;
let betStatusDiv;
let betStatusList;
let balanceLeaderboardDiv;
let betsLeaderboardDiv;

const playCoinSound = () => {
  const audio = new Audio("/coins.wav");
  audio.play().catch((error) => {
    console.error("Error playing sound:", error);
  });
};

const handleFinishedBet = (div, { betType, amount, status }) => {
  const statusClass = `bet-${status.toLowerCase()}`;
  div.innerHTML = `You bet ${amount} on ${betType}. <span class="${statusClass}">Outcome: ${status}</span>.`;
  return div;
};

const createLeaderboardEntry = (username, score) => {
  const entry = document.createElement("div");
  entry.classList.add("leaderboard-entry");

  const usernameSpan = document.createElement("span");
  usernameSpan.classList.add("leaderboard-username");
  usernameSpan.textContent = username;

  const scoreSpan = document.createElement("span");
  scoreSpan.classList.add("leaderboard-score");
  scoreSpan.textContent = score;

  entry.appendChild(usernameSpan);
  entry.appendChild(scoreSpan);

  return entry;
};

const createLeaderboards = async () =>
  fetch(`${API_ENDPOINT}/leaderboards`, {
    method: "GET",
    headers: {
      "Content-Type": "application/json",
    },
    credentials: "include",
  })
    .then((response) => response.json())
    .then((data) => {
      const { topBalances, topBets } = data;
      balanceLeaderboardDiv.innerHTML = "";
      betsLeaderboardDiv.innerHTML = "";

      topBalances.forEach((item) => {
        const entry = createLeaderboardEntry(item.clientId, item.score);
        balanceLeaderboardDiv.appendChild(entry);
      });

      topBets.forEach((item) => {
        const entry = createLeaderboardEntry(item.clientId, item.score);
        betsLeaderboardDiv.appendChild(entry);
      });
    })
    .catch((error) => {
      console.error("Error fetching leaderboards:", error);
    });

const createBetStatusElement = ({ id, betType, amount, status, expiresAt }) => {
  const betMsg = document.createElement("div");
  betMsg.classList.add("bet-status");
  betMsg.classList.add(`bet-${betType}`);
  betMsg.id = `bet-${id}`;

  if (status !== "Pending") {
    handleFinishedBet(betMsg, { betType, amount, status });
    return betMsg;
  }

  const remainingSeconds = Math.ceil((new Date(expiresAt) - new Date()) / 1000);
  betMsg.textContent = `You bet ${amount} on ${betType}. Waiting ${remainingSeconds}s...`;

  let time = remainingSeconds;
  const interval = setInterval(() => {
    betMsg.textContent = `You bet ${amount} on ${betType}. Waiting ${--time}s...`;
    if (time < 0) {
      updateBalanceDisplay();
      updateBet(id);
      clearInterval(interval);
    }
  }, 1000);
  return betMsg;
};

const getBalance = async () =>
  fetch(`${API_ENDPOINT}/balance`, {
    method: "GET",
    headers: {
      "Content-Type": "application/json",
    },
    credentials: "include",
  });

const setBalanceDisplay = (balance) => {
  if (balanceDisplay) {
    balanceDisplay.textContent = `🪙${balance}`;
  } else {
    console.error("Balance display element not found.");
  }
};

const placeBet = async (betType) => {
  const amount = parseInt(betAmountElement.value, 10);
  if (isNaN(amount) || amount <= 0 || !Number.isSafeInteger(amount)) {
    console.error("Invalid bet amount.");
    return;
  }

  fetch(`${API_ENDPOINT}/place-bet`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify({ betType, amount }),
    credentials: "include",
  })
    .then((response) => response.json())
    .then((json) => {
      const { balance, bet } = json;

      setBalanceDisplay(balance);

      const betMsg = createBetStatusElement(bet);

      betStatusList.insertBefore(betMsg, betStatusList.firstChild);
      betAmountElement.value = "";
    })
    .catch((error) => {
      console.error("Error placing bet:", error);
    });
};

const updateBalanceDisplay = () => {
  getBalance()
    .then((response) => response.json())
    .then((data) => data.balance)
    .then(setBalanceDisplay)
    .catch((error) => {
      console.error("Error fetching balance:", error);
    });
};

const updateBet = (id) => {
  fetch(`${API_ENDPOINT}/bet/${id}`, {
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
        if (status === "Won") {
          playCoinSound();
        }
        handleFinishedBet(betMsg, { betType, amount, status });
      } else {
        console.error("Bet message element not found.");
      }
    })
    .catch((error) => {
      console.error("Error updating bet:", error);
    });
};

document.addEventListener("DOMContentLoaded", () => {
  balanceDisplay = document.getElementById("balance-display");
  betAmountElement = document.getElementById("bet-amount");
  betStatusDiv = document.getElementById("bet-statuses");
  betStatusList = document.getElementById("bet-status-list");
  balanceLeaderboardDiv = document.getElementById("balance-leaderboard");
  betsLeaderboardDiv = document.getElementById("bets-leaderboard");

  updateBalanceDisplay();
  createLeaderboards();
  setInterval(createLeaderboards, 3 * 60 * 1000);

  document.getElementById("bet-increase").onclick = () => {
    placeBet("increase");
  };

  document.getElementById("bet-decrease").onclick = () => {
    placeBet("decrease");
  };

  fetch(`${API_ENDPOINT}/bets`, {
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
