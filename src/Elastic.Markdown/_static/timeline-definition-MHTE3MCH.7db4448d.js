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
})({"3f5Sx":[function(require,module,exports,__globalThis) {
var global = arguments[3];
var HMR_HOST = null;
var HMR_PORT = 1234;
var HMR_SERVER_PORT = 1234;
var HMR_SECURE = false;
var HMR_ENV_HASH = "febfef3c5b467b16";
var HMR_USE_SSE = false;
module.bundle.HMR_BUNDLE_ID = "996cfa087db4448d";
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

},{}],"7D1Xq":[function(require,module,exports,__globalThis) {
var parcelHelpers = require("@parcel/transformer-js/src/esmodule-helpers.js");
parcelHelpers.defineInteropFlag(exports);
parcelHelpers.export(exports, "diagram", ()=>ye);
var _chunkNQURTBEVMjs = require("./chunk-NQURTBEV.mjs");
var _chunkGTKDMUJJMjs = require("./chunk-GTKDMUJJ.mjs");
var X = function() {
    var n = (0, _chunkGTKDMUJJMjs.a)(function(m, i, a, c) {
        for(a = a || {}, c = m.length; c--; a[m[c]] = i);
        return a;
    }, "o"), t = [
        6,
        8,
        10,
        11,
        12,
        14,
        16,
        17,
        20,
        21
    ], e = [
        1,
        9
    ], o = [
        1,
        10
    ], r = [
        1,
        11
    ], u = [
        1,
        12
    ], h = [
        1,
        13
    ], f = [
        1,
        16
    ], g = [
        1,
        17
    ], d = {
        trace: (0, _chunkGTKDMUJJMjs.a)(function() {}, "trace"),
        yy: {},
        symbols_: {
            error: 2,
            start: 3,
            timeline: 4,
            document: 5,
            EOF: 6,
            line: 7,
            SPACE: 8,
            statement: 9,
            NEWLINE: 10,
            title: 11,
            acc_title: 12,
            acc_title_value: 13,
            acc_descr: 14,
            acc_descr_value: 15,
            acc_descr_multiline_value: 16,
            section: 17,
            period_statement: 18,
            event_statement: 19,
            period: 20,
            event: 21,
            $accept: 0,
            $end: 1
        },
        terminals_: {
            2: "error",
            4: "timeline",
            6: "EOF",
            8: "SPACE",
            10: "NEWLINE",
            11: "title",
            12: "acc_title",
            13: "acc_title_value",
            14: "acc_descr",
            15: "acc_descr_value",
            16: "acc_descr_multiline_value",
            17: "section",
            20: "period",
            21: "event"
        },
        productions_: [
            0,
            [
                3,
                3
            ],
            [
                5,
                0
            ],
            [
                5,
                2
            ],
            [
                7,
                2
            ],
            [
                7,
                1
            ],
            [
                7,
                1
            ],
            [
                7,
                1
            ],
            [
                9,
                1
            ],
            [
                9,
                2
            ],
            [
                9,
                2
            ],
            [
                9,
                1
            ],
            [
                9,
                1
            ],
            [
                9,
                1
            ],
            [
                9,
                1
            ],
            [
                18,
                1
            ],
            [
                19,
                1
            ]
        ],
        performAction: (0, _chunkGTKDMUJJMjs.a)(function(i, a, c, p, y, l, E) {
            var k = l.length - 1;
            switch(y){
                case 1:
                    return l[k - 1];
                case 2:
                    this.$ = [];
                    break;
                case 3:
                    l[k - 1].push(l[k]), this.$ = l[k - 1];
                    break;
                case 4:
                case 5:
                    this.$ = l[k];
                    break;
                case 6:
                case 7:
                    this.$ = [];
                    break;
                case 8:
                    p.getCommonDb().setDiagramTitle(l[k].substr(6)), this.$ = l[k].substr(6);
                    break;
                case 9:
                    this.$ = l[k].trim(), p.getCommonDb().setAccTitle(this.$);
                    break;
                case 10:
                case 11:
                    this.$ = l[k].trim(), p.getCommonDb().setAccDescription(this.$);
                    break;
                case 12:
                    p.addSection(l[k].substr(8)), this.$ = l[k].substr(8);
                    break;
                case 15:
                    p.addTask(l[k], 0, ""), this.$ = l[k];
                    break;
                case 16:
                    p.addEvent(l[k].substr(2)), this.$ = l[k];
                    break;
            }
        }, "anonymous"),
        table: [
            {
                3: 1,
                4: [
                    1,
                    2
                ]
            },
            {
                1: [
                    3
                ]
            },
            n(t, [
                2,
                2
            ], {
                5: 3
            }),
            {
                6: [
                    1,
                    4
                ],
                7: 5,
                8: [
                    1,
                    6
                ],
                9: 7,
                10: [
                    1,
                    8
                ],
                11: e,
                12: o,
                14: r,
                16: u,
                17: h,
                18: 14,
                19: 15,
                20: f,
                21: g
            },
            n(t, [
                2,
                7
            ], {
                1: [
                    2,
                    1
                ]
            }),
            n(t, [
                2,
                3
            ]),
            {
                9: 18,
                11: e,
                12: o,
                14: r,
                16: u,
                17: h,
                18: 14,
                19: 15,
                20: f,
                21: g
            },
            n(t, [
                2,
                5
            ]),
            n(t, [
                2,
                6
            ]),
            n(t, [
                2,
                8
            ]),
            {
                13: [
                    1,
                    19
                ]
            },
            {
                15: [
                    1,
                    20
                ]
            },
            n(t, [
                2,
                11
            ]),
            n(t, [
                2,
                12
            ]),
            n(t, [
                2,
                13
            ]),
            n(t, [
                2,
                14
            ]),
            n(t, [
                2,
                15
            ]),
            n(t, [
                2,
                16
            ]),
            n(t, [
                2,
                4
            ]),
            n(t, [
                2,
                9
            ]),
            n(t, [
                2,
                10
            ])
        ],
        defaultActions: {},
        parseError: (0, _chunkGTKDMUJJMjs.a)(function(i, a) {
            if (a.recoverable) this.trace(i);
            else {
                var c = new Error(i);
                throw c.hash = a, c;
            }
        }, "parseError"),
        parse: (0, _chunkGTKDMUJJMjs.a)(function(i) {
            var a = this, c = [
                0
            ], p = [], y = [
                null
            ], l = [], E = this.table, k = "", N = 0, C = 0, V = 0, et = 2, L = 1, v = l.slice.call(arguments, 1), b = Object.create(this.lexer), T = {
                yy: {}
            };
            for(var A in this.yy)Object.prototype.hasOwnProperty.call(this.yy, A) && (T.yy[A] = this.yy[A]);
            b.setInput(i, T.yy), T.yy.lexer = b, T.yy.parser = this, typeof b.yylloc > "u" && (b.yylloc = {});
            var P = b.yylloc;
            l.push(P);
            var U = b.options && b.options.ranges;
            typeof T.yy.parseError == "function" ? this.parseError = T.yy.parseError : this.parseError = Object.getPrototypeOf(this).parseError;
            function Zt(I) {
                c.length = c.length - 2 * I, y.length = y.length - I, l.length = l.length - I;
            }
            (0, _chunkGTKDMUJJMjs.a)(Zt, "popStack");
            function Mt() {
                var I;
                return I = p.pop() || b.lex() || L, typeof I != "number" && (I instanceof Array && (p = I, I = p.pop()), I = a.symbols_[I] || I), I;
            }
            (0, _chunkGTKDMUJJMjs.a)(Mt, "lex");
            for(var w, Z, B, M, Jt, J, R = {}, O, $, nt, j;;){
                if (B = c[c.length - 1], this.defaultActions[B] ? M = this.defaultActions[B] : ((w === null || typeof w > "u") && (w = Mt()), M = E[B] && E[B][w]), typeof M > "u" || !M.length || !M[0]) {
                    var K = "";
                    j = [];
                    for(O in E[B])this.terminals_[O] && O > et && j.push("'" + this.terminals_[O] + "'");
                    b.showPosition ? K = "Parse error on line " + (N + 1) + `:
` + b.showPosition() + `
Expecting ` + j.join(", ") + ", got '" + (this.terminals_[w] || w) + "'" : K = "Parse error on line " + (N + 1) + ": Unexpected " + (w == L ? "end of input" : "'" + (this.terminals_[w] || w) + "'"), this.parseError(K, {
                        text: b.match,
                        token: this.terminals_[w] || w,
                        line: b.yylineno,
                        loc: P,
                        expected: j
                    });
                }
                if (M[0] instanceof Array && M.length > 1) throw new Error("Parse Error: multiple actions possible at state: " + B + ", token: " + w);
                switch(M[0]){
                    case 1:
                        c.push(w), y.push(b.yytext), l.push(b.yylloc), c.push(M[1]), w = null, Z ? (w = Z, Z = null) : (C = b.yyleng, k = b.yytext, N = b.yylineno, P = b.yylloc, V > 0 && V--);
                        break;
                    case 2:
                        if ($ = this.productions_[M[1]][1], R.$ = y[y.length - $], R._$ = {
                            first_line: l[l.length - ($ || 1)].first_line,
                            last_line: l[l.length - 1].last_line,
                            first_column: l[l.length - ($ || 1)].first_column,
                            last_column: l[l.length - 1].last_column
                        }, U && (R._$.range = [
                            l[l.length - ($ || 1)].range[0],
                            l[l.length - 1].range[1]
                        ]), J = this.performAction.apply(R, [
                            k,
                            C,
                            N,
                            T.yy,
                            M[1],
                            y,
                            l
                        ].concat(v)), typeof J < "u") return J;
                        $ && (c = c.slice(0, -1 * $ * 2), y = y.slice(0, -1 * $), l = l.slice(0, -1 * $)), c.push(this.productions_[M[1]][0]), y.push(R.$), l.push(R._$), nt = E[c[c.length - 2]][c[c.length - 1]], c.push(nt);
                        break;
                    case 3:
                        return !0;
                }
            }
            return !0;
        }, "parse")
    }, x = function() {
        var m = {
            EOF: 1,
            parseError: (0, _chunkGTKDMUJJMjs.a)(function(a, c) {
                if (this.yy.parser) this.yy.parser.parseError(a, c);
                else throw new Error(a);
            }, "parseError"),
            setInput: (0, _chunkGTKDMUJJMjs.a)(function(i, a) {
                return this.yy = a || this.yy || {}, this._input = i, this._more = this._backtrack = this.done = !1, this.yylineno = this.yyleng = 0, this.yytext = this.matched = this.match = "", this.conditionStack = [
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
                var i = this._input[0];
                this.yytext += i, this.yyleng++, this.offset++, this.match += i, this.matched += i;
                var a = i.match(/(?:\r\n?|\n).*/g);
                return a ? (this.yylineno++, this.yylloc.last_line++) : this.yylloc.last_column++, this.options.ranges && this.yylloc.range[1]++, this._input = this._input.slice(1), i;
            }, "input"),
            unput: (0, _chunkGTKDMUJJMjs.a)(function(i) {
                var a = i.length, c = i.split(/(?:\r\n?|\n)/g);
                this._input = i + this._input, this.yytext = this.yytext.substr(0, this.yytext.length - a), this.offset -= a;
                var p = this.match.split(/(?:\r\n?|\n)/g);
                this.match = this.match.substr(0, this.match.length - 1), this.matched = this.matched.substr(0, this.matched.length - 1), c.length - 1 && (this.yylineno -= c.length - 1);
                var y = this.yylloc.range;
                return this.yylloc = {
                    first_line: this.yylloc.first_line,
                    last_line: this.yylineno + 1,
                    first_column: this.yylloc.first_column,
                    last_column: c ? (c.length === p.length ? this.yylloc.first_column : 0) + p[p.length - c.length].length - c[0].length : this.yylloc.first_column - a
                }, this.options.ranges && (this.yylloc.range = [
                    y[0],
                    y[0] + this.yyleng - a
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
            less: (0, _chunkGTKDMUJJMjs.a)(function(i) {
                this.unput(this.match.slice(i));
            }, "less"),
            pastInput: (0, _chunkGTKDMUJJMjs.a)(function() {
                var i = this.matched.substr(0, this.matched.length - this.match.length);
                return (i.length > 20 ? "..." : "") + i.substr(-20).replace(/\n/g, "");
            }, "pastInput"),
            upcomingInput: (0, _chunkGTKDMUJJMjs.a)(function() {
                var i = this.match;
                return i.length < 20 && (i += this._input.substr(0, 20 - i.length)), (i.substr(0, 20) + (i.length > 20 ? "..." : "")).replace(/\n/g, "");
            }, "upcomingInput"),
            showPosition: (0, _chunkGTKDMUJJMjs.a)(function() {
                var i = this.pastInput(), a = new Array(i.length + 1).join("-");
                return i + this.upcomingInput() + `
` + a + "^";
            }, "showPosition"),
            test_match: (0, _chunkGTKDMUJJMjs.a)(function(i, a) {
                var c, p, y;
                if (this.options.backtrack_lexer && (y = {
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
                }, this.options.ranges && (y.yylloc.range = this.yylloc.range.slice(0))), p = i[0].match(/(?:\r\n?|\n).*/g), p && (this.yylineno += p.length), this.yylloc = {
                    first_line: this.yylloc.last_line,
                    last_line: this.yylineno + 1,
                    first_column: this.yylloc.last_column,
                    last_column: p ? p[p.length - 1].length - p[p.length - 1].match(/\r?\n?/)[0].length : this.yylloc.last_column + i[0].length
                }, this.yytext += i[0], this.match += i[0], this.matches = i, this.yyleng = this.yytext.length, this.options.ranges && (this.yylloc.range = [
                    this.offset,
                    this.offset += this.yyleng
                ]), this._more = !1, this._backtrack = !1, this._input = this._input.slice(i[0].length), this.matched += i[0], c = this.performAction.call(this, this.yy, this, a, this.conditionStack[this.conditionStack.length - 1]), this.done && this._input && (this.done = !1), c) return c;
                if (this._backtrack) {
                    for(var l in y)this[l] = y[l];
                    return !1;
                }
                return !1;
            }, "test_match"),
            next: (0, _chunkGTKDMUJJMjs.a)(function() {
                if (this.done) return this.EOF;
                this._input || (this.done = !0);
                var i, a, c, p;
                this._more || (this.yytext = "", this.match = "");
                for(var y = this._currentRules(), l = 0; l < y.length; l++)if (c = this._input.match(this.rules[y[l]]), c && (!a || c[0].length > a[0].length)) {
                    if (a = c, p = l, this.options.backtrack_lexer) {
                        if (i = this.test_match(c, y[l]), i !== !1) return i;
                        if (this._backtrack) {
                            a = !1;
                            continue;
                        } else return !1;
                    } else if (!this.options.flex) break;
                }
                return a ? (i = this.test_match(a, y[p]), i !== !1 ? i : !1) : this._input === "" ? this.EOF : this.parseError("Lexical error on line " + (this.yylineno + 1) + `. Unrecognized text.
` + this.showPosition(), {
                    text: "",
                    token: null,
                    line: this.yylineno
                });
            }, "next"),
            lex: (0, _chunkGTKDMUJJMjs.a)(function() {
                var a = this.next();
                return a || this.lex();
            }, "lex"),
            begin: (0, _chunkGTKDMUJJMjs.a)(function(a) {
                this.conditionStack.push(a);
            }, "begin"),
            popState: (0, _chunkGTKDMUJJMjs.a)(function() {
                var a = this.conditionStack.length - 1;
                return a > 0 ? this.conditionStack.pop() : this.conditionStack[0];
            }, "popState"),
            _currentRules: (0, _chunkGTKDMUJJMjs.a)(function() {
                return this.conditionStack.length && this.conditionStack[this.conditionStack.length - 1] ? this.conditions[this.conditionStack[this.conditionStack.length - 1]].rules : this.conditions.INITIAL.rules;
            }, "_currentRules"),
            topState: (0, _chunkGTKDMUJJMjs.a)(function(a) {
                return a = this.conditionStack.length - 1 - Math.abs(a || 0), a >= 0 ? this.conditionStack[a] : "INITIAL";
            }, "topState"),
            pushState: (0, _chunkGTKDMUJJMjs.a)(function(a) {
                this.begin(a);
            }, "pushState"),
            stateStackSize: (0, _chunkGTKDMUJJMjs.a)(function() {
                return this.conditionStack.length;
            }, "stateStackSize"),
            options: {
                "case-insensitive": !0
            },
            performAction: (0, _chunkGTKDMUJJMjs.a)(function(a, c, p, y) {
                var l = y;
                switch(p){
                    case 0:
                        break;
                    case 1:
                        break;
                    case 2:
                        return 10;
                    case 3:
                        break;
                    case 4:
                        break;
                    case 5:
                        return 4;
                    case 6:
                        return 11;
                    case 7:
                        return this.begin("acc_title"), 12;
                    case 8:
                        return this.popState(), "acc_title_value";
                    case 9:
                        return this.begin("acc_descr"), 14;
                    case 10:
                        return this.popState(), "acc_descr_value";
                    case 11:
                        this.begin("acc_descr_multiline");
                        break;
                    case 12:
                        this.popState();
                        break;
                    case 13:
                        return "acc_descr_multiline_value";
                    case 14:
                        return 17;
                    case 15:
                        return 21;
                    case 16:
                        return 20;
                    case 17:
                        return 6;
                    case 18:
                        return "INVALID";
                }
            }, "anonymous"),
            rules: [
                /^(?:%(?!\{)[^\n]*)/i,
                /^(?:[^\}]%%[^\n]*)/i,
                /^(?:[\n]+)/i,
                /^(?:\s+)/i,
                /^(?:#[^\n]*)/i,
                /^(?:timeline\b)/i,
                /^(?:title\s[^\n]+)/i,
                /^(?:accTitle\s*:\s*)/i,
                /^(?:(?!\n||)*[^\n]*)/i,
                /^(?:accDescr\s*:\s*)/i,
                /^(?:(?!\n||)*[^\n]*)/i,
                /^(?:accDescr\s*\{\s*)/i,
                /^(?:[\}])/i,
                /^(?:[^\}]*)/i,
                /^(?:section\s[^:\n]+)/i,
                /^(?::\s[^:\n]+)/i,
                /^(?:[^#:\n]+)/i,
                /^(?:$)/i,
                /^(?:.)/i
            ],
            conditions: {
                acc_descr_multiline: {
                    rules: [
                        12,
                        13
                    ],
                    inclusive: !1
                },
                acc_descr: {
                    rules: [
                        10
                    ],
                    inclusive: !1
                },
                acc_title: {
                    rules: [
                        8
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
                        9,
                        11,
                        14,
                        15,
                        16,
                        17,
                        18
                    ],
                    inclusive: !0
                }
            }
        };
        return m;
    }();
    d.lexer = x;
    function _() {
        this.yy = {};
    }
    return (0, _chunkGTKDMUJJMjs.a)(_, "Parser"), _.prototype = d, d.Parser = _, new _;
}();
X.parser = X;
var ht = X;
var D = {};
(0, _chunkGTKDMUJJMjs.c)(D, {
    addEvent: ()=>bt,
    addSection: ()=>ft,
    addTask: ()=>xt,
    addTaskOrg: ()=>kt,
    clear: ()=>yt,
    default: ()=>Lt,
    getCommonDb: ()=>pt,
    getSections: ()=>gt,
    getTasks: ()=>mt
});
var F = "", dt = 0, Y = [], G = [], W = [], pt = (0, _chunkGTKDMUJJMjs.a)(()=>(0, _chunkNQURTBEVMjs.W), "getCommonDb"), yt = (0, _chunkGTKDMUJJMjs.a)(function() {
    Y.length = 0, G.length = 0, F = "", W.length = 0, (0, _chunkNQURTBEVMjs.P)();
}, "clear"), ft = (0, _chunkGTKDMUJJMjs.a)(function(n) {
    F = n, Y.push(n);
}, "addSection"), gt = (0, _chunkGTKDMUJJMjs.a)(function() {
    return Y;
}, "getSections"), mt = (0, _chunkGTKDMUJJMjs.a)(function() {
    let n = ut(), t = 100, e = 0;
    for(; !n && e < t;)n = ut(), e++;
    return G.push(...W), G;
}, "getTasks"), xt = (0, _chunkGTKDMUJJMjs.a)(function(n, t, e) {
    let o = {
        id: dt++,
        section: F,
        type: F,
        task: n,
        score: t || 0,
        events: e ? [
            e
        ] : []
    };
    W.push(o);
}, "addTask"), bt = (0, _chunkGTKDMUJJMjs.a)(function(n) {
    W.find((e)=>e.id === dt - 1).events.push(n);
}, "addEvent"), kt = (0, _chunkGTKDMUJJMjs.a)(function(n) {
    let t = {
        section: F,
        type: F,
        description: n,
        task: n,
        classes: []
    };
    G.push(t);
}, "addTaskOrg"), ut = (0, _chunkGTKDMUJJMjs.a)(function() {
    let n = (0, _chunkGTKDMUJJMjs.a)(function(e) {
        return W[e].processed;
    }, "compileTask"), t = !0;
    for (let [e, o] of W.entries())n(e), t = t && o.processed;
    return t;
}, "compileTasks"), Lt = {
    clear: yt,
    getCommonDb: pt,
    addSection: ft,
    getSections: gt,
    getTasks: mt,
    addTask: xt,
    addTaskOrg: kt,
    addEvent: bt
};
var $t = 12, q = (0, _chunkGTKDMUJJMjs.a)(function(n, t) {
    let e = n.append("rect");
    return e.attr("x", t.x), e.attr("y", t.y), e.attr("fill", t.fill), e.attr("stroke", t.stroke), e.attr("width", t.width), e.attr("height", t.height), e.attr("rx", t.rx), e.attr("ry", t.ry), t.class !== void 0 && e.attr("class", t.class), e;
}, "drawRect"), At = (0, _chunkGTKDMUJJMjs.a)(function(n, t) {
    let o = n.append("circle").attr("cx", t.cx).attr("cy", t.cy).attr("class", "face").attr("r", 15).attr("stroke-width", 2).attr("overflow", "visible"), r = n.append("g");
    r.append("circle").attr("cx", t.cx - 5).attr("cy", t.cy - 5).attr("r", 1.5).attr("stroke-width", 2).attr("fill", "#666").attr("stroke", "#666"), r.append("circle").attr("cx", t.cx + 5).attr("cy", t.cy - 5).attr("r", 1.5).attr("stroke-width", 2).attr("fill", "#666").attr("stroke", "#666");
    function u(g) {
        let d = (0, _chunkNQURTBEVMjs.Aa)().startAngle(Math.PI / 2).endAngle(3 * (Math.PI / 2)).innerRadius(7.5).outerRadius(6.8181818181818175);
        g.append("path").attr("class", "mouth").attr("d", d).attr("transform", "translate(" + t.cx + "," + (t.cy + 2) + ")");
    }
    (0, _chunkGTKDMUJJMjs.a)(u, "smile");
    function h(g) {
        let d = (0, _chunkNQURTBEVMjs.Aa)().startAngle(3 * Math.PI / 2).endAngle(5 * (Math.PI / 2)).innerRadius(7.5).outerRadius(6.8181818181818175);
        g.append("path").attr("class", "mouth").attr("d", d).attr("transform", "translate(" + t.cx + "," + (t.cy + 7) + ")");
    }
    (0, _chunkGTKDMUJJMjs.a)(h, "sad");
    function f(g) {
        g.append("line").attr("class", "mouth").attr("stroke", 2).attr("x1", t.cx - 5).attr("y1", t.cy + 7).attr("x2", t.cx + 5).attr("y2", t.cy + 7).attr("class", "mouth").attr("stroke-width", "1px").attr("stroke", "#666");
    }
    return (0, _chunkGTKDMUJJMjs.a)(f, "ambivalent"), t.score > 3 ? u(r) : t.score < 3 ? h(r) : f(r), o;
}, "drawFace"), Ht = (0, _chunkGTKDMUJJMjs.a)(function(n, t) {
    let e = n.append("circle");
    return e.attr("cx", t.cx), e.attr("cy", t.cy), e.attr("class", "actor-" + t.pos), e.attr("fill", t.fill), e.attr("stroke", t.stroke), e.attr("r", t.r), e.class !== void 0 && e.attr("class", e.class), t.title !== void 0 && e.append("title").text(t.title), e;
}, "drawCircle"), vt = (0, _chunkGTKDMUJJMjs.a)(function(n, t) {
    let e = t.text.replace(/<br\s*\/?>/gi, " "), o = n.append("text");
    o.attr("x", t.x), o.attr("y", t.y), o.attr("class", "legend"), o.style("text-anchor", t.anchor), t.class !== void 0 && o.attr("class", t.class);
    let r = o.append("tspan");
    return r.attr("x", t.x + t.textMargin * 2), r.text(e), o;
}, "drawText"), Ct = (0, _chunkGTKDMUJJMjs.a)(function(n, t) {
    function e(r, u, h, f, g) {
        return r + "," + u + " " + (r + h) + "," + u + " " + (r + h) + "," + (u + f - g) + " " + (r + h - g * 1.2) + "," + (u + f) + " " + r + "," + (u + f);
    }
    (0, _chunkGTKDMUJJMjs.a)(e, "genPoints");
    let o = n.append("polygon");
    o.attr("points", e(t.x, t.y, 50, 20, 7)), o.attr("class", "labelBox"), t.y = t.y + t.labelMargin, t.x = t.x + .5 * t.labelMargin, vt(n, t);
}, "drawLabel"), Pt = (0, _chunkGTKDMUJJMjs.a)(function(n, t, e) {
    let o = n.append("g"), r = tt();
    r.x = t.x, r.y = t.y, r.fill = t.fill, r.width = e.width, r.height = e.height, r.class = "journey-section section-type-" + t.num, r.rx = 3, r.ry = 3, q(o, r), wt(e)(t.text, o, r.x, r.y, r.width, r.height, {
        class: "journey-section section-type-" + t.num
    }, e, t.colour);
}, "drawSection"), _t = -1, Bt = (0, _chunkGTKDMUJJMjs.a)(function(n, t, e) {
    let o = t.x + e.width / 2, r = n.append("g");
    _t++;
    let u = 450;
    r.append("line").attr("id", "task" + _t).attr("x1", o).attr("y1", t.y).attr("x2", o).attr("y2", u).attr("class", "task-line").attr("stroke-width", "1px").attr("stroke-dasharray", "4 2").attr("stroke", "#666"), At(r, {
        cx: o,
        cy: 300 + (5 - t.score) * 30,
        score: t.score
    });
    let h = tt();
    h.x = t.x, h.y = t.y, h.fill = t.fill, h.width = e.width, h.height = e.height, h.class = "task task-type-" + t.num, h.rx = 3, h.ry = 3, q(r, h), wt(e)(t.task, r, h.x, h.y, h.width, h.height, {
        class: "task"
    }, e, t.colour);
}, "drawTask"), Vt = (0, _chunkGTKDMUJJMjs.a)(function(n, t) {
    q(n, {
        x: t.startx,
        y: t.starty,
        width: t.stopx - t.startx,
        height: t.stopy - t.starty,
        fill: t.fill,
        class: "rect"
    }).lower();
}, "drawBackgroundRect"), Rt = (0, _chunkGTKDMUJJMjs.a)(function() {
    return {
        x: 0,
        y: 0,
        fill: void 0,
        "text-anchor": "start",
        width: 100,
        height: 100,
        textMargin: 0,
        rx: 0,
        ry: 0
    };
}, "getTextObj"), tt = (0, _chunkGTKDMUJJMjs.a)(function() {
    return {
        x: 0,
        y: 0,
        width: 100,
        anchor: "start",
        height: 100,
        rx: 0,
        ry: 0
    };
}, "getNoteRect"), wt = function() {
    function n(r, u, h, f, g, d, x, _) {
        let m = u.append("text").attr("x", h + g / 2).attr("y", f + d / 2 + 5).style("font-color", _).style("text-anchor", "middle").text(r);
        o(m, x);
    }
    (0, _chunkGTKDMUJJMjs.a)(n, "byText");
    function t(r, u, h, f, g, d, x, _, m) {
        let { taskFontSize: i, taskFontFamily: a } = _, c = r.split(/<br\s*\/?>/gi);
        for(let p = 0; p < c.length; p++){
            let y = p * i - i * (c.length - 1) / 2, l = u.append("text").attr("x", h + g / 2).attr("y", f).attr("fill", m).style("text-anchor", "middle").style("font-size", i).style("font-family", a);
            l.append("tspan").attr("x", h + g / 2).attr("dy", y).text(c[p]), l.attr("y", f + d / 2).attr("dominant-baseline", "central").attr("alignment-baseline", "central"), o(l, x);
        }
    }
    (0, _chunkGTKDMUJJMjs.a)(t, "byTspan");
    function e(r, u, h, f, g, d, x, _) {
        let m = u.append("switch"), a = m.append("foreignObject").attr("x", h).attr("y", f).attr("width", g).attr("height", d).attr("position", "fixed").append("xhtml:div").style("display", "table").style("height", "100%").style("width", "100%");
        a.append("div").attr("class", "label").style("display", "table-cell").style("text-align", "center").style("vertical-align", "middle").text(r), t(r, m, h, f, g, d, x, _), o(a, x);
    }
    (0, _chunkGTKDMUJJMjs.a)(e, "byFo");
    function o(r, u) {
        for(let h in u)h in u && r.attr(h, u[h]);
    }
    return (0, _chunkGTKDMUJJMjs.a)(o, "_setTextAttrs"), function(r) {
        return r.textPlacement === "fo" ? e : r.textPlacement === "old" ? n : t;
    };
}(), zt = (0, _chunkGTKDMUJJMjs.a)(function(n) {
    n.append("defs").append("marker").attr("id", "arrowhead").attr("refX", 5).attr("refY", 2).attr("markerWidth", 6).attr("markerHeight", 4).attr("orient", "auto").append("path").attr("d", "M 0,0 V 4 L6,2 Z");
}, "initGraphics");
function St(n, t) {
    n.each(function() {
        var e = (0, _chunkNQURTBEVMjs.fa)(this), o = e.text().split(/(\s+|<br>)/).reverse(), r, u = [], h = 1.1, f = e.attr("y"), g = parseFloat(e.attr("dy")), d = e.text(null).append("tspan").attr("x", 0).attr("y", f).attr("dy", g + "em");
        for(let x = 0; x < o.length; x++)r = o[o.length - 1 - x], u.push(r), d.text(u.join(" ").trim()), (d.node().getComputedTextLength() > t || r === "<br>") && (u.pop(), d.text(u.join(" ").trim()), r === "<br>" ? u = [
            ""
        ] : u = [
            r
        ], d = e.append("tspan").attr("x", 0).attr("y", f).attr("dy", h + "em").text(r));
    });
}
(0, _chunkGTKDMUJJMjs.a)(St, "wrap");
var Ft = (0, _chunkGTKDMUJJMjs.a)(function(n, t, e, o) {
    let r = e % $t - 1, u = n.append("g");
    t.section = r, u.attr("class", (t.class ? t.class + " " : "") + "timeline-node " + ("section-" + r));
    let h = u.append("g"), f = u.append("g"), d = f.append("text").text(t.descr).attr("dy", "1em").attr("alignment-baseline", "middle").attr("dominant-baseline", "middle").attr("text-anchor", "middle").call(St, t.width).node().getBBox(), x = o.fontSize?.replace ? o.fontSize.replace("px", "") : o.fontSize;
    return t.height = d.height + x * 0.55 + t.padding, t.height = Math.max(t.height, t.maxHeight), t.width = t.width + 2 * t.padding, f.attr("transform", "translate(" + t.width / 2 + ", " + t.padding / 2 + ")"), Ot(h, t, r, o), t;
}, "drawNode"), Wt = (0, _chunkGTKDMUJJMjs.a)(function(n, t, e) {
    let o = n.append("g"), u = o.append("text").text(t.descr).attr("dy", "1em").attr("alignment-baseline", "middle").attr("dominant-baseline", "middle").attr("text-anchor", "middle").call(St, t.width).node().getBBox(), h = e.fontSize?.replace ? e.fontSize.replace("px", "") : e.fontSize;
    return o.remove(), u.height + h * 0.55 + t.padding;
}, "getVirtualNodeHeight"), Ot = (0, _chunkGTKDMUJJMjs.a)(function(n, t, e) {
    n.append("path").attr("id", "node-" + t.id).attr("class", "node-bkg node-" + t.type).attr("d", `M0 ${t.height - 5} v${-t.height + 10} q0,-5 5,-5 h${t.width - 10} q5,0 5,5 v${t.height - 5} H0 Z`), n.append("line").attr("class", "node-line-" + e).attr("x1", 0).attr("y1", t.height).attr("x2", t.width).attr("y2", t.height);
}, "defaultBkg"), H = {
    drawRect: q,
    drawCircle: Ht,
    drawSection: Pt,
    drawText: vt,
    drawLabel: Ct,
    drawTask: Bt,
    drawBackgroundRect: Vt,
    getTextObj: Rt,
    getNoteRect: tt,
    initGraphics: zt,
    drawNode: Ft,
    getVirtualNodeHeight: Wt
};
var jt = (0, _chunkGTKDMUJJMjs.a)(function(n, t, e, o) {
    let r = (0, _chunkNQURTBEVMjs.X)(), u = r.leftMargin ?? 50;
    (0, _chunkNQURTBEVMjs.b).debug("timeline", o.db);
    let h = r.securityLevel, f;
    h === "sandbox" && (f = (0, _chunkNQURTBEVMjs.fa)("#i" + t));
    let d = (h === "sandbox" ? (0, _chunkNQURTBEVMjs.fa)(f.nodes()[0].contentDocument.body) : (0, _chunkNQURTBEVMjs.fa)("body")).select("#" + t);
    d.append("g");
    let x = o.db.getTasks(), _ = o.db.getCommonDb().getDiagramTitle();
    (0, _chunkNQURTBEVMjs.b).debug("task", x), H.initGraphics(d);
    let m = o.db.getSections();
    (0, _chunkNQURTBEVMjs.b).debug("sections", m);
    let i = 0, a = 0, c = 0, p = 0, y = 50 + u, l = 50;
    p = 50;
    let E = 0, k = !0;
    m.forEach(function(L) {
        let v = {
            number: E,
            descr: L,
            section: E,
            width: 150,
            padding: 20,
            maxHeight: i
        }, b = H.getVirtualNodeHeight(d, v, r);
        (0, _chunkNQURTBEVMjs.b).debug("sectionHeight before draw", b), i = Math.max(i, b + 20);
    });
    let N = 0, C = 0;
    (0, _chunkNQURTBEVMjs.b).debug("tasks.length", x.length);
    for (let [L, v] of x.entries()){
        let b = {
            number: L,
            descr: v,
            section: v.section,
            width: 150,
            padding: 20,
            maxHeight: a
        }, T = H.getVirtualNodeHeight(d, b, r);
        (0, _chunkNQURTBEVMjs.b).debug("taskHeight before draw", T), a = Math.max(a, T + 20), N = Math.max(N, v.events.length);
        let A = 0;
        for (let P of v.events){
            let U = {
                descr: P,
                section: v.section,
                number: v.section,
                width: 150,
                padding: 20,
                maxHeight: 50
            };
            A += H.getVirtualNodeHeight(d, U, r);
        }
        C = Math.max(C, A);
    }
    (0, _chunkNQURTBEVMjs.b).debug("maxSectionHeight before draw", i), (0, _chunkNQURTBEVMjs.b).debug("maxTaskHeight before draw", a), m && m.length > 0 ? m.forEach((L)=>{
        let v = x.filter((P)=>P.section === L), b = {
            number: E,
            descr: L,
            section: E,
            width: 200 * Math.max(v.length, 1) - 50,
            padding: 20,
            maxHeight: i
        };
        (0, _chunkNQURTBEVMjs.b).debug("sectionNode", b);
        let T = d.append("g"), A = H.drawNode(T, b, E, r);
        (0, _chunkNQURTBEVMjs.b).debug("sectionNode output", A), T.attr("transform", `translate(${y}, ${p})`), l += i + 50, v.length > 0 && Et(d, v, E, y, l, a, r, N, C, i, !1), y += 200 * Math.max(v.length, 1), l = p, E++;
    }) : (k = !1, Et(d, x, E, y, l, a, r, N, C, i, !0));
    let V = d.node().getBBox();
    (0, _chunkNQURTBEVMjs.b).debug("bounds", V), _ && d.append("text").text(_).attr("x", V.width / 2 - u).attr("font-size", "4ex").attr("font-weight", "bold").attr("y", 20), c = k ? i + a + 150 : a + 100, d.append("g").attr("class", "lineWrapper").append("line").attr("x1", u).attr("y1", c).attr("x2", V.width + 3 * u).attr("y2", c).attr("stroke-width", 4).attr("stroke", "black").attr("marker-end", "url(#arrowhead)"), (0, _chunkNQURTBEVMjs.N)(void 0, d, r.timeline?.padding ?? 50, r.timeline?.useMaxWidth ?? !1);
}, "draw"), Et = (0, _chunkGTKDMUJJMjs.a)(function(n, t, e, o, r, u, h, f, g, d, x) {
    for (let _ of t){
        let m = {
            descr: _.task,
            section: e,
            number: e,
            width: 150,
            padding: 20,
            maxHeight: u
        };
        (0, _chunkNQURTBEVMjs.b).debug("taskNode", m);
        let i = n.append("g").attr("class", "taskWrapper"), c = H.drawNode(i, m, e, h).height;
        if ((0, _chunkNQURTBEVMjs.b).debug("taskHeight after draw", c), i.attr("transform", `translate(${o}, ${r})`), u = Math.max(u, c), _.events) {
            let p = n.append("g").attr("class", "lineWrapper"), y = u;
            r += 100, y = y + Gt(n, _.events, e, o, r, h), r -= 100, p.append("line").attr("x1", o + 95).attr("y1", r + u).attr("x2", o + 95).attr("y2", r + u + (x ? u : d) + g + 120).attr("stroke-width", 2).attr("stroke", "black").attr("marker-end", "url(#arrowhead)").attr("stroke-dasharray", "5,5");
        }
        o = o + 200, x && !h.timeline?.disableMulticolor && e++;
    }
    r = r - 10;
}, "drawTasks"), Gt = (0, _chunkGTKDMUJJMjs.a)(function(n, t, e, o, r, u) {
    let h = 0, f = r;
    r = r + 100;
    for (let g of t){
        let d = {
            descr: g,
            section: e,
            number: e,
            width: 150,
            padding: 20,
            maxHeight: 50
        };
        (0, _chunkNQURTBEVMjs.b).debug("eventNode", d);
        let x = n.append("g").attr("class", "eventWrapper"), m = H.drawNode(x, d, e, u).height;
        h = h + m, x.attr("transform", `translate(${o}, ${r})`), r = r + 10 + m;
    }
    return r = f, h;
}, "drawEvents"), Tt = {
    setConf: (0, _chunkGTKDMUJJMjs.a)(()=>{}, "setConf"),
    draw: jt
};
var qt = (0, _chunkGTKDMUJJMjs.a)((n)=>{
    let t = "";
    for(let e = 0; e < n.THEME_COLOR_LIMIT; e++)n["lineColor" + e] = n["lineColor" + e] || n["cScaleInv" + e], (0, _chunkNQURTBEVMjs.n)(n["lineColor" + e]) ? n["lineColor" + e] = (0, _chunkNQURTBEVMjs.o)(n["lineColor" + e], 20) : n["lineColor" + e] = (0, _chunkNQURTBEVMjs.p)(n["lineColor" + e], 20);
    for(let e = 0; e < n.THEME_COLOR_LIMIT; e++){
        let o = "" + (17 - 3 * e);
        t += `
    .section-${e - 1} rect, .section-${e - 1} path, .section-${e - 1} circle, .section-${e - 1} path  {
      fill: ${n["cScale" + e]};
    }
    .section-${e - 1} text {
     fill: ${n["cScaleLabel" + e]};
    }
    .node-icon-${e - 1} {
      font-size: 40px;
      color: ${n["cScaleLabel" + e]};
    }
    .section-edge-${e - 1}{
      stroke: ${n["cScale" + e]};
    }
    .edge-depth-${e - 1}{
      stroke-width: ${o};
    }
    .section-${e - 1} line {
      stroke: ${n["cScaleInv" + e]} ;
      stroke-width: 3;
    }

    .lineWrapper line{
      stroke: ${n["cScaleLabel" + e]} ;
    }

    .disabled, .disabled circle, .disabled text {
      fill: lightgray;
    }
    .disabled text {
      fill: #efefef;
    }
    `;
    }
    return t;
}, "genSections"), Ut = (0, _chunkGTKDMUJJMjs.a)((n)=>`
  .edge {
    stroke-width: 3;
  }
  ${qt(n)}
  .section-root rect, .section-root path, .section-root circle  {
    fill: ${n.git0};
  }
  .section-root text {
    fill: ${n.gitBranchLabel0};
  }
  .icon-container {
    height:100%;
    display: flex;
    justify-content: center;
    align-items: center;
  }
  .edge {
    fill: none;
  }
  .eventWrapper  {
   filter: brightness(120%);
  }
`, "getStyles"), It = Ut;
var ye = {
    db: D,
    renderer: Tt,
    parser: ht,
    styles: It
};

},{"./chunk-NQURTBEV.mjs":"iASFe","./chunk-GTKDMUJJ.mjs":"fruhx","@parcel/transformer-js/src/esmodule-helpers.js":"1cdyk"}]},["3f5Sx"], null, "parcelRequire6955", {})

//# sourceMappingURL=timeline-definition-MHTE3MCH.7db4448d.js.map
