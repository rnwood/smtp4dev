"use strict";

Object.defineProperty(exports, "__esModule", {
  value: true
});
exports.BidiPage = void 0;
var _eventsHelper = require("../../utils/eventsHelper");
var _utils = require("../../utils");
var dom = _interopRequireWildcard(require("../dom"));
var dialog = _interopRequireWildcard(require("../dialog"));
var _page = require("../page");
var _bidiInput = require("./bidiInput");
var bidi = _interopRequireWildcard(require("./third_party/bidiProtocol"));
var _bidiExecutionContext = require("./bidiExecutionContext");
var _bidiNetworkManager = require("./bidiNetworkManager");
var _browserContext = require("../browserContext");
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

const UTILITY_WORLD_NAME = '__playwright_utility_world__';
class BidiPage {
  constructor(browserContext, bidiSession, opener) {
    this.rawMouse = void 0;
    this.rawKeyboard = void 0;
    this.rawTouchscreen = void 0;
    this._page = void 0;
    this._pagePromise = void 0;
    this._session = void 0;
    this._opener = void 0;
    this._realmToContext = void 0;
    this._sessionListeners = [];
    this._browserContext = void 0;
    this._networkManager = void 0;
    this._initializedPage = null;
    this._session = bidiSession;
    this._opener = opener;
    this.rawKeyboard = new _bidiInput.RawKeyboardImpl(bidiSession);
    this.rawMouse = new _bidiInput.RawMouseImpl(bidiSession);
    this.rawTouchscreen = new _bidiInput.RawTouchscreenImpl(bidiSession);
    this._realmToContext = new Map();
    this._page = new _page.Page(this, browserContext);
    this._browserContext = browserContext;
    this._networkManager = new _bidiNetworkManager.BidiNetworkManager(this._session, this._page, this._onNavigationResponseStarted.bind(this));
    this._page.on(_page.Page.Events.FrameDetached, frame => this._removeContextsForFrame(frame, false));
    this._sessionListeners = [_eventsHelper.eventsHelper.addEventListener(bidiSession, 'script.realmCreated', this._onRealmCreated.bind(this)), _eventsHelper.eventsHelper.addEventListener(bidiSession, 'browsingContext.contextDestroyed', this._onBrowsingContextDestroyed.bind(this)), _eventsHelper.eventsHelper.addEventListener(bidiSession, 'browsingContext.navigationStarted', this._onNavigationStarted.bind(this)), _eventsHelper.eventsHelper.addEventListener(bidiSession, 'browsingContext.navigationAborted', this._onNavigationAborted.bind(this)), _eventsHelper.eventsHelper.addEventListener(bidiSession, 'browsingContext.navigationFailed', this._onNavigationFailed.bind(this)), _eventsHelper.eventsHelper.addEventListener(bidiSession, 'browsingContext.fragmentNavigated', this._onFragmentNavigated.bind(this)), _eventsHelper.eventsHelper.addEventListener(bidiSession, 'browsingContext.domContentLoaded', this._onDomContentLoaded.bind(this)), _eventsHelper.eventsHelper.addEventListener(bidiSession, 'browsingContext.load', this._onLoad.bind(this)), _eventsHelper.eventsHelper.addEventListener(bidiSession, 'browsingContext.userPromptOpened', this._onUserPromptOpened.bind(this)), _eventsHelper.eventsHelper.addEventListener(bidiSession, 'log.entryAdded', this._onLogEntryAdded.bind(this))];

    // Initialize main frame.
    this._pagePromise = this._initialize().finally(async () => {
      await this._page.initOpener(this._opener);
    }).then(() => {
      this._initializedPage = this._page;
      this._page.reportAsNew();
      return this._page;
    }).catch(e => {
      this._page.reportAsNew(e);
      return e;
    });
  }
  async _initialize() {
    const {
      contexts
    } = await this._session.send('browsingContext.getTree', {
      root: this._session.sessionId
    });
    this._handleFrameTree(contexts[0]);
    await Promise.all([this.updateHttpCredentials(), this.updateRequestInterception(), this._updateViewport()]);
  }
  _handleFrameTree(frameTree) {
    this._onFrameAttached(frameTree.context, frameTree.parent || null);
    if (!frameTree.children) return;
    for (const child of frameTree.children) this._handleFrameTree(child);
  }
  potentiallyUninitializedPage() {
    return this._page;
  }
  didClose() {
    this._session.dispose();
    _eventsHelper.eventsHelper.removeEventListeners(this._sessionListeners);
    this._page._didClose();
  }
  async pageOrError() {
    // TODO: Wait for first execution context to be created and maybe about:blank navigated.
    return this._pagePromise;
  }
  _onFrameAttached(frameId, parentFrameId) {
    return this._page._frameManager.frameAttached(frameId, parentFrameId);
  }
  _removeContextsForFrame(frame, notifyFrame) {
    for (const [contextId, context] of this._realmToContext) {
      if (context.frame === frame) {
        this._realmToContext.delete(contextId);
        if (notifyFrame) frame._contextDestroyed(context);
      }
    }
  }
  _onRealmCreated(realmInfo) {
    if (this._realmToContext.has(realmInfo.realm)) return;
    if (realmInfo.type !== 'window') return;
    const frame = this._page._frameManager.frame(realmInfo.context);
    if (!frame) return;
    const delegate = new _bidiExecutionContext.BidiExecutionContext(this._session, realmInfo);
    let worldName;
    if (!realmInfo.sandbox) {
      worldName = 'main';
      // Force creating utility world every time the main world is created (e.g. due to navigation).
      this._touchUtilityWorld(realmInfo.context);
    } else if (realmInfo.sandbox === UTILITY_WORLD_NAME) {
      worldName = 'utility';
    } else {
      return;
    }
    const context = new dom.FrameExecutionContext(delegate, frame, worldName);
    context[contextDelegateSymbol] = delegate;
    frame._contextCreated(worldName, context);
    this._realmToContext.set(realmInfo.realm, context);
  }
  async _touchUtilityWorld(context) {
    await this._session.sendMayFail('script.evaluate', {
      expression: '1 + 1',
      target: {
        context,
        sandbox: UTILITY_WORLD_NAME
      },
      serializationOptions: {
        maxObjectDepth: 10,
        maxDomDepth: 10
      },
      awaitPromise: true,
      userActivation: true
    });
  }
  _onRealmDestroyed(params) {
    const context = this._realmToContext.get(params.realm);
    if (!context) return false;
    this._realmToContext.delete(params.realm);
    context.frame._contextDestroyed(context);
    return true;
  }

  // TODO: route the message directly to the browser
  _onBrowsingContextDestroyed(params) {
    this._browserContext._browser._onBrowsingContextDestroyed(params);
  }
  _onNavigationStarted(params) {
    const frameId = params.context;
    this._page._frameManager.frameRequestedNavigation(frameId, params.navigation);
    const url = params.url.toLowerCase();
    if (url.startsWith('file:') || url.startsWith('data:') || url === 'about:blank') {
      // Navigation to file urls doesn't emit network events, so we fire 'commit' event right when navigation is started.
      // Doing it in domcontentload would be too late as we'd clear frame tree.
      const frame = this._page._frameManager.frame(frameId);
      if (frame) this._page._frameManager.frameCommittedNewDocumentNavigation(frameId, params.url, '', params.navigation, /* initial */false);
    }
  }

  // TODO: there is no separate event for committed navigation, so we approximate it with responseStarted.
  _onNavigationResponseStarted(params) {
    const frameId = params.context;
    const frame = this._page._frameManager.frame(frameId);
    (0, _utils.assert)(frame);
    this._page._frameManager.frameCommittedNewDocumentNavigation(frameId, params.response.url, '', params.navigation, /* initial */false);
    // if (!initial)
    //   this._firstNonInitialNavigationCommittedFulfill();
  }
  _onDomContentLoaded(params) {
    const frameId = params.context;
    this._page._frameManager.frameLifecycleEvent(frameId, 'domcontentloaded');
  }
  _onLoad(params) {
    this._page._frameManager.frameLifecycleEvent(params.context, 'load');
  }
  _onNavigationAborted(params) {
    this._page._frameManager.frameAbortedNavigation(params.context, 'Navigation aborted', params.navigation || undefined);
  }
  _onNavigationFailed(params) {
    this._page._frameManager.frameAbortedNavigation(params.context, 'Navigation failed', params.navigation || undefined);
  }
  _onFragmentNavigated(params) {
    this._page._frameManager.frameCommittedSameDocumentNavigation(params.context, params.url);
  }
  _onUserPromptOpened(event) {
    this._page.emitOnContext(_browserContext.BrowserContext.Events.Dialog, new dialog.Dialog(this._page, event.type, event.message, async (accept, userText) => {
      await this._session.send('browsingContext.handleUserPrompt', {
        context: event.context,
        accept,
        userText
      });
    }, event.defaultValue));
  }
  _onLogEntryAdded(params) {
    var _params$stackTrace;
    if (params.type !== 'console') return;
    const entry = params;
    const context = this._realmToContext.get(params.source.realm);
    if (!context) return;
    const callFrame = (_params$stackTrace = params.stackTrace) === null || _params$stackTrace === void 0 ? void 0 : _params$stackTrace.callFrames[0];
    const location = callFrame !== null && callFrame !== void 0 ? callFrame : {
      url: '',
      lineNumber: 1,
      columnNumber: 1
    };
    this._page._addConsoleMessage(entry.method, entry.args.map(arg => context.createHandle({
      objectId: arg.handle,
      ...arg
    })), location, params.text || undefined);
  }
  async navigateFrame(frame, url, referrer) {
    const {
      navigation
    } = await this._session.send('browsingContext.navigate', {
      context: frame._id,
      url
    });
    return {
      newDocumentId: navigation || undefined
    };
  }
  async updateExtraHTTPHeaders() {}
  async updateEmulateMedia() {}
  async updateEmulatedViewportSize() {
    await this._updateViewport();
  }
  async updateUserAgent() {}
  async bringToFront() {}
  async _updateViewport() {
    const options = this._browserContext._options;
    const deviceSize = this._page.emulatedSize();
    if (deviceSize === null) return;
    const viewportSize = deviceSize.viewport;
    await this._session.send('browsingContext.setViewport', {
      context: this._session.sessionId,
      viewport: {
        width: viewportSize.width,
        height: viewportSize.height
      },
      devicePixelRatio: options.deviceScaleFactor || 1
    });
  }
  async updateRequestInterception() {
    await this._networkManager.setRequestInterception(this._page.needsRequestInterception());
  }
  async updateOffline() {}
  async updateHttpCredentials() {
    await this._networkManager.setCredentials(this._browserContext._options.httpCredentials);
  }
  async updateFileChooserInterception() {}
  async reload() {
    await this._session.send('browsingContext.reload', {
      context: this._session.sessionId,
      // ignoreCache: true,
      wait: bidi.BrowsingContext.ReadinessState.Interactive
    });
  }
  goBack() {
    throw new Error('Method not implemented.');
  }
  goForward() {
    throw new Error('Method not implemented.');
  }
  async addInitScript(initScript) {
    await this._updateBootstrapScript();
  }
  async removeNonInternalInitScripts() {
    await this._updateBootstrapScript();
  }
  async _updateBootstrapScript() {
    throw new Error('Method not implemented.');
  }
  async closePage(runBeforeUnload) {
    await this._session.send('browsingContext.close', {
      context: this._session.sessionId,
      promptUnload: runBeforeUnload
    });
  }
  async setBackgroundColor(color) {}
  async takeScreenshot(progress, format, documentRect, viewportRect, quality, fitsViewport, scale) {
    throw new Error('Method not implemented.');
  }
  async getContentFrame(handle) {
    const executionContext = toBidiExecutionContext(handle._context);
    const contentWindow = await executionContext.rawCallFunction('e => e.contentWindow', {
      handle: handle._objectId
    });
    if (contentWindow.type === 'window') {
      const frameId = contentWindow.value.context;
      const result = this._page._frameManager.frame(frameId);
      return result;
    }
    return null;
  }
  async getOwnerFrame(handle) {
    throw new Error('Method not implemented.');
  }
  isElementHandle(remoteObject) {
    return remoteObject.type === 'node';
  }
  async getBoundingBox(handle) {
    const box = await handle.evaluate(element => {
      if (!(element instanceof Element)) return null;
      const rect = element.getBoundingClientRect();
      return {
        x: rect.x,
        y: rect.y,
        width: rect.width,
        height: rect.height
      };
    });
    if (!box) return null;
    const position = await this._framePosition(handle._frame);
    if (!position) return null;
    box.x += position.x;
    box.y += position.y;
    return box;
  }

  // TODO: move to Frame.
  async _framePosition(frame) {
    if (frame === this._page.mainFrame()) return {
      x: 0,
      y: 0
    };
    const element = await frame.frameElement();
    const box = await element.boundingBox();
    if (!box) return null;
    const style = await element.evaluateInUtility(([injected, iframe]) => injected.describeIFrameStyle(iframe), {}).catch(e => 'error:notconnected');
    if (style === 'error:notconnected' || style === 'transformed') return null;
    // Content box is offset by border and padding widths.
    box.x += style.left;
    box.y += style.top;
    return box;
  }
  async scrollRectIntoViewIfNeeded(handle, rect) {
    return await handle.evaluateInUtility(([injected, node]) => {
      node.scrollIntoView({
        block: 'center',
        inline: 'center',
        behavior: 'instant'
      });
    }, null).then(() => 'done').catch(e => {
      if (e instanceof Error && e.message.includes('Node is detached from document')) return 'error:notconnected';
      if (e instanceof Error && e.message.includes('Node does not have a layout object')) return 'error:notvisible';
      throw e;
    });
  }
  async setScreencastOptions(options) {}
  rafCountForStablePosition() {
    return 1;
  }
  async getContentQuads(handle) {
    const quads = await handle.evaluateInUtility(([injected, node]) => {
      if (!node.isConnected) return 'error:notconnected';
      const rects = node.getClientRects();
      if (!rects) return null;
      return [...rects].map(rect => [{
        x: rect.left,
        y: rect.top
      }, {
        x: rect.right,
        y: rect.top
      }, {
        x: rect.right,
        y: rect.bottom
      }, {
        x: rect.left,
        y: rect.bottom
      }]);
    }, null);
    if (!quads || quads === 'error:notconnected') return quads;
    // TODO: consider transforming quads to support clicks in iframes.
    const position = await this._framePosition(handle._frame);
    if (!position) return null;
    quads.forEach(quad => quad.forEach(point => {
      point.x += position.x;
      point.y += position.y;
    }));
    return quads;
  }
  async setInputFiles(handle, files) {
    throw new Error('Method not implemented.');
  }
  async setInputFilePaths(handle, paths) {
    throw new Error('Method not implemented.');
  }
  async adoptElementHandle(handle, to) {
    const fromContext = toBidiExecutionContext(handle._context);
    const shared = await fromContext.rawCallFunction('x => x', {
      handle: handle._objectId
    });
    // TODO: store sharedId in the handle.
    if (!('sharedId' in shared)) throw new Error('Element is not a node');
    const sharedId = shared.sharedId;
    const executionContext = toBidiExecutionContext(to);
    const result = await executionContext.rawCallFunction('x => x', {
      sharedId
    });
    if ('handle' in result) return to.createHandle({
      objectId: result.handle,
      ...result
    });
    throw new Error('Failed to adopt element handle.');
  }
  async getAccessibilityTree(needle) {
    throw new Error('Method not implemented.');
  }
  async inputActionEpilogue() {}
  async resetForReuse() {}
  async getFrameElement(frame) {
    const parent = frame.parentFrame();
    if (!parent) throw new Error('Frame has been detached.');
    const parentContext = await parent._mainContext();
    const list = await parentContext.evaluateHandle(() => {
      return [...document.querySelectorAll('iframe,frame')];
    });
    const length = await list.evaluate(list => list.length);
    let foundElement = null;
    for (let i = 0; i < length; i++) {
      const element = await list.evaluateHandle((list, i) => list[i], i);
      const candidate = await element.contentFrame();
      if (frame === candidate) {
        foundElement = element;
        break;
      } else {
        element.dispose();
      }
    }
    list.dispose();
    if (!foundElement) throw new Error('Frame has been detached.');
    return foundElement;
  }
  shouldToggleStyleSheetToSyncAnimations() {
    return true;
  }
  useMainWorldForSetContent() {
    return true;
  }
}
exports.BidiPage = BidiPage;
function toBidiExecutionContext(executionContext) {
  return executionContext[contextDelegateSymbol];
}
const contextDelegateSymbol = Symbol('delegate');