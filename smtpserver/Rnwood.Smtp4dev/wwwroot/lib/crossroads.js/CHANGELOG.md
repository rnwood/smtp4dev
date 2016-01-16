# Crossroads.js Changelog #

## v0.12.1 (2015/07/08) ##

 - allow multiple query parameters with same name (#128)

## v0.12.0 (2013/01/21) ##

 - improve `Route.interpolate()` to support query strings. (#76)
 - make it possible to have a different patternLexer per router. (#67)
 - add trailing/leading ";" to crossroads.min.js to avoid concat issues. (#73)
 - improve UMD wrapper so crossroads.min.js should also work with r.js.


## v0.11.0 (2012/10/31) ##

### API Changes

 - add `crossroads.pipe()` and `crossroads.unpipe()` (#70)
 - added way to toggle case sensitivity `crossroads.ignoreCase`, default is
   `true` (#53)
 - add `crossroads.ignoreState`. (#57)

### Improvements

 - `decodeQueryString()` now respects `shouldTypecast` (#71)
 - changed `Route.rules` array validation to be case insensitive if
   `crossroads.ignoreCase = true` (#49)



## v0.10.0 (2012/08/12) ##

### Improvements

 - Avoid dispatching the routed/bypassed/matched signals if passing same
   request in subsequent calls. (#57)
 - Add `crossroads.resetState()` (#66)



## v0.9.1 (2012/07/29) ##

### Fixes

 - Normalize optional segments behavior on IE 7-8 (#58, #59, #60)
 - Fix `captureVals` on IE 7-8, make sure global flag works properly (#61, #62,
   #63)

### Improvements

 - `Route.interpolate()` accepts Numbers as segments. (#54)


## v0.9.0 (2012/05/28) ##

### API Changes ###

 - added `crossroads.greedy` (#46)
 - added `crossroads.greedyEnabled` (#46)
 - added `crossroads.patternLexer.strict()` and
   `crossroads.patternLexer.loose()` and
   `crossroads.patternLexer.legacy()` (#35)
 - added `Route.interpolate()` (#34)
 - added query string support (#33)

### Fixes

 - `Route.switched` is only dispatched if matching a different route. (#50)

### Other

 - change default behavior of slashes at begin/end of request (#35)
 - query string support affected old segment rules, now `?` is considered as
   a segment divisor as `/` otherwise optional query string RegExp wouldn't
   match proper segment if following a required segment. (#33)



## v0.8.0 (2012/03/05) ##

### API Changes ###

 - added `Route.switched` (#37)
 - added `crossroads.NORM_AS_ARRAY`, `crossroads.NORM_AS_OBJECT` (#31)
 - added option to pass default arguments to `crossroads.parse()` (#44)
 - added rest segments support (#43)

### Other ###

 - change build to Node.js
 - change minifier to UglifyJS.



## v0.7.1 (2012/01/06) ##

### Fixes ###

 - avoid calling `rules.normalize_` during validation step (#39)



## v0.7.0 (2011/11/02) ##

### API Changes ###

 - added `crossroads.normalizeFn` (#31)
 - added `vals_` Array to values object passed to `normalize_` and
   `crossroads.normalizeFn` to increase flexibility. (#31)
 - added `Route.greedy` support. (#20)
 - changed parameters dispatched by `crossroads.routed` signal, passes request
   as first param and a data object as second param. (#20)

### Other ###

 - improve parameter typecasting. (#32)
 - refactoring for better code compression and also simplified some logic to
   increase code readability.



## v0.6.0 (2011/08/31) ##

## API Changes ##

 - changed `crossroads.shouldTypecast` default value to `false` (#23)
 - added magic rule to normalize route params before dispatch `rules.normalize_`. (#21)
 - added crossroads.VERSION

### Fixes ###

 - fix optional "/" between required params. (#25)
 - only test optional params if value != null. (#26)
 - fix CommonJS wrapper, wasn't exporting crossroads properly (#27)

### Other ###

 - Migrated unit tests from YUI to Jasmine to allow testing on nodejs and also
   because it runs locally and gives better error messages. Increased a lot the
   number of tests that helped to spot a few edge cases. (#5)
 - Changed wrapper to generate a single distribution file that runs on all
   environments. (#27)



## v0.5.0 (2011/08/17) ##

### API Changes ###

 - added numbered rules for RegExp pattern and alias to segments (#16)
 - added support to optional segments (#17)
 - added property `crossroads.shouldTypecast` to enable/disable typecasting
   segments values. (#18)
 - added support to multiple instances (#19)

### Other ###

 - Refactored `crossroads` core object to make it cleaner.

### Fixes ###

 - fix trailing slash before optional param (#22)



## v0.4 (2011/06/06) ##

### API Changes ###

 - added magic rule to validate whole request `rules.request_`. (#14)

### Other ###

 - changed behavior of trailing slash at the end of string pattern so it doesn't affect route anymore (#12).
 - added NPM package.



## v0.3 (2011/05/03) ##

### API Changes ###

 - added support for RegExp route pattern. (#8)
 - added signal `routed` to crossroads. (#9)
 - added commonjs module wrapper.



## v0.2 (2011/04/14) ##

### API Changes ###

 - added priority param to `addRoute`. (#2)

### Other ###

 - added "js-signals" as module dependency on AMD version.



## v0.1.1 (2011/04/14) ##

### Fixes ###

 - safe guarded from empty `parse` calls.



## v0.1 (2011/04/14) ##

 - initial release with basic features support.
