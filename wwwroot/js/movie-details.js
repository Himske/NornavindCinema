(() => {
    const modal = document.getElementById("review-modal");
    if (modal && modal.dataset.openOnLoad === "true") {
        bootstrap.Modal.getOrCreateInstance(modal).show();
    }
})();
