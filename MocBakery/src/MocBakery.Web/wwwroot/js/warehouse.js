(() => {
  const form = document.querySelector('[data-receipt]');
  if (!form) return;
  const lines = form.querySelector('[data-lines]');
  const total = form.querySelector('[data-receipt-total]');
  const post = document.querySelector('[data-post-receipt]');
  const notice = document.querySelector('[data-unsaved]');
  const money = value => new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND', maximumFractionDigits: 2 }).format(value);
  const rows = () => Array.from(lines.querySelectorAll('[data-line]'));
  function update() {
    let sum = 0;
    rows().forEach((row, index) => {
      row.querySelector('[data-line-number]').textContent = index + 1;
      row.querySelectorAll('[data-field]').forEach(input => {
        const field = input.dataset.field;
        const oldId = input.id;
        input.name = `Lines[${index}].${field}`;
        input.id = `Lines_${index}__${field}`;
        const label = Array.from(row.querySelectorAll('label')).find(label => label.htmlFor === oldId);
        if (label) label.htmlFor = input.id;
      });
      const quantity = Number(row.querySelector('[data-field=Quantity]').value);
      const cost = Number(row.querySelector('[data-field=Cost]').value);
      const amount = Number.isFinite(quantity * cost) ? Math.round((quantity * cost + Number.EPSILON) * 100) / 100 : 0;
      sum += amount;
      row.querySelector('[data-line-amount]').textContent = money(amount);
    });
    total.textContent = money(sum);
  }
  function dirty() {
    if (post) post.disabled = true;
    if (notice) notice.hidden = false;
    update();
  }
  function cleanRow(row) {
    row.querySelectorAll('[data-field]').forEach(input => {
      if (input.tagName === 'SELECT') input.value = '';
      else if (input.dataset.field === 'Quantity') input.value = '1';
      else if (input.dataset.field === 'Cost') input.value = '0';
      else if (!['Produced', 'Expiry'].includes(input.dataset.field)) input.value = '';
      input.classList.remove('input-validation-error');
    });
  }
  form.querySelector('[data-add-line]')?.addEventListener('click', () => {
    if (rows().length >= 200) return;
    const row = rows()[0].cloneNode(true);
    cleanRow(row); lines.append(row); dirty(); row.querySelector('select').focus();
  });
  lines.addEventListener('click', event => {
    const button = event.target.closest('[data-remove-line]');
    if (!button) return;
    const row = button.closest('[data-line]');
    if (rows().length > 1) row.remove(); else cleanRow(row);
    dirty();
  });
  form.addEventListener('input', dirty);
  form.addEventListener('change', dirty);
  form.classList.add('js-ready'); update();
})();
