// Run against the PublicCatalog --serve fixture; requires Playwright and Microsoft Edge.
const {chromium}=require('playwright');const path=require('path'),fs=require('fs');
(async()=>{
 const base=process.env.MOCBAKERY_PREVIEW_URL||'http://127.0.0.1:5088',out=path.resolve(process.env.MOCBAKERY_EVIDENCE_DIR||path.join(__dirname,'../../artifacts/admin-browser'));fs.mkdirSync(out,{recursive:true});
 const browser=await chromium.launch({headless:true,channel:'msedge'});const context=await browser.newContext();const page=await context.newPage();
 const errors=[];page.on('pageerror',e=>errors.push(e.message));
 await page.goto(base+'/admin/dang-nhap');await page.getByLabel('Tên đăng nhập').fill('owner');await page.getByLabel('Mật khẩu',{exact:true}).fill('PublicTest123!');await page.getByRole('button',{name:'Vào trang quản trị'}).click();await page.waitForURL(base+'/admin');
 let checked=0;
 for(const width of [320,375,768,1440]){
  await page.setViewportSize({width,height:1000});
  for(const route of ['/admin','/admin/don-hang','/admin/banh','/admin/danh-muc','/admin/kho','/admin/khach-hang','/admin/nhan-vien','/admin/tai-khoan','/admin/hoa-don','/admin/bao-cao','/admin/banh/them','/admin/danh-muc/them','/admin/kho/nhap','/admin/khach-hang/them','/admin/nhan-vien/them','/admin/tai-khoan/them']){
   const response=await page.goto(base+route);if(response.status()!==200)throw Error(route+' returned '+response.status());
   if(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth))throw Error('overflow '+route+' '+width);
   checked++;
   if((width===375||width===1440)&&route==='/admin')await page.screenshot({path:path.join(out,`dashboard-${width}.png`),fullPage:true});
  }
 }
 const employee=(await context.cookies()).find(c=>c.name==='MocBakery.Employee');if(!employee||employee.path!=='/admin'||!employee.httpOnly)throw Error('employee cookie not isolated');
 const profile=await context.request.get(base+'/tai-khoan',{maxRedirects:0});if(profile.status()!==302||!profile.headers().location.includes('/tai-khoan/dang-nhap'))throw Error('employee authenticated customer route');
 const customer=await browser.newContext();const store=await customer.newPage();await store.goto(base+'/tai-khoan/dang-nhap');await store.getByLabel('Tên đăng nhập').fill('catalog.customer');await store.getByLabel('Mật khẩu',{exact:true}).fill('CatalogTest123!');await store.getByRole('button',{name:'Đăng nhập',exact:true}).click();await store.waitForURL(base+'/tai-khoan');
 const admin=await customer.request.get(base+'/admin',{maxRedirects:0});if(admin.status()!==302||!admin.headers().location.includes('/admin/dang-nhap'))throw Error('customer accessed admin');
 if(errors.length)throw Error(errors.join('\n'));console.log(JSON.stringify({pages:checked,overflow:false,employeeCookieIsolated:true,customerCannotAccessAdmin:true,employeeCannotImpersonateCustomer:true,pageErrors:errors}));await browser.close();
})().catch(e=>{console.error(e);process.exit(1)});
