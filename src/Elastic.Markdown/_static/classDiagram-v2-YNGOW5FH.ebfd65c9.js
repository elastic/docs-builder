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
})({"iae2r":[function(require,module,exports,__globalThis) {
var global = arguments[3];
var HMR_HOST = null;
var HMR_PORT = 1234;
var HMR_SERVER_PORT = 1234;
var HMR_SECURE = false;
var HMR_ENV_HASH = "febfef3c5b467b16";
var HMR_USE_SSE = false;
module.bundle.HMR_BUNDLE_ID = "4633ed27ebfd65c9";
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

},{}],"1dUZT":[function(require,module,exports,__globalThis) {
var parcelHelpers = require("@parcel/transformer-js/src/esmodule-helpers.js");
parcelHelpers.defineInteropFlag(exports);
parcelHelpers.export(exports, "diagram", ()=>diagram);
var _chunkBAJGW65CMjs = require("./chunk-BAJGW65C.mjs");
var _chunkIQOVSUJRMjs = require("./chunk-IQOVSUJR.mjs");
var _chunkB7GIP3BCMjs = require("./chunk-B7GIP3BC.mjs");
var _chunkHKQCUR3CMjs = require("./chunk-HKQCUR3C.mjs");
var _chunkKW7S66XIMjs = require("./chunk-KW7S66XI.mjs");
var _chunkYP6PVJQ3Mjs = require("./chunk-YP6PVJQ3.mjs");
var _chunkCN5XARC6Mjs = require("./chunk-CN5XARC6.mjs");
var _chunkULVYQCHCMjs = require("./chunk-ULVYQCHC.mjs");
var _chunkI7ZFS43CMjs = require("./chunk-I7ZFS43C.mjs");
var _chunkGKOISANMMjs = require("./chunk-GKOISANM.mjs");
var _chunkDD37ZF33Mjs = require("./chunk-DD37ZF33.mjs");
var _chunkTZBO7MLIMjs = require("./chunk-TZBO7MLI.mjs");
var _chunkGRZAG2UZMjs = require("./chunk-GRZAG2UZ.mjs");
var _chunkHD3LK5B5Mjs = require("./chunk-HD3LK5B5.mjs");
var _chunkDLQEHMXDMjs = require("./chunk-DLQEHMXD.mjs");
// src/dagre-wrapper/mermaid-graphlib.js
var clusterDb = {};
var descendants = {};
var parents = {};
var clear3 = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(()=>{
    descendants = {};
    parents = {};
    clusterDb = {};
}, "clear");
var isDescendant = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((id, ancestorId)=>{
    (0, _chunkDD37ZF33Mjs.log).trace("In isDescendant", ancestorId, " ", id, " = ", descendants[ancestorId].includes(id));
    if (descendants[ancestorId].includes(id)) return true;
    return false;
}, "isDescendant");
var edgeInCluster = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((edge, clusterId)=>{
    (0, _chunkDD37ZF33Mjs.log).info("Descendants of ", clusterId, " is ", descendants[clusterId]);
    (0, _chunkDD37ZF33Mjs.log).info("Edge is ", edge);
    if (edge.v === clusterId) return false;
    if (edge.w === clusterId) return false;
    if (!descendants[clusterId]) {
        (0, _chunkDD37ZF33Mjs.log).debug("Tilt, ", clusterId, ",not in descendants");
        return false;
    }
    return descendants[clusterId].includes(edge.v) || isDescendant(edge.v, clusterId) || isDescendant(edge.w, clusterId) || descendants[clusterId].includes(edge.w);
}, "edgeInCluster");
var copy = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((clusterId, graph, newGraph, rootId)=>{
    (0, _chunkDD37ZF33Mjs.log).warn("Copying children of ", clusterId, "root", rootId, "data", graph.node(clusterId), rootId);
    const nodes = graph.children(clusterId) || [];
    if (clusterId !== rootId) nodes.push(clusterId);
    (0, _chunkDD37ZF33Mjs.log).warn("Copying (nodes) clusterId", clusterId, "nodes", nodes);
    nodes.forEach((node)=>{
        if (graph.children(node).length > 0) copy(node, graph, newGraph, rootId);
        else {
            const data = graph.node(node);
            (0, _chunkDD37ZF33Mjs.log).info("cp ", node, " to ", rootId, " with parent ", clusterId);
            newGraph.setNode(node, data);
            if (rootId !== graph.parent(node)) {
                (0, _chunkDD37ZF33Mjs.log).warn("Setting parent", node, graph.parent(node));
                newGraph.setParent(node, graph.parent(node));
            }
            if (clusterId !== rootId && node !== clusterId) {
                (0, _chunkDD37ZF33Mjs.log).debug("Setting parent", node, clusterId);
                newGraph.setParent(node, clusterId);
            } else {
                (0, _chunkDD37ZF33Mjs.log).info("In copy ", clusterId, "root", rootId, "data", graph.node(clusterId), rootId);
                (0, _chunkDD37ZF33Mjs.log).debug("Not Setting parent for node=", node, "cluster!==rootId", clusterId !== rootId, "node!==clusterId", node !== clusterId);
            }
            const edges = graph.edges(node);
            (0, _chunkDD37ZF33Mjs.log).debug("Copying Edges", edges);
            edges.forEach((edge)=>{
                (0, _chunkDD37ZF33Mjs.log).info("Edge", edge);
                const data2 = graph.edge(edge.v, edge.w, edge.name);
                (0, _chunkDD37ZF33Mjs.log).info("Edge data", data2, rootId);
                try {
                    if (edgeInCluster(edge, rootId)) {
                        (0, _chunkDD37ZF33Mjs.log).info("Copying as ", edge.v, edge.w, data2, edge.name);
                        newGraph.setEdge(edge.v, edge.w, data2, edge.name);
                        (0, _chunkDD37ZF33Mjs.log).info("newGraph edges ", newGraph.edges(), newGraph.edge(newGraph.edges()[0]));
                    } else (0, _chunkDD37ZF33Mjs.log).info("Skipping copy of edge ", edge.v, "-->", edge.w, " rootId: ", rootId, " clusterId:", clusterId);
                } catch (e) {
                    (0, _chunkDD37ZF33Mjs.log).error(e);
                }
            });
        }
        (0, _chunkDD37ZF33Mjs.log).debug("Removing node", node);
        graph.removeNode(node);
    });
}, "copy");
var extractDescendants = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((id, graph)=>{
    const children = graph.children(id);
    let res = [
        ...children
    ];
    for (const child of children){
        parents[child] = id;
        res = [
            ...res,
            ...extractDescendants(child, graph)
        ];
    }
    return res;
}, "extractDescendants");
var findNonClusterChild = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((id, graph)=>{
    (0, _chunkDD37ZF33Mjs.log).trace("Searching", id);
    const children = graph.children(id);
    (0, _chunkDD37ZF33Mjs.log).trace("Searching children of id ", id, children);
    if (children.length < 1) {
        (0, _chunkDD37ZF33Mjs.log).trace("This is a valid node", id);
        return id;
    }
    for (const child of children){
        const _id = findNonClusterChild(child, graph);
        if (_id) {
            (0, _chunkDD37ZF33Mjs.log).trace("Found replacement for", id, " => ", _id);
            return _id;
        }
    }
}, "findNonClusterChild");
var getAnchorId = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((id)=>{
    if (!clusterDb[id]) return id;
    if (!clusterDb[id].externalConnections) return id;
    if (clusterDb[id]) return clusterDb[id].id;
    return id;
}, "getAnchorId");
var adjustClustersAndEdges = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((graph, depth)=>{
    if (!graph || depth > 10) {
        (0, _chunkDD37ZF33Mjs.log).debug("Opting out, no graph ");
        return;
    } else (0, _chunkDD37ZF33Mjs.log).debug("Opting in, graph ");
    graph.nodes().forEach(function(id) {
        const children = graph.children(id);
        if (children.length > 0) {
            (0, _chunkDD37ZF33Mjs.log).warn("Cluster identified", id, " Replacement id in edges: ", findNonClusterChild(id, graph));
            descendants[id] = extractDescendants(id, graph);
            clusterDb[id] = {
                id: findNonClusterChild(id, graph),
                clusterData: graph.node(id)
            };
        }
    });
    graph.nodes().forEach(function(id) {
        const children = graph.children(id);
        const edges = graph.edges();
        if (children.length > 0) {
            (0, _chunkDD37ZF33Mjs.log).debug("Cluster identified", id, descendants);
            edges.forEach((edge)=>{
                if (edge.v !== id && edge.w !== id) {
                    const d1 = isDescendant(edge.v, id);
                    const d2 = isDescendant(edge.w, id);
                    if (d1 ^ d2) {
                        (0, _chunkDD37ZF33Mjs.log).warn("Edge: ", edge, " leaves cluster ", id);
                        (0, _chunkDD37ZF33Mjs.log).warn("Descendants of XXX ", id, ": ", descendants[id]);
                        clusterDb[id].externalConnections = true;
                    }
                }
            });
        } else (0, _chunkDD37ZF33Mjs.log).debug("Not a cluster ", id, descendants);
    });
    for (let id of Object.keys(clusterDb)){
        const nonClusterChild = clusterDb[id].id;
        const parent = graph.parent(nonClusterChild);
        if (parent !== id && clusterDb[parent] && !clusterDb[parent].externalConnections) clusterDb[id].id = parent;
    }
    graph.edges().forEach(function(e) {
        const edge = graph.edge(e);
        (0, _chunkDD37ZF33Mjs.log).warn("Edge " + e.v + " -> " + e.w + ": " + JSON.stringify(e));
        (0, _chunkDD37ZF33Mjs.log).warn("Edge " + e.v + " -> " + e.w + ": " + JSON.stringify(graph.edge(e)));
        let v = e.v;
        let w = e.w;
        (0, _chunkDD37ZF33Mjs.log).warn("Fix XXX", clusterDb, "ids:", e.v, e.w, "Translating: ", clusterDb[e.v], " --- ", clusterDb[e.w]);
        if (clusterDb[e.v] && clusterDb[e.w] && clusterDb[e.v] === clusterDb[e.w]) {
            (0, _chunkDD37ZF33Mjs.log).warn("Fixing and trixing link to self - removing XXX", e.v, e.w, e.name);
            (0, _chunkDD37ZF33Mjs.log).warn("Fixing and trixing - removing XXX", e.v, e.w, e.name);
            v = getAnchorId(e.v);
            w = getAnchorId(e.w);
            graph.removeEdge(e.v, e.w, e.name);
            const specialId = e.w + "---" + e.v;
            graph.setNode(specialId, {
                domId: specialId,
                id: specialId,
                labelStyle: "",
                labelText: edge.label,
                padding: 0,
                shape: "labelRect",
                style: ""
            });
            const edge1 = structuredClone(edge);
            const edge2 = structuredClone(edge);
            edge1.label = "";
            edge1.arrowTypeEnd = "none";
            edge2.label = "";
            edge1.fromCluster = e.v;
            edge2.toCluster = e.v;
            graph.setEdge(v, specialId, edge1, e.name + "-cyclic-special");
            graph.setEdge(specialId, w, edge2, e.name + "-cyclic-special");
        } else if (clusterDb[e.v] || clusterDb[e.w]) {
            (0, _chunkDD37ZF33Mjs.log).warn("Fixing and trixing - removing XXX", e.v, e.w, e.name);
            v = getAnchorId(e.v);
            w = getAnchorId(e.w);
            graph.removeEdge(e.v, e.w, e.name);
            if (v !== e.v) {
                const parent = graph.parent(v);
                clusterDb[parent].externalConnections = true;
                edge.fromCluster = e.v;
            }
            if (w !== e.w) {
                const parent = graph.parent(w);
                clusterDb[parent].externalConnections = true;
                edge.toCluster = e.w;
            }
            (0, _chunkDD37ZF33Mjs.log).warn("Fix Replacing with XXX", v, w, e.name);
            graph.setEdge(v, w, edge, e.name);
        }
    });
    (0, _chunkDD37ZF33Mjs.log).warn("Adjusted Graph", (0, _chunkB7GIP3BCMjs.write)(graph));
    extractor(graph, 0);
    (0, _chunkDD37ZF33Mjs.log).trace(clusterDb);
}, "adjustClustersAndEdges");
var extractor = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((graph, depth)=>{
    (0, _chunkDD37ZF33Mjs.log).warn("extractor - ", depth, (0, _chunkB7GIP3BCMjs.write)(graph), graph.children("D"));
    if (depth > 10) {
        (0, _chunkDD37ZF33Mjs.log).error("Bailing out");
        return;
    }
    let nodes = graph.nodes();
    let hasChildren = false;
    for (const node of nodes){
        const children = graph.children(node);
        hasChildren = hasChildren || children.length > 0;
    }
    if (!hasChildren) {
        (0, _chunkDD37ZF33Mjs.log).debug("Done, no node has children", graph.nodes());
        return;
    }
    (0, _chunkDD37ZF33Mjs.log).debug("Nodes = ", nodes, depth);
    for (const node of nodes){
        (0, _chunkDD37ZF33Mjs.log).debug("Extracting node", node, clusterDb, clusterDb[node] && !clusterDb[node].externalConnections, !graph.parent(node), graph.node(node), graph.children("D"), " Depth ", depth);
        if (!clusterDb[node]) (0, _chunkDD37ZF33Mjs.log).debug("Not a cluster", node, depth);
        else if (!clusterDb[node].externalConnections && // !graph.parent(node) &&
        graph.children(node) && graph.children(node).length > 0) {
            (0, _chunkDD37ZF33Mjs.log).warn("Cluster without external connections, without a parent and with children", node, depth);
            const graphSettings = graph.graph();
            let dir = graphSettings.rankdir === "TB" ? "LR" : "TB";
            if (clusterDb[node]?.clusterData?.dir) {
                dir = clusterDb[node].clusterData.dir;
                (0, _chunkDD37ZF33Mjs.log).warn("Fixing dir", clusterDb[node].clusterData.dir, dir);
            }
            const clusterGraph = new (0, _chunkULVYQCHCMjs.Graph)({
                multigraph: true,
                compound: true
            }).setGraph({
                rankdir: dir,
                // Todo: set proper spacing
                nodesep: 50,
                ranksep: 50,
                marginx: 8,
                marginy: 8
            }).setDefaultEdgeLabel(function() {
                return {};
            });
            (0, _chunkDD37ZF33Mjs.log).warn("Old graph before copy", (0, _chunkB7GIP3BCMjs.write)(graph));
            copy(node, graph, clusterGraph, node);
            graph.setNode(node, {
                clusterNode: true,
                id: node,
                clusterData: clusterDb[node].clusterData,
                labelText: clusterDb[node].labelText,
                graph: clusterGraph
            });
            (0, _chunkDD37ZF33Mjs.log).warn("New graph after copy node: (", node, ")", (0, _chunkB7GIP3BCMjs.write)(clusterGraph));
            (0, _chunkDD37ZF33Mjs.log).debug("Old graph after copy", (0, _chunkB7GIP3BCMjs.write)(graph));
        } else {
            (0, _chunkDD37ZF33Mjs.log).warn("Cluster ** ", node, " **not meeting the criteria !externalConnections:", !clusterDb[node].externalConnections, " no parent: ", !graph.parent(node), " children ", graph.children(node) && graph.children(node).length > 0, graph.children("D"), depth);
            (0, _chunkDD37ZF33Mjs.log).debug(clusterDb);
        }
    }
    nodes = graph.nodes();
    (0, _chunkDD37ZF33Mjs.log).warn("New list of nodes", nodes);
    for (const node of nodes){
        const data = graph.node(node);
        (0, _chunkDD37ZF33Mjs.log).warn(" Now next level", node, data);
        if (data.clusterNode) extractor(data.graph, depth + 1);
    }
}, "extractor");
var sorter = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((graph, nodes)=>{
    if (nodes.length === 0) return [];
    let result = Object.assign(nodes);
    nodes.forEach((node)=>{
        const children = graph.children(node);
        const sorted = sorter(graph, children);
        result = [
            ...result,
            ...sorted
        ];
    });
    return result;
}, "sorter");
var sortNodesByHierarchy = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((graph)=>sorter(graph, graph.children()), "sortNodesByHierarchy");
// src/dagre-wrapper/clusters.js
var rect = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((parent, node)=>{
    (0, _chunkDD37ZF33Mjs.log).info("Creating subgraph rect for ", node.id, node);
    const siteConfig = (0, _chunkDD37ZF33Mjs.getConfig2)();
    const shapeSvg = parent.insert("g").attr("class", "cluster" + (node.class ? " " + node.class : "")).attr("id", node.id);
    const rect2 = shapeSvg.insert("rect", ":first-child");
    const useHtmlLabels = (0, _chunkDD37ZF33Mjs.evaluate)(siteConfig.flowchart.htmlLabels);
    const label = shapeSvg.insert("g").attr("class", "cluster-label");
    const text = node.labelType === "markdown" ? (0, _chunkYP6PVJQ3Mjs.createText)(label, node.labelText, {
        style: node.labelStyle,
        useHtmlLabels
    }, siteConfig) : label.node().appendChild((0, _chunkHKQCUR3CMjs.createLabel_default)(node.labelText, node.labelStyle, void 0, true));
    let bbox = text.getBBox();
    if ((0, _chunkDD37ZF33Mjs.evaluate)(siteConfig.flowchart.htmlLabels)) {
        const div = text.children[0];
        const dv = (0, _chunkDD37ZF33Mjs.select_default)(text);
        bbox = div.getBoundingClientRect();
        dv.attr("width", bbox.width);
        dv.attr("height", bbox.height);
    }
    const padding = 0 * node.padding;
    const halfPadding = padding / 2;
    const width = node.width <= bbox.width + padding ? bbox.width + padding : node.width;
    if (node.width <= bbox.width + padding) node.diff = (bbox.width - node.width) / 2 - node.padding / 2;
    else node.diff = -node.padding / 2;
    (0, _chunkDD37ZF33Mjs.log).trace("Data ", node, JSON.stringify(node));
    rect2.attr("style", node.style).attr("rx", node.rx).attr("ry", node.ry).attr("x", node.x - width / 2).attr("y", node.y - node.height / 2 - halfPadding).attr("width", width).attr("height", node.height + padding);
    const { subGraphTitleTopMargin } = (0, _chunkKW7S66XIMjs.getSubGraphTitleMargins)(siteConfig);
    if (useHtmlLabels) label.attr("transform", // This puts the label on top of the box instead of inside it
    `translate(${node.x - bbox.width / 2}, ${node.y - node.height / 2 + subGraphTitleTopMargin})`);
    else label.attr("transform", // This puts the label on top of the box instead of inside it
    `translate(${node.x}, ${node.y - node.height / 2 + subGraphTitleTopMargin})`);
    const rectBox = rect2.node().getBBox();
    node.width = rectBox.width;
    node.height = rectBox.height;
    node.intersect = function(point) {
        return (0, _chunkHKQCUR3CMjs.intersect_rect_default)(node, point);
    };
    return shapeSvg;
}, "rect");
var noteGroup = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((parent, node)=>{
    const shapeSvg = parent.insert("g").attr("class", "note-cluster").attr("id", node.id);
    const rect2 = shapeSvg.insert("rect", ":first-child");
    const padding = 0 * node.padding;
    const halfPadding = padding / 2;
    rect2.attr("rx", node.rx).attr("ry", node.ry).attr("x", node.x - node.width / 2 - halfPadding).attr("y", node.y - node.height / 2 - halfPadding).attr("width", node.width + padding).attr("height", node.height + padding).attr("fill", "none");
    const rectBox = rect2.node().getBBox();
    node.width = rectBox.width;
    node.height = rectBox.height;
    node.intersect = function(point) {
        return (0, _chunkHKQCUR3CMjs.intersect_rect_default)(node, point);
    };
    return shapeSvg;
}, "noteGroup");
var roundedWithTitle = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((parent, node)=>{
    const siteConfig = (0, _chunkDD37ZF33Mjs.getConfig2)();
    const shapeSvg = parent.insert("g").attr("class", node.classes).attr("id", node.id);
    const rect2 = shapeSvg.insert("rect", ":first-child");
    const label = shapeSvg.insert("g").attr("class", "cluster-label");
    const innerRect = shapeSvg.append("rect");
    const text = label.node().appendChild((0, _chunkHKQCUR3CMjs.createLabel_default)(node.labelText, node.labelStyle, void 0, true));
    let bbox = text.getBBox();
    if ((0, _chunkDD37ZF33Mjs.evaluate)(siteConfig.flowchart.htmlLabels)) {
        const div = text.children[0];
        const dv = (0, _chunkDD37ZF33Mjs.select_default)(text);
        bbox = div.getBoundingClientRect();
        dv.attr("width", bbox.width);
        dv.attr("height", bbox.height);
    }
    bbox = text.getBBox();
    const padding = 0 * node.padding;
    const halfPadding = padding / 2;
    const width = node.width <= bbox.width + node.padding ? bbox.width + node.padding : node.width;
    if (node.width <= bbox.width + node.padding) node.diff = (bbox.width + node.padding * 0 - node.width) / 2;
    else node.diff = -node.padding / 2;
    rect2.attr("class", "outer").attr("x", node.x - width / 2 - halfPadding).attr("y", node.y - node.height / 2 - halfPadding).attr("width", width + padding).attr("height", node.height + padding);
    innerRect.attr("class", "inner").attr("x", node.x - width / 2 - halfPadding).attr("y", node.y - node.height / 2 - halfPadding + bbox.height - 1).attr("width", width + padding).attr("height", node.height + padding - bbox.height - 3);
    const { subGraphTitleTopMargin } = (0, _chunkKW7S66XIMjs.getSubGraphTitleMargins)(siteConfig);
    label.attr("transform", `translate(${node.x - bbox.width / 2}, ${node.y - node.height / 2 - node.padding / 3 + ((0, _chunkDD37ZF33Mjs.evaluate)(siteConfig.flowchart.htmlLabels) ? 5 : 3) + subGraphTitleTopMargin})`);
    const rectBox = rect2.node().getBBox();
    node.height = rectBox.height;
    node.intersect = function(point) {
        return (0, _chunkHKQCUR3CMjs.intersect_rect_default)(node, point);
    };
    return shapeSvg;
}, "roundedWithTitle");
var divider = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((parent, node)=>{
    const shapeSvg = parent.insert("g").attr("class", node.classes).attr("id", node.id);
    const rect2 = shapeSvg.insert("rect", ":first-child");
    const padding = 0 * node.padding;
    const halfPadding = padding / 2;
    rect2.attr("class", "divider").attr("x", node.x - node.width / 2 - halfPadding).attr("y", node.y - node.height / 2).attr("width", node.width + padding).attr("height", node.height + padding);
    const rectBox = rect2.node().getBBox();
    node.width = rectBox.width;
    node.height = rectBox.height;
    node.diff = -node.padding / 2;
    node.intersect = function(point) {
        return (0, _chunkHKQCUR3CMjs.intersect_rect_default)(node, point);
    };
    return shapeSvg;
}, "divider");
var shapes = {
    rect,
    roundedWithTitle,
    noteGroup,
    divider
};
var clusterElems = {};
var insertCluster = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((elem, node)=>{
    (0, _chunkDD37ZF33Mjs.log).trace("Inserting cluster");
    const shape = node.shape || "rect";
    clusterElems[node.id] = shapes[shape](elem, node);
}, "insertCluster");
var clear4 = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(()=>{
    clusterElems = {};
}, "clear");
// src/dagre-wrapper/index.js
var recursiveRender = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(async (_elem, graph, diagramType, id, parentCluster, siteConfig)=>{
    (0, _chunkDD37ZF33Mjs.log).info("Graph in recursive render: XXX", (0, _chunkB7GIP3BCMjs.write)(graph), parentCluster);
    const dir = graph.graph().rankdir;
    (0, _chunkDD37ZF33Mjs.log).trace("Dir in recursive render - dir:", dir);
    const elem = _elem.insert("g").attr("class", "root");
    if (!graph.nodes()) (0, _chunkDD37ZF33Mjs.log).info("No nodes found for", graph);
    else (0, _chunkDD37ZF33Mjs.log).info("Recursive render XXX", graph.nodes());
    if (graph.edges().length > 0) (0, _chunkDD37ZF33Mjs.log).trace("Recursive edges", graph.edge(graph.edges()[0]));
    const clusters = elem.insert("g").attr("class", "clusters");
    const edgePaths = elem.insert("g").attr("class", "edgePaths");
    const edgeLabels = elem.insert("g").attr("class", "edgeLabels");
    const nodes = elem.insert("g").attr("class", "nodes");
    await Promise.all(graph.nodes().map(async function(v) {
        const node = graph.node(v);
        if (parentCluster !== void 0) {
            const data = JSON.parse(JSON.stringify(parentCluster.clusterData));
            (0, _chunkDD37ZF33Mjs.log).info("Setting data for cluster XXX (", v, ") ", data, parentCluster);
            graph.setNode(parentCluster.id, data);
            if (!graph.parent(v)) {
                (0, _chunkDD37ZF33Mjs.log).trace("Setting parent", v, parentCluster.id);
                graph.setParent(v, parentCluster.id, data);
            }
        }
        (0, _chunkDD37ZF33Mjs.log).info("(Insert) Node XXX" + v + ": " + JSON.stringify(graph.node(v)));
        if (node?.clusterNode) {
            (0, _chunkDD37ZF33Mjs.log).info("Cluster identified", v, node.width, graph.node(v));
            const { ranksep, nodesep } = graph.graph();
            node.graph.setGraph({
                ...node.graph.graph(),
                ranksep,
                nodesep
            });
            const o = await recursiveRender(nodes, node.graph, diagramType, id, graph.node(v), siteConfig);
            const newEl = o.elem;
            (0, _chunkHKQCUR3CMjs.updateNodeBounds)(node, newEl);
            node.diff = o.diff || 0;
            (0, _chunkDD37ZF33Mjs.log).info("Node bounds (abc123)", v, node, node.width, node.x, node.y);
            (0, _chunkHKQCUR3CMjs.setNodeElem)(newEl, node);
            (0, _chunkDD37ZF33Mjs.log).warn("Recursive render complete ", newEl, node);
        } else if (graph.children(v).length > 0) {
            (0, _chunkDD37ZF33Mjs.log).info("Cluster - the non recursive path XXX", v, node.id, node, graph);
            (0, _chunkDD37ZF33Mjs.log).info(findNonClusterChild(node.id, graph));
            clusterDb[node.id] = {
                id: findNonClusterChild(node.id, graph),
                node
            };
        } else {
            (0, _chunkDD37ZF33Mjs.log).info("Node - the non recursive path", v, node.id, node);
            await (0, _chunkHKQCUR3CMjs.insertNode)(nodes, graph.node(v), dir);
        }
    }));
    graph.edges().forEach(async function(e) {
        const edge = graph.edge(e.v, e.w, e.name);
        (0, _chunkDD37ZF33Mjs.log).info("Edge " + e.v + " -> " + e.w + ": " + JSON.stringify(e));
        (0, _chunkDD37ZF33Mjs.log).info("Edge " + e.v + " -> " + e.w + ": ", e, " ", JSON.stringify(graph.edge(e)));
        (0, _chunkDD37ZF33Mjs.log).info("Fix", clusterDb, "ids:", e.v, e.w, "Translating: ", clusterDb[e.v], clusterDb[e.w]);
        await (0, _chunkBAJGW65CMjs.insertEdgeLabel)(edgeLabels, edge);
    });
    graph.edges().forEach(function(e) {
        (0, _chunkDD37ZF33Mjs.log).info("Edge " + e.v + " -> " + e.w + ": " + JSON.stringify(e));
    });
    (0, _chunkDD37ZF33Mjs.log).info("Graph before layout:", JSON.stringify((0, _chunkB7GIP3BCMjs.write)(graph)));
    (0, _chunkDD37ZF33Mjs.log).info("#############################################");
    (0, _chunkDD37ZF33Mjs.log).info("###                Layout                 ###");
    (0, _chunkDD37ZF33Mjs.log).info("#############################################");
    (0, _chunkDD37ZF33Mjs.log).info(graph);
    (0, _chunkCN5XARC6Mjs.layout)(graph);
    (0, _chunkDD37ZF33Mjs.log).info("Graph after layout:", JSON.stringify((0, _chunkB7GIP3BCMjs.write)(graph)));
    let diff = 0;
    const { subGraphTitleTotalMargin } = (0, _chunkKW7S66XIMjs.getSubGraphTitleMargins)(siteConfig);
    sortNodesByHierarchy(graph).forEach(function(v) {
        const node = graph.node(v);
        (0, _chunkDD37ZF33Mjs.log).info("Position " + v + ": " + JSON.stringify(graph.node(v)));
        (0, _chunkDD37ZF33Mjs.log).info("Position " + v + ": (" + node.x, "," + node.y, ") width: ", node.width, " height: ", node.height);
        if (node?.clusterNode) {
            node.y += subGraphTitleTotalMargin;
            (0, _chunkHKQCUR3CMjs.positionNode)(node);
        } else if (graph.children(v).length > 0) {
            node.height += subGraphTitleTotalMargin;
            insertCluster(clusters, node);
            clusterDb[node.id].node = node;
        } else {
            node.y += subGraphTitleTotalMargin / 2;
            (0, _chunkHKQCUR3CMjs.positionNode)(node);
        }
    });
    graph.edges().forEach(function(e) {
        const edge = graph.edge(e);
        (0, _chunkDD37ZF33Mjs.log).info("Edge " + e.v + " -> " + e.w + ": " + JSON.stringify(edge), edge);
        edge.points.forEach((point)=>point.y += subGraphTitleTotalMargin / 2);
        const paths = (0, _chunkBAJGW65CMjs.insertEdge)(edgePaths, e, edge, clusterDb, diagramType, graph, id);
        (0, _chunkBAJGW65CMjs.positionEdgeLabel)(edge, paths);
    });
    graph.nodes().forEach(function(v) {
        const n = graph.node(v);
        (0, _chunkDD37ZF33Mjs.log).info(v, n.type, n.diff);
        if (n.type === "group") diff = n.diff;
    });
    return {
        elem,
        diff
    };
}, "recursiveRender");
var render = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(async (elem, graph, markers, diagramType, id)=>{
    (0, _chunkBAJGW65CMjs.markers_default)(elem, markers, diagramType, id);
    (0, _chunkHKQCUR3CMjs.clear)();
    (0, _chunkBAJGW65CMjs.clear)();
    clear4();
    clear3();
    (0, _chunkDD37ZF33Mjs.log).warn("Graph at first:", JSON.stringify((0, _chunkB7GIP3BCMjs.write)(graph)));
    adjustClustersAndEdges(graph);
    (0, _chunkDD37ZF33Mjs.log).warn("Graph after:", JSON.stringify((0, _chunkB7GIP3BCMjs.write)(graph)));
    const siteConfig = (0, _chunkDD37ZF33Mjs.getConfig2)();
    await recursiveRender(elem, graph, diagramType, id, void 0, siteConfig);
}, "render");
// src/diagrams/class/classRenderer-v2.ts
var sanitizeText = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((txt)=>(0, _chunkDD37ZF33Mjs.common_default).sanitizeText(txt, (0, _chunkDD37ZF33Mjs.getConfig2)()), "sanitizeText");
var conf = {
    dividerMargin: 10,
    padding: 5,
    textHeight: 10,
    curve: void 0
};
var addNamespaces = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(namespaces, g, _id, diagObj) {
    (0, _chunkDD37ZF33Mjs.log).info("keys:", [
        ...namespaces.keys()
    ]);
    (0, _chunkDD37ZF33Mjs.log).info(namespaces);
    namespaces.forEach(function(vertex) {
        const shape = "rect";
        const node = {
            shape,
            id: vertex.id,
            domId: vertex.domId,
            labelText: sanitizeText(vertex.id),
            labelStyle: "",
            style: "fill: none; stroke: black",
            // TODO V10: Flowchart ? Keeping flowchart for backwards compatibility. Remove in next major release
            padding: (0, _chunkDD37ZF33Mjs.getConfig2)().flowchart?.padding ?? (0, _chunkDD37ZF33Mjs.getConfig2)().class?.padding
        };
        g.setNode(vertex.id, node);
        addClasses(vertex.classes, g, _id, diagObj, vertex.id);
        (0, _chunkDD37ZF33Mjs.log).info("setNode", node);
    });
}, "addNamespaces");
var addClasses = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(classes, g, _id, diagObj, parent) {
    (0, _chunkDD37ZF33Mjs.log).info("keys:", [
        ...classes.keys()
    ]);
    (0, _chunkDD37ZF33Mjs.log).info(classes);
    [
        ...classes.values()
    ].filter((vertex)=>vertex.parent === parent).forEach(function(vertex) {
        const cssClassStr = vertex.cssClasses.join(" ");
        const styles = (0, _chunkI7ZFS43CMjs.getStylesFromArray)(vertex.styles);
        const vertexText = vertex.label ?? vertex.id;
        const radius = 0;
        const shape = "class_box";
        const node = {
            labelStyle: styles.labelStyle,
            shape,
            labelText: sanitizeText(vertexText),
            classData: vertex,
            rx: radius,
            ry: radius,
            class: cssClassStr,
            style: styles.style,
            id: vertex.id,
            domId: vertex.domId,
            tooltip: diagObj.db.getTooltip(vertex.id, parent) || "",
            haveCallback: vertex.haveCallback,
            link: vertex.link,
            width: vertex.type === "group" ? 500 : void 0,
            type: vertex.type,
            // TODO V10: Flowchart ? Keeping flowchart for backwards compatibility. Remove in next major release
            padding: (0, _chunkDD37ZF33Mjs.getConfig2)().flowchart?.padding ?? (0, _chunkDD37ZF33Mjs.getConfig2)().class?.padding
        };
        g.setNode(vertex.id, node);
        if (parent) g.setParent(vertex.id, parent);
        (0, _chunkDD37ZF33Mjs.log).info("setNode", node);
    });
}, "addClasses");
var addNotes = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(notes, g, startEdgeId, classes) {
    (0, _chunkDD37ZF33Mjs.log).info(notes);
    notes.forEach(function(note, i) {
        const vertex = note;
        const cssNoteStr = "";
        const styles = {
            labelStyle: "",
            style: ""
        };
        const vertexText = vertex.text;
        const radius = 0;
        const shape = "note";
        const node = {
            labelStyle: styles.labelStyle,
            shape,
            labelText: sanitizeText(vertexText),
            noteData: vertex,
            rx: radius,
            ry: radius,
            class: cssNoteStr,
            style: styles.style,
            id: vertex.id,
            domId: vertex.id,
            tooltip: "",
            type: "note",
            // TODO V10: Flowchart ? Keeping flowchart for backwards compatibility. Remove in next major release
            padding: (0, _chunkDD37ZF33Mjs.getConfig2)().flowchart?.padding ?? (0, _chunkDD37ZF33Mjs.getConfig2)().class?.padding
        };
        g.setNode(vertex.id, node);
        (0, _chunkDD37ZF33Mjs.log).info("setNode", node);
        if (!vertex.class || !classes.has(vertex.class)) return;
        const edgeId = startEdgeId + i;
        const edgeData = {
            id: `edgeNote${edgeId}`,
            //Set relationship style and line type
            classes: "relation",
            pattern: "dotted",
            // Set link type for rendering
            arrowhead: "none",
            //Set edge extra labels
            startLabelRight: "",
            endLabelLeft: "",
            //Set relation arrow types
            arrowTypeStart: "none",
            arrowTypeEnd: "none",
            style: "fill:none",
            labelStyle: "",
            curve: (0, _chunkI7ZFS43CMjs.interpolateToCurve)(conf.curve, (0, _chunkDD37ZF33Mjs.linear_default))
        };
        g.setEdge(vertex.id, vertex.class, edgeData, edgeId);
    });
}, "addNotes");
var addRelations = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(relations, g) {
    const conf2 = (0, _chunkDD37ZF33Mjs.getConfig2)().flowchart;
    let cnt = 0;
    relations.forEach(function(edge) {
        cnt++;
        const edgeData = {
            //Set relationship style and line type
            classes: "relation",
            pattern: edge.relation.lineType == 1 ? "dashed" : "solid",
            id: (0, _chunkI7ZFS43CMjs.getEdgeId)(edge.id1, edge.id2, {
                prefix: "id",
                counter: cnt
            }),
            // Set link type for rendering
            arrowhead: edge.type === "arrow_open" ? "none" : "normal",
            //Set edge extra labels
            startLabelRight: edge.relationTitle1 === "none" ? "" : edge.relationTitle1,
            endLabelLeft: edge.relationTitle2 === "none" ? "" : edge.relationTitle2,
            //Set relation arrow types
            arrowTypeStart: getArrowMarker(edge.relation.type1),
            arrowTypeEnd: getArrowMarker(edge.relation.type2),
            style: "fill:none",
            labelStyle: "",
            curve: (0, _chunkI7ZFS43CMjs.interpolateToCurve)(conf2?.curve, (0, _chunkDD37ZF33Mjs.linear_default))
        };
        (0, _chunkDD37ZF33Mjs.log).info(edgeData, edge);
        if (edge.style !== void 0) {
            const styles = (0, _chunkI7ZFS43CMjs.getStylesFromArray)(edge.style);
            edgeData.style = styles.style;
            edgeData.labelStyle = styles.labelStyle;
        }
        edge.text = edge.title;
        if (edge.text === void 0) {
            if (edge.style !== void 0) edgeData.arrowheadStyle = "fill: #333";
        } else {
            edgeData.arrowheadStyle = "fill: #333";
            edgeData.labelpos = "c";
            if ((0, _chunkDD37ZF33Mjs.getConfig2)().flowchart?.htmlLabels ?? (0, _chunkDD37ZF33Mjs.getConfig2)().htmlLabels) {
                edgeData.labelType = "html";
                edgeData.label = '<span class="edgeLabel">' + edge.text + "</span>";
            } else {
                edgeData.labelType = "text";
                edgeData.label = edge.text.replace((0, _chunkDD37ZF33Mjs.common_default).lineBreakRegex, "\n");
                if (edge.style === void 0) edgeData.style = edgeData.style || "stroke: #333; stroke-width: 1.5px;fill:none";
                edgeData.labelStyle = edgeData.labelStyle.replace("color:", "fill:");
            }
        }
        g.setEdge(edge.id1, edge.id2, edgeData, cnt);
    });
}, "addRelations");
var setConf = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(cnf) {
    conf = {
        ...conf,
        ...cnf
    };
}, "setConf");
var draw = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(async function(text, id, _version, diagObj) {
    (0, _chunkDD37ZF33Mjs.log).info("Drawing class - ", id);
    const conf2 = (0, _chunkDD37ZF33Mjs.getConfig2)().flowchart ?? (0, _chunkDD37ZF33Mjs.getConfig2)().class;
    const securityLevel = (0, _chunkDD37ZF33Mjs.getConfig2)().securityLevel;
    (0, _chunkDD37ZF33Mjs.log).info("config:", conf2);
    const nodeSpacing = conf2?.nodeSpacing ?? 50;
    const rankSpacing = conf2?.rankSpacing ?? 50;
    const g = new (0, _chunkULVYQCHCMjs.Graph)({
        multigraph: true,
        compound: true
    }).setGraph({
        rankdir: diagObj.db.getDirection(),
        nodesep: nodeSpacing,
        ranksep: rankSpacing,
        marginx: 8,
        marginy: 8
    }).setDefaultEdgeLabel(function() {
        return {};
    });
    const namespaces = diagObj.db.getNamespaces();
    const classes = diagObj.db.getClasses();
    const relations = diagObj.db.getRelations();
    const notes = diagObj.db.getNotes();
    (0, _chunkDD37ZF33Mjs.log).info(relations);
    addNamespaces(namespaces, g, id, diagObj);
    addClasses(classes, g, id, diagObj);
    addRelations(relations, g);
    addNotes(notes, g, relations.length + 1, classes);
    let sandboxElement;
    if (securityLevel === "sandbox") sandboxElement = (0, _chunkDD37ZF33Mjs.select_default)("#i" + id);
    const root = securityLevel === "sandbox" ? (0, _chunkDD37ZF33Mjs.select_default)(sandboxElement.nodes()[0].contentDocument.body) : (0, _chunkDD37ZF33Mjs.select_default)("body");
    const svg = root.select(`[id="${id}"]`);
    const element = root.select("#" + id + " g");
    await render(element, g, [
        "aggregation",
        "extension",
        "composition",
        "dependency",
        "lollipop"
    ], "classDiagram", id);
    (0, _chunkI7ZFS43CMjs.utils_default).insertTitle(svg, "classTitleText", conf2?.titleTopMargin ?? 5, diagObj.db.getDiagramTitle());
    (0, _chunkDD37ZF33Mjs.setupGraphViewbox)(g, svg, conf2?.diagramPadding, conf2?.useMaxWidth);
    if (!conf2?.htmlLabels) {
        const doc = securityLevel === "sandbox" ? sandboxElement.nodes()[0].contentDocument : document;
        const labels = doc.querySelectorAll('[id="' + id + '"] .edgeLabel .label');
        for (const label of labels){
            const dim = label.getBBox();
            const rect2 = doc.createElementNS("http://www.w3.org/2000/svg", "rect");
            rect2.setAttribute("rx", 0);
            rect2.setAttribute("ry", 0);
            rect2.setAttribute("width", dim.width);
            rect2.setAttribute("height", dim.height);
            label.insertBefore(rect2, label.firstChild);
        }
    }
}, "draw");
function getArrowMarker(type) {
    let marker;
    switch(type){
        case 0:
            marker = "aggregation";
            break;
        case 1:
            marker = "extension";
            break;
        case 2:
            marker = "composition";
            break;
        case 3:
            marker = "dependency";
            break;
        case 4:
            marker = "lollipop";
            break;
        default:
            marker = "none";
    }
    return marker;
}
(0, _chunkDLQEHMXDMjs.__name)(getArrowMarker, "getArrowMarker");
var classRenderer_v2_default = {
    setConf,
    draw
};
// src/diagrams/class/classDiagram-v2.ts
var diagram = {
    parser: (0, _chunkIQOVSUJRMjs.classDiagram_default),
    db: (0, _chunkIQOVSUJRMjs.classDb_default),
    renderer: classRenderer_v2_default,
    styles: (0, _chunkIQOVSUJRMjs.styles_default),
    init: /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((cnf)=>{
        if (!cnf.class) cnf.class = {};
        cnf.class.arrowMarkerAbsolute = cnf.arrowMarkerAbsolute;
        (0, _chunkIQOVSUJRMjs.classDb_default).clear();
    }, "init")
};

},{"./chunk-BAJGW65C.mjs":"gf7Uq","./chunk-IQOVSUJR.mjs":"izhVd","./chunk-B7GIP3BC.mjs":"5wvIA","./chunk-HKQCUR3C.mjs":"fcjSH","./chunk-KW7S66XI.mjs":"98JMR","./chunk-YP6PVJQ3.mjs":"21NKC","./chunk-CN5XARC6.mjs":"c7FQv","./chunk-ULVYQCHC.mjs":"h2Yj3","./chunk-I7ZFS43C.mjs":"huUtc","./chunk-GKOISANM.mjs":"5yZtl","./chunk-DD37ZF33.mjs":"f4pI5","./chunk-TZBO7MLI.mjs":"fY3tK","./chunk-GRZAG2UZ.mjs":"d1pnj","./chunk-HD3LK5B5.mjs":"lpo2u","./chunk-DLQEHMXD.mjs":"h5YRf","@parcel/transformer-js/src/esmodule-helpers.js":"1cdyk"}],"5wvIA":[function(require,module,exports,__globalThis) {
var parcelHelpers = require("@parcel/transformer-js/src/esmodule-helpers.js");
parcelHelpers.defineInteropFlag(exports);
parcelHelpers.export(exports, "write", ()=>write);
var _chunkTZBO7MLIMjs = require("./chunk-TZBO7MLI.mjs");
var _chunkDLQEHMXDMjs = require("./chunk-DLQEHMXD.mjs");
// ../../node_modules/.pnpm/dagre-d3-es@7.0.10/node_modules/dagre-d3-es/src/graphlib/json.js
function write(g) {
    var json = {
        options: {
            directed: g.isDirected(),
            multigraph: g.isMultigraph(),
            compound: g.isCompound()
        },
        nodes: writeNodes(g),
        edges: writeEdges(g)
    };
    if (!(0, _chunkTZBO7MLIMjs.isUndefined_default)(g.graph())) json.value = (0, _chunkTZBO7MLIMjs.clone_default)(g.graph());
    return json;
}
(0, _chunkDLQEHMXDMjs.__name)(write, "write");
function writeNodes(g) {
    return (0, _chunkTZBO7MLIMjs.map_default)(g.nodes(), function(v) {
        var nodeValue = g.node(v);
        var parent = g.parent(v);
        var node = {
            v
        };
        if (!(0, _chunkTZBO7MLIMjs.isUndefined_default)(nodeValue)) node.value = nodeValue;
        if (!(0, _chunkTZBO7MLIMjs.isUndefined_default)(parent)) node.parent = parent;
        return node;
    });
}
(0, _chunkDLQEHMXDMjs.__name)(writeNodes, "writeNodes");
function writeEdges(g) {
    return (0, _chunkTZBO7MLIMjs.map_default)(g.edges(), function(e) {
        var edgeValue = g.edge(e);
        var edge = {
            v: e.v,
            w: e.w
        };
        if (!(0, _chunkTZBO7MLIMjs.isUndefined_default)(e.name)) edge.name = e.name;
        if (!(0, _chunkTZBO7MLIMjs.isUndefined_default)(edgeValue)) edge.value = edgeValue;
        return edge;
    });
}
(0, _chunkDLQEHMXDMjs.__name)(writeEdges, "writeEdges");

},{"./chunk-TZBO7MLI.mjs":"fY3tK","./chunk-DLQEHMXD.mjs":"h5YRf","@parcel/transformer-js/src/esmodule-helpers.js":"1cdyk"}],"h2Yj3":[function(require,module,exports,__globalThis) {
var parcelHelpers = require("@parcel/transformer-js/src/esmodule-helpers.js");
parcelHelpers.defineInteropFlag(exports);
parcelHelpers.export(exports, "Graph", ()=>Graph);
var _chunkTZBO7MLIMjs = require("./chunk-TZBO7MLI.mjs");
var _chunkGRZAG2UZMjs = require("./chunk-GRZAG2UZ.mjs");
var _chunkHD3LK5B5Mjs = require("./chunk-HD3LK5B5.mjs");
var _chunkDLQEHMXDMjs = require("./chunk-DLQEHMXD.mjs");
// ../../node_modules/.pnpm/dagre-d3-es@7.0.10/node_modules/dagre-d3-es/src/graphlib/graph.js
var DEFAULT_EDGE_NAME = "\0";
var GRAPH_NODE = "\0";
var EDGE_KEY_DELIM = "";
var Graph = class {
    static #_ = (0, _chunkDLQEHMXDMjs.__name)(this, "Graph");
    constructor(opts = {}){
        this._isDirected = (0, _chunkTZBO7MLIMjs.has_default)(opts, "directed") ? opts.directed : true;
        this._isMultigraph = (0, _chunkTZBO7MLIMjs.has_default)(opts, "multigraph") ? opts.multigraph : false;
        this._isCompound = (0, _chunkTZBO7MLIMjs.has_default)(opts, "compound") ? opts.compound : false;
        this._label = void 0;
        this._defaultNodeLabelFn = (0, _chunkHD3LK5B5Mjs.constant_default)(void 0);
        this._defaultEdgeLabelFn = (0, _chunkHD3LK5B5Mjs.constant_default)(void 0);
        this._nodes = {};
        if (this._isCompound) {
            this._parent = {};
            this._children = {};
            this._children[GRAPH_NODE] = {};
        }
        this._in = {};
        this._preds = {};
        this._out = {};
        this._sucs = {};
        this._edgeObjs = {};
        this._edgeLabels = {};
    }
    /* === Graph functions ========= */ isDirected() {
        return this._isDirected;
    }
    isMultigraph() {
        return this._isMultigraph;
    }
    isCompound() {
        return this._isCompound;
    }
    setGraph(label) {
        this._label = label;
        return this;
    }
    graph() {
        return this._label;
    }
    /* === Node functions ========== */ setDefaultNodeLabel(newDefault) {
        if (!(0, _chunkHD3LK5B5Mjs.isFunction_default)(newDefault)) newDefault = (0, _chunkHD3LK5B5Mjs.constant_default)(newDefault);
        this._defaultNodeLabelFn = newDefault;
        return this;
    }
    nodeCount() {
        return this._nodeCount;
    }
    nodes() {
        return (0, _chunkTZBO7MLIMjs.keys_default)(this._nodes);
    }
    sources() {
        var self = this;
        return (0, _chunkTZBO7MLIMjs.filter_default)(this.nodes(), function(v) {
            return (0, _chunkGRZAG2UZMjs.isEmpty_default)(self._in[v]);
        });
    }
    sinks() {
        var self = this;
        return (0, _chunkTZBO7MLIMjs.filter_default)(this.nodes(), function(v) {
            return (0, _chunkGRZAG2UZMjs.isEmpty_default)(self._out[v]);
        });
    }
    setNodes(vs, value) {
        var args = arguments;
        var self = this;
        (0, _chunkTZBO7MLIMjs.forEach_default)(vs, function(v) {
            if (args.length > 1) self.setNode(v, value);
            else self.setNode(v);
        });
        return this;
    }
    setNode(v, value) {
        if ((0, _chunkTZBO7MLIMjs.has_default)(this._nodes, v)) {
            if (arguments.length > 1) this._nodes[v] = value;
            return this;
        }
        this._nodes[v] = arguments.length > 1 ? value : this._defaultNodeLabelFn(v);
        if (this._isCompound) {
            this._parent[v] = GRAPH_NODE;
            this._children[v] = {};
            this._children[GRAPH_NODE][v] = true;
        }
        this._in[v] = {};
        this._preds[v] = {};
        this._out[v] = {};
        this._sucs[v] = {};
        ++this._nodeCount;
        return this;
    }
    node(v) {
        return this._nodes[v];
    }
    hasNode(v) {
        return (0, _chunkTZBO7MLIMjs.has_default)(this._nodes, v);
    }
    removeNode(v) {
        var self = this;
        if ((0, _chunkTZBO7MLIMjs.has_default)(this._nodes, v)) {
            var removeEdge = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(e) {
                self.removeEdge(self._edgeObjs[e]);
            }, "removeEdge");
            delete this._nodes[v];
            if (this._isCompound) {
                this._removeFromParentsChildList(v);
                delete this._parent[v];
                (0, _chunkTZBO7MLIMjs.forEach_default)(this.children(v), function(child) {
                    self.setParent(child);
                });
                delete this._children[v];
            }
            (0, _chunkTZBO7MLIMjs.forEach_default)((0, _chunkTZBO7MLIMjs.keys_default)(this._in[v]), removeEdge);
            delete this._in[v];
            delete this._preds[v];
            (0, _chunkTZBO7MLIMjs.forEach_default)((0, _chunkTZBO7MLIMjs.keys_default)(this._out[v]), removeEdge);
            delete this._out[v];
            delete this._sucs[v];
            --this._nodeCount;
        }
        return this;
    }
    setParent(v, parent) {
        if (!this._isCompound) throw new Error("Cannot set parent in a non-compound graph");
        if ((0, _chunkTZBO7MLIMjs.isUndefined_default)(parent)) parent = GRAPH_NODE;
        else {
            parent += "";
            for(var ancestor = parent; !(0, _chunkTZBO7MLIMjs.isUndefined_default)(ancestor); ancestor = this.parent(ancestor)){
                if (ancestor === v) throw new Error("Setting " + parent + " as parent of " + v + " would create a cycle");
            }
            this.setNode(parent);
        }
        this.setNode(v);
        this._removeFromParentsChildList(v);
        this._parent[v] = parent;
        this._children[parent][v] = true;
        return this;
    }
    _removeFromParentsChildList(v) {
        delete this._children[this._parent[v]][v];
    }
    parent(v) {
        if (this._isCompound) {
            var parent = this._parent[v];
            if (parent !== GRAPH_NODE) return parent;
        }
    }
    children(v) {
        if ((0, _chunkTZBO7MLIMjs.isUndefined_default)(v)) v = GRAPH_NODE;
        if (this._isCompound) {
            var children = this._children[v];
            if (children) return (0, _chunkTZBO7MLIMjs.keys_default)(children);
        } else if (v === GRAPH_NODE) return this.nodes();
        else if (this.hasNode(v)) return [];
    }
    predecessors(v) {
        var predsV = this._preds[v];
        if (predsV) return (0, _chunkTZBO7MLIMjs.keys_default)(predsV);
    }
    successors(v) {
        var sucsV = this._sucs[v];
        if (sucsV) return (0, _chunkTZBO7MLIMjs.keys_default)(sucsV);
    }
    neighbors(v) {
        var preds = this.predecessors(v);
        if (preds) return (0, _chunkTZBO7MLIMjs.union_default)(preds, this.successors(v));
    }
    isLeaf(v) {
        var neighbors;
        if (this.isDirected()) neighbors = this.successors(v);
        else neighbors = this.neighbors(v);
        return neighbors.length === 0;
    }
    filterNodes(filter) {
        var copy = new this.constructor({
            directed: this._isDirected,
            multigraph: this._isMultigraph,
            compound: this._isCompound
        });
        copy.setGraph(this.graph());
        var self = this;
        (0, _chunkTZBO7MLIMjs.forEach_default)(this._nodes, function(value, v) {
            if (filter(v)) copy.setNode(v, value);
        });
        (0, _chunkTZBO7MLIMjs.forEach_default)(this._edgeObjs, function(e) {
            if (copy.hasNode(e.v) && copy.hasNode(e.w)) copy.setEdge(e, self.edge(e));
        });
        var parents = {};
        function findParent(v) {
            var parent = self.parent(v);
            if (parent === void 0 || copy.hasNode(parent)) {
                parents[v] = parent;
                return parent;
            } else if (parent in parents) return parents[parent];
            else return findParent(parent);
        }
        (0, _chunkDLQEHMXDMjs.__name)(findParent, "findParent");
        if (this._isCompound) (0, _chunkTZBO7MLIMjs.forEach_default)(copy.nodes(), function(v) {
            copy.setParent(v, findParent(v));
        });
        return copy;
    }
    /* === Edge functions ========== */ setDefaultEdgeLabel(newDefault) {
        if (!(0, _chunkHD3LK5B5Mjs.isFunction_default)(newDefault)) newDefault = (0, _chunkHD3LK5B5Mjs.constant_default)(newDefault);
        this._defaultEdgeLabelFn = newDefault;
        return this;
    }
    edgeCount() {
        return this._edgeCount;
    }
    edges() {
        return (0, _chunkTZBO7MLIMjs.values_default)(this._edgeObjs);
    }
    setPath(vs, value) {
        var self = this;
        var args = arguments;
        (0, _chunkTZBO7MLIMjs.reduce_default)(vs, function(v, w) {
            if (args.length > 1) self.setEdge(v, w, value);
            else self.setEdge(v, w);
            return w;
        });
        return this;
    }
    /*
   * setEdge(v, w, [value, [name]])
   * setEdge({ v, w, [name] }, [value])
   */ setEdge() {
        var v, w, name, value;
        var valueSpecified = false;
        var arg0 = arguments[0];
        if (typeof arg0 === "object" && arg0 !== null && "v" in arg0) {
            v = arg0.v;
            w = arg0.w;
            name = arg0.name;
            if (arguments.length === 2) {
                value = arguments[1];
                valueSpecified = true;
            }
        } else {
            v = arg0;
            w = arguments[1];
            name = arguments[3];
            if (arguments.length > 2) {
                value = arguments[2];
                valueSpecified = true;
            }
        }
        v = "" + v;
        w = "" + w;
        if (!(0, _chunkTZBO7MLIMjs.isUndefined_default)(name)) name = "" + name;
        var e = edgeArgsToId(this._isDirected, v, w, name);
        if ((0, _chunkTZBO7MLIMjs.has_default)(this._edgeLabels, e)) {
            if (valueSpecified) this._edgeLabels[e] = value;
            return this;
        }
        if (!(0, _chunkTZBO7MLIMjs.isUndefined_default)(name) && !this._isMultigraph) throw new Error("Cannot set a named edge when isMultigraph = false");
        this.setNode(v);
        this.setNode(w);
        this._edgeLabels[e] = valueSpecified ? value : this._defaultEdgeLabelFn(v, w, name);
        var edgeObj = edgeArgsToObj(this._isDirected, v, w, name);
        v = edgeObj.v;
        w = edgeObj.w;
        Object.freeze(edgeObj);
        this._edgeObjs[e] = edgeObj;
        incrementOrInitEntry(this._preds[w], v);
        incrementOrInitEntry(this._sucs[v], w);
        this._in[w][e] = edgeObj;
        this._out[v][e] = edgeObj;
        this._edgeCount++;
        return this;
    }
    edge(v, w, name) {
        var e = arguments.length === 1 ? edgeObjToId(this._isDirected, arguments[0]) : edgeArgsToId(this._isDirected, v, w, name);
        return this._edgeLabels[e];
    }
    hasEdge(v, w, name) {
        var e = arguments.length === 1 ? edgeObjToId(this._isDirected, arguments[0]) : edgeArgsToId(this._isDirected, v, w, name);
        return (0, _chunkTZBO7MLIMjs.has_default)(this._edgeLabels, e);
    }
    removeEdge(v, w, name) {
        var e = arguments.length === 1 ? edgeObjToId(this._isDirected, arguments[0]) : edgeArgsToId(this._isDirected, v, w, name);
        var edge = this._edgeObjs[e];
        if (edge) {
            v = edge.v;
            w = edge.w;
            delete this._edgeLabels[e];
            delete this._edgeObjs[e];
            decrementOrRemoveEntry(this._preds[w], v);
            decrementOrRemoveEntry(this._sucs[v], w);
            delete this._in[w][e];
            delete this._out[v][e];
            this._edgeCount--;
        }
        return this;
    }
    inEdges(v, u) {
        var inV = this._in[v];
        if (inV) {
            var edges = (0, _chunkTZBO7MLIMjs.values_default)(inV);
            if (!u) return edges;
            return (0, _chunkTZBO7MLIMjs.filter_default)(edges, function(edge) {
                return edge.v === u;
            });
        }
    }
    outEdges(v, w) {
        var outV = this._out[v];
        if (outV) {
            var edges = (0, _chunkTZBO7MLIMjs.values_default)(outV);
            if (!w) return edges;
            return (0, _chunkTZBO7MLIMjs.filter_default)(edges, function(edge) {
                return edge.w === w;
            });
        }
    }
    nodeEdges(v, w) {
        var inEdges = this.inEdges(v, w);
        if (inEdges) return inEdges.concat(this.outEdges(v, w));
    }
};
Graph.prototype._nodeCount = 0;
Graph.prototype._edgeCount = 0;
function incrementOrInitEntry(map, k) {
    if (map[k]) map[k]++;
    else map[k] = 1;
}
(0, _chunkDLQEHMXDMjs.__name)(incrementOrInitEntry, "incrementOrInitEntry");
function decrementOrRemoveEntry(map, k) {
    if (!--map[k]) delete map[k];
}
(0, _chunkDLQEHMXDMjs.__name)(decrementOrRemoveEntry, "decrementOrRemoveEntry");
function edgeArgsToId(isDirected, v_, w_, name) {
    var v = "" + v_;
    var w = "" + w_;
    if (!isDirected && v > w) {
        var tmp = v;
        v = w;
        w = tmp;
    }
    return v + EDGE_KEY_DELIM + w + EDGE_KEY_DELIM + ((0, _chunkTZBO7MLIMjs.isUndefined_default)(name) ? DEFAULT_EDGE_NAME : name);
}
(0, _chunkDLQEHMXDMjs.__name)(edgeArgsToId, "edgeArgsToId");
function edgeArgsToObj(isDirected, v_, w_, name) {
    var v = "" + v_;
    var w = "" + w_;
    if (!isDirected && v > w) {
        var tmp = v;
        v = w;
        w = tmp;
    }
    var edgeObj = {
        v,
        w
    };
    if (name) edgeObj.name = name;
    return edgeObj;
}
(0, _chunkDLQEHMXDMjs.__name)(edgeArgsToObj, "edgeArgsToObj");
function edgeObjToId(isDirected, edgeObj) {
    return edgeArgsToId(isDirected, edgeObj.v, edgeObj.w, edgeObj.name);
}
(0, _chunkDLQEHMXDMjs.__name)(edgeObjToId, "edgeObjToId");

},{"./chunk-TZBO7MLI.mjs":"fY3tK","./chunk-GRZAG2UZ.mjs":"d1pnj","./chunk-HD3LK5B5.mjs":"lpo2u","./chunk-DLQEHMXD.mjs":"h5YRf","@parcel/transformer-js/src/esmodule-helpers.js":"1cdyk"}]},["iae2r"], null, "parcelRequire6955", {})

//# sourceMappingURL=classDiagram-v2-YNGOW5FH.ebfd65c9.js.map
