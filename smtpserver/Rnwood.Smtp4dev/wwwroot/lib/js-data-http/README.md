<img src="https://raw.githubusercontent.com/js-data/js-data/master/js-data.png" alt="js-data logo" title="js-data" align="right" width="96" height="96" />

# js-data-http

[![Slack Status][sl_b]][sl_l]
[![npm version][npm_b]][npm_l]
[![Circle CI][circle_b]][circle_l]
[![npm downloads][dn_b]][dn_l]
[![Coverage Status][cov_b]][cov_l]
[![Codacy][cod_b]][cod_l]

HTTP adapter for [js-data](http://www.js-data.io/).

To get started, visit __[http://js-data.io](http://www.js-data.io)__.

## Table of contents

* [Quick start](#quick-start)
* [Guides and Tutorials](#guides-and-tutorials)
* [API Reference Docs](#api-reference-docs)
* [Community](#community)
* [Support](#support)
* [Contributing](#contributing)
* [License](#license)

## Quick Start
`npm install --save js-data js-data-http` or `bower install --save js-data js-data-http`.

Load `js-data-http.js` after `js-data.js`.

```js
var adapter = new DSHttpAdapter();

var store = new JSData.DS();
store.registerAdapter('http', adapter, { default: true });

// "store" will now use the http adapter for all async operations
```

## Guides and Tutorials

[Get started at http://js-data.io](http://js-data.io)

## API Reference Docs

[Visit http://api.js-data.io](http://api.js-data.io).

## Community

[Explore the Community](http://js-data.io/docs/community).

## Support

[Find out how to Get Support](http://js-data.io/docs/support).

## Contributing

[Read the Contributing Guide](http://js-data.io/docs/contributing).

## License

The MIT License (MIT)

Copyright (c) 2014-2016 js-data-http project authors

* [LICENSE](https://github.com/js-data/js-data-http/blob/master/LICENSE)
* [AUTHORS](https://github.com/js-data/js-data-http/blob/master/AUTHORS)
* [CONTRIBUTORS](https://github.com/js-data/js-data-http/blob/master/CONTRIBUTORS)

[sl_b]: http://slack.js-data.io/badge.svg
[sl_l]: http://slack.js-data.io
[npm_b]: https://img.shields.io/npm/v/js-data-http.svg?style=flat
[npm_l]: https://www.npmjs.org/package/js-data-http
[circle_b]: https://img.shields.io/circleci/project/js-data/js-data-http/master.svg?style=flat
[circle_l]: https://circleci.com/gh/js-data/js-data-http/tree/master
[dn_b]: https://img.shields.io/npm/dm/js-data-http.svg?style=flat
[dn_l]: https://www.npmjs.org/package/js-data-http
[cov_b]: https://img.shields.io/coveralls/js-data/js-data-http/master.svg?style=flat
[cov_l]: https://coveralls.io/github/js-data/js-data-http?branch=master
[cod_b]: https://img.shields.io/codacy/3931bbd8d838463297f70640aa78251b.svg
[cod_l]: https://www.codacy.com/app/jasondobry/js-data-http/dashboard