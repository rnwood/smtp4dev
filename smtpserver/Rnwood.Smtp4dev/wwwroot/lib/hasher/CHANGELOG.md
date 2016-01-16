Hasher Changelog
================

v1.2.0 (2013/11/11)
-------------------

 - hasher.raw for percent-encoded strings (#59, thanks @mwoc)
 - wrap decodeURIComponent into `try/catch` (#57)
 - bower.json


v1.1.4 (2013/04/03)
-------------------

 - node.js compliance + package.json (#52)


v1.1.3 (2013/03/14)
-------------------

 - escape RegExp on `trimHash` to avoid removing `$` from hash value (#49)
 - use unnamed AMD module for greater portability (#51)


v1.1.2 (2012/10/31)
-------------------

 - fix unnecessary "changed" events during consecutive redirects (#39)
 - fix hash containing "%" (#42)


v1.1.1 (2012/10/25)
-------------------

 - fix iOS5 bug when going to a new page and coming back afterwards, caused by
   cached reference to an old instance of the `window.location`. (#43)
 - fix IE compatibility mode. (#44).


v1.1.0 (2011/11/01)
-------------------

 - add `hasher.replaceHash()` (#35)
 - `hasher.initialized.memorize = true` avoid issues if adding listener after
   `initialized` already dispatched if using signals 0.7.0+. (#33)
 - single distribution file for AMD and plain browser. (#34)


v1.0.0 (2011/08/03)
-------------------

 - initial public release.
