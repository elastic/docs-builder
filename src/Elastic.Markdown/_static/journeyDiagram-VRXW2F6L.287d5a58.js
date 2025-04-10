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
})({"kHAxc":[function(require,module,exports,__globalThis) {
var global = arguments[3];
var HMR_HOST = null;
var HMR_PORT = 1234;
var HMR_SERVER_PORT = 1234;
var HMR_SECURE = false;
var HMR_ENV_HASH = "febfef3c5b467b16";
var HMR_USE_SSE = false;
module.bundle.HMR_BUNDLE_ID = "de114a05287d5a58";
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

},{}],"jqpVZ":[function(require,module,exports,__globalThis) {
var parcelHelpers = require("@parcel/transformer-js/src/esmodule-helpers.js");
parcelHelpers.defineInteropFlag(exports);
parcelHelpers.export(exports, "diagram", ()=>me);
var _chunk6IZS222MMjs = require("./chunk-6IZS222M.mjs");
var _chunkTI4EEUUGMjs = require("./chunk-TI4EEUUG.mjs");
var _chunkNQURTBEVMjs = require("./chunk-NQURTBEV.mjs");
var _chunkGTKDMUJJMjs = require("./chunk-GTKDMUJJ.mjs");
var U = function() {
    var t = (0, _chunkGTKDMUJJMjs.a)(function(g, n, a, o) {
        for(a = a || {}, o = g.length; o--; a[g[o]] = n);
        return a;
    }, "o"), e = [
        6,
        8,
        10,
        11,
        12,
        14,
        16,
        17,
        18
    ], s = [
        1,
        9
    ], c = [
        1,
        10
    ], i = [
        1,
        11
    ], u = [
        1,
        12
    ], h = [
        1,
        13
    ], d = [
        1,
        14
    ], f = {
        trace: (0, _chunkGTKDMUJJMjs.a)(function() {}, "trace"),
        yy: {},
        symbols_: {
            error: 2,
            start: 3,
            journey: 4,
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
            taskName: 18,
            taskData: 19,
            $accept: 0,
            $end: 1
        },
        terminals_: {
            2: "error",
            4: "journey",
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
            18: "taskName",
            19: "taskData"
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
                2
            ]
        ],
        performAction: (0, _chunkGTKDMUJJMjs.a)(function(n, a, o, y, p, l, M) {
            var _ = l.length - 1;
            switch(p){
                case 1:
                    return l[_ - 1];
                case 2:
                    this.$ = [];
                    break;
                case 3:
                    l[_ - 1].push(l[_]), this.$ = l[_ - 1];
                    break;
                case 4:
                case 5:
                    this.$ = l[_];
                    break;
                case 6:
                case 7:
                    this.$ = [];
                    break;
                case 8:
                    y.setDiagramTitle(l[_].substr(6)), this.$ = l[_].substr(6);
                    break;
                case 9:
                    this.$ = l[_].trim(), y.setAccTitle(this.$);
                    break;
                case 10:
                case 11:
                    this.$ = l[_].trim(), y.setAccDescription(this.$);
                    break;
                case 12:
                    y.addSection(l[_].substr(8)), this.$ = l[_].substr(8);
                    break;
                case 13:
                    y.addTask(l[_ - 1], l[_]), this.$ = "task";
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
            t(e, [
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
                11: s,
                12: c,
                14: i,
                16: u,
                17: h,
                18: d
            },
            t(e, [
                2,
                7
            ], {
                1: [
                    2,
                    1
                ]
            }),
            t(e, [
                2,
                3
            ]),
            {
                9: 15,
                11: s,
                12: c,
                14: i,
                16: u,
                17: h,
                18: d
            },
            t(e, [
                2,
                5
            ]),
            t(e, [
                2,
                6
            ]),
            t(e, [
                2,
                8
            ]),
            {
                13: [
                    1,
                    16
                ]
            },
            {
                15: [
                    1,
                    17
                ]
            },
            t(e, [
                2,
                11
            ]),
            t(e, [
                2,
                12
            ]),
            {
                19: [
                    1,
                    18
                ]
            },
            t(e, [
                2,
                4
            ]),
            t(e, [
                2,
                9
            ]),
            t(e, [
                2,
                10
            ]),
            t(e, [
                2,
                13
            ])
        ],
        defaultActions: {},
        parseError: (0, _chunkGTKDMUJJMjs.a)(function(n, a) {
            if (a.recoverable) this.trace(n);
            else {
                var o = new Error(n);
                throw o.hash = a, o;
            }
        }, "parseError"),
        parse: (0, _chunkGTKDMUJJMjs.a)(function(n) {
            var a = this, o = [
                0
            ], y = [], p = [
                null
            ], l = [], M = this.table, _ = "", N = 0, et = 0, nt = 0, Tt = 2, rt = 1, Mt = l.slice.call(arguments, 1), k = Object.create(this.lexer), C = {
                yy: {}
            };
            for(var O in this.yy)Object.prototype.hasOwnProperty.call(this.yy, O) && (C.yy[O] = this.yy[O]);
            k.setInput(n, C.yy), C.yy.lexer = k, C.yy.parser = this, typeof k.yylloc > "u" && (k.yylloc = {});
            var q = k.yylloc;
            l.push(q);
            var St = k.options && k.options.ranges;
            typeof C.yy.parseError == "function" ? this.parseError = C.yy.parseError : this.parseError = Object.getPrototypeOf(this).parseError;
            function Gt(v) {
                o.length = o.length - 2 * v, p.length = p.length - v, l.length = l.length - v;
            }
            (0, _chunkGTKDMUJJMjs.a)(Gt, "popStack");
            function $t() {
                var v;
                return v = y.pop() || k.lex() || rt, typeof v != "number" && (v instanceof Array && (y = v, v = y.pop()), v = a.symbols_[v] || v), v;
            }
            (0, _chunkGTKDMUJJMjs.a)($t, "lex");
            for(var b, D, P, w, Ht, W, A = {}, B, S, it, j;;){
                if (P = o[o.length - 1], this.defaultActions[P] ? w = this.defaultActions[P] : ((b === null || typeof b > "u") && (b = $t()), w = M[P] && M[P][b]), typeof w > "u" || !w.length || !w[0]) {
                    var X = "";
                    j = [];
                    for(B in M[P])this.terminals_[B] && B > Tt && j.push("'" + this.terminals_[B] + "'");
                    k.showPosition ? X = "Parse error on line " + (N + 1) + `:
` + k.showPosition() + `
Expecting ` + j.join(", ") + ", got '" + (this.terminals_[b] || b) + "'" : X = "Parse error on line " + (N + 1) + ": Unexpected " + (b == rt ? "end of input" : "'" + (this.terminals_[b] || b) + "'"), this.parseError(X, {
                        text: k.match,
                        token: this.terminals_[b] || b,
                        line: k.yylineno,
                        loc: q,
                        expected: j
                    });
                }
                if (w[0] instanceof Array && w.length > 1) throw new Error("Parse Error: multiple actions possible at state: " + P + ", token: " + b);
                switch(w[0]){
                    case 1:
                        o.push(b), p.push(k.yytext), l.push(k.yylloc), o.push(w[1]), b = null, D ? (b = D, D = null) : (et = k.yyleng, _ = k.yytext, N = k.yylineno, q = k.yylloc, nt > 0 && nt--);
                        break;
                    case 2:
                        if (S = this.productions_[w[1]][1], A.$ = p[p.length - S], A._$ = {
                            first_line: l[l.length - (S || 1)].first_line,
                            last_line: l[l.length - 1].last_line,
                            first_column: l[l.length - (S || 1)].first_column,
                            last_column: l[l.length - 1].last_column
                        }, St && (A._$.range = [
                            l[l.length - (S || 1)].range[0],
                            l[l.length - 1].range[1]
                        ]), W = this.performAction.apply(A, [
                            _,
                            et,
                            N,
                            C.yy,
                            w[1],
                            p,
                            l
                        ].concat(Mt)), typeof W < "u") return W;
                        S && (o = o.slice(0, -1 * S * 2), p = p.slice(0, -1 * S), l = l.slice(0, -1 * S)), o.push(this.productions_[w[1]][0]), p.push(A.$), l.push(A._$), it = M[o[o.length - 2]][o[o.length - 1]], o.push(it);
                        break;
                    case 3:
                        return !0;
                }
            }
            return !0;
        }, "parse")
    }, x = function() {
        var g = {
            EOF: 1,
            parseError: (0, _chunkGTKDMUJJMjs.a)(function(a, o) {
                if (this.yy.parser) this.yy.parser.parseError(a, o);
                else throw new Error(a);
            }, "parseError"),
            setInput: (0, _chunkGTKDMUJJMjs.a)(function(n, a) {
                return this.yy = a || this.yy || {}, this._input = n, this._more = this._backtrack = this.done = !1, this.yylineno = this.yyleng = 0, this.yytext = this.matched = this.match = "", this.conditionStack = [
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
                var n = this._input[0];
                this.yytext += n, this.yyleng++, this.offset++, this.match += n, this.matched += n;
                var a = n.match(/(?:\r\n?|\n).*/g);
                return a ? (this.yylineno++, this.yylloc.last_line++) : this.yylloc.last_column++, this.options.ranges && this.yylloc.range[1]++, this._input = this._input.slice(1), n;
            }, "input"),
            unput: (0, _chunkGTKDMUJJMjs.a)(function(n) {
                var a = n.length, o = n.split(/(?:\r\n?|\n)/g);
                this._input = n + this._input, this.yytext = this.yytext.substr(0, this.yytext.length - a), this.offset -= a;
                var y = this.match.split(/(?:\r\n?|\n)/g);
                this.match = this.match.substr(0, this.match.length - 1), this.matched = this.matched.substr(0, this.matched.length - 1), o.length - 1 && (this.yylineno -= o.length - 1);
                var p = this.yylloc.range;
                return this.yylloc = {
                    first_line: this.yylloc.first_line,
                    last_line: this.yylineno + 1,
                    first_column: this.yylloc.first_column,
                    last_column: o ? (o.length === y.length ? this.yylloc.first_column : 0) + y[y.length - o.length].length - o[0].length : this.yylloc.first_column - a
                }, this.options.ranges && (this.yylloc.range = [
                    p[0],
                    p[0] + this.yyleng - a
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
            less: (0, _chunkGTKDMUJJMjs.a)(function(n) {
                this.unput(this.match.slice(n));
            }, "less"),
            pastInput: (0, _chunkGTKDMUJJMjs.a)(function() {
                var n = this.matched.substr(0, this.matched.length - this.match.length);
                return (n.length > 20 ? "..." : "") + n.substr(-20).replace(/\n/g, "");
            }, "pastInput"),
            upcomingInput: (0, _chunkGTKDMUJJMjs.a)(function() {
                var n = this.match;
                return n.length < 20 && (n += this._input.substr(0, 20 - n.length)), (n.substr(0, 20) + (n.length > 20 ? "..." : "")).replace(/\n/g, "");
            }, "upcomingInput"),
            showPosition: (0, _chunkGTKDMUJJMjs.a)(function() {
                var n = this.pastInput(), a = new Array(n.length + 1).join("-");
                return n + this.upcomingInput() + `
` + a + "^";
            }, "showPosition"),
            test_match: (0, _chunkGTKDMUJJMjs.a)(function(n, a) {
                var o, y, p;
                if (this.options.backtrack_lexer && (p = {
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
                }, this.options.ranges && (p.yylloc.range = this.yylloc.range.slice(0))), y = n[0].match(/(?:\r\n?|\n).*/g), y && (this.yylineno += y.length), this.yylloc = {
                    first_line: this.yylloc.last_line,
                    last_line: this.yylineno + 1,
                    first_column: this.yylloc.last_column,
                    last_column: y ? y[y.length - 1].length - y[y.length - 1].match(/\r?\n?/)[0].length : this.yylloc.last_column + n[0].length
                }, this.yytext += n[0], this.match += n[0], this.matches = n, this.yyleng = this.yytext.length, this.options.ranges && (this.yylloc.range = [
                    this.offset,
                    this.offset += this.yyleng
                ]), this._more = !1, this._backtrack = !1, this._input = this._input.slice(n[0].length), this.matched += n[0], o = this.performAction.call(this, this.yy, this, a, this.conditionStack[this.conditionStack.length - 1]), this.done && this._input && (this.done = !1), o) return o;
                if (this._backtrack) {
                    for(var l in p)this[l] = p[l];
                    return !1;
                }
                return !1;
            }, "test_match"),
            next: (0, _chunkGTKDMUJJMjs.a)(function() {
                if (this.done) return this.EOF;
                this._input || (this.done = !0);
                var n, a, o, y;
                this._more || (this.yytext = "", this.match = "");
                for(var p = this._currentRules(), l = 0; l < p.length; l++)if (o = this._input.match(this.rules[p[l]]), o && (!a || o[0].length > a[0].length)) {
                    if (a = o, y = l, this.options.backtrack_lexer) {
                        if (n = this.test_match(o, p[l]), n !== !1) return n;
                        if (this._backtrack) {
                            a = !1;
                            continue;
                        } else return !1;
                    } else if (!this.options.flex) break;
                }
                return a ? (n = this.test_match(a, p[y]), n !== !1 ? n : !1) : this._input === "" ? this.EOF : this.parseError("Lexical error on line " + (this.yylineno + 1) + `. Unrecognized text.
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
            performAction: (0, _chunkGTKDMUJJMjs.a)(function(a, o, y, p) {
                var l = p;
                switch(y){
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
                        return 18;
                    case 16:
                        return 19;
                    case 17:
                        return ":";
                    case 18:
                        return 6;
                    case 19:
                        return "INVALID";
                }
            }, "anonymous"),
            rules: [
                /^(?:%(?!\{)[^\n]*)/i,
                /^(?:[^\}]%%[^\n]*)/i,
                /^(?:[\n]+)/i,
                /^(?:\s+)/i,
                /^(?:#[^\n]*)/i,
                /^(?:journey\b)/i,
                /^(?:title\s[^#\n;]+)/i,
                /^(?:accTitle\s*:\s*)/i,
                /^(?:(?!\n||)*[^\n]*)/i,
                /^(?:accDescr\s*:\s*)/i,
                /^(?:(?!\n||)*[^\n]*)/i,
                /^(?:accDescr\s*\{\s*)/i,
                /^(?:[\}])/i,
                /^(?:[^\}]*)/i,
                /^(?:section\s[^#:\n;]+)/i,
                /^(?:[^#:\n;]+)/i,
                /^(?::[^#\n;]+)/i,
                /^(?::)/i,
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
                        18,
                        19
                    ],
                    inclusive: !0
                }
            }
        };
        return g;
    }();
    f.lexer = x;
    function m() {
        this.yy = {};
    }
    return (0, _chunkGTKDMUJJMjs.a)(m, "Parser"), m.prototype = f, f.Parser = m, new m;
}();
U.parser = U;
var gt = U;
var V = "", Z = [], L = [], R = [], Et = (0, _chunkGTKDMUJJMjs.a)(function() {
    Z.length = 0, L.length = 0, V = "", R.length = 0, (0, _chunkNQURTBEVMjs.P)();
}, "clear"), Ct = (0, _chunkGTKDMUJJMjs.a)(function(t) {
    V = t, Z.push(t);
}, "addSection"), Pt = (0, _chunkGTKDMUJJMjs.a)(function() {
    return Z;
}, "getSections"), It = (0, _chunkGTKDMUJJMjs.a)(function() {
    let t = mt(), e = 100, s = 0;
    for(; !t && s < e;)t = mt(), s++;
    return L.push(...R), L;
}, "getTasks"), At = (0, _chunkGTKDMUJJMjs.a)(function() {
    let t = [];
    return L.forEach((s)=>{
        s.people && t.push(...s.people);
    }), [
        ...new Set(t)
    ].sort();
}, "updateActors"), Vt = (0, _chunkGTKDMUJJMjs.a)(function(t, e) {
    let s = e.substr(1).split(":"), c = 0, i = [];
    s.length === 1 ? (c = Number(s[0]), i = []) : (c = Number(s[0]), i = s[1].split(","));
    let u = i.map((d)=>d.trim()), h = {
        section: V,
        type: V,
        people: u,
        task: t,
        score: c
    };
    R.push(h);
}, "addTask"), Ft = (0, _chunkGTKDMUJJMjs.a)(function(t) {
    let e = {
        section: V,
        type: V,
        description: t,
        task: t,
        classes: []
    };
    L.push(e);
}, "addTaskOrg"), mt = (0, _chunkGTKDMUJJMjs.a)(function() {
    let t = (0, _chunkGTKDMUJJMjs.a)(function(s) {
        return R[s].processed;
    }, "compileTask"), e = !0;
    for (let [s, c] of R.entries())t(s), e = e && c.processed;
    return e;
}, "compileTasks"), Lt = (0, _chunkGTKDMUJJMjs.a)(function() {
    return At();
}, "getActors"), J = {
    getConfig: (0, _chunkGTKDMUJJMjs.a)(()=>(0, _chunkNQURTBEVMjs.X)().journey, "getConfig"),
    clear: Et,
    setDiagramTitle: (0, _chunkNQURTBEVMjs.U),
    getDiagramTitle: (0, _chunkNQURTBEVMjs.V),
    setAccTitle: (0, _chunkNQURTBEVMjs.Q),
    getAccTitle: (0, _chunkNQURTBEVMjs.R),
    setAccDescription: (0, _chunkNQURTBEVMjs.S),
    getAccDescription: (0, _chunkNQURTBEVMjs.T),
    addSection: Ct,
    getSections: Pt,
    getTasks: It,
    addTask: Vt,
    addTaskOrg: Ft,
    getActors: Lt
};
var Rt = (0, _chunkGTKDMUJJMjs.a)((t)=>`.label {
    font-family: 'trebuchet ms', verdana, arial, sans-serif;
    font-family: var(--mermaid-font-family);
    color: ${t.textColor};
  }
  .mouth {
    stroke: #666;
  }

  line {
    stroke: ${t.textColor}
  }

  .legend {
    fill: ${t.textColor};
  }

  .label text {
    fill: #333;
  }
  .label {
    color: ${t.textColor}
  }

  .face {
    ${t.faceColor ? `fill: ${t.faceColor}` : "fill: #FFF8DC"};
    stroke: #999;
  }

  .node rect,
  .node circle,
  .node ellipse,
  .node polygon,
  .node path {
    fill: ${t.mainBkg};
    stroke: ${t.nodeBorder};
    stroke-width: 1px;
  }

  .node .label {
    text-align: center;
  }
  .node.clickable {
    cursor: pointer;
  }

  .arrowheadPath {
    fill: ${t.arrowheadColor};
  }

  .edgePath .path {
    stroke: ${t.lineColor};
    stroke-width: 1.5px;
  }

  .flowchart-link {
    stroke: ${t.lineColor};
    fill: none;
  }

  .edgeLabel {
    background-color: ${t.edgeLabelBackground};
    rect {
      opacity: 0.5;
    }
    text-align: center;
  }

  .cluster rect {
  }

  .cluster text {
    fill: ${t.titleColor};
  }

  div.mermaidTooltip {
    position: absolute;
    text-align: center;
    max-width: 200px;
    padding: 2px;
    font-family: 'trebuchet ms', verdana, arial, sans-serif;
    font-family: var(--mermaid-font-family);
    font-size: 12px;
    background: ${t.tertiaryColor};
    border: 1px solid ${t.border2};
    border-radius: 2px;
    pointer-events: none;
    z-index: 100;
  }

  .task-type-0, .section-type-0  {
    ${t.fillType0 ? `fill: ${t.fillType0}` : ""};
  }
  .task-type-1, .section-type-1  {
    ${t.fillType0 ? `fill: ${t.fillType1}` : ""};
  }
  .task-type-2, .section-type-2  {
    ${t.fillType0 ? `fill: ${t.fillType2}` : ""};
  }
  .task-type-3, .section-type-3  {
    ${t.fillType0 ? `fill: ${t.fillType3}` : ""};
  }
  .task-type-4, .section-type-4  {
    ${t.fillType0 ? `fill: ${t.fillType4}` : ""};
  }
  .task-type-5, .section-type-5  {
    ${t.fillType0 ? `fill: ${t.fillType5}` : ""};
  }
  .task-type-6, .section-type-6  {
    ${t.fillType0 ? `fill: ${t.fillType6}` : ""};
  }
  .task-type-7, .section-type-7  {
    ${t.fillType0 ? `fill: ${t.fillType7}` : ""};
  }

  .actor-0 {
    ${t.actor0 ? `fill: ${t.actor0}` : ""};
  }
  .actor-1 {
    ${t.actor1 ? `fill: ${t.actor1}` : ""};
  }
  .actor-2 {
    ${t.actor2 ? `fill: ${t.actor2}` : ""};
  }
  .actor-3 {
    ${t.actor3 ? `fill: ${t.actor3}` : ""};
  }
  .actor-4 {
    ${t.actor4 ? `fill: ${t.actor4}` : ""};
  }
  .actor-5 {
    ${t.actor5 ? `fill: ${t.actor5}` : ""};
  }
`, "getStyles"), xt = Rt;
var K = (0, _chunkGTKDMUJJMjs.a)(function(t, e) {
    return (0, _chunk6IZS222MMjs.a)(t, e);
}, "drawRect"), Nt = (0, _chunkGTKDMUJJMjs.a)(function(t, e) {
    let c = t.append("circle").attr("cx", e.cx).attr("cy", e.cy).attr("class", "face").attr("r", 15).attr("stroke-width", 2).attr("overflow", "visible"), i = t.append("g");
    i.append("circle").attr("cx", e.cx - 5).attr("cy", e.cy - 5).attr("r", 1.5).attr("stroke-width", 2).attr("fill", "#666").attr("stroke", "#666"), i.append("circle").attr("cx", e.cx + 5).attr("cy", e.cy - 5).attr("r", 1.5).attr("stroke-width", 2).attr("fill", "#666").attr("stroke", "#666");
    function u(f) {
        let x = (0, _chunkNQURTBEVMjs.Aa)().startAngle(Math.PI / 2).endAngle(3 * (Math.PI / 2)).innerRadius(7.5).outerRadius(6.8181818181818175);
        f.append("path").attr("class", "mouth").attr("d", x).attr("transform", "translate(" + e.cx + "," + (e.cy + 2) + ")");
    }
    (0, _chunkGTKDMUJJMjs.a)(u, "smile");
    function h(f) {
        let x = (0, _chunkNQURTBEVMjs.Aa)().startAngle(3 * Math.PI / 2).endAngle(5 * (Math.PI / 2)).innerRadius(7.5).outerRadius(6.8181818181818175);
        f.append("path").attr("class", "mouth").attr("d", x).attr("transform", "translate(" + e.cx + "," + (e.cy + 7) + ")");
    }
    (0, _chunkGTKDMUJJMjs.a)(h, "sad");
    function d(f) {
        f.append("line").attr("class", "mouth").attr("stroke", 2).attr("x1", e.cx - 5).attr("y1", e.cy + 7).attr("x2", e.cx + 5).attr("y2", e.cy + 7).attr("class", "mouth").attr("stroke-width", "1px").attr("stroke", "#666");
    }
    return (0, _chunkGTKDMUJJMjs.a)(d, "ambivalent"), e.score > 3 ? u(i) : e.score < 3 ? h(i) : d(i), c;
}, "drawFace"), _t = (0, _chunkGTKDMUJJMjs.a)(function(t, e) {
    let s = t.append("circle");
    return s.attr("cx", e.cx), s.attr("cy", e.cy), s.attr("class", "actor-" + e.pos), s.attr("fill", e.fill), s.attr("stroke", e.stroke), s.attr("r", e.r), s.class !== void 0 && s.attr("class", s.class), e.title !== void 0 && s.append("title").text(e.title), s;
}, "drawCircle"), bt = (0, _chunkGTKDMUJJMjs.a)(function(t, e) {
    return (0, _chunk6IZS222MMjs.c)(t, e);
}, "drawText"), Bt = (0, _chunkGTKDMUJJMjs.a)(function(t, e) {
    function s(i, u, h, d, f) {
        return i + "," + u + " " + (i + h) + "," + u + " " + (i + h) + "," + (u + d - f) + " " + (i + h - f * 1.2) + "," + (u + d) + " " + i + "," + (u + d);
    }
    (0, _chunkGTKDMUJJMjs.a)(s, "genPoints");
    let c = t.append("polygon");
    c.attr("points", s(e.x, e.y, 50, 20, 7)), c.attr("class", "labelBox"), e.y = e.y + e.labelMargin, e.x = e.x + .5 * e.labelMargin, bt(t, e);
}, "drawLabel"), jt = (0, _chunkGTKDMUJJMjs.a)(function(t, e, s) {
    let c = t.append("g"), i = (0, _chunk6IZS222MMjs.f)();
    i.x = e.x, i.y = e.y, i.fill = e.fill, i.width = s.width * e.taskCount + s.diagramMarginX * (e.taskCount - 1), i.height = s.height, i.class = "journey-section section-type-" + e.num, i.rx = 3, i.ry = 3, K(c, i), vt(s)(e.text, c, i.x, i.y, i.width, i.height, {
        class: "journey-section section-type-" + e.num
    }, s, e.colour);
}, "drawSection"), kt = -1, zt = (0, _chunkGTKDMUJJMjs.a)(function(t, e, s) {
    let c = e.x + s.width / 2, i = t.append("g");
    kt++;
    let u = 450;
    i.append("line").attr("id", "task" + kt).attr("x1", c).attr("y1", e.y).attr("x2", c).attr("y2", u).attr("class", "task-line").attr("stroke-width", "1px").attr("stroke-dasharray", "4 2").attr("stroke", "#666"), Nt(i, {
        cx: c,
        cy: 300 + (5 - e.score) * 30,
        score: e.score
    });
    let h = (0, _chunk6IZS222MMjs.f)();
    h.x = e.x, h.y = e.y, h.fill = e.fill, h.width = s.width, h.height = s.height, h.class = "task task-type-" + e.num, h.rx = 3, h.ry = 3, K(i, h);
    let d = e.x + 14;
    e.people.forEach((f)=>{
        let x = e.actors[f].color, m = {
            cx: d,
            cy: e.y,
            r: 7,
            fill: x,
            stroke: "#000",
            title: f,
            pos: e.actors[f].position
        };
        _t(i, m), d += 10;
    }), vt(s)(e.task, i, h.x, h.y, h.width, h.height, {
        class: "task"
    }, s, e.colour);
}, "drawTask"), Yt = (0, _chunkGTKDMUJJMjs.a)(function(t, e) {
    (0, _chunk6IZS222MMjs.b)(t, e);
}, "drawBackgroundRect"), vt = function() {
    function t(i, u, h, d, f, x, m, g) {
        let n = u.append("text").attr("x", h + f / 2).attr("y", d + x / 2 + 5).style("font-color", g).style("text-anchor", "middle").text(i);
        c(n, m);
    }
    (0, _chunkGTKDMUJJMjs.a)(t, "byText");
    function e(i, u, h, d, f, x, m, g, n) {
        let { taskFontSize: a, taskFontFamily: o } = g, y = i.split(/<br\s*\/?>/gi);
        for(let p = 0; p < y.length; p++){
            let l = p * a - a * (y.length - 1) / 2, M = u.append("text").attr("x", h + f / 2).attr("y", d).attr("fill", n).style("text-anchor", "middle").style("font-size", a).style("font-family", o);
            M.append("tspan").attr("x", h + f / 2).attr("dy", l).text(y[p]), M.attr("y", d + x / 2).attr("dominant-baseline", "central").attr("alignment-baseline", "central"), c(M, m);
        }
    }
    (0, _chunkGTKDMUJJMjs.a)(e, "byTspan");
    function s(i, u, h, d, f, x, m, g) {
        let n = u.append("switch"), o = n.append("foreignObject").attr("x", h).attr("y", d).attr("width", f).attr("height", x).attr("position", "fixed").append("xhtml:div").style("display", "table").style("height", "100%").style("width", "100%");
        o.append("div").attr("class", "label").style("display", "table-cell").style("text-align", "center").style("vertical-align", "middle").text(i), e(i, n, h, d, f, x, m, g), c(o, m);
    }
    (0, _chunkGTKDMUJJMjs.a)(s, "byFo");
    function c(i, u) {
        for(let h in u)h in u && i.attr(h, u[h]);
    }
    return (0, _chunkGTKDMUJJMjs.a)(c, "_setTextAttrs"), function(i) {
        return i.textPlacement === "fo" ? s : i.textPlacement === "old" ? t : e;
    };
}(), Ot = (0, _chunkGTKDMUJJMjs.a)(function(t) {
    t.append("defs").append("marker").attr("id", "arrowhead").attr("refX", 5).attr("refY", 2).attr("markerWidth", 6).attr("markerHeight", 4).attr("orient", "auto").append("path").attr("d", "M 0,0 V 4 L6,2 Z");
}, "initGraphics"), F = {
    drawRect: K,
    drawCircle: _t,
    drawSection: jt,
    drawText: bt,
    drawLabel: Bt,
    drawTask: zt,
    drawBackgroundRect: Yt,
    initGraphics: Ot
};
var qt = (0, _chunkGTKDMUJJMjs.a)(function(t) {
    Object.keys(t).forEach(function(s) {
        Y[s] = t[s];
    });
}, "setConf"), E = {};
function Dt(t) {
    let e = (0, _chunkNQURTBEVMjs.X)().journey, s = 60;
    Object.keys(E).forEach((c)=>{
        let i = E[c].color, u = {
            cx: 20,
            cy: s,
            r: 7,
            fill: i,
            stroke: "#000",
            pos: E[c].position
        };
        F.drawCircle(t, u);
        let h = {
            x: 40,
            y: s + 7,
            fill: "#666",
            text: c,
            textMargin: e.boxTextMargin | 5
        };
        F.drawText(t, h), s += 20;
    });
}
(0, _chunkGTKDMUJJMjs.a)(Dt, "drawActorLegend");
var Y = (0, _chunkNQURTBEVMjs.X)().journey, I = Y.leftMargin, Wt = (0, _chunkGTKDMUJJMjs.a)(function(t, e, s, c) {
    let i = (0, _chunkNQURTBEVMjs.X)().journey, u = (0, _chunkNQURTBEVMjs.X)().securityLevel, h;
    u === "sandbox" && (h = (0, _chunkNQURTBEVMjs.fa)("#i" + e));
    let d = u === "sandbox" ? (0, _chunkNQURTBEVMjs.fa)(h.nodes()[0].contentDocument.body) : (0, _chunkNQURTBEVMjs.fa)("body");
    T.init();
    let f = d.select("#" + e);
    F.initGraphics(f);
    let x = c.db.getTasks(), m = c.db.getDiagramTitle(), g = c.db.getActors();
    for(let l in E)delete E[l];
    let n = 0;
    g.forEach((l)=>{
        E[l] = {
            color: i.actorColours[n % i.actorColours.length],
            position: n
        }, n++;
    }), Dt(f), T.insert(0, 0, I, Object.keys(E).length * 50), Xt(f, x, 0);
    let a = T.getBounds();
    m && f.append("text").text(m).attr("x", I).attr("font-size", "4ex").attr("font-weight", "bold").attr("y", 25);
    let o = a.stopy - a.starty + 2 * i.diagramMarginY, y = I + a.stopx + 2 * i.diagramMarginX;
    (0, _chunkNQURTBEVMjs.M)(f, o, y, i.useMaxWidth), f.append("line").attr("x1", I).attr("y1", i.height * 4).attr("x2", y - I - 4).attr("y2", i.height * 4).attr("stroke-width", 4).attr("stroke", "black").attr("marker-end", "url(#arrowhead)");
    let p = m ? 70 : 0;
    f.attr("viewBox", `${a.startx} -25 ${y} ${o + p}`), f.attr("preserveAspectRatio", "xMinYMin meet"), f.attr("height", o + p + 25);
}, "draw"), T = {
    data: {
        startx: void 0,
        stopx: void 0,
        starty: void 0,
        stopy: void 0
    },
    verticalPos: 0,
    sequenceItems: [],
    init: (0, _chunkGTKDMUJJMjs.a)(function() {
        this.sequenceItems = [], this.data = {
            startx: void 0,
            stopx: void 0,
            starty: void 0,
            stopy: void 0
        }, this.verticalPos = 0;
    }, "init"),
    updateVal: (0, _chunkGTKDMUJJMjs.a)(function(t, e, s, c) {
        t[e] === void 0 ? t[e] = s : t[e] = c(s, t[e]);
    }, "updateVal"),
    updateBounds: (0, _chunkGTKDMUJJMjs.a)(function(t, e, s, c) {
        let i = (0, _chunkNQURTBEVMjs.X)().journey, u = this, h = 0;
        function d(f) {
            return (0, _chunkGTKDMUJJMjs.a)(function(m) {
                h++;
                let g = u.sequenceItems.length - h + 1;
                u.updateVal(m, "starty", e - g * i.boxMargin, Math.min), u.updateVal(m, "stopy", c + g * i.boxMargin, Math.max), u.updateVal(T.data, "startx", t - g * i.boxMargin, Math.min), u.updateVal(T.data, "stopx", s + g * i.boxMargin, Math.max), f !== "activation" && (u.updateVal(m, "startx", t - g * i.boxMargin, Math.min), u.updateVal(m, "stopx", s + g * i.boxMargin, Math.max), u.updateVal(T.data, "starty", e - g * i.boxMargin, Math.min), u.updateVal(T.data, "stopy", c + g * i.boxMargin, Math.max));
            }, "updateItemBounds");
        }
        (0, _chunkGTKDMUJJMjs.a)(d, "updateFn"), this.sequenceItems.forEach(d());
    }, "updateBounds"),
    insert: (0, _chunkGTKDMUJJMjs.a)(function(t, e, s, c) {
        let i = Math.min(t, s), u = Math.max(t, s), h = Math.min(e, c), d = Math.max(e, c);
        this.updateVal(T.data, "startx", i, Math.min), this.updateVal(T.data, "starty", h, Math.min), this.updateVal(T.data, "stopx", u, Math.max), this.updateVal(T.data, "stopy", d, Math.max), this.updateBounds(i, h, u, d);
    }, "insert"),
    bumpVerticalPos: (0, _chunkGTKDMUJJMjs.a)(function(t) {
        this.verticalPos = this.verticalPos + t, this.data.stopy = this.verticalPos;
    }, "bumpVerticalPos"),
    getVerticalPos: (0, _chunkGTKDMUJJMjs.a)(function() {
        return this.verticalPos;
    }, "getVerticalPos"),
    getBounds: (0, _chunkGTKDMUJJMjs.a)(function() {
        return this.data;
    }, "getBounds")
}, Q = Y.sectionFills, wt = Y.sectionColours, Xt = (0, _chunkGTKDMUJJMjs.a)(function(t, e, s) {
    let c = (0, _chunkNQURTBEVMjs.X)().journey, i = "", u = c.height * 2 + c.diagramMarginY, h = s + u, d = 0, f = "#CCC", x = "black", m = 0;
    for (let [g, n] of e.entries()){
        if (i !== n.section) {
            f = Q[d % Q.length], m = d % Q.length, x = wt[d % wt.length];
            let o = 0, y = n.section;
            for(let l = g; l < e.length && e[l].section == y; l++)o = o + 1;
            let p = {
                x: g * c.taskMargin + g * c.width + I,
                y: 50,
                text: n.section,
                fill: f,
                num: m,
                colour: x,
                taskCount: o
            };
            F.drawSection(t, p, c), i = n.section, d++;
        }
        let a = n.people.reduce((o, y)=>(E[y] && (o[y] = E[y]), o), {});
        n.x = g * c.taskMargin + g * c.width + I, n.y = h, n.width = c.diagramMarginX, n.height = c.diagramMarginY, n.colour = x, n.fill = f, n.num = m, n.actors = a, F.drawTask(t, n, c), T.insert(n.x, n.y, n.x + n.width + c.taskMargin, 450);
    }
}, "drawTasks"), tt = {
    setConf: qt,
    draw: Wt
};
var me = {
    parser: gt,
    db: J,
    renderer: tt,
    styles: xt,
    init: (0, _chunkGTKDMUJJMjs.a)((t)=>{
        tt.setConf(t.journey), J.clear();
    }, "init")
};

},{"./chunk-6IZS222M.mjs":"7jH0l","./chunk-TI4EEUUG.mjs":"8SKrN","./chunk-NQURTBEV.mjs":"iASFe","./chunk-GTKDMUJJ.mjs":"fruhx","@parcel/transformer-js/src/esmodule-helpers.js":"1cdyk"}],"7jH0l":[function(require,module,exports,__globalThis) {
var parcelHelpers = require("@parcel/transformer-js/src/esmodule-helpers.js");
parcelHelpers.defineInteropFlag(exports);
parcelHelpers.export(exports, "a", ()=>x);
parcelHelpers.export(exports, "b", ()=>g);
parcelHelpers.export(exports, "c", ()=>y);
parcelHelpers.export(exports, "d", ()=>d);
parcelHelpers.export(exports, "e", ()=>E);
parcelHelpers.export(exports, "f", ()=>h);
parcelHelpers.export(exports, "g", ()=>f);
var _chunkTI4EEUUGMjs = require("./chunk-TI4EEUUG.mjs");
var _chunkNQURTBEVMjs = require("./chunk-NQURTBEV.mjs");
var _chunkGTKDMUJJMjs = require("./chunk-GTKDMUJJ.mjs");
var i = (0, _chunkGTKDMUJJMjs.e)((0, _chunkTI4EEUUGMjs.a)(), 1);
var x = (0, _chunkGTKDMUJJMjs.a)((n, t)=>{
    let e = n.append("rect");
    if (e.attr("x", t.x), e.attr("y", t.y), e.attr("fill", t.fill), e.attr("stroke", t.stroke), e.attr("width", t.width), e.attr("height", t.height), t.name && e.attr("name", t.name), t.rx && e.attr("rx", t.rx), t.ry && e.attr("ry", t.ry), t.attrs !== void 0) for(let r in t.attrs)e.attr(r, t.attrs[r]);
    return t.class && e.attr("class", t.class), e;
}, "drawRect"), g = (0, _chunkGTKDMUJJMjs.a)((n, t)=>{
    let e = {
        x: t.startx,
        y: t.starty,
        width: t.stopx - t.startx,
        height: t.stopy - t.starty,
        fill: t.fill,
        stroke: t.stroke,
        class: "rect"
    };
    x(n, e).lower();
}, "drawBackgroundRect"), y = (0, _chunkGTKDMUJJMjs.a)((n, t)=>{
    let e = t.text.replace((0, _chunkNQURTBEVMjs.E), " "), r = n.append("text");
    r.attr("x", t.x), r.attr("y", t.y), r.attr("class", "legend"), r.style("text-anchor", t.anchor), t.class && r.attr("class", t.class);
    let s = r.append("tspan");
    return s.attr("x", t.x + t.textMargin * 2), s.text(e), r;
}, "drawText"), d = (0, _chunkGTKDMUJJMjs.a)((n, t, e, r)=>{
    let s = n.append("image");
    s.attr("x", t), s.attr("y", e);
    let a = (0, i.sanitizeUrl)(r);
    s.attr("xlink:href", a);
}, "drawImage"), E = (0, _chunkGTKDMUJJMjs.a)((n, t, e, r)=>{
    let s = n.append("use");
    s.attr("x", t), s.attr("y", e);
    let a = (0, i.sanitizeUrl)(r);
    s.attr("xlink:href", `#${a}`);
}, "drawEmbeddedImage"), h = (0, _chunkGTKDMUJJMjs.a)(()=>({
        x: 0,
        y: 0,
        width: 100,
        height: 100,
        fill: "#EDF2AE",
        stroke: "#666",
        anchor: "start",
        rx: 0,
        ry: 0
    }), "getNoteRect"), f = (0, _chunkGTKDMUJJMjs.a)(()=>({
        x: 0,
        y: 0,
        width: 100,
        height: 100,
        "text-anchor": "start",
        style: "#666",
        textMargin: 0,
        rx: 0,
        ry: 0,
        tspan: !0
    }), "getTextObj");

},{"./chunk-TI4EEUUG.mjs":"8SKrN","./chunk-NQURTBEV.mjs":"iASFe","./chunk-GTKDMUJJ.mjs":"fruhx","@parcel/transformer-js/src/esmodule-helpers.js":"1cdyk"}]},["kHAxc"], null, "parcelRequire6955", {})

//# sourceMappingURL=journeyDiagram-VRXW2F6L.287d5a58.js.map
