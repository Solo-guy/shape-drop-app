// Đổi mã định danh app (package name / bundle ID) ở mọi chỗ cần thiết.
// Cách dùng:  node scripts/set-app-id.mjs com.tencongty.shapedrop
import { readFileSync, writeFileSync } from 'node:fs';

const id = process.argv[2];
if (!id || !/^[a-z][a-z0-9_]*(\.[a-z][a-z0-9_]*){1,}$/.test(id)) {
  console.error('Mã không hợp lệ. Ví dụ: node scripts/set-app-id.mjs com.tencongty.shapedrop');
  process.exit(1);
}
const edit = (file, fn) => { const s = readFileSync(file, 'utf8'); const t = fn(s); writeFileSync(file, t); console.log('✔', file); };

edit('capacitor.config.json', s => { const j = JSON.parse(s); j.appId = id; return JSON.stringify(j, null, 2) + '\n'; });
edit('android/app/build.gradle', s => s.replace(/applicationId "[^"]+"/, `applicationId "${id}"`));
edit('ios/App/App.xcodeproj/project.pbxproj', s => s.replace(/PRODUCT_BUNDLE_IDENTIFIER = [^;]+;/g, `PRODUCT_BUNDLE_IDENTIFIER = ${id};`));
console.log(`\nXong. Mã app mới: ${id}\nLưu ý: sau khi đã đưa app lên store thì KHÔNG được đổi mã này nữa.`);
