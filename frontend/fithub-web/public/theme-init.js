(function restoreFitHubTheme() {
  var defaultTheme = 'volt';
  var supportedThemes = ['volt', 'pulse', 'ocean', 'high-contrast'];
  var selectedTheme = defaultTheme;

  try {
    var storedTheme = window.localStorage.getItem('fithub.theme.active');
    if (supportedThemes.indexOf(storedTheme) !== -1) selectedTheme = storedTheme;
  } catch (_error) {
    selectedTheme = defaultTheme;
  }

  document.documentElement.setAttribute('data-theme', selectedTheme);
  document.documentElement.classList.add('theme-ready');
})();
