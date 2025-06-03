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
})({"fiXCT":[function(require,module,exports,__globalThis) {
var global = arguments[3];
var HMR_HOST = null;
var HMR_PORT = 1234;
var HMR_SERVER_PORT = 1234;
var HMR_SECURE = false;
var HMR_ENV_HASH = "febfef3c5b467b16";
var HMR_USE_SSE = false;
module.bundle.HMR_BUNDLE_ID = "0c2cd4ddf9b455da";
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

},{}],"bBvyn":[function(require,module,exports,__globalThis) {
var parcelHelpers = require("@parcel/transformer-js/src/esmodule-helpers.js");
parcelHelpers.defineInteropFlag(exports);
parcelHelpers.export(exports, "a", ()=>jt);
parcelHelpers.export(exports, "b", ()=>as);
parcelHelpers.export(exports, "c", ()=>os);
var _chunkAC3VT7B7Mjs = require("./chunk-AC3VT7B7.mjs");
var _chunkNQURTBEVMjs = require("./chunk-NQURTBEV.mjs");
var _chunkGTKDMUJJMjs = require("./chunk-GTKDMUJJ.mjs");
var Ge = function() {
    var e = (0, _chunkGTKDMUJJMjs.a)(function(O, a, l, p) {
        for(l = l || {}, p = O.length; p--; l[O[p]] = a);
        return l;
    }, "o"), i = [
        1,
        17
    ], u = [
        1,
        18
    ], h = [
        1,
        19
    ], o = [
        1,
        39
    ], f = [
        1,
        40
    ], g = [
        1,
        25
    ], _ = [
        1,
        23
    ], S = [
        1,
        24
    ], x = [
        1,
        31
    ], be = [
        1,
        32
    ], ke = [
        1,
        33
    ], me = [
        1,
        34
    ], Ce = [
        1,
        35
    ], Ee = [
        1,
        36
    ], ye = [
        1,
        26
    ], Te = [
        1,
        27
    ], Fe = [
        1,
        28
    ], De = [
        1,
        29
    ], b = [
        1,
        43
    ], Be = [
        1,
        30
    ], k = [
        1,
        42
    ], m = [
        1,
        44
    ], C = [
        1,
        41
    ], F = [
        1,
        45
    ], _e = [
        1,
        9
    ], c = [
        1,
        8,
        9
    ], j = [
        1,
        56
    ], X = [
        1,
        57
    ], H = [
        1,
        58
    ], W = [
        1,
        59
    ], q = [
        1,
        60
    ], Se = [
        1,
        61
    ], Ne = [
        1,
        62
    ], J = [
        1,
        8,
        9,
        39
    ], Qe = [
        1,
        74
    ], U = [
        1,
        8,
        9,
        12,
        13,
        21,
        37,
        39,
        42,
        59,
        60,
        61,
        62,
        63,
        64,
        65,
        70,
        72
    ], Z = [
        1,
        8,
        9,
        12,
        13,
        19,
        21,
        37,
        39,
        42,
        46,
        59,
        60,
        61,
        62,
        63,
        64,
        65,
        70,
        72,
        74,
        80,
        95,
        97,
        98
    ], $ = [
        13,
        74,
        80,
        95,
        97,
        98
    ], z = [
        13,
        64,
        65,
        74,
        80,
        95,
        97,
        98
    ], je = [
        13,
        59,
        60,
        61,
        62,
        63,
        74,
        80,
        95,
        97,
        98
    ], xe = [
        1,
        93
    ], ee = [
        1,
        110
    ], te = [
        1,
        108
    ], se = [
        1,
        102
    ], ie = [
        1,
        103
    ], re = [
        1,
        104
    ], ne = [
        1,
        105
    ], ae = [
        1,
        106
    ], ue = [
        1,
        107
    ], le = [
        1,
        109
    ], Le = [
        1,
        8,
        9,
        37,
        39,
        42
    ], oe = [
        1,
        8,
        9,
        21
    ], Xe = [
        1,
        8,
        9,
        78
    ], N = [
        1,
        8,
        9,
        21,
        73,
        74,
        78,
        80,
        81,
        82,
        83,
        84,
        85
    ], ve = {
        trace: (0, _chunkGTKDMUJJMjs.a)(function() {}, "trace"),
        yy: {},
        symbols_: {
            error: 2,
            start: 3,
            mermaidDoc: 4,
            statements: 5,
            graphConfig: 6,
            CLASS_DIAGRAM: 7,
            NEWLINE: 8,
            EOF: 9,
            statement: 10,
            classLabel: 11,
            SQS: 12,
            STR: 13,
            SQE: 14,
            namespaceName: 15,
            alphaNumToken: 16,
            className: 17,
            classLiteralName: 18,
            GENERICTYPE: 19,
            relationStatement: 20,
            LABEL: 21,
            namespaceStatement: 22,
            classStatement: 23,
            memberStatement: 24,
            annotationStatement: 25,
            clickStatement: 26,
            styleStatement: 27,
            cssClassStatement: 28,
            noteStatement: 29,
            direction: 30,
            acc_title: 31,
            acc_title_value: 32,
            acc_descr: 33,
            acc_descr_value: 34,
            acc_descr_multiline_value: 35,
            namespaceIdentifier: 36,
            STRUCT_START: 37,
            classStatements: 38,
            STRUCT_STOP: 39,
            NAMESPACE: 40,
            classIdentifier: 41,
            STYLE_SEPARATOR: 42,
            members: 43,
            CLASS: 44,
            ANNOTATION_START: 45,
            ANNOTATION_END: 46,
            MEMBER: 47,
            SEPARATOR: 48,
            relation: 49,
            NOTE_FOR: 50,
            noteText: 51,
            NOTE: 52,
            direction_tb: 53,
            direction_bt: 54,
            direction_rl: 55,
            direction_lr: 56,
            relationType: 57,
            lineType: 58,
            AGGREGATION: 59,
            EXTENSION: 60,
            COMPOSITION: 61,
            DEPENDENCY: 62,
            LOLLIPOP: 63,
            LINE: 64,
            DOTTED_LINE: 65,
            CALLBACK: 66,
            LINK: 67,
            LINK_TARGET: 68,
            CLICK: 69,
            CALLBACK_NAME: 70,
            CALLBACK_ARGS: 71,
            HREF: 72,
            STYLE: 73,
            ALPHA: 74,
            stylesOpt: 75,
            CSSCLASS: 76,
            style: 77,
            COMMA: 78,
            styleComponent: 79,
            NUM: 80,
            COLON: 81,
            UNIT: 82,
            SPACE: 83,
            BRKT: 84,
            PCT: 85,
            commentToken: 86,
            textToken: 87,
            graphCodeTokens: 88,
            textNoTagsToken: 89,
            TAGSTART: 90,
            TAGEND: 91,
            "==": 92,
            "--": 93,
            DEFAULT: 94,
            MINUS: 95,
            keywords: 96,
            UNICODE_TEXT: 97,
            BQUOTE_STR: 98,
            $accept: 0,
            $end: 1
        },
        terminals_: {
            2: "error",
            7: "CLASS_DIAGRAM",
            8: "NEWLINE",
            9: "EOF",
            12: "SQS",
            13: "STR",
            14: "SQE",
            19: "GENERICTYPE",
            21: "LABEL",
            31: "acc_title",
            32: "acc_title_value",
            33: "acc_descr",
            34: "acc_descr_value",
            35: "acc_descr_multiline_value",
            37: "STRUCT_START",
            39: "STRUCT_STOP",
            40: "NAMESPACE",
            42: "STYLE_SEPARATOR",
            44: "CLASS",
            45: "ANNOTATION_START",
            46: "ANNOTATION_END",
            47: "MEMBER",
            48: "SEPARATOR",
            50: "NOTE_FOR",
            52: "NOTE",
            53: "direction_tb",
            54: "direction_bt",
            55: "direction_rl",
            56: "direction_lr",
            59: "AGGREGATION",
            60: "EXTENSION",
            61: "COMPOSITION",
            62: "DEPENDENCY",
            63: "LOLLIPOP",
            64: "LINE",
            65: "DOTTED_LINE",
            66: "CALLBACK",
            67: "LINK",
            68: "LINK_TARGET",
            69: "CLICK",
            70: "CALLBACK_NAME",
            71: "CALLBACK_ARGS",
            72: "HREF",
            73: "STYLE",
            74: "ALPHA",
            76: "CSSCLASS",
            78: "COMMA",
            80: "NUM",
            81: "COLON",
            82: "UNIT",
            83: "SPACE",
            84: "BRKT",
            85: "PCT",
            88: "graphCodeTokens",
            90: "TAGSTART",
            91: "TAGEND",
            92: "==",
            93: "--",
            94: "DEFAULT",
            95: "MINUS",
            96: "keywords",
            97: "UNICODE_TEXT",
            98: "BQUOTE_STR"
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
                4,
                1
            ],
            [
                6,
                4
            ],
            [
                5,
                1
            ],
            [
                5,
                2
            ],
            [
                5,
                3
            ],
            [
                11,
                3
            ],
            [
                15,
                1
            ],
            [
                15,
                2
            ],
            [
                17,
                1
            ],
            [
                17,
                1
            ],
            [
                17,
                2
            ],
            [
                17,
                2
            ],
            [
                17,
                2
            ],
            [
                10,
                1
            ],
            [
                10,
                2
            ],
            [
                10,
                1
            ],
            [
                10,
                1
            ],
            [
                10,
                1
            ],
            [
                10,
                1
            ],
            [
                10,
                1
            ],
            [
                10,
                1
            ],
            [
                10,
                1
            ],
            [
                10,
                1
            ],
            [
                10,
                1
            ],
            [
                10,
                2
            ],
            [
                10,
                2
            ],
            [
                10,
                1
            ],
            [
                22,
                4
            ],
            [
                22,
                5
            ],
            [
                36,
                2
            ],
            [
                38,
                1
            ],
            [
                38,
                2
            ],
            [
                38,
                3
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
                23,
                4
            ],
            [
                23,
                6
            ],
            [
                41,
                2
            ],
            [
                41,
                3
            ],
            [
                25,
                4
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
                24,
                1
            ],
            [
                24,
                2
            ],
            [
                24,
                1
            ],
            [
                24,
                1
            ],
            [
                20,
                3
            ],
            [
                20,
                4
            ],
            [
                20,
                4
            ],
            [
                20,
                5
            ],
            [
                29,
                3
            ],
            [
                29,
                2
            ],
            [
                30,
                1
            ],
            [
                30,
                1
            ],
            [
                30,
                1
            ],
            [
                30,
                1
            ],
            [
                49,
                3
            ],
            [
                49,
                2
            ],
            [
                49,
                2
            ],
            [
                49,
                1
            ],
            [
                57,
                1
            ],
            [
                57,
                1
            ],
            [
                57,
                1
            ],
            [
                57,
                1
            ],
            [
                57,
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
                26,
                3
            ],
            [
                26,
                4
            ],
            [
                26,
                3
            ],
            [
                26,
                4
            ],
            [
                26,
                4
            ],
            [
                26,
                5
            ],
            [
                26,
                3
            ],
            [
                26,
                4
            ],
            [
                26,
                4
            ],
            [
                26,
                5
            ],
            [
                26,
                4
            ],
            [
                26,
                5
            ],
            [
                26,
                5
            ],
            [
                26,
                6
            ],
            [
                27,
                3
            ],
            [
                28,
                3
            ],
            [
                75,
                1
            ],
            [
                75,
                3
            ],
            [
                77,
                1
            ],
            [
                77,
                2
            ],
            [
                79,
                1
            ],
            [
                79,
                1
            ],
            [
                79,
                1
            ],
            [
                79,
                1
            ],
            [
                79,
                1
            ],
            [
                79,
                1
            ],
            [
                79,
                1
            ],
            [
                79,
                1
            ],
            [
                79,
                1
            ],
            [
                86,
                1
            ],
            [
                86,
                1
            ],
            [
                87,
                1
            ],
            [
                87,
                1
            ],
            [
                87,
                1
            ],
            [
                87,
                1
            ],
            [
                87,
                1
            ],
            [
                87,
                1
            ],
            [
                87,
                1
            ],
            [
                89,
                1
            ],
            [
                89,
                1
            ],
            [
                89,
                1
            ],
            [
                89,
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
                18,
                1
            ],
            [
                51,
                1
            ]
        ],
        performAction: (0, _chunkGTKDMUJJMjs.a)(function(a, l, p, r, A, t, K) {
            var s = t.length - 1;
            switch(A){
                case 8:
                    this.$ = t[s - 1];
                    break;
                case 9:
                case 11:
                case 12:
                    this.$ = t[s];
                    break;
                case 10:
                case 13:
                    this.$ = t[s - 1] + t[s];
                    break;
                case 14:
                case 15:
                    this.$ = t[s - 1] + "~" + t[s] + "~";
                    break;
                case 16:
                    r.addRelation(t[s]);
                    break;
                case 17:
                    t[s - 1].title = r.cleanupLabel(t[s]), r.addRelation(t[s - 1]);
                    break;
                case 27:
                    this.$ = t[s].trim(), r.setAccTitle(this.$);
                    break;
                case 28:
                case 29:
                    this.$ = t[s].trim(), r.setAccDescription(this.$);
                    break;
                case 30:
                    r.addClassesToNamespace(t[s - 3], t[s - 1]);
                    break;
                case 31:
                    r.addClassesToNamespace(t[s - 4], t[s - 1]);
                    break;
                case 32:
                    this.$ = t[s], r.addNamespace(t[s]);
                    break;
                case 33:
                    this.$ = [
                        t[s]
                    ];
                    break;
                case 34:
                    this.$ = [
                        t[s - 1]
                    ];
                    break;
                case 35:
                    t[s].unshift(t[s - 2]), this.$ = t[s];
                    break;
                case 37:
                    r.setCssClass(t[s - 2], t[s]);
                    break;
                case 38:
                    r.addMembers(t[s - 3], t[s - 1]);
                    break;
                case 39:
                    r.setCssClass(t[s - 5], t[s - 3]), r.addMembers(t[s - 5], t[s - 1]);
                    break;
                case 40:
                    this.$ = t[s], r.addClass(t[s]);
                    break;
                case 41:
                    this.$ = t[s - 1], r.addClass(t[s - 1]), r.setClassLabel(t[s - 1], t[s]);
                    break;
                case 42:
                    r.addAnnotation(t[s], t[s - 2]);
                    break;
                case 43:
                    this.$ = [
                        t[s]
                    ];
                    break;
                case 44:
                    t[s].push(t[s - 1]), this.$ = t[s];
                    break;
                case 45:
                    break;
                case 46:
                    r.addMember(t[s - 1], r.cleanupLabel(t[s]));
                    break;
                case 47:
                    break;
                case 48:
                    break;
                case 49:
                    this.$ = {
                        id1: t[s - 2],
                        id2: t[s],
                        relation: t[s - 1],
                        relationTitle1: "none",
                        relationTitle2: "none"
                    };
                    break;
                case 50:
                    this.$ = {
                        id1: t[s - 3],
                        id2: t[s],
                        relation: t[s - 1],
                        relationTitle1: t[s - 2],
                        relationTitle2: "none"
                    };
                    break;
                case 51:
                    this.$ = {
                        id1: t[s - 3],
                        id2: t[s],
                        relation: t[s - 2],
                        relationTitle1: "none",
                        relationTitle2: t[s - 1]
                    };
                    break;
                case 52:
                    this.$ = {
                        id1: t[s - 4],
                        id2: t[s],
                        relation: t[s - 2],
                        relationTitle1: t[s - 3],
                        relationTitle2: t[s - 1]
                    };
                    break;
                case 53:
                    r.addNote(t[s], t[s - 1]);
                    break;
                case 54:
                    r.addNote(t[s]);
                    break;
                case 55:
                    r.setDirection("TB");
                    break;
                case 56:
                    r.setDirection("BT");
                    break;
                case 57:
                    r.setDirection("RL");
                    break;
                case 58:
                    r.setDirection("LR");
                    break;
                case 59:
                    this.$ = {
                        type1: t[s - 2],
                        type2: t[s],
                        lineType: t[s - 1]
                    };
                    break;
                case 60:
                    this.$ = {
                        type1: "none",
                        type2: t[s],
                        lineType: t[s - 1]
                    };
                    break;
                case 61:
                    this.$ = {
                        type1: t[s - 1],
                        type2: "none",
                        lineType: t[s]
                    };
                    break;
                case 62:
                    this.$ = {
                        type1: "none",
                        type2: "none",
                        lineType: t[s]
                    };
                    break;
                case 63:
                    this.$ = r.relationType.AGGREGATION;
                    break;
                case 64:
                    this.$ = r.relationType.EXTENSION;
                    break;
                case 65:
                    this.$ = r.relationType.COMPOSITION;
                    break;
                case 66:
                    this.$ = r.relationType.DEPENDENCY;
                    break;
                case 67:
                    this.$ = r.relationType.LOLLIPOP;
                    break;
                case 68:
                    this.$ = r.lineType.LINE;
                    break;
                case 69:
                    this.$ = r.lineType.DOTTED_LINE;
                    break;
                case 70:
                case 76:
                    this.$ = t[s - 2], r.setClickEvent(t[s - 1], t[s]);
                    break;
                case 71:
                case 77:
                    this.$ = t[s - 3], r.setClickEvent(t[s - 2], t[s - 1]), r.setTooltip(t[s - 2], t[s]);
                    break;
                case 72:
                    this.$ = t[s - 2], r.setLink(t[s - 1], t[s]);
                    break;
                case 73:
                    this.$ = t[s - 3], r.setLink(t[s - 2], t[s - 1], t[s]);
                    break;
                case 74:
                    this.$ = t[s - 3], r.setLink(t[s - 2], t[s - 1]), r.setTooltip(t[s - 2], t[s]);
                    break;
                case 75:
                    this.$ = t[s - 4], r.setLink(t[s - 3], t[s - 2], t[s]), r.setTooltip(t[s - 3], t[s - 1]);
                    break;
                case 78:
                    this.$ = t[s - 3], r.setClickEvent(t[s - 2], t[s - 1], t[s]);
                    break;
                case 79:
                    this.$ = t[s - 4], r.setClickEvent(t[s - 3], t[s - 2], t[s - 1]), r.setTooltip(t[s - 3], t[s]);
                    break;
                case 80:
                    this.$ = t[s - 3], r.setLink(t[s - 2], t[s]);
                    break;
                case 81:
                    this.$ = t[s - 4], r.setLink(t[s - 3], t[s - 1], t[s]);
                    break;
                case 82:
                    this.$ = t[s - 4], r.setLink(t[s - 3], t[s - 1]), r.setTooltip(t[s - 3], t[s]);
                    break;
                case 83:
                    this.$ = t[s - 5], r.setLink(t[s - 4], t[s - 2], t[s]), r.setTooltip(t[s - 4], t[s - 1]);
                    break;
                case 84:
                    this.$ = t[s - 2], r.setCssStyle(t[s - 1], t[s]);
                    break;
                case 85:
                    r.setCssClass(t[s - 1], t[s]);
                    break;
                case 86:
                    this.$ = [
                        t[s]
                    ];
                    break;
                case 87:
                    t[s - 2].push(t[s]), this.$ = t[s - 2];
                    break;
                case 89:
                    this.$ = t[s - 1] + t[s];
                    break;
            }
        }, "anonymous"),
        table: [
            {
                3: 1,
                4: 2,
                5: 3,
                6: 4,
                7: [
                    1,
                    6
                ],
                10: 5,
                16: 37,
                17: 20,
                18: 38,
                20: 7,
                22: 8,
                23: 9,
                24: 10,
                25: 11,
                26: 12,
                27: 13,
                28: 14,
                29: 15,
                30: 16,
                31: i,
                33: u,
                35: h,
                36: 21,
                40: o,
                41: 22,
                44: f,
                45: g,
                47: _,
                48: S,
                50: x,
                52: be,
                53: ke,
                54: me,
                55: Ce,
                56: Ee,
                66: ye,
                67: Te,
                69: Fe,
                73: De,
                74: b,
                76: Be,
                80: k,
                95: m,
                97: C,
                98: F
            },
            {
                1: [
                    3
                ]
            },
            {
                1: [
                    2,
                    1
                ]
            },
            {
                1: [
                    2,
                    2
                ]
            },
            {
                1: [
                    2,
                    3
                ]
            },
            e(_e, [
                2,
                5
            ], {
                8: [
                    1,
                    46
                ]
            }),
            {
                8: [
                    1,
                    47
                ]
            },
            e(c, [
                2,
                16
            ], {
                21: [
                    1,
                    48
                ]
            }),
            e(c, [
                2,
                18
            ]),
            e(c, [
                2,
                19
            ]),
            e(c, [
                2,
                20
            ]),
            e(c, [
                2,
                21
            ]),
            e(c, [
                2,
                22
            ]),
            e(c, [
                2,
                23
            ]),
            e(c, [
                2,
                24
            ]),
            e(c, [
                2,
                25
            ]),
            e(c, [
                2,
                26
            ]),
            {
                32: [
                    1,
                    49
                ]
            },
            {
                34: [
                    1,
                    50
                ]
            },
            e(c, [
                2,
                29
            ]),
            e(c, [
                2,
                45
            ], {
                49: 51,
                57: 54,
                58: 55,
                13: [
                    1,
                    52
                ],
                21: [
                    1,
                    53
                ],
                59: j,
                60: X,
                61: H,
                62: W,
                63: q,
                64: Se,
                65: Ne
            }),
            {
                37: [
                    1,
                    63
                ]
            },
            e(J, [
                2,
                36
            ], {
                37: [
                    1,
                    65
                ],
                42: [
                    1,
                    64
                ]
            }),
            e(c, [
                2,
                47
            ]),
            e(c, [
                2,
                48
            ]),
            {
                16: 66,
                74: b,
                80: k,
                95: m,
                97: C
            },
            {
                16: 37,
                17: 67,
                18: 38,
                74: b,
                80: k,
                95: m,
                97: C,
                98: F
            },
            {
                16: 37,
                17: 68,
                18: 38,
                74: b,
                80: k,
                95: m,
                97: C,
                98: F
            },
            {
                16: 37,
                17: 69,
                18: 38,
                74: b,
                80: k,
                95: m,
                97: C,
                98: F
            },
            {
                74: [
                    1,
                    70
                ]
            },
            {
                13: [
                    1,
                    71
                ]
            },
            {
                16: 37,
                17: 72,
                18: 38,
                74: b,
                80: k,
                95: m,
                97: C,
                98: F
            },
            {
                13: Qe,
                51: 73
            },
            e(c, [
                2,
                55
            ]),
            e(c, [
                2,
                56
            ]),
            e(c, [
                2,
                57
            ]),
            e(c, [
                2,
                58
            ]),
            e(U, [
                2,
                11
            ], {
                16: 37,
                18: 38,
                17: 75,
                19: [
                    1,
                    76
                ],
                74: b,
                80: k,
                95: m,
                97: C,
                98: F
            }),
            e(U, [
                2,
                12
            ], {
                19: [
                    1,
                    77
                ]
            }),
            {
                15: 78,
                16: 79,
                74: b,
                80: k,
                95: m,
                97: C
            },
            {
                16: 37,
                17: 80,
                18: 38,
                74: b,
                80: k,
                95: m,
                97: C,
                98: F
            },
            e(Z, [
                2,
                112
            ]),
            e(Z, [
                2,
                113
            ]),
            e(Z, [
                2,
                114
            ]),
            e(Z, [
                2,
                115
            ]),
            e([
                1,
                8,
                9,
                12,
                13,
                19,
                21,
                37,
                39,
                42,
                59,
                60,
                61,
                62,
                63,
                64,
                65,
                70,
                72
            ], [
                2,
                116
            ]),
            e(_e, [
                2,
                6
            ], {
                10: 5,
                20: 7,
                22: 8,
                23: 9,
                24: 10,
                25: 11,
                26: 12,
                27: 13,
                28: 14,
                29: 15,
                30: 16,
                17: 20,
                36: 21,
                41: 22,
                16: 37,
                18: 38,
                5: 81,
                31: i,
                33: u,
                35: h,
                40: o,
                44: f,
                45: g,
                47: _,
                48: S,
                50: x,
                52: be,
                53: ke,
                54: me,
                55: Ce,
                56: Ee,
                66: ye,
                67: Te,
                69: Fe,
                73: De,
                74: b,
                76: Be,
                80: k,
                95: m,
                97: C,
                98: F
            }),
            {
                5: 82,
                10: 5,
                16: 37,
                17: 20,
                18: 38,
                20: 7,
                22: 8,
                23: 9,
                24: 10,
                25: 11,
                26: 12,
                27: 13,
                28: 14,
                29: 15,
                30: 16,
                31: i,
                33: u,
                35: h,
                36: 21,
                40: o,
                41: 22,
                44: f,
                45: g,
                47: _,
                48: S,
                50: x,
                52: be,
                53: ke,
                54: me,
                55: Ce,
                56: Ee,
                66: ye,
                67: Te,
                69: Fe,
                73: De,
                74: b,
                76: Be,
                80: k,
                95: m,
                97: C,
                98: F
            },
            e(c, [
                2,
                17
            ]),
            e(c, [
                2,
                27
            ]),
            e(c, [
                2,
                28
            ]),
            {
                13: [
                    1,
                    84
                ],
                16: 37,
                17: 83,
                18: 38,
                74: b,
                80: k,
                95: m,
                97: C,
                98: F
            },
            {
                49: 85,
                57: 54,
                58: 55,
                59: j,
                60: X,
                61: H,
                62: W,
                63: q,
                64: Se,
                65: Ne
            },
            e(c, [
                2,
                46
            ]),
            {
                58: 86,
                64: Se,
                65: Ne
            },
            e($, [
                2,
                62
            ], {
                57: 87,
                59: j,
                60: X,
                61: H,
                62: W,
                63: q
            }),
            e(z, [
                2,
                63
            ]),
            e(z, [
                2,
                64
            ]),
            e(z, [
                2,
                65
            ]),
            e(z, [
                2,
                66
            ]),
            e(z, [
                2,
                67
            ]),
            e(je, [
                2,
                68
            ]),
            e(je, [
                2,
                69
            ]),
            {
                8: [
                    1,
                    89
                ],
                23: 90,
                38: 88,
                41: 22,
                44: f
            },
            {
                16: 91,
                74: b,
                80: k,
                95: m,
                97: C
            },
            {
                43: 92,
                47: xe
            },
            {
                46: [
                    1,
                    94
                ]
            },
            {
                13: [
                    1,
                    95
                ]
            },
            {
                13: [
                    1,
                    96
                ]
            },
            {
                70: [
                    1,
                    97
                ],
                72: [
                    1,
                    98
                ]
            },
            {
                21: ee,
                73: te,
                74: se,
                75: 99,
                77: 100,
                79: 101,
                80: ie,
                81: re,
                82: ne,
                83: ae,
                84: ue,
                85: le
            },
            {
                74: [
                    1,
                    111
                ]
            },
            {
                13: Qe,
                51: 112
            },
            e(c, [
                2,
                54
            ]),
            e(c, [
                2,
                117
            ]),
            e(U, [
                2,
                13
            ]),
            e(U, [
                2,
                14
            ]),
            e(U, [
                2,
                15
            ]),
            {
                37: [
                    2,
                    32
                ]
            },
            {
                15: 113,
                16: 79,
                37: [
                    2,
                    9
                ],
                74: b,
                80: k,
                95: m,
                97: C
            },
            e(Le, [
                2,
                40
            ], {
                11: 114,
                12: [
                    1,
                    115
                ]
            }),
            e(_e, [
                2,
                7
            ]),
            {
                9: [
                    1,
                    116
                ]
            },
            e(oe, [
                2,
                49
            ]),
            {
                16: 37,
                17: 117,
                18: 38,
                74: b,
                80: k,
                95: m,
                97: C,
                98: F
            },
            {
                13: [
                    1,
                    119
                ],
                16: 37,
                17: 118,
                18: 38,
                74: b,
                80: k,
                95: m,
                97: C,
                98: F
            },
            e($, [
                2,
                61
            ], {
                57: 120,
                59: j,
                60: X,
                61: H,
                62: W,
                63: q
            }),
            e($, [
                2,
                60
            ]),
            {
                39: [
                    1,
                    121
                ]
            },
            {
                23: 90,
                38: 122,
                41: 22,
                44: f
            },
            {
                8: [
                    1,
                    123
                ],
                39: [
                    2,
                    33
                ]
            },
            e(J, [
                2,
                37
            ], {
                37: [
                    1,
                    124
                ]
            }),
            {
                39: [
                    1,
                    125
                ]
            },
            {
                39: [
                    2,
                    43
                ],
                43: 126,
                47: xe
            },
            {
                16: 37,
                17: 127,
                18: 38,
                74: b,
                80: k,
                95: m,
                97: C,
                98: F
            },
            e(c, [
                2,
                70
            ], {
                13: [
                    1,
                    128
                ]
            }),
            e(c, [
                2,
                72
            ], {
                13: [
                    1,
                    130
                ],
                68: [
                    1,
                    129
                ]
            }),
            e(c, [
                2,
                76
            ], {
                13: [
                    1,
                    131
                ],
                71: [
                    1,
                    132
                ]
            }),
            {
                13: [
                    1,
                    133
                ]
            },
            e(c, [
                2,
                84
            ], {
                78: [
                    1,
                    134
                ]
            }),
            e(Xe, [
                2,
                86
            ], {
                79: 135,
                21: ee,
                73: te,
                74: se,
                80: ie,
                81: re,
                82: ne,
                83: ae,
                84: ue,
                85: le
            }),
            e(N, [
                2,
                88
            ]),
            e(N, [
                2,
                90
            ]),
            e(N, [
                2,
                91
            ]),
            e(N, [
                2,
                92
            ]),
            e(N, [
                2,
                93
            ]),
            e(N, [
                2,
                94
            ]),
            e(N, [
                2,
                95
            ]),
            e(N, [
                2,
                96
            ]),
            e(N, [
                2,
                97
            ]),
            e(N, [
                2,
                98
            ]),
            e(c, [
                2,
                85
            ]),
            e(c, [
                2,
                53
            ]),
            {
                37: [
                    2,
                    10
                ]
            },
            e(Le, [
                2,
                41
            ]),
            {
                13: [
                    1,
                    136
                ]
            },
            {
                1: [
                    2,
                    4
                ]
            },
            e(oe, [
                2,
                51
            ]),
            e(oe, [
                2,
                50
            ]),
            {
                16: 37,
                17: 137,
                18: 38,
                74: b,
                80: k,
                95: m,
                97: C,
                98: F
            },
            e($, [
                2,
                59
            ]),
            e(c, [
                2,
                30
            ]),
            {
                39: [
                    1,
                    138
                ]
            },
            {
                23: 90,
                38: 139,
                39: [
                    2,
                    34
                ],
                41: 22,
                44: f
            },
            {
                43: 140,
                47: xe
            },
            e(J, [
                2,
                38
            ]),
            {
                39: [
                    2,
                    44
                ]
            },
            e(c, [
                2,
                42
            ]),
            e(c, [
                2,
                71
            ]),
            e(c, [
                2,
                73
            ]),
            e(c, [
                2,
                74
            ], {
                68: [
                    1,
                    141
                ]
            }),
            e(c, [
                2,
                77
            ]),
            e(c, [
                2,
                78
            ], {
                13: [
                    1,
                    142
                ]
            }),
            e(c, [
                2,
                80
            ], {
                13: [
                    1,
                    144
                ],
                68: [
                    1,
                    143
                ]
            }),
            {
                21: ee,
                73: te,
                74: se,
                77: 145,
                79: 101,
                80: ie,
                81: re,
                82: ne,
                83: ae,
                84: ue,
                85: le
            },
            e(N, [
                2,
                89
            ]),
            {
                14: [
                    1,
                    146
                ]
            },
            e(oe, [
                2,
                52
            ]),
            e(c, [
                2,
                31
            ]),
            {
                39: [
                    2,
                    35
                ]
            },
            {
                39: [
                    1,
                    147
                ]
            },
            e(c, [
                2,
                75
            ]),
            e(c, [
                2,
                79
            ]),
            e(c, [
                2,
                81
            ]),
            e(c, [
                2,
                82
            ], {
                68: [
                    1,
                    148
                ]
            }),
            e(Xe, [
                2,
                87
            ], {
                79: 135,
                21: ee,
                73: te,
                74: se,
                80: ie,
                81: re,
                82: ne,
                83: ae,
                84: ue,
                85: le
            }),
            e(Le, [
                2,
                8
            ]),
            e(J, [
                2,
                39
            ]),
            e(c, [
                2,
                83
            ])
        ],
        defaultActions: {
            2: [
                2,
                1
            ],
            3: [
                2,
                2
            ],
            4: [
                2,
                3
            ],
            78: [
                2,
                32
            ],
            113: [
                2,
                10
            ],
            116: [
                2,
                4
            ],
            126: [
                2,
                44
            ],
            139: [
                2,
                35
            ]
        },
        parseError: (0, _chunkGTKDMUJJMjs.a)(function(a, l) {
            if (l.recoverable) this.trace(a);
            else {
                var p = new Error(a);
                throw p.hash = l, p;
            }
        }, "parseError"),
        parse: (0, _chunkGTKDMUJJMjs.a)(function(a) {
            var l = this, p = [
                0
            ], r = [], A = [
                null
            ], t = [], K = this.table, s = "", ce = 0, He = 0, We = 0, At = 2, qe = 1, ft = t.slice.call(arguments, 1), E = Object.create(this.lexer), R = {
                yy: {}
            };
            for(var Oe in this.yy)Object.prototype.hasOwnProperty.call(this.yy, Oe) && (R.yy[Oe] = this.yy[Oe]);
            E.setInput(a, R.yy), R.yy.lexer = E, R.yy.parser = this, typeof E.yylloc > "u" && (E.yylloc = {});
            var Re = E.yylloc;
            t.push(Re);
            var gt = E.options && E.options.ranges;
            typeof R.yy.parseError == "function" ? this.parseError = R.yy.parseError : this.parseError = Object.getPrototypeOf(this).parseError;
            function Yt(D) {
                p.length = p.length - 2 * D, A.length = A.length - D, t.length = t.length - D;
            }
            (0, _chunkGTKDMUJJMjs.a)(Yt, "popStack");
            function dt() {
                var D;
                return D = r.pop() || E.lex() || qe, typeof D != "number" && (D instanceof Array && (r = D, D = r.pop()), D = l.symbols_[D] || D), D;
            }
            (0, _chunkGTKDMUJJMjs.a)(dt, "lex");
            for(var y, Ve, V, B, Qt, we, P = {}, he, v, Je, pe;;){
                if (V = p[p.length - 1], this.defaultActions[V] ? B = this.defaultActions[V] : ((y === null || typeof y > "u") && (y = dt()), B = K[V] && K[V][y]), typeof B > "u" || !B.length || !B[0]) {
                    var Me = "";
                    pe = [];
                    for(he in K[V])this.terminals_[he] && he > At && pe.push("'" + this.terminals_[he] + "'");
                    E.showPosition ? Me = "Parse error on line " + (ce + 1) + `:
` + E.showPosition() + `
Expecting ` + pe.join(", ") + ", got '" + (this.terminals_[y] || y) + "'" : Me = "Parse error on line " + (ce + 1) + ": Unexpected " + (y == qe ? "end of input" : "'" + (this.terminals_[y] || y) + "'"), this.parseError(Me, {
                        text: E.match,
                        token: this.terminals_[y] || y,
                        line: E.yylineno,
                        loc: Re,
                        expected: pe
                    });
                }
                if (B[0] instanceof Array && B.length > 1) throw new Error("Parse Error: multiple actions possible at state: " + V + ", token: " + y);
                switch(B[0]){
                    case 1:
                        p.push(y), A.push(E.yytext), t.push(E.yylloc), p.push(B[1]), y = null, Ve ? (y = Ve, Ve = null) : (He = E.yyleng, s = E.yytext, ce = E.yylineno, Re = E.yylloc, We > 0 && We--);
                        break;
                    case 2:
                        if (v = this.productions_[B[1]][1], P.$ = A[A.length - v], P._$ = {
                            first_line: t[t.length - (v || 1)].first_line,
                            last_line: t[t.length - 1].last_line,
                            first_column: t[t.length - (v || 1)].first_column,
                            last_column: t[t.length - 1].last_column
                        }, gt && (P._$.range = [
                            t[t.length - (v || 1)].range[0],
                            t[t.length - 1].range[1]
                        ]), we = this.performAction.apply(P, [
                            s,
                            He,
                            ce,
                            R.yy,
                            B[1],
                            A,
                            t
                        ].concat(ft)), typeof we < "u") return we;
                        v && (p = p.slice(0, -1 * v * 2), A = A.slice(0, -1 * v), t = t.slice(0, -1 * v)), p.push(this.productions_[B[1]][0]), A.push(P.$), t.push(P._$), Je = K[p[p.length - 2]][p[p.length - 1]], p.push(Je);
                        break;
                    case 3:
                        return !0;
                }
            }
            return !0;
        }, "parse")
    }, pt = function() {
        var O = {
            EOF: 1,
            parseError: (0, _chunkGTKDMUJJMjs.a)(function(l, p) {
                if (this.yy.parser) this.yy.parser.parseError(l, p);
                else throw new Error(l);
            }, "parseError"),
            setInput: (0, _chunkGTKDMUJJMjs.a)(function(a, l) {
                return this.yy = l || this.yy || {}, this._input = a, this._more = this._backtrack = this.done = !1, this.yylineno = this.yyleng = 0, this.yytext = this.matched = this.match = "", this.conditionStack = [
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
                var a = this._input[0];
                this.yytext += a, this.yyleng++, this.offset++, this.match += a, this.matched += a;
                var l = a.match(/(?:\r\n?|\n).*/g);
                return l ? (this.yylineno++, this.yylloc.last_line++) : this.yylloc.last_column++, this.options.ranges && this.yylloc.range[1]++, this._input = this._input.slice(1), a;
            }, "input"),
            unput: (0, _chunkGTKDMUJJMjs.a)(function(a) {
                var l = a.length, p = a.split(/(?:\r\n?|\n)/g);
                this._input = a + this._input, this.yytext = this.yytext.substr(0, this.yytext.length - l), this.offset -= l;
                var r = this.match.split(/(?:\r\n?|\n)/g);
                this.match = this.match.substr(0, this.match.length - 1), this.matched = this.matched.substr(0, this.matched.length - 1), p.length - 1 && (this.yylineno -= p.length - 1);
                var A = this.yylloc.range;
                return this.yylloc = {
                    first_line: this.yylloc.first_line,
                    last_line: this.yylineno + 1,
                    first_column: this.yylloc.first_column,
                    last_column: p ? (p.length === r.length ? this.yylloc.first_column : 0) + r[r.length - p.length].length - p[0].length : this.yylloc.first_column - l
                }, this.options.ranges && (this.yylloc.range = [
                    A[0],
                    A[0] + this.yyleng - l
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
            less: (0, _chunkGTKDMUJJMjs.a)(function(a) {
                this.unput(this.match.slice(a));
            }, "less"),
            pastInput: (0, _chunkGTKDMUJJMjs.a)(function() {
                var a = this.matched.substr(0, this.matched.length - this.match.length);
                return (a.length > 20 ? "..." : "") + a.substr(-20).replace(/\n/g, "");
            }, "pastInput"),
            upcomingInput: (0, _chunkGTKDMUJJMjs.a)(function() {
                var a = this.match;
                return a.length < 20 && (a += this._input.substr(0, 20 - a.length)), (a.substr(0, 20) + (a.length > 20 ? "..." : "")).replace(/\n/g, "");
            }, "upcomingInput"),
            showPosition: (0, _chunkGTKDMUJJMjs.a)(function() {
                var a = this.pastInput(), l = new Array(a.length + 1).join("-");
                return a + this.upcomingInput() + `
` + l + "^";
            }, "showPosition"),
            test_match: (0, _chunkGTKDMUJJMjs.a)(function(a, l) {
                var p, r, A;
                if (this.options.backtrack_lexer && (A = {
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
                }, this.options.ranges && (A.yylloc.range = this.yylloc.range.slice(0))), r = a[0].match(/(?:\r\n?|\n).*/g), r && (this.yylineno += r.length), this.yylloc = {
                    first_line: this.yylloc.last_line,
                    last_line: this.yylineno + 1,
                    first_column: this.yylloc.last_column,
                    last_column: r ? r[r.length - 1].length - r[r.length - 1].match(/\r?\n?/)[0].length : this.yylloc.last_column + a[0].length
                }, this.yytext += a[0], this.match += a[0], this.matches = a, this.yyleng = this.yytext.length, this.options.ranges && (this.yylloc.range = [
                    this.offset,
                    this.offset += this.yyleng
                ]), this._more = !1, this._backtrack = !1, this._input = this._input.slice(a[0].length), this.matched += a[0], p = this.performAction.call(this, this.yy, this, l, this.conditionStack[this.conditionStack.length - 1]), this.done && this._input && (this.done = !1), p) return p;
                if (this._backtrack) {
                    for(var t in A)this[t] = A[t];
                    return !1;
                }
                return !1;
            }, "test_match"),
            next: (0, _chunkGTKDMUJJMjs.a)(function() {
                if (this.done) return this.EOF;
                this._input || (this.done = !0);
                var a, l, p, r;
                this._more || (this.yytext = "", this.match = "");
                for(var A = this._currentRules(), t = 0; t < A.length; t++)if (p = this._input.match(this.rules[A[t]]), p && (!l || p[0].length > l[0].length)) {
                    if (l = p, r = t, this.options.backtrack_lexer) {
                        if (a = this.test_match(p, A[t]), a !== !1) return a;
                        if (this._backtrack) {
                            l = !1;
                            continue;
                        } else return !1;
                    } else if (!this.options.flex) break;
                }
                return l ? (a = this.test_match(l, A[r]), a !== !1 ? a : !1) : this._input === "" ? this.EOF : this.parseError("Lexical error on line " + (this.yylineno + 1) + `. Unrecognized text.
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
            options: {},
            performAction: (0, _chunkGTKDMUJJMjs.a)(function(l, p, r, A) {
                var t = A;
                switch(r){
                    case 0:
                        return 53;
                    case 1:
                        return 54;
                    case 2:
                        return 55;
                    case 3:
                        return 56;
                    case 4:
                        break;
                    case 5:
                        break;
                    case 6:
                        return this.begin("acc_title"), 31;
                    case 7:
                        return this.popState(), "acc_title_value";
                    case 8:
                        return this.begin("acc_descr"), 33;
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
                        return 8;
                    case 14:
                        break;
                    case 15:
                        return 7;
                    case 16:
                        return 7;
                    case 17:
                        return "EDGE_STATE";
                    case 18:
                        this.begin("callback_name");
                        break;
                    case 19:
                        this.popState();
                        break;
                    case 20:
                        this.popState(), this.begin("callback_args");
                        break;
                    case 21:
                        return 70;
                    case 22:
                        this.popState();
                        break;
                    case 23:
                        return 71;
                    case 24:
                        this.popState();
                        break;
                    case 25:
                        return "STR";
                    case 26:
                        this.begin("string");
                        break;
                    case 27:
                        return 73;
                    case 28:
                        return this.begin("namespace"), 40;
                    case 29:
                        return this.popState(), 8;
                    case 30:
                        break;
                    case 31:
                        return this.begin("namespace-body"), 37;
                    case 32:
                        return this.popState(), 39;
                    case 33:
                        return "EOF_IN_STRUCT";
                    case 34:
                        return 8;
                    case 35:
                        break;
                    case 36:
                        return "EDGE_STATE";
                    case 37:
                        return this.begin("class"), 44;
                    case 38:
                        return this.popState(), 8;
                    case 39:
                        break;
                    case 40:
                        return this.popState(), this.popState(), 39;
                    case 41:
                        return this.begin("class-body"), 37;
                    case 42:
                        return this.popState(), 39;
                    case 43:
                        return "EOF_IN_STRUCT";
                    case 44:
                        return "EDGE_STATE";
                    case 45:
                        return "OPEN_IN_STRUCT";
                    case 46:
                        break;
                    case 47:
                        return "MEMBER";
                    case 48:
                        return 76;
                    case 49:
                        return 66;
                    case 50:
                        return 67;
                    case 51:
                        return 69;
                    case 52:
                        return 50;
                    case 53:
                        return 52;
                    case 54:
                        return 45;
                    case 55:
                        return 46;
                    case 56:
                        return 72;
                    case 57:
                        this.popState();
                        break;
                    case 58:
                        return "GENERICTYPE";
                    case 59:
                        this.begin("generic");
                        break;
                    case 60:
                        this.popState();
                        break;
                    case 61:
                        return "BQUOTE_STR";
                    case 62:
                        this.begin("bqstring");
                        break;
                    case 63:
                        return 68;
                    case 64:
                        return 68;
                    case 65:
                        return 68;
                    case 66:
                        return 68;
                    case 67:
                        return 60;
                    case 68:
                        return 60;
                    case 69:
                        return 62;
                    case 70:
                        return 62;
                    case 71:
                        return 61;
                    case 72:
                        return 59;
                    case 73:
                        return 63;
                    case 74:
                        return 64;
                    case 75:
                        return 65;
                    case 76:
                        return 21;
                    case 77:
                        return 42;
                    case 78:
                        return 95;
                    case 79:
                        return "DOT";
                    case 80:
                        return "PLUS";
                    case 81:
                        return 81;
                    case 82:
                        return 78;
                    case 83:
                        return 84;
                    case 84:
                        return 84;
                    case 85:
                        return 85;
                    case 86:
                        return "EQUALS";
                    case 87:
                        return "EQUALS";
                    case 88:
                        return 74;
                    case 89:
                        return 12;
                    case 90:
                        return 14;
                    case 91:
                        return "PUNCTUATION";
                    case 92:
                        return 80;
                    case 93:
                        return 97;
                    case 94:
                        return 83;
                    case 95:
                        return 83;
                    case 96:
                        return 9;
                }
            }, "anonymous"),
            rules: [
                /^(?:.*direction\s+TB[^\n]*)/,
                /^(?:.*direction\s+BT[^\n]*)/,
                /^(?:.*direction\s+RL[^\n]*)/,
                /^(?:.*direction\s+LR[^\n]*)/,
                /^(?:%%(?!\{)*[^\n]*(\r?\n?)+)/,
                /^(?:%%[^\n]*(\r?\n)*)/,
                /^(?:accTitle\s*:\s*)/,
                /^(?:(?!\n||)*[^\n]*)/,
                /^(?:accDescr\s*:\s*)/,
                /^(?:(?!\n||)*[^\n]*)/,
                /^(?:accDescr\s*\{\s*)/,
                /^(?:[\}])/,
                /^(?:[^\}]*)/,
                /^(?:\s*(\r?\n)+)/,
                /^(?:\s+)/,
                /^(?:classDiagram-v2\b)/,
                /^(?:classDiagram\b)/,
                /^(?:\[\*\])/,
                /^(?:call[\s]+)/,
                /^(?:\([\s]*\))/,
                /^(?:\()/,
                /^(?:[^(]*)/,
                /^(?:\))/,
                /^(?:[^)]*)/,
                /^(?:["])/,
                /^(?:[^"]*)/,
                /^(?:["])/,
                /^(?:style\b)/,
                /^(?:namespace\b)/,
                /^(?:\s*(\r?\n)+)/,
                /^(?:\s+)/,
                /^(?:[{])/,
                /^(?:[}])/,
                /^(?:$)/,
                /^(?:\s*(\r?\n)+)/,
                /^(?:\s+)/,
                /^(?:\[\*\])/,
                /^(?:class\b)/,
                /^(?:\s*(\r?\n)+)/,
                /^(?:\s+)/,
                /^(?:[}])/,
                /^(?:[{])/,
                /^(?:[}])/,
                /^(?:$)/,
                /^(?:\[\*\])/,
                /^(?:[{])/,
                /^(?:[\n])/,
                /^(?:[^{}\n]*)/,
                /^(?:cssClass\b)/,
                /^(?:callback\b)/,
                /^(?:link\b)/,
                /^(?:click\b)/,
                /^(?:note for\b)/,
                /^(?:note\b)/,
                /^(?:<<)/,
                /^(?:>>)/,
                /^(?:href\b)/,
                /^(?:[~])/,
                /^(?:[^~]*)/,
                /^(?:~)/,
                /^(?:[`])/,
                /^(?:[^`]+)/,
                /^(?:[`])/,
                /^(?:_self\b)/,
                /^(?:_blank\b)/,
                /^(?:_parent\b)/,
                /^(?:_top\b)/,
                /^(?:\s*<\|)/,
                /^(?:\s*\|>)/,
                /^(?:\s*>)/,
                /^(?:\s*<)/,
                /^(?:\s*\*)/,
                /^(?:\s*o\b)/,
                /^(?:\s*\(\))/,
                /^(?:--)/,
                /^(?:\.\.)/,
                /^(?::{1}[^:\n;]+)/,
                /^(?::{3})/,
                /^(?:-)/,
                /^(?:\.)/,
                /^(?:\+)/,
                /^(?::)/,
                /^(?:,)/,
                /^(?:#)/,
                /^(?:#)/,
                /^(?:%)/,
                /^(?:=)/,
                /^(?:=)/,
                /^(?:\w+)/,
                /^(?:\[)/,
                /^(?:\])/,
                /^(?:[!"#$%&'*+,-.`?\\/])/,
                /^(?:[0-9]+)/,
                /^(?:[\u00AA\u00B5\u00BA\u00C0-\u00D6\u00D8-\u00F6]|[\u00F8-\u02C1\u02C6-\u02D1\u02E0-\u02E4\u02EC\u02EE\u0370-\u0374\u0376\u0377]|[\u037A-\u037D\u0386\u0388-\u038A\u038C\u038E-\u03A1\u03A3-\u03F5]|[\u03F7-\u0481\u048A-\u0527\u0531-\u0556\u0559\u0561-\u0587\u05D0-\u05EA]|[\u05F0-\u05F2\u0620-\u064A\u066E\u066F\u0671-\u06D3\u06D5\u06E5\u06E6\u06EE]|[\u06EF\u06FA-\u06FC\u06FF\u0710\u0712-\u072F\u074D-\u07A5\u07B1\u07CA-\u07EA]|[\u07F4\u07F5\u07FA\u0800-\u0815\u081A\u0824\u0828\u0840-\u0858\u08A0]|[\u08A2-\u08AC\u0904-\u0939\u093D\u0950\u0958-\u0961\u0971-\u0977]|[\u0979-\u097F\u0985-\u098C\u098F\u0990\u0993-\u09A8\u09AA-\u09B0\u09B2]|[\u09B6-\u09B9\u09BD\u09CE\u09DC\u09DD\u09DF-\u09E1\u09F0\u09F1\u0A05-\u0A0A]|[\u0A0F\u0A10\u0A13-\u0A28\u0A2A-\u0A30\u0A32\u0A33\u0A35\u0A36\u0A38\u0A39]|[\u0A59-\u0A5C\u0A5E\u0A72-\u0A74\u0A85-\u0A8D\u0A8F-\u0A91\u0A93-\u0AA8]|[\u0AAA-\u0AB0\u0AB2\u0AB3\u0AB5-\u0AB9\u0ABD\u0AD0\u0AE0\u0AE1\u0B05-\u0B0C]|[\u0B0F\u0B10\u0B13-\u0B28\u0B2A-\u0B30\u0B32\u0B33\u0B35-\u0B39\u0B3D\u0B5C]|[\u0B5D\u0B5F-\u0B61\u0B71\u0B83\u0B85-\u0B8A\u0B8E-\u0B90\u0B92-\u0B95\u0B99]|[\u0B9A\u0B9C\u0B9E\u0B9F\u0BA3\u0BA4\u0BA8-\u0BAA\u0BAE-\u0BB9\u0BD0]|[\u0C05-\u0C0C\u0C0E-\u0C10\u0C12-\u0C28\u0C2A-\u0C33\u0C35-\u0C39\u0C3D]|[\u0C58\u0C59\u0C60\u0C61\u0C85-\u0C8C\u0C8E-\u0C90\u0C92-\u0CA8\u0CAA-\u0CB3]|[\u0CB5-\u0CB9\u0CBD\u0CDE\u0CE0\u0CE1\u0CF1\u0CF2\u0D05-\u0D0C\u0D0E-\u0D10]|[\u0D12-\u0D3A\u0D3D\u0D4E\u0D60\u0D61\u0D7A-\u0D7F\u0D85-\u0D96\u0D9A-\u0DB1]|[\u0DB3-\u0DBB\u0DBD\u0DC0-\u0DC6\u0E01-\u0E30\u0E32\u0E33\u0E40-\u0E46\u0E81]|[\u0E82\u0E84\u0E87\u0E88\u0E8A\u0E8D\u0E94-\u0E97\u0E99-\u0E9F\u0EA1-\u0EA3]|[\u0EA5\u0EA7\u0EAA\u0EAB\u0EAD-\u0EB0\u0EB2\u0EB3\u0EBD\u0EC0-\u0EC4\u0EC6]|[\u0EDC-\u0EDF\u0F00\u0F40-\u0F47\u0F49-\u0F6C\u0F88-\u0F8C\u1000-\u102A]|[\u103F\u1050-\u1055\u105A-\u105D\u1061\u1065\u1066\u106E-\u1070\u1075-\u1081]|[\u108E\u10A0-\u10C5\u10C7\u10CD\u10D0-\u10FA\u10FC-\u1248\u124A-\u124D]|[\u1250-\u1256\u1258\u125A-\u125D\u1260-\u1288\u128A-\u128D\u1290-\u12B0]|[\u12B2-\u12B5\u12B8-\u12BE\u12C0\u12C2-\u12C5\u12C8-\u12D6\u12D8-\u1310]|[\u1312-\u1315\u1318-\u135A\u1380-\u138F\u13A0-\u13F4\u1401-\u166C]|[\u166F-\u167F\u1681-\u169A\u16A0-\u16EA\u1700-\u170C\u170E-\u1711]|[\u1720-\u1731\u1740-\u1751\u1760-\u176C\u176E-\u1770\u1780-\u17B3\u17D7]|[\u17DC\u1820-\u1877\u1880-\u18A8\u18AA\u18B0-\u18F5\u1900-\u191C]|[\u1950-\u196D\u1970-\u1974\u1980-\u19AB\u19C1-\u19C7\u1A00-\u1A16]|[\u1A20-\u1A54\u1AA7\u1B05-\u1B33\u1B45-\u1B4B\u1B83-\u1BA0\u1BAE\u1BAF]|[\u1BBA-\u1BE5\u1C00-\u1C23\u1C4D-\u1C4F\u1C5A-\u1C7D\u1CE9-\u1CEC]|[\u1CEE-\u1CF1\u1CF5\u1CF6\u1D00-\u1DBF\u1E00-\u1F15\u1F18-\u1F1D]|[\u1F20-\u1F45\u1F48-\u1F4D\u1F50-\u1F57\u1F59\u1F5B\u1F5D\u1F5F-\u1F7D]|[\u1F80-\u1FB4\u1FB6-\u1FBC\u1FBE\u1FC2-\u1FC4\u1FC6-\u1FCC\u1FD0-\u1FD3]|[\u1FD6-\u1FDB\u1FE0-\u1FEC\u1FF2-\u1FF4\u1FF6-\u1FFC\u2071\u207F]|[\u2090-\u209C\u2102\u2107\u210A-\u2113\u2115\u2119-\u211D\u2124\u2126\u2128]|[\u212A-\u212D\u212F-\u2139\u213C-\u213F\u2145-\u2149\u214E\u2183\u2184]|[\u2C00-\u2C2E\u2C30-\u2C5E\u2C60-\u2CE4\u2CEB-\u2CEE\u2CF2\u2CF3]|[\u2D00-\u2D25\u2D27\u2D2D\u2D30-\u2D67\u2D6F\u2D80-\u2D96\u2DA0-\u2DA6]|[\u2DA8-\u2DAE\u2DB0-\u2DB6\u2DB8-\u2DBE\u2DC0-\u2DC6\u2DC8-\u2DCE]|[\u2DD0-\u2DD6\u2DD8-\u2DDE\u2E2F\u3005\u3006\u3031-\u3035\u303B\u303C]|[\u3041-\u3096\u309D-\u309F\u30A1-\u30FA\u30FC-\u30FF\u3105-\u312D]|[\u3131-\u318E\u31A0-\u31BA\u31F0-\u31FF\u3400-\u4DB5\u4E00-\u9FCC]|[\uA000-\uA48C\uA4D0-\uA4FD\uA500-\uA60C\uA610-\uA61F\uA62A\uA62B]|[\uA640-\uA66E\uA67F-\uA697\uA6A0-\uA6E5\uA717-\uA71F\uA722-\uA788]|[\uA78B-\uA78E\uA790-\uA793\uA7A0-\uA7AA\uA7F8-\uA801\uA803-\uA805]|[\uA807-\uA80A\uA80C-\uA822\uA840-\uA873\uA882-\uA8B3\uA8F2-\uA8F7\uA8FB]|[\uA90A-\uA925\uA930-\uA946\uA960-\uA97C\uA984-\uA9B2\uA9CF\uAA00-\uAA28]|[\uAA40-\uAA42\uAA44-\uAA4B\uAA60-\uAA76\uAA7A\uAA80-\uAAAF\uAAB1\uAAB5]|[\uAAB6\uAAB9-\uAABD\uAAC0\uAAC2\uAADB-\uAADD\uAAE0-\uAAEA\uAAF2-\uAAF4]|[\uAB01-\uAB06\uAB09-\uAB0E\uAB11-\uAB16\uAB20-\uAB26\uAB28-\uAB2E]|[\uABC0-\uABE2\uAC00-\uD7A3\uD7B0-\uD7C6\uD7CB-\uD7FB\uF900-\uFA6D]|[\uFA70-\uFAD9\uFB00-\uFB06\uFB13-\uFB17\uFB1D\uFB1F-\uFB28\uFB2A-\uFB36]|[\uFB38-\uFB3C\uFB3E\uFB40\uFB41\uFB43\uFB44\uFB46-\uFBB1\uFBD3-\uFD3D]|[\uFD50-\uFD8F\uFD92-\uFDC7\uFDF0-\uFDFB\uFE70-\uFE74\uFE76-\uFEFC]|[\uFF21-\uFF3A\uFF41-\uFF5A\uFF66-\uFFBE\uFFC2-\uFFC7\uFFCA-\uFFCF]|[\uFFD2-\uFFD7\uFFDA-\uFFDC])/,
                /^(?:\s)/,
                /^(?:\s)/,
                /^(?:$)/
            ],
            conditions: {
                "namespace-body": {
                    rules: [
                        26,
                        32,
                        33,
                        34,
                        35,
                        36,
                        37,
                        48,
                        49,
                        50,
                        51,
                        52,
                        53,
                        54,
                        55,
                        56,
                        59,
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
                        77,
                        78,
                        79,
                        80,
                        85,
                        86,
                        87,
                        88,
                        89,
                        90,
                        91,
                        92,
                        93,
                        94,
                        96
                    ],
                    inclusive: !1
                },
                namespace: {
                    rules: [
                        26,
                        28,
                        29,
                        30,
                        31,
                        48,
                        49,
                        50,
                        51,
                        52,
                        53,
                        54,
                        55,
                        56,
                        59,
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
                        77,
                        78,
                        79,
                        80,
                        85,
                        86,
                        87,
                        88,
                        89,
                        90,
                        91,
                        92,
                        93,
                        94,
                        96
                    ],
                    inclusive: !1
                },
                "class-body": {
                    rules: [
                        26,
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
                        55,
                        56,
                        59,
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
                        77,
                        78,
                        79,
                        80,
                        85,
                        86,
                        87,
                        88,
                        89,
                        90,
                        91,
                        92,
                        93,
                        94,
                        96
                    ],
                    inclusive: !1
                },
                class: {
                    rules: [
                        26,
                        38,
                        39,
                        40,
                        41,
                        48,
                        49,
                        50,
                        51,
                        52,
                        53,
                        54,
                        55,
                        56,
                        59,
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
                        77,
                        78,
                        79,
                        80,
                        85,
                        86,
                        87,
                        88,
                        89,
                        90,
                        91,
                        92,
                        93,
                        94,
                        96
                    ],
                    inclusive: !1
                },
                acc_descr_multiline: {
                    rules: [
                        11,
                        12,
                        26,
                        48,
                        49,
                        50,
                        51,
                        52,
                        53,
                        54,
                        55,
                        56,
                        59,
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
                        77,
                        78,
                        79,
                        80,
                        85,
                        86,
                        87,
                        88,
                        89,
                        90,
                        91,
                        92,
                        93,
                        94,
                        96
                    ],
                    inclusive: !1
                },
                acc_descr: {
                    rules: [
                        9,
                        26,
                        48,
                        49,
                        50,
                        51,
                        52,
                        53,
                        54,
                        55,
                        56,
                        59,
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
                        77,
                        78,
                        79,
                        80,
                        85,
                        86,
                        87,
                        88,
                        89,
                        90,
                        91,
                        92,
                        93,
                        94,
                        96
                    ],
                    inclusive: !1
                },
                acc_title: {
                    rules: [
                        7,
                        26,
                        48,
                        49,
                        50,
                        51,
                        52,
                        53,
                        54,
                        55,
                        56,
                        59,
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
                        77,
                        78,
                        79,
                        80,
                        85,
                        86,
                        87,
                        88,
                        89,
                        90,
                        91,
                        92,
                        93,
                        94,
                        96
                    ],
                    inclusive: !1
                },
                callback_args: {
                    rules: [
                        22,
                        23,
                        26,
                        48,
                        49,
                        50,
                        51,
                        52,
                        53,
                        54,
                        55,
                        56,
                        59,
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
                        77,
                        78,
                        79,
                        80,
                        85,
                        86,
                        87,
                        88,
                        89,
                        90,
                        91,
                        92,
                        93,
                        94,
                        96
                    ],
                    inclusive: !1
                },
                callback_name: {
                    rules: [
                        19,
                        20,
                        21,
                        26,
                        48,
                        49,
                        50,
                        51,
                        52,
                        53,
                        54,
                        55,
                        56,
                        59,
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
                        77,
                        78,
                        79,
                        80,
                        85,
                        86,
                        87,
                        88,
                        89,
                        90,
                        91,
                        92,
                        93,
                        94,
                        96
                    ],
                    inclusive: !1
                },
                href: {
                    rules: [
                        26,
                        48,
                        49,
                        50,
                        51,
                        52,
                        53,
                        54,
                        55,
                        56,
                        59,
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
                        77,
                        78,
                        79,
                        80,
                        85,
                        86,
                        87,
                        88,
                        89,
                        90,
                        91,
                        92,
                        93,
                        94,
                        96
                    ],
                    inclusive: !1
                },
                struct: {
                    rules: [
                        26,
                        48,
                        49,
                        50,
                        51,
                        52,
                        53,
                        54,
                        55,
                        56,
                        59,
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
                        77,
                        78,
                        79,
                        80,
                        85,
                        86,
                        87,
                        88,
                        89,
                        90,
                        91,
                        92,
                        93,
                        94,
                        96
                    ],
                    inclusive: !1
                },
                generic: {
                    rules: [
                        26,
                        48,
                        49,
                        50,
                        51,
                        52,
                        53,
                        54,
                        55,
                        56,
                        57,
                        58,
                        59,
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
                        77,
                        78,
                        79,
                        80,
                        85,
                        86,
                        87,
                        88,
                        89,
                        90,
                        91,
                        92,
                        93,
                        94,
                        96
                    ],
                    inclusive: !1
                },
                bqstring: {
                    rules: [
                        26,
                        48,
                        49,
                        50,
                        51,
                        52,
                        53,
                        54,
                        55,
                        56,
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
                        77,
                        78,
                        79,
                        80,
                        85,
                        86,
                        87,
                        88,
                        89,
                        90,
                        91,
                        92,
                        93,
                        94,
                        96
                    ],
                    inclusive: !1
                },
                string: {
                    rules: [
                        24,
                        25,
                        26,
                        48,
                        49,
                        50,
                        51,
                        52,
                        53,
                        54,
                        55,
                        56,
                        59,
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
                        77,
                        78,
                        79,
                        80,
                        85,
                        86,
                        87,
                        88,
                        89,
                        90,
                        91,
                        92,
                        93,
                        94,
                        96
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
                        8,
                        10,
                        13,
                        14,
                        15,
                        16,
                        17,
                        18,
                        26,
                        27,
                        28,
                        37,
                        48,
                        49,
                        50,
                        51,
                        52,
                        53,
                        54,
                        55,
                        56,
                        59,
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
                        77,
                        78,
                        79,
                        80,
                        81,
                        82,
                        83,
                        84,
                        85,
                        86,
                        87,
                        88,
                        89,
                        90,
                        91,
                        92,
                        93,
                        94,
                        95,
                        96
                    ],
                    inclusive: !0
                }
            }
        };
        return O;
    }();
    ve.lexer = pt;
    function Ie() {
        this.yy = {};
    }
    return (0, _chunkGTKDMUJJMjs.a)(Ie, "Parser"), Ie.prototype = ve, ve.Parser = Ie, new Ie;
}();
Ge.parser = Ge;
var jt = Ge;
var ut = [
    "#",
    "+",
    "~",
    "-",
    ""
], Y = class {
    static{
        (0, _chunkGTKDMUJJMjs.a)(this, "ClassMember");
    }
    constructor(i, u){
        this.memberType = u, this.visibility = "", this.classifier = "";
        let h = (0, _chunkNQURTBEVMjs.F)(i, (0, _chunkNQURTBEVMjs.X)());
        this.parseMember(h);
    }
    getDisplayDetails() {
        let i = this.visibility + (0, _chunkNQURTBEVMjs.H)(this.id);
        this.memberType === "method" && (i += `(${(0, _chunkNQURTBEVMjs.H)(this.parameters.trim())})`, this.returnType && (i += " : " + (0, _chunkNQURTBEVMjs.H)(this.returnType))), i = i.trim();
        let u = this.parseClassifier();
        return {
            displayText: i,
            cssStyle: u
        };
    }
    parseMember(i) {
        let u = "";
        if (this.memberType === "method") {
            let o = /([#+~-])?(.+)\((.*)\)([\s$*])?(.*)([$*])?/.exec(i);
            if (o) {
                let f = o[1] ? o[1].trim() : "";
                if (ut.includes(f) && (this.visibility = f), this.id = o[2].trim(), this.parameters = o[3] ? o[3].trim() : "", u = o[4] ? o[4].trim() : "", this.returnType = o[5] ? o[5].trim() : "", u === "") {
                    let g = this.returnType.substring(this.returnType.length - 1);
                    /[$*]/.exec(g) && (u = g, this.returnType = this.returnType.substring(0, this.returnType.length - 1));
                }
            }
        } else {
            let h = i.length, o = i.substring(0, 1), f = i.substring(h - 1);
            ut.includes(o) && (this.visibility = o), /[$*]/.exec(f) && (u = f), this.id = i.substring(this.visibility === "" ? 0 : 1, u === "" ? h : h - 1);
        }
        this.classifier = u;
    }
    parseClassifier() {
        switch(this.classifier){
            case "*":
                return "font-style:italic;";
            case "$":
                return "text-decoration:underline;";
            default:
                return "";
        }
    }
};
var de = "classId-", ze = [], d = new Map, fe = [], lt = 0, I = new Map, Ue = 0, Q = [], w = (0, _chunkGTKDMUJJMjs.a)((e)=>(0, _chunkNQURTBEVMjs.L).sanitizeText(e, (0, _chunkNQURTBEVMjs.X)()), "sanitizeText"), M = (0, _chunkGTKDMUJJMjs.a)(function(e) {
    let i = (0, _chunkNQURTBEVMjs.L).sanitizeText(e, (0, _chunkNQURTBEVMjs.X)()), u = "", h = i;
    if (i.indexOf("~") > 0) {
        let o = i.split("~");
        h = w(o[0]), u = w(o[1]);
    }
    return {
        className: h,
        type: u
    };
}, "splitClassNameAndType"), bt = (0, _chunkGTKDMUJJMjs.a)(function(e, i) {
    let u = (0, _chunkNQURTBEVMjs.L).sanitizeText(e, (0, _chunkNQURTBEVMjs.X)());
    i && (i = w(i));
    let { className: h } = M(u);
    d.get(h).label = i;
}, "setClassLabel"), ge = (0, _chunkGTKDMUJJMjs.a)(function(e) {
    let i = (0, _chunkNQURTBEVMjs.L).sanitizeText(e, (0, _chunkNQURTBEVMjs.X)()), { className: u, type: h } = M(i);
    if (d.has(u)) return;
    let o = (0, _chunkNQURTBEVMjs.L).sanitizeText(u, (0, _chunkNQURTBEVMjs.X)());
    d.set(o, {
        id: o,
        type: h,
        label: o,
        cssClasses: [],
        methods: [],
        members: [],
        annotations: [],
        styles: [],
        domId: de + o + "-" + lt
    }), lt++;
}, "addClass"), ot = (0, _chunkGTKDMUJJMjs.a)(function(e) {
    let i = (0, _chunkNQURTBEVMjs.L).sanitizeText(e, (0, _chunkNQURTBEVMjs.X)());
    if (d.has(i)) return d.get(i).domId;
    throw new Error("Class not found: " + i);
}, "lookUpDomId"), kt = (0, _chunkGTKDMUJJMjs.a)(function() {
    ze = [], d = new Map, fe = [], Q = [], Q.push(ht), I = new Map, Ue = 0, Ye = "TB", (0, _chunkNQURTBEVMjs.P)();
}, "clear"), mt = (0, _chunkGTKDMUJJMjs.a)(function(e) {
    return d.get(e);
}, "getClass"), Ct = (0, _chunkGTKDMUJJMjs.a)(function() {
    return d;
}, "getClasses"), Et = (0, _chunkGTKDMUJJMjs.a)(function() {
    return ze;
}, "getRelations"), yt = (0, _chunkGTKDMUJJMjs.a)(function() {
    return fe;
}, "getNotes"), Tt = (0, _chunkGTKDMUJJMjs.a)(function(e) {
    (0, _chunkNQURTBEVMjs.b).debug("Adding relation: " + JSON.stringify(e)), ge(e.id1), ge(e.id2), e.id1 = M(e.id1).className, e.id2 = M(e.id2).className, e.relationTitle1 = (0, _chunkNQURTBEVMjs.L).sanitizeText(e.relationTitle1.trim(), (0, _chunkNQURTBEVMjs.X)()), e.relationTitle2 = (0, _chunkNQURTBEVMjs.L).sanitizeText(e.relationTitle2.trim(), (0, _chunkNQURTBEVMjs.X)()), ze.push(e);
}, "addRelation"), Ft = (0, _chunkGTKDMUJJMjs.a)(function(e, i) {
    let u = M(e).className;
    d.get(u).annotations.push(i);
}, "addAnnotation"), ct = (0, _chunkGTKDMUJJMjs.a)(function(e, i) {
    ge(e);
    let u = M(e).className, h = d.get(u);
    if (typeof i == "string") {
        let o = i.trim();
        o.startsWith("<<") && o.endsWith(">>") ? h.annotations.push(w(o.substring(2, o.length - 2))) : o.indexOf(")") > 0 ? h.methods.push(new Y(o, "method")) : o && h.members.push(new Y(o, "attribute"));
    }
}, "addMember"), Dt = (0, _chunkGTKDMUJJMjs.a)(function(e, i) {
    Array.isArray(i) && (i.reverse(), i.forEach((u)=>ct(e, u)));
}, "addMembers"), Bt = (0, _chunkGTKDMUJJMjs.a)(function(e, i) {
    let u = {
        id: `note${fe.length}`,
        class: i,
        text: e
    };
    fe.push(u);
}, "addNote"), _t = (0, _chunkGTKDMUJJMjs.a)(function(e) {
    return e.startsWith(":") && (e = e.substring(1)), w(e.trim());
}, "cleanupLabel"), Ke = (0, _chunkGTKDMUJJMjs.a)(function(e, i) {
    e.split(",").forEach(function(u) {
        let h = u;
        /\d/.exec(u[0]) && (h = de + h);
        let o = d.get(h);
        o && o.cssClasses.push(i);
    });
}, "setCssClass"), St = (0, _chunkGTKDMUJJMjs.a)(function(e, i) {
    e.split(",").forEach(function(u) {
        i !== void 0 && (d.get(u).tooltip = w(i));
    });
}, "setTooltip"), Nt = (0, _chunkGTKDMUJJMjs.a)(function(e, i) {
    return i && I.has(i) ? I.get(i).classes.get(e).tooltip : d.get(e).tooltip;
}, "getTooltip"), xt = (0, _chunkGTKDMUJJMjs.a)(function(e, i, u) {
    let h = (0, _chunkNQURTBEVMjs.X)();
    e.split(",").forEach(function(o) {
        let f = o;
        /\d/.exec(o[0]) && (f = de + f);
        let g = d.get(f);
        g && (g.link = (0, _chunkAC3VT7B7Mjs.m).formatUrl(i, h), h.securityLevel === "sandbox" ? g.linkTarget = "_top" : typeof u == "string" ? g.linkTarget = w(u) : g.linkTarget = "_blank");
    }), Ke(e, "clickable");
}, "setLink"), Lt = (0, _chunkGTKDMUJJMjs.a)(function(e, i, u) {
    e.split(",").forEach(function(h) {
        vt(h, i, u), d.get(h).haveCallback = !0;
    }), Ke(e, "clickable");
}, "setClickEvent"), vt = (0, _chunkGTKDMUJJMjs.a)(function(e, i, u) {
    let h = (0, _chunkNQURTBEVMjs.L).sanitizeText(e, (0, _chunkNQURTBEVMjs.X)());
    if ((0, _chunkNQURTBEVMjs.X)().securityLevel !== "loose" || i === void 0) return;
    let f = h;
    if (d.has(f)) {
        let g = ot(f), _ = [];
        if (typeof u == "string") {
            _ = u.split(/,(?=(?:(?:[^"]*"){2})*[^"]*$)/);
            for(let S = 0; S < _.length; S++){
                let x = _[S].trim();
                x.startsWith('"') && x.endsWith('"') && (x = x.substr(1, x.length - 2)), _[S] = x;
            }
        }
        _.length === 0 && _.push(g), Q.push(function() {
            let S = document.querySelector(`[id="${g}"]`);
            S !== null && S.addEventListener("click", function() {
                (0, _chunkAC3VT7B7Mjs.m).runFunc(i, ..._);
            }, !1);
        });
    }
}, "setClickFunc"), It = (0, _chunkGTKDMUJJMjs.a)(function(e) {
    Q.forEach(function(i) {
        i(e);
    });
}, "bindFunctions"), Ot = {
    LINE: 0,
    DOTTED_LINE: 1
}, Rt = {
    AGGREGATION: 0,
    EXTENSION: 1,
    COMPOSITION: 2,
    DEPENDENCY: 3,
    LOLLIPOP: 4
}, ht = (0, _chunkGTKDMUJJMjs.a)(function(e) {
    let i = (0, _chunkNQURTBEVMjs.fa)(".mermaidTooltip");
    (i._groups || i)[0][0] === null && (i = (0, _chunkNQURTBEVMjs.fa)("body").append("div").attr("class", "mermaidTooltip").style("opacity", 0)), (0, _chunkNQURTBEVMjs.fa)(e).select("svg").selectAll("g.node").on("mouseover", function() {
        let o = (0, _chunkNQURTBEVMjs.fa)(this);
        if (o.attr("title") === null) return;
        let g = this.getBoundingClientRect();
        i.transition().duration(200).style("opacity", ".9"), i.text(o.attr("title")).style("left", window.scrollX + g.left + (g.right - g.left) / 2 + "px").style("top", window.scrollY + g.top - 14 + document.body.scrollTop + "px"), i.html(i.html().replace(/&lt;br\/&gt;/g, "<br/>")), o.classed("hover", !0);
    }).on("mouseout", function() {
        i.transition().duration(500).style("opacity", 0), (0, _chunkNQURTBEVMjs.fa)(this).classed("hover", !1);
    });
}, "setupToolTips");
Q.push(ht);
var Ye = "TB", Vt = (0, _chunkGTKDMUJJMjs.a)(()=>Ye, "getDirection"), wt = (0, _chunkGTKDMUJJMjs.a)((e)=>{
    Ye = e;
}, "setDirection"), Mt = (0, _chunkGTKDMUJJMjs.a)(function(e) {
    I.has(e) || (I.set(e, {
        id: e,
        classes: new Map,
        children: {},
        domId: de + e + "-" + Ue
    }), Ue++);
}, "addNamespace"), Pt = (0, _chunkGTKDMUJJMjs.a)(function(e) {
    return I.get(e);
}, "getNamespace"), Gt = (0, _chunkGTKDMUJJMjs.a)(function() {
    return I;
}, "getNamespaces"), Ut = (0, _chunkGTKDMUJJMjs.a)(function(e, i) {
    if (I.has(e)) for (let u of i){
        let { className: h } = M(u);
        d.get(h).parent = e, I.get(e).classes.set(h, d.get(h));
    }
}, "addClassesToNamespace"), zt = (0, _chunkGTKDMUJJMjs.a)(function(e, i) {
    let u = d.get(e);
    if (!(!i || !u)) for (let h of i)h.includes(",") ? u.styles.push(...h.split(",")) : u.styles.push(h);
}, "setCssStyle"), as = {
    setAccTitle: (0, _chunkNQURTBEVMjs.Q),
    getAccTitle: (0, _chunkNQURTBEVMjs.R),
    getAccDescription: (0, _chunkNQURTBEVMjs.T),
    setAccDescription: (0, _chunkNQURTBEVMjs.S),
    getConfig: (0, _chunkGTKDMUJJMjs.a)(()=>(0, _chunkNQURTBEVMjs.X)().class, "getConfig"),
    addClass: ge,
    bindFunctions: It,
    clear: kt,
    getClass: mt,
    getClasses: Ct,
    getNotes: yt,
    addAnnotation: Ft,
    addNote: Bt,
    getRelations: Et,
    addRelation: Tt,
    getDirection: Vt,
    setDirection: wt,
    addMember: ct,
    addMembers: Dt,
    cleanupLabel: _t,
    lineType: Ot,
    relationType: Rt,
    setClickEvent: Lt,
    setCssClass: Ke,
    setLink: xt,
    getTooltip: Nt,
    setTooltip: St,
    lookUpDomId: ot,
    setDiagramTitle: (0, _chunkNQURTBEVMjs.U),
    getDiagramTitle: (0, _chunkNQURTBEVMjs.V),
    setClassLabel: bt,
    addNamespace: Mt,
    addClassesToNamespace: Ut,
    getNamespace: Pt,
    getNamespaces: Gt,
    setCssStyle: zt
};
var Kt = (0, _chunkGTKDMUJJMjs.a)((e)=>`g.classGroup text {
  fill: ${e.nodeBorder || e.classText};
  stroke: none;
  font-family: ${e.fontFamily};
  font-size: 10px;

  .title {
    font-weight: bolder;
  }

}

.nodeLabel, .edgeLabel {
  color: ${e.classText};
}
.edgeLabel .label rect {
  fill: ${e.mainBkg};
}
.label text {
  fill: ${e.classText};
}
.edgeLabel .label span {
  background: ${e.mainBkg};
}

.classTitle {
  font-weight: bolder;
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


.divider {
  stroke: ${e.nodeBorder};
  stroke-width: 1;
}

g.clickable {
  cursor: pointer;
}

g.classGroup rect {
  fill: ${e.mainBkg};
  stroke: ${e.nodeBorder};
}

g.classGroup line {
  stroke: ${e.nodeBorder};
  stroke-width: 1;
}

.classLabel .box {
  stroke: none;
  stroke-width: 0;
  fill: ${e.mainBkg};
  opacity: 0.5;
}

.classLabel .label {
  fill: ${e.nodeBorder};
  font-size: 10px;
}

.relation {
  stroke: ${e.lineColor};
  stroke-width: 1;
  fill: none;
}

.dashed-line{
  stroke-dasharray: 3;
}

.dotted-line{
  stroke-dasharray: 1 2;
}

#compositionStart, .composition {
  fill: ${e.lineColor} !important;
  stroke: ${e.lineColor} !important;
  stroke-width: 1;
}

#compositionEnd, .composition {
  fill: ${e.lineColor} !important;
  stroke: ${e.lineColor} !important;
  stroke-width: 1;
}

#dependencyStart, .dependency {
  fill: ${e.lineColor} !important;
  stroke: ${e.lineColor} !important;
  stroke-width: 1;
}

#dependencyStart, .dependency {
  fill: ${e.lineColor} !important;
  stroke: ${e.lineColor} !important;
  stroke-width: 1;
}

#extensionStart, .extension {
  fill: transparent !important;
  stroke: ${e.lineColor} !important;
  stroke-width: 1;
}

#extensionEnd, .extension {
  fill: transparent !important;
  stroke: ${e.lineColor} !important;
  stroke-width: 1;
}

#aggregationStart, .aggregation {
  fill: transparent !important;
  stroke: ${e.lineColor} !important;
  stroke-width: 1;
}

#aggregationEnd, .aggregation {
  fill: transparent !important;
  stroke: ${e.lineColor} !important;
  stroke-width: 1;
}

#lollipopStart, .lollipop {
  fill: ${e.mainBkg} !important;
  stroke: ${e.lineColor} !important;
  stroke-width: 1;
}

#lollipopEnd, .lollipop {
  fill: ${e.mainBkg} !important;
  stroke: ${e.lineColor} !important;
  stroke-width: 1;
}

.edgeTerminals {
  font-size: 11px;
  line-height: initial;
}

.classTitleText {
  text-anchor: middle;
  font-size: 18px;
  fill: ${e.textColor};
}
`, "getStyles"), os = Kt;

},{"./chunk-AC3VT7B7.mjs":"1eiUz","./chunk-NQURTBEV.mjs":"iASFe","./chunk-GTKDMUJJ.mjs":"fruhx","@parcel/transformer-js/src/esmodule-helpers.js":"1cdyk"}]},["fiXCT"], null, "parcelRequire6955", {})

//# sourceMappingURL=classDiagram-EPVPYR3L.f9b455da.js.map
