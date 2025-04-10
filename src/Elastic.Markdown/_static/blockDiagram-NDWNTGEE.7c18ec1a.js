// modules are defined as an array
// [ module function, map of requires ]
//
// map of requires is short require name -> numeric require
//
// anything defined in a previous bundle is accessed via the
// orig method which is the require for previous bundles

(function (
  modules,
  entry,
  mainEntry,
  parcelRequireName,
  externals,
  distDir,
  publicUrl,
  devServer
) {
  /* eslint-disable no-undef */
  var globalObject =
    typeof globalThis !== 'undefined'
      ? globalThis
      : typeof self !== 'undefined'
      ? self
      : typeof window !== 'undefined'
      ? window
      : typeof global !== 'undefined'
      ? global
      : {};
  /* eslint-enable no-undef */

  // Save the require from previous bundle to this closure if any
  var previousRequire =
    typeof globalObject[parcelRequireName] === 'function' &&
    globalObject[parcelRequireName];

  var importMap = previousRequire.i || {};
  var cache = previousRequire.cache || {};
  // Do not use `require` to prevent Webpack from trying to bundle this call
  var nodeRequire =
    typeof module !== 'undefined' &&
    typeof module.require === 'function' &&
    module.require.bind(module);

  function newRequire(name, jumped) {
    if (!cache[name]) {
      if (!modules[name]) {
        if (externals[name]) {
          return externals[name];
        }
        // if we cannot find the module within our internal map or
        // cache jump to the current global require ie. the last bundle
        // that was added to the page.
        var currentRequire =
          typeof globalObject[parcelRequireName] === 'function' &&
          globalObject[parcelRequireName];
        if (!jumped && currentRequire) {
          return currentRequire(name, true);
        }

        // If there are other bundles on this page the require from the
        // previous one is saved to 'previousRequire'. Repeat this as
        // many times as there are bundles until the module is found or
        // we exhaust the require chain.
        if (previousRequire) {
          return previousRequire(name, true);
        }

        // Try the node require function if it exists.
        if (nodeRequire && typeof name === 'string') {
          return nodeRequire(name);
        }

        var err = new Error("Cannot find module '" + name + "'");
        err.code = 'MODULE_NOT_FOUND';
        throw err;
      }

      localRequire.resolve = resolve;
      localRequire.cache = {};

      var module = (cache[name] = new newRequire.Module(name));

      modules[name][0].call(
        module.exports,
        localRequire,
        module,
        module.exports,
        globalObject
      );
    }

    return cache[name].exports;

    function localRequire(x) {
      var res = localRequire.resolve(x);
      return res === false ? {} : newRequire(res);
    }

    function resolve(x) {
      var id = modules[name][1][x];
      return id != null ? id : x;
    }
  }

  function Module(moduleName) {
    this.id = moduleName;
    this.bundle = newRequire;
    this.require = nodeRequire;
    this.exports = {};
  }

  newRequire.isParcelRequire = true;
  newRequire.Module = Module;
  newRequire.modules = modules;
  newRequire.cache = cache;
  newRequire.parent = previousRequire;
  newRequire.distDir = distDir;
  newRequire.publicUrl = publicUrl;
  newRequire.devServer = devServer;
  newRequire.i = importMap;
  newRequire.register = function (id, exports) {
    modules[id] = [
      function (require, module) {
        module.exports = exports;
      },
      {},
    ];
  };

  // Only insert newRequire.load when it is actually used.
  // The code in this file is linted against ES5, so dynamic import is not allowed.
  // INSERT_LOAD_HERE

  Object.defineProperty(newRequire, 'root', {
    get: function () {
      return globalObject[parcelRequireName];
    },
  });

  globalObject[parcelRequireName] = newRequire;

  for (var i = 0; i < entry.length; i++) {
    newRequire(entry[i]);
  }

  if (mainEntry) {
    // Expose entry point to Node, AMD or browser globals
    // Based on https://github.com/ForbesLindesay/umd/blob/master/template.js
    var mainExports = newRequire(mainEntry);

    // CommonJS
    if (typeof exports === 'object' && typeof module !== 'undefined') {
      module.exports = mainExports;

      // RequireJS
    } else if (typeof define === 'function' && define.amd) {
      define(function () {
        return mainExports;
      });
    }
  }
})({"56RUd":[function(require,module,exports,__globalThis) {
var global = arguments[3];
var HMR_HOST = null;
var HMR_PORT = 1234;
var HMR_SERVER_PORT = 1234;
var HMR_SECURE = false;
var HMR_ENV_HASH = "febfef3c5b467b16";
var HMR_USE_SSE = false;
module.bundle.HMR_BUNDLE_ID = "7a08f1e57c18ec1a";
"use strict";
/* global HMR_HOST, HMR_PORT, HMR_SERVER_PORT, HMR_ENV_HASH, HMR_SECURE, HMR_USE_SSE, chrome, browser, __parcel__import__, __parcel__importScripts__, ServiceWorkerGlobalScope */ /*::
import type {
  HMRAsset,
  HMRMessage,
} from '@parcel/reporter-dev-server/src/HMRServer.js';
interface ParcelRequire {
  (string): mixed;
  cache: {|[string]: ParcelModule|};
  hotData: {|[string]: mixed|};
  Module: any;
  parent: ?ParcelRequire;
  isParcelRequire: true;
  modules: {|[string]: [Function, {|[string]: string|}]|};
  HMR_BUNDLE_ID: string;
  root: ParcelRequire;
}
interface ParcelModule {
  hot: {|
    data: mixed,
    accept(cb: (Function) => void): void,
    dispose(cb: (mixed) => void): void,
    // accept(deps: Array<string> | string, cb: (Function) => void): void,
    // decline(): void,
    _acceptCallbacks: Array<(Function) => void>,
    _disposeCallbacks: Array<(mixed) => void>,
  |};
}
interface ExtensionContext {
  runtime: {|
    reload(): void,
    getURL(url: string): string;
    getManifest(): {manifest_version: number, ...};
  |};
}
declare var module: {bundle: ParcelRequire, ...};
declare var HMR_HOST: string;
declare var HMR_PORT: string;
declare var HMR_SERVER_PORT: string;
declare var HMR_ENV_HASH: string;
declare var HMR_SECURE: boolean;
declare var HMR_USE_SSE: boolean;
declare var chrome: ExtensionContext;
declare var browser: ExtensionContext;
declare var __parcel__import__: (string) => Promise<void>;
declare var __parcel__importScripts__: (string) => Promise<void>;
declare var globalThis: typeof self;
declare var ServiceWorkerGlobalScope: Object;
*/ var OVERLAY_ID = '__parcel__error__overlay__';
var OldModule = module.bundle.Module;
function Module(moduleName) {
    OldModule.call(this, moduleName);
    this.hot = {
        data: module.bundle.hotData[moduleName],
        _acceptCallbacks: [],
        _disposeCallbacks: [],
        accept: function(fn) {
            this._acceptCallbacks.push(fn || function() {});
        },
        dispose: function(fn) {
            this._disposeCallbacks.push(fn);
        }
    };
    module.bundle.hotData[moduleName] = undefined;
}
module.bundle.Module = Module;
module.bundle.hotData = {};
var checkedAssets /*: {|[string]: boolean|} */ , disposedAssets /*: {|[string]: boolean|} */ , assetsToDispose /*: Array<[ParcelRequire, string]> */ , assetsToAccept /*: Array<[ParcelRequire, string]> */ , bundleNotFound = false;
function getHostname() {
    return HMR_HOST || (typeof location !== 'undefined' && location.protocol.indexOf('http') === 0 ? location.hostname : 'localhost');
}
function getPort() {
    return HMR_PORT || (typeof location !== 'undefined' ? location.port : HMR_SERVER_PORT);
}
// eslint-disable-next-line no-redeclare
let WebSocket = globalThis.WebSocket;
if (!WebSocket && typeof module.bundle.root === 'function') try {
    // eslint-disable-next-line no-global-assign
    WebSocket = module.bundle.root('ws');
} catch  {
// ignore.
}
var hostname = getHostname();
var port = getPort();
var protocol = HMR_SECURE || typeof location !== 'undefined' && location.protocol === 'https:' && ![
    'localhost',
    '127.0.0.1',
    '0.0.0.0'
].includes(hostname) ? 'wss' : 'ws';
// eslint-disable-next-line no-redeclare
var parent = module.bundle.parent;
if (!parent || !parent.isParcelRequire) {
    // Web extension context
    var extCtx = typeof browser === 'undefined' ? typeof chrome === 'undefined' ? null : chrome : browser;
    // Safari doesn't support sourceURL in error stacks.
    // eval may also be disabled via CSP, so do a quick check.
    var supportsSourceURL = false;
    try {
        (0, eval)('throw new Error("test"); //# sourceURL=test.js');
    } catch (err) {
        supportsSourceURL = err.stack.includes('test.js');
    }
    var ws;
    if (HMR_USE_SSE) ws = new EventSource('/__parcel_hmr');
    else try {
        // If we're running in the dev server's node runner, listen for messages on the parent port.
        let { workerData, parentPort } = module.bundle.root('node:worker_threads') /*: any*/ ;
        if (workerData !== null && workerData !== void 0 && workerData.__parcel) {
            parentPort.on('message', async (message)=>{
                try {
                    await handleMessage(message);
                    parentPort.postMessage('updated');
                } catch  {
                    parentPort.postMessage('restart');
                }
            });
            // After the bundle has finished running, notify the dev server that the HMR update is complete.
            queueMicrotask(()=>parentPort.postMessage('ready'));
        }
    } catch  {
        if (typeof WebSocket !== 'undefined') try {
            ws = new WebSocket(protocol + '://' + hostname + (port ? ':' + port : '') + '/');
        } catch (err) {
            // Ignore cloudflare workers error.
            if (err.message && !err.message.includes('Disallowed operation called within global scope')) console.error(err.message);
        }
    }
    if (ws) {
        // $FlowFixMe
        ws.onmessage = async function(event /*: {data: string, ...} */ ) {
            var data /*: HMRMessage */  = JSON.parse(event.data);
            await handleMessage(data);
        };
        if (ws instanceof WebSocket) {
            ws.onerror = function(e) {
                if (e.message) console.error(e.message);
            };
            ws.onclose = function() {
                console.warn("[parcel] \uD83D\uDEA8 Connection to the HMR server was lost");
            };
        }
    }
}
async function handleMessage(data /*: HMRMessage */ ) {
    checkedAssets = {} /*: {|[string]: boolean|} */ ;
    disposedAssets = {} /*: {|[string]: boolean|} */ ;
    assetsToAccept = [];
    assetsToDispose = [];
    bundleNotFound = false;
    if (data.type === 'reload') fullReload();
    else if (data.type === 'update') {
        // Remove error overlay if there is one
        if (typeof document !== 'undefined') removeErrorOverlay();
        let assets = data.assets;
        // Handle HMR Update
        let handled = assets.every((asset)=>{
            return asset.type === 'css' || asset.type === 'js' && hmrAcceptCheck(module.bundle.root, asset.id, asset.depsByBundle);
        });
        // Dispatch a custom event in case a bundle was not found. This might mean
        // an asset on the server changed and we should reload the page. This event
        // gives the client an opportunity to refresh without losing state
        // (e.g. via React Server Components). If e.preventDefault() is not called,
        // we will trigger a full page reload.
        if (handled && bundleNotFound && assets.some((a)=>a.envHash !== HMR_ENV_HASH) && typeof window !== 'undefined' && typeof CustomEvent !== 'undefined') handled = !window.dispatchEvent(new CustomEvent('parcelhmrreload', {
            cancelable: true
        }));
        if (handled) {
            console.clear();
            // Dispatch custom event so other runtimes (e.g React Refresh) are aware.
            if (typeof window !== 'undefined' && typeof CustomEvent !== 'undefined') window.dispatchEvent(new CustomEvent('parcelhmraccept'));
            await hmrApplyUpdates(assets);
            hmrDisposeQueue();
            // Run accept callbacks. This will also re-execute other disposed assets in topological order.
            let processedAssets = {};
            for(let i = 0; i < assetsToAccept.length; i++){
                let id = assetsToAccept[i][1];
                if (!processedAssets[id]) {
                    hmrAccept(assetsToAccept[i][0], id);
                    processedAssets[id] = true;
                }
            }
        } else fullReload();
    }
    if (data.type === 'error') {
        // Log parcel errors to console
        for (let ansiDiagnostic of data.diagnostics.ansi){
            let stack = ansiDiagnostic.codeframe ? ansiDiagnostic.codeframe : ansiDiagnostic.stack;
            console.error("\uD83D\uDEA8 [parcel]: " + ansiDiagnostic.message + '\n' + stack + '\n\n' + ansiDiagnostic.hints.join('\n'));
        }
        if (typeof document !== 'undefined') {
            // Render the fancy html overlay
            removeErrorOverlay();
            var overlay = createErrorOverlay(data.diagnostics.html);
            // $FlowFixMe
            document.body.appendChild(overlay);
        }
    }
}
function removeErrorOverlay() {
    var overlay = document.getElementById(OVERLAY_ID);
    if (overlay) {
        overlay.remove();
        console.log("[parcel] \u2728 Error resolved");
    }
}
function createErrorOverlay(diagnostics) {
    var overlay = document.createElement('div');
    overlay.id = OVERLAY_ID;
    let errorHTML = '<div style="background: black; opacity: 0.85; font-size: 16px; color: white; position: fixed; height: 100%; width: 100%; top: 0px; left: 0px; padding: 30px; font-family: Menlo, Consolas, monospace; z-index: 9999;">';
    for (let diagnostic of diagnostics){
        let stack = diagnostic.frames.length ? diagnostic.frames.reduce((p, frame)=>{
            return `${p}
<a href="${protocol === 'wss' ? 'https' : 'http'}://${hostname}:${port}/__parcel_launch_editor?file=${encodeURIComponent(frame.location)}" style="text-decoration: underline; color: #888" onclick="fetch(this.href); return false">${frame.location}</a>
${frame.code}`;
        }, '') : diagnostic.stack;
        errorHTML += `
      <div>
        <div style="font-size: 18px; font-weight: bold; margin-top: 20px;">
          \u{1F6A8} ${diagnostic.message}
        </div>
        <pre>${stack}</pre>
        <div>
          ${diagnostic.hints.map((hint)=>"<div>\uD83D\uDCA1 " + hint + '</div>').join('')}
        </div>
        ${diagnostic.documentation ? `<div>\u{1F4DD} <a style="color: violet" href="${diagnostic.documentation}" target="_blank">Learn more</a></div>` : ''}
      </div>
    `;
    }
    errorHTML += '</div>';
    overlay.innerHTML = errorHTML;
    return overlay;
}
function fullReload() {
    if (typeof location !== 'undefined' && 'reload' in location) location.reload();
    else if (typeof extCtx !== 'undefined' && extCtx && extCtx.runtime && extCtx.runtime.reload) extCtx.runtime.reload();
    else try {
        let { workerData, parentPort } = module.bundle.root('node:worker_threads') /*: any*/ ;
        if (workerData !== null && workerData !== void 0 && workerData.__parcel) parentPort.postMessage('restart');
    } catch (err) {
        console.error("[parcel] \u26A0\uFE0F An HMR update was not accepted. Please restart the process.");
    }
}
function getParents(bundle, id) /*: Array<[ParcelRequire, string]> */ {
    var modules = bundle.modules;
    if (!modules) return [];
    var parents = [];
    var k, d, dep;
    for(k in modules)for(d in modules[k][1]){
        dep = modules[k][1][d];
        if (dep === id || Array.isArray(dep) && dep[dep.length - 1] === id) parents.push([
            bundle,
            k
        ]);
    }
    if (bundle.parent) parents = parents.concat(getParents(bundle.parent, id));
    return parents;
}
function updateLink(link) {
    var href = link.getAttribute('href');
    if (!href) return;
    var newLink = link.cloneNode();
    newLink.onload = function() {
        if (link.parentNode !== null) // $FlowFixMe
        link.parentNode.removeChild(link);
    };
    newLink.setAttribute('href', // $FlowFixMe
    href.split('?')[0] + '?' + Date.now());
    // $FlowFixMe
    link.parentNode.insertBefore(newLink, link.nextSibling);
}
var cssTimeout = null;
function reloadCSS() {
    if (cssTimeout || typeof document === 'undefined') return;
    cssTimeout = setTimeout(function() {
        var links = document.querySelectorAll('link[rel="stylesheet"]');
        for(var i = 0; i < links.length; i++){
            // $FlowFixMe[incompatible-type]
            var href /*: string */  = links[i].getAttribute('href');
            var hostname = getHostname();
            var servedFromHMRServer = hostname === 'localhost' ? new RegExp('^(https?:\\/\\/(0.0.0.0|127.0.0.1)|localhost):' + getPort()).test(href) : href.indexOf(hostname + ':' + getPort());
            var absolute = /^https?:\/\//i.test(href) && href.indexOf(location.origin) !== 0 && !servedFromHMRServer;
            if (!absolute) updateLink(links[i]);
        }
        cssTimeout = null;
    }, 50);
}
function hmrDownload(asset) {
    if (asset.type === 'js') {
        if (typeof document !== 'undefined') {
            let script = document.createElement('script');
            script.src = asset.url + '?t=' + Date.now();
            if (asset.outputFormat === 'esmodule') script.type = 'module';
            return new Promise((resolve, reject)=>{
                var _document$head;
                script.onload = ()=>resolve(script);
                script.onerror = reject;
                (_document$head = document.head) === null || _document$head === void 0 || _document$head.appendChild(script);
            });
        } else if (typeof importScripts === 'function') {
            // Worker scripts
            if (asset.outputFormat === 'esmodule') return import(asset.url + '?t=' + Date.now());
            else return new Promise((resolve, reject)=>{
                try {
                    importScripts(asset.url + '?t=' + Date.now());
                    resolve();
                } catch (err) {
                    reject(err);
                }
            });
        }
    }
}
async function hmrApplyUpdates(assets) {
    global.parcelHotUpdate = Object.create(null);
    let scriptsToRemove;
    try {
        // If sourceURL comments aren't supported in eval, we need to load
        // the update from the dev server over HTTP so that stack traces
        // are correct in errors/logs. This is much slower than eval, so
        // we only do it if needed (currently just Safari).
        // https://bugs.webkit.org/show_bug.cgi?id=137297
        // This path is also taken if a CSP disallows eval.
        if (!supportsSourceURL) {
            let promises = assets.map((asset)=>{
                var _hmrDownload;
                return (_hmrDownload = hmrDownload(asset)) === null || _hmrDownload === void 0 ? void 0 : _hmrDownload.catch((err)=>{
                    // Web extension fix
                    if (extCtx && extCtx.runtime && extCtx.runtime.getManifest().manifest_version == 3 && typeof ServiceWorkerGlobalScope != 'undefined' && global instanceof ServiceWorkerGlobalScope) {
                        extCtx.runtime.reload();
                        return;
                    }
                    throw err;
                });
            });
            scriptsToRemove = await Promise.all(promises);
        }
        assets.forEach(function(asset) {
            hmrApply(module.bundle.root, asset);
        });
    } finally{
        delete global.parcelHotUpdate;
        if (scriptsToRemove) scriptsToRemove.forEach((script)=>{
            if (script) {
                var _document$head2;
                (_document$head2 = document.head) === null || _document$head2 === void 0 || _document$head2.removeChild(script);
            }
        });
    }
}
function hmrApply(bundle /*: ParcelRequire */ , asset /*:  HMRAsset */ ) {
    var modules = bundle.modules;
    if (!modules) return;
    if (asset.type === 'css') reloadCSS();
    else if (asset.type === 'js') {
        let deps = asset.depsByBundle[bundle.HMR_BUNDLE_ID];
        if (deps) {
            if (modules[asset.id]) {
                // Remove dependencies that are removed and will become orphaned.
                // This is necessary so that if the asset is added back again, the cache is gone, and we prevent a full page reload.
                let oldDeps = modules[asset.id][1];
                for(let dep in oldDeps)if (!deps[dep] || deps[dep] !== oldDeps[dep]) {
                    let id = oldDeps[dep];
                    let parents = getParents(module.bundle.root, id);
                    if (parents.length === 1) hmrDelete(module.bundle.root, id);
                }
            }
            if (supportsSourceURL) // Global eval. We would use `new Function` here but browser
            // support for source maps is better with eval.
            (0, eval)(asset.output);
            // $FlowFixMe
            let fn = global.parcelHotUpdate[asset.id];
            modules[asset.id] = [
                fn,
                deps
            ];
        }
        // Always traverse to the parent bundle, even if we already replaced the asset in this bundle.
        // This is required in case modules are duplicated. We need to ensure all instances have the updated code.
        if (bundle.parent) hmrApply(bundle.parent, asset);
    }
}
function hmrDelete(bundle, id) {
    let modules = bundle.modules;
    if (!modules) return;
    if (modules[id]) {
        // Collect dependencies that will become orphaned when this module is deleted.
        let deps = modules[id][1];
        let orphans = [];
        for(let dep in deps){
            let parents = getParents(module.bundle.root, deps[dep]);
            if (parents.length === 1) orphans.push(deps[dep]);
        }
        // Delete the module. This must be done before deleting dependencies in case of circular dependencies.
        delete modules[id];
        delete bundle.cache[id];
        // Now delete the orphans.
        orphans.forEach((id)=>{
            hmrDelete(module.bundle.root, id);
        });
    } else if (bundle.parent) hmrDelete(bundle.parent, id);
}
function hmrAcceptCheck(bundle /*: ParcelRequire */ , id /*: string */ , depsByBundle /*: ?{ [string]: { [string]: string } }*/ ) {
    checkedAssets = {};
    if (hmrAcceptCheckOne(bundle, id, depsByBundle)) return true;
    // Traverse parents breadth first. All possible ancestries must accept the HMR update, or we'll reload.
    let parents = getParents(module.bundle.root, id);
    let accepted = false;
    while(parents.length > 0){
        let v = parents.shift();
        let a = hmrAcceptCheckOne(v[0], v[1], null);
        if (a) // If this parent accepts, stop traversing upward, but still consider siblings.
        accepted = true;
        else if (a !== null) {
            // Otherwise, queue the parents in the next level upward.
            let p = getParents(module.bundle.root, v[1]);
            if (p.length === 0) {
                // If there are no parents, then we've reached an entry without accepting. Reload.
                accepted = false;
                break;
            }
            parents.push(...p);
        }
    }
    return accepted;
}
function hmrAcceptCheckOne(bundle /*: ParcelRequire */ , id /*: string */ , depsByBundle /*: ?{ [string]: { [string]: string } }*/ ) {
    var modules = bundle.modules;
    if (!modules) return;
    if (depsByBundle && !depsByBundle[bundle.HMR_BUNDLE_ID]) {
        // If we reached the root bundle without finding where the asset should go,
        // there's nothing to do. Mark as "accepted" so we don't reload the page.
        if (!bundle.parent) {
            bundleNotFound = true;
            return true;
        }
        return hmrAcceptCheckOne(bundle.parent, id, depsByBundle);
    }
    if (checkedAssets[id]) return null;
    checkedAssets[id] = true;
    var cached = bundle.cache[id];
    if (!cached) return true;
    assetsToDispose.push([
        bundle,
        id
    ]);
    if (cached && cached.hot && cached.hot._acceptCallbacks.length) {
        assetsToAccept.push([
            bundle,
            id
        ]);
        return true;
    }
    return false;
}
function hmrDisposeQueue() {
    // Dispose all old assets.
    for(let i = 0; i < assetsToDispose.length; i++){
        let id = assetsToDispose[i][1];
        if (!disposedAssets[id]) {
            hmrDispose(assetsToDispose[i][0], id);
            disposedAssets[id] = true;
        }
    }
    assetsToDispose = [];
}
function hmrDispose(bundle /*: ParcelRequire */ , id /*: string */ ) {
    var cached = bundle.cache[id];
    bundle.hotData[id] = {};
    if (cached && cached.hot) cached.hot.data = bundle.hotData[id];
    if (cached && cached.hot && cached.hot._disposeCallbacks.length) cached.hot._disposeCallbacks.forEach(function(cb) {
        cb(bundle.hotData[id]);
    });
    delete bundle.cache[id];
}
function hmrAccept(bundle /*: ParcelRequire */ , id /*: string */ ) {
    // Execute the module.
    bundle(id);
    // Run the accept callbacks in the new version of the module.
    var cached = bundle.cache[id];
    if (cached && cached.hot && cached.hot._acceptCallbacks.length) {
        let assetsToAlsoAccept = [];
        cached.hot._acceptCallbacks.forEach(function(cb) {
            let additionalAssets = cb(function() {
                return getParents(module.bundle.root, id);
            });
            if (Array.isArray(additionalAssets) && additionalAssets.length) assetsToAlsoAccept.push(...additionalAssets);
        });
        if (assetsToAlsoAccept.length) {
            let handled = assetsToAlsoAccept.every(function(a) {
                return hmrAcceptCheck(a[0], a[1]);
            });
            if (!handled) return fullReload();
            hmrDisposeQueue();
        }
    }
}

},{}],"1FSgd":[function(require,module,exports,__globalThis) {
var parcelHelpers = require("@parcel/transformer-js/src/esmodule-helpers.js");
parcelHelpers.defineInteropFlag(exports);
parcelHelpers.export(exports, "diagram", ()=>rr);
var _chunk2RSIMOBZMjs = require("./chunk-2RSIMOBZ.mjs");
var _chunkUWHJNN4QMjs = require("./chunk-UWHJNN4Q.mjs");
var _chunkU6LOUQAFMjs = require("./chunk-U6LOUQAF.mjs");
var _chunkKMOJB3TBMjs = require("./chunk-KMOJB3TB.mjs");
var _chunk6XGRHI2AMjs = require("./chunk-6XGRHI2A.mjs");
var _chunkAC3VT7B7Mjs = require("./chunk-AC3VT7B7.mjs");
var _chunkTI4EEUUGMjs = require("./chunk-TI4EEUUG.mjs");
var _chunkNQURTBEVMjs = require("./chunk-NQURTBEV.mjs");
var _chunkBKDDFIKNMjs = require("./chunk-BKDDFIKN.mjs");
var _chunkYPUTD6PBMjs = require("./chunk-YPUTD6PB.mjs");
var _chunk6BY5RJGCMjs = require("./chunk-6BY5RJGC.mjs");
var _chunkGTKDMUJJMjs = require("./chunk-GTKDMUJJ.mjs");
var ie = function() {
    var e = (0, _chunkGTKDMUJJMjs.a)(function(C, h, r, n) {
        for(r = r || {}, n = C.length; n--; r[C[n]] = h);
        return r;
    }, "o"), o = [
        1,
        7
    ], g = [
        1,
        13
    ], l = [
        1,
        14
    ], s = [
        1,
        15
    ], u = [
        1,
        19
    ], a = [
        1,
        16
    ], b = [
        1,
        17
    ], p = [
        1,
        18
    ], k = [
        8,
        30
    ], f = [
        8,
        21,
        28,
        29,
        30,
        31,
        32,
        40,
        44,
        47
    ], L = [
        1,
        23
    ], I = [
        1,
        24
    ], x = [
        8,
        15,
        16,
        21,
        28,
        29,
        30,
        31,
        32,
        40,
        44,
        47
    ], E = [
        8,
        15,
        16,
        21,
        27,
        28,
        29,
        30,
        31,
        32,
        40,
        44,
        47
    ], _ = [
        1,
        49
    ], B = {
        trace: (0, _chunkGTKDMUJJMjs.a)(function() {}, "trace"),
        yy: {},
        symbols_: {
            error: 2,
            spaceLines: 3,
            SPACELINE: 4,
            NL: 5,
            separator: 6,
            SPACE: 7,
            EOF: 8,
            start: 9,
            BLOCK_DIAGRAM_KEY: 10,
            document: 11,
            stop: 12,
            statement: 13,
            link: 14,
            LINK: 15,
            START_LINK: 16,
            LINK_LABEL: 17,
            STR: 18,
            nodeStatement: 19,
            columnsStatement: 20,
            SPACE_BLOCK: 21,
            blockStatement: 22,
            classDefStatement: 23,
            cssClassStatement: 24,
            styleStatement: 25,
            node: 26,
            SIZE: 27,
            COLUMNS: 28,
            "id-block": 29,
            end: 30,
            block: 31,
            NODE_ID: 32,
            nodeShapeNLabel: 33,
            dirList: 34,
            DIR: 35,
            NODE_DSTART: 36,
            NODE_DEND: 37,
            BLOCK_ARROW_START: 38,
            BLOCK_ARROW_END: 39,
            classDef: 40,
            CLASSDEF_ID: 41,
            CLASSDEF_STYLEOPTS: 42,
            DEFAULT: 43,
            class: 44,
            CLASSENTITY_IDS: 45,
            STYLECLASS: 46,
            style: 47,
            STYLE_ENTITY_IDS: 48,
            STYLE_DEFINITION_DATA: 49,
            $accept: 0,
            $end: 1
        },
        terminals_: {
            2: "error",
            4: "SPACELINE",
            5: "NL",
            7: "SPACE",
            8: "EOF",
            10: "BLOCK_DIAGRAM_KEY",
            15: "LINK",
            16: "START_LINK",
            17: "LINK_LABEL",
            18: "STR",
            21: "SPACE_BLOCK",
            27: "SIZE",
            28: "COLUMNS",
            29: "id-block",
            30: "end",
            31: "block",
            32: "NODE_ID",
            35: "DIR",
            36: "NODE_DSTART",
            37: "NODE_DEND",
            38: "BLOCK_ARROW_START",
            39: "BLOCK_ARROW_END",
            40: "classDef",
            41: "CLASSDEF_ID",
            42: "CLASSDEF_STYLEOPTS",
            43: "DEFAULT",
            44: "class",
            45: "CLASSENTITY_IDS",
            46: "STYLECLASS",
            47: "style",
            48: "STYLE_ENTITY_IDS",
            49: "STYLE_DEFINITION_DATA"
        },
        productions_: [
            0,
            [
                3,
                1
            ],
            [
                3,
                2
            ],
            [
                3,
                2
            ],
            [
                6,
                1
            ],
            [
                6,
                1
            ],
            [
                6,
                1
            ],
            [
                9,
                3
            ],
            [
                12,
                1
            ],
            [
                12,
                1
            ],
            [
                12,
                2
            ],
            [
                12,
                2
            ],
            [
                11,
                1
            ],
            [
                11,
                2
            ],
            [
                14,
                1
            ],
            [
                14,
                4
            ],
            [
                13,
                1
            ],
            [
                13,
                1
            ],
            [
                13,
                1
            ],
            [
                13,
                1
            ],
            [
                13,
                1
            ],
            [
                13,
                1
            ],
            [
                13,
                1
            ],
            [
                19,
                3
            ],
            [
                19,
                2
            ],
            [
                19,
                1
            ],
            [
                20,
                1
            ],
            [
                22,
                4
            ],
            [
                22,
                3
            ],
            [
                26,
                1
            ],
            [
                26,
                2
            ],
            [
                34,
                1
            ],
            [
                34,
                2
            ],
            [
                33,
                3
            ],
            [
                33,
                4
            ],
            [
                23,
                3
            ],
            [
                23,
                3
            ],
            [
                24,
                3
            ],
            [
                25,
                3
            ]
        ],
        performAction: (0, _chunkGTKDMUJJMjs.a)(function(h, r, n, d, m, t, K) {
            var i = t.length - 1;
            switch(m){
                case 4:
                    d.getLogger().debug("Rule: separator (NL) ");
                    break;
                case 5:
                    d.getLogger().debug("Rule: separator (Space) ");
                    break;
                case 6:
                    d.getLogger().debug("Rule: separator (EOF) ");
                    break;
                case 7:
                    d.getLogger().debug("Rule: hierarchy: ", t[i - 1]), d.setHierarchy(t[i - 1]);
                    break;
                case 8:
                    d.getLogger().debug("Stop NL ");
                    break;
                case 9:
                    d.getLogger().debug("Stop EOF ");
                    break;
                case 10:
                    d.getLogger().debug("Stop NL2 ");
                    break;
                case 11:
                    d.getLogger().debug("Stop EOF2 ");
                    break;
                case 12:
                    d.getLogger().debug("Rule: statement: ", t[i]), typeof t[i].length == "number" ? this.$ = t[i] : this.$ = [
                        t[i]
                    ];
                    break;
                case 13:
                    d.getLogger().debug("Rule: statement #2: ", t[i - 1]), this.$ = [
                        t[i - 1]
                    ].concat(t[i]);
                    break;
                case 14:
                    d.getLogger().debug("Rule: link: ", t[i], h), this.$ = {
                        edgeTypeStr: t[i],
                        label: ""
                    };
                    break;
                case 15:
                    d.getLogger().debug("Rule: LABEL link: ", t[i - 3], t[i - 1], t[i]), this.$ = {
                        edgeTypeStr: t[i],
                        label: t[i - 1]
                    };
                    break;
                case 18:
                    let v = parseInt(t[i]), W = d.generateId();
                    this.$ = {
                        id: W,
                        type: "space",
                        label: "",
                        width: v,
                        children: []
                    };
                    break;
                case 23:
                    d.getLogger().debug("Rule: (nodeStatement link node) ", t[i - 2], t[i - 1], t[i], " typestr: ", t[i - 1].edgeTypeStr);
                    let j = d.edgeStrToEdgeData(t[i - 1].edgeTypeStr);
                    this.$ = [
                        {
                            id: t[i - 2].id,
                            label: t[i - 2].label,
                            type: t[i - 2].type,
                            directions: t[i - 2].directions
                        },
                        {
                            id: t[i - 2].id + "-" + t[i].id,
                            start: t[i - 2].id,
                            end: t[i].id,
                            label: t[i - 1].label,
                            type: "edge",
                            directions: t[i].directions,
                            arrowTypeEnd: j,
                            arrowTypeStart: "arrow_open"
                        },
                        {
                            id: t[i].id,
                            label: t[i].label,
                            type: d.typeStr2Type(t[i].typeStr),
                            directions: t[i].directions
                        }
                    ];
                    break;
                case 24:
                    d.getLogger().debug("Rule: nodeStatement (abc88 node size) ", t[i - 1], t[i]), this.$ = {
                        id: t[i - 1].id,
                        label: t[i - 1].label,
                        type: d.typeStr2Type(t[i - 1].typeStr),
                        directions: t[i - 1].directions,
                        widthInColumns: parseInt(t[i], 10)
                    };
                    break;
                case 25:
                    d.getLogger().debug("Rule: nodeStatement (node) ", t[i]), this.$ = {
                        id: t[i].id,
                        label: t[i].label,
                        type: d.typeStr2Type(t[i].typeStr),
                        directions: t[i].directions,
                        widthInColumns: 1
                    };
                    break;
                case 26:
                    d.getLogger().debug("APA123", this ? this : "na"), d.getLogger().debug("COLUMNS: ", t[i]), this.$ = {
                        type: "column-setting",
                        columns: t[i] === "auto" ? -1 : parseInt(t[i])
                    };
                    break;
                case 27:
                    d.getLogger().debug("Rule: id-block statement : ", t[i - 2], t[i - 1]);
                    let ge = d.generateId();
                    this.$ = {
                        ...t[i - 2],
                        type: "composite",
                        children: t[i - 1]
                    };
                    break;
                case 28:
                    d.getLogger().debug("Rule: blockStatement : ", t[i - 2], t[i - 1], t[i]);
                    let G = d.generateId();
                    this.$ = {
                        id: G,
                        type: "composite",
                        label: "",
                        children: t[i - 1]
                    };
                    break;
                case 29:
                    d.getLogger().debug("Rule: node (NODE_ID separator): ", t[i]), this.$ = {
                        id: t[i]
                    };
                    break;
                case 30:
                    d.getLogger().debug("Rule: node (NODE_ID nodeShapeNLabel separator): ", t[i - 1], t[i]), this.$ = {
                        id: t[i - 1],
                        label: t[i].label,
                        typeStr: t[i].typeStr,
                        directions: t[i].directions
                    };
                    break;
                case 31:
                    d.getLogger().debug("Rule: dirList: ", t[i]), this.$ = [
                        t[i]
                    ];
                    break;
                case 32:
                    d.getLogger().debug("Rule: dirList: ", t[i - 1], t[i]), this.$ = [
                        t[i - 1]
                    ].concat(t[i]);
                    break;
                case 33:
                    d.getLogger().debug("Rule: nodeShapeNLabel: ", t[i - 2], t[i - 1], t[i]), this.$ = {
                        typeStr: t[i - 2] + t[i],
                        label: t[i - 1]
                    };
                    break;
                case 34:
                    d.getLogger().debug("Rule: BLOCK_ARROW nodeShapeNLabel: ", t[i - 3], t[i - 2], " #3:", t[i - 1], t[i]), this.$ = {
                        typeStr: t[i - 3] + t[i],
                        label: t[i - 2],
                        directions: t[i - 1]
                    };
                    break;
                case 35:
                case 36:
                    this.$ = {
                        type: "classDef",
                        id: t[i - 1].trim(),
                        css: t[i].trim()
                    };
                    break;
                case 37:
                    this.$ = {
                        type: "applyClass",
                        id: t[i - 1].trim(),
                        styleClass: t[i].trim()
                    };
                    break;
                case 38:
                    this.$ = {
                        type: "applyStyles",
                        id: t[i - 1].trim(),
                        stylesStr: t[i].trim()
                    };
                    break;
            }
        }, "anonymous"),
        table: [
            {
                9: 1,
                10: [
                    1,
                    2
                ]
            },
            {
                1: [
                    3
                ]
            },
            {
                11: 3,
                13: 4,
                19: 5,
                20: 6,
                21: o,
                22: 8,
                23: 9,
                24: 10,
                25: 11,
                26: 12,
                28: g,
                29: l,
                31: s,
                32: u,
                40: a,
                44: b,
                47: p
            },
            {
                8: [
                    1,
                    20
                ]
            },
            e(k, [
                2,
                12
            ], {
                13: 4,
                19: 5,
                20: 6,
                22: 8,
                23: 9,
                24: 10,
                25: 11,
                26: 12,
                11: 21,
                21: o,
                28: g,
                29: l,
                31: s,
                32: u,
                40: a,
                44: b,
                47: p
            }),
            e(f, [
                2,
                16
            ], {
                14: 22,
                15: L,
                16: I
            }),
            e(f, [
                2,
                17
            ]),
            e(f, [
                2,
                18
            ]),
            e(f, [
                2,
                19
            ]),
            e(f, [
                2,
                20
            ]),
            e(f, [
                2,
                21
            ]),
            e(f, [
                2,
                22
            ]),
            e(x, [
                2,
                25
            ], {
                27: [
                    1,
                    25
                ]
            }),
            e(f, [
                2,
                26
            ]),
            {
                19: 26,
                26: 12,
                32: u
            },
            {
                11: 27,
                13: 4,
                19: 5,
                20: 6,
                21: o,
                22: 8,
                23: 9,
                24: 10,
                25: 11,
                26: 12,
                28: g,
                29: l,
                31: s,
                32: u,
                40: a,
                44: b,
                47: p
            },
            {
                41: [
                    1,
                    28
                ],
                43: [
                    1,
                    29
                ]
            },
            {
                45: [
                    1,
                    30
                ]
            },
            {
                48: [
                    1,
                    31
                ]
            },
            e(E, [
                2,
                29
            ], {
                33: 32,
                36: [
                    1,
                    33
                ],
                38: [
                    1,
                    34
                ]
            }),
            {
                1: [
                    2,
                    7
                ]
            },
            e(k, [
                2,
                13
            ]),
            {
                26: 35,
                32: u
            },
            {
                32: [
                    2,
                    14
                ]
            },
            {
                17: [
                    1,
                    36
                ]
            },
            e(x, [
                2,
                24
            ]),
            {
                11: 37,
                13: 4,
                14: 22,
                15: L,
                16: I,
                19: 5,
                20: 6,
                21: o,
                22: 8,
                23: 9,
                24: 10,
                25: 11,
                26: 12,
                28: g,
                29: l,
                31: s,
                32: u,
                40: a,
                44: b,
                47: p
            },
            {
                30: [
                    1,
                    38
                ]
            },
            {
                42: [
                    1,
                    39
                ]
            },
            {
                42: [
                    1,
                    40
                ]
            },
            {
                46: [
                    1,
                    41
                ]
            },
            {
                49: [
                    1,
                    42
                ]
            },
            e(E, [
                2,
                30
            ]),
            {
                18: [
                    1,
                    43
                ]
            },
            {
                18: [
                    1,
                    44
                ]
            },
            e(x, [
                2,
                23
            ]),
            {
                18: [
                    1,
                    45
                ]
            },
            {
                30: [
                    1,
                    46
                ]
            },
            e(f, [
                2,
                28
            ]),
            e(f, [
                2,
                35
            ]),
            e(f, [
                2,
                36
            ]),
            e(f, [
                2,
                37
            ]),
            e(f, [
                2,
                38
            ]),
            {
                37: [
                    1,
                    47
                ]
            },
            {
                34: 48,
                35: _
            },
            {
                15: [
                    1,
                    50
                ]
            },
            e(f, [
                2,
                27
            ]),
            e(E, [
                2,
                33
            ]),
            {
                39: [
                    1,
                    51
                ]
            },
            {
                34: 52,
                35: _,
                39: [
                    2,
                    31
                ]
            },
            {
                32: [
                    2,
                    15
                ]
            },
            e(E, [
                2,
                34
            ]),
            {
                39: [
                    2,
                    32
                ]
            }
        ],
        defaultActions: {
            20: [
                2,
                7
            ],
            23: [
                2,
                14
            ],
            50: [
                2,
                15
            ],
            52: [
                2,
                32
            ]
        },
        parseError: (0, _chunkGTKDMUJJMjs.a)(function(h, r) {
            if (r.recoverable) this.trace(h);
            else {
                var n = new Error(h);
                throw n.hash = r, n;
            }
        }, "parseError"),
        parse: (0, _chunkGTKDMUJJMjs.a)(function(h) {
            var r = this, n = [
                0
            ], d = [], m = [
                null
            ], t = [], K = this.table, i = "", v = 0, W = 0, j = 0, ge = 2, G = 1, Ye = t.slice.call(arguments, 1), D = Object.create(this.lexer), A = {
                yy: {}
            };
            for(var Q in this.yy)Object.prototype.hasOwnProperty.call(this.yy, Q) && (A.yy[Q] = this.yy[Q]);
            D.setInput(h, A.yy), A.yy.lexer = D, A.yy.parser = this, typeof D.yylloc > "u" && (D.yylloc = {});
            var $ = D.yylloc;
            t.push($);
            var Ve = D.options && D.options.ranges;
            typeof A.yy.parseError == "function" ? this.parseError = A.yy.parseError : this.parseError = Object.getPrototypeOf(this).parseError;
            function St(N) {
                n.length = n.length - 2 * N, m.length = m.length - N, t.length = t.length - N;
            }
            (0, _chunkGTKDMUJJMjs.a)(St, "popStack");
            function We() {
                var N;
                return N = d.pop() || D.lex() || G, typeof N != "number" && (N instanceof Array && (d = N, N = d.pop()), N = r.symbols_[N] || N), N;
            }
            (0, _chunkGTKDMUJJMjs.a)(We, "lex");
            for(var w, ee, R, T, mt, te, P = {}, H, z, he, U;;){
                if (R = n[n.length - 1], this.defaultActions[R] ? T = this.defaultActions[R] : ((w === null || typeof w > "u") && (w = We()), T = K[R] && K[R][w]), typeof T > "u" || !T.length || !T[0]) {
                    var re = "";
                    U = [];
                    for(H in K[R])this.terminals_[H] && H > ge && U.push("'" + this.terminals_[H] + "'");
                    D.showPosition ? re = "Parse error on line " + (v + 1) + `:
` + D.showPosition() + `
Expecting ` + U.join(", ") + ", got '" + (this.terminals_[w] || w) + "'" : re = "Parse error on line " + (v + 1) + ": Unexpected " + (w == G ? "end of input" : "'" + (this.terminals_[w] || w) + "'"), this.parseError(re, {
                        text: D.match,
                        token: this.terminals_[w] || w,
                        line: D.yylineno,
                        loc: $,
                        expected: U
                    });
                }
                if (T[0] instanceof Array && T.length > 1) throw new Error("Parse Error: multiple actions possible at state: " + R + ", token: " + w);
                switch(T[0]){
                    case 1:
                        n.push(w), m.push(D.yytext), t.push(D.yylloc), n.push(T[1]), w = null, ee ? (w = ee, ee = null) : (W = D.yyleng, i = D.yytext, v = D.yylineno, $ = D.yylloc, j > 0 && j--);
                        break;
                    case 2:
                        if (z = this.productions_[T[1]][1], P.$ = m[m.length - z], P._$ = {
                            first_line: t[t.length - (z || 1)].first_line,
                            last_line: t[t.length - 1].last_line,
                            first_column: t[t.length - (z || 1)].first_column,
                            last_column: t[t.length - 1].last_column
                        }, Ve && (P._$.range = [
                            t[t.length - (z || 1)].range[0],
                            t[t.length - 1].range[1]
                        ]), te = this.performAction.apply(P, [
                            i,
                            W,
                            v,
                            A.yy,
                            T[1],
                            m,
                            t
                        ].concat(Ye)), typeof te < "u") return te;
                        z && (n = n.slice(0, -1 * z * 2), m = m.slice(0, -1 * z), t = t.slice(0, -1 * z)), n.push(this.productions_[T[1]][0]), m.push(P.$), t.push(P._$), he = K[n[n.length - 2]][n[n.length - 1]], n.push(he);
                        break;
                    case 3:
                        return !0;
                }
            }
            return !0;
        }, "parse")
    }, Z = function() {
        var C = {
            EOF: 1,
            parseError: (0, _chunkGTKDMUJJMjs.a)(function(r, n) {
                if (this.yy.parser) this.yy.parser.parseError(r, n);
                else throw new Error(r);
            }, "parseError"),
            setInput: (0, _chunkGTKDMUJJMjs.a)(function(h, r) {
                return this.yy = r || this.yy || {}, this._input = h, this._more = this._backtrack = this.done = !1, this.yylineno = this.yyleng = 0, this.yytext = this.matched = this.match = "", this.conditionStack = [
                    "INITIAL"
                ], this.yylloc = {
                    first_line: 1,
                    first_column: 0,
                    last_line: 1,
                    last_column: 0
                }, this.options.ranges && (this.yylloc.range = [
                    0,
                    0
                ]), this.offset = 0, this;
            }, "setInput"),
            input: (0, _chunkGTKDMUJJMjs.a)(function() {
                var h = this._input[0];
                this.yytext += h, this.yyleng++, this.offset++, this.match += h, this.matched += h;
                var r = h.match(/(?:\r\n?|\n).*/g);
                return r ? (this.yylineno++, this.yylloc.last_line++) : this.yylloc.last_column++, this.options.ranges && this.yylloc.range[1]++, this._input = this._input.slice(1), h;
            }, "input"),
            unput: (0, _chunkGTKDMUJJMjs.a)(function(h) {
                var r = h.length, n = h.split(/(?:\r\n?|\n)/g);
                this._input = h + this._input, this.yytext = this.yytext.substr(0, this.yytext.length - r), this.offset -= r;
                var d = this.match.split(/(?:\r\n?|\n)/g);
                this.match = this.match.substr(0, this.match.length - 1), this.matched = this.matched.substr(0, this.matched.length - 1), n.length - 1 && (this.yylineno -= n.length - 1);
                var m = this.yylloc.range;
                return this.yylloc = {
                    first_line: this.yylloc.first_line,
                    last_line: this.yylineno + 1,
                    first_column: this.yylloc.first_column,
                    last_column: n ? (n.length === d.length ? this.yylloc.first_column : 0) + d[d.length - n.length].length - n[0].length : this.yylloc.first_column - r
                }, this.options.ranges && (this.yylloc.range = [
                    m[0],
                    m[0] + this.yyleng - r
                ]), this.yyleng = this.yytext.length, this;
            }, "unput"),
            more: (0, _chunkGTKDMUJJMjs.a)(function() {
                return this._more = !0, this;
            }, "more"),
            reject: (0, _chunkGTKDMUJJMjs.a)(function() {
                if (this.options.backtrack_lexer) this._backtrack = !0;
                else return this.parseError("Lexical error on line " + (this.yylineno + 1) + `. You can only invoke reject() in the lexer when the lexer is of the backtracking persuasion (options.backtrack_lexer = true).
` + this.showPosition(), {
                    text: "",
                    token: null,
                    line: this.yylineno
                });
                return this;
            }, "reject"),
            less: (0, _chunkGTKDMUJJMjs.a)(function(h) {
                this.unput(this.match.slice(h));
            }, "less"),
            pastInput: (0, _chunkGTKDMUJJMjs.a)(function() {
                var h = this.matched.substr(0, this.matched.length - this.match.length);
                return (h.length > 20 ? "..." : "") + h.substr(-20).replace(/\n/g, "");
            }, "pastInput"),
            upcomingInput: (0, _chunkGTKDMUJJMjs.a)(function() {
                var h = this.match;
                return h.length < 20 && (h += this._input.substr(0, 20 - h.length)), (h.substr(0, 20) + (h.length > 20 ? "..." : "")).replace(/\n/g, "");
            }, "upcomingInput"),
            showPosition: (0, _chunkGTKDMUJJMjs.a)(function() {
                var h = this.pastInput(), r = new Array(h.length + 1).join("-");
                return h + this.upcomingInput() + `
` + r + "^";
            }, "showPosition"),
            test_match: (0, _chunkGTKDMUJJMjs.a)(function(h, r) {
                var n, d, m;
                if (this.options.backtrack_lexer && (m = {
                    yylineno: this.yylineno,
                    yylloc: {
                        first_line: this.yylloc.first_line,
                        last_line: this.last_line,
                        first_column: this.yylloc.first_column,
                        last_column: this.yylloc.last_column
                    },
                    yytext: this.yytext,
                    match: this.match,
                    matches: this.matches,
                    matched: this.matched,
                    yyleng: this.yyleng,
                    offset: this.offset,
                    _more: this._more,
                    _input: this._input,
                    yy: this.yy,
                    conditionStack: this.conditionStack.slice(0),
                    done: this.done
                }, this.options.ranges && (m.yylloc.range = this.yylloc.range.slice(0))), d = h[0].match(/(?:\r\n?|\n).*/g), d && (this.yylineno += d.length), this.yylloc = {
                    first_line: this.yylloc.last_line,
                    last_line: this.yylineno + 1,
                    first_column: this.yylloc.last_column,
                    last_column: d ? d[d.length - 1].length - d[d.length - 1].match(/\r?\n?/)[0].length : this.yylloc.last_column + h[0].length
                }, this.yytext += h[0], this.match += h[0], this.matches = h, this.yyleng = this.yytext.length, this.options.ranges && (this.yylloc.range = [
                    this.offset,
                    this.offset += this.yyleng
                ]), this._more = !1, this._backtrack = !1, this._input = this._input.slice(h[0].length), this.matched += h[0], n = this.performAction.call(this, this.yy, this, r, this.conditionStack[this.conditionStack.length - 1]), this.done && this._input && (this.done = !1), n) return n;
                if (this._backtrack) {
                    for(var t in m)this[t] = m[t];
                    return !1;
                }
                return !1;
            }, "test_match"),
            next: (0, _chunkGTKDMUJJMjs.a)(function() {
                if (this.done) return this.EOF;
                this._input || (this.done = !0);
                var h, r, n, d;
                this._more || (this.yytext = "", this.match = "");
                for(var m = this._currentRules(), t = 0; t < m.length; t++)if (n = this._input.match(this.rules[m[t]]), n && (!r || n[0].length > r[0].length)) {
                    if (r = n, d = t, this.options.backtrack_lexer) {
                        if (h = this.test_match(n, m[t]), h !== !1) return h;
                        if (this._backtrack) {
                            r = !1;
                            continue;
                        } else return !1;
                    } else if (!this.options.flex) break;
                }
                return r ? (h = this.test_match(r, m[d]), h !== !1 ? h : !1) : this._input === "" ? this.EOF : this.parseError("Lexical error on line " + (this.yylineno + 1) + `. Unrecognized text.
` + this.showPosition(), {
                    text: "",
                    token: null,
                    line: this.yylineno
                });
            }, "next"),
            lex: (0, _chunkGTKDMUJJMjs.a)(function() {
                var r = this.next();
                return r || this.lex();
            }, "lex"),
            begin: (0, _chunkGTKDMUJJMjs.a)(function(r) {
                this.conditionStack.push(r);
            }, "begin"),
            popState: (0, _chunkGTKDMUJJMjs.a)(function() {
                var r = this.conditionStack.length - 1;
                return r > 0 ? this.conditionStack.pop() : this.conditionStack[0];
            }, "popState"),
            _currentRules: (0, _chunkGTKDMUJJMjs.a)(function() {
                return this.conditionStack.length && this.conditionStack[this.conditionStack.length - 1] ? this.conditions[this.conditionStack[this.conditionStack.length - 1]].rules : this.conditions.INITIAL.rules;
            }, "_currentRules"),
            topState: (0, _chunkGTKDMUJJMjs.a)(function(r) {
                return r = this.conditionStack.length - 1 - Math.abs(r || 0), r >= 0 ? this.conditionStack[r] : "INITIAL";
            }, "topState"),
            pushState: (0, _chunkGTKDMUJJMjs.a)(function(r) {
                this.begin(r);
            }, "pushState"),
            stateStackSize: (0, _chunkGTKDMUJJMjs.a)(function() {
                return this.conditionStack.length;
            }, "stateStackSize"),
            options: {},
            performAction: (0, _chunkGTKDMUJJMjs.a)(function(r, n, d, m) {
                var t = m;
                switch(d){
                    case 0:
                        return 10;
                    case 1:
                        return r.getLogger().debug("Found space-block"), 31;
                    case 2:
                        return r.getLogger().debug("Found nl-block"), 31;
                    case 3:
                        return r.getLogger().debug("Found space-block"), 29;
                    case 4:
                        r.getLogger().debug(".", n.yytext);
                        break;
                    case 5:
                        r.getLogger().debug("_", n.yytext);
                        break;
                    case 6:
                        return 5;
                    case 7:
                        return n.yytext = -1, 28;
                    case 8:
                        return n.yytext = n.yytext.replace(/columns\s+/, ""), r.getLogger().debug("COLUMNS (LEX)", n.yytext), 28;
                    case 9:
                        this.pushState("md_string");
                        break;
                    case 10:
                        return "MD_STR";
                    case 11:
                        this.popState();
                        break;
                    case 12:
                        this.pushState("string");
                        break;
                    case 13:
                        r.getLogger().debug("LEX: POPPING STR:", n.yytext), this.popState();
                        break;
                    case 14:
                        return r.getLogger().debug("LEX: STR end:", n.yytext), "STR";
                    case 15:
                        return n.yytext = n.yytext.replace(/space\:/, ""), r.getLogger().debug("SPACE NUM (LEX)", n.yytext), 21;
                    case 16:
                        return n.yytext = "1", r.getLogger().debug("COLUMNS (LEX)", n.yytext), 21;
                    case 17:
                        return 43;
                    case 18:
                        return "LINKSTYLE";
                    case 19:
                        return "INTERPOLATE";
                    case 20:
                        return this.pushState("CLASSDEF"), 40;
                    case 21:
                        return this.popState(), this.pushState("CLASSDEFID"), "DEFAULT_CLASSDEF_ID";
                    case 22:
                        return this.popState(), this.pushState("CLASSDEFID"), 41;
                    case 23:
                        return this.popState(), 42;
                    case 24:
                        return this.pushState("CLASS"), 44;
                    case 25:
                        return this.popState(), this.pushState("CLASS_STYLE"), 45;
                    case 26:
                        return this.popState(), 46;
                    case 27:
                        return this.pushState("STYLE_STMNT"), 47;
                    case 28:
                        return this.popState(), this.pushState("STYLE_DEFINITION"), 48;
                    case 29:
                        return this.popState(), 49;
                    case 30:
                        return this.pushState("acc_title"), "acc_title";
                    case 31:
                        return this.popState(), "acc_title_value";
                    case 32:
                        return this.pushState("acc_descr"), "acc_descr";
                    case 33:
                        return this.popState(), "acc_descr_value";
                    case 34:
                        this.pushState("acc_descr_multiline");
                        break;
                    case 35:
                        this.popState();
                        break;
                    case 36:
                        return "acc_descr_multiline_value";
                    case 37:
                        return 30;
                    case 38:
                        return this.popState(), r.getLogger().debug("Lex: (("), "NODE_DEND";
                    case 39:
                        return this.popState(), r.getLogger().debug("Lex: (("), "NODE_DEND";
                    case 40:
                        return this.popState(), r.getLogger().debug("Lex: ))"), "NODE_DEND";
                    case 41:
                        return this.popState(), r.getLogger().debug("Lex: (("), "NODE_DEND";
                    case 42:
                        return this.popState(), r.getLogger().debug("Lex: (("), "NODE_DEND";
                    case 43:
                        return this.popState(), r.getLogger().debug("Lex: (-"), "NODE_DEND";
                    case 44:
                        return this.popState(), r.getLogger().debug("Lex: -)"), "NODE_DEND";
                    case 45:
                        return this.popState(), r.getLogger().debug("Lex: (("), "NODE_DEND";
                    case 46:
                        return this.popState(), r.getLogger().debug("Lex: ]]"), "NODE_DEND";
                    case 47:
                        return this.popState(), r.getLogger().debug("Lex: ("), "NODE_DEND";
                    case 48:
                        return this.popState(), r.getLogger().debug("Lex: ])"), "NODE_DEND";
                    case 49:
                        return this.popState(), r.getLogger().debug("Lex: /]"), "NODE_DEND";
                    case 50:
                        return this.popState(), r.getLogger().debug("Lex: /]"), "NODE_DEND";
                    case 51:
                        return this.popState(), r.getLogger().debug("Lex: )]"), "NODE_DEND";
                    case 52:
                        return this.popState(), r.getLogger().debug("Lex: )"), "NODE_DEND";
                    case 53:
                        return this.popState(), r.getLogger().debug("Lex: ]>"), "NODE_DEND";
                    case 54:
                        return this.popState(), r.getLogger().debug("Lex: ]"), "NODE_DEND";
                    case 55:
                        return r.getLogger().debug("Lexa: -)"), this.pushState("NODE"), 36;
                    case 56:
                        return r.getLogger().debug("Lexa: (-"), this.pushState("NODE"), 36;
                    case 57:
                        return r.getLogger().debug("Lexa: ))"), this.pushState("NODE"), 36;
                    case 58:
                        return r.getLogger().debug("Lexa: )"), this.pushState("NODE"), 36;
                    case 59:
                        return r.getLogger().debug("Lex: ((("), this.pushState("NODE"), 36;
                    case 60:
                        return r.getLogger().debug("Lexa: )"), this.pushState("NODE"), 36;
                    case 61:
                        return r.getLogger().debug("Lexa: )"), this.pushState("NODE"), 36;
                    case 62:
                        return r.getLogger().debug("Lexa: )"), this.pushState("NODE"), 36;
                    case 63:
                        return r.getLogger().debug("Lexc: >"), this.pushState("NODE"), 36;
                    case 64:
                        return r.getLogger().debug("Lexa: (["), this.pushState("NODE"), 36;
                    case 65:
                        return r.getLogger().debug("Lexa: )"), this.pushState("NODE"), 36;
                    case 66:
                        return this.pushState("NODE"), 36;
                    case 67:
                        return this.pushState("NODE"), 36;
                    case 68:
                        return this.pushState("NODE"), 36;
                    case 69:
                        return this.pushState("NODE"), 36;
                    case 70:
                        return this.pushState("NODE"), 36;
                    case 71:
                        return this.pushState("NODE"), 36;
                    case 72:
                        return this.pushState("NODE"), 36;
                    case 73:
                        return r.getLogger().debug("Lexa: ["), this.pushState("NODE"), 36;
                    case 74:
                        return this.pushState("BLOCK_ARROW"), r.getLogger().debug("LEX ARR START"), 38;
                    case 75:
                        return r.getLogger().debug("Lex: NODE_ID", n.yytext), 32;
                    case 76:
                        return r.getLogger().debug("Lex: EOF", n.yytext), 8;
                    case 77:
                        this.pushState("md_string");
                        break;
                    case 78:
                        this.pushState("md_string");
                        break;
                    case 79:
                        return "NODE_DESCR";
                    case 80:
                        this.popState();
                        break;
                    case 81:
                        r.getLogger().debug("Lex: Starting string"), this.pushState("string");
                        break;
                    case 82:
                        r.getLogger().debug("LEX ARR: Starting string"), this.pushState("string");
                        break;
                    case 83:
                        return r.getLogger().debug("LEX: NODE_DESCR:", n.yytext), "NODE_DESCR";
                    case 84:
                        r.getLogger().debug("LEX POPPING"), this.popState();
                        break;
                    case 85:
                        r.getLogger().debug("Lex: =>BAE"), this.pushState("ARROW_DIR");
                        break;
                    case 86:
                        return n.yytext = n.yytext.replace(/^,\s*/, ""), r.getLogger().debug("Lex (right): dir:", n.yytext), "DIR";
                    case 87:
                        return n.yytext = n.yytext.replace(/^,\s*/, ""), r.getLogger().debug("Lex (left):", n.yytext), "DIR";
                    case 88:
                        return n.yytext = n.yytext.replace(/^,\s*/, ""), r.getLogger().debug("Lex (x):", n.yytext), "DIR";
                    case 89:
                        return n.yytext = n.yytext.replace(/^,\s*/, ""), r.getLogger().debug("Lex (y):", n.yytext), "DIR";
                    case 90:
                        return n.yytext = n.yytext.replace(/^,\s*/, ""), r.getLogger().debug("Lex (up):", n.yytext), "DIR";
                    case 91:
                        return n.yytext = n.yytext.replace(/^,\s*/, ""), r.getLogger().debug("Lex (down):", n.yytext), "DIR";
                    case 92:
                        return n.yytext = "]>", r.getLogger().debug("Lex (ARROW_DIR end):", n.yytext), this.popState(), this.popState(), "BLOCK_ARROW_END";
                    case 93:
                        return r.getLogger().debug("Lex: LINK", "#" + n.yytext + "#"), 15;
                    case 94:
                        return r.getLogger().debug("Lex: LINK", n.yytext), 15;
                    case 95:
                        return r.getLogger().debug("Lex: LINK", n.yytext), 15;
                    case 96:
                        return r.getLogger().debug("Lex: LINK", n.yytext), 15;
                    case 97:
                        return r.getLogger().debug("Lex: START_LINK", n.yytext), this.pushState("LLABEL"), 16;
                    case 98:
                        return r.getLogger().debug("Lex: START_LINK", n.yytext), this.pushState("LLABEL"), 16;
                    case 99:
                        return r.getLogger().debug("Lex: START_LINK", n.yytext), this.pushState("LLABEL"), 16;
                    case 100:
                        this.pushState("md_string");
                        break;
                    case 101:
                        return r.getLogger().debug("Lex: Starting string"), this.pushState("string"), "LINK_LABEL";
                    case 102:
                        return this.popState(), r.getLogger().debug("Lex: LINK", "#" + n.yytext + "#"), 15;
                    case 103:
                        return this.popState(), r.getLogger().debug("Lex: LINK", n.yytext), 15;
                    case 104:
                        return this.popState(), r.getLogger().debug("Lex: LINK", n.yytext), 15;
                    case 105:
                        return r.getLogger().debug("Lex: COLON", n.yytext), n.yytext = n.yytext.slice(1), 27;
                }
            }, "anonymous"),
            rules: [
                /^(?:block-beta\b)/,
                /^(?:block\s+)/,
                /^(?:block\n+)/,
                /^(?:block:)/,
                /^(?:[\s]+)/,
                /^(?:[\n]+)/,
                /^(?:((\u000D\u000A)|(\u000A)))/,
                /^(?:columns\s+auto\b)/,
                /^(?:columns\s+[\d]+)/,
                /^(?:["][`])/,
                /^(?:[^`"]+)/,
                /^(?:[`]["])/,
                /^(?:["])/,
                /^(?:["])/,
                /^(?:[^"]*)/,
                /^(?:space[:]\d+)/,
                /^(?:space\b)/,
                /^(?:default\b)/,
                /^(?:linkStyle\b)/,
                /^(?:interpolate\b)/,
                /^(?:classDef\s+)/,
                /^(?:DEFAULT\s+)/,
                /^(?:\w+\s+)/,
                /^(?:[^\n]*)/,
                /^(?:class\s+)/,
                /^(?:(\w+)+((,\s*\w+)*))/,
                /^(?:[^\n]*)/,
                /^(?:style\s+)/,
                /^(?:(\w+)+((,\s*\w+)*))/,
                /^(?:[^\n]*)/,
                /^(?:accTitle\s*:\s*)/,
                /^(?:(?!\n||)*[^\n]*)/,
                /^(?:accDescr\s*:\s*)/,
                /^(?:(?!\n||)*[^\n]*)/,
                /^(?:accDescr\s*\{\s*)/,
                /^(?:[\}])/,
                /^(?:[^\}]*)/,
                /^(?:end\b\s*)/,
                /^(?:\(\(\()/,
                /^(?:\)\)\))/,
                /^(?:[\)]\))/,
                /^(?:\}\})/,
                /^(?:\})/,
                /^(?:\(-)/,
                /^(?:-\))/,
                /^(?:\(\()/,
                /^(?:\]\])/,
                /^(?:\()/,
                /^(?:\]\))/,
                /^(?:\\\])/,
                /^(?:\/\])/,
                /^(?:\)\])/,
                /^(?:[\)])/,
                /^(?:\]>)/,
                /^(?:[\]])/,
                /^(?:-\))/,
                /^(?:\(-)/,
                /^(?:\)\))/,
                /^(?:\))/,
                /^(?:\(\(\()/,
                /^(?:\(\()/,
                /^(?:\{\{)/,
                /^(?:\{)/,
                /^(?:>)/,
                /^(?:\(\[)/,
                /^(?:\()/,
                /^(?:\[\[)/,
                /^(?:\[\|)/,
                /^(?:\[\()/,
                /^(?:\)\)\))/,
                /^(?:\[\\)/,
                /^(?:\[\/)/,
                /^(?:\[\\)/,
                /^(?:\[)/,
                /^(?:<\[)/,
                /^(?:[^\(\[\n\-\)\{\}\s\<\>:]+)/,
                /^(?:$)/,
                /^(?:["][`])/,
                /^(?:["][`])/,
                /^(?:[^`"]+)/,
                /^(?:[`]["])/,
                /^(?:["])/,
                /^(?:["])/,
                /^(?:[^"]+)/,
                /^(?:["])/,
                /^(?:\]>\s*\()/,
                /^(?:,?\s*right\s*)/,
                /^(?:,?\s*left\s*)/,
                /^(?:,?\s*x\s*)/,
                /^(?:,?\s*y\s*)/,
                /^(?:,?\s*up\s*)/,
                /^(?:,?\s*down\s*)/,
                /^(?:\)\s*)/,
                /^(?:\s*[xo<]?--+[-xo>]\s*)/,
                /^(?:\s*[xo<]?==+[=xo>]\s*)/,
                /^(?:\s*[xo<]?-?\.+-[xo>]?\s*)/,
                /^(?:\s*~~[\~]+\s*)/,
                /^(?:\s*[xo<]?--\s*)/,
                /^(?:\s*[xo<]?==\s*)/,
                /^(?:\s*[xo<]?-\.\s*)/,
                /^(?:["][`])/,
                /^(?:["])/,
                /^(?:\s*[xo<]?--+[-xo>]\s*)/,
                /^(?:\s*[xo<]?==+[=xo>]\s*)/,
                /^(?:\s*[xo<]?-?\.+-[xo>]?\s*)/,
                /^(?::\d+)/
            ],
            conditions: {
                STYLE_DEFINITION: {
                    rules: [
                        29
                    ],
                    inclusive: !1
                },
                STYLE_STMNT: {
                    rules: [
                        28
                    ],
                    inclusive: !1
                },
                CLASSDEFID: {
                    rules: [
                        23
                    ],
                    inclusive: !1
                },
                CLASSDEF: {
                    rules: [
                        21,
                        22
                    ],
                    inclusive: !1
                },
                CLASS_STYLE: {
                    rules: [
                        26
                    ],
                    inclusive: !1
                },
                CLASS: {
                    rules: [
                        25
                    ],
                    inclusive: !1
                },
                LLABEL: {
                    rules: [
                        100,
                        101,
                        102,
                        103,
                        104
                    ],
                    inclusive: !1
                },
                ARROW_DIR: {
                    rules: [
                        86,
                        87,
                        88,
                        89,
                        90,
                        91,
                        92
                    ],
                    inclusive: !1
                },
                BLOCK_ARROW: {
                    rules: [
                        77,
                        82,
                        85
                    ],
                    inclusive: !1
                },
                NODE: {
                    rules: [
                        38,
                        39,
                        40,
                        41,
                        42,
                        43,
                        44,
                        45,
                        46,
                        47,
                        48,
                        49,
                        50,
                        51,
                        52,
                        53,
                        54,
                        78,
                        81
                    ],
                    inclusive: !1
                },
                md_string: {
                    rules: [
                        10,
                        11,
                        79,
                        80
                    ],
                    inclusive: !1
                },
                space: {
                    rules: [],
                    inclusive: !1
                },
                string: {
                    rules: [
                        13,
                        14,
                        83,
                        84
                    ],
                    inclusive: !1
                },
                acc_descr_multiline: {
                    rules: [
                        35,
                        36
                    ],
                    inclusive: !1
                },
                acc_descr: {
                    rules: [
                        33
                    ],
                    inclusive: !1
                },
                acc_title: {
                    rules: [
                        31
                    ],
                    inclusive: !1
                },
                INITIAL: {
                    rules: [
                        0,
                        1,
                        2,
                        3,
                        4,
                        5,
                        6,
                        7,
                        8,
                        9,
                        12,
                        15,
                        16,
                        17,
                        18,
                        19,
                        20,
                        24,
                        27,
                        30,
                        32,
                        34,
                        37,
                        55,
                        56,
                        57,
                        58,
                        59,
                        60,
                        61,
                        62,
                        63,
                        64,
                        65,
                        66,
                        67,
                        68,
                        69,
                        70,
                        71,
                        72,
                        73,
                        74,
                        75,
                        76,
                        93,
                        94,
                        95,
                        96,
                        97,
                        98,
                        99,
                        105
                    ],
                    inclusive: !0
                }
            }
        };
        return C;
    }();
    B.lexer = Z;
    function M() {
        this.yy = {};
    }
    return (0, _chunkGTKDMUJJMjs.a)(M, "Parser"), M.prototype = B, B.Parser = M, new M;
}();
ie.parser = ie;
var _e = ie;
var O = new Map, ae = [], ne = new Map, we = "color", Be = "fill", Ge = "bgFill", Te = ",", He = (0, _chunkNQURTBEVMjs.X)(), q = new Map, Ue = (0, _chunkGTKDMUJJMjs.a)((e)=>(0, _chunkNQURTBEVMjs.L).sanitizeText(e, He), "sanitizeText"), Xe = (0, _chunkGTKDMUJJMjs.a)(function(e, o = "") {
    let g = q.get(e);
    g || (g = {
        id: e,
        styles: [],
        textStyles: []
    }, q.set(e, g)), o?.split(Te).forEach((l)=>{
        let s = l.replace(/([^;]*);/, "$1").trim();
        if (RegExp(we).exec(l)) {
            let a = s.replace(Be, Ge).replace(we, Be);
            g.textStyles.push(a);
        }
        g.styles.push(s);
    });
}, "addStyleClass"), qe = (0, _chunkGTKDMUJJMjs.a)(function(e, o = "") {
    let g = O.get(e);
    o != null && (g.styles = o.split(Te));
}, "addStyle2Node"), Je = (0, _chunkGTKDMUJJMjs.a)(function(e, o) {
    e.split(",").forEach(function(g) {
        let l = O.get(g);
        if (l === void 0) {
            let s = g.trim();
            l = {
                id: s,
                type: "na",
                children: []
            }, O.set(s, l);
        }
        l.classes || (l.classes = []), l.classes.push(o);
    });
}, "setCssClass"), Oe = (0, _chunkGTKDMUJJMjs.a)((e, o)=>{
    let g = e.flat(), l = [];
    for (let s of g){
        if (s.label && (s.label = Ue(s.label)), s.type === "classDef") {
            Xe(s.id, s.css);
            continue;
        }
        if (s.type === "applyClass") {
            Je(s.id, s?.styleClass ?? "");
            continue;
        }
        if (s.type === "applyStyles") {
            s?.stylesStr && qe(s.id, s?.stylesStr);
            continue;
        }
        if (s.type === "column-setting") o.columns = s.columns ?? -1;
        else if (s.type === "edge") {
            let u = (ne.get(s.id) ?? 0) + 1;
            ne.set(s.id, u), s.id = u + "-" + s.id, ae.push(s);
        } else {
            s.label || (s.type === "composite" ? s.label = "" : s.label = s.id);
            let u = O.get(s.id);
            if (u === void 0 ? O.set(s.id, s) : (s.type !== "na" && (u.type = s.type), s.label !== s.id && (u.label = s.label)), s.children && Oe(s.children, s), s.type === "space") {
                let a = s.width ?? 1;
                for(let b = 0; b < a; b++){
                    let p = (0, _chunkBKDDFIKNMjs.e)(s);
                    p.id = p.id + "-" + b, O.set(p.id, p), l.push(p);
                }
            } else u === void 0 && l.push(s);
        }
    }
    o.children = l;
}, "populateBlockDatabase"), oe = [], V = {
    id: "root",
    type: "composite",
    children: [],
    columns: -1
}, Ze = (0, _chunkGTKDMUJJMjs.a)(()=>{
    (0, _chunkNQURTBEVMjs.b).debug("Clear called"), (0, _chunkNQURTBEVMjs.P)(), V = {
        id: "root",
        type: "composite",
        children: [],
        columns: -1
    }, O = new Map([
        [
            "root",
            V
        ]
    ]), oe = [], q = new Map, ae = [], ne = new Map;
}, "clear");
function Qe(e) {
    switch((0, _chunkNQURTBEVMjs.b).debug("typeStr2Type", e), e){
        case "[]":
            return "square";
        case "()":
            return (0, _chunkNQURTBEVMjs.b).debug("we have a round"), "round";
        case "(())":
            return "circle";
        case ">]":
            return "rect_left_inv_arrow";
        case "{}":
            return "diamond";
        case "{{}}":
            return "hexagon";
        case "([])":
            return "stadium";
        case "[[]]":
            return "subroutine";
        case "[()]":
            return "cylinder";
        case "((()))":
            return "doublecircle";
        case "[//]":
            return "lean_right";
        case "[\\\\]":
            return "lean_left";
        case "[/\\]":
            return "trapezoid";
        case "[\\/]":
            return "inv_trapezoid";
        case "<[]>":
            return "block_arrow";
        default:
            return "na";
    }
}
(0, _chunkGTKDMUJJMjs.a)(Qe, "typeStr2Type");
function $e(e) {
    switch((0, _chunkNQURTBEVMjs.b).debug("typeStr2Type", e), e){
        case "==":
            return "thick";
        default:
            return "normal";
    }
}
(0, _chunkGTKDMUJJMjs.a)($e, "edgeTypeStr2Type");
function et(e) {
    switch(e.trim()){
        case "--x":
            return "arrow_cross";
        case "--o":
            return "arrow_circle";
        default:
            return "arrow_point";
    }
}
(0, _chunkGTKDMUJJMjs.a)(et, "edgeStrToEdgeData");
var Ne = 0, tt = (0, _chunkGTKDMUJJMjs.a)(()=>(Ne++, "id-" + Math.random().toString(36).substr(2, 12) + "-" + Ne), "generateId"), rt = (0, _chunkGTKDMUJJMjs.a)((e)=>{
    V.children = e, Oe(e, V), oe = V.children;
}, "setHierarchy"), st = (0, _chunkGTKDMUJJMjs.a)((e)=>{
    let o = O.get(e);
    return o ? o.columns ? o.columns : o.children ? o.children.length : -1 : -1;
}, "getColumns"), it = (0, _chunkGTKDMUJJMjs.a)(()=>[
        ...O.values()
    ], "getBlocksFlat"), nt = (0, _chunkGTKDMUJJMjs.a)(()=>oe || [], "getBlocks"), at = (0, _chunkGTKDMUJJMjs.a)(()=>ae, "getEdges"), ot = (0, _chunkGTKDMUJJMjs.a)((e)=>O.get(e), "getBlock"), lt = (0, _chunkGTKDMUJJMjs.a)((e)=>{
    O.set(e.id, e);
}, "setBlock"), ct = (0, _chunkGTKDMUJJMjs.a)(()=>console, "getLogger"), gt = (0, _chunkGTKDMUJJMjs.a)(function() {
    return q;
}, "getClasses"), ht = {
    getConfig: (0, _chunkGTKDMUJJMjs.a)(()=>(0, _chunkNQURTBEVMjs.A)().block, "getConfig"),
    typeStr2Type: Qe,
    edgeTypeStr2Type: $e,
    edgeStrToEdgeData: et,
    getLogger: ct,
    getBlocksFlat: it,
    getBlocks: nt,
    getEdges: at,
    setHierarchy: rt,
    getBlock: ot,
    setBlock: lt,
    getColumns: st,
    getClasses: gt,
    clear: Ze,
    generateId: tt
}, Ce = ht;
var J = (0, _chunkGTKDMUJJMjs.a)((e, o)=>{
    let g = (0, _chunkNQURTBEVMjs.m), l = g(e, "r"), s = g(e, "g"), u = g(e, "b");
    return (0, _chunkNQURTBEVMjs.l)(l, s, u, o);
}, "fade"), ut = (0, _chunkGTKDMUJJMjs.a)((e)=>`.label {
    font-family: ${e.fontFamily};
    color: ${e.nodeTextColor || e.textColor};
  }
  .cluster-label text {
    fill: ${e.titleColor};
  }
  .cluster-label span,p {
    color: ${e.titleColor};
  }



  .label text,span,p {
    fill: ${e.nodeTextColor || e.textColor};
    color: ${e.nodeTextColor || e.textColor};
  }

  .node rect,
  .node circle,
  .node ellipse,
  .node polygon,
  .node path {
    fill: ${e.mainBkg};
    stroke: ${e.nodeBorder};
    stroke-width: 1px;
  }
  .flowchart-label text {
    text-anchor: middle;
  }
  // .flowchart-label .text-outer-tspan {
  //   text-anchor: middle;
  // }
  // .flowchart-label .text-inner-tspan {
  //   text-anchor: start;
  // }

  .node .label {
    text-align: center;
  }
  .node.clickable {
    cursor: pointer;
  }

  .arrowheadPath {
    fill: ${e.arrowheadColor};
  }

  .edgePath .path {
    stroke: ${e.lineColor};
    stroke-width: 2.0px;
  }

  .flowchart-link {
    stroke: ${e.lineColor};
    fill: none;
  }

  .edgeLabel {
    background-color: ${e.edgeLabelBackground};
    rect {
      opacity: 0.5;
      background-color: ${e.edgeLabelBackground};
      fill: ${e.edgeLabelBackground};
    }
    text-align: center;
  }

  /* For html labels only */
  .labelBkg {
    background-color: ${J(e.edgeLabelBackground, .5)};
    // background-color:
  }

  .node .cluster {
    // fill: ${J(e.mainBkg, .5)};
    fill: ${J(e.clusterBkg, .5)};
    stroke: ${J(e.clusterBorder, .2)};
    box-shadow: rgba(50, 50, 93, 0.25) 0px 13px 27px -5px, rgba(0, 0, 0, 0.3) 0px 8px 16px -8px;
    stroke-width: 1px;
  }

  .cluster text {
    fill: ${e.titleColor};
  }

  .cluster span,p {
    color: ${e.titleColor};
  }
  /* .cluster div {
    color: ${e.titleColor};
  } */

  div.mermaidTooltip {
    position: absolute;
    text-align: center;
    max-width: 200px;
    padding: 2px;
    font-family: ${e.fontFamily};
    font-size: 12px;
    background: ${e.tertiaryColor};
    border: 1px solid ${e.border2};
    border-radius: 2px;
    pointer-events: none;
    z-index: 100;
  }

  .flowchartTitleText {
    text-anchor: middle;
    font-size: 18px;
    fill: ${e.textColor};
  }
`, "getStyles"), Ie = ut;
var y = (0, _chunkNQURTBEVMjs.X)()?.block?.padding ?? 8;
function dt(e, o) {
    if (e === 0 || !Number.isInteger(e)) throw new Error("Columns must be an integer !== 0.");
    if (o < 0 || !Number.isInteger(o)) throw new Error("Position must be a non-negative integer." + o);
    if (e < 0) return {
        px: o,
        py: 0
    };
    if (e === 1) return {
        px: 0,
        py: o
    };
    let g = o % e, l = Math.floor(o / e);
    return {
        px: g,
        py: l
    };
}
(0, _chunkGTKDMUJJMjs.a)(dt, "calculateBlockPosition");
var pt = (0, _chunkGTKDMUJJMjs.a)((e)=>{
    let o = 0, g = 0;
    for (let l of e.children){
        let { width: s, height: u, x: a, y: b } = l.size ?? {
            width: 0,
            height: 0,
            x: 0,
            y: 0
        };
        (0, _chunkNQURTBEVMjs.b).debug("getMaxChildSize abc95 child:", l.id, "width:", s, "height:", u, "x:", a, "y:", b, l.type), l.type !== "space" && (s > o && (o = s / (e.widthInColumns ?? 1)), u > g && (g = u));
    }
    return {
        width: o,
        height: g
    };
}, "getMaxChildSize");
function le(e, o, g = 0, l = 0) {
    (0, _chunkNQURTBEVMjs.b).debug("setBlockSizes abc95 (start)", e.id, e?.size?.x, "block width =", e?.size, "sieblingWidth", g), e?.size?.width || (e.size = {
        width: g,
        height: l,
        x: 0,
        y: 0
    });
    let s = 0, u = 0;
    if (e.children?.length > 0) {
        for (let x of e.children)le(x, o);
        let a = pt(e);
        s = a.width, u = a.height, (0, _chunkNQURTBEVMjs.b).debug("setBlockSizes abc95 maxWidth of", e.id, ":s children is ", s, u);
        for (let x of e.children)x.size && ((0, _chunkNQURTBEVMjs.b).debug(`abc95 Setting size of children of ${e.id} id=${x.id} ${s} ${u} ${JSON.stringify(x.size)}`), x.size.width = s * (x.widthInColumns ?? 1) + y * ((x.widthInColumns ?? 1) - 1), x.size.height = u, x.size.x = 0, x.size.y = 0, (0, _chunkNQURTBEVMjs.b).debug(`abc95 updating size of ${e.id} children child:${x.id} maxWidth:${s} maxHeight:${u}`));
        for (let x of e.children)le(x, o, s, u);
        let b = e.columns ?? -1, p = 0;
        for (let x of e.children)p += x.widthInColumns ?? 1;
        let k = e.children.length;
        b > 0 && b < p && (k = b);
        let f = Math.ceil(p / k), L = k * (s + y) + y, I = f * (u + y) + y;
        if (L < g) {
            (0, _chunkNQURTBEVMjs.b).debug(`Detected to small siebling: abc95 ${e.id} sieblingWidth ${g} sieblingHeight ${l} width ${L}`), L = g, I = l;
            let x = (g - k * y - y) / k, E = (l - f * y - y) / f;
            (0, _chunkNQURTBEVMjs.b).debug("Size indata abc88", e.id, "childWidth", x, "maxWidth", s), (0, _chunkNQURTBEVMjs.b).debug("Size indata abc88", e.id, "childHeight", E, "maxHeight", u), (0, _chunkNQURTBEVMjs.b).debug("Size indata abc88 xSize", k, "padding", y);
            for (let _ of e.children)_.size && (_.size.width = x, _.size.height = E, _.size.x = 0, _.size.y = 0);
        }
        if ((0, _chunkNQURTBEVMjs.b).debug(`abc95 (finale calc) ${e.id} xSize ${k} ySize ${f} columns ${b}${e.children.length} width=${Math.max(L, e.size?.width || 0)}`), L < (e?.size?.width || 0)) {
            L = e?.size?.width || 0;
            let x = b > 0 ? Math.min(e.children.length, b) : e.children.length;
            if (x > 0) {
                let E = (L - x * y - y) / x;
                (0, _chunkNQURTBEVMjs.b).debug("abc95 (growing to fit) width", e.id, L, e.size?.width, E);
                for (let _ of e.children)_.size && (_.size.width = E);
            }
        }
        e.size = {
            width: L,
            height: I,
            x: 0,
            y: 0
        };
    }
    (0, _chunkNQURTBEVMjs.b).debug("setBlockSizes abc94 (done)", e.id, e?.size?.x, e?.size?.width, e?.size?.y, e?.size?.height);
}
(0, _chunkGTKDMUJJMjs.a)(le, "setBlockSizes");
function ze(e, o) {
    (0, _chunkNQURTBEVMjs.b).debug(`abc85 layout blocks (=>layoutBlocks) ${e.id} x: ${e?.size?.x} y: ${e?.size?.y} width: ${e?.size?.width}`);
    let g = e.columns ?? -1;
    if ((0, _chunkNQURTBEVMjs.b).debug("layoutBlocks columns abc95", e.id, "=>", g, e), e.children && e.children.length > 0) {
        let l = e?.children[0]?.size?.width ?? 0, s = e.children.length * l + (e.children.length - 1) * y;
        (0, _chunkNQURTBEVMjs.b).debug("widthOfChildren 88", s, "posX");
        let u = 0;
        (0, _chunkNQURTBEVMjs.b).debug("abc91 block?.size?.x", e.id, e?.size?.x);
        let a = e?.size?.x ? e?.size?.x + (-e?.size?.width / 2 || 0) : -y, b = 0;
        for (let p of e.children){
            let k = e;
            if (!p.size) continue;
            let { width: f, height: L } = p.size, { px: I, py: x } = dt(g, u);
            if (x != b && (b = x, a = e?.size?.x ? e?.size?.x + (-e?.size?.width / 2 || 0) : -y, (0, _chunkNQURTBEVMjs.b).debug("New row in layout for block", e.id, " and child ", p.id, b)), (0, _chunkNQURTBEVMjs.b).debug(`abc89 layout blocks (child) id: ${p.id} Pos: ${u} (px, py) ${I},${x} (${k?.size?.x},${k?.size?.y}) parent: ${k.id} width: ${f}${y}`), k.size) {
                let E = f / 2;
                p.size.x = a + y + E, (0, _chunkNQURTBEVMjs.b).debug(`abc91 layout blocks (calc) px, pyid:${p.id} startingPos=X${a} new startingPosX${p.size.x} ${E} padding=${y} width=${f} halfWidth=${E} => x:${p.size.x} y:${p.size.y} ${p.widthInColumns} (width * (child?.w || 1)) / 2 ${f * (p?.widthInColumns ?? 1) / 2}`), a = p.size.x + E, p.size.y = k.size.y - k.size.height / 2 + x * (L + y) + L / 2 + y, (0, _chunkNQURTBEVMjs.b).debug(`abc88 layout blocks (calc) px, pyid:${p.id}startingPosX${a}${y}${E}=>x:${p.size.x}y:${p.size.y}${p.widthInColumns}(width * (child?.w || 1)) / 2${f * (p?.widthInColumns ?? 1) / 2}`);
            }
            p.children && ze(p, o), u += p?.widthInColumns ?? 1, (0, _chunkNQURTBEVMjs.b).debug("abc88 columnsPos", p, u);
        }
    }
    (0, _chunkNQURTBEVMjs.b).debug(`layout blocks (<==layoutBlocks) ${e.id} x: ${e?.size?.x} y: ${e?.size?.y} width: ${e?.size?.width}`);
}
(0, _chunkGTKDMUJJMjs.a)(ze, "layoutBlocks");
function Ae(e, { minX: o, minY: g, maxX: l, maxY: s } = {
    minX: 0,
    minY: 0,
    maxX: 0,
    maxY: 0
}) {
    if (e.size && e.id !== "root") {
        let { x: u, y: a, width: b, height: p } = e.size;
        u - b / 2 < o && (o = u - b / 2), a - p / 2 < g && (g = a - p / 2), u + b / 2 > l && (l = u + b / 2), a + p / 2 > s && (s = a + p / 2);
    }
    if (e.children) for (let u of e.children)({ minX: o, minY: g, maxX: l, maxY: s } = Ae(u, {
        minX: o,
        minY: g,
        maxX: l,
        maxY: s
    }));
    return {
        minX: o,
        minY: g,
        maxX: l,
        maxY: s
    };
}
(0, _chunkGTKDMUJJMjs.a)(Ae, "findBounds");
function Re(e) {
    let o = e.getBlock("root");
    if (!o) return;
    le(o, e, 0, 0), ze(o, e), (0, _chunkNQURTBEVMjs.b).debug("getBlocks", JSON.stringify(o, null, 2));
    let { minX: g, minY: l, maxX: s, maxY: u } = Ae(o), a = u - l, b = s - g;
    return {
        x: g,
        y: l,
        width: b,
        height: a
    };
}
(0, _chunkGTKDMUJJMjs.a)(Re, "layout");
function ve(e, o, g = !1) {
    let l = e, s = "default";
    (l?.classes?.length || 0) > 0 && (s = (l?.classes ?? []).join(" ")), s = s + " flowchart-label";
    let u = 0, a = "", b;
    switch(l.type){
        case "round":
            u = 5, a = "rect";
            break;
        case "composite":
            u = 0, a = "composite", b = 0;
            break;
        case "square":
            a = "rect";
            break;
        case "diamond":
            a = "question";
            break;
        case "hexagon":
            a = "hexagon";
            break;
        case "block_arrow":
            a = "block_arrow";
            break;
        case "odd":
            a = "rect_left_inv_arrow";
            break;
        case "lean_right":
            a = "lean_right";
            break;
        case "lean_left":
            a = "lean_left";
            break;
        case "trapezoid":
            a = "trapezoid";
            break;
        case "inv_trapezoid":
            a = "inv_trapezoid";
            break;
        case "rect_left_inv_arrow":
            a = "rect_left_inv_arrow";
            break;
        case "circle":
            a = "circle";
            break;
        case "ellipse":
            a = "ellipse";
            break;
        case "stadium":
            a = "stadium";
            break;
        case "subroutine":
            a = "subroutine";
            break;
        case "cylinder":
            a = "cylinder";
            break;
        case "group":
            a = "rect";
            break;
        case "doublecircle":
            a = "doublecircle";
            break;
        default:
            a = "rect";
    }
    let p = (0, _chunkAC3VT7B7Mjs.d)(l?.styles ?? []), k = l.label, f = l.size ?? {
        width: 0,
        height: 0,
        x: 0,
        y: 0
    };
    return {
        labelStyle: p.labelStyle,
        shape: a,
        labelText: k,
        rx: u,
        ry: u,
        class: s,
        style: p.style,
        id: l.id,
        directions: l.directions,
        width: f.width,
        height: f.height,
        x: f.x,
        y: f.y,
        positioned: g,
        intersect: void 0,
        type: l.type,
        padding: b ?? (0, _chunkNQURTBEVMjs.A)()?.block?.padding ?? 0
    };
}
(0, _chunkGTKDMUJJMjs.a)(ve, "getNodeFromBlock");
async function bt(e, o, g) {
    let l = ve(o, g, !1);
    if (l.type === "group") return;
    let s = await (0, _chunkUWHJNN4QMjs.d)(e, l), u = s.node().getBBox(), a = g.getBlock(l.id);
    a.size = {
        width: u.width,
        height: u.height,
        x: 0,
        y: 0,
        node: s
    }, g.setBlock(a), s.remove();
}
(0, _chunkGTKDMUJJMjs.a)(bt, "calculateBlockSize");
async function ft(e, o, g) {
    let l = ve(o, g, !0);
    g.getBlock(l.id).type !== "space" && (await (0, _chunkUWHJNN4QMjs.d)(e, l), o.intersect = l?.intersect, (0, _chunkUWHJNN4QMjs.g)(l));
}
(0, _chunkGTKDMUJJMjs.a)(ft, "insertBlockPositioned");
async function ce(e, o, g, l) {
    for (let s of o)await l(e, s, g), s.children && await ce(e, s.children, g, l);
}
(0, _chunkGTKDMUJJMjs.a)(ce, "performOperations");
async function Pe(e, o, g) {
    await ce(e, o, g, bt);
}
(0, _chunkGTKDMUJJMjs.a)(Pe, "calculateBlockSizes");
async function Fe(e, o, g) {
    await ce(e, o, g, ft);
}
(0, _chunkGTKDMUJJMjs.a)(Fe, "insertBlocks");
async function Me(e, o, g, l, s) {
    let u = new (0, _chunk6XGRHI2AMjs.a)({
        multigraph: !0,
        compound: !0
    });
    u.setGraph({
        rankdir: "TB",
        nodesep: 10,
        ranksep: 10,
        marginx: 8,
        marginy: 8
    });
    for (let a of g)a.size && u.setNode(a.id, {
        width: a.size.width,
        height: a.size.height,
        intersect: a.intersect
    });
    for (let a of o)if (a.start && a.end) {
        let b = l.getBlock(a.start), p = l.getBlock(a.end);
        if (b?.size && p?.size) {
            let k = b.size, f = p.size, L = [
                {
                    x: k.x,
                    y: k.y
                },
                {
                    x: k.x + (f.x - k.x) / 2,
                    y: k.y + (f.y - k.y) / 2
                },
                {
                    x: f.x,
                    y: f.y
                }
            ];
            (0, _chunk2RSIMOBZMjs.e)(e, {
                v: a.start,
                w: a.end,
                name: a.id
            }, {
                ...a,
                arrowTypeEnd: a.arrowTypeEnd,
                arrowTypeStart: a.arrowTypeStart,
                points: L,
                classes: "edge-thickness-normal edge-pattern-solid flowchart-link LS-a1 LE-b1"
            }, void 0, "block", u, s), a.label && (await (0, _chunk2RSIMOBZMjs.c)(e, {
                ...a,
                label: a.label,
                labelStyle: "stroke: #333; stroke-width: 1.5px;fill:none;",
                arrowTypeEnd: a.arrowTypeEnd,
                arrowTypeStart: a.arrowTypeStart,
                points: L,
                classes: "edge-thickness-normal edge-pattern-solid flowchart-link LS-a1 LE-b1"
            }), (0, _chunk2RSIMOBZMjs.d)({
                ...a,
                x: L[1].x,
                y: L[1].y
            }, {
                originalPath: L
            }));
        }
    }
}
(0, _chunkGTKDMUJJMjs.a)(Me, "insertEdges");
var xt = (0, _chunkGTKDMUJJMjs.a)(function(e, o) {
    return o.db.getClasses();
}, "getClasses"), kt = (0, _chunkGTKDMUJJMjs.a)(async function(e, o, g, l) {
    let { securityLevel: s, block: u } = (0, _chunkNQURTBEVMjs.A)(), a = l.db, b;
    s === "sandbox" && (b = (0, _chunkNQURTBEVMjs.fa)("#i" + o));
    let p = s === "sandbox" ? (0, _chunkNQURTBEVMjs.fa)(b.nodes()[0].contentDocument.body) : (0, _chunkNQURTBEVMjs.fa)("body"), k = s === "sandbox" ? p.select(`[id="${o}"]`) : (0, _chunkNQURTBEVMjs.fa)(`[id="${o}"]`);
    (0, _chunk2RSIMOBZMjs.a)(k, [
        "point",
        "circle",
        "cross"
    ], l.type, o);
    let L = a.getBlocks(), I = a.getBlocksFlat(), x = a.getEdges(), E = k.insert("g").attr("class", "block");
    await Pe(E, L, a);
    let _ = Re(a);
    if (await Fe(E, L, a), await Me(E, x, I, a, o), _) {
        let B = _, Z = Math.max(1, Math.round(.125 * (B.width / B.height))), M = B.height + Z + 10, C = B.width + 10, { useMaxWidth: h } = u;
        (0, _chunkNQURTBEVMjs.M)(k, M, C, !!h), (0, _chunkNQURTBEVMjs.b).debug("Here Bounds", _, B), k.attr("viewBox", `${B.x - 5} ${B.y - 5} ${B.width + 10} ${B.height + 10}`);
    }
}, "draw"), Ke = {
    draw: kt,
    getClasses: xt
};
var rr = {
    parser: _e,
    db: Ce,
    renderer: Ke,
    styles: Ie
};

},{"./chunk-2RSIMOBZ.mjs":"6y77U","./chunk-UWHJNN4Q.mjs":"6LAlC","./chunk-U6LOUQAF.mjs":"v9pSW","./chunk-KMOJB3TB.mjs":"aJH4M","./chunk-6XGRHI2A.mjs":"fUQIF","./chunk-AC3VT7B7.mjs":"1eiUz","./chunk-TI4EEUUG.mjs":"8SKrN","./chunk-NQURTBEV.mjs":"iASFe","./chunk-BKDDFIKN.mjs":"hADfH","./chunk-YPUTD6PB.mjs":"aSdv1","./chunk-6BY5RJGC.mjs":"bRXnR","./chunk-GTKDMUJJ.mjs":"fruhx","@parcel/transformer-js/src/esmodule-helpers.js":"1cdyk"}],"6y77U":[function(require,module,exports,__globalThis) {
var parcelHelpers = require("@parcel/transformer-js/src/esmodule-helpers.js");
parcelHelpers.defineInteropFlag(exports);
parcelHelpers.export(exports, "a", ()=>tt);
parcelHelpers.export(exports, "b", ()=>bt);
parcelHelpers.export(exports, "c", ()=>xt);
parcelHelpers.export(exports, "d", ()=>yt);
parcelHelpers.export(exports, "e", ()=>Lt);
var _chunkUWHJNN4QMjs = require("./chunk-UWHJNN4Q.mjs");
var _chunkU6LOUQAFMjs = require("./chunk-U6LOUQAF.mjs");
var _chunkKMOJB3TBMjs = require("./chunk-KMOJB3TB.mjs");
var _chunkAC3VT7B7Mjs = require("./chunk-AC3VT7B7.mjs");
var _chunkNQURTBEVMjs = require("./chunk-NQURTBEV.mjs");
var _chunkGTKDMUJJMjs = require("./chunk-GTKDMUJJ.mjs");
var Z = (0, _chunkGTKDMUJJMjs.a)((a, t, r, p)=>{
    t.forEach((o)=>{
        F[o](a, r, p);
    });
}, "insertMarkers"), G = (0, _chunkGTKDMUJJMjs.a)((a, t, r)=>{
    (0, _chunkNQURTBEVMjs.b).trace("Making markers for ", r), a.append("defs").append("marker").attr("id", r + "_" + t + "-extensionStart").attr("class", "marker extension " + t).attr("refX", 18).attr("refY", 7).attr("markerWidth", 190).attr("markerHeight", 240).attr("orient", "auto").append("path").attr("d", "M 1,7 L18,13 V 1 Z"), a.append("defs").append("marker").attr("id", r + "_" + t + "-extensionEnd").attr("class", "marker extension " + t).attr("refX", 1).attr("refY", 7).attr("markerWidth", 20).attr("markerHeight", 28).attr("orient", "auto").append("path").attr("d", "M 1,1 V 13 L18,7 Z");
}, "extension"), V = (0, _chunkGTKDMUJJMjs.a)((a, t, r)=>{
    a.append("defs").append("marker").attr("id", r + "_" + t + "-compositionStart").attr("class", "marker composition " + t).attr("refX", 18).attr("refY", 7).attr("markerWidth", 190).attr("markerHeight", 240).attr("orient", "auto").append("path").attr("d", "M 18,7 L9,13 L1,7 L9,1 Z"), a.append("defs").append("marker").attr("id", r + "_" + t + "-compositionEnd").attr("class", "marker composition " + t).attr("refX", 1).attr("refY", 7).attr("markerWidth", 20).attr("markerHeight", 28).attr("orient", "auto").append("path").attr("d", "M 18,7 L9,13 L1,7 L9,1 Z");
}, "composition"), q = (0, _chunkGTKDMUJJMjs.a)((a, t, r)=>{
    a.append("defs").append("marker").attr("id", r + "_" + t + "-aggregationStart").attr("class", "marker aggregation " + t).attr("refX", 18).attr("refY", 7).attr("markerWidth", 190).attr("markerHeight", 240).attr("orient", "auto").append("path").attr("d", "M 18,7 L9,13 L1,7 L9,1 Z"), a.append("defs").append("marker").attr("id", r + "_" + t + "-aggregationEnd").attr("class", "marker aggregation " + t).attr("refX", 1).attr("refY", 7).attr("markerWidth", 20).attr("markerHeight", 28).attr("orient", "auto").append("path").attr("d", "M 18,7 L9,13 L1,7 L9,1 Z");
}, "aggregation"), A = (0, _chunkGTKDMUJJMjs.a)((a, t, r)=>{
    a.append("defs").append("marker").attr("id", r + "_" + t + "-dependencyStart").attr("class", "marker dependency " + t).attr("refX", 6).attr("refY", 7).attr("markerWidth", 190).attr("markerHeight", 240).attr("orient", "auto").append("path").attr("d", "M 5,7 L9,13 L1,7 L9,1 Z"), a.append("defs").append("marker").attr("id", r + "_" + t + "-dependencyEnd").attr("class", "marker dependency " + t).attr("refX", 13).attr("refY", 7).attr("markerWidth", 20).attr("markerHeight", 28).attr("orient", "auto").append("path").attr("d", "M 18,7 L9,13 L14,7 L9,1 Z");
}, "dependency"), I = (0, _chunkGTKDMUJJMjs.a)((a, t, r)=>{
    a.append("defs").append("marker").attr("id", r + "_" + t + "-lollipopStart").attr("class", "marker lollipop " + t).attr("refX", 13).attr("refY", 7).attr("markerWidth", 190).attr("markerHeight", 240).attr("orient", "auto").append("circle").attr("stroke", "black").attr("fill", "transparent").attr("cx", 7).attr("cy", 7).attr("r", 6), a.append("defs").append("marker").attr("id", r + "_" + t + "-lollipopEnd").attr("class", "marker lollipop " + t).attr("refX", 1).attr("refY", 7).attr("markerWidth", 190).attr("markerHeight", 240).attr("orient", "auto").append("circle").attr("stroke", "black").attr("fill", "transparent").attr("cx", 7).attr("cy", 7).attr("r", 6);
}, "lollipop"), N = (0, _chunkGTKDMUJJMjs.a)((a, t, r)=>{
    a.append("marker").attr("id", r + "_" + t + "-pointEnd").attr("class", "marker " + t).attr("viewBox", "0 0 10 10").attr("refX", 6).attr("refY", 5).attr("markerUnits", "userSpaceOnUse").attr("markerWidth", 12).attr("markerHeight", 12).attr("orient", "auto").append("path").attr("d", "M 0 0 L 10 5 L 0 10 z").attr("class", "arrowMarkerPath").style("stroke-width", 1).style("stroke-dasharray", "1,0"), a.append("marker").attr("id", r + "_" + t + "-pointStart").attr("class", "marker " + t).attr("viewBox", "0 0 10 10").attr("refX", 4.5).attr("refY", 5).attr("markerUnits", "userSpaceOnUse").attr("markerWidth", 12).attr("markerHeight", 12).attr("orient", "auto").append("path").attr("d", "M 0 5 L 10 10 L 10 0 z").attr("class", "arrowMarkerPath").style("stroke-width", 1).style("stroke-dasharray", "1,0");
}, "point"), Q = (0, _chunkGTKDMUJJMjs.a)((a, t, r)=>{
    a.append("marker").attr("id", r + "_" + t + "-circleEnd").attr("class", "marker " + t).attr("viewBox", "0 0 10 10").attr("refX", 11).attr("refY", 5).attr("markerUnits", "userSpaceOnUse").attr("markerWidth", 11).attr("markerHeight", 11).attr("orient", "auto").append("circle").attr("cx", "5").attr("cy", "5").attr("r", "5").attr("class", "arrowMarkerPath").style("stroke-width", 1).style("stroke-dasharray", "1,0"), a.append("marker").attr("id", r + "_" + t + "-circleStart").attr("class", "marker " + t).attr("viewBox", "0 0 10 10").attr("refX", -1).attr("refY", 5).attr("markerUnits", "userSpaceOnUse").attr("markerWidth", 11).attr("markerHeight", 11).attr("orient", "auto").append("circle").attr("cx", "5").attr("cy", "5").attr("r", "5").attr("class", "arrowMarkerPath").style("stroke-width", 1).style("stroke-dasharray", "1,0");
}, "circle"), j = (0, _chunkGTKDMUJJMjs.a)((a, t, r)=>{
    a.append("marker").attr("id", r + "_" + t + "-crossEnd").attr("class", "marker cross " + t).attr("viewBox", "0 0 11 11").attr("refX", 12).attr("refY", 5.2).attr("markerUnits", "userSpaceOnUse").attr("markerWidth", 11).attr("markerHeight", 11).attr("orient", "auto").append("path").attr("d", "M 1,1 l 9,9 M 10,1 l -9,9").attr("class", "arrowMarkerPath").style("stroke-width", 2).style("stroke-dasharray", "1,0"), a.append("marker").attr("id", r + "_" + t + "-crossStart").attr("class", "marker cross " + t).attr("viewBox", "0 0 11 11").attr("refX", -1).attr("refY", 5.2).attr("markerUnits", "userSpaceOnUse").attr("markerWidth", 11).attr("markerHeight", 11).attr("orient", "auto").append("path").attr("d", "M 1,1 l 9,9 M 10,1 l -9,9").attr("class", "arrowMarkerPath").style("stroke-width", 2).style("stroke-dasharray", "1,0");
}, "cross"), z = (0, _chunkGTKDMUJJMjs.a)((a, t, r)=>{
    a.append("defs").append("marker").attr("id", r + "_" + t + "-barbEnd").attr("refX", 19).attr("refY", 7).attr("markerWidth", 20).attr("markerHeight", 14).attr("markerUnits", "strokeWidth").attr("orient", "auto").append("path").attr("d", "M 19,7 L9,13 L14,7 L9,1 Z");
}, "barb"), F = {
    extension: G,
    composition: V,
    aggregation: q,
    dependency: A,
    lollipop: I,
    point: N,
    circle: Q,
    cross: j,
    barb: z
}, tt = Z;
var H = (0, _chunkGTKDMUJJMjs.a)((a, t, r, p, o)=>{
    t.arrowTypeStart && C(a, "start", t.arrowTypeStart, r, p, o), t.arrowTypeEnd && C(a, "end", t.arrowTypeEnd, r, p, o);
}, "addEdgeMarkers"), J = {
    arrow_cross: "cross",
    arrow_point: "point",
    arrow_barb: "barb",
    arrow_circle: "circle",
    aggregation: "aggregation",
    extension: "extension",
    composition: "composition",
    dependency: "dependency",
    lollipop: "lollipop"
}, C = (0, _chunkGTKDMUJJMjs.a)((a, t, r, p, o, c)=>{
    let n = J[r];
    if (!n) {
        (0, _chunkNQURTBEVMjs.b).warn(`Unknown arrow type: ${r}`);
        return;
    }
    let e = t === "start" ? "Start" : "End";
    a.attr(`marker-${t}`, `url(${p}#${o}_${c}-${n}${e})`);
}, "addEdgeMarker");
var M = {}, m = {}, bt = (0, _chunkGTKDMUJJMjs.a)(()=>{
    M = {}, m = {};
}, "clear"), xt = (0, _chunkGTKDMUJJMjs.a)((a, t)=>{
    let r = (0, _chunkNQURTBEVMjs.X)(), p = (0, _chunkNQURTBEVMjs.G)(r.flowchart.htmlLabels), o = t.labelType === "markdown" ? (0, _chunkKMOJB3TBMjs.d)(a, t.label, {
        style: t.labelStyle,
        useHtmlLabels: p,
        addSvgBackground: !0
    }, r) : (0, _chunkUWHJNN4QMjs.a)(t.label, t.labelStyle), c = a.insert("g").attr("class", "edgeLabel"), n = c.insert("g").attr("class", "label");
    n.node().appendChild(o);
    let e = o.getBBox();
    if (p) {
        let i = o.children[0], l = (0, _chunkNQURTBEVMjs.fa)(o);
        e = i.getBoundingClientRect(), l.attr("width", e.width), l.attr("height", e.height);
    }
    n.attr("transform", "translate(" + -e.width / 2 + ", " + -e.height / 2 + ")"), M[t.id] = c, t.width = e.width, t.height = e.height;
    let s;
    if (t.startLabelLeft) {
        let i = (0, _chunkUWHJNN4QMjs.a)(t.startLabelLeft, t.labelStyle), l = a.insert("g").attr("class", "edgeTerminals"), f = l.insert("g").attr("class", "inner");
        s = f.node().appendChild(i);
        let h = i.getBBox();
        f.attr("transform", "translate(" + -h.width / 2 + ", " + -h.height / 2 + ")"), m[t.id] || (m[t.id] = {}), m[t.id].startLeft = l, u(s, t.startLabelLeft);
    }
    if (t.startLabelRight) {
        let i = (0, _chunkUWHJNN4QMjs.a)(t.startLabelRight, t.labelStyle), l = a.insert("g").attr("class", "edgeTerminals"), f = l.insert("g").attr("class", "inner");
        s = l.node().appendChild(i), f.node().appendChild(i);
        let h = i.getBBox();
        f.attr("transform", "translate(" + -h.width / 2 + ", " + -h.height / 2 + ")"), m[t.id] || (m[t.id] = {}), m[t.id].startRight = l, u(s, t.startLabelRight);
    }
    if (t.endLabelLeft) {
        let i = (0, _chunkUWHJNN4QMjs.a)(t.endLabelLeft, t.labelStyle), l = a.insert("g").attr("class", "edgeTerminals"), f = l.insert("g").attr("class", "inner");
        s = f.node().appendChild(i);
        let h = i.getBBox();
        f.attr("transform", "translate(" + -h.width / 2 + ", " + -h.height / 2 + ")"), l.node().appendChild(i), m[t.id] || (m[t.id] = {}), m[t.id].endLeft = l, u(s, t.endLabelLeft);
    }
    if (t.endLabelRight) {
        let i = (0, _chunkUWHJNN4QMjs.a)(t.endLabelRight, t.labelStyle), l = a.insert("g").attr("class", "edgeTerminals"), f = l.insert("g").attr("class", "inner");
        s = f.node().appendChild(i);
        let h = i.getBBox();
        f.attr("transform", "translate(" + -h.width / 2 + ", " + -h.height / 2 + ")"), l.node().appendChild(i), m[t.id] || (m[t.id] = {}), m[t.id].endRight = l, u(s, t.endLabelRight);
    }
    return o;
}, "insertEdgeLabel");
function u(a, t) {
    (0, _chunkNQURTBEVMjs.X)().flowchart.htmlLabels && a && (a.style.width = t.length * 9 + "px", a.style.height = "12px");
}
(0, _chunkGTKDMUJJMjs.a)(u, "setTerminalWidth");
var yt = (0, _chunkGTKDMUJJMjs.a)((a, t)=>{
    (0, _chunkNQURTBEVMjs.b).debug("Moving label abc88 ", a.id, a.label, M[a.id], t);
    let r = t.updatedPath ? t.updatedPath : t.originalPath, p = (0, _chunkNQURTBEVMjs.X)(), { subGraphTitleTotalMargin: o } = (0, _chunkU6LOUQAFMjs.a)(p);
    if (a.label) {
        let c = M[a.id], n = a.x, e = a.y;
        if (r) {
            let s = (0, _chunkAC3VT7B7Mjs.m).calcLabelPosition(r);
            (0, _chunkNQURTBEVMjs.b).debug("Moving label " + a.label + " from (", n, ",", e, ") to (", s.x, ",", s.y, ") abc88"), t.updatedPath && (n = s.x, e = s.y);
        }
        c.attr("transform", `translate(${n}, ${e + o / 2})`);
    }
    if (a.startLabelLeft) {
        let c = m[a.id].startLeft, n = a.x, e = a.y;
        if (r) {
            let s = (0, _chunkAC3VT7B7Mjs.m).calcTerminalLabelPosition(a.arrowTypeStart ? 10 : 0, "start_left", r);
            n = s.x, e = s.y;
        }
        c.attr("transform", `translate(${n}, ${e})`);
    }
    if (a.startLabelRight) {
        let c = m[a.id].startRight, n = a.x, e = a.y;
        if (r) {
            let s = (0, _chunkAC3VT7B7Mjs.m).calcTerminalLabelPosition(a.arrowTypeStart ? 10 : 0, "start_right", r);
            n = s.x, e = s.y;
        }
        c.attr("transform", `translate(${n}, ${e})`);
    }
    if (a.endLabelLeft) {
        let c = m[a.id].endLeft, n = a.x, e = a.y;
        if (r) {
            let s = (0, _chunkAC3VT7B7Mjs.m).calcTerminalLabelPosition(a.arrowTypeEnd ? 10 : 0, "end_left", r);
            n = s.x, e = s.y;
        }
        c.attr("transform", `translate(${n}, ${e})`);
    }
    if (a.endLabelRight) {
        let c = m[a.id].endRight, n = a.x, e = a.y;
        if (r) {
            let s = (0, _chunkAC3VT7B7Mjs.m).calcTerminalLabelPosition(a.arrowTypeEnd ? 10 : 0, "end_right", r);
            n = s.x, e = s.y;
        }
        c.attr("transform", `translate(${n}, ${e})`);
    }
}, "positionEdgeLabel"), D = (0, _chunkGTKDMUJJMjs.a)((a, t)=>{
    let r = a.x, p = a.y, o = Math.abs(t.x - r), c = Math.abs(t.y - p), n = a.width / 2, e = a.height / 2;
    return o >= n || c >= e;
}, "outsideNode"), K = (0, _chunkGTKDMUJJMjs.a)((a, t, r)=>{
    (0, _chunkNQURTBEVMjs.b).debug(`intersection calc abc89:
  outsidePoint: ${JSON.stringify(t)}
  insidePoint : ${JSON.stringify(r)}
  node        : x:${a.x} y:${a.y} w:${a.width} h:${a.height}`);
    let p = a.x, o = a.y, c = Math.abs(p - r.x), n = a.width / 2, e = r.x < t.x ? n - c : n + c, s = a.height / 2, i = Math.abs(t.y - r.y), l = Math.abs(t.x - r.x);
    if (Math.abs(o - t.y) * n > Math.abs(p - t.x) * s) {
        let f = r.y < t.y ? t.y - s - o : o - s - t.y;
        e = l * f / i;
        let h = {
            x: r.x < t.x ? r.x + e : r.x - l + e,
            y: r.y < t.y ? r.y + i - f : r.y - i + f
        };
        return e === 0 && (h.x = t.x, h.y = t.y), l === 0 && (h.x = t.x), i === 0 && (h.y = t.y), (0, _chunkNQURTBEVMjs.b).debug(`abc89 topp/bott calc, Q ${i}, q ${f}, R ${l}, r ${e}`, h), h;
    } else {
        r.x < t.x ? e = t.x - n - p : e = p - n - t.x;
        let f = i * e / l, h = r.x < t.x ? r.x + l - e : r.x - l + e, x = r.y < t.y ? r.y + f : r.y - f;
        return (0, _chunkNQURTBEVMjs.b).debug(`sides calc abc89, Q ${i}, q ${f}, R ${l}, r ${e}`, {
            _x: h,
            _y: x
        }), e === 0 && (h = t.x, x = t.y), l === 0 && (h = t.x), i === 0 && (x = t.y), {
            x: h,
            y: x
        };
    }
}, "intersection"), R = (0, _chunkGTKDMUJJMjs.a)((a, t)=>{
    (0, _chunkNQURTBEVMjs.b).debug("abc88 cutPathAtIntersect", a, t);
    let r = [], p = a[0], o = !1;
    return a.forEach((c)=>{
        if (!D(t, c) && !o) {
            let n = K(t, p, c), e = !1;
            r.forEach((s)=>{
                e = e || s.x === n.x && s.y === n.y;
            }), r.some((s)=>s.x === n.x && s.y === n.y) || r.push(n), o = !0;
        } else p = c, o || r.push(c);
    }), r;
}, "cutPathAtIntersect"), Lt = (0, _chunkGTKDMUJJMjs.a)(function(a, t, r, p, o, c, n) {
    let e = r.points;
    (0, _chunkNQURTBEVMjs.b).debug("abc88 InsertEdge: edge=", r, "e=", t);
    let s = !1, i = c.node(t.v);
    var l = c.node(t.w);
    l?.intersect && i?.intersect && (e = e.slice(1, r.points.length - 1), e.unshift(i.intersect(e[0])), e.push(l.intersect(e[e.length - 1]))), r.toCluster && ((0, _chunkNQURTBEVMjs.b).debug("to cluster abc88", p[r.toCluster]), e = R(r.points, p[r.toCluster].node), s = !0), r.fromCluster && ((0, _chunkNQURTBEVMjs.b).debug("from cluster abc88", p[r.fromCluster]), e = R(e.reverse(), p[r.fromCluster].node).reverse(), s = !0);
    let f = e.filter((O)=>!Number.isNaN(O.y)), h = (0, _chunkNQURTBEVMjs.Ga);
    r.curve && (o === "graph" || o === "flowchart") && (h = r.curve);
    let { x, y: X } = (0, _chunkU6LOUQAFMjs.b)(r), Y = (0, _chunkNQURTBEVMjs.Ca)().x(x).y(X).curve(h), b;
    switch(r.thickness){
        case "normal":
            b = "edge-thickness-normal";
            break;
        case "thick":
            b = "edge-thickness-thick";
            break;
        case "invisible":
            b = "edge-thickness-thick";
            break;
        default:
            b = "";
    }
    switch(r.pattern){
        case "solid":
            b += " edge-pattern-solid";
            break;
        case "dotted":
            b += " edge-pattern-dotted";
            break;
        case "dashed":
            b += " edge-pattern-dashed";
            break;
    }
    let U = a.append("path").attr("d", Y(f)).attr("id", r.id).attr("class", " " + b + (r.classes ? " " + r.classes : "")).attr("style", r.style), y = "";
    ((0, _chunkNQURTBEVMjs.X)().flowchart.arrowMarkerAbsolute || (0, _chunkNQURTBEVMjs.X)().state.arrowMarkerAbsolute) && (y = window.location.protocol + "//" + window.location.host + window.location.pathname + window.location.search, y = y.replace(/\(/g, "\\("), y = y.replace(/\)/g, "\\)")), H(U, r, y, n, o);
    let E = {};
    return s && (E.updatedPath = e), E.originalPath = r.points, E;
}, "insertEdge");

},{"./chunk-UWHJNN4Q.mjs":"6LAlC","./chunk-U6LOUQAF.mjs":"v9pSW","./chunk-KMOJB3TB.mjs":"aJH4M","./chunk-AC3VT7B7.mjs":"1eiUz","./chunk-NQURTBEV.mjs":"iASFe","./chunk-GTKDMUJJ.mjs":"fruhx","@parcel/transformer-js/src/esmodule-helpers.js":"1cdyk"}],"fUQIF":[function(require,module,exports,__globalThis) {
var parcelHelpers = require("@parcel/transformer-js/src/esmodule-helpers.js");
parcelHelpers.defineInteropFlag(exports);
parcelHelpers.export(exports, "a", ()=>b);
var _chunkBKDDFIKNMjs = require("./chunk-BKDDFIKN.mjs");
var _chunkYPUTD6PBMjs = require("./chunk-YPUTD6PB.mjs");
var _chunk6BY5RJGCMjs = require("./chunk-6BY5RJGC.mjs");
var _chunkGTKDMUJJMjs = require("./chunk-GTKDMUJJ.mjs");
var j = "\0", f = "\0", D = "", b = class {
    static #_ = (0, _chunkGTKDMUJJMjs.a)(this, "Graph");
    constructor(e = {}){
        this._isDirected = (0, _chunkBKDDFIKNMjs.x)(e, "directed") ? e.directed : !0, this._isMultigraph = (0, _chunkBKDDFIKNMjs.x)(e, "multigraph") ? e.multigraph : !1, this._isCompound = (0, _chunkBKDDFIKNMjs.x)(e, "compound") ? e.compound : !1, this._label = void 0, this._defaultNodeLabelFn = (0, _chunk6BY5RJGCMjs.O)(void 0), this._defaultEdgeLabelFn = (0, _chunk6BY5RJGCMjs.O)(void 0), this._nodes = {}, this._isCompound && (this._parent = {}, this._children = {}, this._children[f] = {}), this._in = {}, this._preds = {}, this._out = {}, this._sucs = {}, this._edgeObjs = {}, this._edgeLabels = {};
    }
    isDirected() {
        return this._isDirected;
    }
    isMultigraph() {
        return this._isMultigraph;
    }
    isCompound() {
        return this._isCompound;
    }
    setGraph(e) {
        return this._label = e, this;
    }
    graph() {
        return this._label;
    }
    setDefaultNodeLabel(e) {
        return (0, _chunk6BY5RJGCMjs.e)(e) || (e = (0, _chunk6BY5RJGCMjs.O)(e)), this._defaultNodeLabelFn = e, this;
    }
    nodeCount() {
        return this._nodeCount;
    }
    nodes() {
        return (0, _chunkBKDDFIKNMjs.b)(this._nodes);
    }
    sources() {
        var e = this;
        return (0, _chunkBKDDFIKNMjs.p)(this.nodes(), function(t) {
            return (0, _chunkYPUTD6PBMjs.d)(e._in[t]);
        });
    }
    sinks() {
        var e = this;
        return (0, _chunkBKDDFIKNMjs.p)(this.nodes(), function(t) {
            return (0, _chunkYPUTD6PBMjs.d)(e._out[t]);
        });
    }
    setNodes(e, t) {
        var s = arguments, i = this;
        return (0, _chunkBKDDFIKNMjs.n)(e, function(r) {
            s.length > 1 ? i.setNode(r, t) : i.setNode(r);
        }), this;
    }
    setNode(e, t) {
        return (0, _chunkBKDDFIKNMjs.x)(this._nodes, e) ? (arguments.length > 1 && (this._nodes[e] = t), this) : (this._nodes[e] = arguments.length > 1 ? t : this._defaultNodeLabelFn(e), this._isCompound && (this._parent[e] = f, this._children[e] = {}, this._children[f][e] = !0), this._in[e] = {}, this._preds[e] = {}, this._out[e] = {}, this._sucs[e] = {}, ++this._nodeCount, this);
    }
    node(e) {
        return this._nodes[e];
    }
    hasNode(e) {
        return (0, _chunkBKDDFIKNMjs.x)(this._nodes, e);
    }
    removeNode(e) {
        var t = this;
        if ((0, _chunkBKDDFIKNMjs.x)(this._nodes, e)) {
            var s = (0, _chunkGTKDMUJJMjs.a)(function(i) {
                t.removeEdge(t._edgeObjs[i]);
            }, "removeEdge");
            delete this._nodes[e], this._isCompound && (this._removeFromParentsChildList(e), delete this._parent[e], (0, _chunkBKDDFIKNMjs.n)(this.children(e), function(i) {
                t.setParent(i);
            }), delete this._children[e]), (0, _chunkBKDDFIKNMjs.n)((0, _chunkBKDDFIKNMjs.b)(this._in[e]), s), delete this._in[e], delete this._preds[e], (0, _chunkBKDDFIKNMjs.n)((0, _chunkBKDDFIKNMjs.b)(this._out[e]), s), delete this._out[e], delete this._sucs[e], --this._nodeCount;
        }
        return this;
    }
    setParent(e, t) {
        if (!this._isCompound) throw new Error("Cannot set parent in a non-compound graph");
        if ((0, _chunkBKDDFIKNMjs.D)(t)) t = f;
        else {
            t += "";
            for(var s = t; !(0, _chunkBKDDFIKNMjs.D)(s); s = this.parent(s))if (s === e) throw new Error("Setting " + t + " as parent of " + e + " would create a cycle");
            this.setNode(t);
        }
        return this.setNode(e), this._removeFromParentsChildList(e), this._parent[e] = t, this._children[t][e] = !0, this;
    }
    _removeFromParentsChildList(e) {
        delete this._children[this._parent[e]][e];
    }
    parent(e) {
        if (this._isCompound) {
            var t = this._parent[e];
            if (t !== f) return t;
        }
    }
    children(e) {
        if ((0, _chunkBKDDFIKNMjs.D)(e) && (e = f), this._isCompound) {
            var t = this._children[e];
            if (t) return (0, _chunkBKDDFIKNMjs.b)(t);
        } else {
            if (e === f) return this.nodes();
            if (this.hasNode(e)) return [];
        }
    }
    predecessors(e) {
        var t = this._preds[e];
        if (t) return (0, _chunkBKDDFIKNMjs.b)(t);
    }
    successors(e) {
        var t = this._sucs[e];
        if (t) return (0, _chunkBKDDFIKNMjs.b)(t);
    }
    neighbors(e) {
        var t = this.predecessors(e);
        if (t) return (0, _chunkBKDDFIKNMjs.Q)(t, this.successors(e));
    }
    isLeaf(e) {
        var t;
        return this.isDirected() ? t = this.successors(e) : t = this.neighbors(e), t.length === 0;
    }
    filterNodes(e) {
        var t = new this.constructor({
            directed: this._isDirected,
            multigraph: this._isMultigraph,
            compound: this._isCompound
        });
        t.setGraph(this.graph());
        var s = this;
        (0, _chunkBKDDFIKNMjs.n)(this._nodes, function(n, h) {
            e(h) && t.setNode(h, n);
        }), (0, _chunkBKDDFIKNMjs.n)(this._edgeObjs, function(n) {
            t.hasNode(n.v) && t.hasNode(n.w) && t.setEdge(n, s.edge(n));
        });
        var i = {};
        function r(n) {
            var h = s.parent(n);
            return h === void 0 || t.hasNode(h) ? (i[n] = h, h) : h in i ? i[h] : r(h);
        }
        return (0, _chunkGTKDMUJJMjs.a)(r, "findParent"), this._isCompound && (0, _chunkBKDDFIKNMjs.n)(t.nodes(), function(n) {
            t.setParent(n, r(n));
        }), t;
    }
    setDefaultEdgeLabel(e) {
        return (0, _chunk6BY5RJGCMjs.e)(e) || (e = (0, _chunk6BY5RJGCMjs.O)(e)), this._defaultEdgeLabelFn = e, this;
    }
    edgeCount() {
        return this._edgeCount;
    }
    edges() {
        return (0, _chunkBKDDFIKNMjs.z)(this._edgeObjs);
    }
    setPath(e, t) {
        var s = this, i = arguments;
        return (0, _chunkBKDDFIKNMjs.L)(e, function(r, n) {
            return i.length > 1 ? s.setEdge(r, n, t) : s.setEdge(r, n), n;
        }), this;
    }
    setEdge() {
        var e, t, s, i, r = !1, n = arguments[0];
        typeof n == "object" && n !== null && "v" in n ? (e = n.v, t = n.w, s = n.name, arguments.length === 2 && (i = arguments[1], r = !0)) : (e = n, t = arguments[1], s = arguments[3], arguments.length > 2 && (i = arguments[2], r = !0)), e = "" + e, t = "" + t, (0, _chunkBKDDFIKNMjs.D)(s) || (s = "" + s);
        var h = p(this._isDirected, e, t, s);
        if ((0, _chunkBKDDFIKNMjs.x)(this._edgeLabels, h)) return r && (this._edgeLabels[h] = i), this;
        if (!(0, _chunkBKDDFIKNMjs.D)(s) && !this._isMultigraph) throw new Error("Cannot set a named edge when isMultigraph = false");
        this.setNode(e), this.setNode(t), this._edgeLabels[h] = r ? i : this._defaultEdgeLabelFn(e, t, s);
        var c = P(this._isDirected, e, t, s);
        return e = c.v, t = c.w, Object.freeze(c), this._edgeObjs[h] = c, O(this._preds[t], e), O(this._sucs[e], t), this._in[t][h] = c, this._out[e][h] = c, this._edgeCount++, this;
    }
    edge(e, t, s) {
        var i = arguments.length === 1 ? N(this._isDirected, arguments[0]) : p(this._isDirected, e, t, s);
        return this._edgeLabels[i];
    }
    hasEdge(e, t, s) {
        var i = arguments.length === 1 ? N(this._isDirected, arguments[0]) : p(this._isDirected, e, t, s);
        return (0, _chunkBKDDFIKNMjs.x)(this._edgeLabels, i);
    }
    removeEdge(e, t, s) {
        var i = arguments.length === 1 ? N(this._isDirected, arguments[0]) : p(this._isDirected, e, t, s), r = this._edgeObjs[i];
        return r && (e = r.v, t = r.w, delete this._edgeLabels[i], delete this._edgeObjs[i], F(this._preds[t], e), F(this._sucs[e], t), delete this._in[t][i], delete this._out[e][i], this._edgeCount--), this;
    }
    inEdges(e, t) {
        var s = this._in[e];
        if (s) {
            var i = (0, _chunkBKDDFIKNMjs.z)(s);
            return t ? (0, _chunkBKDDFIKNMjs.p)(i, function(r) {
                return r.v === t;
            }) : i;
        }
    }
    outEdges(e, t) {
        var s = this._out[e];
        if (s) {
            var i = (0, _chunkBKDDFIKNMjs.z)(s);
            return t ? (0, _chunkBKDDFIKNMjs.p)(i, function(r) {
                return r.w === t;
            }) : i;
        }
    }
    nodeEdges(e, t) {
        var s = this.inEdges(e, t);
        if (s) return s.concat(this.outEdges(e, t));
    }
};
b.prototype._nodeCount = 0;
b.prototype._edgeCount = 0;
function O(d, e) {
    d[e] ? d[e]++ : d[e] = 1;
}
(0, _chunkGTKDMUJJMjs.a)(O, "incrementOrInitEntry");
function F(d, e) {
    --d[e] || delete d[e];
}
(0, _chunkGTKDMUJJMjs.a)(F, "decrementOrRemoveEntry");
function p(d, e, t, s) {
    var i = "" + e, r = "" + t;
    if (!d && i > r) {
        var n = i;
        i = r, r = n;
    }
    return i + D + r + D + ((0, _chunkBKDDFIKNMjs.D)(s) ? j : s);
}
(0, _chunkGTKDMUJJMjs.a)(p, "edgeArgsToId");
function P(d, e, t, s) {
    var i = "" + e, r = "" + t;
    if (!d && i > r) {
        var n = i;
        i = r, r = n;
    }
    var h = {
        v: i,
        w: r
    };
    return s && (h.name = s), h;
}
(0, _chunkGTKDMUJJMjs.a)(P, "edgeArgsToObj");
function N(d, e) {
    return p(d, e.v, e.w, e.name);
}
(0, _chunkGTKDMUJJMjs.a)(N, "edgeObjToId");

},{"./chunk-BKDDFIKN.mjs":"hADfH","./chunk-YPUTD6PB.mjs":"aSdv1","./chunk-6BY5RJGC.mjs":"bRXnR","./chunk-GTKDMUJJ.mjs":"fruhx","@parcel/transformer-js/src/esmodule-helpers.js":"1cdyk"}]},["56RUd"], null, "parcelRequire6955", {})

//# sourceMappingURL=blockDiagram-NDWNTGEE.7c18ec1a.js.map
