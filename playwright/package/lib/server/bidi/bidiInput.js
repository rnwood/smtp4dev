"use strict";

Object.defineProperty(exports, "__esModule", {
  value: true
});
exports.RawTouchscreenImpl = exports.RawMouseImpl = exports.RawKeyboardImpl = void 0;
var bidi = _interopRequireWildcard(require("./third_party/bidiProtocol"));
var _bidiKeyboard = require("./third_party/bidiKeyboard");
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

class RawKeyboardImpl {
  constructor(session) {
    this._session = void 0;
    this._session = session;
  }
  setSession(session) {
    this._session = session;
  }
  async keydown(modifiers, code, keyCode, keyCodeWithoutLocation, key, location, autoRepeat, text) {
    const actions = [];
    actions.push({
      type: 'keyDown',
      value: (0, _bidiKeyboard.getBidiKeyValue)(key)
    });
    // TODO: add modifiers?
    await this._performActions(actions);
  }
  async keyup(modifiers, code, keyCode, keyCodeWithoutLocation, key, location) {
    const actions = [];
    actions.push({
      type: 'keyUp',
      value: (0, _bidiKeyboard.getBidiKeyValue)(key)
    });
    await this._performActions(actions);
  }
  async sendText(text) {
    const actions = [];
    for (const char of text) {
      const value = (0, _bidiKeyboard.getBidiKeyValue)(char);
      actions.push({
        type: 'keyDown',
        value
      });
      actions.push({
        type: 'keyUp',
        value
      });
    }
    await this._performActions(actions);
  }
  async _performActions(actions) {
    await this._session.send('input.performActions', {
      context: this._session.sessionId,
      actions: [{
        type: 'key',
        id: 'pw_keyboard',
        actions
      }]
    });
  }
}
exports.RawKeyboardImpl = RawKeyboardImpl;
class RawMouseImpl {
  constructor(session) {
    this._session = void 0;
    this._session = session;
  }
  async move(x, y, button, buttons, modifiers, forClick) {
    // TODO: bidi throws when x/y are not integers.
    x = Math.round(x);
    y = Math.round(y);
    await this._performActions([{
      type: 'pointerMove',
      x,
      y
    }]);
  }
  async down(x, y, button, buttons, modifiers, clickCount) {
    await this._performActions([{
      type: 'pointerDown',
      button: toBidiButton(button)
    }]);
  }
  async up(x, y, button, buttons, modifiers, clickCount) {
    await this._performActions([{
      type: 'pointerUp',
      button: toBidiButton(button)
    }]);
  }
  async click(x, y, options = {}) {
    x = Math.round(x);
    y = Math.round(y);
    const button = toBidiButton(options.button || 'left');
    const {
      delay = null,
      clickCount = 1
    } = options;
    const actions = [];
    actions.push({
      type: 'pointerMove',
      x,
      y
    });
    for (let cc = 1; cc <= clickCount; ++cc) {
      actions.push({
        type: 'pointerDown',
        button
      });
      if (delay) actions.push({
        type: 'pause',
        duration: delay
      });
      actions.push({
        type: 'pointerUp',
        button
      });
      if (delay && cc < clickCount) actions.push({
        type: 'pause',
        duration: delay
      });
    }
    await this._performActions(actions);
  }
  async wheel(x, y, buttons, modifiers, deltaX, deltaY) {}
  async _performActions(actions) {
    await this._session.send('input.performActions', {
      context: this._session.sessionId,
      actions: [{
        type: 'pointer',
        id: 'pw_mouse',
        parameters: {
          pointerType: bidi.Input.PointerType.Mouse
        },
        actions
      }]
    });
  }
}
exports.RawMouseImpl = RawMouseImpl;
class RawTouchscreenImpl {
  constructor(session) {
    this._session = void 0;
    this._session = session;
  }
  async tap(x, y, modifiers) {}
}
exports.RawTouchscreenImpl = RawTouchscreenImpl;
function toBidiButton(button) {
  switch (button) {
    case 'left':
      return 0;
    case 'right':
      return 2;
    case 'middle':
      return 1;
  }
  throw new Error('Unknown button: ' + button);
}