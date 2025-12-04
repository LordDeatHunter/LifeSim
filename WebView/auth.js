let currentUser = null;

let loginPanel;
let userPanel;
let discordLoginButton;
let logoutButton;
let userAvatar;
let userName;

const initAuth = () => {
  loginPanel = document.getElementById("login-panel");
  userPanel = document.getElementById("user-panel");
  discordLoginButton = document.getElementById("discord-login-button");
  logoutButton = document.getElementById("logout-button");
  userAvatar = document.getElementById("user-avatar");
  userName = document.getElementById("user-name");

  discordLoginButton.addEventListener("click", handleDiscordLogin);
  logoutButton.addEventListener("click", handleLogout);

  checkAuthStatus();
};

const checkAuthStatus = async () => {
  try {
    const response = await fetch(`${API_ENDPOINT}/auth/me`, {
      credentials: "include",
    });

    if (response.ok) {
      const user = await response.json();
      setAuthenticatedUser(user);
    } else {
      setUnauthenticated();
    }
  } catch (error) {
    console.error("Error checking auth status:", error);
    setUnauthenticated();
  }
};

const handleDiscordLogin = async () => {
  try {
    const response = await fetch(`${API_ENDPOINT}/auth/discord`, {
      credentials: "include",
    });

    if (response.ok) {
      const data = await response.json();
      window.location.href = data.url;
    } else {
      console.error("Failed to get Discord login URL");
    }
  } catch (error) {
    console.error("Error initiating Discord login:", error);
  }
};

const handleLogout = async () => {
  try {
    await fetch(`${API_ENDPOINT}/auth/logout`, {
      method: "POST",
      credentials: "include",
    });

    currentUser = null;
    setUnauthenticated();

    // Refresh the page to reset state
    window.location.reload();
  } catch (error) {
    console.error("Error logging out:", error);
  }
};

const setAuthenticatedUser = (user) => {
  currentUser = user;
  loginPanel.style.display = "none";
  userPanel.style.display = "flex";

  userName.innerText = user.name;

  if (user.discord && user.discord.avatar) {
    const avatarUrl = `https://cdn.discordapp.com/avatars/${user.discord.id}/${user.discord.avatar}.png?size=64`;
    userAvatar.src = avatarUrl;
    userAvatar.style.display = "inline-block";
  } else {
    userAvatar.style.display = "none";
  }

  const bettingMenu = document.getElementById("betting-menu");
  const rightInfo = document.getElementById("right-info");
  if (bettingMenu) bettingMenu.style.display = "block";
  if (rightInfo) rightInfo.style.display = "flex";

  updateBalanceDisplay();
  fetchUserBets();
};

const setUnauthenticated = () => {
  currentUser = null;
  loginPanel.style.display = "block";
  userPanel.style.display = "none";

 const bettingMenu = document.getElementById("betting-menu");
  const rightInfo = document.getElementById("right-info");
  if (bettingMenu) bettingMenu.style.display = "none";
  if (rightInfo) rightInfo.style.display = "none";
};
if (document.readyState === "loading") {
  document.addEventListener("DOMContentLoaded", initAuth);
} else {
  initAuth();
}
