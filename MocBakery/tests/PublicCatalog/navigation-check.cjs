// Real browser navigation checks against the isolated PublicCatalog --serve fixture.
const {chromium}=require('playwright');
const fs=require('fs'),path=require('path');
(async()=>{
 const base=process.env.MOCBAKERY_PREVIEW_URL||'http://127.0.0.1:5088';
 const browser=await chromium.launch({headless:true,channel:'msedge'});
 const shopper=await browser.newContext({reducedMotion:'reduce'}),employee=await browser.newContext({reducedMotion:'reduce'});
 const user=await shopper.newPage(),admin=await employee.newPage(),errors=[],checks=[];
 for(const p of [user,admin])p.on('pageerror',e=>errors.push(e.message));
 const assert=(ok,message)=>{if(!ok)throw Error(message)};
 const at=async(p,route)=>{await p.waitForURL(url=>url.origin===base&&url.pathname===route);assert(new URL(p.url()).pathname===route,'Wrong destination '+p.url())};
 const click=async(p,name,route)=>{await p.getByRole('link',{name,exact:true}).click();await at(p,route)};
 async function check(name,work){await work();checks.push(name);console.log('PASS '+name)}
 await check('Anonymous checkout returns to checkout after login; registration link retains destination',async()=>{
  await user.goto(base+'/banh/2');await user.getByRole('button',{name:'Thêm vào giỏ',exact:true}).click();await at(user,'/gio-hang');
  await user.getByRole('link',{name:'Tiếp tục xác nhận',exact:true}).click();
  await user.waitForURL(/\/tai-khoan\/dang-nhap\?/);
  await click(user,'Tạo tài khoản','/tai-khoan/dang-ky');assert(await user.locator('[name=ReturnUrl]').inputValue()==='/gio-hang/xac-nhan','Registration lost checkout');
  await user.locator('.switch').getByRole('link',{name:'Đăng nhập',exact:true}).click();await at(user,'/tai-khoan/dang-nhap');
  await click(user,'← Quay lại giỏ hàng','/gio-hang');
  await user.getByRole('link',{name:'Tiếp tục xác nhận',exact:true}).click();
  await user.getByLabel('Tên đăng nhập').fill('catalog.customer');await user.getByLabel('Mật khẩu',{exact:true}).fill('CatalogTest123!');
  await user.getByRole('button',{name:'Đăng nhập',exact:true}).click();await at(user,'/gio-hang/xac-nhan');
 });
 await check('Checkout back preserves cart; quantity update and remove behave correctly',async()=>{
  await click(user,'← Sửa giỏ hàng','/gio-hang');assert(await user.locator('.cart-item').count()===1,'Back lost cart');
  await user.locator('input[name=quantity]').fill('2');await user.getByRole('button',{name:/Cập nhật/}).click();await at(user,'/gio-hang');
  assert(await user.locator('input[name=quantity]').inputValue()==='2','Quantity not updated');
  await user.getByRole('button',{name:/Xóa/}).click();assert(await user.locator('.cart-item').count()===0,'Remove failed');
  await user.goto(base+'/banh/2');await user.getByRole('button',{name:'Thêm vào giỏ',exact:true}).click();
  await user.getByRole('link',{name:'Tiếp tục xác nhận',exact:true}).click();await at(user,'/gio-hang/xac-nhan');
 });
 let orderRoute;
 await check('Submit purchase, receipt back and browser Back do not create another order',async()=>{
  await user.getByLabel('Họ tên người nhận').fill('Khách kiểm tra điều hướng');await user.getByLabel('Số điện thoại',{exact:false}).fill('0901122334');await user.getByLabel('Email',{exact:false}).fill('navigation@example.com');
  await user.getByRole('button',{name:'Xác nhận mua hàng',exact:true}).click();await user.waitForURL(/\/don-hang\/\d+$/);orderRoute=new URL(user.url()).pathname;
  await user.goBack();await user.reload();await at(user,'/gio-hang');assert(await user.locator('.cart-item').count()===0,'Old checkout restored sold cart');
  await user.goto(base+orderRoute);await click(user,'← Đơn hàng của bạn','/don-hang');
  assert(await user.getByRole('link',{name:'Đơn #'+orderRoute.split('/').pop(),exact:true}).count()===1,'Duplicate receipt');
  await click(user,'← Quay lại trang chủ','/');
 });
 await check('Profile and password cancel return to customer account; account returns home',async()=>{
  await user.goto(base+'/tai-khoan/ho-so');await click(user,'Hủy và quay lại','/tai-khoan');
  await user.goto(base+'/tai-khoan/doi-mat-khau');await click(user,'Hủy và quay lại','/tai-khoan');
  await click(user,'← Quay lại trang chủ','/');
 });
 await check('Customer login rejects external, admin and authentication-loop return URLs',async()=>{
  for(const target of ['https://example.com','//example.com','/admin','/tai-khoan/dang-nhap']){
   await user.goto(base+'/tai-khoan/dang-nhap?returnUrl='+encodeURIComponent(target));assert(await user.locator('[name=ReturnUrl]').inputValue()==='','Unsafe return URL accepted');
  }
 });
 await check('Admin login resumes requested management page',async()=>{
  await admin.goto(base+'/admin/don-hang');await admin.waitForURL(/\/admin\/dang-nhap\?/);
  await admin.getByLabel('Tên đăng nhập').fill('owner');await admin.getByLabel('Mật khẩu',{exact:true}).fill('PublicTest123!');
  await admin.getByRole('button',{name:'Vào trang quản trị'}).click();await at(admin,'/admin/don-hang');
 });
 await check('All admin lists have working back to dashboard links',async()=>{
  for(const width of [375,1440]){await admin.setViewportSize({width,height:960});
   for(const module of ['don-hang','banh','danh-muc','kho','khach-hang','nhan-vien','tai-khoan','hoa-don','bao-cao']){
    await admin.goto(base+'/admin/'+module);await click(admin,'← Về tổng quan','/admin');
   }
  }
 });
 await check('Admin create-form back and cancel return to the correct parent lists',async()=>{
  for(const [route,parent,label] of [
   ['banh/them','banh','← Danh sách bánh'],['danh-muc/them','danh-muc','← Danh mục'],['kho/nhap','kho','← Kho bánh'],
   ['khach-hang/them','khach-hang','← Khách hàng'],['nhan-vien/them','nhan-vien','← Nhân viên'],['tai-khoan/them','tai-khoan','← Tài khoản']]){
    await admin.goto(base+'/admin/'+route);await click(admin,label,'/admin/'+parent);
    await admin.goto(base+'/admin/'+route);await click(admin,'Hủy','/admin/'+parent);
   }
 });
 await check('Admin password back returns to dashboard without signing customer out',async()=>{
  await admin.goto(base+'/admin/doi-mat-khau');await click(admin,'← Về quản trị','/admin');
  await user.goto(base+'/tai-khoan');await at(user,'/tai-khoan');
 });
 await check('Customer order flows through admin acceptance and delivery; receipt refresh matches',async()=>{
  const route='/admin'+orderRoute;await admin.goto(base+route);
  assert((await admin.locator('main').innerText()).includes('navigation@example.com'),'Wrong order contact');
  await admin.getByRole('button',{name:'Tiếp nhận và chuẩn bị'}).click();await at(admin,route);
  await user.goto(base+orderRoute);assert((await user.locator('.cart-summary').innerText()).includes('Đang chuẩn bị'),'Customer status not preparing');
  await admin.getByRole('button',{name:'Giao đơn và lập hóa đơn'}).click();await admin.waitForURL(/\/admin\/hoa-don\/\d+$/);
  await click(admin,'← Hóa đơn','/admin/hoa-don');await admin.goBack();assert(new URL(admin.url()).pathname.startsWith('/admin/hoa-don/'),'Browser Back did not return invoice');
  await user.reload();assert((await user.locator('.cart-summary').innerText()).includes('Đã giao'),'Customer status not delivered');
  await admin.goto(base+route);assert(await admin.getByRole('button',{name:'Giao đơn và lập hóa đơn'}).count()===0,'Delivered order can deliver twice');
  await click(admin,'← Danh sách đơn','/admin/don-hang');
 });
 await check('Admin logout plus browser Back cannot reopen protected content',async()=>{
  await admin.getByRole('button',{name:'Đăng xuất quản trị',exact:true}).click();await at(admin,'/admin/dang-nhap');
  await admin.goBack();await admin.reload();await admin.waitForURL(/\/admin\/dang-nhap/);
  assert(await admin.locator('.admin-sidebar').count()===0,'Admin content restored after logout');
 });
 await check('Customer logout plus browser Back requires login for account',async()=>{
  await user.goto(base+'/tai-khoan');await user.getByRole('navigation',{name:'Điều hướng chính'}).getByRole('button',{name:'Đăng xuất',exact:true}).click();await at(user,'/tai-khoan/dang-nhap');
  await user.goBack();await user.reload();await user.waitForURL(/\/tai-khoan\/dang-nhap/);
  assert(await user.getByRole('button',{name:'Đăng xuất',exact:true}).count()===0,'Customer session remained after logout');
 });
 assert(errors.length===0,errors.join('\n'));
 const report={date:new Date().toISOString(),checks,passed:checks.length,pageErrors:errors};
 const out=path.resolve(__dirname,'../../artifacts/navigation-check.json');fs.mkdirSync(path.dirname(out),{recursive:true});fs.writeFileSync(out,JSON.stringify(report,null,2));console.log(JSON.stringify(report));await browser.close();
})().catch(e=>{console.error(e);process.exit(1)});
