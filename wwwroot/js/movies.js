(() => {
    const rows = Array.from(document.querySelectorAll("#movie-rows tr[data-title]"));
    const search = document.getElementById("movie-search");
    const genre = document.getElementById("movie-genre");
    const count = document.getElementById("movie-count");
    const pageInfo = document.getElementById("movie-page-info");
    const pageNumber = document.getElementById("movie-page-number");
    const emptyMessage = document.getElementById("movie-empty");
    const first = document.getElementById("movie-first");
    const previous = document.getElementById("movie-previous");
    const next = document.getElementById("movie-next");
    const last = document.getElementById("movie-last");
    const refresh = document.getElementById("movie-refresh");
    const stateWarning = document.getElementById("movie-state-warning");

    if (!(search instanceof HTMLInputElement) || !(genre instanceof HTMLSelectElement)
        || !count || !pageInfo || !pageNumber || !emptyMessage || !stateWarning
        || !(first instanceof HTMLButtonElement) || !(previous instanceof HTMLButtonElement)
        || !(next instanceof HTMLButtonElement) || !(last instanceof HTMLButtonElement) || !refresh) {
        throw new Error("Movie list controls are missing or invalid.");
    }

    const storageKey = "nornavind.movieListState";
    const pageSize = 8;
    let currentPage = 1;
    let filteredRows = rows;
    let savedState = null;

    try {
        const stored = sessionStorage.getItem(storageKey);
        if (stored !== null) savedState = JSON.parse(stored);
    } catch (error) {
        if (!(error instanceof DOMException) && !(error instanceof SyntaxError)) throw error;
        console.warn("Unable to restore movie list state.", error);
        stateWarning.hidden = false;
    }

    if (savedState && typeof savedState === "object") {
        if (typeof savedState.search === "string") search.value = savedState.search;
        if (typeof savedState.genre === "string") genre.value = savedState.genre;
        if (Number.isSafeInteger(savedState.page) && savedState.page > 0) currentPage = savedState.page;
    }

    const saveState = (scrollY = null) => {
        try {
            sessionStorage.setItem(storageKey, JSON.stringify({ search: search.value, genre: genre.value, page: currentPage, scrollY }));
        } catch (error) {
            if (!(error instanceof DOMException)) throw error;
            console.warn("Unable to save movie list state.", error);
            stateWarning.hidden = false;
        }
    };

    const render = () => {
        const query = search.value.trim().toLocaleLowerCase();
        const selectedGenre = genre.value.toLocaleLowerCase();
        filteredRows = rows.filter(row => {
            const title = row.getAttribute("data-title");
            const genreValues = row.getAttribute("data-genres");
            if (title === null || genreValues === null) {
                throw new Error("Movie row data is missing.");
            }
            const titleMatches = title.toLocaleLowerCase().includes(query);
            const genres = genreValues.split("|").map(value => value.trim().toLocaleLowerCase());
            return titleMatches && (!selectedGenre || genres.includes(selectedGenre));
        });

        const pageCount = Math.max(1, Math.ceil(filteredRows.length / pageSize));
        currentPage = Math.min(currentPage, pageCount);
        const start = (currentPage - 1) * pageSize;
        const visibleRows = new Set(filteredRows.slice(start, start + pageSize));

        rows.forEach(row => {
            if (!(row instanceof HTMLTableRowElement)) {
                throw new Error("Invalid movie row.");
            }
            row.hidden = !visibleRows.has(row);
            if (visibleRows.has(row)) {
                const number = row.querySelector(".movie-number");
                if (!number) {
                    throw new Error("Movie row number is missing.");
                }
                number.textContent = String(filteredRows.indexOf(row) + 1);
            }
        });

        emptyMessage.hidden = filteredRows.length !== 0;
        count.textContent = `Showing ${filteredRows.length ? start + 1 : 0} to ${Math.min(start + pageSize, filteredRows.length)} of ${filteredRows.length} movies`;
        pageInfo.textContent = `Showing ${filteredRows.length ? start + 1 : 0} to ${Math.min(start + pageSize, filteredRows.length)} of ${filteredRows.length} entries`;
        pageNumber.textContent = String(currentPage);
        first.disabled = previous.disabled = currentPage <= 1;
        last.disabled = next.disabled = currentPage >= pageCount;
        saveState();
    };

    search.addEventListener("input", () => { currentPage = 1; render(); });
    genre.addEventListener("change", () => { currentPage = 1; render(); });
    first.addEventListener("click", () => { currentPage = 1; render(); });
    previous.addEventListener("click", () => { currentPage--; render(); });
    next.addEventListener("click", () => { currentPage++; render(); });
    last.addEventListener("click", () => { currentPage = Math.max(1, Math.ceil(filteredRows.length / pageSize)); render(); });
    refresh.addEventListener("click", () => {
        search.value = "";
        genre.value = "";
        currentPage = 1;
        render();
    });
    document.querySelectorAll("#movie-rows a.movie-title").forEach(link => {
        link.addEventListener("click", () => saveState(window.scrollY));
    });

    render();
    if (savedState && Number.isFinite(savedState.scrollY) && savedState.scrollY >= 0) {
        window.scrollTo(0, savedState.scrollY);
    }
})();
