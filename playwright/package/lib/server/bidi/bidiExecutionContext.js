"use strict";

Object.defineProperty(exports, "__esModule", {
  value: true
});
exports.BidiExecutionContext = void 0;
var _utilityScriptSerializers = require("../isomorphic/utilityScriptSerializers");
var js = _interopRequireWildcard(require("../javascript"));
var _bidiDeserializer = require("./third_party/bidiDeserializer");
var bidi = _interopRequireWildcard(require("./third_party/bidiProtocol"));
var _bidiSerializer = require("./third_party/bidiSerializer");
function _getRequireWildcardCache(e) { if ("function" != typeof WeakMap) return null; var r = new WeakMap(), t = new WeakMap(); return (_getRequireWildcardCache = function (e) { return e ? t : r; })(e); }
function _interopRequireWildcard(e, r) { if (!r && e && e.__esModule) return e; if (null === e || "object" != typeof e && "function" != typeof e) return { default: e }; var t = _getRequireWildcardCache(r); if (t && t.has(e)) return t.get(e); var n = { __proto__: null }, a = Object.defineProperty && Object.getOwnPropertyDescriptor; for (var u in e) if ("default" !== u && Object.prototype.hasOwnProperty.call(e, u)) { var i = a ? Object.getOwnPropertyDescriptor(e, u) : null; i && (i.get || i.set) ? Object.defineProperty(n, u, i) : n[u] = e[u]; } return n.default = e, t && t.set(e, n), n; }
/**
 * Copyright (c) Microsoft Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the 'License');
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an 'AS IS' BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

class BidiExecutionContext {
  constructor(session, realmInfo) {
    this._session = void 0;
    this._target = void 0;
    this._session = session;
    if (realmInfo.type === 'window') {
      // Simple realm does not seem to work for Window contexts.
      this._target = {
        context: realmInfo.context,
        sandbox: realmInfo.sandbox
      };
    } else {
      this._target = {
        realm: realmInfo.realm
      };
    }
  }
  async rawEvaluateJSON(expression) {
    const response = await this._session.send('script.evaluate', {
      expression,
      target: this._target,
      serializationOptions: {
        maxObjectDepth: 10,
        maxDomDepth: 10
      },
      awaitPromise: true,
      userActivation: true
    });
    if (response.type === 'success') return _bidiDeserializer.BidiDeserializer.deserialize(response.result);
    if (response.type === 'exception') throw new js.JavaScriptErrorInEvaluate(response.exceptionDetails.text + '\nFull val: ' + JSON.stringify(response.exceptionDetails));
    throw new js.JavaScriptErrorInEvaluate('Unexpected response type: ' + JSON.stringify(response));
  }
  async rawEvaluateHandle(expression) {
    const response = await this._session.send('script.evaluate', {
      expression,
      target: this._target,
      resultOwnership: bidi.Script.ResultOwnership.Root,
      // Necessary for the handle to be returned.
      serializationOptions: {
        maxObjectDepth: 0,
        maxDomDepth: 0
      },
      awaitPromise: true,
      userActivation: true
    });
    if (response.type === 'success') {
      if ('handle' in response.result) return response.result.handle;
      throw new js.JavaScriptErrorInEvaluate('Cannot get handle: ' + JSON.stringify(response.result));
    }
    if (response.type === 'exception') throw new js.JavaScriptErrorInEvaluate(response.exceptionDetails.text + '\nFull val: ' + JSON.stringify(response.exceptionDetails));
    throw new js.JavaScriptErrorInEvaluate('Unexpected response type: ' + JSON.stringify(response));
  }
  rawCallFunctionNoReply(func, ...args) {
    throw new Error('Method not implemented.');
  }
  async evaluateWithArguments(functionDeclaration, returnByValue, utilityScript, values, objectIds) {
    const response = await this._session.send('script.callFunction', {
      functionDeclaration,
      target: this._target,
      arguments: [{
        handle: utilityScript._objectId
      }, ...values.map(_bidiSerializer.BidiSerializer.serialize), ...objectIds.map(handle => ({
        handle
      }))],
      resultOwnership: returnByValue ? undefined : bidi.Script.ResultOwnership.Root,
      // Necessary for the handle to be returned.
      serializationOptions: returnByValue ? {} : {
        maxObjectDepth: 0,
        maxDomDepth: 0
      },
      awaitPromise: true,
      userActivation: true
    });
    if (response.type === 'exception') throw new js.JavaScriptErrorInEvaluate(response.exceptionDetails.text + '\nFull val: ' + JSON.stringify(response.exceptionDetails));
    if (response.type === 'success') {
      if (returnByValue) return (0, _utilityScriptSerializers.parseEvaluationResultValue)(_bidiDeserializer.BidiDeserializer.deserialize(response.result));
      const objectId = 'handle' in response.result ? response.result.handle : undefined;
      return utilityScript._context.createHandle({
        objectId,
        ...response.result
      });
    }
    throw new js.JavaScriptErrorInEvaluate('Unexpected response type: ' + JSON.stringify(response));
  }
  async getProperties(context, objectId) {
    throw new Error('Method not implemented.');
  }
  createHandle(context, jsRemoteObject) {
    const remoteObject = jsRemoteObject;
    return new js.JSHandle(context, remoteObject.type, renderPreview(remoteObject), jsRemoteObject.objectId, remoteObjectValue(remoteObject));
  }
  async releaseHandle(objectId) {
    await this._session.send('script.disown', {
      target: this._target,
      handles: [objectId]
    });
  }
  objectCount(objectId) {
    throw new Error('Method not implemented.');
  }
  async rawCallFunction(functionDeclaration, arg) {
    const response = await this._session.send('script.callFunction', {
      functionDeclaration,
      target: this._target,
      arguments: [arg],
      resultOwnership: bidi.Script.ResultOwnership.Root,
      // Necessary for the handle to be returned.
      serializationOptions: {
        maxObjectDepth: 0,
        maxDomDepth: 0
      },
      awaitPromise: true,
      userActivation: true
    });
    if (response.type === 'exception') throw new js.JavaScriptErrorInEvaluate(response.exceptionDetails.text + '\nFull val: ' + JSON.stringify(response.exceptionDetails));
    if (response.type === 'success') return response.result;
    throw new js.JavaScriptErrorInEvaluate('Unexpected response type: ' + JSON.stringify(response));
  }
}
exports.BidiExecutionContext = BidiExecutionContext;
function renderPreview(remoteObject) {
  if (remoteObject.type === 'undefined') return 'undefined';
  if (remoteObject.type === 'null') return 'null';
  if ('value' in remoteObject) return String(remoteObject.value);
  return `<${remoteObject.type}>`;
}
function remoteObjectValue(remoteObject) {
  if (remoteObject.type === 'undefined') return undefined;
  if (remoteObject.type === 'null') return null;
  if (remoteObject.type === 'number' && typeof remoteObject.value === 'string') return js.parseUnserializableValue(remoteObject.value);
  if ('value' in remoteObject) return remoteObject.value;
  return undefined;
}