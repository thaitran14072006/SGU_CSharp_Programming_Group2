// Destructive interaction checks run only against the disposable 5088 fixture.
const {chromium}=require('playwright');
const path=require('path'),fs=require('fs');
(async()=>{
 const base=process.env.MOCBAKERY_PREVIEW_URL||'http://127.0.0.1:5088';
 if(!['http://127.0.0.1:5088','http://localhost:5088'].includes(base))throw Error('Use isolated fixture only');
 const browser=await chromium.launch({headless:true,channel:'msedge'}),context=await browser.newContext({viewport:{width:1440,height:960}}),page=await context.newPage();
 const errors=[],checks=[],out=path.resolve(__dirname,'../../artifacts/admin-actions');fs.mkdirSync(out,{recursive:true});
 page.on('pageerror',e=>errors.push(e.message));
 const assert=(ok,message)=>{if(!ok)throw Error(message)};
 const check=async(name,fn)=>{await fn();checks.push(name);console.log('PASS '+name)};
 const modal=page.getByRole('dialog');
 await page.goto(base+'/admin/dang-nhap');await page.getByLabel('Tên đăng nhập').fill('owner');await page.getByLabel('Mật khẩu',{exact:true}).fill('PublicTest123!');await page.getByRole('button',{name:'Vào trang quản trị'}).click();await page.waitForURL(base+'/admin');
 const category='Danh mục UX '+Date.now();let id;
 await check('Ordinary saves stay fast; repeated submission creates one record',async()=>{
  await page.goto(base+'/admin/danh-muc/them');await page.getByLabel('Tên danh mục *',{exact:true}).fill(category);
  await page.route('**/admin/danh-muc/luu',async route=>{await new Promise(r=>setTimeout(r,400));await route.continue()});
  const states=[];await page.exposeFunction('observeAdminSubmit',state=>states.push(state));
  await page.evaluate(()=>{let observed=false;document.addEventListener('submit',event=>{if(observed)return;observed=true;const form=event.target;const duplicate=new Event('submit',{bubbles:true,cancelable:true});form.dispatchEvent(duplicate);window.observeAdminSubmit({busy:form.getAttribute('aria-busy')==='true',duplicateBlocked:duplicate.defaultPrevented});});});
  let posts=0;page.on('request',r=>{if(r.method()==='POST'&&r.url()===base+'/admin/danh-muc/luu')posts++});
  await page.getByRole('button',{name:'Lưu danh mục',exact:true}).click();
  await page.waitForURL(base+'/admin/danh-muc');assert(posts===1,'Duplicate save request');assert(states.length===1&&states[0].busy&&states[0].duplicateBlocked,'Busy state or duplicate protection missing');
  const row=page.getByRole('row').filter({hasText:category});assert(await row.count()===1,'Saved category missing or duplicated');id=(await row.getByRole('link',{name:'Sửa',exact:true}).getAttribute('href')).match(/\/(\d+)\/sua/)[1];
 });
 await check('Delete explains the exact record; cancel and Escape preserve it and restore focus',async()=>{
  const button=page.getByRole('row').filter({hasText:category}).getByRole('button',{name:'Xóa nếu chưa dùng',exact:true});
  await button.click();assert((await modal.innerText()).includes(category),'Wrong confirmation target');
  assert(await modal.getByRole('button',{name:'Quay lại',exact:true}).evaluate(el=>el===document.activeElement),'Destructive action received initial focus');
  await page.screenshot({path:path.join(out,'confirmation-1440.png')});
  await page.setViewportSize({width:375,height:850});assert(!await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth),'Dialog overflows mobile');await page.screenshot({path:path.join(out,'confirmation-375.png')});
  await modal.getByRole('button',{name:'Quay lại',exact:true}).click();assert(await button.evaluate(el=>el===document.activeElement),'Cancel lost focus');
  await button.click();await page.keyboard.press('Escape');assert(!await modal.isVisible()&&await page.getByRole('row').filter({hasText:category}).count()===1,'Escape deleted category');
 });
 await check('Confirm delete submits once and returns a success result',async()=>{
  let deletes=0;page.on('request',r=>{if(r.method()==='POST'&&r.url()===base+`/admin/danh-muc/${id}/xoa`)deletes++});
  await page.getByRole('row').filter({hasText:category}).getByRole('button',{name:'Xóa nếu chưa dùng',exact:true}).click();await modal.getByRole('button',{name:'Xóa danh mục',exact:true}).dblclick();
  await page.getByRole('status').filter({hasText:'Đã xóa'}).waitFor();assert(deletes===1&&await page.getByRole('row').filter({hasText:category}).count()===0,'Delete repeated or record still present');
 });
 await check('A category already used by cakes cannot be deleted and shows an explanation',async()=>{
  const row=page.getByRole('row').filter({hasText:'Bánh mì'});await row.getByRole('button',{name:'Xóa nếu chưa dùng',exact:true}).click();await modal.getByRole('button',{name:'Xóa danh mục',exact:true}).click();
  await page.getByRole('alert').filter({hasText:'không thể xóa'}).waitFor();assert(await row.count()===1,'Used category deleted');
 });
 await check('Invalid input never opens a confirmation or submits',async()=>{
  await page.goto(base+'/admin/ton-kho/dieu-chinh?kind=NguyenLieu');await page.getByRole('button',{name:'Ghi nhận xuất kho',exact:true}).click();assert(!await modal.isVisible(),'Invalid form opened confirmation');assert(new URL(page.url()).pathname==='/admin/ton-kho/dieu-chinh','Invalid form navigated');
 });
 await check('Stopping a cake asks confirmation; cancel preserves selling state',async()=>{
  await page.setViewportSize({width:1440,height:960});await page.goto(base+'/admin/banh');const row=page.getByRole('row').filter({hasText:'Croissant'}).first();await row.getByRole('button',{name:'Ngừng bán',exact:true}).click();
  assert((await modal.innerText()).includes('Lịch sử đơn hàng'),'Stop does not explain history');await modal.getByRole('button',{name:'Quay lại',exact:true}).click();assert(await row.getByRole('button',{name:'Ngừng bán',exact:true}).isEnabled(),'Cancel changed selling state');
 });
 assert(errors.length===0,errors.join('\n'));console.log(JSON.stringify({passed:checks.length,pageErrors:errors,fixtureOnly:true}));await browser.close();
})().catch(e=>{console.error(e);process.exit(1)});
