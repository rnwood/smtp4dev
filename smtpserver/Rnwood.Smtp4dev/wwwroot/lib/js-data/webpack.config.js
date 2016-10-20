var webpack = require('webpack');
var path = require('path');
var pkg = JSON.parse(require('fs').readFileSync('package.json'));
var banner = 'js-data\n' +
  '@version ' + pkg.version + ' - Homepage <http://www.js-data.io/>\n' +
  '@author Jason Dobry <jason.dobry@gmail.com>\n' +
  '@copyright (c) 2014-2016 Jason Dobry \n' +
  '@license MIT <https://github.com/js-data/js-data/blob/master/LICENSE>\n' +
  '\n' +
  '@overview Robust framework-agnostic data store.';

module.exports = {
  devtool: 'source-map',
  entry: './src/index.js',
  output: {
    filename: './dist/js-data-debug.js',
    libraryTarget: 'umd',
    library: 'JSData'
  },
  module: {
    loaders: [
      {
        loader: 'babel-loader',
        include: [
          path.resolve(__dirname, 'src')
        ],
        test: /\.js$/
      }
    ]
  },
  plugins: [
    new webpack.BannerPlugin(banner)
  ]
};
