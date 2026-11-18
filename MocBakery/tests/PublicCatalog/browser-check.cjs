// Run against the --serve isolated fixture. Requires Playwright and a Chromium installation.
const { chromium } = require('playwright');
const path = require('path');
const fs = require('fs');
(async () => {
  const base = process.env.MOCBAKERY_PREVIEW_URL || 'http://127.0.0.1:5088';
  const out = path.resolve(__dirname, '../../docs/web/evidence');
  fs.mkdirSync(out, { recursive: true });
  const browser = await chromium.launch({ headless: true, channel: 'msedge' });
  const context = await browser.newContext();
  const page = await context.newPage();
  const errors = [];
  page.on('pageerror', e => errors.push(e.message));
  let checked = 0;
  const products = await (await context.request.get(base + '/api/banh')).json();
  const id = products.items[0].id;
  for (const width of [320, 375, 768, 1440]) {
    await page.setViewportSize({ width, height: 960 });
    for (const route of ['/', '/banh', '/danh-muc', '/banh/' + id, '/banh?q=khong-tim-thay', '/tai-khoan/dang-nhap']) {
      await page.goto(base + route);
      await page.evaluate(() => document.fonts.ready);
      const overflow = await page.evaluate(() => document.documentElement.scrollWidth > innerWidth);
      if (overflow) throw new Error('Horizontal overflow: ' + width + ' ' + route);
      const heading = await page.locator('h1').count();
      if (heading !== 1) throw new Error('Expected one h1: ' + route);
      checked++;
      if ((width === 375 || width === 1440) && (route === '/' || route === '/banh/' + id)) {
        await page.screenshot({ path: path.join(out, `${route === '/' ? 'home' : 'detail'}-${width}.png`), fullPage: true });
      }
    }
  }
  await page.setViewportSize({ width: 375, height: 812 });
  await page.goto(base + '/banh');
  await page.getByLabel('Tên hoặc mã bánh').fill('Croissant');
  await page.getByLabel('Sắp xếp').selectOption('price-asc');
  await page.getByRole('button', { name: 'Tìm bánh', exact: true }).click();
  await page.waitForURL(/q=Croissant/);
  if (await page.locator('.product').count() !== 1) throw new Error('Mobile search result mismatch');
  await page.locator('.product h2 a').click();
  if (!(await page.locator('h1').innerText()).includes('Croissant')) throw new Error('Detail navigation failed');
  await page.goto(base + '/banh?q=%3Cscript%3Ealert(1)%3C%2Fscript%3E');
  if (await page.locator('script').count()) throw new Error('Script injection');
  await page.goto(base + '/banh');
  await page.keyboard.press('Tab');
  if ((await page.evaluate(() => document.activeElement.textContent)).trim() !== 'Đến nội dung chính') throw new Error('Skip link not first focus');
  await page.keyboard.press('Enter');
  if (await page.evaluate(() => location.hash) !== '#content') throw new Error('Skip link failed');
  if (errors.length) throw new Error(errors.join('\n'));
  const report = { date: new Date().toISOString(), widths: [320,375,768,1440], pagesChecked: checked, interactions: ['mobile GET search and sorting', 'product detail navigation', 'XSS encoding', 'keyboard skip link'], horizontalOverflow: false, pageErrors: errors };
  fs.writeFileSync(path.join(out, 'browser-result.json'), JSON.stringify(report, null, 2) + '\n');
  console.log(JSON.stringify(report));
  await browser.close();
})().catch(e => { console.error(e); process.exit(1); });
