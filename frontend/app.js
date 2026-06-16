const productionApiBaseUrl = "https://dragoes-api.onrender.com";

const storageKeys = {
    activeUser: "dragoes-active-user"
};

const state = {
    apiBaseUrl: getApiBaseUrl(),
    activeUser: safeParse(localStorage.getItem(storageKeys.activeUser)),
    currentQuestion: null,
    questionStartedAt: null,
    timerIntervalId: null
};

const elements = {
    authPanel: document.getElementById("auth-panel"),
    quizLayout: document.getElementById("quiz-layout"),
    loginTab: document.getElementById("login-tab"),
    registerTab: document.getElementById("register-tab"),
    loginForm: document.getElementById("login-form"),
    registerForm: document.getElementById("register-form"),
    loginUser: document.getElementById("login-user"),
    loginPassword: document.getElementById("login-password"),
    registerName: document.getElementById("register-name"),
    registerLogin: document.getElementById("register-login"),
    registerPassword: document.getElementById("register-password"),
    authStatus: document.getElementById("auth-status"),
    dragonImage: document.getElementById("dragon-image"),
    imageEmpty: document.getElementById("image-empty"),
    answerForm: document.getElementById("answer-form"),
    answerInput: document.getElementById("answer-input"),
    feedbackMessage: document.getElementById("feedback-message"),
    nextQuestion: document.getElementById("next-question"),
    refreshRanking: document.getElementById("refresh-ranking"),
    rankingList: document.getElementById("ranking-list"),
    rankingStatus: document.getElementById("ranking-status"),
    activeUserName: document.getElementById("active-user-name"),
    activeUserScore: document.getElementById("active-user-score"),
    timerDisplay: document.getElementById("timer-display"),
    logoutButton: document.getElementById("logout-button")
};

initialize();

function initialize() {
    elements.loginTab.addEventListener("click", () => showAuthMode("login"));
    elements.registerTab.addEventListener("click", () => showAuthMode("register"));
    elements.loginForm.addEventListener("submit", handleLogin);
    elements.registerForm.addEventListener("submit", handleRegister);
    elements.answerForm.addEventListener("submit", handleAnswer);
    elements.nextQuestion.addEventListener("click", fetchNextQuestion);
    elements.refreshRanking.addEventListener("click", refreshRanking);
    elements.logoutButton.addEventListener("click", logout);

    renderSession();

    if (state.activeUser) {
        refreshRanking();
        fetchNextQuestion();
    }
}

async function handleLogin(event) {
    event.preventDefault();

    const login = elements.loginUser.value.trim().toLowerCase();
    const senha = elements.loginPassword.value.trim();

    if (!login || !senha) {
        setAuthStatus("Preencha login e senha.", "error");
        return;
    }

    try {
        setAuthStatus("Entrando...", "");
        const user = await apiRequest("/usuarios/login", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ login, senha })
        });

        startSession(user);
    } catch (error) {
        setAuthStatus(error.message, "error");
    }
}

async function handleRegister(event) {
    event.preventDefault();

    const nome = elements.registerName.value.trim();
    const login = elements.registerLogin.value.trim().toLowerCase();
    const senha = elements.registerPassword.value.trim();

    if (!nome || !login || !senha) {
        setAuthStatus("Preencha nome, login e senha.", "error");
        return;
    }

    if (senha.length < 6) {
        setAuthStatus("A senha deve ter pelo menos 6 caracteres.", "error");
        return;
    }

    try {
        setAuthStatus("Cadastrando...", "");
        const user = await apiRequest("/usuarios", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ nome, login, senha })
        });

        startSession(user);
    } catch (error) {
        setAuthStatus(error.message, "error");
    }
}

function startSession(user) {
    state.activeUser = user;
    localStorage.setItem(storageKeys.activeUser, JSON.stringify(user));
    elements.loginForm.reset();
    elements.registerForm.reset();
    setAuthStatus("", "");
    renderSession();
    refreshRanking();
    fetchNextQuestion();
}

function logout() {
    state.activeUser = null;
    state.currentQuestion = null;
    stopTimer();
    localStorage.removeItem(storageKeys.activeUser);
    renderSession();
}

function renderSession() {
    if (!state.activeUser) {
        elements.authPanel.classList.remove("hidden");
        elements.quizLayout.classList.add("hidden");
        return;
    }

    elements.authPanel.classList.add("hidden");
    elements.quizLayout.classList.remove("hidden");
    renderActiveUser();
}

function renderActiveUser() {
    elements.activeUserName.textContent = state.activeUser.nome;
    elements.activeUserScore.textContent = `${state.activeUser.pontuacaoTotal ?? 0} pontos | ${state.activeUser.totalAcertos ?? 0} acertos`;
}

async function fetchNextQuestion() {
    if (!state.activeUser) {
        return;
    }

    try {
        setFeedback("Carregando proximo dragao...", "");
        const question = await apiRequest("/quiz/pergunta");
        state.currentQuestion = question;

        elements.dragonImage.src = question.imagemUrl;
        elements.dragonImage.classList.remove("hidden");
        elements.imageEmpty.classList.add("hidden");
        elements.answerInput.value = "";
        elements.answerInput.disabled = false;
        elements.answerInput.focus();

        setFeedback("Olhe a imagem e digite o nome do dragao.", "");
        startTimer();
    } catch (error) {
        state.currentQuestion = null;
        elements.dragonImage.removeAttribute("src");
        elements.dragonImage.classList.add("hidden");
        elements.imageEmpty.classList.remove("hidden");
        setFeedback(error.message, "error");
        stopTimer();
    }
}

async function handleAnswer(event) {
    event.preventDefault();

    if (!state.activeUser) {
        setFeedback("Faca login para responder.", "error");
        return;
    }

    if (!state.currentQuestion) {
        setFeedback("Carregue um dragao antes de responder.", "error");
        return;
    }

    const respostaInformada = elements.answerInput.value.trim();
    if (!respostaInformada) {
        setFeedback("Digite o nome do dragao.", "error");
        return;
    }

    const tempoRespostaSegundos = Math.max(0, Math.round(getElapsedSeconds()));

    try {
        const result = await apiRequest("/quiz/responder", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
                usuarioId: state.activeUser.id,
                dragaoId: state.currentQuestion.id,
                respostaInformada,
                tempoRespostaSegundos
            })
        });

        stopTimer();
        elements.answerInput.disabled = true;
        state.activeUser = {
            ...state.activeUser,
            nome: result.nome,
            totalAcertos: result.totalAcertos,
            pontuacaoTotal: result.pontuacaoTotal
        };
        localStorage.setItem(storageKeys.activeUser, JSON.stringify(state.activeUser));
        renderActiveUser();

        const message = result.acertou
            ? `Acertou! +${result.pontosGanhos} pontos em ${tempoRespostaSegundos}s.`
            : `Errou. Voce respondeu em ${tempoRespostaSegundos}s e nao pontuou.`;

        setFeedback(message, result.acertou ? "success" : "error");
        refreshRanking();
    } catch (error) {
        setFeedback(error.message, "error");
    }
}

async function refreshRanking() {
    try {
        const ranking = await apiRequest("/quiz/ranking");
        renderRanking(ranking);
    } catch (error) {
        elements.rankingList.innerHTML = "";
        setRankingStatus(error.message, "error");
    }
}

function renderRanking(ranking) {
    elements.rankingList.innerHTML = "";

    if (!ranking.length) {
        setRankingStatus("O ranking aparece depois dos primeiros jogadores.", "");
        return;
    }

    setRankingStatus("", "");
    ranking.forEach((item, index) => {
        const entry = document.createElement("li");
        entry.className = "ranking-item";
        entry.innerHTML = `
            <span class="ranking-position">${index + 1}</span>
            <div>
                <strong>${escapeHtml(item.nome)}</strong>
                <small>${item.pontuacaoTotal} pontos | ${item.totalAcertos} acertos</small>
            </div>
        `;
        elements.rankingList.appendChild(entry);
    });
}

function showAuthMode(mode) {
    const isLogin = mode === "login";

    elements.loginTab.classList.toggle("active", isLogin);
    elements.registerTab.classList.toggle("active", !isLogin);
    elements.loginForm.classList.toggle("hidden", !isLogin);
    elements.registerForm.classList.toggle("hidden", isLogin);
    setAuthStatus("", "");
}

function startTimer() {
    stopTimer();
    state.questionStartedAt = performance.now();
    updateTimer();
    state.timerIntervalId = window.setInterval(updateTimer, 100);
}

function stopTimer() {
    if (state.timerIntervalId) {
        window.clearInterval(state.timerIntervalId);
        state.timerIntervalId = null;
    }
}

function updateTimer() {
    elements.timerDisplay.textContent = `${getElapsedSeconds().toFixed(1)}s`;
}

function getElapsedSeconds() {
    if (!state.questionStartedAt) {
        return 0;
    }

    return (performance.now() - state.questionStartedAt) / 1000;
}

async function apiRequest(path, options = {}) {
    const baseUrl = sanitizeApiBaseUrl(state.apiBaseUrl);
    if (!baseUrl) {
        throw new Error("URL da API nao configurada.");
    }

    let response;

    try {
        response = await fetch(`${baseUrl}${path}`, options);
    } catch {
        throw new Error("Falha de rede ao acessar a API. Verifique se a API do Render esta no ar e se o CORS foi liberado.");
    }

    const text = await response.text();
    const data = text ? tryParseJson(text) : null;

    if (!response.ok) {
        throw new Error(typeof data === "string" ? data : text || `Falha ao chamar ${path}.`);
    }

    return data;
}

function getApiBaseUrl() {
    const { protocol, hostname, origin } = window.location;

    if (hostname === "localhost" || hostname === "127.0.0.1") {
        return "http://localhost:5266";
    }

    if (protocol.startsWith("http") && hostname.endsWith(".onrender.com")) {
        return origin;
    }

    return productionApiBaseUrl;
}

function setAuthStatus(message, type) {
    setStatus(elements.authStatus, message, type);
}

function setFeedback(message, type) {
    setStatus(elements.feedbackMessage, message, type);
}

function setRankingStatus(message, type) {
    setStatus(elements.rankingStatus, message, type);
}

function setStatus(element, message, type) {
    element.textContent = message;
    element.classList.remove("success", "error");

    if (type) {
        element.classList.add(type);
    }
}

function sanitizeApiBaseUrl(url) {
    return (url || "").trim().replace(/\/+$/, "");
}

function safeParse(value) {
    try {
        return value ? JSON.parse(value) : null;
    } catch {
        return null;
    }
}

function tryParseJson(text) {
    try {
        return JSON.parse(text);
    } catch {
        return text;
    }
}

function escapeHtml(value) {
    return String(value)
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#039;");
}
