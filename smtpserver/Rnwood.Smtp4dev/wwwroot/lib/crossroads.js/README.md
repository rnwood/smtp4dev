[![Build Status](https://secure.travis-ci.org/millermedeiros/crossroads.js.svg)](https://travis-ci.org/millermedeiros/crossroads.js)

---

![Crossroads - JavaScript Routes](https://github.com/millermedeiros/crossroads.js/raw/master/_assets/crossroads_logo.png)


## Introduction ##

Crossroads.js is a routing library inspired by URL Route/Dispatch utilities present on frameworks like Rails, Pyramid, Django, CakePHP, CodeIgniter, etc...
It parses a string input and decides which action should be executed by matching the string against multiple patterns.

If used properly it can reduce code complexity by decoupling objects and also by abstracting navigation paths.

See [project page](http://millermedeiros.github.com/crossroads.js/) for documentation and more details.




## Links ##

 - [Project page and documentation](http://millermedeiros.github.com/crossroads.js/)
 - [Usage examples](https://github.com/millermedeiros/crossroads.js/wiki/Examples)
 - [Changelog](https://github.com/millermedeiros/crossroads.js/blob/master/CHANGELOG.md)



## Dependencies ##

**This library requires [JS-Signals](http://millermedeiros.github.com/js-signals/) to work.**



## License ##

[MIT License](http://www.opensource.org/licenses/mit-license.php)



## Distribution Files ##

Files inside `dist` folder.

 * crossroads.js : Uncompressed source code with comments.
 * crossroads.min.js : Compressed code.

You can install Crossroads on Node.js using [NPM](http://npmjs.org/)

    npm install crossroads



## Repository Structure ##

### Folder Structure ###

    dev       ->  development files
    |- lib          ->  3rd-party libraries
    |- src          ->  source files
    |- tests        ->  unit tests
    dist      ->  distribution files

### Branches ###

    master      ->  always contain code from the latest stable version
    release-**  ->  code canditate for the next stable version (alpha/beta)
    dev         ->  main development branch (nightly)
    gh-pages    ->  project page
    **other**   ->  features/hotfixes/experimental, probably non-stable code



## Building your own ##

This project uses [Node.js](http://nodejs.org/) for the build process. If for some reason you need to build a custom version install Node.js and run:

    node build

This will delete all JS files inside the `dist` folder, merge/update/compress source files and copy the output to the `dist` folder.

**IMPORTANT:** `dist` folder always contain the latest version, regular users should **not** need to run build task.



## Running unit tests ##

### On the browser ###

Open `dev/tests/spec_runner-dist.html` on your browser.

`spec_runner-dist` tests `dist/crossroads.js` and `spec_runner-dev` tests files inside
`dev/src` - they all run the same specs.


### On Node.js ###

Install [npm](http://npmjs.org) and run:

```
npm install --dev
npm test
```

Each time you run `npm test` the files inside the `dist` folder will be updated
(it executes `node build` as a `pretest` script).
