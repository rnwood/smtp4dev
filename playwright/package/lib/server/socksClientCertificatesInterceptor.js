"use strict";

Object.defineProperty(exports, "__esModule", {
  value: true
});
exports.ClientCertificatesProxy = void 0;
exports.getMatchingTLSOptionsForOrigin = getMatchingTLSOptionsForOrigin;
exports.rewriteOpenSSLErrorIfNeeded = rewriteOpenSSLErrorIfNeeded;
var _net = _interopRequireDefault(require("net"));
var _http = _interopRequireDefault(require("http2"));
var _tls = _interopRequireDefault(require("tls"));
var _stream = _interopRequireDefault(require("stream"));
var _happyEyeballs = require("../utils/happy-eyeballs");
var _utils = require("../utils");
var _socksProxy = require("../common/socksProxy");
var _debugLogger = require("../utils/debugLogger");
function _interopRequireDefault(obj) { return obj && obj.__esModule ? obj : { default: obj }; }
/**
 * Copyright (c) Microsoft Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the 'License");
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

let dummyServerTlsOptions = undefined;
function loadDummyServerCertsIfNeeded() {
  if (dummyServerTlsOptions) return;
  const {
    cert,
    key
  } = (0, _utils.generateSelfSignedCertificate)();
  dummyServerTlsOptions = {
    key,
    cert
  };
}
class ALPNCache {
  constructor() {
    this._cache = new Map();
  }
  get(host, port, success) {
    const cacheKey = `${host}:${port}`;
    {
      const result = this._cache.get(cacheKey);
      if (result) {
        result.then(success);
        return;
      }
    }
    const result = new _utils.ManualPromise();
    this._cache.set(cacheKey, result);
    result.then(success);
    (0, _happyEyeballs.createTLSSocket)({
      host,
      port,
      servername: _net.default.isIP(host) ? undefined : host,
      ALPNProtocols: ['h2', 'http/1.1'],
      rejectUnauthorized: false
    }).then(socket => {
      // The server may not respond with ALPN, in which case we default to http/1.1.
      result.resolve(socket.alpnProtocol || 'http/1.1');
      socket.end();
    }).catch(error => {
      _debugLogger.debugLogger.log('client-certificates', `ALPN error: ${error.message}`);
      result.resolve('http/1.1');
    });
  }
}
class SocksProxyConnection {
  constructor(socksProxy, uid, host, port) {
    this.socksProxy = void 0;
    this.uid = void 0;
    this.host = void 0;
    this.port = void 0;
    this.firstPackageReceived = false;
    this.target = void 0;
    // In case of http, we just pipe data to the target socket and they are |undefined|.
    this.internal = void 0;
    this.internalTLS = void 0;
    this._targetCloseEventListener = void 0;
    this._dummyServer = void 0;
    this._closed = false;
    this.socksProxy = socksProxy;
    this.uid = uid;
    this.host = host;
    this.port = port;
    this._targetCloseEventListener = () => {
      var _this$internalTLS, _this$_dummyServer;
      // Close the other end and cleanup TLS resources.
      this.socksProxy._socksProxy.sendSocketEnd({
        uid: this.uid
      });
      (_this$internalTLS = this.internalTLS) === null || _this$internalTLS === void 0 || _this$internalTLS.destroy();
      (_this$_dummyServer = this._dummyServer) === null || _this$_dummyServer === void 0 || _this$_dummyServer.close();
    };
  }
  async connect() {
    this.target = await (0, _happyEyeballs.createSocket)(rewriteToLocalhostIfNeeded(this.host), this.port);
    this.target.once('close', this._targetCloseEventListener);
    this.target.once('error', error => this.socksProxy._socksProxy.sendSocketError({
      uid: this.uid,
      error: error.message
    }));
    if (this._closed) {
      this.target.destroy();
      return;
    }
    this.socksProxy._socksProxy.socketConnected({
      uid: this.uid,
      host: this.target.localAddress,
      port: this.target.localPort
    });
  }
  onClose() {
    var _this$internalTLS2, _this$_dummyServer2;
    // Close the other end and cleanup TLS resources.
    this.target.destroy();
    (_this$internalTLS2 = this.internalTLS) === null || _this$internalTLS2 === void 0 || _this$internalTLS2.destroy();
    (_this$_dummyServer2 = this._dummyServer) === null || _this$_dummyServer2 === void 0 || _this$_dummyServer2.close();
    this._closed = true;
  }
  onData(data) {
    // HTTP / TLS are client-hello based protocols. This allows us to detect
    // the protocol on the first package and attach appropriate listeners.
    if (!this.firstPackageReceived) {
      this.firstPackageReceived = true;
      // 0x16 is SSLv3/TLS "handshake" content type: https://en.wikipedia.org/wiki/Transport_Layer_Security#TLS_record
      if (data[0] === 0x16) this._attachTLSListeners();else this.target.on('data', data => this.socksProxy._socksProxy.sendSocketData({
        uid: this.uid,
        data
      }));
    }
    if (this.internal) this.internal.push(data);else this.target.write(data);
  }
  _attachTLSListeners() {
    this.internal = new _stream.default.Duplex({
      read: () => {},
      write: (data, encoding, callback) => {
        this.socksProxy._socksProxy.sendSocketData({
          uid: this.uid,
          data
        });
        callback();
      }
    });
    this.socksProxy.alpnCache.get(rewriteToLocalhostIfNeeded(this.host), this.port, alpnProtocolChosenByServer => {
      _debugLogger.debugLogger.log('client-certificates', `Proxy->Target ${this.host}:${this.port} chooses ALPN ${alpnProtocolChosenByServer}`);
      if (this._closed) return;
      this._dummyServer = _tls.default.createServer({
        ...dummyServerTlsOptions,
        ALPNProtocols: alpnProtocolChosenByServer === 'h2' ? ['h2', 'http/1.1'] : ['http/1.1']
      });
      this._dummyServer.emit('connection', this.internal);
      this._dummyServer.once('secureConnection', internalTLS => {
        this.internalTLS = internalTLS;
        _debugLogger.debugLogger.log('client-certificates', `Browser->Proxy ${this.host}:${this.port} chooses ALPN ${internalTLS.alpnProtocol}`);
        let targetTLS = undefined;
        const handleError = error => {
          _debugLogger.debugLogger.log('client-certificates', `error when connecting to target: ${error.message.replaceAll('\n', ' ')}`);
          const responseBody = (0, _utils.escapeHTML)('Playwright client-certificate error: ' + error.message).replaceAll('\n', ' <br>');
          if ((internalTLS === null || internalTLS === void 0 ? void 0 : internalTLS.alpnProtocol) === 'h2') {
            // This method is available only in Node.js 20+
            if ('performServerHandshake' in _http.default) {
              // In case of an 'error' event on the target connection, we still need to perform the http2 handshake on the browser side.
              // This is an async operation, so we need to remove the listener to prevent the socket from being closed too early.
              // This means we call this._targetCloseEventListener manually.
              this.target.removeListener('close', this._targetCloseEventListener);
              // @ts-expect-error
              const session = _http.default.performServerHandshake(internalTLS);
              session.on('error', () => {
                this.target.destroy();
                this._targetCloseEventListener();
              });
              session.once('stream', stream => {
                stream.respond({
                  'content-type': 'text/html',
                  [_http.default.constants.HTTP2_HEADER_STATUS]: 503
                });
                const cleanup = () => {
                  session.close();
                  this.target.destroy();
                  this._targetCloseEventListener();
                };
                stream.end(responseBody, cleanup);
                stream.once('error', cleanup);
              });
            } else {
              this.target.destroy();
            }
          } else {
            internalTLS.end(['HTTP/1.1 503 Internal Server Error', 'Content-Type: text/html; charset=utf-8', 'Content-Length: ' + Buffer.byteLength(responseBody), '', responseBody].join('\r\n'));
            this.target.destroy();
          }
        };
        if (this._closed) {
          internalTLS.destroy();
          return;
        }
        targetTLS = _tls.default.connect({
          socket: this.target,
          host: this.host,
          port: this.port,
          rejectUnauthorized: !this.socksProxy.ignoreHTTPSErrors,
          ALPNProtocols: [internalTLS.alpnProtocol || 'http/1.1'],
          servername: !_net.default.isIP(this.host) ? this.host : undefined,
          secureContext: this.socksProxy.secureContextMap.get(new URL(`https://${this.host}:${this.port}`).origin)
        });
        targetTLS.once('secureConnect', () => {
          internalTLS.pipe(targetTLS);
          targetTLS.pipe(internalTLS);
        });
        internalTLS.once('error', () => this.target.destroy());
        targetTLS.once('error', handleError);
      });
    });
  }
}
class ClientCertificatesProxy {
  constructor(contextOptions) {
    this._socksProxy = void 0;
    this._connections = new Map();
    this.ignoreHTTPSErrors = void 0;
    this.secureContextMap = new Map();
    this.alpnCache = void 0;
    this.alpnCache = new ALPNCache();
    this.ignoreHTTPSErrors = contextOptions.ignoreHTTPSErrors;
    this._initSecureContexts(contextOptions.clientCertificates);
    this._socksProxy = new _socksProxy.SocksProxy();
    this._socksProxy.setPattern('*');
    this._socksProxy.addListener(_socksProxy.SocksProxy.Events.SocksRequested, async payload => {
      try {
        const connection = new SocksProxyConnection(this, payload.uid, payload.host, payload.port);
        await connection.connect();
        this._connections.set(payload.uid, connection);
      } catch (error) {
        this._socksProxy.socketFailed({
          uid: payload.uid,
          errorCode: error.code
        });
      }
    });
    this._socksProxy.addListener(_socksProxy.SocksProxy.Events.SocksData, async payload => {
      var _this$_connections$ge;
      (_this$_connections$ge = this._connections.get(payload.uid)) === null || _this$_connections$ge === void 0 || _this$_connections$ge.onData(payload.data);
    });
    this._socksProxy.addListener(_socksProxy.SocksProxy.Events.SocksClosed, payload => {
      var _this$_connections$ge2;
      (_this$_connections$ge2 = this._connections.get(payload.uid)) === null || _this$_connections$ge2 === void 0 || _this$_connections$ge2.onClose();
      this._connections.delete(payload.uid);
    });
    loadDummyServerCertsIfNeeded();
  }
  _initSecureContexts(clientCertificates) {
    // Step 1. Group certificates by origin.
    const origin2certs = new Map();
    for (const cert of clientCertificates || []) {
      const origin = normalizeOrigin(cert.origin);
      const certs = origin2certs.get(origin) || [];
      certs.push(cert);
      origin2certs.set(origin, certs);
    }

    // Step 2. Create secure contexts for each origin.
    for (const [origin, certs] of origin2certs) {
      try {
        this.secureContextMap.set(origin, _tls.default.createSecureContext(convertClientCertificatesToTLSOptions(certs)));
      } catch (error) {
        error = rewriteOpenSSLErrorIfNeeded(error);
        throw (0, _utils.rewriteErrorMessage)(error, `Failed to load client certificate: ${error.message}`);
      }
    }
  }
  async listen() {
    const port = await this._socksProxy.listen(0, '127.0.0.1');
    return `socks5://127.0.0.1:${port}`;
  }
  async close() {
    await this._socksProxy.close();
  }
}
exports.ClientCertificatesProxy = ClientCertificatesProxy;
function normalizeOrigin(origin) {
  try {
    return new URL(origin).origin;
  } catch (error) {
    return origin;
  }
}
function convertClientCertificatesToTLSOptions(clientCertificates) {
  if (!clientCertificates || !clientCertificates.length) return;
  const tlsOptions = {
    pfx: [],
    key: [],
    cert: []
  };
  for (const cert of clientCertificates) {
    if (cert.cert) tlsOptions.cert.push(cert.cert);
    if (cert.key) tlsOptions.key.push({
      pem: cert.key,
      passphrase: cert.passphrase
    });
    if (cert.pfx) tlsOptions.pfx.push({
      buf: cert.pfx,
      passphrase: cert.passphrase
    });
  }
  return tlsOptions;
}
function getMatchingTLSOptionsForOrigin(clientCertificates, origin) {
  const matchingCerts = clientCertificates === null || clientCertificates === void 0 ? void 0 : clientCertificates.filter(c => normalizeOrigin(c.origin) === origin);
  return convertClientCertificatesToTLSOptions(matchingCerts);
}
function rewriteToLocalhostIfNeeded(host) {
  return host === 'local.playwright' ? 'localhost' : host;
}
function rewriteOpenSSLErrorIfNeeded(error) {
  if (error.message !== 'unsupported') return error;
  return (0, _utils.rewriteErrorMessage)(error, ['Unsupported TLS certificate.', 'Most likely, the security algorithm of the given certificate was deprecated by OpenSSL.', 'For more details, see https://github.com/openssl/openssl/blob/master/README-PROVIDERS.md#the-legacy-provider', 'You could probably modernize the certificate by following the steps at https://github.com/nodejs/node/issues/40672#issuecomment-1243648223'].join('\n'));
}