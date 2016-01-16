var fs = require('fs');
var pkg = require('../package.json');

console.log('Adding version to dist/ files...');

function version(filepath) {
  var file = fs.readFileSync(filepath, {
    encoding: 'utf-8'
  });

  file = file.replace(/<%= pkg\.version %>/gi, pkg.version);

  var parts = pkg.version.split('-');
  var numbers = parts[0].split('.');

  file = file.replace(/<%= major %>/gi, numbers[0]);
  file = file.replace(/<%= minor %>/gi, numbers[1]);
  file = file.replace(/<%= patch %>/gi, numbers[2]);

  if (pkg.version.indexOf('alpha') !== -1) {
    file = file.replace(/<%= alpha %>/gi, parts[1].replace('alpha.', '') + (parts.length > 2 ? '-' + parts[2] : ''));
  }
  else {
    file = file.replace(/<%= alpha %>/gi, false);
  }

  if (pkg.version.indexOf('beta') !== -1) {
    file = file.replace(/<%= beta %>/gi, parts[1].replace('beta.', '') + (parts.length > 2 ? '-' + parts[2] : ''));
  }
  else {
    file = file.replace(/<%= beta %>/gi, false);
  }

  fs.writeFileSync(filepath, file, {
    encoding: 'utf-8'
  });
}

version('dist/js-data-debug.js');
version('dist/js-data.js');

console.log('Done!');
