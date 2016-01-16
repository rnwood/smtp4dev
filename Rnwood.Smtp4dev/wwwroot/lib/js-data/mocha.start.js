/*global assert:true */
'use strict';

require('es6-promise').polyfill();

var assert = require('chai').assert;
var mocha = require('mocha');
var sinon = require('sinon');
var JSData = require('./dist/js-data-debug.js');

var store, DSUtils, DSErrors;

assert.objectsEqual = function (a, b, msg) {
  assert.deepEqual(JSON.parse(JSON.stringify(a)), JSON.parse(JSON.stringify(b)), msg || 'Expected objects or arrays to be equal');
}
var lifecycle = {};

var globals = module.exports = {
  fail: function (msg) {
    assert.equal('should not reach this!: ' + msg, 'failure');
  },
  TYPES_EXCEPT_STRING: [123, 123.123, null, undefined, {}, [], true, false, function () {
  }],
  TYPES_EXCEPT_STRING_OR_ARRAY: [123, 123.123, null, undefined, {}, true, false, function () {
  }],
  TYPES_EXCEPT_STRING_OR_NUMBER: [null, undefined, {}, [], true, false, function () {
  }],
  TYPES_EXCEPT_STRING_OR_OBJECT: [123, 123.123, null, undefined, [], true, false, function () {
  }],
  TYPES_EXCEPT_STRING_OR_NUMBER_OBJECT: [null, undefined, [], true, false, function () {
  }],
  TYPES_EXCEPT_ARRAY: ['string', 123, 123.123, null, undefined, {}, true, false, function () {
  }],
  TYPES_EXCEPT_STRING_OR_ARRAY_OR_NUMBER: [null, undefined, {}, true, false, function () {
  }],
  TYPES_EXCEPT_NUMBER: ['string', null, undefined, {}, [], true, false, function () {
  }],
  TYPES_EXCEPT_OBJECT: ['string', 123, 123.123, null, undefined, true, false, function () {
  }],
  TYPES_EXCEPT_BOOLEAN: ['string', 123, 123.123, null, undefined, {}, [], function () {
  }],
  TYPES_EXCEPT_FUNCTION: ['string', 123, 123.123, null, undefined, {}, [], true, false],
  assert: assert,
  sinon: sinon,
  store: undefined
};

var test = new mocha();

var testGlobals = [];

for (var key in globals) {
  global[key] = globals[key];
  testGlobals.push(globals[key]);
}
test.globals(testGlobals);

beforeEach(function () {
  lifecycle.beforeValidate = function (resourceName, attrs, cb) {
    lifecycle.beforeValidate.callCount += 1;
    cb(null, attrs);
  };
  lifecycle.validate = function (resourceName, attrs, cb) {
    lifecycle.validate.callCount += 1;
    cb(null, attrs);
  };
  lifecycle.afterValidate = function (resourceName, attrs, cb) {
    lifecycle.afterValidate.callCount += 1;
    cb(null, attrs);
  };
  lifecycle.beforeCreate = function (resourceName, attrs, cb) {
    lifecycle.beforeCreate.callCount += 1;
    cb(null, attrs);
  };
  lifecycle.afterCreate = function (resourceName, attrs, cb) {
    lifecycle.afterCreate.callCount += 1;
    cb(null, attrs);
  };
  lifecycle.beforeUpdate = function (resourceName, attrs, cb) {
    lifecycle.beforeUpdate.callCount += 1;
    cb(null, attrs);
  };
  lifecycle.afterUpdate = function (resourceName, attrs, cb) {
    lifecycle.afterUpdate.callCount += 1;
    cb(null, attrs);
  };
  lifecycle.beforeDestroy = function (resourceName, attrs, cb) {
    console.log(resourceName, attrs, cb);
    lifecycle.beforeDestroy.callCount += 1;
    cb(null, attrs);
  };
  lifecycle.afterDestroy = function (resourceName, attrs, cb) {
    lifecycle.afterDestroy.callCount += 1;
    cb(null, attrs);
  };
  lifecycle.beforeInject = function () {
    lifecycle.beforeInject.callCount += 1;
  };
  lifecycle.afterInject = function () {
    lifecycle.afterInject.callCount += 1;
  };
  lifecycle.serialize = function (resourceName, data) {
    lifecycle.serialize.callCount += 1;
    return data;
  };
  lifecycle.deserialize = function (resourceName, data) {
    lifecycle.deserialize.callCount += 1;
    return data ? ('data' in data ? data.data : data) : data;
  };
  lifecycle.queryTransform = function (resourceName, query) {
    lifecycle.queryTransform.callCount += 1;
    return query;
  };
  store = new JSData.DS({
    basePath: 'http://test.js-data.io',
    beforeValidate: lifecycle.beforeValidate,
    cacheResponse: true,
    notify: true,
    upsert: true,
    validate: lifecycle.validate,
    afterValidate: lifecycle.afterValidate,
    beforeCreate: lifecycle.beforeCreate,
    afterCreate: lifecycle.afterCreate,
    beforeUpdate: lifecycle.beforeUpdate,
    afterUpdate: lifecycle.afterUpdate,
    beforeDestroy: lifecycle.beforeDestroy,
    afterDestroy: lifecycle.afterDestroy,
    beforeInject: lifecycle.beforeInject,
    afterInject: lifecycle.afterInject,
    linkRelations: true,
    log: false,
    methods: {
      say: function () {
        return 'hi';
      }
    }
  });
  DSUtils = JSData.DSUtils;
  DSErrors = JSData.DSErrors;
  globals.Post = global.Post = store.defineResource({
    name: 'post',
    keepChangeHistory: true,
    endpoint: '/posts'
  });
  globals.User = global.User = store.defineResource({
    name: 'user',
    relations: {
      hasMany: {
        comment: {
          localField: 'comments',
          foreignKey: 'approvedBy'
        },
        group: {
          localField: 'groups',
          foreignKeys: 'userIds'
        }
      },
      hasOne: {
        profile: {
          localField: 'profile',
          foreignKey: 'userId'
        }
      },
      belongsTo: {
        organization: {
          parent: true,
          localKey: 'organizationId',
          localField: 'organization'
        }
      }
    }
  });

  globals.Group = global.Group = store.defineResource({
    name: 'group',
    relations: {
      hasMany: {
        user: {
          localField: 'users',
          localKeys: 'userIds'
        }
      }
    }
  });

  globals.Organization = global.Organization = store.defineResource({
    name: 'organization',
    relations: {
      hasMany: {
        user: {
          localField: 'users',
          foreignKey: 'organizationId'
        }
      }
    }
  });

  globals.Profile = global.Profile = store.defineResource({
    name: 'profile',
    relations: {
      belongsTo: {
        user: {
          localField: 'user',
          localKey: 'userId'
        }
      }
    }
  });

  globals.Comment = global.Comment = store.defineResource({
    name: 'comment',
    relations: {
      belongsTo: {
        user: [
          {
            localField: 'user',
            localKey: 'userId'
          },
          {
            parent: true,
            localField: 'approvedByUser',
            localKey: 'approvedBy'
          }
        ]
      }
    }
  });

  lifecycle.beforeValidate.callCount = 0;
  lifecycle.validate.callCount = 0;
  lifecycle.afterValidate.callCount = 0;
  lifecycle.beforeCreate.callCount = 0;
  lifecycle.afterCreate.callCount = 0;
  lifecycle.beforeUpdate.callCount = 0;
  lifecycle.afterUpdate.callCount = 0;
  lifecycle.beforeDestroy.callCount = 0;
  lifecycle.afterDestroy.callCount = 0;
  lifecycle.beforeInject.callCount = 0;
  lifecycle.afterInject.callCount = 0;
  lifecycle.serialize.callCount = 0;
  lifecycle.deserialize.callCount = 0;
  lifecycle.queryTransform.callCount = 0;

  globals.p1 = global.p1 = { author: 'John', age: 30, id: 5 };
  globals.p2 = global.p2 = { author: 'Sally', age: 31, id: 6 };
  globals.p3 = global.p3 = { author: 'Mike', age: 32, id: 7 };
  globals.p4 = global.p4 = { author: 'Adam', age: 33, id: 8 };
  globals.p5 = global.p5 = { author: 'Adam', age: 33, id: 9 };

  globals.user1 = global.user1 = {
    name: 'John Anderson',
    id: 1,
    organizationId: 2
  };
  globals.organization2 = global.organization2 = {
    name: 'Test Corp 2',
    id: 2
  };
  globals.comment3 = global.comment3 = {
    content: 'test comment 3',
    id: 3,
    userId: 1
  };
  globals.profile4 = global.profile4 = {
    content: 'test profile 4',
    id: 4,
    userId: 1
  };

  globals.comment11 = global.comment11 = {
    id: 11,
    userId: 10,
    content: 'test comment 11'
  };
  globals.comment12 = global.comment12 = {
    id: 12,
    userId: 10,
    content: 'test comment 12'
  };
  globals.comment13 = global.comment13 = {
    id: 13,
    userId: 10,
    content: 'test comment 13'
  };
  globals.organization14 = global.organization14 = {
    id: 14,
    name: 'Test Corp'
  };
  globals.profile15 = global.profile15 = {
    id: 15,
    userId: 10,
    email: 'john.anderson@test.com'
  };
  globals.user10 = global.user10 = {
    name: 'John Anderson',
    id: 10,
    organizationId: 14,
    comments: [
      globals.comment11,
      globals.comment12,
      globals.comment13
    ],
    organization: globals.organization14,
    profile: globals.profile15
  };
  globals.user16 = global.user16 = {
    id: 16,
    organizationId: 15,
    name: 'test user 16'
  };
  globals.user17 = global.user17 = {
    id: 17,
    organizationId: 15,
    name: 'test user 17'
  };
  globals.user18 = global.user18 = {
    id: 18,
    organizationId: 15,
    name: 'test user 18'
  };
  globals.group1 = global.group1 = {
    name: 'group 1',
    id: 1,
    userIds: [10]
  };
  globals.group2 = global.group2 = {
    name: 'group 2',
    id: 2,
    userIds: [10]
  };
  globals.organization15 = global.organization15 = {
    name: 'Another Test Corp',
    id: 15,
    users: [
      globals.user16,
      globals.user17,
      globals.user18
    ]
  };
  globals.user19 = global.user19 = {
    id: 19,
    name: 'test user 19'
  };
  globals.user20 = global.user20 = {
    id: 20,
    name: 'test user 20'
  };
  globals.comment19 = global.comment19 = {
    content: 'test comment 19',
    id: 19,
    approvedBy: 19,
    approvedByUser: globals.user19,
    userId: 20,
    user: globals.user20
  };
  globals.user22 = global.user22 = {
    id: 22,
    name: 'test user 22'
  };
  globals.profile21 = global.profile21 = {
    content: 'test profile 21',
    id: 21,
    userId: 22,
    user: globals.user22
  };

  globals.store = store;
  global.store = globals.store;

  globals.JSData = JSData;
  global.JSData = globals.JSData;

  globals.DSUtils = DSUtils;
  global.DSUtils = globals.DSUtils;

  globals.DSErrors = DSErrors;
  global.DSErrors = globals.DSErrors;

  globals.DSErrors = DSErrors;
  global.DSErrors = globals.DSErrors;

  globals.DSErrors = DSErrors;
  global.DSErrors = globals.DSErrors;

  globals.lifecycle = lifecycle;
  global.lifecycle = globals.lifecycle;

  globals.isNode = true;
  global.isNode = true;
  this.isNode = true;
});

afterEach(function () {
  globals.store = null;
  global.store = null;
});
