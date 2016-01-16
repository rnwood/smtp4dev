##### 2.1.2 - 28 October 2015

###### Other
- Dropped Grunt
- Now reporting code coverage properly

##### 2.1.1 - 20 September 2015

###### Backwards compatible bug fixes
- #18 - Cannot read property 'http' of undefined
- #27 - logResponse doesn't reject when this.http() rejects with something that is not an Error

##### 2.1.0 - 11 September 2015

###### Backwards compatible API changes
- #20 - DSHttpAdapter.POST does not pick DSHttpAdapter.defaults.basePath
- #25 - Allow urlPath override for httpAdapter PR by @internalfx
- #26 - Add support for full url override

###### Backwards compatible bug fixes
- #21 - Cannot read property 'method' of undefined
- #22 - Fixing issue where logging responses cannot handle Error objects. PR by @RobertHerhold

##### 2.0.0 - 02 July 2015

Stable Version 2.0.0

##### 2.0.0-rc.1 - 30 June 2015

Added `getEndpoint()`, which was removed from JSData

##### 2.0.0-beta.1 - 17 April 2015

Prepare for 2.0

##### 1.2.3 - 07 March 2015

###### Other
- Converted code to ES6.

##### 1.2.2 - 04 March 2015

###### Backwards compatible bug fixes
- #10 - DSHttpAdapter#find does not call queryTransform

###### Other
- Switched build to webpack. UMD should actually work now.

##### 1.2.1 - 25 February 2015

###### Backwards compatible bug fixes
- #9 - Does not properly throw error in find() (like other adapters) when the item cannot be found

##### 1.2.0 - 24 February 2015

###### Backwards compatible API changes
- Added `suffix` option

##### 1.1.0 - 04 February 2015

Now requiring js-data 1.1.0 to allow for safe stringification of cyclic objects

##### 1.0.0 - 03 February 2015

Stable Version 1.0.0

##### 1.0.0-beta.2 - 10 January 2015

Fixed some tests.

##### 1.0.0-beta.1 - 10 January 2015

Now in beta

##### 1.0.0-alpha.6 - 05 December 2014

###### Backwards compatible bug fixes
- Fix for making copies of `options`

##### 1.0.0-alpha.5 - 05 December 2014

###### Backwards compatible bug fixes
- Updated dependencies
- Now safely making copies of the `options` passed into methods

##### 1.0.0-alpha.4 - 01 December 2014

###### Backwards compatible API changes
- Added DSHttpAdapter.getPath

###### Backwards compatible bug fixes
- #8 - Fixed handling of `forceTrailingSlash` option

##### 1.0.0-alpha.3 - 19 November 2014

###### Breaking API changes
- `queryTransform`, `serialize` and `deserialize` now take the resource definition as the first argument instead of just the resource name

##### 1.0.0-alpha.2 - 01 November 2014

###### Backwards compatible API changes
- #4 - Log failures. See also #5 and #6

##### 1.0.0-alpha.1 - 31 October 2014

###### Backwards compatible bug fixes
- #3 - _this.defaults.log() throws Illegal invocation

##### 0.4.2 - 22 October 2014

###### Backwards compatible bug fixes
- Fixed illegal invocation error

##### 0.4.1 - 30 September 2014

###### Backwards compatible API changes
- Added `forceTrailingSlash` option to `DSHttpAdapter#defaults`

###### Other
- Improved checking for the js-data dependency

##### 0.4.0 - 25 September 2014

###### Breaking API changes
- Refactored from `baseUrl` to `basePath`, as `baseUrl` doesn't make sense for all adapters, but `basePath` does

##### 0.3.0 - 21 September 2014

###### Backwards compatible API changes
- Small re-organization.

##### 0.2.0 - 21 September 2014

###### Backwards compatible API changes
- Added deserialize and serialize.

##### 0.1.0 - 17 September 2014

- Initial release
