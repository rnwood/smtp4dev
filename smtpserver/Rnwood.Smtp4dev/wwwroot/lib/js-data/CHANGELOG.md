##### 2.10.0 - 22 September 2016

###### Backwards compatible changes
- #324 - Added `applyDefaultsOnInject` option, which defaults to `false`
- #364 - Added `usePendingFind` and `usePendingFindAll` options, which both default to `true`

###### Bug fixes
- #316 - Merge/Replace inject does not reevaluate computed properties
- #324 - Inconsistent defaultValues behavior.
- #364 - Resource.pendingQueries on server causes unexpected behaviour
- #373 - DS.clear() extremely slow
- #407 - DSUtils.copy blacklist applies to nested fields

##### 2.9.0 - 17 February 2016

###### Backwards compatible bug API changes
- #273 - DS.save, use id from incoming arguments by @zuzusik
- #284 - (Partial) Support for temporary items
- #290 - Add save() option to always include specified properties when using changesOnly by @OzzieOrca
- #305 - Add support for multiple parents by @tfoxy

###### Backwards compatible bug fixes
- #251 - The 'localKey' of the 'belongsTo' relation ship is not set.
- #262 - `defaultValues` are shallow copied
- #272 - lastSaved is broken when API doesn't return saved object in response
- #304 - Relations ignore useClass on the server

##### 2.8.2 - 04 November 2015

###### Backwards compatible bug fixes
- #258 - CSP violations due to use of new Function()

##### 2.8.1 - 02 November 2015

###### Backwards compatible bug fixes
- #239 - loadRelations assumes cacheResponse and linkRelations options are true
- #259, #260 - Reverting undefined keys by @davincho

##### 2.8.0 - 26 October 2015

###### Backwards compatible API changes
- #211 - Add case insensitive filtering in query syntax

##### 2.7.0 - 22 October 2015

###### Backwards compatible API changes
- #205 - DS#revert should ignore omitted fields
- #243 - DS#commit
- #245 - Closes #205 by @internalfx
- #248 - Fix `belongsTo` relation with zero value by @Pencroff

###### Other
- Dropped Grunt

##### 2.6.1 - 12 October 2015

###### Bug fixes
- #223 - Zero value Id in relations fixed in #237 by @Pencroff

##### 2.6.0 - 08 October 2015

###### Backwards compatible API changes
- #234 - findAll should query adapter if previous query is expired.
- #235 - Support maxAge in find/findAll requests by @antoinebrault

###### Bug fixes
- #236 - actions defined in defineResource are shared across definitions

##### 2.5.0 - 04 October 2015

###### Backwards compatible API changes
- #187 - No way to hook into error events globally
- #201 - Feature request: hook into loadRelations
- #220 - Optionally disable injection of nested relations
- #231 - Added hasMany relations linking using "foreignKeys" by @treyenelson

###### Bug fixes
- #229 - DS.change is emitted on an instance multiple times after only 1 modification
- #232 - Adapter default basepath is taken instead of definition basepath when using an action.

##### 2.4.0 - 22 September 2015

###### Backwards compatible API changes
- #179 - Implemented a feature like Sequelize Scopes
- #201 - Feature request: hook into loadRelations
- #217 - Add afterFind, afterFindAll, and afterLoadRelations hooks

###### Bug fixes
- #203 - createInstance/compute don't know about computed properties as property accessors
- #215 - Javascript error when trying to merge model with null value for relation
- #216 - Update remove circular to support File objects
- #218 - linkRelations (like cacheResponse) should have defaulted to false on the server

###### Other
- #204 - Choose official code style for project
- Switched unnecessary arrow functions back to regular functions to improve performance
- Updated CONTRIBUTING.md

##### 2.3.0 - 30 July 2015

###### Backwards compatible API changes
- #186 - Add relation setters for convenience
- #191 - Add ability to disable change detection
- #192 - Add ability to configure computed property as a property accessor

###### Backwards compatible bug fixes
- #190 - computed properties false positive minified code warning

##### 2.2.3 - 22 July 2015

###### Backwards compatible bug fixes
- Removed some asinine optimizations

##### 2.2.2 - 10 July 2015

###### Backwards compatible bug fixes
- #177 - Fix Events.off

##### 2.2.1 - 09 July 2015

###### Backwards compatible bug fixes
- #176 - `localKey`, `localKeys` and `foreignKey` don't support nested fields.

##### 2.2.0 - 07 July 2015

###### Backwards compatible API changes
- #173 - Added `DS#revert(resourceName, id)` Thanks @internalfx

##### 2.1.0 - 07 July 2015

###### Backwards compatible API changes
- Added `DS#clear()`, which is a method only available on a store, and will call `ejectAll` on all of the store's resources

##### 2.0.0 - 02 July 2015

Stable Version 2.0.0

##### 2.0.0-rc.3 - 30 June 2015

- Tweak to custom relation getters

##### 2.0.0-rc.2 - 30 June 2015

###### Backwards compatible API changes
- Enhanced relation getters and better localKeys support

##### 2.0.0-rc.1 - 27 June 2015

###### Breaking API changes
- Moved the `getEndpoint` method to the http adapter

##### 2.0.0-beta.11 - 26 June 2015

###### Backwards compatible API changes
- #167 - DS#refreshAll
- #168 - DS#inject - replace instead of merge. `onConflict: 'replace'` will replace existing items instead of merging into them.

##### 2.0.0-beta.10 - 26 June 2015

###### Backwards compatible bug fixes
- Fix so `DS#loadRelations` can load all relations

##### 2.0.0-beta.9 - 26 June 2015

###### Breaking API changes
- #161 - By default, computed properties are no longer sent to adapters. You can also configure other properties that shouldn't be sent.

###### Backwards compatible API changes
- #162 - Return query metadata as second parameter from a promise.

###### Backwards compatible bug fixes
- #165 - global leak

##### 2.0.0-beta.8 - 22 June 2015

###### Backwards compatible API changes
- #160 - Add "DS.change" events, fired on Resources and instances

##### 2.0.0-beta.7 - 09 June 2015

###### Breaking API changes
- #158 - Data store should consume resource definition methods internally (might not be breaking)

###### Backwards compatible API changes
- #157 - DSEject not available on instances

###### Other
- #156 - Thoroughly annotate all source code to encourage contribution

##### 2.0.0-beta.6 - 04 June 2015

###### Breaking API changes
- #150 - Debug output, `debug` now defaults to `false`

###### Backwards compatible API changes
- #145 - A little AOP, add a `.before` to all methods, allowing per-method argument customization

##### 2.0.0-beta.5 - 27 May 2015

###### Breaking API changes
- #54 - feat: Call the inject and eject lifecycle hooks regardless of if the notify option is enabled

###### Backwards compatible API changes
- #131 - array of IDs based hasMany relations
- #132 - Allow resources to extend other resources
- #133 - Allow filtering by nested fields
- #135 - JSData caching inconsistent behaviour when ejecting items
- #138 - Collection class
- #139 - Option to specify default values of new resource instances.

###### Backwards compatible bug fixes
- #127 - Memory leak in DS.changes
- #134 - All resources get all methods defined on any resource
- #142 - Allow omitting options in getEndpoint

##### 2.0.0-beta.4 - 28 April 2015

###### Backwards compatible API changes
- #129 - Add interceptors to actions

##### 2.0.0-beta.2 - 17 April 2015

Updated a dependency for better umd amd/r.js support

##### 2.0.0-beta.1 - 17 April 2015

###### Breaking API changes
- #107 - Switch to property accessors (getter/setter) for relations links. (Relation links are no longer enumerable)
- #121 - Remove bundled Promise code (The developer must now ensure an ES6-style Promise constructor is available)
- #122 - Remove coupling with js-data-schema (You can still use js-data-schema, js-data just doesn't know anything about js-data-schema anymore)

###### Backwards compatible API changes
- Computed properties now support nested fields (both the computed field and the fields it depends on) e.g. `computed: { 'name.fullName': ['name.first', 'name.last', function (first, last) { return first + ' ' + last; } }`

##### 1.8.0 - 14 April 2015

###### Backwards compatible API changes
- #117 - .find skips the object in the store
- #118 - DS#find() returns items cached with DS#inject() - Thanks @mightyguava!
- `createInstance` will now initialize computed properties (but they won't be updated until the item is injected into the store, or unless you use `Instance#set(key, value)` to mutate the instance)

###### Backwards compatible bug fixes
- #115 - removeCircular bug

##### 1.7.0 - 09 April 2015

###### Backwards compatible API changes
- #106 - Add pathname option to actions
- #114 - Add support to actions for having item ids in the path

##### 1.6.3 - 03 April 2015

###### Backwards compatible bug fixes
- #106 - loadRelations: check params.where instead when allowSimpleWhere is disabled - Thanks @maninga!

##### 1.6.2 - 01 April 2015

###### Backwards compatible bug fixes
- #104 - DS.schemator is undefined when using browserify

##### 1.6.1 - 31 March 2015

###### Backwards compatible bug fixes
- #101 - Reject instead of throw, as throw is messy in the console

##### 1.6.0 - 29 March 2015

###### Backwards compatible API changes
- #97 - Don't link relations where localField is undefined

###### Backwards compatible bug fixes
- #95 - actions should use defaultAdapter of the resource

##### 1.5.13 - 25 March 2015

###### Backwards compatible bug fixes
- #91 - Wrong second argument passed to afterCreateInstance

##### 1.5.12 - 23 March 2015

###### Backwards compatible bug fixes
- #84 - DS.Inject performance issues when reloading data (`DSUtils.copy` was attempting to copy relations)

##### 1.5.11 - 22 March 2015

###### Backwards compatible bug fixes
- #83 - Change detection incorrectly handles cycles in the object

##### 1.5.10 - 19 March 2015

###### Backwards compatible bug fixes
- #81 - Sometimes `inject` with nested relations causes an infinite loop

###### Other
- Added `.npmignore` for a slimmer npm package

##### 1.5.9 - 18 March 2015

###### Backwards compatible bug fixes
- #76 - Saving relation fields with changesOnly=true
- #80 - save + changesOnly + nested relations + no actual changes results in an error

###### Other
- Upgraded dependencies

##### 1.5.8 - 14 March 2015

###### Other
- Extracted BinaryHeap class to its own npm module

##### 1.5.7 - 13 March 2015

###### Backwards compatible bug fixes
- #75 - `DSUtils.removeCircular` is removing more stuff than it should

##### 1.5.6 - 07 March 2015

###### Backwards compatible bug fixes
- Fixed loading of the optional js-data-schema

##### 1.5.5 - 07 March 2015

###### Other
- Re-wrote a good amount of the code to use ES6. Now using Babel.js to transpile back to ES5.

##### 1.5.4 - 05 March 2015

###### Backwards compatible bug fixes
- #72 - bug: items injected via a relationship fail to fire notifications (fixed more cases of this happening)

##### 1.5.3 - 05 March 2015

###### Backwards compatible bug fixes
- #35 - beforeInject not called on relationships
- #72 - bug: items injected via a relationship fail to fire notifications

##### 1.5.2 - 02 March 2015

###### Backwards compatible bug fixes
- Now using `DSUtils.copy` when saving "original" attributes so changes can be computed properly

##### 1.5.1 - 02 March 2015

###### Backwards compatible bug fixes
- #66 - "saved" and "lastSaved" method seems to be a misnomer
- #69 - Using resource base class w/additional properties has some side effects
- #70 - "lastSaved" timestamp changes too often

###### Other
- Removed use of `DSUtils.copy` in the event hooks. This should increase performance quite a bit.

##### 1.5.0 - 27 February 2015

###### Backwards compatible API changes
- #17 - feat: Load relations based on local field name

###### Backwards compatible bug fixes
- #62 - getAdapter when called from a Resource fails
- #65 - internal emit api was not updated to use Resource instead of Resource.name like the lifecycle hooks were

###### Other
- Internal optimizations to shave ~2kb off the minified build

##### 1.4.1 - 27 February 2015

###### Backwards compatible bug fixes
- #64 - Two possible error cases in `DS#find`

##### 1.4.0 - 24 February 2015

###### Backwards compatible api changes
- #51 - Allow resource instances to be created from a base class

##### 1.3.0 - 11 February 2015

###### Backwards compatible api changes
- #50 - Added a `DS#is(resourceName, instance)` or `Resource#is(instance)` method to check if an object is an instance of a particular resource

###### Backwards compatible bug fixes
- When items are ejected cached collection queries are now checked to see if all the cached items from that query are gone, and if so, the cache query is deleted

##### 1.2.1 - 06 February 2015

###### Backwards compatible bug fixes
- #42 - deserialize and beforeInject are called from the parent relation when loadRelations is used

##### 1.2.0 - 05 February 2015

###### Backwards compatible bug fixes
- Added a `getResource(resourceName)` method to resource definitions so adapters can grab the definitions of a resource's relations

##### 1.1.1 - 05 February 2015

###### Backwards compatible bug fixes
- #46 - "actions" don't inherit basePath properly

##### 1.1.0 - 04 February 2015

##### Backwards compatible API changes
- Allow nested keys in "orderBy" clauses, i.e. `orderBy: 'foo.bar'`
- Added `get` and `set` methods to the instance prototype for getter/setter manipulation of data store items. Use of `set` will trigger immediate recalculation of computed properties on the instance. Both `get` and `set` support nested key names.
- Added a `removeCircular` util method so cyclic objects can be saved without fuss
- #43 - Added `contains` operator to the default filter

##### Backwards compatible bug fixes
- Added missing `createInstance` calls

##### 1.0.0 - 03 February 2015

Stable Version 1.0.0

###### Other
- Upgraded to the latest observe-js

##### 1.0.0-beta.2 - 23 January 2015

###### Backwards compatible API changes
- Updates to defining "actions"

##### 1.0.0-beta.1 - 10 January 2015

###### Breaking API changes
- #30 - Issue with offset. To solve this a `useFilter` option was added, which defaults to `false`. Previously `DS#filter` was used to return cached `findAll` queries, but that had problems. Now, cached items are also tracked by the query that retrieved them, so when you make a query again you consistently get the right data.

###### Backwards compatible API changes
- #6 - Allow logging to be configurable
- #29 - Add version to JSData export
- #31 - Add build for js-data-debug.js which contains lots of debugging statements and a configurable logger.

##### 1.0.0-alpha.5-8 - 05 December 2014

###### Backwards compatible API changes
- #27 - Properly resolve parent params for generating the URL

##### 1.0.0-alpha.5-7 - 05 December 2014

###### Backwards compatible API changes
- #26 - Added the DSCreate instance method

###### Backwards compatible bug fixes
- #23 - DS#findAll: make a copy of options.params if it's passed in and manipulate that

##### 1.0.0-alpha.5-6 - 03 December 2014

###### Backwards compatible bug fixes
- Backport jmdobry/angular-data#262

###### Other
- Optimized utility functions to save several kilobytes off of minified file
- Change detection of nested properties "should" work now

##### 1.0.0-alpha.5-5 - 30 November 2014

###### Breaking API changes
- findInverseLinks, findBelongsTo, findHasOne, and findHasMany now default to true

###### Backwards compatible bug fixes
- Backport jmdobry/angular-data#253

##### 1.0.0-alpha.5-3 - 28 November 2014

###### Backwards compatible API changes
- Added the isectEmpty, isectNotEmpty, |isectEmpty, and |isectNotEmpty filter operators

###### Other
- Fixed file size of browser dist file

##### 1.0.0-alpha.5-3 - 26 November 2014

###### Backwards compatible API changes
- Server-side js-data now uses the Bluebird promise library

##### 1.0.0-alpha.5-2 - 23 November 2014

###### Backwards compatible API changes
- items don't have to be in the data store to call destroy on them anymore

##### 1.0.0-alpha.5-1 - 19 November 2014

Removed DSUtils.deepFreeze

##### 1.0.0-alpha.5-0 - 18 November 2014

###### Breaking API changes
- All hooks now take the resource definition object as the first argument instead of just the name of the resource

###### Backwards compatible API changes
- jmdobry/angular-data#238

##### 1.0.0-alpha.4-3 - 11 November 2014

###### Backwards compatible bug fixes
- #19 - multiple orderBy does not work

##### 1.0.0-alpha.4-2 - 09 November 2014

###### Backwards compatible API changes
- jmdobry/angular-data#227 - Supporting methods on model instances

###### Backwards compatible bug fixes
- jmdobry/angular-data#235 - IE 8 support

##### 1.0.0-alpha.4-1 - 08 November 2014

###### Backwards compatible bug fixes
- Various fixes

##### 1.0.0-alpha.4-0 - 04 November 2014

###### Backwards compatible API changes
- jmdobry/angular-data#208 - ng-repeat $$hashKey affecting hasChanges

###### Backwards compatible bug fixes
- jmdobry/angular-data#225 - If the server returned an empty array for a get request (valid scenario), angular-data throws an exception

##### 1.0.0-alpha.2 - 31 October 2014

###### Backwards compatible API changes
- #20 - es6-promise finally polyfill

##### 1.0.0-alpha.1-2 - 30 October 2014

###### Backwards compatible bug fixes
- Fixed an issue with the options defaults util function

##### 1.0.0-alpha.1-1 - 19 October 2014

###### Backwards compatible API changes
- #10 - Add js-data-schema integration

##### 1.0.0-alpha.1-0 - 13 October 2014

###### Backwards compatible API changes
- #15 - Add beforeCreateInstance & afterCreateInstance

##### 0.4.2 - 06 October 2014

###### Backwards compatible API changes
- #12 - Add expiration capabilities (reapInterval, reapAction, maxAge, DS#reap)

##### 0.4.1 - 01 October 2014

###### Backwards compatible API changes
- #9 - Make all options passed to methods also inherit from Resource defaults

###### Backwards compatible bug fixes
- jmdobry/angular-data#195 - throw an error when you try to inject a relation but the resource for it hasn't been defined

###### Other
- Added official support for NodeJS

##### 0.4.0 - 25 September 2014

###### Breaking API changes
- Refactored from `baseUrl` to `basePath`, as `baseUrl` doesn't make sense for all adapters, but `basePath` does
- Made `notify` configurable globally and per-resource

##### 0.3.0 - 22 September 2014

###### Backwards compatible API changes
- Added `beforeDestroy` and `afterDestroy` to `DS#destroyAll`
- Added `eagerEject` option to `DS#destroyAll` and `DS#destroy`

##### 0.2.0 - 20 September 2014

###### Backwards compatible API changes
- jmdobry/angular-data#145 - Add "useClass" option to inject, find, findAll, create
- jmdobry/angular-data#159 - Find which items from collection have changed with lastModified
- jmdobry/angular-data#166 - Add ID Resolver
- jmdobry/angular-data#167 - Default params argument of bindAll to empty object
- jmdobry/angular-data#170 - Global callbacks
- jmdobry/angular-data#171 - "not in" query
- jmdobry/angular-data#177 - Allow promises to be returned in lifecycle hooks

###### Backwards compatible bug fixes
- jmdobry/angular-data#156 - cached findAll pending query doesn't get removed sometimes
- jmdobry/angular-data#163 - loadRelations shouldn't try to load a relation if the id for it is missing
- jmdobry/angular-data#165 - DS.hasChanges() reports changes after loading relations

###### Other
- Moved api documentation out of comments and into the GitHub wiki
- Re-organized code and shaved 5.5kb off the minified file
