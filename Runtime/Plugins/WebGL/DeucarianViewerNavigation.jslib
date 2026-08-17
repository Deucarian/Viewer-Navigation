mergeInto(LibraryManager.library, {
  DeucarianViewerNavigationPrefersReducedMotion: function () {
    if (typeof window === "undefined" ||
        typeof window.matchMedia !== "function") {
      return 0;
    }

    try {
      return window.matchMedia("(prefers-reduced-motion: reduce)").matches
        ? 1
        : 0;
    } catch (error) {
      return 0;
    }
  }
});
