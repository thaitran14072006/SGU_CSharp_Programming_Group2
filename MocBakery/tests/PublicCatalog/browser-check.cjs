// Run against the --serve isolated fixture. Requires Playwright and a Chromium installation.
const { chromium } = require('playwright');
const path = require('path');
const fs = require('fs');
(async () => {
  const base = process.env.MOCBAKERY_PREVIEW_URL || 'http://127.0.0.1:5088';
  const out = process.env.MOCBAKERY_EVIDENCE_DIR ? path.resolve(process.env.MOCBAKERY_EVIDENCE_DIR) : path.resolve(__dirname, '../../docs/web/evidence');
  fs.mkdirSync(out, { recursive: true });
  const browser = await chromium.launch({ headless: true, channel: 'msedge' });
  const context = await browser.newContext({ reducedMotion: 'reduce' });
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
      if (await page.locator('.validation-summary-valid').isVisible()) throw new Error('Empty validation summary is visible: ' + route);
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
  await page.goto(base + '/');
  await page.getByRole('navigation', { name: 'Chọn danh mục bánh' }).getByRole('link').filter({ hasText: 'Bánh mì' }).click();
  if (await page.locator('.product').count() !== 1) throw new Error('Home category filter failed');
  // This account exists only in the isolated --serve fixture, never in production.
  await page.goto(base + '/tai-khoan/dang-nhap');
  await page.getByLabel('Tên đăng nhập').fill('catalog.customer');
  await page.getByLabel('Mật khẩu', { exact: true }).fill('CatalogTest123!');
  await page.getByRole('button', { name: 'Đăng nhập', exact: true }).click();
  await page.waitForURL(base + '/tai-khoan');
  for (const width of [320,375,768,1440]) {
    await page.setViewportSize({ width, height: 960 });
    for (const route of ['/tai-khoan', '/tai-khoan/ho-so', '/banh']) {
      await page.goto(base + route);
      if (await page.evaluate(() => document.documentElement.scrollWidth > innerWidth)) throw new Error('Authenticated overflow: ' + width + ' ' + route);
      if (await page.locator('.validation-summary-valid').isVisible()) throw new Error('Empty profile validation summary is visible');
      checked++;
      if (route === '/tai-khoan/ho-so' && (width === 375 || width === 1440)) await page.screenshot({ path: path.join(out, `profile-${width}.png`), fullPage: true });
    }
  }
  await page.goto(base + '/tai-khoan/ho-so');
  await page.getByLabel('Họ tên').fill('Khách xem bánh đã sửa');
  await page.getByRole('button', { name: 'Lưu thay đổi' }).click();
  await page.waitForURL(base + '/tai-khoan');
  if (!(await page.locator('h1').innerText()).includes('Khách xem bánh đã sửa')) throw new Error('Profile update not reflected');
  await page.getByRole('navigation', { name: 'Điều hướng chính' }).getByRole('link', { name: 'Thực đơn bánh' }).click();
  if (!await page.getByRole('button', { name: 'Đăng xuất' }).isVisible()) throw new Error('Lost customer session');
  await page.getByRole('link', { name: 'Croissant', exact: true }).click();
  await page.locator('input[name="quantity"]').fill('1');
  await page.getByRole('button', { name: 'Thêm vào giỏ' }).click();
  for (const width of [320,375,768,1440]) {
    await page.setViewportSize({ width, height: 960 });
    await page.goto(base + '/gio-hang');
    if (await page.evaluate(() => document.documentElement.scrollWidth > innerWidth)) throw new Error('Cart overflow: ' + width);
    checked++;
    await page.goto(base + '/gio-hang/xac-nhan');
    if (await page.evaluate(() => document.documentElement.scrollWidth > innerWidth)) throw new Error('Checkout overflow: ' + width);
    if ((await page.locator('.cart-summary h2').innerText()).normalize('NFC') !== 'Tổng yêu cầu') throw new Error('Vietnamese heading changed');
    if (!(await page.locator('.cart-summary h2').evaluate(el => getComputedStyle(el).fontFamily)).includes('Times New Roman')) throw new Error('Vietnamese font override missing');
    checked++;
    if (width === 375 || width === 1440) await page.screenshot({ path: path.join(out, `checkout-${width}.png`), fullPage: true });
  }
  await page.getByRole('button', { name: 'Xác nhận mua hàng', exact: true }).click();
  if (!(new URL(page.url()).pathname === '/gio-hang/xac-nhan')) throw new Error('Checkout accepted missing contact');
  for(const name of ['FullName','Phone','Email'])if(!await page.locator('#'+name).evaluate(el=>el.required&&el.validity.valueMissing))throw new Error('Contact is not required: '+name);
  await page.getByLabel('Họ tên người nhận').fill('Người nhận thử giao diện');
  await page.getByLabel('Số điện thoại', { exact: false }).fill('0901122334');
  await page.getByLabel('Email', { exact: false }).fill('recipient@example.com');
  await page.getByLabel('Yêu cầu thêm').fill('Nhận tại tiệm, đóng hộp riêng.');
  await page.getByRole('button', { name: 'Xác nhận mua hàng', exact: true }).click();
  await page.waitForURL(/\/don-hang\/\d+$/);
  const receipt = new URL(page.url()).pathname;
  if (!(await page.locator('.cart-summary').innerText()).includes('Mới tiếp nhận')) throw new Error('Order was not received');
  if (!(await page.locator('.order-contact').innerText()).includes('recipient@example.com')) throw new Error('Recipient contact missing on receipt');
  for (const width of [320,375,768,1440]) {
    await page.setViewportSize({ width, height: 960 });
    for (const route of [receipt, '/don-hang']) {
      await page.goto(base + route);
      if (await page.evaluate(() => document.documentElement.scrollWidth > innerWidth)) throw new Error('Order overflow: ' + width + ' ' + route);
      checked++;
      if (route === receipt && (width === 375 || width === 1440)) await page.screenshot({ path: path.join(out, `receipt-${width}.png`), fullPage: true });
    }
  }
  if (errors.length) throw new Error(errors.join('\n'));
  const report = { date: new Date().toISOString(), widths: [320,375,768,1440], pagesChecked: checked, interactions: ['mobile GET search and sorting', 'product detail navigation', 'XSS encoding', 'keyboard skip link', 'home category navigation', 'SQL login, profile update and return to catalog', 'required name, phone and email before purchase', 'Vietnamese checkout heading, submit SQL order and read own receipt'], horizontalOverflow: false, pageErrors: errors };
  fs.writeFileSync(path.join(out, 'browser-result.json'), JSON.stringify(report, null, 2) + '\n');
  console.log(JSON.stringify(report));
  await browser.close();
})().catch(e => { console.error(e); process.exit(1); });
