# JS-Signals Changelog #


## v1.0.0 (2012/11/29) ##

 - bump version! API is stable for a long time. (#52)



## v0.9.0 (2012/10/31) ##

 - auto bind `Signal.dispatch()` context. (#47)
 - add `SignalBinding.getSignal()`. (#48)



## v0.8.1 (2012/07/31) ##

 - fix AMD `define()` bug introduced on v0.8.0 (#46)



## v0.8.0 (2012/07/31) ##

 - expose Signal constructor for brevity instead of namespace while still
   keeping an alias for backwards compatibility. (#44)



## v0.7.4 (2012/02/24) ##

### Fixes ###

 - changed UMD to use unnamed define.(#41)



## v0.7.3 (2012/02/02) ##

### Fixes ###

 - `remove()` and `has()` now accepts the `context` as second argument. (#40)



## v0.7.2 (2012/01/12) ##

### Fixes ###

 - allow `add()` and `addOnce()` same method multiple times if passing
   different context. (#39)



## v0.7.1 (2011/11/29) ##

 - Improve `dispatch()` performance if `Signal` doesn't have any listeners.



## v0.7.0 (2011/11/02) ##

### API changes ###

 - Added:
   - `Signal.memorize`. (#29)
   - `Signal.forget()`. (#29)
   - `Signal.has()`. (#35)

### Other ###

 - changed the way the code is wrapped to have a single distribution file for
   all the environments. (#33)



## v0.6.3 (2011/07/11) ##

### Fixes ###

 - improved `SignalBinding.detach()` behavior. (#25)

### API changes ###

 - Added:
   - `SignalBinding.prototype.isBound()` (#25)
   - `SignalBinding.params` (#28)

 - Removed:
   - `SignalBinding.prototype.dispose()` (#27)

### Other ###

 - minor code cleaning, better error messages.



## v0.6.2 (2011/06/11) ##

### Fixes ###

 - removing a listener during dispatch was causing an error since listener was
   undefined. (#24 - thanks @paullewis)

### Other ###

 - minor code cleaning.
 - renamed distribution files to "signals.js" (#22)



## v0.6.1 (2011/05/03) ##

 - added NPM package.json and CommmonJS wrapper for NPM distribution. (#21 - thanks @tomyan)



## v0.6 (2011/04/09) ##

### API changes ###

 - Added:
   - `Signal.active`
   - `SignalBinding.active`

 - Removed:
   - `Signal.protytpe.enable()`
   - `Signal.protytpe.disable()`
   - `Signal.protytpe.isEnabled()`
   - `SignalBinding.protytpe.enable()`
   - `SignalBinding.protytpe.disable()`
   - `SignalBinding.protytpe.isEnabled()`

### Other ###

 - created AMD wrapped version.
 - switched from "module pattern" to a closure with a global export.



## v0.5.3 (2011/02/21) ##

### API changes ###

 - added priority parameter to `add` and `addOnce`.

### Other ###

 - improved code structure.



## v0.5.2 (2011/02/18) ##

### Other ###

 - changed to a module pattern.
 - added YUI test coverage.
 - improved build and src files structure.
 - simplified `remove`, `removeAll`, `add`.
 - improved error messages.



## v0.5.1 (2011/01/30) ##

### API changes ###

 - made `SignalBinding` constructor private. (#15)
 - changed params order on `SignalBinding` constructor.
 - removed `signals.isDef()`. (#14)

### Other ###

 - added JSLint to the build process. (#12)
 - validated source code using JSLint. (#13)
 - improved docs.



## v0.5 (2010/12/03) ##

### API changes ###

 - Added:
   - `SignalBinding.prototype.getListener()` (#3)
   - `Signal.prototype.dispose()` (#6)
   - `signals.VERSION`
   - `signals.isDef()`

 - Removed:
   - `SignalBinding.listener` (#3)

 - Renamed:
   - `SignalBinding.listenerScope` -> `SignalBinding.context` (#4)

### Fixes ###

 - Removed unnecessary function names (#5)
 - Improved `remove()`, `removeAll()` to dispose binding (#10)

### Test Changes ###

 - Added different HTML files to test dev/dist/min files.
 - Updated test cases to match new API.

### Other ###

 - Improved source code comments and documentation.
 - Small refactoring for better organization and DRY.
 - Added error messages for required params.
 - Removed unnecessary info from `SignalBinding.toString()`.



## v0.4 (2010/11/27) ##

### API changes ###

 - Added:
   - `SignalBinding.prototype.detach()`
   - `SignalBinding.prototype.dispose()`

### Test Changes ###

 - Added test cases for `detach` and `dispose`.

### Other ###

 - Improved docs for a few methods.
 - Added internal method `Signal.prototype._addBinding()`.



## v0.3 (2010/11/27) ##

### API changes ###

 - Renamed:
   - `Signal.prototype.stopPropagation()` -> `Signal.prototype.halt()`
   - `Signal.prototype.pause()` -> `Signal.prototype.disable()`
   - `Signal.prototype.resume()` -> `Signal.prototype.enable()`
   - `Signal.prototype.isPaused()` -> `Signal.prototype.isEnabled()`
   - `SignalBinding.prototype.pause()` -> `SignalBinding.prototype.disable()`
   - `SignalBinding.prototype.resume()` -> `SignalBinding.prototype.enable()`
   - `SignalBinding.prototype.isPaused()` -> `SignalBinding.prototype.isEnabled()`

### Fixes ###

 - Calling `halt()` before/after `dispatch()` doesn't affect listeners execution anymore, `halt()` only works during propagation.

### Test Changes ###

 - updated API calls to reflect new method names.
 - added tests that match `halt()` before/after `dispatch()`.

### Other ###

Added inline documentation to source code and included an HTML version of the documentation together with distribution files.



## v0.2 (2010/11/26) ##

### API changes ###

 - Added:
   - `Signal.prototype.pause()`
   - `Signal.prototype.resume()`
   - `Signal.prototype.isPaused()`
   - `Signal.prototype.stopPropagation()`

### Fixes ###

 - `SignalBinding.prototype.isPaused()`

### Test Changes ###

 - Increased test coverage a lot.
 - Tests added:
   - pause/resume (for individual bindings and signal)
   - stopPropagation (using `return false` and `Signal.prototype.stopPropagation()`)
   - `SignalBindings.prototype.isOnce()`
   - if same listener added twice returns same binding

### Other ###

Small refactoring and code cleaning.



## v0.1 (2010/11/26) ##

 - initial release, support of basic features.
