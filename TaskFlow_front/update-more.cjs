const fs = require('fs');
const path = require('path');
function processDir(dir) {
  const files = fs.readdirSync(dir);
  for (const file of files) {
    const fullPath = path.join(dir, file);
    if (fs.statSync(fullPath).isDirectory()) {
      processDir(fullPath);
    } else if (fullPath.endsWith('.tsx') || fullPath.endsWith('.ts')) {
      let content = fs.readFileSync(fullPath, 'utf8');
      let newContent = content
        .replace(/text-gray-700([^/]|$)/g, 'text-gray-700 dark:text-gray-200$1')
        .replace(/bg-gray-100([^/]|$)/g, 'bg-gray-100 dark:bg-gray-700$1')
        .replace(/bg-gray-200([^/]|$)/g, 'bg-gray-200 dark:bg-gray-600$1')
        .replace(/bg-gray-50([^/]|$)/g, 'bg-gray-50 dark:bg-gray-800$1');
      if (content !== newContent) {
        fs.writeFileSync(fullPath, newContent);
      }
    }
  }
}
processDir('src');
console.log('done updating more colors');
