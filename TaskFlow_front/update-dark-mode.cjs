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
        .replace(/bg-white([^/]|$)/g, 'bg-white dark:bg-gray-800 dark:text-gray-100$1')
        .replace(/bg-gray-50([^/]|$)/g, 'bg-gray-50 dark:bg-gray-900$1')
        .replace(/border-gray-100([^/]|$)/g, 'border-gray-100 dark:border-gray-700$1')
        .replace(/border-gray-200([^/]|$)/g, 'border-gray-200 dark:border-gray-700$1')
        .replace(/border-gray-300([^/]|$)/g, 'border-gray-300 dark:border-gray-600$1')
        .replace(/text-gray-900([^/]|$)/g, 'text-gray-900 dark:text-gray-50$1')
        .replace(/text-gray-800([^/]|$)/g, 'text-gray-800 dark:text-gray-100$1')
        .replace(/text-gray-600([^/]|$)/g, 'text-gray-600 dark:text-gray-300$1')
        .replace(/text-gray-500([^/]|$)/g, 'text-gray-500 dark:text-gray-400$1');
      if (content !== newContent) {
        fs.writeFileSync(fullPath, newContent);
      }
    }
  }
}

processDir('src');
console.log('Done modifying files for dark mode.');
