"use strict";

Object.defineProperty(exports, "__esModule", {
  value: true
});
exports.Recorder = void 0;
var fs = _interopRequireWildcard(require("fs"));
var consoleApiSource = _interopRequireWildcard(require("../generated/consoleApiSource"));
var _utils = require("../utils");
var _locatorParser = require("../utils/isomorphic/locatorParser");
var _browserContext = require("./browserContext");
var _debugger = require("./debugger");
var _contextRecorder = require("./recorder/contextRecorder");
var _recorderUtils = require("./recorder/recorderUtils");
function _getRequireWildcardCache(e) { if ("function" != typeof WeakMap) return null; var r = new WeakMap(), t = new WeakMap(); return (_getRequireWildcardCache = function (e) { return e ? t : r; })(e); }
function _interopRequireWildcard(e, r) { if (!r && e && e.__esModule) return e; if (null === e || "object" != typeof e && "function" != typeof e) return { default: e }; var t = _getRequireWildcardCache(r); if (t && t.has(e)) return t.get(e); var n = { __proto__: null }, a = Object.defineProperty && Object.getOwnPropertyDescriptor; for (var u in e) if ("default" !== u && Object.prototype.hasOwnProperty.call(e, u)) { var i = a ? Object.getOwnPropertyDescriptor(e, u) : null; i && (i.get || i.set) ? Object.defineProperty(n, u, i) : n[u] = e[u]; } return n.default = e, t && t.set(e, n), n; }
/**
 * Copyright (c) Microsoft Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

const recorderSymbol = Symbol('recorderSymbol');
class Recorder {
  static showInspector(context, recorderAppFactory) {
    const params = {};
    if ((0, _utils.isUnderTest)()) params.language = process.env.TEST_INSPECTOR_LANGUAGE;
    Recorder.show(context, recorderAppFactory, params).catch(() => {});
  }
  static show(context, recorderAppFactory, params = {}) {
    let recorderPromise = context[recorderSymbol];
    if (!recorderPromise) {
      recorderPromise = Recorder._create(context, recorderAppFactory, params);
      context[recorderSymbol] = recorderPromise;
    }
    return recorderPromise;
  }
  static async _create(context, recorderAppFactory, params = {}) {
    const recorder = new Recorder(context, params);
    const recorderApp = await recorderAppFactory(recorder);
    await recorder._install(recorderApp);
    return recorder;
  }
  constructor(context, params) {
    this._context = void 0;
    this._mode = void 0;
    this._highlightedSelector = '';
    this._overlayState = {
      offsetX: 0
    };
    this._recorderApp = null;
    this._currentCallsMetadata = new Map();
    this._recorderSources = [];
    this._userSources = new Map();
    this._debugger = void 0;
    this._contextRecorder = void 0;
    this._omitCallTracking = false;
    this._currentLanguage = void 0;
    this._mode = params.mode || 'none';
    this._contextRecorder = new _contextRecorder.ContextRecorder(context, params, {});
    this._context = context;
    this._omitCallTracking = !!params.omitCallTracking;
    this._debugger = context.debugger();
    context.instrumentation.addListener(this, context);
    this._currentLanguage = this._contextRecorder.languageName();
    if ((0, _utils.isUnderTest)()) {
      // Most of our tests put elements at the top left, so get out of the way.
      this._overlayState.offsetX = 200;
    }
  }
  async _install(recorderApp) {
    this._recorderApp = recorderApp;
    recorderApp.once('close', () => {
      this._debugger.resume(false);
      this._recorderApp = null;
    });
    recorderApp.on('event', data => {
      if (data.event === 'setMode') {
        this.setMode(data.params.mode);
        return;
      }
      if (data.event === 'selectorUpdated') {
        this.setHighlightedSelector(this._currentLanguage, data.params.selector);
        return;
      }
      if (data.event === 'step') {
        this._debugger.resume(true);
        return;
      }
      if (data.event === 'fileChanged') {
        this._currentLanguage = this._contextRecorder.languageName(data.params.file);
        this._refreshOverlay();
        return;
      }
      if (data.event === 'resume') {
        this._debugger.resume(false);
        return;
      }
      if (data.event === 'pause') {
        this._debugger.pauseOnNextStatement();
        return;
      }
      if (data.event === 'clear') {
        this._contextRecorder.clearScript();
        return;
      }
    });
    await Promise.all([recorderApp.setMode(this._mode), recorderApp.setPaused(this._debugger.isPaused()), this._pushAllSources()]);
    this._context.once(_browserContext.BrowserContext.Events.Close, () => {
      var _this$_recorderApp;
      this._contextRecorder.dispose();
      this._context.instrumentation.removeListener(this);
      (_this$_recorderApp = this._recorderApp) === null || _this$_recorderApp === void 0 || _this$_recorderApp.close().catch(() => {});
    });
    this._contextRecorder.on(_contextRecorder.ContextRecorder.Events.Change, data => {
      var _this$_recorderApp2;
      this._recorderSources = data.sources;
      this._pushAllSources();
      (_this$_recorderApp2 = this._recorderApp) === null || _this$_recorderApp2 === void 0 || _this$_recorderApp2.setFileIfNeeded(data.primaryFileName);
    });
    await this._context.exposeBinding('__pw_recorderState', false, source => {
      let actionSelector = '';
      let actionPoint;
      const hasActiveScreenshotCommand = [...this._currentCallsMetadata.keys()].some(isScreenshotCommand);
      if (!hasActiveScreenshotCommand) {
        actionSelector = this._highlightedSelector;
        for (const [metadata, sdkObject] of this._currentCallsMetadata) {
          if (source.page === sdkObject.attribution.page) {
            actionPoint = metadata.point || actionPoint;
            actionSelector = actionSelector || metadata.params.selector;
          }
        }
      }
      const uiState = {
        mode: this._mode,
        actionPoint,
        actionSelector,
        language: this._currentLanguage,
        testIdAttributeName: this._contextRecorder.testIdAttributeName(),
        overlay: this._overlayState
      };
      return uiState;
    });
    await this._context.exposeBinding('__pw_recorderSetSelector', false, async ({
      frame
    }, selector) => {
      var _this$_recorderApp3;
      const selectorChain = await (0, _contextRecorder.generateFrameSelector)(frame);
      await ((_this$_recorderApp3 = this._recorderApp) === null || _this$_recorderApp3 === void 0 ? void 0 : _this$_recorderApp3.setSelector((0, _recorderUtils.buildFullSelector)(selectorChain, selector), true));
    });
    await this._context.exposeBinding('__pw_recorderSetMode', false, async ({
      frame
    }, mode) => {
      if (frame.parentFrame()) return;
      this.setMode(mode);
    });
    await this._context.exposeBinding('__pw_recorderSetOverlayState', false, async ({
      frame
    }, state) => {
      if (frame.parentFrame()) return;
      this._overlayState = state;
    });
    await this._context.exposeBinding('__pw_resume', false, () => {
      this._debugger.resume(false);
    });
    await this._context.extendInjectedScript(consoleApiSource.source);
    await this._contextRecorder.install();
    if (this._debugger.isPaused()) this._pausedStateChanged();
    this._debugger.on(_debugger.Debugger.Events.PausedStateChanged, () => this._pausedStateChanged());
    this._context.recorderAppForTest = this._recorderApp;
  }
  _pausedStateChanged() {
    var _this$_recorderApp4;
    // If we are called upon page.pause, we don't have metadatas, populate them.
    for (const {
      metadata,
      sdkObject
    } of this._debugger.pausedDetails()) {
      if (!this._currentCallsMetadata.has(metadata)) this.onBeforeCall(sdkObject, metadata);
    }
    (_this$_recorderApp4 = this._recorderApp) === null || _this$_recorderApp4 === void 0 || _this$_recorderApp4.setPaused(this._debugger.isPaused());
    this._updateUserSources();
    this.updateCallLog([...this._currentCallsMetadata.keys()]);
  }
  setMode(mode) {
    var _this$_recorderApp5;
    if (this._mode === mode) return;
    this._highlightedSelector = '';
    this._mode = mode;
    (_this$_recorderApp5 = this._recorderApp) === null || _this$_recorderApp5 === void 0 || _this$_recorderApp5.setMode(this._mode);
    this._contextRecorder.setEnabled(this._mode === 'recording' || this._mode === 'assertingText' || this._mode === 'assertingVisibility' || this._mode === 'assertingValue');
    this._debugger.setMuted(this._mode === 'recording' || this._mode === 'assertingText' || this._mode === 'assertingVisibility' || this._mode === 'assertingValue');
    if (this._mode !== 'none' && this._mode !== 'standby' && this._context.pages().length === 1) this._context.pages()[0].bringToFront().catch(() => {});
    this._refreshOverlay();
  }
  resume() {
    this._debugger.resume(false);
  }
  mode() {
    return this._mode;
  }
  setHighlightedSelector(language, selector) {
    this._highlightedSelector = (0, _locatorParser.locatorOrSelectorAsSelector)(language, selector, this._context.selectors().testIdAttributeName());
    this._refreshOverlay();
  }
  hideHighlightedSelector() {
    this._highlightedSelector = '';
    this._refreshOverlay();
  }
  setOutput(codegenId, outputFile) {
    this._contextRecorder.setOutput(codegenId, outputFile);
  }
  _refreshOverlay() {
    for (const page of this._context.pages()) page.mainFrame().evaluateExpression('window.__pw_refreshOverlay()').catch(() => {});
  }
  async onBeforeCall(sdkObject, metadata) {
    if (this._omitCallTracking || this._mode === 'recording' || this._mode === 'assertingText' || this._mode === 'assertingVisibility' || this._mode === 'assertingValue') return;
    this._currentCallsMetadata.set(metadata, sdkObject);
    this._updateUserSources();
    this.updateCallLog([metadata]);
    if (isScreenshotCommand(metadata)) {
      this.hideHighlightedSelector();
    } else if (metadata.params && metadata.params.selector) {
      var _this$_recorderApp6;
      this._highlightedSelector = metadata.params.selector;
      (_this$_recorderApp6 = this._recorderApp) === null || _this$_recorderApp6 === void 0 || _this$_recorderApp6.setSelector(this._highlightedSelector).catch(() => {});
    }
  }
  async onAfterCall(sdkObject, metadata) {
    if (this._omitCallTracking || this._mode === 'recording' || this._mode === 'assertingText' || this._mode === 'assertingVisibility' || this._mode === 'assertingValue') return;
    if (!metadata.error) this._currentCallsMetadata.delete(metadata);
    this._updateUserSources();
    this.updateCallLog([metadata]);
  }
  _updateUserSources() {
    var _this$_recorderApp7;
    // Remove old decorations.
    for (const source of this._userSources.values()) {
      source.highlight = [];
      source.revealLine = undefined;
    }

    // Apply new decorations.
    let fileToSelect = undefined;
    for (const metadata of this._currentCallsMetadata.keys()) {
      if (!metadata.location) continue;
      const {
        file,
        line
      } = metadata.location;
      let source = this._userSources.get(file);
      if (!source) {
        source = {
          isRecorded: false,
          label: file,
          id: file,
          text: this._readSource(file),
          highlight: [],
          language: languageForFile(file)
        };
        this._userSources.set(file, source);
      }
      if (line) {
        const paused = this._debugger.isPaused(metadata);
        source.highlight.push({
          line,
          type: metadata.error ? 'error' : paused ? 'paused' : 'running'
        });
        source.revealLine = line;
        fileToSelect = source.id;
      }
    }
    this._pushAllSources();
    if (fileToSelect) (_this$_recorderApp7 = this._recorderApp) === null || _this$_recorderApp7 === void 0 || _this$_recorderApp7.setFileIfNeeded(fileToSelect);
  }
  _pushAllSources() {
    var _this$_recorderApp8;
    (_this$_recorderApp8 = this._recorderApp) === null || _this$_recorderApp8 === void 0 || _this$_recorderApp8.setSources([...this._recorderSources, ...this._userSources.values()]);
  }
  async onBeforeInputAction(sdkObject, metadata) {}
  async onCallLog(sdkObject, metadata, logName, message) {
    this.updateCallLog([metadata]);
  }
  updateCallLog(metadatas) {
    var _this$_recorderApp9;
    if (this._mode === 'recording' || this._mode === 'assertingText' || this._mode === 'assertingVisibility' || this._mode === 'assertingValue') return;
    const logs = [];
    for (const metadata of metadatas) {
      if (!metadata.method || metadata.internal) continue;
      let status = 'done';
      if (this._currentCallsMetadata.has(metadata)) status = 'in-progress';
      if (this._debugger.isPaused(metadata)) status = 'paused';
      logs.push((0, _recorderUtils.metadataToCallLog)(metadata, status));
    }
    (_this$_recorderApp9 = this._recorderApp) === null || _this$_recorderApp9 === void 0 || _this$_recorderApp9.updateCallLogs(logs);
  }
  _readSource(fileName) {
    try {
      return fs.readFileSync(fileName, 'utf-8');
    } catch (e) {
      return '// No source available';
    }
  }
}
exports.Recorder = Recorder;
function isScreenshotCommand(metadata) {
  return metadata.method.toLowerCase().includes('screenshot');
}
function languageForFile(file) {
  if (file.endsWith('.py')) return 'python';
  if (file.endsWith('.java')) return 'java';
  if (file.endsWith('.cs')) return 'csharp';
  return 'javascript';
}