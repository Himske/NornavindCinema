(function () {
    var hero = document.querySelector("[data-hero]");
    if (hero) {
        var slides = hero.querySelectorAll("[data-hero-slide]");
        var dots = hero.querySelectorAll("[data-hero-dot]");
        var current = 0;
        var timer = null;

        var show = function (index) {
            current = (index + slides.length) % slides.length;
            slides.forEach(function (s, i) { s.classList.toggle("is-active", i === current); });
            dots.forEach(function (d, i) { d.classList.toggle("is-active", i === current); });
        };
        var restart = function () {
            clearInterval(timer);
            if (slides.length > 1) {
                timer = setInterval(function () { show(current + 1); }, 8000);
            }
        };

        dots.forEach(function (dot) {
            dot.addEventListener("click", function () {
                show(parseInt(dot.getAttribute("data-hero-dot"), 10));
                restart();
            });
        });
        restart();

        var stored = [];
        try { stored = JSON.parse(localStorage.getItem("cinema-list") || "[]"); } catch (e) { stored = []; }
        var buttons = hero.querySelectorAll("[data-add-list]");
        var paint = function (btn) {
            var on = stored.indexOf(btn.getAttribute("data-add-list")) >= 0;
            btn.textContent = on ? "\u2713 IN LIST" : "+ ADD LIST";
        };
        buttons.forEach(function (btn) {
            paint(btn);
            btn.addEventListener("click", function () {
                var id = btn.getAttribute("data-add-list");
                var at = stored.indexOf(id);
                if (at >= 0) { stored.splice(at, 1); } else { stored.push(id); }
                try { localStorage.setItem("cinema-list", JSON.stringify(stored)); } catch (e) { }
                paint(btn);
            });
        });
    }

    var trends = document.querySelector("[data-trends]");
    if (!trends) { return; }

    var tabs = trends.querySelectorAll("[data-tab]");
    var chips = trends.querySelectorAll("[data-genre]");
    var panels = trends.querySelectorAll("[data-panel]");
    var activeTab = "popular";
    var activeGenre = "";

    var apply = function () {
        tabs.forEach(function (t) { t.classList.toggle("is-active", t.getAttribute("data-tab") === activeTab); });
        chips.forEach(function (c) { c.classList.toggle("is-active", c.getAttribute("data-genre") === activeGenre); });
        panels.forEach(function (panel) {
            var visible = panel.getAttribute("data-panel") === activeTab;
            panel.hidden = !visible;
            if (!visible) { return; }
            var shown = 0;
            panel.querySelectorAll(".cinema-card").forEach(function (card) {
                var genres = (card.getAttribute("data-genres") || "").split("|");
                var match = !activeGenre || genres.indexOf(activeGenre) >= 0;
                card.hidden = !match;
                if (match) { shown++; }
            });
            panel.querySelector(".cinema-carousel__empty").hidden = shown > 0;
            panel.querySelector("[data-track]").scrollLeft = 0;
        });
    };

    tabs.forEach(function (t) {
        t.addEventListener("click", function () { activeTab = t.getAttribute("data-tab"); apply(); });
    });
    chips.forEach(function (c) {
        c.addEventListener("click", function () { activeGenre = c.getAttribute("data-genre"); apply(); });
    });

    trends.querySelectorAll("[data-scroll]").forEach(function (arrow) {
        arrow.addEventListener("click", function () {
            var track = arrow.parentElement.querySelector("[data-track]");
            var direction = parseInt(arrow.getAttribute("data-scroll"), 10);
            track.scrollBy({ left: direction * track.clientWidth * 0.8, behavior: "smooth" });
        });
    });
})();
