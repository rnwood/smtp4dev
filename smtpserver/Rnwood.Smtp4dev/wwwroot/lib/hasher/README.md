![hasher-logo.gif](https://github.com/millermedeiros/Hasher/raw/master/assets/hasher-logo.gif)

Hasher is a set of JavaScript functions to control browser history for rich-media websites and applications.
It works as an abstraction of browsers native methods plus some extra helper methods, it also has the advantage of dispatching Events when the history state change across multiple browsers (since this feature isn't supported by all of them).



## Why? ##

 - Browsers evolved since the other available solutions were created.
 - Some of the alternatives are way too complex, sometimes doing more things automatically than you actually want it to do.
 - Source code of most of the solutions are way too cryptic making it impossible to customize for your need or to debug it in case you find any issue.
 - Some of the solutions require extra markup and/or blank files to make it work.
 - The HTML5 History API is awesome but some for some kinds of applications using the
   `location.hash` may still be the recommended solution for saving application state.



## Goals ##

 - Be simple.
 - Work on the main browsers (IE6+, newest versions of Firefox, Safari, Opera and Chrome).
 - Clean source code, making it easy to debug/customize/maintain.
 - Follow best practices/standards.
 - Fully unit tested. ([tests](http://millermedeiros.github.com/Hasher/test/unit.html))
 - Don't break application if for some reason `location.hash` can't be updated.
   (it should still dispatch `changed` signal at each `hasher.setHash()`)



## Dependencies ##

 - **This library requires [JS-Signals](http://millermedeiros.github.com/js-signals/) to work.**



## Basic Example ##


### HTML ###

Include [JS-Signals](http://millermedeiros.github.com/js-signals/) and **hasher** to your HTML file:

```html
  <script type="text/javascript" src="signals.js"></script>
  <script type="text/javascript" src="hasher.js"></script>
```

**IMPORTANT:** `signals.js` should be included before `hasher.js`.


### JavaScript ###

```js
  //handle hash changes
  function handleChanges(newHash, oldHash){
    console.log(newHash);
  }

  hasher.changed.add(handleChanges); //add hash change listener
  hasher.initialized.add(handleChanges); //add initialized listener (to grab initial value in case it is already set)
  hasher.init(); //initialize hasher (start listening for history changes)

  hasher.setHash('foo'); //change hash value (generates new history record)
```


## Advanced Usage ##


### Hash Bang! ###

Google have a proposal for [making Ajax content crawlable](http://code.google.com/web/ajaxcrawling/docs/getting-started.html) by specifying that a certain *hash value* also have an static snapshot. Those *hash values* should start with an exclamation mark `!`:

```js
hasher.prependHash = '!'; //default value is "/"
hasher.setHash('foo'); //will update location.hash to "#!foo" -> htttp://example.com/#!foo
```

PS: Only use the hashbang if you are generating static snapshots for the hash.


### Setting hash value without dispatching changed signal ##

One of the greatest benefits of Hasher over other solutions is that it uses JS-Signals for the event dispatch, which provides [many advanced features](https://github.com/millermedeiros/js-signals/wiki/Examples). This can be useful when you are setting the hash value and your `changed` handler doesn't need to be called (e.g. updating hash value during scroll). Use it with care.

```js
function setHashSilently(hash){
  hasher.changed.active = false; //disable changed signal
  hasher.setHash(hash); //set hash without dispatching changed signal
  hasher.changed.active = true; //re-enable signal
}

hasher.init(); //start listening for changes
hasher.changed.add(console.log, console); //log all changes
hasher.setHash('foo');
setHashSilently('lorem/ipsum'); //set hash value without dispatching changed event (will generate history record anyway)
hasher.setHash('bar');
```


### Setting hash value without generating a new history record

Hasher also contains the method `replaceHash()`. It works very similarly to the
`setHash()` method (will also dispatch a `changed` signal), the main difference
it that it won't keep the **previous** hash on the history record (similar to
`location.replace()`). It's useful for redirections and any other change that
shouldn't be on the browser history.

```js
function onHasherInit(curHash){
  if (curHash == '') {
    // redirect to "home" hash without keeping the empty hash on the history
    hasher.replaceHash('home');
  }
}
hasher.initialized.add(onHasherInit);
hasher.changed.add(console.log, console); // log all hashes
hasher.init();
```


### Routes: Using Hasher together with Crossroads.js ###

Hasher is only focused on providing a reliable and clear API for setting hash values and
listening to hash state change event. If you need an advanced *routing* system
check [crossroads.js](http://millermedeiros.github.com/crossroads.js/). Both
were designed to work together easily:

```js
//setup crossroads
crossroads.addRoute('home');
crossroads.addRoute('lorem');
crossroads.addRoute('lorem/ipsum');
crossroads.routed.add(console.log, console); //log all routes

//setup hasher
function parseHash(newHash, oldHash){
  crossroads.parse(newHash);
}
hasher.initialized.add(parseHash); // parse initial hash
hasher.changed.add(parseHash); //parse hash changes
hasher.init(); //start listening for history change
```



## How does it work? ##

Hasher will listen for the browser `onhashchange` event if it is supported (FF3.6+, IE8+, Chrome 5+, Safari 5+, Opera 10.6+)
or it will fallback to pooling the `window.location` on an interval to check if
hash value changed. On IE 6-7 it also uses an hidden iframe to trigger
the history state changes (since updating the hash value won't do the trick).
This is the same method used by most of the other available solutions like swfaddress,
jQuery Address, YUI History, jqBBQ, Really Simple History, etc...

The main difference from the other solutions are the API, code structure and
the fact that it doesn't require jQuery/YUI/dojo/moootools/etc to work. It also
uses [JS-Signals](http://millermedeiros.github.com/js-signals/) for the events which
provides a sane way of handling events and some really useful advanced features.



## Why should I use it? ##

Besides the fact of making history state work across multiple browsers it also
normalizes and fixes many bugs, here are a few of the advantges:

 * Normalizes the hash value across browsers (firefox decode hash value and
   all the other browsers don't).
 * Fix IE8 bug if `location.hash` contains a "?" character and file is being
   accessed locally it would break the history stack. [iss #6]
 * Fix Safari 4-5 bug while setting `location.hash` to a value that contain
   non-printable ASCII chars (non-latin, accents, etc..). [iss #8]
 * Degrade gracefully if for some reason `location.hash` isn't available, will
   dispatch the `changed` signal at each `hasher.setHash()` and application
   can still work, it just won't generate a new history record.
 * Doesn't rely on callbacks so you can add as many listeners as you want and
   since it uses [JS-Signals](http://millermedeiros.github.com/js-signals/)
   for the event system it also provides many advanced featured that wouldn't
   be available through a simple callback system, like disabling the dispatch
   of an event (so you can change the hash value without affecting your app
   state), removing all the listeners at once, dispose objects, etc...
 * Option to start/stop pooling/listening for changes on the hash whenever you
   want giving more control over how you app is supposed to work.
 * Available as an AMD module which can be easily integrated into other
   projects without polluting the global scope or affecting you aplication
   structure.
 * Isn't a plugin for a large JS library/framework (so you can use it with
   *any* library).
 * Can be easily integrated into a Router like
   [crossroads.js](http://millermedeiros.github.com/crossroads.js/).
 * Sometimes regular URLs doesn't make any sense, specially when you *can't*
   provide a fallback to all of them or when you just want to save the state of
   the application and that change wouldn't make sense on a full page reload
   (scrolling through the same page, interactive slideshow, etc..), also some
   content may not need to be indexed by search engines (although you can use
   *hashbangs* to make [Ajax content crawlable](http://code.google.com/web/ajaxcrawling/docs/getting-started.html)...).
   **Each scenario requires a different approach, be pragmatic.**
 * Clean API.



## Documentation ##

Documentation can be found inside the `dist/docs` folder or at [http://millermedeiros.github.com/Hasher/docs/](http://millermedeiros.github.com/Hasher/docs/).



## Unit Tests ##

Hasher is *usually* tested on IE (6,7,8,9), FF (3.6, 4.0, 5.0+ - mac/pc),
Chrome (latest stable - mac/pc), Safari Mac (4.3, 5.0) and Opera (latest - mac/pc).

You can also run the test by yourself at [http://millermedeiros.github.com/Hasher/test/unit.html](http://millermedeiros.github.com/Hasher/test/unit.html)



## Repository Structure ##

### Folder Structure ###

    dev       ->  development files
    |- build        ->  files used on the build process
    |- lib          ->  3rd-party libraries
    |- src          ->  source files
    |- tests        ->  unit tests
    dist      ->  distribution files
    |- docs         ->  documentation
    |- js           ->  javascript files

### Branches ###

    master      ->  always contain code from the latest stable version
    release-**  ->  code canditate for the next stable version (alpha/beta)
    dev         ->  main development branch (nightly)
    gh-pages    ->  project page
    **other**   ->  features/hotfixes/experimental, probably non-stable code



## Distribution Files ##

Files inside `dist/js` folder.

 * hasher.js : Uncompressed source code with comments. Works as a plain script
   or can be loaded by an AMD loader like [RequireJS](http://requirejs.org/)
   without generating any global variables.
 * hasher.min.js : Compressed code.

Documentation is inside the `dist/docs` folder.



## Building your own ##

This project uses [Apache Ant](http://ant.apache.org/) for the build process. If for some reason you need to build a custom version install Ant and run:

    ant compile

This will delete all JS files inside the `dist` folder, merge/update/compress source files and copy the output to the `dist` folder.

    ant deploy

This will delete all files inside *dist* folder, is runs `ant compile` and generate documentation files.

**IMPORTANT:** `dist` folder always contain the latest version, regular users should **not** need to run build task.



## License ##

Released under the [MIT license](http://www.opensource.org/licenses/mit-license.php).



## Important ##

 - Weird case scenarios like calling methods from inside (i)frame, wrong doctype,
   plugins, 3rd party code, etc, **MAY** prevent script from working properly.
 - Hasher was designed on a way that it will still dispatch the
   `changed` signal even if it can't update the browser `location.hash`, so
   application should keep working even if back/prev buttons doesn't work as
   expected.
 - Consider using the new [HTML5 history API](http://robertnyman.com/2011/08/03/html5-history-api-and-improving-end-user-experience/)
   if normal URLs would make sense on the kind of site/application you are building and
   you have static fallbacks for all of them (in some cases that may not be
   possible or even a good option). [History.js](https://github.com/balupton/history.js)
   is probably the most used *polyfill* for the History API, check it out.


&copy; [Miller Medeiros](http://www.millermedeiros.com)
