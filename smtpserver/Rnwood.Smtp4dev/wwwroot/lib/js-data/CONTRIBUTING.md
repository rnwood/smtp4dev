# Contributing Guide

First, support is handled via the [Slack Channel](http://slack.js-data.io) and the [Mailing List](https://groups.io/org/groupsio/jsdata). Ask your questions there.

When submitting issues on GitHub, please include as much detail as possible to make debugging quick and easy.

- good - Your versions of Angular, JSData, etc, relevant console logs/error, code examples that revealed the issue
- better - A [plnkr](http://plnkr.co/), [fiddle](http://jsfiddle.net/), or [bin](http://jsbin.com/?html,output) that demonstrates the issue
- best - A Pull Request that fixes the issue, including test coverage for the issue and the fix

[Github Issues](https://github.com/js-data/js-data/issues).

#### Submitting Pull Requests

1. Contribute to the issue/discussion that is the reason you'll be developing in the first place
1. Fork js-data
1. `git clone git@github.com:<you>/js-data.git`
1. `cd js-data; npm install; bower install;`
1. Write your code, including relevant documentation and tests
1. Run `npm test` (build and test)
1. Your code will be linted and checked for formatting, the tests will be run
1. The `dist/` folder & files will be generated, do NOT commit `dist/*`! They will be committed when a release is cut.
1. Submit your PR and we'll review!
1. Thanks!

#### Have write access?

To cut a release:

1. Checkout master
1. Bump version in `package.json` appropriately
1. Run `npm test`
1. Update `CHANGELOG.md` appropriately
1. Commit and push changes, including the `dist/` folder
1. Make a GitHub release
  - set tag name to version
  - set release name to version
  - set release body to changelog entry for the version
  - attach the files in the `dist/` folder
1. `git fetch origin`
1. `npm publish .`