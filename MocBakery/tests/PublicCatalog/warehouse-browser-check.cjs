// Run against PublicCatalog --serve only. It creates disposable warehouse fixture records.
const {chromium}=require('playwright');const path=require('path'),fs=require('fs');
(async()=>{
 const base=process.env.MOCBAKERY_PREVIEW_URL||'http://127.0.0.1:5088';
 if(!['http://127.0.0.1:5088','http://localhost:5088'].includes(base))throw Error('Use the isolated 5088 fixture, never the group database');
 const out=path.resolve(process.env.MOCBAKERY_EVIDENCE_DIR||path.join(__dirname,'../../artifacts/warehouse-browser'));fs.mkdirSync(out,{recursive:true});
 const browser=await chromium.launch({headless:true,channel:'msedge'}),context=await browser.newContext({reducedMotion:'reduce',viewport:{width:1440,height:960}}),page=await context.newPage();
 const checks=[],errors=[];page.on('pageerror',e=>errors.push(e.message));const assert=(ok,message)=>{if(!ok)throw Error(message)};
 async function check(name,work){await work();checks.push(name);console.log('PASS '+name)}
 const suffix=Date.now().toString(),supplier='NCC giao diện '+suffix,material='Bột giao diện '+suffix;
 await page.goto(base+'/admin/dang-nhap');await page.getByLabel('Tên đăng nhập',{exact:true}).fill('owner');await page.getByLabel('Mật khẩu',{exact:true}).fill('PublicTest123!');await page.getByRole('button',{name:'Vào trang quản trị'}).click();await page.waitForURL(base+'/admin');
 await check('Create supplier through browser and return to supplier list',async()=>{
  await page.getByRole('link',{name:'Nhà cung cấp',exact:true}).click();await page.getByRole('link',{name:'+ Thêm nhà cung cấp',exact:true}).click();
  await page.getByLabel('Tên nhà cung cấp *',{exact:true}).fill(supplier);await page.getByLabel('Điện thoại *',{exact:true}).fill('02812345678');await page.getByLabel('Email',{exact:true}).fill('fixture@example.com');await page.getByRole('button',{name:'Lưu nhà cung cấp'}).click();await page.waitForURL(base+'/admin/nha-cung-cap');assert(await page.getByRole('row').filter({hasText:supplier}).count()===1,'Supplier not saved');
 });
 let materialId;
 await check('Create material with fractional warning threshold',async()=>{
  await page.getByRole('link',{name:'Nguyên liệu',exact:true}).click();await page.getByRole('link',{name:'+ Thêm nguyên liệu'}).click();await page.getByLabel('Tên nguyên liệu *',{exact:true}).fill(material);await page.getByLabel('Đơn vị *',{exact:true}).fill('Kg');await page.getByLabel('Ngưỡng cảnh báo *',{exact:true}).fill('0.125');await page.getByRole('button',{name:'Lưu nguyên liệu'}).click();await page.waitForURL(base+'/admin/nguyen-lieu');
  const row=page.getByRole('row').filter({hasText:material});materialId=(await row.getByRole('link',{name:'Sửa',exact:true}).getAttribute('href')).match(/\/(\d+)\/sua/)[1];assert((await row.innerText()).includes('0,125'),'Fractional threshold changed');
 });
 let receiptRoute,stockBefore;
 async function cakeStock(){const p=await context.newPage();await p.goto(base+'/admin/kho');const row=p.getByRole('row').filter({hasText:'Croissant'}).first();const text=await row.locator('td').nth(2).innerText();await p.close();return Number(text.replace(/[^0-9]/g,''))}
 await check('Build multiline receipt, remove middle row and recalculate total',async()=>{
  stockBefore=await cakeStock();await page.getByRole('link',{name:'Phiếu nhập kho',exact:true}).click();await page.getByRole('link',{name:'+ Lập phiếu nhập'}).click();
  const supplierValue=await page.getByLabel('Nhà cung cấp *',{exact:true}).locator('option').filter({hasText:supplier}).getAttribute('value');await page.getByLabel('Nhà cung cấp *',{exact:true}).selectOption(supplierValue);
  const row=page.locator('[data-line]').first();await row.locator('[data-field=Item]').selectOption('Banh:2');await row.locator('[data-field=LotCode]').fill('BROWSER-CAKE-'+suffix);await row.locator('[data-field=Quantity]').fill('3');await row.locator('[data-field=Cost]').fill('18000');
  await page.getByRole('button',{name:'+ Thêm dòng hàng'}).click();await page.getByRole('button',{name:'+ Thêm dòng hàng'}).click();
  const third=page.locator('[data-line]').nth(2);await third.locator('[data-field=Item]').selectOption('NguyenLieu:'+materialId);await third.locator('[data-field=LotCode]').fill('BROWSER-RAW-'+suffix);await third.locator('[data-field=Quantity]').fill('2.5');await third.locator('[data-field=Cost]').fill('24000');
  await page.locator('[data-line]').nth(1).getByRole('button',{name:'Bỏ dòng'}).click();assert(await page.locator('[data-line]').count()===2,'Middle row not removed');assert(await page.locator('[data-line]').nth(1).locator('[data-field=Item]').getAttribute('name')==='Lines[1].Item','Indexes not repaired');assert((await page.locator('[data-receipt-total]').innerText()).includes('114.000'),'Incorrect draft total');
  const cost=page.locator('[data-line]').nth(1).locator('[data-field=Cost]');await cost.fill('999999999999999999999999999');assert(await page.locator('[data-receipt-total]').innerText()==='—','Invalid amount must not show a misleading total');await cost.fill('24000');assert((await page.locator('[data-receipt-total]').innerText()).includes('114.000'),'Valid total did not recover');
 });
 await check('Save draft leaves stock unchanged and edit disables posting until saved',async()=>{
  await page.getByRole('button',{name:'Lưu nháp',exact:true}).click();await page.waitForURL(/\/admin\/phieu-nhap\/\d+$/);receiptRoute=new URL(page.url()).pathname;
  assert(await cakeStock()===stockBefore,'Draft changed stock');assert(await page.getByRole('button',{name:'Ghi sổ phiếu đã lưu'}).isEnabled(),'Saved draft cannot post');
  await page.locator('[data-line]').first().locator('[data-field=Cost]').fill('18001');assert(await page.getByRole('button',{name:'Ghi sổ phiếu đã lưu'}).isDisabled(),'Unsaved draft can post');await page.reload();
  await page.screenshot({path:path.join(out,'receipt-draft-1440.png'),fullPage:true});
 });
 await check('Post receipt creates stock and locks the receipt',async()=>{
  await page.getByRole('button',{name:'Ghi sổ phiếu đã lưu'}).click();await page.getByRole('dialog').getByRole('button',{name:'Xác nhận ghi sổ',exact:true}).click();await page.waitForURL(base+receiptRoute);await page.getByRole('paragraph').filter({hasText:'Đã ghi sổ — phiếu được giữ nguyên'}).waitFor();assert(await page.getByRole('button',{name:'Lưu nháp',exact:true}).count()===0,'Posted receipt remains editable');assert(await page.getByLabel('Nhà cung cấp *',{exact:true}).isDisabled(),'Posted fields not locked');assert(await cakeStock()===stockBefore+3,'Posted cake stock mismatch');
  await page.screenshot({path:path.join(out,'receipt-posted-1440.png'),fullPage:true});
 });
 let historyRoute;
 await check('Stocktake records old and new quantity with reason and returns to history',async()=>{
  await page.getByRole('link',{name:'Tồn kho và hạn dùng',exact:true}).click();await page.getByLabel('Loại hàng',{exact:true}).selectOption('NguyenLieu');await page.getByRole('button',{name:'Lọc tồn kho'}).click();const row=page.getByRole('row').filter({hasText:material});await row.getByRole('link',{name:'Kiểm kê lô'}).click();await page.getByLabel('Số lượng thực đếm *',{exact:true}).fill('3.125');await page.getByLabel('Lý do *',{exact:true}).fill('Kiểm kê: minh chứng trình duyệt');await page.getByRole('button',{name:'Lưu kiểm kê'}).click();await page.getByRole('dialog').getByRole('button',{name:'Lưu kiểm kê',exact:true}).click();await page.waitForURL(/\/admin\/ton-kho\/NguyenLieu\/\d+\/lich-su$/);historyRoute=new URL(page.url()).pathname;
  const audit=page.getByRole('row').filter({hasText:'minh chứng trình duyệt'});assert((await audit.innerText()).includes('2,5')&&(await audit.innerText()).includes('3,125'),'Audit quantities not preserved');
  await page.screenshot({path:path.join(out,'stocktake-history-1440.png'),fullPage:true});
 });
 let pages=0;
 await check('Warehouse lists forms receipt and audit fit desktop and mobile',async()=>{
  const routes=['/admin/nha-cung-cap','/admin/nha-cung-cap/them','/admin/nguyen-lieu','/admin/nguyen-lieu/them','/admin/phieu-nhap','/admin/phieu-nhap/them',receiptRoute,'/admin/ton-kho','/admin/ton-kho?kind=NguyenLieu','/admin/ton-kho/dieu-chinh?kind=NguyenLieu',historyRoute];
  for(const width of [320,375,768,1440]){await page.setViewportSize({width,height:960});for(const route of routes){const response=await page.goto(base+route);assert(response.status()===200,route+' returned '+response.status());assert(!await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth),'Overflow '+width+' '+route);pages++;if(width===375&&(route===receiptRoute||route===historyRoute))await page.screenshot({path:path.join(out,route===receiptRoute?'receipt-posted-375.png':'stocktake-history-375.png'),fullPage:true});}}
 });
 await check('New warehouse forms cancel and back return to correct lists',async()=>{
  for(const [route,parent,back] of [['/admin/nha-cung-cap/them','/admin/nha-cung-cap','← Nhà cung cấp'],['/admin/nguyen-lieu/them','/admin/nguyen-lieu','← Nguyên liệu'],['/admin/phieu-nhap/them','/admin/phieu-nhap','← Phiếu nhập kho']]){
   await page.goto(base+route);await page.getByRole('link',{name:back,exact:true}).click();assert(new URL(page.url()).pathname===parent,'Wrong back');await page.goto(base+route);await page.getByRole('link',{name:route.includes('phieu-nhap')?'Hủy và quay lại':'Hủy',exact:true}).click();assert(new URL(page.url()).pathname===parent,'Wrong cancel');
  }
 });
 assert(errors.length===0,errors.join('\n'));const report={date:new Date().toISOString(),checks,passed:checks.length,pagesChecked:pages,pageErrors:errors,fixtureOnly:true};fs.writeFileSync(path.join(out,'warehouse-browser-result.json'),JSON.stringify(report,null,2));console.log(JSON.stringify(report));await browser.close();
})().catch(e=>{console.error(e);process.exit(1)});
