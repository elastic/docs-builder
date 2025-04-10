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
})({"juLtk":[function(require,module,exports,__globalThis) {
var global = arguments[3];
var HMR_HOST = null;
var HMR_PORT = 1234;
var HMR_SERVER_PORT = 1234;
var HMR_SECURE = false;
var HMR_ENV_HASH = "febfef3c5b467b16";
var HMR_USE_SSE = false;
module.bundle.HMR_BUNDLE_ID = "98a4dbe394fe84f5";
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

},{}],"h1C6b":[function(require,module,exports,__globalThis) {
var parcelHelpers = require("@parcel/transformer-js/src/esmodule-helpers.js");
parcelHelpers.defineInteropFlag(exports);
parcelHelpers.export(exports, "diagram", ()=>ha);
var _chunkNQURTBEVMjs = require("./chunk-NQURTBEV.mjs");
var _chunkGTKDMUJJMjs = require("./chunk-GTKDMUJJ.mjs");
var Et = function() {
    var t = (0, _chunkGTKDMUJJMjs.a)(function(X, o, l, x) {
        for(l = l || {}, x = X.length; x--; l[X[x]] = o);
        return l;
    }, "o"), n = [
        1,
        3
    ], f = [
        1,
        4
    ], u = [
        1,
        5
    ], c = [
        1,
        6
    ], g = [
        1,
        7
    ], y = [
        1,
        4,
        5,
        10,
        12,
        13,
        14,
        18,
        25,
        35,
        37,
        39,
        41,
        42,
        48,
        50,
        51,
        52,
        53,
        54,
        55,
        56,
        57,
        60,
        61,
        63,
        64,
        65,
        66,
        67
    ], S = [
        1,
        4,
        5,
        10,
        12,
        13,
        14,
        18,
        25,
        28,
        35,
        37,
        39,
        41,
        42,
        48,
        50,
        51,
        52,
        53,
        54,
        55,
        56,
        57,
        60,
        61,
        63,
        64,
        65,
        66,
        67
    ], i = [
        55,
        56,
        57
    ], A = [
        2,
        36
    ], h = [
        1,
        37
    ], T = [
        1,
        36
    ], m = [
        1,
        38
    ], b = [
        1,
        35
    ], q = [
        1,
        43
    ], p = [
        1,
        41
    ], K = [
        1,
        14
    ], dt = [
        1,
        23
    ], ft = [
        1,
        18
    ], pt = [
        1,
        19
    ], gt = [
        1,
        20
    ], ut = [
        1,
        21
    ], kt = [
        1,
        22
    ], ct = [
        1,
        24
    ], a = [
        1,
        25
    ], Bt = [
        1,
        26
    ], wt = [
        1,
        27
    ], It = [
        1,
        28
    ], Ot = [
        1,
        29
    ], W = [
        1,
        32
    ], N = [
        1,
        33
    ], P = [
        1,
        34
    ], _ = [
        1,
        39
    ], F = [
        1,
        40
    ], Q = [
        1,
        42
    ], C = [
        1,
        44
    ], R = [
        1,
        62
    ], H = [
        1,
        61
    ], v = [
        4,
        5,
        8,
        10,
        12,
        13,
        14,
        18,
        44,
        47,
        49,
        55,
        56,
        57,
        63,
        64,
        65,
        66,
        67
    ], Wt = [
        1,
        65
    ], Nt = [
        1,
        66
    ], Rt = [
        1,
        67
    ], Ht = [
        1,
        68
    ], Ut = [
        1,
        69
    ], jt = [
        1,
        70
    ], Xt = [
        1,
        71
    ], Mt = [
        1,
        72
    ], Yt = [
        1,
        73
    ], Gt = [
        1,
        74
    ], Kt = [
        1,
        75
    ], Zt = [
        1,
        76
    ], B = [
        4,
        5,
        6,
        7,
        8,
        9,
        10,
        11,
        12,
        13,
        14,
        15,
        18
    ], Z = [
        1,
        90
    ], J = [
        1,
        91
    ], $ = [
        1,
        92
    ], tt = [
        1,
        99
    ], et = [
        1,
        93
    ], at = [
        1,
        96
    ], it = [
        1,
        94
    ], nt = [
        1,
        95
    ], rt = [
        1,
        97
    ], st = [
        1,
        98
    ], St = [
        1,
        102
    ], Jt = [
        10,
        55,
        56,
        57
    ], I = [
        4,
        5,
        6,
        8,
        10,
        11,
        13,
        17,
        18,
        19,
        20,
        55,
        56,
        57
    ], At = {
        trace: (0, _chunkGTKDMUJJMjs.a)(function() {}, "trace"),
        yy: {},
        symbols_: {
            error: 2,
            idStringToken: 3,
            ALPHA: 4,
            NUM: 5,
            NODE_STRING: 6,
            DOWN: 7,
            MINUS: 8,
            DEFAULT: 9,
            COMMA: 10,
            COLON: 11,
            AMP: 12,
            BRKT: 13,
            MULT: 14,
            UNICODE_TEXT: 15,
            styleComponent: 16,
            UNIT: 17,
            SPACE: 18,
            STYLE: 19,
            PCT: 20,
            idString: 21,
            style: 22,
            stylesOpt: 23,
            classDefStatement: 24,
            CLASSDEF: 25,
            start: 26,
            eol: 27,
            QUADRANT: 28,
            document: 29,
            line: 30,
            statement: 31,
            axisDetails: 32,
            quadrantDetails: 33,
            points: 34,
            title: 35,
            title_value: 36,
            acc_title: 37,
            acc_title_value: 38,
            acc_descr: 39,
            acc_descr_value: 40,
            acc_descr_multiline_value: 41,
            section: 42,
            text: 43,
            point_start: 44,
            point_x: 45,
            point_y: 46,
            class_name: 47,
            "X-AXIS": 48,
            "AXIS-TEXT-DELIMITER": 49,
            "Y-AXIS": 50,
            QUADRANT_1: 51,
            QUADRANT_2: 52,
            QUADRANT_3: 53,
            QUADRANT_4: 54,
            NEWLINE: 55,
            SEMI: 56,
            EOF: 57,
            alphaNumToken: 58,
            textNoTagsToken: 59,
            STR: 60,
            MD_STR: 61,
            alphaNum: 62,
            PUNCTUATION: 63,
            PLUS: 64,
            EQUALS: 65,
            DOT: 66,
            UNDERSCORE: 67,
            $accept: 0,
            $end: 1
        },
        terminals_: {
            2: "error",
            4: "ALPHA",
            5: "NUM",
            6: "NODE_STRING",
            7: "DOWN",
            8: "MINUS",
            9: "DEFAULT",
            10: "COMMA",
            11: "COLON",
            12: "AMP",
            13: "BRKT",
            14: "MULT",
            15: "UNICODE_TEXT",
            17: "UNIT",
            18: "SPACE",
            19: "STYLE",
            20: "PCT",
            25: "CLASSDEF",
            28: "QUADRANT",
            35: "title",
            36: "title_value",
            37: "acc_title",
            38: "acc_title_value",
            39: "acc_descr",
            40: "acc_descr_value",
            41: "acc_descr_multiline_value",
            42: "section",
            44: "point_start",
            45: "point_x",
            46: "point_y",
            47: "class_name",
            48: "X-AXIS",
            49: "AXIS-TEXT-DELIMITER",
            50: "Y-AXIS",
            51: "QUADRANT_1",
            52: "QUADRANT_2",
            53: "QUADRANT_3",
            54: "QUADRANT_4",
            55: "NEWLINE",
            56: "SEMI",
            57: "EOF",
            60: "STR",
            61: "MD_STR",
            63: "PUNCTUATION",
            64: "PLUS",
            65: "EQUALS",
            66: "DOT",
            67: "UNDERSCORE"
        },
        productions_: [
            0,
            [
                3,
                1
            ],
            [
                3,
                1
            ],
            [
                3,
                1
            ],
            [
                3,
                1
            ],
            [
                3,
                1
            ],
            [
                3,
                1
            ],
            [
                3,
                1
            ],
            [
                3,
                1
            ],
            [
                3,
                1
            ],
            [
                3,
                1
            ],
            [
                3,
                1
            ],
            [
                3,
                1
            ],
            [
                16,
                1
            ],
            [
                16,
                1
            ],
            [
                16,
                1
            ],
            [
                16,
                1
            ],
            [
                16,
                1
            ],
            [
                16,
                1
            ],
            [
                16,
                1
            ],
            [
                16,
                1
            ],
            [
                16,
                1
            ],
            [
                16,
                1
            ],
            [
                21,
                1
            ],
            [
                21,
                2
            ],
            [
                22,
                1
            ],
            [
                22,
                2
            ],
            [
                23,
                1
            ],
            [
                23,
                3
            ],
            [
                24,
                5
            ],
            [
                26,
                2
            ],
            [
                26,
                2
            ],
            [
                26,
                2
            ],
            [
                29,
                0
            ],
            [
                29,
                2
            ],
            [
                30,
                2
            ],
            [
                31,
                0
            ],
            [
                31,
                1
            ],
            [
                31,
                2
            ],
            [
                31,
                1
            ],
            [
                31,
                1
            ],
            [
                31,
                1
            ],
            [
                31,
                2
            ],
            [
                31,
                2
            ],
            [
                31,
                2
            ],
            [
                31,
                1
            ],
            [
                31,
                1
            ],
            [
                34,
                4
            ],
            [
                34,
                5
            ],
            [
                34,
                5
            ],
            [
                34,
                6
            ],
            [
                32,
                4
            ],
            [
                32,
                3
            ],
            [
                32,
                2
            ],
            [
                32,
                4
            ],
            [
                32,
                3
            ],
            [
                32,
                2
            ],
            [
                33,
                2
            ],
            [
                33,
                2
            ],
            [
                33,
                2
            ],
            [
                33,
                2
            ],
            [
                27,
                1
            ],
            [
                27,
                1
            ],
            [
                27,
                1
            ],
            [
                43,
                1
            ],
            [
                43,
                2
            ],
            [
                43,
                1
            ],
            [
                43,
                1
            ],
            [
                62,
                1
            ],
            [
                62,
                2
            ],
            [
                58,
                1
            ],
            [
                58,
                1
            ],
            [
                58,
                1
            ],
            [
                58,
                1
            ],
            [
                58,
                1
            ],
            [
                58,
                1
            ],
            [
                58,
                1
            ],
            [
                58,
                1
            ],
            [
                58,
                1
            ],
            [
                58,
                1
            ],
            [
                58,
                1
            ],
            [
                59,
                1
            ],
            [
                59,
                1
            ],
            [
                59,
                1
            ]
        ],
        performAction: (0, _chunkGTKDMUJJMjs.a)(function(o, l, x, d, k, e, ht) {
            var s = e.length - 1;
            switch(k){
                case 23:
                    this.$ = e[s];
                    break;
                case 24:
                    this.$ = e[s - 1] + "" + e[s];
                    break;
                case 26:
                    this.$ = e[s - 1] + e[s];
                    break;
                case 27:
                    this.$ = [
                        e[s].trim()
                    ];
                    break;
                case 28:
                    e[s - 2].push(e[s].trim()), this.$ = e[s - 2];
                    break;
                case 29:
                    this.$ = e[s - 4], d.addClass(e[s - 2], e[s]);
                    break;
                case 37:
                    this.$ = [];
                    break;
                case 42:
                    this.$ = e[s].trim(), d.setDiagramTitle(this.$);
                    break;
                case 43:
                    this.$ = e[s].trim(), d.setAccTitle(this.$);
                    break;
                case 44:
                case 45:
                    this.$ = e[s].trim(), d.setAccDescription(this.$);
                    break;
                case 46:
                    d.addSection(e[s].substr(8)), this.$ = e[s].substr(8);
                    break;
                case 47:
                    d.addPoint(e[s - 3], "", e[s - 1], e[s], []);
                    break;
                case 48:
                    d.addPoint(e[s - 4], e[s - 3], e[s - 1], e[s], []);
                    break;
                case 49:
                    d.addPoint(e[s - 4], "", e[s - 2], e[s - 1], e[s]);
                    break;
                case 50:
                    d.addPoint(e[s - 5], e[s - 4], e[s - 2], e[s - 1], e[s]);
                    break;
                case 51:
                    d.setXAxisLeftText(e[s - 2]), d.setXAxisRightText(e[s]);
                    break;
                case 52:
                    e[s - 1].text += " \u27F6 ", d.setXAxisLeftText(e[s - 1]);
                    break;
                case 53:
                    d.setXAxisLeftText(e[s]);
                    break;
                case 54:
                    d.setYAxisBottomText(e[s - 2]), d.setYAxisTopText(e[s]);
                    break;
                case 55:
                    e[s - 1].text += " \u27F6 ", d.setYAxisBottomText(e[s - 1]);
                    break;
                case 56:
                    d.setYAxisBottomText(e[s]);
                    break;
                case 57:
                    d.setQuadrant1Text(e[s]);
                    break;
                case 58:
                    d.setQuadrant2Text(e[s]);
                    break;
                case 59:
                    d.setQuadrant3Text(e[s]);
                    break;
                case 60:
                    d.setQuadrant4Text(e[s]);
                    break;
                case 64:
                    this.$ = {
                        text: e[s],
                        type: "text"
                    };
                    break;
                case 65:
                    this.$ = {
                        text: e[s - 1].text + "" + e[s],
                        type: e[s - 1].type
                    };
                    break;
                case 66:
                    this.$ = {
                        text: e[s],
                        type: "text"
                    };
                    break;
                case 67:
                    this.$ = {
                        text: e[s],
                        type: "markdown"
                    };
                    break;
                case 68:
                    this.$ = e[s];
                    break;
                case 69:
                    this.$ = e[s - 1] + "" + e[s];
                    break;
            }
        }, "anonymous"),
        table: [
            {
                18: n,
                26: 1,
                27: 2,
                28: f,
                55: u,
                56: c,
                57: g
            },
            {
                1: [
                    3
                ]
            },
            {
                18: n,
                26: 8,
                27: 2,
                28: f,
                55: u,
                56: c,
                57: g
            },
            {
                18: n,
                26: 9,
                27: 2,
                28: f,
                55: u,
                56: c,
                57: g
            },
            t(y, [
                2,
                33
            ], {
                29: 10
            }),
            t(S, [
                2,
                61
            ]),
            t(S, [
                2,
                62
            ]),
            t(S, [
                2,
                63
            ]),
            {
                1: [
                    2,
                    30
                ]
            },
            {
                1: [
                    2,
                    31
                ]
            },
            t(i, A, {
                30: 11,
                31: 12,
                24: 13,
                32: 15,
                33: 16,
                34: 17,
                43: 30,
                58: 31,
                1: [
                    2,
                    32
                ],
                4: h,
                5: T,
                10: m,
                12: b,
                13: q,
                14: p,
                18: K,
                25: dt,
                35: ft,
                37: pt,
                39: gt,
                41: ut,
                42: kt,
                48: ct,
                50: a,
                51: Bt,
                52: wt,
                53: It,
                54: Ot,
                60: W,
                61: N,
                63: P,
                64: _,
                65: F,
                66: Q,
                67: C
            }),
            t(y, [
                2,
                34
            ]),
            {
                27: 45,
                55: u,
                56: c,
                57: g
            },
            t(i, [
                2,
                37
            ]),
            t(i, A, {
                24: 13,
                32: 15,
                33: 16,
                34: 17,
                43: 30,
                58: 31,
                31: 46,
                4: h,
                5: T,
                10: m,
                12: b,
                13: q,
                14: p,
                18: K,
                25: dt,
                35: ft,
                37: pt,
                39: gt,
                41: ut,
                42: kt,
                48: ct,
                50: a,
                51: Bt,
                52: wt,
                53: It,
                54: Ot,
                60: W,
                61: N,
                63: P,
                64: _,
                65: F,
                66: Q,
                67: C
            }),
            t(i, [
                2,
                39
            ]),
            t(i, [
                2,
                40
            ]),
            t(i, [
                2,
                41
            ]),
            {
                36: [
                    1,
                    47
                ]
            },
            {
                38: [
                    1,
                    48
                ]
            },
            {
                40: [
                    1,
                    49
                ]
            },
            t(i, [
                2,
                45
            ]),
            t(i, [
                2,
                46
            ]),
            {
                18: [
                    1,
                    50
                ]
            },
            {
                4: h,
                5: T,
                10: m,
                12: b,
                13: q,
                14: p,
                43: 51,
                58: 31,
                60: W,
                61: N,
                63: P,
                64: _,
                65: F,
                66: Q,
                67: C
            },
            {
                4: h,
                5: T,
                10: m,
                12: b,
                13: q,
                14: p,
                43: 52,
                58: 31,
                60: W,
                61: N,
                63: P,
                64: _,
                65: F,
                66: Q,
                67: C
            },
            {
                4: h,
                5: T,
                10: m,
                12: b,
                13: q,
                14: p,
                43: 53,
                58: 31,
                60: W,
                61: N,
                63: P,
                64: _,
                65: F,
                66: Q,
                67: C
            },
            {
                4: h,
                5: T,
                10: m,
                12: b,
                13: q,
                14: p,
                43: 54,
                58: 31,
                60: W,
                61: N,
                63: P,
                64: _,
                65: F,
                66: Q,
                67: C
            },
            {
                4: h,
                5: T,
                10: m,
                12: b,
                13: q,
                14: p,
                43: 55,
                58: 31,
                60: W,
                61: N,
                63: P,
                64: _,
                65: F,
                66: Q,
                67: C
            },
            {
                4: h,
                5: T,
                10: m,
                12: b,
                13: q,
                14: p,
                43: 56,
                58: 31,
                60: W,
                61: N,
                63: P,
                64: _,
                65: F,
                66: Q,
                67: C
            },
            {
                4: h,
                5: T,
                8: R,
                10: m,
                12: b,
                13: q,
                14: p,
                18: H,
                44: [
                    1,
                    57
                ],
                47: [
                    1,
                    58
                ],
                58: 60,
                59: 59,
                63: P,
                64: _,
                65: F,
                66: Q,
                67: C
            },
            t(v, [
                2,
                64
            ]),
            t(v, [
                2,
                66
            ]),
            t(v, [
                2,
                67
            ]),
            t(v, [
                2,
                70
            ]),
            t(v, [
                2,
                71
            ]),
            t(v, [
                2,
                72
            ]),
            t(v, [
                2,
                73
            ]),
            t(v, [
                2,
                74
            ]),
            t(v, [
                2,
                75
            ]),
            t(v, [
                2,
                76
            ]),
            t(v, [
                2,
                77
            ]),
            t(v, [
                2,
                78
            ]),
            t(v, [
                2,
                79
            ]),
            t(v, [
                2,
                80
            ]),
            t(y, [
                2,
                35
            ]),
            t(i, [
                2,
                38
            ]),
            t(i, [
                2,
                42
            ]),
            t(i, [
                2,
                43
            ]),
            t(i, [
                2,
                44
            ]),
            {
                3: 64,
                4: Wt,
                5: Nt,
                6: Rt,
                7: Ht,
                8: Ut,
                9: jt,
                10: Xt,
                11: Mt,
                12: Yt,
                13: Gt,
                14: Kt,
                15: Zt,
                21: 63
            },
            t(i, [
                2,
                53
            ], {
                59: 59,
                58: 60,
                4: h,
                5: T,
                8: R,
                10: m,
                12: b,
                13: q,
                14: p,
                18: H,
                49: [
                    1,
                    77
                ],
                63: P,
                64: _,
                65: F,
                66: Q,
                67: C
            }),
            t(i, [
                2,
                56
            ], {
                59: 59,
                58: 60,
                4: h,
                5: T,
                8: R,
                10: m,
                12: b,
                13: q,
                14: p,
                18: H,
                49: [
                    1,
                    78
                ],
                63: P,
                64: _,
                65: F,
                66: Q,
                67: C
            }),
            t(i, [
                2,
                57
            ], {
                59: 59,
                58: 60,
                4: h,
                5: T,
                8: R,
                10: m,
                12: b,
                13: q,
                14: p,
                18: H,
                63: P,
                64: _,
                65: F,
                66: Q,
                67: C
            }),
            t(i, [
                2,
                58
            ], {
                59: 59,
                58: 60,
                4: h,
                5: T,
                8: R,
                10: m,
                12: b,
                13: q,
                14: p,
                18: H,
                63: P,
                64: _,
                65: F,
                66: Q,
                67: C
            }),
            t(i, [
                2,
                59
            ], {
                59: 59,
                58: 60,
                4: h,
                5: T,
                8: R,
                10: m,
                12: b,
                13: q,
                14: p,
                18: H,
                63: P,
                64: _,
                65: F,
                66: Q,
                67: C
            }),
            t(i, [
                2,
                60
            ], {
                59: 59,
                58: 60,
                4: h,
                5: T,
                8: R,
                10: m,
                12: b,
                13: q,
                14: p,
                18: H,
                63: P,
                64: _,
                65: F,
                66: Q,
                67: C
            }),
            {
                45: [
                    1,
                    79
                ]
            },
            {
                44: [
                    1,
                    80
                ]
            },
            t(v, [
                2,
                65
            ]),
            t(v, [
                2,
                81
            ]),
            t(v, [
                2,
                82
            ]),
            t(v, [
                2,
                83
            ]),
            {
                3: 82,
                4: Wt,
                5: Nt,
                6: Rt,
                7: Ht,
                8: Ut,
                9: jt,
                10: Xt,
                11: Mt,
                12: Yt,
                13: Gt,
                14: Kt,
                15: Zt,
                18: [
                    1,
                    81
                ]
            },
            t(B, [
                2,
                23
            ]),
            t(B, [
                2,
                1
            ]),
            t(B, [
                2,
                2
            ]),
            t(B, [
                2,
                3
            ]),
            t(B, [
                2,
                4
            ]),
            t(B, [
                2,
                5
            ]),
            t(B, [
                2,
                6
            ]),
            t(B, [
                2,
                7
            ]),
            t(B, [
                2,
                8
            ]),
            t(B, [
                2,
                9
            ]),
            t(B, [
                2,
                10
            ]),
            t(B, [
                2,
                11
            ]),
            t(B, [
                2,
                12
            ]),
            t(i, [
                2,
                52
            ], {
                58: 31,
                43: 83,
                4: h,
                5: T,
                10: m,
                12: b,
                13: q,
                14: p,
                60: W,
                61: N,
                63: P,
                64: _,
                65: F,
                66: Q,
                67: C
            }),
            t(i, [
                2,
                55
            ], {
                58: 31,
                43: 84,
                4: h,
                5: T,
                10: m,
                12: b,
                13: q,
                14: p,
                60: W,
                61: N,
                63: P,
                64: _,
                65: F,
                66: Q,
                67: C
            }),
            {
                46: [
                    1,
                    85
                ]
            },
            {
                45: [
                    1,
                    86
                ]
            },
            {
                4: Z,
                5: J,
                6: $,
                8: tt,
                11: et,
                13: at,
                16: 89,
                17: it,
                18: nt,
                19: rt,
                20: st,
                22: 88,
                23: 87
            },
            t(B, [
                2,
                24
            ]),
            t(i, [
                2,
                51
            ], {
                59: 59,
                58: 60,
                4: h,
                5: T,
                8: R,
                10: m,
                12: b,
                13: q,
                14: p,
                18: H,
                63: P,
                64: _,
                65: F,
                66: Q,
                67: C
            }),
            t(i, [
                2,
                54
            ], {
                59: 59,
                58: 60,
                4: h,
                5: T,
                8: R,
                10: m,
                12: b,
                13: q,
                14: p,
                18: H,
                63: P,
                64: _,
                65: F,
                66: Q,
                67: C
            }),
            t(i, [
                2,
                47
            ], {
                22: 88,
                16: 89,
                23: 100,
                4: Z,
                5: J,
                6: $,
                8: tt,
                11: et,
                13: at,
                17: it,
                18: nt,
                19: rt,
                20: st
            }),
            {
                46: [
                    1,
                    101
                ]
            },
            t(i, [
                2,
                29
            ], {
                10: St
            }),
            t(Jt, [
                2,
                27
            ], {
                16: 103,
                4: Z,
                5: J,
                6: $,
                8: tt,
                11: et,
                13: at,
                17: it,
                18: nt,
                19: rt,
                20: st
            }),
            t(I, [
                2,
                25
            ]),
            t(I, [
                2,
                13
            ]),
            t(I, [
                2,
                14
            ]),
            t(I, [
                2,
                15
            ]),
            t(I, [
                2,
                16
            ]),
            t(I, [
                2,
                17
            ]),
            t(I, [
                2,
                18
            ]),
            t(I, [
                2,
                19
            ]),
            t(I, [
                2,
                20
            ]),
            t(I, [
                2,
                21
            ]),
            t(I, [
                2,
                22
            ]),
            t(i, [
                2,
                49
            ], {
                10: St
            }),
            t(i, [
                2,
                48
            ], {
                22: 88,
                16: 89,
                23: 104,
                4: Z,
                5: J,
                6: $,
                8: tt,
                11: et,
                13: at,
                17: it,
                18: nt,
                19: rt,
                20: st
            }),
            {
                4: Z,
                5: J,
                6: $,
                8: tt,
                11: et,
                13: at,
                16: 89,
                17: it,
                18: nt,
                19: rt,
                20: st,
                22: 105
            },
            t(I, [
                2,
                26
            ]),
            t(i, [
                2,
                50
            ], {
                10: St
            }),
            t(Jt, [
                2,
                28
            ], {
                16: 103,
                4: Z,
                5: J,
                6: $,
                8: tt,
                11: et,
                13: at,
                17: it,
                18: nt,
                19: rt,
                20: st
            })
        ],
        defaultActions: {
            8: [
                2,
                30
            ],
            9: [
                2,
                31
            ]
        },
        parseError: (0, _chunkGTKDMUJJMjs.a)(function(o, l) {
            if (l.recoverable) this.trace(o);
            else {
                var x = new Error(o);
                throw x.hash = l, x;
            }
        }, "parseError"),
        parse: (0, _chunkGTKDMUJJMjs.a)(function(o) {
            var l = this, x = [
                0
            ], d = [], k = [
                null
            ], e = [], ht = this.table, s = "", yt = 0, $t = 0, te = 0, Te = 2, ee = 1, me = e.slice.call(arguments, 1), D = Object.create(this.lexer), M = {
                yy: {}
            };
            for(var _t in this.yy)Object.prototype.hasOwnProperty.call(this.yy, _t) && (M.yy[_t] = this.yy[_t]);
            D.setInput(o, M.yy), M.yy.lexer = D, M.yy.parser = this, typeof D.yylloc > "u" && (D.yylloc = {});
            var Ft = D.yylloc;
            e.push(Ft);
            var be = D.options && D.options.ranges;
            typeof M.yy.parseError == "function" ? this.parseError = M.yy.parseError : this.parseError = Object.getPrototypeOf(this).parseError;
            function Ie(w) {
                x.length = x.length - 2 * w, k.length = k.length - w, e.length = e.length - w;
            }
            (0, _chunkGTKDMUJJMjs.a)(Ie, "popStack");
            function qe() {
                var w;
                return w = d.pop() || D.lex() || ee, typeof w != "number" && (w instanceof Array && (d = w, w = d.pop()), w = l.symbols_[w] || w), w;
            }
            (0, _chunkGTKDMUJJMjs.a)(qe, "lex");
            for(var z, Qt, Y, O, Oe, Ct, ot = {}, Tt, U, ae, mt;;){
                if (Y = x[x.length - 1], this.defaultActions[Y] ? O = this.defaultActions[Y] : ((z === null || typeof z > "u") && (z = qe()), O = ht[Y] && ht[Y][z]), typeof O > "u" || !O.length || !O[0]) {
                    var Lt = "";
                    mt = [];
                    for(Tt in ht[Y])this.terminals_[Tt] && Tt > Te && mt.push("'" + this.terminals_[Tt] + "'");
                    D.showPosition ? Lt = "Parse error on line " + (yt + 1) + `:
` + D.showPosition() + `
Expecting ` + mt.join(", ") + ", got '" + (this.terminals_[z] || z) + "'" : Lt = "Parse error on line " + (yt + 1) + ": Unexpected " + (z == ee ? "end of input" : "'" + (this.terminals_[z] || z) + "'"), this.parseError(Lt, {
                        text: D.match,
                        token: this.terminals_[z] || z,
                        line: D.yylineno,
                        loc: Ft,
                        expected: mt
                    });
                }
                if (O[0] instanceof Array && O.length > 1) throw new Error("Parse Error: multiple actions possible at state: " + Y + ", token: " + z);
                switch(O[0]){
                    case 1:
                        x.push(z), k.push(D.yytext), e.push(D.yylloc), x.push(O[1]), z = null, Qt ? (z = Qt, Qt = null) : ($t = D.yyleng, s = D.yytext, yt = D.yylineno, Ft = D.yylloc, te > 0 && te--);
                        break;
                    case 2:
                        if (U = this.productions_[O[1]][1], ot.$ = k[k.length - U], ot._$ = {
                            first_line: e[e.length - (U || 1)].first_line,
                            last_line: e[e.length - 1].last_line,
                            first_column: e[e.length - (U || 1)].first_column,
                            last_column: e[e.length - 1].last_column
                        }, be && (ot._$.range = [
                            e[e.length - (U || 1)].range[0],
                            e[e.length - 1].range[1]
                        ]), Ct = this.performAction.apply(ot, [
                            s,
                            $t,
                            yt,
                            M.yy,
                            O[1],
                            k,
                            e
                        ].concat(me)), typeof Ct < "u") return Ct;
                        U && (x = x.slice(0, -1 * U * 2), k = k.slice(0, -1 * U), e = e.slice(0, -1 * U)), x.push(this.productions_[O[1]][0]), k.push(ot.$), e.push(ot._$), ae = ht[x[x.length - 2]][x[x.length - 1]], x.push(ae);
                        break;
                    case 3:
                        return !0;
                }
            }
            return !0;
        }, "parse")
    }, ye = function() {
        var X = {
            EOF: 1,
            parseError: (0, _chunkGTKDMUJJMjs.a)(function(l, x) {
                if (this.yy.parser) this.yy.parser.parseError(l, x);
                else throw new Error(l);
            }, "parseError"),
            setInput: (0, _chunkGTKDMUJJMjs.a)(function(o, l) {
                return this.yy = l || this.yy || {}, this._input = o, this._more = this._backtrack = this.done = !1, this.yylineno = this.yyleng = 0, this.yytext = this.matched = this.match = "", this.conditionStack = [
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
                var o = this._input[0];
                this.yytext += o, this.yyleng++, this.offset++, this.match += o, this.matched += o;
                var l = o.match(/(?:\r\n?|\n).*/g);
                return l ? (this.yylineno++, this.yylloc.last_line++) : this.yylloc.last_column++, this.options.ranges && this.yylloc.range[1]++, this._input = this._input.slice(1), o;
            }, "input"),
            unput: (0, _chunkGTKDMUJJMjs.a)(function(o) {
                var l = o.length, x = o.split(/(?:\r\n?|\n)/g);
                this._input = o + this._input, this.yytext = this.yytext.substr(0, this.yytext.length - l), this.offset -= l;
                var d = this.match.split(/(?:\r\n?|\n)/g);
                this.match = this.match.substr(0, this.match.length - 1), this.matched = this.matched.substr(0, this.matched.length - 1), x.length - 1 && (this.yylineno -= x.length - 1);
                var k = this.yylloc.range;
                return this.yylloc = {
                    first_line: this.yylloc.first_line,
                    last_line: this.yylineno + 1,
                    first_column: this.yylloc.first_column,
                    last_column: x ? (x.length === d.length ? this.yylloc.first_column : 0) + d[d.length - x.length].length - x[0].length : this.yylloc.first_column - l
                }, this.options.ranges && (this.yylloc.range = [
                    k[0],
                    k[0] + this.yyleng - l
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
            less: (0, _chunkGTKDMUJJMjs.a)(function(o) {
                this.unput(this.match.slice(o));
            }, "less"),
            pastInput: (0, _chunkGTKDMUJJMjs.a)(function() {
                var o = this.matched.substr(0, this.matched.length - this.match.length);
                return (o.length > 20 ? "..." : "") + o.substr(-20).replace(/\n/g, "");
            }, "pastInput"),
            upcomingInput: (0, _chunkGTKDMUJJMjs.a)(function() {
                var o = this.match;
                return o.length < 20 && (o += this._input.substr(0, 20 - o.length)), (o.substr(0, 20) + (o.length > 20 ? "..." : "")).replace(/\n/g, "");
            }, "upcomingInput"),
            showPosition: (0, _chunkGTKDMUJJMjs.a)(function() {
                var o = this.pastInput(), l = new Array(o.length + 1).join("-");
                return o + this.upcomingInput() + `
` + l + "^";
            }, "showPosition"),
            test_match: (0, _chunkGTKDMUJJMjs.a)(function(o, l) {
                var x, d, k;
                if (this.options.backtrack_lexer && (k = {
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
                }, this.options.ranges && (k.yylloc.range = this.yylloc.range.slice(0))), d = o[0].match(/(?:\r\n?|\n).*/g), d && (this.yylineno += d.length), this.yylloc = {
                    first_line: this.yylloc.last_line,
                    last_line: this.yylineno + 1,
                    first_column: this.yylloc.last_column,
                    last_column: d ? d[d.length - 1].length - d[d.length - 1].match(/\r?\n?/)[0].length : this.yylloc.last_column + o[0].length
                }, this.yytext += o[0], this.match += o[0], this.matches = o, this.yyleng = this.yytext.length, this.options.ranges && (this.yylloc.range = [
                    this.offset,
                    this.offset += this.yyleng
                ]), this._more = !1, this._backtrack = !1, this._input = this._input.slice(o[0].length), this.matched += o[0], x = this.performAction.call(this, this.yy, this, l, this.conditionStack[this.conditionStack.length - 1]), this.done && this._input && (this.done = !1), x) return x;
                if (this._backtrack) {
                    for(var e in k)this[e] = k[e];
                    return !1;
                }
                return !1;
            }, "test_match"),
            next: (0, _chunkGTKDMUJJMjs.a)(function() {
                if (this.done) return this.EOF;
                this._input || (this.done = !0);
                var o, l, x, d;
                this._more || (this.yytext = "", this.match = "");
                for(var k = this._currentRules(), e = 0; e < k.length; e++)if (x = this._input.match(this.rules[k[e]]), x && (!l || x[0].length > l[0].length)) {
                    if (l = x, d = e, this.options.backtrack_lexer) {
                        if (o = this.test_match(x, k[e]), o !== !1) return o;
                        if (this._backtrack) {
                            l = !1;
                            continue;
                        } else return !1;
                    } else if (!this.options.flex) break;
                }
                return l ? (o = this.test_match(l, k[d]), o !== !1 ? o : !1) : this._input === "" ? this.EOF : this.parseError("Lexical error on line " + (this.yylineno + 1) + `. Unrecognized text.
` + this.showPosition(), {
                    text: "",
                    token: null,
                    line: this.yylineno
                });
            }, "next"),
            lex: (0, _chunkGTKDMUJJMjs.a)(function() {
                var l = this.next();
                return l || this.lex();
            }, "lex"),
            begin: (0, _chunkGTKDMUJJMjs.a)(function(l) {
                this.conditionStack.push(l);
            }, "begin"),
            popState: (0, _chunkGTKDMUJJMjs.a)(function() {
                var l = this.conditionStack.length - 1;
                return l > 0 ? this.conditionStack.pop() : this.conditionStack[0];
            }, "popState"),
            _currentRules: (0, _chunkGTKDMUJJMjs.a)(function() {
                return this.conditionStack.length && this.conditionStack[this.conditionStack.length - 1] ? this.conditions[this.conditionStack[this.conditionStack.length - 1]].rules : this.conditions.INITIAL.rules;
            }, "_currentRules"),
            topState: (0, _chunkGTKDMUJJMjs.a)(function(l) {
                return l = this.conditionStack.length - 1 - Math.abs(l || 0), l >= 0 ? this.conditionStack[l] : "INITIAL";
            }, "topState"),
            pushState: (0, _chunkGTKDMUJJMjs.a)(function(l) {
                this.begin(l);
            }, "pushState"),
            stateStackSize: (0, _chunkGTKDMUJJMjs.a)(function() {
                return this.conditionStack.length;
            }, "stateStackSize"),
            options: {
                "case-insensitive": !0
            },
            performAction: (0, _chunkGTKDMUJJMjs.a)(function(l, x, d, k) {
                var e = k;
                switch(d){
                    case 0:
                        break;
                    case 1:
                        break;
                    case 2:
                        return 55;
                    case 3:
                        break;
                    case 4:
                        return this.begin("title"), 35;
                    case 5:
                        return this.popState(), "title_value";
                    case 6:
                        return this.begin("acc_title"), 37;
                    case 7:
                        return this.popState(), "acc_title_value";
                    case 8:
                        return this.begin("acc_descr"), 39;
                    case 9:
                        return this.popState(), "acc_descr_value";
                    case 10:
                        this.begin("acc_descr_multiline");
                        break;
                    case 11:
                        this.popState();
                        break;
                    case 12:
                        return "acc_descr_multiline_value";
                    case 13:
                        return 48;
                    case 14:
                        return 50;
                    case 15:
                        return 49;
                    case 16:
                        return 51;
                    case 17:
                        return 52;
                    case 18:
                        return 53;
                    case 19:
                        return 54;
                    case 20:
                        return 25;
                    case 21:
                        this.begin("md_string");
                        break;
                    case 22:
                        return "MD_STR";
                    case 23:
                        this.popState();
                        break;
                    case 24:
                        this.begin("string");
                        break;
                    case 25:
                        this.popState();
                        break;
                    case 26:
                        return "STR";
                    case 27:
                        this.begin("class_name");
                        break;
                    case 28:
                        return this.popState(), 47;
                    case 29:
                        return this.begin("point_start"), 44;
                    case 30:
                        return this.begin("point_x"), 45;
                    case 31:
                        this.popState();
                        break;
                    case 32:
                        this.popState(), this.begin("point_y");
                        break;
                    case 33:
                        return this.popState(), 46;
                    case 34:
                        return 28;
                    case 35:
                        return 4;
                    case 36:
                        return 11;
                    case 37:
                        return 64;
                    case 38:
                        return 10;
                    case 39:
                        return 65;
                    case 40:
                        return 65;
                    case 41:
                        return 14;
                    case 42:
                        return 13;
                    case 43:
                        return 67;
                    case 44:
                        return 66;
                    case 45:
                        return 12;
                    case 46:
                        return 8;
                    case 47:
                        return 5;
                    case 48:
                        return 18;
                    case 49:
                        return 56;
                    case 50:
                        return 63;
                    case 51:
                        return 57;
                }
            }, "anonymous"),
            rules: [
                /^(?:%%(?!\{)[^\n]*)/i,
                /^(?:[^\}]%%[^\n]*)/i,
                /^(?:[\n\r]+)/i,
                /^(?:%%[^\n]*)/i,
                /^(?:title\b)/i,
                /^(?:(?!\n||)*[^\n]*)/i,
                /^(?:accTitle\s*:\s*)/i,
                /^(?:(?!\n||)*[^\n]*)/i,
                /^(?:accDescr\s*:\s*)/i,
                /^(?:(?!\n||)*[^\n]*)/i,
                /^(?:accDescr\s*\{\s*)/i,
                /^(?:[\}])/i,
                /^(?:[^\}]*)/i,
                /^(?: *x-axis *)/i,
                /^(?: *y-axis *)/i,
                /^(?: *--+> *)/i,
                /^(?: *quadrant-1 *)/i,
                /^(?: *quadrant-2 *)/i,
                /^(?: *quadrant-3 *)/i,
                /^(?: *quadrant-4 *)/i,
                /^(?:classDef\b)/i,
                /^(?:["][`])/i,
                /^(?:[^`"]+)/i,
                /^(?:[`]["])/i,
                /^(?:["])/i,
                /^(?:["])/i,
                /^(?:[^"]*)/i,
                /^(?::::)/i,
                /^(?:^\w+)/i,
                /^(?:\s*:\s*\[\s*)/i,
                /^(?:(1)|(0(.\d+)?))/i,
                /^(?:\s*\] *)/i,
                /^(?:\s*,\s*)/i,
                /^(?:(1)|(0(.\d+)?))/i,
                /^(?: *quadrantChart *)/i,
                /^(?:[A-Za-z]+)/i,
                /^(?::)/i,
                /^(?:\+)/i,
                /^(?:,)/i,
                /^(?:=)/i,
                /^(?:=)/i,
                /^(?:\*)/i,
                /^(?:#)/i,
                /^(?:[\_])/i,
                /^(?:\.)/i,
                /^(?:&)/i,
                /^(?:-)/i,
                /^(?:[0-9]+)/i,
                /^(?:\s)/i,
                /^(?:;)/i,
                /^(?:[!"#$%&'*+,-.`?\\_/])/i,
                /^(?:$)/i
            ],
            conditions: {
                class_name: {
                    rules: [
                        28
                    ],
                    inclusive: !1
                },
                point_y: {
                    rules: [
                        33
                    ],
                    inclusive: !1
                },
                point_x: {
                    rules: [
                        32
                    ],
                    inclusive: !1
                },
                point_start: {
                    rules: [
                        30,
                        31
                    ],
                    inclusive: !1
                },
                acc_descr_multiline: {
                    rules: [
                        11,
                        12
                    ],
                    inclusive: !1
                },
                acc_descr: {
                    rules: [
                        9
                    ],
                    inclusive: !1
                },
                acc_title: {
                    rules: [
                        7
                    ],
                    inclusive: !1
                },
                title: {
                    rules: [
                        5
                    ],
                    inclusive: !1
                },
                md_string: {
                    rules: [
                        22,
                        23
                    ],
                    inclusive: !1
                },
                string: {
                    rules: [
                        25,
                        26
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
                        6,
                        8,
                        10,
                        13,
                        14,
                        15,
                        16,
                        17,
                        18,
                        19,
                        20,
                        21,
                        24,
                        27,
                        29,
                        34,
                        35,
                        36,
                        37,
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
                        51
                    ],
                    inclusive: !0
                }
            }
        };
        return X;
    }();
    At.lexer = ye;
    function Pt() {
        this.yy = {};
    }
    return (0, _chunkGTKDMUJJMjs.a)(Pt, "Parser"), Pt.prototype = At, At.Parser = Pt, new Pt;
}();
Et.parser = Et;
var he = Et;
var V = (0, _chunkNQURTBEVMjs.q)(), qt = class {
    constructor(){
        this.classes = new Map;
        this.config = this.getDefaultConfig(), this.themeConfig = this.getDefaultThemeConfig(), this.data = this.getDefaultData();
    }
    static #_ = (0, _chunkGTKDMUJJMjs.a)(this, "QuadrantBuilder");
    getDefaultData() {
        return {
            titleText: "",
            quadrant1Text: "",
            quadrant2Text: "",
            quadrant3Text: "",
            quadrant4Text: "",
            xAxisLeftText: "",
            xAxisRightText: "",
            yAxisBottomText: "",
            yAxisTopText: "",
            points: []
        };
    }
    getDefaultConfig() {
        return {
            showXAxis: !0,
            showYAxis: !0,
            showTitle: !0,
            chartHeight: (0, _chunkNQURTBEVMjs.s).quadrantChart?.chartWidth || 500,
            chartWidth: (0, _chunkNQURTBEVMjs.s).quadrantChart?.chartHeight || 500,
            titlePadding: (0, _chunkNQURTBEVMjs.s).quadrantChart?.titlePadding || 10,
            titleFontSize: (0, _chunkNQURTBEVMjs.s).quadrantChart?.titleFontSize || 20,
            quadrantPadding: (0, _chunkNQURTBEVMjs.s).quadrantChart?.quadrantPadding || 5,
            xAxisLabelPadding: (0, _chunkNQURTBEVMjs.s).quadrantChart?.xAxisLabelPadding || 5,
            yAxisLabelPadding: (0, _chunkNQURTBEVMjs.s).quadrantChart?.yAxisLabelPadding || 5,
            xAxisLabelFontSize: (0, _chunkNQURTBEVMjs.s).quadrantChart?.xAxisLabelFontSize || 16,
            yAxisLabelFontSize: (0, _chunkNQURTBEVMjs.s).quadrantChart?.yAxisLabelFontSize || 16,
            quadrantLabelFontSize: (0, _chunkNQURTBEVMjs.s).quadrantChart?.quadrantLabelFontSize || 16,
            quadrantTextTopPadding: (0, _chunkNQURTBEVMjs.s).quadrantChart?.quadrantTextTopPadding || 5,
            pointTextPadding: (0, _chunkNQURTBEVMjs.s).quadrantChart?.pointTextPadding || 5,
            pointLabelFontSize: (0, _chunkNQURTBEVMjs.s).quadrantChart?.pointLabelFontSize || 12,
            pointRadius: (0, _chunkNQURTBEVMjs.s).quadrantChart?.pointRadius || 5,
            xAxisPosition: (0, _chunkNQURTBEVMjs.s).quadrantChart?.xAxisPosition || "top",
            yAxisPosition: (0, _chunkNQURTBEVMjs.s).quadrantChart?.yAxisPosition || "left",
            quadrantInternalBorderStrokeWidth: (0, _chunkNQURTBEVMjs.s).quadrantChart?.quadrantInternalBorderStrokeWidth || 1,
            quadrantExternalBorderStrokeWidth: (0, _chunkNQURTBEVMjs.s).quadrantChart?.quadrantExternalBorderStrokeWidth || 2
        };
    }
    getDefaultThemeConfig() {
        return {
            quadrant1Fill: V.quadrant1Fill,
            quadrant2Fill: V.quadrant2Fill,
            quadrant3Fill: V.quadrant3Fill,
            quadrant4Fill: V.quadrant4Fill,
            quadrant1TextFill: V.quadrant1TextFill,
            quadrant2TextFill: V.quadrant2TextFill,
            quadrant3TextFill: V.quadrant3TextFill,
            quadrant4TextFill: V.quadrant4TextFill,
            quadrantPointFill: V.quadrantPointFill,
            quadrantPointTextFill: V.quadrantPointTextFill,
            quadrantXAxisTextFill: V.quadrantXAxisTextFill,
            quadrantYAxisTextFill: V.quadrantYAxisTextFill,
            quadrantTitleFill: V.quadrantTitleFill,
            quadrantInternalBorderStrokeFill: V.quadrantInternalBorderStrokeFill,
            quadrantExternalBorderStrokeFill: V.quadrantExternalBorderStrokeFill
        };
    }
    clear() {
        this.config = this.getDefaultConfig(), this.themeConfig = this.getDefaultThemeConfig(), this.data = this.getDefaultData(), this.classes = new Map, (0, _chunkNQURTBEVMjs.b).info("clear called");
    }
    setData(n) {
        this.data = {
            ...this.data,
            ...n
        };
    }
    addPoints(n) {
        this.data.points = [
            ...n,
            ...this.data.points
        ];
    }
    addClass(n, f) {
        this.classes.set(n, f);
    }
    setConfig(n) {
        (0, _chunkNQURTBEVMjs.b).trace("setConfig called with: ", n), this.config = {
            ...this.config,
            ...n
        };
    }
    setThemeConfig(n) {
        (0, _chunkNQURTBEVMjs.b).trace("setThemeConfig called with: ", n), this.themeConfig = {
            ...this.themeConfig,
            ...n
        };
    }
    calculateSpace(n, f, u, c) {
        let g = this.config.xAxisLabelPadding * 2 + this.config.xAxisLabelFontSize, y = {
            top: n === "top" && f ? g : 0,
            bottom: n === "bottom" && f ? g : 0
        }, S = this.config.yAxisLabelPadding * 2 + this.config.yAxisLabelFontSize, i = {
            left: this.config.yAxisPosition === "left" && u ? S : 0,
            right: this.config.yAxisPosition === "right" && u ? S : 0
        }, A = this.config.titleFontSize + this.config.titlePadding * 2, h = {
            top: c ? A : 0
        }, T = this.config.quadrantPadding + i.left, m = this.config.quadrantPadding + y.top + h.top, b = this.config.chartWidth - this.config.quadrantPadding * 2 - i.left - i.right, q = this.config.chartHeight - this.config.quadrantPadding * 2 - y.top - y.bottom - h.top, p = b / 2, K = q / 2;
        return {
            xAxisSpace: y,
            yAxisSpace: i,
            titleSpace: h,
            quadrantSpace: {
                quadrantLeft: T,
                quadrantTop: m,
                quadrantWidth: b,
                quadrantHalfWidth: p,
                quadrantHeight: q,
                quadrantHalfHeight: K
            }
        };
    }
    getAxisLabels(n, f, u, c) {
        let { quadrantSpace: g, titleSpace: y } = c, { quadrantHalfHeight: S, quadrantHeight: i, quadrantLeft: A, quadrantHalfWidth: h, quadrantTop: T, quadrantWidth: m } = g, b = !!this.data.xAxisRightText, q = !!this.data.yAxisTopText, p = [];
        return this.data.xAxisLeftText && f && p.push({
            text: this.data.xAxisLeftText,
            fill: this.themeConfig.quadrantXAxisTextFill,
            x: A + (b ? h / 2 : 0),
            y: n === "top" ? this.config.xAxisLabelPadding + y.top : this.config.xAxisLabelPadding + T + i + this.config.quadrantPadding,
            fontSize: this.config.xAxisLabelFontSize,
            verticalPos: b ? "center" : "left",
            horizontalPos: "top",
            rotation: 0
        }), this.data.xAxisRightText && f && p.push({
            text: this.data.xAxisRightText,
            fill: this.themeConfig.quadrantXAxisTextFill,
            x: A + h + (b ? h / 2 : 0),
            y: n === "top" ? this.config.xAxisLabelPadding + y.top : this.config.xAxisLabelPadding + T + i + this.config.quadrantPadding,
            fontSize: this.config.xAxisLabelFontSize,
            verticalPos: b ? "center" : "left",
            horizontalPos: "top",
            rotation: 0
        }), this.data.yAxisBottomText && u && p.push({
            text: this.data.yAxisBottomText,
            fill: this.themeConfig.quadrantYAxisTextFill,
            x: this.config.yAxisPosition === "left" ? this.config.yAxisLabelPadding : this.config.yAxisLabelPadding + A + m + this.config.quadrantPadding,
            y: T + i - (q ? S / 2 : 0),
            fontSize: this.config.yAxisLabelFontSize,
            verticalPos: q ? "center" : "left",
            horizontalPos: "top",
            rotation: -90
        }), this.data.yAxisTopText && u && p.push({
            text: this.data.yAxisTopText,
            fill: this.themeConfig.quadrantYAxisTextFill,
            x: this.config.yAxisPosition === "left" ? this.config.yAxisLabelPadding : this.config.yAxisLabelPadding + A + m + this.config.quadrantPadding,
            y: T + S - (q ? S / 2 : 0),
            fontSize: this.config.yAxisLabelFontSize,
            verticalPos: q ? "center" : "left",
            horizontalPos: "top",
            rotation: -90
        }), p;
    }
    getQuadrants(n) {
        let { quadrantSpace: f } = n, { quadrantHalfHeight: u, quadrantLeft: c, quadrantHalfWidth: g, quadrantTop: y } = f, S = [
            {
                text: {
                    text: this.data.quadrant1Text,
                    fill: this.themeConfig.quadrant1TextFill,
                    x: 0,
                    y: 0,
                    fontSize: this.config.quadrantLabelFontSize,
                    verticalPos: "center",
                    horizontalPos: "middle",
                    rotation: 0
                },
                x: c + g,
                y,
                width: g,
                height: u,
                fill: this.themeConfig.quadrant1Fill
            },
            {
                text: {
                    text: this.data.quadrant2Text,
                    fill: this.themeConfig.quadrant2TextFill,
                    x: 0,
                    y: 0,
                    fontSize: this.config.quadrantLabelFontSize,
                    verticalPos: "center",
                    horizontalPos: "middle",
                    rotation: 0
                },
                x: c,
                y,
                width: g,
                height: u,
                fill: this.themeConfig.quadrant2Fill
            },
            {
                text: {
                    text: this.data.quadrant3Text,
                    fill: this.themeConfig.quadrant3TextFill,
                    x: 0,
                    y: 0,
                    fontSize: this.config.quadrantLabelFontSize,
                    verticalPos: "center",
                    horizontalPos: "middle",
                    rotation: 0
                },
                x: c,
                y: y + u,
                width: g,
                height: u,
                fill: this.themeConfig.quadrant3Fill
            },
            {
                text: {
                    text: this.data.quadrant4Text,
                    fill: this.themeConfig.quadrant4TextFill,
                    x: 0,
                    y: 0,
                    fontSize: this.config.quadrantLabelFontSize,
                    verticalPos: "center",
                    horizontalPos: "middle",
                    rotation: 0
                },
                x: c + g,
                y: y + u,
                width: g,
                height: u,
                fill: this.themeConfig.quadrant4Fill
            }
        ];
        for (let i of S)i.text.x = i.x + i.width / 2, this.data.points.length === 0 ? (i.text.y = i.y + i.height / 2, i.text.horizontalPos = "middle") : (i.text.y = i.y + this.config.quadrantTextTopPadding, i.text.horizontalPos = "top");
        return S;
    }
    getQuadrantPoints(n) {
        let { quadrantSpace: f } = n, { quadrantHeight: u, quadrantLeft: c, quadrantTop: g, quadrantWidth: y } = f, S = (0, _chunkNQURTBEVMjs.ja)().domain([
            0,
            1
        ]).range([
            c,
            y + c
        ]), i = (0, _chunkNQURTBEVMjs.ja)().domain([
            0,
            1
        ]).range([
            u + g,
            g
        ]);
        return this.data.points.map((h)=>{
            let T = this.classes.get(h.className);
            return T && (h = {
                ...T,
                ...h
            }), {
                x: S(h.x),
                y: i(h.y),
                fill: h.color ?? this.themeConfig.quadrantPointFill,
                radius: h.radius ?? this.config.pointRadius,
                text: {
                    text: h.text,
                    fill: this.themeConfig.quadrantPointTextFill,
                    x: S(h.x),
                    y: i(h.y) + this.config.pointTextPadding,
                    verticalPos: "center",
                    horizontalPos: "top",
                    fontSize: this.config.pointLabelFontSize,
                    rotation: 0
                },
                strokeColor: h.strokeColor ?? this.themeConfig.quadrantPointFill,
                strokeWidth: h.strokeWidth ?? "0px"
            };
        });
    }
    getBorders(n) {
        let f = this.config.quadrantExternalBorderStrokeWidth / 2, { quadrantSpace: u } = n, { quadrantHalfHeight: c, quadrantHeight: g, quadrantLeft: y, quadrantHalfWidth: S, quadrantTop: i, quadrantWidth: A } = u;
        return [
            {
                strokeFill: this.themeConfig.quadrantExternalBorderStrokeFill,
                strokeWidth: this.config.quadrantExternalBorderStrokeWidth,
                x1: y - f,
                y1: i,
                x2: y + A + f,
                y2: i
            },
            {
                strokeFill: this.themeConfig.quadrantExternalBorderStrokeFill,
                strokeWidth: this.config.quadrantExternalBorderStrokeWidth,
                x1: y + A,
                y1: i + f,
                x2: y + A,
                y2: i + g - f
            },
            {
                strokeFill: this.themeConfig.quadrantExternalBorderStrokeFill,
                strokeWidth: this.config.quadrantExternalBorderStrokeWidth,
                x1: y - f,
                y1: i + g,
                x2: y + A + f,
                y2: i + g
            },
            {
                strokeFill: this.themeConfig.quadrantExternalBorderStrokeFill,
                strokeWidth: this.config.quadrantExternalBorderStrokeWidth,
                x1: y,
                y1: i + f,
                x2: y,
                y2: i + g - f
            },
            {
                strokeFill: this.themeConfig.quadrantInternalBorderStrokeFill,
                strokeWidth: this.config.quadrantInternalBorderStrokeWidth,
                x1: y + S,
                y1: i + f,
                x2: y + S,
                y2: i + g - f
            },
            {
                strokeFill: this.themeConfig.quadrantInternalBorderStrokeFill,
                strokeWidth: this.config.quadrantInternalBorderStrokeWidth,
                x1: y + f,
                y1: i + c,
                x2: y + A - f,
                y2: i + c
            }
        ];
    }
    getTitle(n) {
        if (n) return {
            text: this.data.titleText,
            fill: this.themeConfig.quadrantTitleFill,
            fontSize: this.config.titleFontSize,
            horizontalPos: "top",
            verticalPos: "center",
            rotation: 0,
            y: this.config.titlePadding,
            x: this.config.chartWidth / 2
        };
    }
    build() {
        let n = this.config.showXAxis && !!(this.data.xAxisLeftText || this.data.xAxisRightText), f = this.config.showYAxis && !!(this.data.yAxisTopText || this.data.yAxisBottomText), u = this.config.showTitle && !!this.data.titleText, c = this.data.points.length > 0 ? "bottom" : this.config.xAxisPosition, g = this.calculateSpace(c, n, f, u);
        return {
            points: this.getQuadrantPoints(g),
            quadrants: this.getQuadrants(g),
            axisLabels: this.getAxisLabels(c, n, f, g),
            borderLines: this.getBorders(g),
            title: this.getTitle(u)
        };
    }
};
var G = class extends Error {
    static #_ = (0, _chunkGTKDMUJJMjs.a)(this, "InvalidStyleError");
    constructor(n, f, u){
        super(`value for ${n} ${f} is invalid, please use a valid ${u}`), this.name = "InvalidStyleError";
    }
};
function zt(t) {
    return !/^#?([\dA-Fa-f]{6}|[\dA-Fa-f]{3})$/.test(t);
}
(0, _chunkGTKDMUJJMjs.a)(zt, "validateHexCode");
function xe(t) {
    return !/^\d+$/.test(t);
}
(0, _chunkGTKDMUJJMjs.a)(xe, "validateNumber");
function fe(t) {
    return !/^\d+px$/.test(t);
}
(0, _chunkGTKDMUJJMjs.a)(fe, "validateSizeInPixels");
var ke = (0, _chunkNQURTBEVMjs.X)();
function j(t) {
    return (0, _chunkNQURTBEVMjs.F)(t.trim(), ke);
}
(0, _chunkGTKDMUJJMjs.a)(j, "textSanitizer");
var E = new qt;
function Se(t) {
    E.setData({
        quadrant1Text: j(t.text)
    });
}
(0, _chunkGTKDMUJJMjs.a)(Se, "setQuadrant1Text");
function Ae(t) {
    E.setData({
        quadrant2Text: j(t.text)
    });
}
(0, _chunkGTKDMUJJMjs.a)(Ae, "setQuadrant2Text");
function Pe(t) {
    E.setData({
        quadrant3Text: j(t.text)
    });
}
(0, _chunkGTKDMUJJMjs.a)(Pe, "setQuadrant3Text");
function _e(t) {
    E.setData({
        quadrant4Text: j(t.text)
    });
}
(0, _chunkGTKDMUJJMjs.a)(_e, "setQuadrant4Text");
function Fe(t) {
    E.setData({
        xAxisLeftText: j(t.text)
    });
}
(0, _chunkGTKDMUJJMjs.a)(Fe, "setXAxisLeftText");
function Qe(t) {
    E.setData({
        xAxisRightText: j(t.text)
    });
}
(0, _chunkGTKDMUJJMjs.a)(Qe, "setXAxisRightText");
function Ce(t) {
    E.setData({
        yAxisTopText: j(t.text)
    });
}
(0, _chunkGTKDMUJJMjs.a)(Ce, "setYAxisTopText");
function Le(t) {
    E.setData({
        yAxisBottomText: j(t.text)
    });
}
(0, _chunkGTKDMUJJMjs.a)(Le, "setYAxisBottomText");
function Vt(t) {
    let n = {};
    for (let f of t){
        let [u, c] = f.trim().split(/\s*:\s*/);
        if (u === "radius") {
            if (xe(c)) throw new G(u, c, "number");
            n.radius = parseInt(c);
        } else if (u === "color") {
            if (zt(c)) throw new G(u, c, "hex code");
            n.color = c;
        } else if (u === "stroke-color") {
            if (zt(c)) throw new G(u, c, "hex code");
            n.strokeColor = c;
        } else if (u === "stroke-width") {
            if (fe(c)) throw new G(u, c, "number of pixels (eg. 10px)");
            n.strokeWidth = c;
        } else throw new Error(`style named ${u} is not supported.`);
    }
    return n;
}
(0, _chunkGTKDMUJJMjs.a)(Vt, "parseStyles");
function ve(t, n, f, u, c) {
    let g = Vt(c);
    E.addPoints([
        {
            x: f,
            y: u,
            text: j(t.text),
            className: n,
            ...g
        }
    ]);
}
(0, _chunkGTKDMUJJMjs.a)(ve, "addPoint");
function De(t, n) {
    E.addClass(t, Vt(n));
}
(0, _chunkGTKDMUJJMjs.a)(De, "addClass");
function Ee(t) {
    E.setConfig({
        chartWidth: t
    });
}
(0, _chunkGTKDMUJJMjs.a)(Ee, "setWidth");
function ze(t) {
    E.setConfig({
        chartHeight: t
    });
}
(0, _chunkGTKDMUJJMjs.a)(ze, "setHeight");
function Ve() {
    let t = (0, _chunkNQURTBEVMjs.X)(), { themeVariables: n, quadrantChart: f } = t;
    return f && E.setConfig(f), E.setThemeConfig({
        quadrant1Fill: n.quadrant1Fill,
        quadrant2Fill: n.quadrant2Fill,
        quadrant3Fill: n.quadrant3Fill,
        quadrant4Fill: n.quadrant4Fill,
        quadrant1TextFill: n.quadrant1TextFill,
        quadrant2TextFill: n.quadrant2TextFill,
        quadrant3TextFill: n.quadrant3TextFill,
        quadrant4TextFill: n.quadrant4TextFill,
        quadrantPointFill: n.quadrantPointFill,
        quadrantPointTextFill: n.quadrantPointTextFill,
        quadrantXAxisTextFill: n.quadrantXAxisTextFill,
        quadrantYAxisTextFill: n.quadrantYAxisTextFill,
        quadrantExternalBorderStrokeFill: n.quadrantExternalBorderStrokeFill,
        quadrantInternalBorderStrokeFill: n.quadrantInternalBorderStrokeFill,
        quadrantTitleFill: n.quadrantTitleFill
    }), E.setData({
        titleText: (0, _chunkNQURTBEVMjs.V)()
    }), E.build();
}
(0, _chunkGTKDMUJJMjs.a)(Ve, "getQuadrantData");
var Be = (0, _chunkGTKDMUJJMjs.a)(function() {
    E.clear(), (0, _chunkNQURTBEVMjs.P)();
}, "clear"), pe = {
    setWidth: Ee,
    setHeight: ze,
    setQuadrant1Text: Se,
    setQuadrant2Text: Ae,
    setQuadrant3Text: Pe,
    setQuadrant4Text: _e,
    setXAxisLeftText: Fe,
    setXAxisRightText: Qe,
    setYAxisTopText: Ce,
    setYAxisBottomText: Le,
    parseStyles: Vt,
    addPoint: ve,
    addClass: De,
    getQuadrantData: Ve,
    clear: Be,
    setAccTitle: (0, _chunkNQURTBEVMjs.Q),
    getAccTitle: (0, _chunkNQURTBEVMjs.R),
    setDiagramTitle: (0, _chunkNQURTBEVMjs.U),
    getDiagramTitle: (0, _chunkNQURTBEVMjs.V),
    getAccDescription: (0, _chunkNQURTBEVMjs.T),
    setAccDescription: (0, _chunkNQURTBEVMjs.S)
};
var we = (0, _chunkGTKDMUJJMjs.a)((t, n, f, u)=>{
    function c(a) {
        return a === "top" ? "hanging" : "middle";
    }
    (0, _chunkGTKDMUJJMjs.a)(c, "getDominantBaseLine");
    function g(a) {
        return a === "left" ? "start" : "middle";
    }
    (0, _chunkGTKDMUJJMjs.a)(g, "getTextAnchor");
    function y(a) {
        return `translate(${a.x}, ${a.y}) rotate(${a.rotation || 0})`;
    }
    (0, _chunkGTKDMUJJMjs.a)(y, "getTransformation");
    let S = (0, _chunkNQURTBEVMjs.X)();
    (0, _chunkNQURTBEVMjs.b).debug(`Rendering quadrant chart
` + t);
    let i = S.securityLevel, A;
    i === "sandbox" && (A = (0, _chunkNQURTBEVMjs.fa)("#i" + n));
    let T = (i === "sandbox" ? (0, _chunkNQURTBEVMjs.fa)(A.nodes()[0].contentDocument.body) : (0, _chunkNQURTBEVMjs.fa)("body")).select(`[id="${n}"]`), m = T.append("g").attr("class", "main"), b = S.quadrantChart?.chartWidth ?? 500, q = S.quadrantChart?.chartHeight ?? 500;
    (0, _chunkNQURTBEVMjs.M)(T, q, b, S.quadrantChart?.useMaxWidth ?? !0), T.attr("viewBox", "0 0 " + b + " " + q), u.db.setHeight(q), u.db.setWidth(b);
    let p = u.db.getQuadrantData(), K = m.append("g").attr("class", "quadrants"), dt = m.append("g").attr("class", "border"), ft = m.append("g").attr("class", "data-points"), pt = m.append("g").attr("class", "labels"), gt = m.append("g").attr("class", "title");
    p.title && gt.append("text").attr("x", 0).attr("y", 0).attr("fill", p.title.fill).attr("font-size", p.title.fontSize).attr("dominant-baseline", c(p.title.horizontalPos)).attr("text-anchor", g(p.title.verticalPos)).attr("transform", y(p.title)).text(p.title.text), p.borderLines && dt.selectAll("line").data(p.borderLines).enter().append("line").attr("x1", (a)=>a.x1).attr("y1", (a)=>a.y1).attr("x2", (a)=>a.x2).attr("y2", (a)=>a.y2).style("stroke", (a)=>a.strokeFill).style("stroke-width", (a)=>a.strokeWidth);
    let ut = K.selectAll("g.quadrant").data(p.quadrants).enter().append("g").attr("class", "quadrant");
    ut.append("rect").attr("x", (a)=>a.x).attr("y", (a)=>a.y).attr("width", (a)=>a.width).attr("height", (a)=>a.height).attr("fill", (a)=>a.fill), ut.append("text").attr("x", 0).attr("y", 0).attr("fill", (a)=>a.text.fill).attr("font-size", (a)=>a.text.fontSize).attr("dominant-baseline", (a)=>c(a.text.horizontalPos)).attr("text-anchor", (a)=>g(a.text.verticalPos)).attr("transform", (a)=>y(a.text)).text((a)=>a.text.text), pt.selectAll("g.label").data(p.axisLabels).enter().append("g").attr("class", "label").append("text").attr("x", 0).attr("y", 0).text((a)=>a.text).attr("fill", (a)=>a.fill).attr("font-size", (a)=>a.fontSize).attr("dominant-baseline", (a)=>c(a.horizontalPos)).attr("text-anchor", (a)=>g(a.verticalPos)).attr("transform", (a)=>y(a));
    let ct = ft.selectAll("g.data-point").data(p.points).enter().append("g").attr("class", "data-point");
    ct.append("circle").attr("cx", (a)=>a.x).attr("cy", (a)=>a.y).attr("r", (a)=>a.radius).attr("fill", (a)=>a.fill).attr("stroke", (a)=>a.strokeColor).attr("stroke-width", (a)=>a.strokeWidth), ct.append("text").attr("x", 0).attr("y", 0).text((a)=>a.text.text).attr("fill", (a)=>a.text.fill).attr("font-size", (a)=>a.text.fontSize).attr("dominant-baseline", (a)=>c(a.text.horizontalPos)).attr("text-anchor", (a)=>g(a.text.verticalPos)).attr("transform", (a)=>y(a.text));
}, "draw"), ge = {
    draw: we
};
var ha = {
    parser: he,
    db: pe,
    renderer: ge,
    styles: (0, _chunkGTKDMUJJMjs.a)(()=>"", "styles")
};

},{"./chunk-NQURTBEV.mjs":"iASFe","./chunk-GTKDMUJJ.mjs":"fruhx","@parcel/transformer-js/src/esmodule-helpers.js":"1cdyk"}]},["juLtk"], null, "parcelRequire6955", {})

//# sourceMappingURL=quadrantDiagram-K5BY4R5E.94fe84f5.js.map
