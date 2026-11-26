const {chromium}=require('playwright');(async()=>{const b=await chromium.launch();
for(const [f,w,h] of [['main',1302,822],['login',302,202]]){const p=await b.newPage({viewport:{width:w,height:h},deviceScaleFactor:1});await p.goto('file://'+process.cwd()+'/'+f+'.html');await p.screenshot({path:f+'.png'});}
await b.close()})()
