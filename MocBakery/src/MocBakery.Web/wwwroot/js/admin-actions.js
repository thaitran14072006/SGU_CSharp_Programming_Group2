(() => {
    const dialog = document.getElementById('admin-confirm');
    if (!dialog) return;
    let pending = null;
    const accept = dialog.querySelector('[data-confirm-accept]');
    dialog.querySelector('[data-confirm-cancel]').addEventListener('click', () => dialog.close());
    dialog.addEventListener('close', () => { pending = null; });
    accept.addEventListener('click', () => {
        if (!pending) return;
        const { form, button } = pending;
        pending = null;
        dialog.close();
        if (!form.isConnected) return;
        form.dataset.confirmedOnce = 'true';
        if (button) form.requestSubmit(button); else form.requestSubmit();
        delete form.dataset.confirmedOnce;
    });
    document.addEventListener('submit', event => {
        const form = event.target;
        if (!(form instanceof HTMLFormElement) || form.method.toLowerCase() !== 'post' || event.defaultPrevented) return;
        if (form.dataset.submitting) { event.preventDefault(); return; }
        const button = event.submitter;
        if (form.dataset.confirmTitle && !form.dataset.confirmedOnce) {
            event.preventDefault();
            pending = { form, button };
            dialog.querySelector('#confirm-title').textContent = form.dataset.confirmTitle;
            let message = form.dataset.confirmMessage || '';
            if (form.dataset.confirmSummary === 'stock') {
                const item = form.querySelector('select[name="ItemId"]');
                const name = item?.selectedOptions[0]?.textContent || form.dataset.confirmItem || '';
                const quantity = form.querySelector('[name="Quantity"]')?.value || '';
                const disposal = form.querySelector('[name="Disposal"]')?.checked;
                message += `\n\nMặt hàng: ${name.trim()}\nSố lượng: ${quantity}${disposal ? '\nThao tác: Hủy hàng hỏng hoặc hết hạn' : ''}`;
            }
            const reason = form.querySelector('[name="reason"], [name="Reason"]')?.value.trim();
            if (reason) message += `\n\nLý do: ${reason}`;
            dialog.querySelector('#confirm-message').textContent = message;
            accept.textContent = form.dataset.confirmLabel || 'Xác nhận';
            accept.classList.toggle('danger', form.dataset.confirmDanger === 'true');
            accept.classList.toggle('primary', form.dataset.confirmDanger !== 'true');
            dialog.showModal();
            return;
        }
        delete form.dataset.confirmedOnce;
        form.dataset.submitting = 'true';
        form.setAttribute('aria-busy', 'true');
        // No submit control is disabled until the browser has collected its form data.
        setTimeout(() => {
            form.querySelectorAll('button[type="submit"]:not(:disabled)').forEach(control => {
                control.disabled = true;
                control.dataset.submitDisabled = 'true';
            });
            button?.classList.add('is-submitting');
        }, 0);
    });
    window.addEventListener('pageshow', () => {
        document.querySelectorAll('form[data-submitting]').forEach(form => {
            delete form.dataset.submitting;
            form.removeAttribute('aria-busy');
            form.querySelectorAll('[data-submit-disabled]').forEach(control => {
                control.disabled = false;
                delete control.dataset.submitDisabled;
                control.classList.remove('is-submitting');
            });
        });
    });
})();
