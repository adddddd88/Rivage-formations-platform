window.RivageAi = (function () {
  let anamClient = null;

  async function startSession(moduleId) {
    const statusEl = document.getElementById("ai-status");
    const logEl = document.getElementById("ai-log");
    const videoEl = document.getElementById("persona-video");
    setStatus(statusEl, "Connexion au formateur IA…");

    const res = await fetch(`/api/ai-avatar/session/${moduleId}`, { method: "POST" });
    if (!res.ok) {
      setStatus(statusEl, "Impossible de démarrer la session IA.");
      return;
    }
    const data = await res.json();

    if (data.isMock || !data.sessionToken) {
      setStatus(statusEl, data.message || "Mode démonstration (TTS navigateur).");
      if (data.narrationScript) {
        speak(data.narrationScript);
        if (logEl) logEl.textContent = data.narrationScript;
      }
      return;
    }

    try {
      const { createClient } = await import("https://esm.sh/@anam-ai/js-sdk@latest");
      anamClient = createClient(data.sessionToken);
      if (videoEl) {
        await anamClient.streamToVideoElement("persona-video");
      }
      setStatus(statusEl, "Formateur Anam.ai connecté — parlez ou posez une question.");
    } catch (err) {
      console.error(err);
      setStatus(statusEl, "Échec Anam — bascule TTS.");
      if (data.narrationScript) speak(data.narrationScript);
    }
  }

  async function ask(moduleId) {
    const input = document.getElementById("ai-question");
    const logEl = document.getElementById("ai-log");
    const statusEl = document.getElementById("ai-status");
    const question = (input?.value || "").trim();
    if (!question) return;

    setStatus(statusEl, "Réflexion…");
    const res = await fetch(`/api/ai-avatar/ask/${moduleId}`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ question })
    });
    if (!res.ok) {
      setStatus(statusEl, "Erreur lors de la question.");
      return;
    }
    const data = await res.json();
    if (logEl) logEl.textContent = data.answer;
    speak(data.answer);
    setStatus(statusEl, data.isMock ? "Réponse (mode démo)." : "Réponse prête.");
    if (input) input.value = "";
  }

  function speak(text) {
    if (!("speechSynthesis" in window)) return;
    window.speechSynthesis.cancel();
    const utter = new SpeechSynthesisUtterance(text);
    utter.lang = "fr-FR";
    utter.rate = 1;
    window.speechSynthesis.speak(utter);
  }

  function stop() {
    window.speechSynthesis?.cancel();
    try { anamClient?.stopStreaming?.(); } catch (_) {}
    anamClient = null;
    setStatus(document.getElementById("ai-status"), "Session arrêtée.");
  }

  function setStatus(el, text) {
    if (el) el.textContent = text;
  }

  return { startSession, ask, stop, speak };
})();
