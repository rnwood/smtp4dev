var fs = require('fs');
var pkg = require('../package.json');

console.log('Creating dist/js-data-debug.js...');

var file = fs.readFileSync('dist/js-data-debug.js', { encoding: 'utf-8' });

var lines = file.split('\n');

var newLines = [];

lines.forEach(function (line) {
  if (line.indexOf('logFn(') === -1) {
    newLines.push(line);
  }
});

file = newLines.join('\n');

file += '\n';

fs.writeFileSync('dist/js-data.js', file, { encoding: 'utf-8' });

console.log('Done!');
