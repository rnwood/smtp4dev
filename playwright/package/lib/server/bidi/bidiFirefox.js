"use strict";

Object.defineProperty(exports, "__esModule", {
  value: true
});
exports.BidiFirefox = void 0;
var _os = _interopRequireDefault(require("os"));
var _path = _interopRequireDefault(require("path"));
var _utils = require("../../utils");
var _browserType = require("../browserType");
var _bidiBrowser = require("./bidiBrowser");
var _bidiConnection = require("./bidiConnection");
function _interopRequireDefault(obj) { return obj && obj.__esModule ? obj : { default: obj }; }
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

class BidiFirefox extends _browserType.BrowserType {
  constructor(parent) {
    super(parent, 'bidi');
    this._useBidi = true;
  }
  async connectToTransport(transport, options) {
    return _bidiBrowser.BidiBrowser.connect(this.attribution.playwright, transport, options);
  }
  doRewriteStartupLog(error) {
    if (!error.logs) return error;
    // https://github.com/microsoft/playwright/issues/6500
    if (error.logs.includes(`as root in a regular user's session is not supported.`)) error.logs = '\n' + (0, _utils.wrapInASCIIBox)(`Firefox is unable to launch if the $HOME folder isn't owned by the current user.\nWorkaround: Set the HOME=/root environment variable${process.env.GITHUB_ACTION ? ' in your GitHub Actions workflow file' : ''} when running Playwright.`, 1);
    if (error.logs.includes('no DISPLAY environment variable specified')) error.logs = '\n' + (0, _utils.wrapInASCIIBox)(_browserType.kNoXServerRunningError, 1);
    return error;
  }
  amendEnvironment(env, userDataDir, executable, browserArguments) {
    if (!_path.default.isAbsolute(_os.default.homedir())) throw new Error(`Cannot launch Firefox with relative home directory. Did you set ${_os.default.platform() === 'win32' ? 'USERPROFILE' : 'HOME'} to a relative path?`);
    if (_os.default.platform() === 'linux') {
      // Always remove SNAP_NAME and SNAP_INSTANCE_NAME env variables since they
      // confuse Firefox: in our case, builds never come from SNAP.
      // See https://github.com/microsoft/playwright/issues/20555
      return {
        ...env,
        SNAP_NAME: undefined,
        SNAP_INSTANCE_NAME: undefined
      };
    }
    return env;
  }
  attemptToGracefullyCloseBrowser(transport) {
    transport.send({
      method: 'browser.close',
      params: {},
      id: _bidiConnection.kBrowserCloseMessageId
    });
  }
  defaultArgs(options, isPersistent, userDataDir) {
    const {
      args = [],
      headless
    } = options;
    const userDataDirArg = args.find(arg => arg.startsWith('-profile') || arg.startsWith('--profile'));
    if (userDataDirArg) throw this._createUserDataDirArgMisuseError('--profile');
    const firefoxArguments = ['--remote-debugging-port=0'];
    if (headless) firefoxArguments.push('--headless');else firefoxArguments.push('--foreground');
    firefoxArguments.push(`--profile`, userDataDir);
    firefoxArguments.push(...args);
    // TODO: make ephemeral context work without this argument.
    firefoxArguments.push('about:blank');
    // if (isPersistent)
    //   firefoxArguments.push('about:blank');
    // else
    //   firefoxArguments.push('-silent');
    return firefoxArguments;
  }
  readyState(options) {
    (0, _utils.assert)(options.useWebSocket);
    return new BidiReadyState();
  }
}
exports.BidiFirefox = BidiFirefox;
class BidiReadyState {
  constructor() {
    this._wsEndpoint = new _utils.ManualPromise();
  }
  onBrowserOutput(message) {
    // Bidi WebSocket in Firefox.
    const match = message.match(/WebDriver BiDi listening on (ws:\/\/.*)$/);
    if (match) this._wsEndpoint.resolve(match[1] + '/session');
  }
  onBrowserExit() {
    // Unblock launch when browser prematurely exits.
    this._wsEndpoint.resolve(undefined);
  }
  async waitUntilReady() {
    const wsEndpoint = await this._wsEndpoint;
    return {
      wsEndpoint
    };
  }
}