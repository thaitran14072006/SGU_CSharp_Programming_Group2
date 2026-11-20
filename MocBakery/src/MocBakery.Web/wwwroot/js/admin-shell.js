(() => {
    const toggle = document.querySelector('.admin-menu-toggle');
    const menu = document.getElementById('admin-navigation');
    if (!toggle || !menu) return;
    const compact = matchMedia('(max-width: 850px)');
    const close = (restoreFocus = false) => {
        toggle.setAttribute('aria-expanded', 'false');
        toggle.setAttribute('aria-label', 'Mở menu quản trị');
        menu.hidden = compact.matches;
        if (restoreFocus) toggle.focus();
    };
    document.documentElement.classList.add('admin-nav-ready');
    close();
    toggle.addEventListener('click', () => {
        const open = toggle.getAttribute('aria-expanded') !== 'true';
        toggle.setAttribute('aria-expanded', String(open));
        toggle.setAttribute('aria-label', open ? 'Đóng menu quản trị' : 'Mở menu quản trị');
        menu.hidden = !open;
        if (open) menu.querySelector('a[aria-current="page"]')?.focus();
    });
    document.addEventListener('keydown', event => {
        if (event.key === 'Escape' && toggle.getAttribute('aria-expanded') === 'true') close(true);
    });
    document.addEventListener('click', event => {
        if (compact.matches && !menu.contains(event.target) && !toggle.contains(event.target)) close(menu.contains(document.activeElement));
    });
    document.addEventListener('focusin', event => {
        if (compact.matches && !menu.contains(event.target) && !toggle.contains(event.target)) close();
    });
    compact.addEventListener('change', () => {
        const focusWasInMenu = menu.contains(document.activeElement);
        close(compact.matches && focusWasInMenu);
    });
})();
