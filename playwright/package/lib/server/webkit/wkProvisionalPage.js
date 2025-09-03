"use strict";

Object.defineProperty(exports, "__esModule", {
  value: true
});
exports.WKProvisionalPage = void 0;
var _eventsHelper = require("../../utils/eventsHelper");
var _utils = require("../../utils");
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

class WKProvisionalPage {
  constructor(session, page) {
    var _page$_page$mainFrame;
    this._session = void 0;
    this._wkPage = void 0;
    this._coopNavigationRequest = void 0;
    this._sessionListeners = [];
    this._mainFrameId = null;
    this.initializationPromise = void 0;
    this._session = session;
    this._wkPage = page;
    // Cross-Origin-Opener-Policy (COOP) request starts in one process and once response headers
    // have been received, continues in another.
    //
    // Network.requestWillBeSent and requestIntercepted (if intercepting) from the original web process
    // will always come before a provisional page is created based on the response COOP headers.
    // Thereafter we'll receive targetCreated (provisional) and later on in some order loadingFailed from the
    // original process and requestWillBeSent from the provisional one. We should ignore loadingFailed
    // as the original request continues in the provisional process. But if the provisional load is later
    // canceled we should dispatch loadingFailed to the client.
    this._coopNavigationRequest = (_page$_page$mainFrame = page._page.mainFrame().pendingDocument()) === null || _page$_page$mainFrame === void 0 ? void 0 : _page$_page$mainFrame.request;
    const overrideFrameId = handler => {
      return payload => {
        // Pretend that the events happened in the same process.
        if (payload.frameId) payload.frameId = this._wkPage._page._frameManager.mainFrame()._id;
        handler(payload);
      };
    };
    const wkPage = this._wkPage;
    this._sessionListeners = [_eventsHelper.eventsHelper.addEventListener(session, 'Network.requestWillBeSent', overrideFrameId(e => this._onRequestWillBeSent(e))), _eventsHelper.eventsHelper.addEventListener(session, 'Network.requestIntercepted', overrideFrameId(e => wkPage._onRequestIntercepted(session, e))), _eventsHelper.eventsHelper.addEventListener(session, 'Network.responseReceived', overrideFrameId(e => wkPage._onResponseReceived(session, e))), _eventsHelper.eventsHelper.addEventListener(session, 'Network.loadingFinished', overrideFrameId(e => this._onLoadingFinished(e))), _eventsHelper.eventsHelper.addEventListener(session, 'Network.loadingFailed', overrideFrameId(e => this._onLoadingFailed(e)))];
    this.initializationPromise = this._wkPage._initializeSession(session, true, ({
      frameTree
    }) => this._handleFrameTree(frameTree));
  }
  coopNavigationRequest() {
    return this._coopNavigationRequest;
  }
  dispose() {
    _eventsHelper.eventsHelper.removeEventListeners(this._sessionListeners);
  }
  commit() {
    (0, _utils.assert)(this._mainFrameId);
    this._wkPage._onFrameAttached(this._mainFrameId, null);
  }
  _onRequestWillBeSent(event) {
    if (this._coopNavigationRequest && this._coopNavigationRequest.url() === event.request.url) {
      // If it's a continuation of the main frame navigation request after COOP headers were received,
      // take over original request, and replace its request id with the new one.
      this._wkPage._adoptRequestFromNewProcess(this._coopNavigationRequest, this._session, event.requestId);
      // Simply ignore this event as it has already been dispatched from the original process
      // and there will ne no requestIntercepted event from the provisional process as it resumes
      // existing network load (that has already received reponse headers).
      return;
    }
    this._wkPage._onRequestWillBeSent(this._session, event);
  }
  _onLoadingFinished(event) {
    this._coopNavigationRequest = undefined;
    this._wkPage._onLoadingFinished(event);
  }
  _onLoadingFailed(event) {
    this._coopNavigationRequest = undefined;
    this._wkPage._onLoadingFailed(this._session, event);
  }
  _handleFrameTree(frameTree) {
    (0, _utils.assert)(!frameTree.frame.parentId);
    this._mainFrameId = frameTree.frame.id;
  }
}
exports.WKProvisionalPage = WKProvisionalPage;