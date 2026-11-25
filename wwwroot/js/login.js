(() => {
    document.querySelectorAll(".login-card__password-toggle").forEach((toggle) => {
        if (!(toggle instanceof HTMLButtonElement)) {
            return;
        }

        const password = document.getElementById(toggle.getAttribute("aria-controls"));
        if (!(password instanceof HTMLInputElement)) {
            return;
        }

        const fieldName = password.labels?.[0]?.textContent?.trim() || "password";
        toggle.hidden = false;
        toggle.addEventListener("click", () => {
            const visible = password.type === "password";
            password.type = visible ? "text" : "password";
            toggle.textContent = visible ? "Hide" : "Show";
            toggle.setAttribute("aria-label", `${visible ? "Hide" : "Show"} ${fieldName}`);
            toggle.setAttribute("aria-pressed", String(visible));
        });
    });
})();
