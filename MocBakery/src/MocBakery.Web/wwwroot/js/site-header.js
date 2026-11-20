(() => {
  const toggle = document.querySelector('[data-header-search]');
  const panel = document.querySelector('#header-search');
  const input = panel?.querySelector('input');
  if (!toggle || !panel || !input) return;
  function close() { panel.hidden = true; toggle.setAttribute('aria-expanded', 'false'); toggle.focus(); }
  toggle.addEventListener('click', () => {
    if (!panel.hidden) { close(); return; }
    panel.hidden = false; toggle.setAttribute('aria-expanded', 'true'); input.focus();
  });
  panel.querySelector('[data-header-search-close]').addEventListener('click', close);
  document.addEventListener('keydown', event => {
    if (event.key === 'Escape' && !panel.hidden) { event.preventDefault(); close(); }
  });
})();
