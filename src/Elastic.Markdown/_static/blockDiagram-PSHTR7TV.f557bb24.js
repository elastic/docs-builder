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
})({"9uaM2":[function(require,module,exports,__globalThis) {
var global = arguments[3];
var HMR_HOST = null;
var HMR_PORT = 1234;
var HMR_SERVER_PORT = 1234;
var HMR_SECURE = false;
var HMR_ENV_HASH = "febfef3c5b467b16";
var HMR_USE_SSE = false;
module.bundle.HMR_BUNDLE_ID = "44fb5573f557bb24";
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

},{}],"cL828":[function(require,module,exports,__globalThis) {
var parcelHelpers = require("@parcel/transformer-js/src/esmodule-helpers.js");
parcelHelpers.defineInteropFlag(exports);
parcelHelpers.export(exports, "diagram", ()=>diagram);
var _chunkBAJGW65CMjs = require("./chunk-BAJGW65C.mjs");
var _chunkHKQCUR3CMjs = require("./chunk-HKQCUR3C.mjs");
var _chunkKW7S66XIMjs = require("./chunk-KW7S66XI.mjs");
var _chunkYP6PVJQ3Mjs = require("./chunk-YP6PVJQ3.mjs");
var _chunkULVYQCHCMjs = require("./chunk-ULVYQCHC.mjs");
var _chunkI7ZFS43CMjs = require("./chunk-I7ZFS43C.mjs");
var _chunkGKOISANMMjs = require("./chunk-GKOISANM.mjs");
var _chunkDD37ZF33Mjs = require("./chunk-DD37ZF33.mjs");
var _chunkTZBO7MLIMjs = require("./chunk-TZBO7MLI.mjs");
var _chunkGRZAG2UZMjs = require("./chunk-GRZAG2UZ.mjs");
var _chunkHD3LK5B5Mjs = require("./chunk-HD3LK5B5.mjs");
var _chunkDLQEHMXDMjs = require("./chunk-DLQEHMXD.mjs");
// src/diagrams/block/parser/block.jison
var parser = function() {
    var o = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(k, v, o2, l) {
        for(o2 = o2 || {}, l = k.length; l--; o2[k[l]] = v);
        return o2;
    }, "o"), $V0 = [
        1,
        7
    ], $V1 = [
        1,
        13
    ], $V2 = [
        1,
        14
    ], $V3 = [
        1,
        15
    ], $V4 = [
        1,
        19
    ], $V5 = [
        1,
        16
    ], $V6 = [
        1,
        17
    ], $V7 = [
        1,
        18
    ], $V8 = [
        8,
        30
    ], $V9 = [
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
    ], $Va = [
        1,
        23
    ], $Vb = [
        1,
        24
    ], $Vc = [
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
    ], $Vd = [
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
    ], $Ve = [
        1,
        49
    ];
    var parser2 = {
        trace: /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function trace() {}, "trace"),
        yy: {},
        symbols_: {
            "error": 2,
            "spaceLines": 3,
            "SPACELINE": 4,
            "NL": 5,
            "separator": 6,
            "SPACE": 7,
            "EOF": 8,
            "start": 9,
            "BLOCK_DIAGRAM_KEY": 10,
            "document": 11,
            "stop": 12,
            "statement": 13,
            "link": 14,
            "LINK": 15,
            "START_LINK": 16,
            "LINK_LABEL": 17,
            "STR": 18,
            "nodeStatement": 19,
            "columnsStatement": 20,
            "SPACE_BLOCK": 21,
            "blockStatement": 22,
            "classDefStatement": 23,
            "cssClassStatement": 24,
            "styleStatement": 25,
            "node": 26,
            "SIZE": 27,
            "COLUMNS": 28,
            "id-block": 29,
            "end": 30,
            "block": 31,
            "NODE_ID": 32,
            "nodeShapeNLabel": 33,
            "dirList": 34,
            "DIR": 35,
            "NODE_DSTART": 36,
            "NODE_DEND": 37,
            "BLOCK_ARROW_START": 38,
            "BLOCK_ARROW_END": 39,
            "classDef": 40,
            "CLASSDEF_ID": 41,
            "CLASSDEF_STYLEOPTS": 42,
            "DEFAULT": 43,
            "class": 44,
            "CLASSENTITY_IDS": 45,
            "STYLECLASS": 46,
            "style": 47,
            "STYLE_ENTITY_IDS": 48,
            "STYLE_DEFINITION_DATA": 49,
            "$accept": 0,
            "$end": 1
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
        performAction: /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function anonymous(yytext, yyleng, yylineno, yy, yystate, $$, _$) {
            var $0 = $$.length - 1;
            switch(yystate){
                case 4:
                    yy.getLogger().debug("Rule: separator (NL) ");
                    break;
                case 5:
                    yy.getLogger().debug("Rule: separator (Space) ");
                    break;
                case 6:
                    yy.getLogger().debug("Rule: separator (EOF) ");
                    break;
                case 7:
                    yy.getLogger().debug("Rule: hierarchy: ", $$[$0 - 1]);
                    yy.setHierarchy($$[$0 - 1]);
                    break;
                case 8:
                    yy.getLogger().debug("Stop NL ");
                    break;
                case 9:
                    yy.getLogger().debug("Stop EOF ");
                    break;
                case 10:
                    yy.getLogger().debug("Stop NL2 ");
                    break;
                case 11:
                    yy.getLogger().debug("Stop EOF2 ");
                    break;
                case 12:
                    yy.getLogger().debug("Rule: statement: ", $$[$0]);
                    typeof $$[$0].length === "number" ? this.$ = $$[$0] : this.$ = [
                        $$[$0]
                    ];
                    break;
                case 13:
                    yy.getLogger().debug("Rule: statement #2: ", $$[$0 - 1]);
                    this.$ = [
                        $$[$0 - 1]
                    ].concat($$[$0]);
                    break;
                case 14:
                    yy.getLogger().debug("Rule: link: ", $$[$0], yytext);
                    this.$ = {
                        edgeTypeStr: $$[$0],
                        label: ""
                    };
                    break;
                case 15:
                    yy.getLogger().debug("Rule: LABEL link: ", $$[$0 - 3], $$[$0 - 1], $$[$0]);
                    this.$ = {
                        edgeTypeStr: $$[$0],
                        label: $$[$0 - 1]
                    };
                    break;
                case 18:
                    const num = parseInt($$[$0]);
                    const spaceId = yy.generateId();
                    this.$ = {
                        id: spaceId,
                        type: "space",
                        label: "",
                        width: num,
                        children: []
                    };
                    break;
                case 23:
                    yy.getLogger().debug("Rule: (nodeStatement link node) ", $$[$0 - 2], $$[$0 - 1], $$[$0], " typestr: ", $$[$0 - 1].edgeTypeStr);
                    const edgeData = yy.edgeStrToEdgeData($$[$0 - 1].edgeTypeStr);
                    this.$ = [
                        {
                            id: $$[$0 - 2].id,
                            label: $$[$0 - 2].label,
                            type: $$[$0 - 2].type,
                            directions: $$[$0 - 2].directions
                        },
                        {
                            id: $$[$0 - 2].id + "-" + $$[$0].id,
                            start: $$[$0 - 2].id,
                            end: $$[$0].id,
                            label: $$[$0 - 1].label,
                            type: "edge",
                            directions: $$[$0].directions,
                            arrowTypeEnd: edgeData,
                            arrowTypeStart: "arrow_open"
                        },
                        {
                            id: $$[$0].id,
                            label: $$[$0].label,
                            type: yy.typeStr2Type($$[$0].typeStr),
                            directions: $$[$0].directions
                        }
                    ];
                    break;
                case 24:
                    yy.getLogger().debug("Rule: nodeStatement (abc88 node size) ", $$[$0 - 1], $$[$0]);
                    this.$ = {
                        id: $$[$0 - 1].id,
                        label: $$[$0 - 1].label,
                        type: yy.typeStr2Type($$[$0 - 1].typeStr),
                        directions: $$[$0 - 1].directions,
                        widthInColumns: parseInt($$[$0], 10)
                    };
                    break;
                case 25:
                    yy.getLogger().debug("Rule: nodeStatement (node) ", $$[$0]);
                    this.$ = {
                        id: $$[$0].id,
                        label: $$[$0].label,
                        type: yy.typeStr2Type($$[$0].typeStr),
                        directions: $$[$0].directions,
                        widthInColumns: 1
                    };
                    break;
                case 26:
                    yy.getLogger().debug("APA123", this ? this : "na");
                    yy.getLogger().debug("COLUMNS: ", $$[$0]);
                    this.$ = {
                        type: "column-setting",
                        columns: $$[$0] === "auto" ? -1 : parseInt($$[$0])
                    };
                    break;
                case 27:
                    yy.getLogger().debug("Rule: id-block statement : ", $$[$0 - 2], $$[$0 - 1]);
                    const id2 = yy.generateId();
                    this.$ = {
                        ...$$[$0 - 2],
                        type: "composite",
                        children: $$[$0 - 1]
                    };
                    break;
                case 28:
                    yy.getLogger().debug("Rule: blockStatement : ", $$[$0 - 2], $$[$0 - 1], $$[$0]);
                    const id = yy.generateId();
                    this.$ = {
                        id,
                        type: "composite",
                        label: "",
                        children: $$[$0 - 1]
                    };
                    break;
                case 29:
                    yy.getLogger().debug("Rule: node (NODE_ID separator): ", $$[$0]);
                    this.$ = {
                        id: $$[$0]
                    };
                    break;
                case 30:
                    yy.getLogger().debug("Rule: node (NODE_ID nodeShapeNLabel separator): ", $$[$0 - 1], $$[$0]);
                    this.$ = {
                        id: $$[$0 - 1],
                        label: $$[$0].label,
                        typeStr: $$[$0].typeStr,
                        directions: $$[$0].directions
                    };
                    break;
                case 31:
                    yy.getLogger().debug("Rule: dirList: ", $$[$0]);
                    this.$ = [
                        $$[$0]
                    ];
                    break;
                case 32:
                    yy.getLogger().debug("Rule: dirList: ", $$[$0 - 1], $$[$0]);
                    this.$ = [
                        $$[$0 - 1]
                    ].concat($$[$0]);
                    break;
                case 33:
                    yy.getLogger().debug("Rule: nodeShapeNLabel: ", $$[$0 - 2], $$[$0 - 1], $$[$0]);
                    this.$ = {
                        typeStr: $$[$0 - 2] + $$[$0],
                        label: $$[$0 - 1]
                    };
                    break;
                case 34:
                    yy.getLogger().debug("Rule: BLOCK_ARROW nodeShapeNLabel: ", $$[$0 - 3], $$[$0 - 2], " #3:", $$[$0 - 1], $$[$0]);
                    this.$ = {
                        typeStr: $$[$0 - 3] + $$[$0],
                        label: $$[$0 - 2],
                        directions: $$[$0 - 1]
                    };
                    break;
                case 35:
                case 36:
                    this.$ = {
                        type: "classDef",
                        id: $$[$0 - 1].trim(),
                        css: $$[$0].trim()
                    };
                    break;
                case 37:
                    this.$ = {
                        type: "applyClass",
                        id: $$[$0 - 1].trim(),
                        styleClass: $$[$0].trim()
                    };
                    break;
                case 38:
                    this.$ = {
                        type: "applyStyles",
                        id: $$[$0 - 1].trim(),
                        stylesStr: $$[$0].trim()
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
                21: $V0,
                22: 8,
                23: 9,
                24: 10,
                25: 11,
                26: 12,
                28: $V1,
                29: $V2,
                31: $V3,
                32: $V4,
                40: $V5,
                44: $V6,
                47: $V7
            },
            {
                8: [
                    1,
                    20
                ]
            },
            o($V8, [
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
                21: $V0,
                28: $V1,
                29: $V2,
                31: $V3,
                32: $V4,
                40: $V5,
                44: $V6,
                47: $V7
            }),
            o($V9, [
                2,
                16
            ], {
                14: 22,
                15: $Va,
                16: $Vb
            }),
            o($V9, [
                2,
                17
            ]),
            o($V9, [
                2,
                18
            ]),
            o($V9, [
                2,
                19
            ]),
            o($V9, [
                2,
                20
            ]),
            o($V9, [
                2,
                21
            ]),
            o($V9, [
                2,
                22
            ]),
            o($Vc, [
                2,
                25
            ], {
                27: [
                    1,
                    25
                ]
            }),
            o($V9, [
                2,
                26
            ]),
            {
                19: 26,
                26: 12,
                32: $V4
            },
            {
                11: 27,
                13: 4,
                19: 5,
                20: 6,
                21: $V0,
                22: 8,
                23: 9,
                24: 10,
                25: 11,
                26: 12,
                28: $V1,
                29: $V2,
                31: $V3,
                32: $V4,
                40: $V5,
                44: $V6,
                47: $V7
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
            o($Vd, [
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
            o($V8, [
                2,
                13
            ]),
            {
                26: 35,
                32: $V4
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
            o($Vc, [
                2,
                24
            ]),
            {
                11: 37,
                13: 4,
                14: 22,
                15: $Va,
                16: $Vb,
                19: 5,
                20: 6,
                21: $V0,
                22: 8,
                23: 9,
                24: 10,
                25: 11,
                26: 12,
                28: $V1,
                29: $V2,
                31: $V3,
                32: $V4,
                40: $V5,
                44: $V6,
                47: $V7
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
            o($Vd, [
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
            o($Vc, [
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
            o($V9, [
                2,
                28
            ]),
            o($V9, [
                2,
                35
            ]),
            o($V9, [
                2,
                36
            ]),
            o($V9, [
                2,
                37
            ]),
            o($V9, [
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
                35: $Ve
            },
            {
                15: [
                    1,
                    50
                ]
            },
            o($V9, [
                2,
                27
            ]),
            o($Vd, [
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
                35: $Ve,
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
            o($Vd, [
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
        parseError: /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function parseError(str, hash) {
            if (hash.recoverable) this.trace(str);
            else {
                var error = new Error(str);
                error.hash = hash;
                throw error;
            }
        }, "parseError"),
        parse: /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function parse(input) {
            var self = this, stack = [
                0
            ], tstack = [], vstack = [
                null
            ], lstack = [], table = this.table, yytext = "", yylineno = 0, yyleng = 0, recovering = 0, TERROR = 2, EOF = 1;
            var args = lstack.slice.call(arguments, 1);
            var lexer2 = Object.create(this.lexer);
            var sharedState = {
                yy: {}
            };
            for(var k in this.yy)if (Object.prototype.hasOwnProperty.call(this.yy, k)) sharedState.yy[k] = this.yy[k];
            lexer2.setInput(input, sharedState.yy);
            sharedState.yy.lexer = lexer2;
            sharedState.yy.parser = this;
            if (typeof lexer2.yylloc == "undefined") lexer2.yylloc = {};
            var yyloc = lexer2.yylloc;
            lstack.push(yyloc);
            var ranges = lexer2.options && lexer2.options.ranges;
            if (typeof sharedState.yy.parseError === "function") this.parseError = sharedState.yy.parseError;
            else this.parseError = Object.getPrototypeOf(this).parseError;
            function popStack(n) {
                stack.length = stack.length - 2 * n;
                vstack.length = vstack.length - n;
                lstack.length = lstack.length - n;
            }
            (0, _chunkDLQEHMXDMjs.__name)(popStack, "popStack");
            function lex() {
                var token;
                token = tstack.pop() || lexer2.lex() || EOF;
                if (typeof token !== "number") {
                    if (token instanceof Array) {
                        tstack = token;
                        token = tstack.pop();
                    }
                    token = self.symbols_[token] || token;
                }
                return token;
            }
            (0, _chunkDLQEHMXDMjs.__name)(lex, "lex");
            var symbol, preErrorSymbol, state, action, a, r, yyval = {}, p, len, newState, expected;
            while(true){
                state = stack[stack.length - 1];
                if (this.defaultActions[state]) action = this.defaultActions[state];
                else {
                    if (symbol === null || typeof symbol == "undefined") symbol = lex();
                    action = table[state] && table[state][symbol];
                }
                if (typeof action === "undefined" || !action.length || !action[0]) {
                    var errStr = "";
                    expected = [];
                    for(p in table[state])if (this.terminals_[p] && p > TERROR) expected.push("'" + this.terminals_[p] + "'");
                    if (lexer2.showPosition) errStr = "Parse error on line " + (yylineno + 1) + ":\n" + lexer2.showPosition() + "\nExpecting " + expected.join(", ") + ", got '" + (this.terminals_[symbol] || symbol) + "'";
                    else errStr = "Parse error on line " + (yylineno + 1) + ": Unexpected " + (symbol == EOF ? "end of input" : "'" + (this.terminals_[symbol] || symbol) + "'");
                    this.parseError(errStr, {
                        text: lexer2.match,
                        token: this.terminals_[symbol] || symbol,
                        line: lexer2.yylineno,
                        loc: yyloc,
                        expected
                    });
                }
                if (action[0] instanceof Array && action.length > 1) throw new Error("Parse Error: multiple actions possible at state: " + state + ", token: " + symbol);
                switch(action[0]){
                    case 1:
                        stack.push(symbol);
                        vstack.push(lexer2.yytext);
                        lstack.push(lexer2.yylloc);
                        stack.push(action[1]);
                        symbol = null;
                        if (!preErrorSymbol) {
                            yyleng = lexer2.yyleng;
                            yytext = lexer2.yytext;
                            yylineno = lexer2.yylineno;
                            yyloc = lexer2.yylloc;
                            if (recovering > 0) recovering--;
                        } else {
                            symbol = preErrorSymbol;
                            preErrorSymbol = null;
                        }
                        break;
                    case 2:
                        len = this.productions_[action[1]][1];
                        yyval.$ = vstack[vstack.length - len];
                        yyval._$ = {
                            first_line: lstack[lstack.length - (len || 1)].first_line,
                            last_line: lstack[lstack.length - 1].last_line,
                            first_column: lstack[lstack.length - (len || 1)].first_column,
                            last_column: lstack[lstack.length - 1].last_column
                        };
                        if (ranges) yyval._$.range = [
                            lstack[lstack.length - (len || 1)].range[0],
                            lstack[lstack.length - 1].range[1]
                        ];
                        r = this.performAction.apply(yyval, [
                            yytext,
                            yyleng,
                            yylineno,
                            sharedState.yy,
                            action[1],
                            vstack,
                            lstack
                        ].concat(args));
                        if (typeof r !== "undefined") return r;
                        if (len) {
                            stack = stack.slice(0, -1 * len * 2);
                            vstack = vstack.slice(0, -1 * len);
                            lstack = lstack.slice(0, -1 * len);
                        }
                        stack.push(this.productions_[action[1]][0]);
                        vstack.push(yyval.$);
                        lstack.push(yyval._$);
                        newState = table[stack[stack.length - 2]][stack[stack.length - 1]];
                        stack.push(newState);
                        break;
                    case 3:
                        return true;
                }
            }
            return true;
        }, "parse")
    };
    var lexer = /* @__PURE__ */ function() {
        var lexer2 = {
            EOF: 1,
            parseError: /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function parseError(str, hash) {
                if (this.yy.parser) this.yy.parser.parseError(str, hash);
                else throw new Error(str);
            }, "parseError"),
            // resets the lexer, sets new input
            setInput: /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(input, yy) {
                this.yy = yy || this.yy || {};
                this._input = input;
                this._more = this._backtrack = this.done = false;
                this.yylineno = this.yyleng = 0;
                this.yytext = this.matched = this.match = "";
                this.conditionStack = [
                    "INITIAL"
                ];
                this.yylloc = {
                    first_line: 1,
                    first_column: 0,
                    last_line: 1,
                    last_column: 0
                };
                if (this.options.ranges) this.yylloc.range = [
                    0,
                    0
                ];
                this.offset = 0;
                return this;
            }, "setInput"),
            // consumes and returns one char from the input
            input: /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function() {
                var ch = this._input[0];
                this.yytext += ch;
                this.yyleng++;
                this.offset++;
                this.match += ch;
                this.matched += ch;
                var lines = ch.match(/(?:\r\n?|\n).*/g);
                if (lines) {
                    this.yylineno++;
                    this.yylloc.last_line++;
                } else this.yylloc.last_column++;
                if (this.options.ranges) this.yylloc.range[1]++;
                this._input = this._input.slice(1);
                return ch;
            }, "input"),
            // unshifts one char (or a string) into the input
            unput: /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(ch) {
                var len = ch.length;
                var lines = ch.split(/(?:\r\n?|\n)/g);
                this._input = ch + this._input;
                this.yytext = this.yytext.substr(0, this.yytext.length - len);
                this.offset -= len;
                var oldLines = this.match.split(/(?:\r\n?|\n)/g);
                this.match = this.match.substr(0, this.match.length - 1);
                this.matched = this.matched.substr(0, this.matched.length - 1);
                if (lines.length - 1) this.yylineno -= lines.length - 1;
                var r = this.yylloc.range;
                this.yylloc = {
                    first_line: this.yylloc.first_line,
                    last_line: this.yylineno + 1,
                    first_column: this.yylloc.first_column,
                    last_column: lines ? (lines.length === oldLines.length ? this.yylloc.first_column : 0) + oldLines[oldLines.length - lines.length].length - lines[0].length : this.yylloc.first_column - len
                };
                if (this.options.ranges) this.yylloc.range = [
                    r[0],
                    r[0] + this.yyleng - len
                ];
                this.yyleng = this.yytext.length;
                return this;
            }, "unput"),
            // When called from action, caches matched text and appends it on next action
            more: /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function() {
                this._more = true;
                return this;
            }, "more"),
            // When called from action, signals the lexer that this rule fails to match the input, so the next matching rule (regex) should be tested instead.
            reject: /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function() {
                if (this.options.backtrack_lexer) this._backtrack = true;
                else return this.parseError("Lexical error on line " + (this.yylineno + 1) + ". You can only invoke reject() in the lexer when the lexer is of the backtracking persuasion (options.backtrack_lexer = true).\n" + this.showPosition(), {
                    text: "",
                    token: null,
                    line: this.yylineno
                });
                return this;
            }, "reject"),
            // retain first n characters of the match
            less: /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(n) {
                this.unput(this.match.slice(n));
            }, "less"),
            // displays already matched input, i.e. for error messages
            pastInput: /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function() {
                var past = this.matched.substr(0, this.matched.length - this.match.length);
                return (past.length > 20 ? "..." : "") + past.substr(-20).replace(/\n/g, "");
            }, "pastInput"),
            // displays upcoming input, i.e. for error messages
            upcomingInput: /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function() {
                var next = this.match;
                if (next.length < 20) next += this._input.substr(0, 20 - next.length);
                return (next.substr(0, 20) + (next.length > 20 ? "..." : "")).replace(/\n/g, "");
            }, "upcomingInput"),
            // displays the character position where the lexing error occurred, i.e. for error messages
            showPosition: /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function() {
                var pre = this.pastInput();
                var c = new Array(pre.length + 1).join("-");
                return pre + this.upcomingInput() + "\n" + c + "^";
            }, "showPosition"),
            // test the lexed token: return FALSE when not a match, otherwise return token
            test_match: /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(match, indexed_rule) {
                var token, lines, backup;
                if (this.options.backtrack_lexer) {
                    backup = {
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
                    };
                    if (this.options.ranges) backup.yylloc.range = this.yylloc.range.slice(0);
                }
                lines = match[0].match(/(?:\r\n?|\n).*/g);
                if (lines) this.yylineno += lines.length;
                this.yylloc = {
                    first_line: this.yylloc.last_line,
                    last_line: this.yylineno + 1,
                    first_column: this.yylloc.last_column,
                    last_column: lines ? lines[lines.length - 1].length - lines[lines.length - 1].match(/\r?\n?/)[0].length : this.yylloc.last_column + match[0].length
                };
                this.yytext += match[0];
                this.match += match[0];
                this.matches = match;
                this.yyleng = this.yytext.length;
                if (this.options.ranges) this.yylloc.range = [
                    this.offset,
                    this.offset += this.yyleng
                ];
                this._more = false;
                this._backtrack = false;
                this._input = this._input.slice(match[0].length);
                this.matched += match[0];
                token = this.performAction.call(this, this.yy, this, indexed_rule, this.conditionStack[this.conditionStack.length - 1]);
                if (this.done && this._input) this.done = false;
                if (token) return token;
                else if (this._backtrack) {
                    for(var k in backup)this[k] = backup[k];
                    return false;
                }
                return false;
            }, "test_match"),
            // return next match in input
            next: /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function() {
                if (this.done) return this.EOF;
                if (!this._input) this.done = true;
                var token, match, tempMatch, index;
                if (!this._more) {
                    this.yytext = "";
                    this.match = "";
                }
                var rules = this._currentRules();
                for(var i = 0; i < rules.length; i++){
                    tempMatch = this._input.match(this.rules[rules[i]]);
                    if (tempMatch && (!match || tempMatch[0].length > match[0].length)) {
                        match = tempMatch;
                        index = i;
                        if (this.options.backtrack_lexer) {
                            token = this.test_match(tempMatch, rules[i]);
                            if (token !== false) return token;
                            else if (this._backtrack) {
                                match = false;
                                continue;
                            } else return false;
                        } else if (!this.options.flex) break;
                    }
                }
                if (match) {
                    token = this.test_match(match, rules[index]);
                    if (token !== false) return token;
                    return false;
                }
                if (this._input === "") return this.EOF;
                else return this.parseError("Lexical error on line " + (this.yylineno + 1) + ". Unrecognized text.\n" + this.showPosition(), {
                    text: "",
                    token: null,
                    line: this.yylineno
                });
            }, "next"),
            // return next match that has a token
            lex: /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function lex() {
                var r = this.next();
                if (r) return r;
                else return this.lex();
            }, "lex"),
            // activates a new lexer condition state (pushes the new lexer condition state onto the condition stack)
            begin: /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function begin(condition) {
                this.conditionStack.push(condition);
            }, "begin"),
            // pop the previously active lexer condition state off the condition stack
            popState: /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function popState() {
                var n = this.conditionStack.length - 1;
                if (n > 0) return this.conditionStack.pop();
                else return this.conditionStack[0];
            }, "popState"),
            // produce the lexer rule set which is active for the currently active lexer condition state
            _currentRules: /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function _currentRules() {
                if (this.conditionStack.length && this.conditionStack[this.conditionStack.length - 1]) return this.conditions[this.conditionStack[this.conditionStack.length - 1]].rules;
                else return this.conditions["INITIAL"].rules;
            }, "_currentRules"),
            // return the currently active lexer condition state; when an index argument is provided it produces the N-th previous condition state, if available
            topState: /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function topState(n) {
                n = this.conditionStack.length - 1 - Math.abs(n || 0);
                if (n >= 0) return this.conditionStack[n];
                else return "INITIAL";
            }, "topState"),
            // alias for begin(condition)
            pushState: /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function pushState(condition) {
                this.begin(condition);
            }, "pushState"),
            // return the number of states currently on the stack
            stateStackSize: /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function stateStackSize() {
                return this.conditionStack.length;
            }, "stateStackSize"),
            options: {},
            performAction: /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function anonymous(yy, yy_, $avoiding_name_collisions, YY_START) {
                var YYSTATE = YY_START;
                switch($avoiding_name_collisions){
                    case 0:
                        return 10;
                    case 1:
                        yy.getLogger().debug("Found space-block");
                        return 31;
                    case 2:
                        yy.getLogger().debug("Found nl-block");
                        return 31;
                    case 3:
                        yy.getLogger().debug("Found space-block");
                        return 29;
                    case 4:
                        yy.getLogger().debug(".", yy_.yytext);
                        break;
                    case 5:
                        yy.getLogger().debug("_", yy_.yytext);
                        break;
                    case 6:
                        return 5;
                    case 7:
                        yy_.yytext = -1;
                        return 28;
                    case 8:
                        yy_.yytext = yy_.yytext.replace(/columns\s+/, "");
                        yy.getLogger().debug("COLUMNS (LEX)", yy_.yytext);
                        return 28;
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
                        yy.getLogger().debug("LEX: POPPING STR:", yy_.yytext);
                        this.popState();
                        break;
                    case 14:
                        yy.getLogger().debug("LEX: STR end:", yy_.yytext);
                        return "STR";
                    case 15:
                        yy_.yytext = yy_.yytext.replace(/space\:/, "");
                        yy.getLogger().debug("SPACE NUM (LEX)", yy_.yytext);
                        return 21;
                    case 16:
                        yy_.yytext = "1";
                        yy.getLogger().debug("COLUMNS (LEX)", yy_.yytext);
                        return 21;
                    case 17:
                        return 43;
                    case 18:
                        return "LINKSTYLE";
                    case 19:
                        return "INTERPOLATE";
                    case 20:
                        this.pushState("CLASSDEF");
                        return 40;
                    case 21:
                        this.popState();
                        this.pushState("CLASSDEFID");
                        return "DEFAULT_CLASSDEF_ID";
                    case 22:
                        this.popState();
                        this.pushState("CLASSDEFID");
                        return 41;
                    case 23:
                        this.popState();
                        return 42;
                    case 24:
                        this.pushState("CLASS");
                        return 44;
                    case 25:
                        this.popState();
                        this.pushState("CLASS_STYLE");
                        return 45;
                    case 26:
                        this.popState();
                        return 46;
                    case 27:
                        this.pushState("STYLE_STMNT");
                        return 47;
                    case 28:
                        this.popState();
                        this.pushState("STYLE_DEFINITION");
                        return 48;
                    case 29:
                        this.popState();
                        return 49;
                    case 30:
                        this.pushState("acc_title");
                        return "acc_title";
                    case 31:
                        this.popState();
                        return "acc_title_value";
                    case 32:
                        this.pushState("acc_descr");
                        return "acc_descr";
                    case 33:
                        this.popState();
                        return "acc_descr_value";
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
                        this.popState();
                        yy.getLogger().debug("Lex: ((");
                        return "NODE_DEND";
                    case 39:
                        this.popState();
                        yy.getLogger().debug("Lex: ((");
                        return "NODE_DEND";
                    case 40:
                        this.popState();
                        yy.getLogger().debug("Lex: ))");
                        return "NODE_DEND";
                    case 41:
                        this.popState();
                        yy.getLogger().debug("Lex: ((");
                        return "NODE_DEND";
                    case 42:
                        this.popState();
                        yy.getLogger().debug("Lex: ((");
                        return "NODE_DEND";
                    case 43:
                        this.popState();
                        yy.getLogger().debug("Lex: (-");
                        return "NODE_DEND";
                    case 44:
                        this.popState();
                        yy.getLogger().debug("Lex: -)");
                        return "NODE_DEND";
                    case 45:
                        this.popState();
                        yy.getLogger().debug("Lex: ((");
                        return "NODE_DEND";
                    case 46:
                        this.popState();
                        yy.getLogger().debug("Lex: ]]");
                        return "NODE_DEND";
                    case 47:
                        this.popState();
                        yy.getLogger().debug("Lex: (");
                        return "NODE_DEND";
                    case 48:
                        this.popState();
                        yy.getLogger().debug("Lex: ])");
                        return "NODE_DEND";
                    case 49:
                        this.popState();
                        yy.getLogger().debug("Lex: /]");
                        return "NODE_DEND";
                    case 50:
                        this.popState();
                        yy.getLogger().debug("Lex: /]");
                        return "NODE_DEND";
                    case 51:
                        this.popState();
                        yy.getLogger().debug("Lex: )]");
                        return "NODE_DEND";
                    case 52:
                        this.popState();
                        yy.getLogger().debug("Lex: )");
                        return "NODE_DEND";
                    case 53:
                        this.popState();
                        yy.getLogger().debug("Lex: ]>");
                        return "NODE_DEND";
                    case 54:
                        this.popState();
                        yy.getLogger().debug("Lex: ]");
                        return "NODE_DEND";
                    case 55:
                        yy.getLogger().debug("Lexa: -)");
                        this.pushState("NODE");
                        return 36;
                    case 56:
                        yy.getLogger().debug("Lexa: (-");
                        this.pushState("NODE");
                        return 36;
                    case 57:
                        yy.getLogger().debug("Lexa: ))");
                        this.pushState("NODE");
                        return 36;
                    case 58:
                        yy.getLogger().debug("Lexa: )");
                        this.pushState("NODE");
                        return 36;
                    case 59:
                        yy.getLogger().debug("Lex: (((");
                        this.pushState("NODE");
                        return 36;
                    case 60:
                        yy.getLogger().debug("Lexa: )");
                        this.pushState("NODE");
                        return 36;
                    case 61:
                        yy.getLogger().debug("Lexa: )");
                        this.pushState("NODE");
                        return 36;
                    case 62:
                        yy.getLogger().debug("Lexa: )");
                        this.pushState("NODE");
                        return 36;
                    case 63:
                        yy.getLogger().debug("Lexc: >");
                        this.pushState("NODE");
                        return 36;
                    case 64:
                        yy.getLogger().debug("Lexa: ([");
                        this.pushState("NODE");
                        return 36;
                    case 65:
                        yy.getLogger().debug("Lexa: )");
                        this.pushState("NODE");
                        return 36;
                    case 66:
                        this.pushState("NODE");
                        return 36;
                    case 67:
                        this.pushState("NODE");
                        return 36;
                    case 68:
                        this.pushState("NODE");
                        return 36;
                    case 69:
                        this.pushState("NODE");
                        return 36;
                    case 70:
                        this.pushState("NODE");
                        return 36;
                    case 71:
                        this.pushState("NODE");
                        return 36;
                    case 72:
                        this.pushState("NODE");
                        return 36;
                    case 73:
                        yy.getLogger().debug("Lexa: [");
                        this.pushState("NODE");
                        return 36;
                    case 74:
                        this.pushState("BLOCK_ARROW");
                        yy.getLogger().debug("LEX ARR START");
                        return 38;
                    case 75:
                        yy.getLogger().debug("Lex: NODE_ID", yy_.yytext);
                        return 32;
                    case 76:
                        yy.getLogger().debug("Lex: EOF", yy_.yytext);
                        return 8;
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
                        yy.getLogger().debug("Lex: Starting string");
                        this.pushState("string");
                        break;
                    case 82:
                        yy.getLogger().debug("LEX ARR: Starting string");
                        this.pushState("string");
                        break;
                    case 83:
                        yy.getLogger().debug("LEX: NODE_DESCR:", yy_.yytext);
                        return "NODE_DESCR";
                    case 84:
                        yy.getLogger().debug("LEX POPPING");
                        this.popState();
                        break;
                    case 85:
                        yy.getLogger().debug("Lex: =>BAE");
                        this.pushState("ARROW_DIR");
                        break;
                    case 86:
                        yy_.yytext = yy_.yytext.replace(/^,\s*/, "");
                        yy.getLogger().debug("Lex (right): dir:", yy_.yytext);
                        return "DIR";
                    case 87:
                        yy_.yytext = yy_.yytext.replace(/^,\s*/, "");
                        yy.getLogger().debug("Lex (left):", yy_.yytext);
                        return "DIR";
                    case 88:
                        yy_.yytext = yy_.yytext.replace(/^,\s*/, "");
                        yy.getLogger().debug("Lex (x):", yy_.yytext);
                        return "DIR";
                    case 89:
                        yy_.yytext = yy_.yytext.replace(/^,\s*/, "");
                        yy.getLogger().debug("Lex (y):", yy_.yytext);
                        return "DIR";
                    case 90:
                        yy_.yytext = yy_.yytext.replace(/^,\s*/, "");
                        yy.getLogger().debug("Lex (up):", yy_.yytext);
                        return "DIR";
                    case 91:
                        yy_.yytext = yy_.yytext.replace(/^,\s*/, "");
                        yy.getLogger().debug("Lex (down):", yy_.yytext);
                        return "DIR";
                    case 92:
                        yy_.yytext = "]>";
                        yy.getLogger().debug("Lex (ARROW_DIR end):", yy_.yytext);
                        this.popState();
                        this.popState();
                        return "BLOCK_ARROW_END";
                    case 93:
                        yy.getLogger().debug("Lex: LINK", "#" + yy_.yytext + "#");
                        return 15;
                    case 94:
                        yy.getLogger().debug("Lex: LINK", yy_.yytext);
                        return 15;
                    case 95:
                        yy.getLogger().debug("Lex: LINK", yy_.yytext);
                        return 15;
                    case 96:
                        yy.getLogger().debug("Lex: LINK", yy_.yytext);
                        return 15;
                    case 97:
                        yy.getLogger().debug("Lex: START_LINK", yy_.yytext);
                        this.pushState("LLABEL");
                        return 16;
                    case 98:
                        yy.getLogger().debug("Lex: START_LINK", yy_.yytext);
                        this.pushState("LLABEL");
                        return 16;
                    case 99:
                        yy.getLogger().debug("Lex: START_LINK", yy_.yytext);
                        this.pushState("LLABEL");
                        return 16;
                    case 100:
                        this.pushState("md_string");
                        break;
                    case 101:
                        yy.getLogger().debug("Lex: Starting string");
                        this.pushState("string");
                        return "LINK_LABEL";
                    case 102:
                        this.popState();
                        yy.getLogger().debug("Lex: LINK", "#" + yy_.yytext + "#");
                        return 15;
                    case 103:
                        this.popState();
                        yy.getLogger().debug("Lex: LINK", yy_.yytext);
                        return 15;
                    case 104:
                        this.popState();
                        yy.getLogger().debug("Lex: LINK", yy_.yytext);
                        return 15;
                    case 105:
                        yy.getLogger().debug("Lex: COLON", yy_.yytext);
                        yy_.yytext = yy_.yytext.slice(1);
                        return 27;
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
                "STYLE_DEFINITION": {
                    "rules": [
                        29
                    ],
                    "inclusive": false
                },
                "STYLE_STMNT": {
                    "rules": [
                        28
                    ],
                    "inclusive": false
                },
                "CLASSDEFID": {
                    "rules": [
                        23
                    ],
                    "inclusive": false
                },
                "CLASSDEF": {
                    "rules": [
                        21,
                        22
                    ],
                    "inclusive": false
                },
                "CLASS_STYLE": {
                    "rules": [
                        26
                    ],
                    "inclusive": false
                },
                "CLASS": {
                    "rules": [
                        25
                    ],
                    "inclusive": false
                },
                "LLABEL": {
                    "rules": [
                        100,
                        101,
                        102,
                        103,
                        104
                    ],
                    "inclusive": false
                },
                "ARROW_DIR": {
                    "rules": [
                        86,
                        87,
                        88,
                        89,
                        90,
                        91,
                        92
                    ],
                    "inclusive": false
                },
                "BLOCK_ARROW": {
                    "rules": [
                        77,
                        82,
                        85
                    ],
                    "inclusive": false
                },
                "NODE": {
                    "rules": [
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
                    "inclusive": false
                },
                "md_string": {
                    "rules": [
                        10,
                        11,
                        79,
                        80
                    ],
                    "inclusive": false
                },
                "space": {
                    "rules": [],
                    "inclusive": false
                },
                "string": {
                    "rules": [
                        13,
                        14,
                        83,
                        84
                    ],
                    "inclusive": false
                },
                "acc_descr_multiline": {
                    "rules": [
                        35,
                        36
                    ],
                    "inclusive": false
                },
                "acc_descr": {
                    "rules": [
                        33
                    ],
                    "inclusive": false
                },
                "acc_title": {
                    "rules": [
                        31
                    ],
                    "inclusive": false
                },
                "INITIAL": {
                    "rules": [
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
                    "inclusive": true
                }
            }
        };
        return lexer2;
    }();
    parser2.lexer = lexer;
    function Parser() {
        this.yy = {};
    }
    (0, _chunkDLQEHMXDMjs.__name)(Parser, "Parser");
    Parser.prototype = parser2;
    parser2.Parser = Parser;
    return new Parser();
}();
parser.parser = parser;
var block_default = parser;
// src/diagrams/block/blockDB.ts
var blockDatabase = /* @__PURE__ */ new Map();
var edgeList = [];
var edgeCount = /* @__PURE__ */ new Map();
var COLOR_KEYWORD = "color";
var FILL_KEYWORD = "fill";
var BG_FILL = "bgFill";
var STYLECLASS_SEP = ",";
var config = (0, _chunkDD37ZF33Mjs.getConfig2)();
var classes = /* @__PURE__ */ new Map();
var sanitizeText = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((txt)=>(0, _chunkDD37ZF33Mjs.common_default).sanitizeText(txt, config), "sanitizeText");
var addStyleClass = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(id, styleAttributes = "") {
    let foundClass = classes.get(id);
    if (!foundClass) {
        foundClass = {
            id,
            styles: [],
            textStyles: []
        };
        classes.set(id, foundClass);
    }
    if (styleAttributes !== void 0 && styleAttributes !== null) styleAttributes.split(STYLECLASS_SEP).forEach((attrib)=>{
        const fixedAttrib = attrib.replace(/([^;]*);/, "$1").trim();
        if (RegExp(COLOR_KEYWORD).exec(attrib)) {
            const newStyle1 = fixedAttrib.replace(FILL_KEYWORD, BG_FILL);
            const newStyle2 = newStyle1.replace(COLOR_KEYWORD, FILL_KEYWORD);
            foundClass.textStyles.push(newStyle2);
        }
        foundClass.styles.push(fixedAttrib);
    });
}, "addStyleClass");
var addStyle2Node = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(id, styles = "") {
    const foundBlock = blockDatabase.get(id);
    if (styles !== void 0 && styles !== null) foundBlock.styles = styles.split(STYLECLASS_SEP);
}, "addStyle2Node");
var setCssClass = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(itemIds, cssClassName) {
    itemIds.split(",").forEach(function(id) {
        let foundBlock = blockDatabase.get(id);
        if (foundBlock === void 0) {
            const trimmedId = id.trim();
            foundBlock = {
                id: trimmedId,
                type: "na",
                children: []
            };
            blockDatabase.set(trimmedId, foundBlock);
        }
        if (!foundBlock.classes) foundBlock.classes = [];
        foundBlock.classes.push(cssClassName);
    });
}, "setCssClass");
var populateBlockDatabase = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((_blockList, parent)=>{
    const blockList = _blockList.flat();
    const children = [];
    for (const block of blockList){
        if (block.label) block.label = sanitizeText(block.label);
        if (block.type === "classDef") {
            addStyleClass(block.id, block.css);
            continue;
        }
        if (block.type === "applyClass") {
            setCssClass(block.id, block?.styleClass ?? "");
            continue;
        }
        if (block.type === "applyStyles") {
            if (block?.stylesStr) addStyle2Node(block.id, block?.stylesStr);
            continue;
        }
        if (block.type === "column-setting") parent.columns = block.columns ?? -1;
        else if (block.type === "edge") {
            const count = (edgeCount.get(block.id) ?? 0) + 1;
            edgeCount.set(block.id, count);
            block.id = count + "-" + block.id;
            edgeList.push(block);
        } else {
            if (!block.label) {
                if (block.type === "composite") block.label = "";
                else block.label = block.id;
            }
            const existingBlock = blockDatabase.get(block.id);
            if (existingBlock === void 0) blockDatabase.set(block.id, block);
            else {
                if (block.type !== "na") existingBlock.type = block.type;
                if (block.label !== block.id) existingBlock.label = block.label;
            }
            if (block.children) populateBlockDatabase(block.children, block);
            if (block.type === "space") {
                const w = block.width ?? 1;
                for(let j = 0; j < w; j++){
                    const newBlock = (0, _chunkTZBO7MLIMjs.clone_default)(block);
                    newBlock.id = newBlock.id + "-" + j;
                    blockDatabase.set(newBlock.id, newBlock);
                    children.push(newBlock);
                }
            } else if (existingBlock === void 0) children.push(block);
        }
    }
    parent.children = children;
}, "populateBlockDatabase");
var blocks = [];
var rootBlock = {
    id: "root",
    type: "composite",
    children: [],
    columns: -1
};
var clear2 = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(()=>{
    (0, _chunkDD37ZF33Mjs.log).debug("Clear called");
    (0, _chunkDD37ZF33Mjs.clear)();
    rootBlock = {
        id: "root",
        type: "composite",
        children: [],
        columns: -1
    };
    blockDatabase = /* @__PURE__ */ new Map([
        [
            "root",
            rootBlock
        ]
    ]);
    blocks = [];
    classes = /* @__PURE__ */ new Map();
    edgeList = [];
    edgeCount = /* @__PURE__ */ new Map();
}, "clear");
function typeStr2Type(typeStr) {
    (0, _chunkDD37ZF33Mjs.log).debug("typeStr2Type", typeStr);
    switch(typeStr){
        case "[]":
            return "square";
        case "()":
            (0, _chunkDD37ZF33Mjs.log).debug("we have a round");
            return "round";
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
(0, _chunkDLQEHMXDMjs.__name)(typeStr2Type, "typeStr2Type");
function edgeTypeStr2Type(typeStr) {
    (0, _chunkDD37ZF33Mjs.log).debug("typeStr2Type", typeStr);
    switch(typeStr){
        case "==":
            return "thick";
        default:
            return "normal";
    }
}
(0, _chunkDLQEHMXDMjs.__name)(edgeTypeStr2Type, "edgeTypeStr2Type");
function edgeStrToEdgeData(typeStr) {
    switch(typeStr.trim()){
        case "--x":
            return "arrow_cross";
        case "--o":
            return "arrow_circle";
        default:
            return "arrow_point";
    }
}
(0, _chunkDLQEHMXDMjs.__name)(edgeStrToEdgeData, "edgeStrToEdgeData");
var cnt = 0;
var generateId = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(()=>{
    cnt++;
    return "id-" + Math.random().toString(36).substr(2, 12) + "-" + cnt;
}, "generateId");
var setHierarchy = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((block)=>{
    rootBlock.children = block;
    populateBlockDatabase(block, rootBlock);
    blocks = rootBlock.children;
}, "setHierarchy");
var getColumns = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((blockId)=>{
    const block = blockDatabase.get(blockId);
    if (!block) return -1;
    if (block.columns) return block.columns;
    if (!block.children) return -1;
    return block.children.length;
}, "getColumns");
var getBlocksFlat = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(()=>{
    return [
        ...blockDatabase.values()
    ];
}, "getBlocksFlat");
var getBlocks = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(()=>{
    return blocks || [];
}, "getBlocks");
var getEdges = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(()=>{
    return edgeList;
}, "getEdges");
var getBlock = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((id)=>{
    return blockDatabase.get(id);
}, "getBlock");
var setBlock = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((block)=>{
    blockDatabase.set(block.id, block);
}, "setBlock");
var getLogger = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(()=>console, "getLogger");
var getClasses = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function() {
    return classes;
}, "getClasses");
var db = {
    getConfig: /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(()=>(0, _chunkDD37ZF33Mjs.getConfig)().block, "getConfig"),
    typeStr2Type,
    edgeTypeStr2Type,
    edgeStrToEdgeData,
    getLogger,
    getBlocksFlat,
    getBlocks,
    getEdges,
    setHierarchy,
    getBlock,
    setBlock,
    getColumns,
    getClasses,
    clear: clear2,
    generateId
};
var blockDB_default = db;
// src/diagrams/block/styles.ts
var fade = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((color, opacity)=>{
    const channel = (0, _chunkDD37ZF33Mjs.channel_default);
    const r = channel(color, "r");
    const g = channel(color, "g");
    const b = channel(color, "b");
    return (0, _chunkDD37ZF33Mjs.rgba_default)(r, g, b, opacity);
}, "fade");
var getStyles = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((options)=>`.label {
    font-family: ${options.fontFamily};
    color: ${options.nodeTextColor || options.textColor};
  }
  .cluster-label text {
    fill: ${options.titleColor};
  }
  .cluster-label span,p {
    color: ${options.titleColor};
  }



  .label text,span,p {
    fill: ${options.nodeTextColor || options.textColor};
    color: ${options.nodeTextColor || options.textColor};
  }

  .node rect,
  .node circle,
  .node ellipse,
  .node polygon,
  .node path {
    fill: ${options.mainBkg};
    stroke: ${options.nodeBorder};
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
    fill: ${options.arrowheadColor};
  }

  .edgePath .path {
    stroke: ${options.lineColor};
    stroke-width: 2.0px;
  }

  .flowchart-link {
    stroke: ${options.lineColor};
    fill: none;
  }

  .edgeLabel {
    background-color: ${options.edgeLabelBackground};
    rect {
      opacity: 0.5;
      background-color: ${options.edgeLabelBackground};
      fill: ${options.edgeLabelBackground};
    }
    text-align: center;
  }

  /* For html labels only */
  .labelBkg {
    background-color: ${fade(options.edgeLabelBackground, 0.5)};
    // background-color:
  }

  .node .cluster {
    // fill: ${fade(options.mainBkg, 0.5)};
    fill: ${fade(options.clusterBkg, 0.5)};
    stroke: ${fade(options.clusterBorder, 0.2)};
    box-shadow: rgba(50, 50, 93, 0.25) 0px 13px 27px -5px, rgba(0, 0, 0, 0.3) 0px 8px 16px -8px;
    stroke-width: 1px;
  }

  .cluster text {
    fill: ${options.titleColor};
  }

  .cluster span,p {
    color: ${options.titleColor};
  }
  /* .cluster div {
    color: ${options.titleColor};
  } */

  div.mermaidTooltip {
    position: absolute;
    text-align: center;
    max-width: 200px;
    padding: 2px;
    font-family: ${options.fontFamily};
    font-size: 12px;
    background: ${options.tertiaryColor};
    border: 1px solid ${options.border2};
    border-radius: 2px;
    pointer-events: none;
    z-index: 100;
  }

  .flowchartTitleText {
    text-anchor: middle;
    font-size: 18px;
    fill: ${options.textColor};
  }
`, "getStyles");
var styles_default = getStyles;
// src/diagrams/block/layout.ts
var padding = (0, _chunkDD37ZF33Mjs.getConfig2)()?.block?.padding ?? 8;
function calculateBlockPosition(columns, position) {
    if (columns === 0 || !Number.isInteger(columns)) throw new Error("Columns must be an integer !== 0.");
    if (position < 0 || !Number.isInteger(position)) throw new Error("Position must be a non-negative integer." + position);
    if (columns < 0) return {
        px: position,
        py: 0
    };
    if (columns === 1) return {
        px: 0,
        py: position
    };
    const px = position % columns;
    const py = Math.floor(position / columns);
    return {
        px,
        py
    };
}
(0, _chunkDLQEHMXDMjs.__name)(calculateBlockPosition, "calculateBlockPosition");
var getMaxChildSize = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((block)=>{
    let maxWidth = 0;
    let maxHeight = 0;
    for (const child of block.children){
        const { width, height, x, y } = child.size ?? {
            width: 0,
            height: 0,
            x: 0,
            y: 0
        };
        (0, _chunkDD37ZF33Mjs.log).debug("getMaxChildSize abc95 child:", child.id, "width:", width, "height:", height, "x:", x, "y:", y, child.type);
        if (child.type === "space") continue;
        if (width > maxWidth) maxWidth = width / (block.widthInColumns ?? 1);
        if (height > maxHeight) maxHeight = height;
    }
    return {
        width: maxWidth,
        height: maxHeight
    };
}, "getMaxChildSize");
function setBlockSizes(block, db2, siblingWidth = 0, siblingHeight = 0) {
    (0, _chunkDD37ZF33Mjs.log).debug("setBlockSizes abc95 (start)", block.id, block?.size?.x, "block width =", block?.size, "sieblingWidth", siblingWidth);
    if (!block?.size?.width) block.size = {
        width: siblingWidth,
        height: siblingHeight,
        x: 0,
        y: 0
    };
    let maxWidth = 0;
    let maxHeight = 0;
    if (block.children?.length > 0) {
        for (const child of block.children)setBlockSizes(child, db2);
        const childSize = getMaxChildSize(block);
        maxWidth = childSize.width;
        maxHeight = childSize.height;
        (0, _chunkDD37ZF33Mjs.log).debug("setBlockSizes abc95 maxWidth of", block.id, ":s children is ", maxWidth, maxHeight);
        for (const child of block.children)if (child.size) {
            (0, _chunkDD37ZF33Mjs.log).debug(`abc95 Setting size of children of ${block.id} id=${child.id} ${maxWidth} ${maxHeight} ${JSON.stringify(child.size)}`);
            child.size.width = maxWidth * (child.widthInColumns ?? 1) + padding * ((child.widthInColumns ?? 1) - 1);
            child.size.height = maxHeight;
            child.size.x = 0;
            child.size.y = 0;
            (0, _chunkDD37ZF33Mjs.log).debug(`abc95 updating size of ${block.id} children child:${child.id} maxWidth:${maxWidth} maxHeight:${maxHeight}`);
        }
        for (const child of block.children)setBlockSizes(child, db2, maxWidth, maxHeight);
        const columns = block.columns ?? -1;
        let numItems = 0;
        for (const child of block.children)numItems += child.widthInColumns ?? 1;
        let xSize = block.children.length;
        if (columns > 0 && columns < numItems) xSize = columns;
        const ySize = Math.ceil(numItems / xSize);
        let width = xSize * (maxWidth + padding) + padding;
        let height = ySize * (maxHeight + padding) + padding;
        if (width < siblingWidth) {
            (0, _chunkDD37ZF33Mjs.log).debug(`Detected to small siebling: abc95 ${block.id} sieblingWidth ${siblingWidth} sieblingHeight ${siblingHeight} width ${width}`);
            width = siblingWidth;
            height = siblingHeight;
            const childWidth = (siblingWidth - xSize * padding - padding) / xSize;
            const childHeight = (siblingHeight - ySize * padding - padding) / ySize;
            (0, _chunkDD37ZF33Mjs.log).debug("Size indata abc88", block.id, "childWidth", childWidth, "maxWidth", maxWidth);
            (0, _chunkDD37ZF33Mjs.log).debug("Size indata abc88", block.id, "childHeight", childHeight, "maxHeight", maxHeight);
            (0, _chunkDD37ZF33Mjs.log).debug("Size indata abc88 xSize", xSize, "padding", padding);
            for (const child of block.children)if (child.size) {
                child.size.width = childWidth;
                child.size.height = childHeight;
                child.size.x = 0;
                child.size.y = 0;
            }
        }
        (0, _chunkDD37ZF33Mjs.log).debug(`abc95 (finale calc) ${block.id} xSize ${xSize} ySize ${ySize} columns ${columns}${block.children.length} width=${Math.max(width, block.size?.width || 0)}`);
        if (width < (block?.size?.width || 0)) {
            width = block?.size?.width || 0;
            const num = columns > 0 ? Math.min(block.children.length, columns) : block.children.length;
            if (num > 0) {
                const childWidth = (width - num * padding - padding) / num;
                (0, _chunkDD37ZF33Mjs.log).debug("abc95 (growing to fit) width", block.id, width, block.size?.width, childWidth);
                for (const child of block.children)if (child.size) child.size.width = childWidth;
            }
        }
        block.size = {
            width,
            height,
            x: 0,
            y: 0
        };
    }
    (0, _chunkDD37ZF33Mjs.log).debug("setBlockSizes abc94 (done)", block.id, block?.size?.x, block?.size?.width, block?.size?.y, block?.size?.height);
}
(0, _chunkDLQEHMXDMjs.__name)(setBlockSizes, "setBlockSizes");
function layoutBlocks(block, db2) {
    (0, _chunkDD37ZF33Mjs.log).debug(`abc85 layout blocks (=>layoutBlocks) ${block.id} x: ${block?.size?.x} y: ${block?.size?.y} width: ${block?.size?.width}`);
    const columns = block.columns ?? -1;
    (0, _chunkDD37ZF33Mjs.log).debug("layoutBlocks columns abc95", block.id, "=>", columns, block);
    if (block.children && // find max width of children
    block.children.length > 0) {
        const width = block?.children[0]?.size?.width ?? 0;
        const widthOfChildren = block.children.length * width + (block.children.length - 1) * padding;
        (0, _chunkDD37ZF33Mjs.log).debug("widthOfChildren 88", widthOfChildren, "posX");
        let columnPos = 0;
        (0, _chunkDD37ZF33Mjs.log).debug("abc91 block?.size?.x", block.id, block?.size?.x);
        let startingPosX = block?.size?.x ? block?.size?.x + (-block?.size?.width / 2 || 0) : -padding;
        let rowPos = 0;
        for (const child of block.children){
            const parent = block;
            if (!child.size) continue;
            const { width: width2, height } = child.size;
            const { px, py } = calculateBlockPosition(columns, columnPos);
            if (py != rowPos) {
                rowPos = py;
                startingPosX = block?.size?.x ? block?.size?.x + (-block?.size?.width / 2 || 0) : -padding;
                (0, _chunkDD37ZF33Mjs.log).debug("New row in layout for block", block.id, " and child ", child.id, rowPos);
            }
            (0, _chunkDD37ZF33Mjs.log).debug(`abc89 layout blocks (child) id: ${child.id} Pos: ${columnPos} (px, py) ${px},${py} (${parent?.size?.x},${parent?.size?.y}) parent: ${parent.id} width: ${width2}${padding}`);
            if (parent.size) {
                const halfWidth = width2 / 2;
                child.size.x = startingPosX + padding + halfWidth;
                (0, _chunkDD37ZF33Mjs.log).debug(`abc91 layout blocks (calc) px, pyid:${child.id} startingPos=X${startingPosX} new startingPosX${child.size.x} ${halfWidth} padding=${padding} width=${width2} halfWidth=${halfWidth} => x:${child.size.x} y:${child.size.y} ${child.widthInColumns} (width * (child?.w || 1)) / 2 ${width2 * (child?.widthInColumns ?? 1) / 2}`);
                startingPosX = child.size.x + halfWidth;
                child.size.y = parent.size.y - parent.size.height / 2 + py * (height + padding) + height / 2 + padding;
                (0, _chunkDD37ZF33Mjs.log).debug(`abc88 layout blocks (calc) px, pyid:${child.id}startingPosX${startingPosX}${padding}${halfWidth}=>x:${child.size.x}y:${child.size.y}${child.widthInColumns}(width * (child?.w || 1)) / 2${width2 * (child?.widthInColumns ?? 1) / 2}`);
            }
            if (child.children) layoutBlocks(child, db2);
            columnPos += child?.widthInColumns ?? 1;
            (0, _chunkDD37ZF33Mjs.log).debug("abc88 columnsPos", child, columnPos);
        }
    }
    (0, _chunkDD37ZF33Mjs.log).debug(`layout blocks (<==layoutBlocks) ${block.id} x: ${block?.size?.x} y: ${block?.size?.y} width: ${block?.size?.width}`);
}
(0, _chunkDLQEHMXDMjs.__name)(layoutBlocks, "layoutBlocks");
function findBounds(block, { minX, minY, maxX, maxY } = {
    minX: 0,
    minY: 0,
    maxX: 0,
    maxY: 0
}) {
    if (block.size && block.id !== "root") {
        const { x, y, width, height } = block.size;
        if (x - width / 2 < minX) minX = x - width / 2;
        if (y - height / 2 < minY) minY = y - height / 2;
        if (x + width / 2 > maxX) maxX = x + width / 2;
        if (y + height / 2 > maxY) maxY = y + height / 2;
    }
    if (block.children) for (const child of block.children)({ minX, minY, maxX, maxY } = findBounds(child, {
        minX,
        minY,
        maxX,
        maxY
    }));
    return {
        minX,
        minY,
        maxX,
        maxY
    };
}
(0, _chunkDLQEHMXDMjs.__name)(findBounds, "findBounds");
function layout(db2) {
    const root = db2.getBlock("root");
    if (!root) return;
    setBlockSizes(root, db2, 0, 0);
    layoutBlocks(root, db2);
    (0, _chunkDD37ZF33Mjs.log).debug("getBlocks", JSON.stringify(root, null, 2));
    const { minX, minY, maxX, maxY } = findBounds(root);
    const height = maxY - minY;
    const width = maxX - minX;
    return {
        x: minX,
        y: minY,
        width,
        height
    };
}
(0, _chunkDLQEHMXDMjs.__name)(layout, "layout");
// src/diagrams/block/renderHelpers.ts
function getNodeFromBlock(block, db2, positioned = false) {
    const vertex = block;
    let classStr = "default";
    if ((vertex?.classes?.length || 0) > 0) classStr = (vertex?.classes ?? []).join(" ");
    classStr = classStr + " flowchart-label";
    let radius = 0;
    let shape = "";
    let padding2;
    switch(vertex.type){
        case "round":
            radius = 5;
            shape = "rect";
            break;
        case "composite":
            radius = 0;
            shape = "composite";
            padding2 = 0;
            break;
        case "square":
            shape = "rect";
            break;
        case "diamond":
            shape = "question";
            break;
        case "hexagon":
            shape = "hexagon";
            break;
        case "block_arrow":
            shape = "block_arrow";
            break;
        case "odd":
            shape = "rect_left_inv_arrow";
            break;
        case "lean_right":
            shape = "lean_right";
            break;
        case "lean_left":
            shape = "lean_left";
            break;
        case "trapezoid":
            shape = "trapezoid";
            break;
        case "inv_trapezoid":
            shape = "inv_trapezoid";
            break;
        case "rect_left_inv_arrow":
            shape = "rect_left_inv_arrow";
            break;
        case "circle":
            shape = "circle";
            break;
        case "ellipse":
            shape = "ellipse";
            break;
        case "stadium":
            shape = "stadium";
            break;
        case "subroutine":
            shape = "subroutine";
            break;
        case "cylinder":
            shape = "cylinder";
            break;
        case "group":
            shape = "rect";
            break;
        case "doublecircle":
            shape = "doublecircle";
            break;
        default:
            shape = "rect";
    }
    const styles = (0, _chunkI7ZFS43CMjs.getStylesFromArray)(vertex?.styles ?? []);
    const vertexText = vertex.label;
    const bounds = vertex.size ?? {
        width: 0,
        height: 0,
        x: 0,
        y: 0
    };
    const node = {
        labelStyle: styles.labelStyle,
        shape,
        labelText: vertexText,
        rx: radius,
        ry: radius,
        class: classStr,
        style: styles.style,
        id: vertex.id,
        directions: vertex.directions,
        width: bounds.width,
        height: bounds.height,
        x: bounds.x,
        y: bounds.y,
        positioned,
        intersect: void 0,
        type: vertex.type,
        padding: padding2 ?? (0, _chunkDD37ZF33Mjs.getConfig)()?.block?.padding ?? 0
    };
    return node;
}
(0, _chunkDLQEHMXDMjs.__name)(getNodeFromBlock, "getNodeFromBlock");
async function calculateBlockSize(elem, block, db2) {
    const node = getNodeFromBlock(block, db2, false);
    if (node.type === "group") return;
    const nodeEl = await (0, _chunkHKQCUR3CMjs.insertNode)(elem, node);
    const boundingBox = nodeEl.node().getBBox();
    const obj = db2.getBlock(node.id);
    obj.size = {
        width: boundingBox.width,
        height: boundingBox.height,
        x: 0,
        y: 0,
        node: nodeEl
    };
    db2.setBlock(obj);
    nodeEl.remove();
}
(0, _chunkDLQEHMXDMjs.__name)(calculateBlockSize, "calculateBlockSize");
async function insertBlockPositioned(elem, block, db2) {
    const node = getNodeFromBlock(block, db2, true);
    const obj = db2.getBlock(node.id);
    if (obj.type !== "space") {
        await (0, _chunkHKQCUR3CMjs.insertNode)(elem, node);
        block.intersect = node?.intersect;
        (0, _chunkHKQCUR3CMjs.positionNode)(node);
    }
}
(0, _chunkDLQEHMXDMjs.__name)(insertBlockPositioned, "insertBlockPositioned");
async function performOperations(elem, blocks2, db2, operation) {
    for (const block of blocks2){
        await operation(elem, block, db2);
        if (block.children) await performOperations(elem, block.children, db2, operation);
    }
}
(0, _chunkDLQEHMXDMjs.__name)(performOperations, "performOperations");
async function calculateBlockSizes(elem, blocks2, db2) {
    await performOperations(elem, blocks2, db2, calculateBlockSize);
}
(0, _chunkDLQEHMXDMjs.__name)(calculateBlockSizes, "calculateBlockSizes");
async function insertBlocks(elem, blocks2, db2) {
    await performOperations(elem, blocks2, db2, insertBlockPositioned);
}
(0, _chunkDLQEHMXDMjs.__name)(insertBlocks, "insertBlocks");
async function insertEdges(elem, edges, blocks2, db2, id) {
    const g = new (0, _chunkULVYQCHCMjs.Graph)({
        multigraph: true,
        compound: true
    });
    g.setGraph({
        rankdir: "TB",
        nodesep: 10,
        ranksep: 10,
        marginx: 8,
        marginy: 8
    });
    for (const block of blocks2)if (block.size) g.setNode(block.id, {
        width: block.size.width,
        height: block.size.height,
        intersect: block.intersect
    });
    for (const edge of edges)if (edge.start && edge.end) {
        const startBlock = db2.getBlock(edge.start);
        const endBlock = db2.getBlock(edge.end);
        if (startBlock?.size && endBlock?.size) {
            const start = startBlock.size;
            const end = endBlock.size;
            const points = [
                {
                    x: start.x,
                    y: start.y
                },
                {
                    x: start.x + (end.x - start.x) / 2,
                    y: start.y + (end.y - start.y) / 2
                },
                {
                    x: end.x,
                    y: end.y
                }
            ];
            (0, _chunkBAJGW65CMjs.insertEdge)(elem, {
                v: edge.start,
                w: edge.end,
                name: edge.id
            }, {
                ...edge,
                arrowTypeEnd: edge.arrowTypeEnd,
                arrowTypeStart: edge.arrowTypeStart,
                points,
                classes: "edge-thickness-normal edge-pattern-solid flowchart-link LS-a1 LE-b1"
            }, void 0, "block", g, id);
            if (edge.label) {
                await (0, _chunkBAJGW65CMjs.insertEdgeLabel)(elem, {
                    ...edge,
                    label: edge.label,
                    labelStyle: "stroke: #333; stroke-width: 1.5px;fill:none;",
                    arrowTypeEnd: edge.arrowTypeEnd,
                    arrowTypeStart: edge.arrowTypeStart,
                    points,
                    classes: "edge-thickness-normal edge-pattern-solid flowchart-link LS-a1 LE-b1"
                });
                (0, _chunkBAJGW65CMjs.positionEdgeLabel)({
                    ...edge,
                    x: points[1].x,
                    y: points[1].y
                }, {
                    originalPath: points
                });
            }
        }
    }
}
(0, _chunkDLQEHMXDMjs.__name)(insertEdges, "insertEdges");
// src/diagrams/block/blockRenderer.ts
var getClasses2 = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(text, diagObj) {
    return diagObj.db.getClasses();
}, "getClasses");
var draw = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(async function(text, id, _version, diagObj) {
    const { securityLevel, block: conf } = (0, _chunkDD37ZF33Mjs.getConfig)();
    const db2 = diagObj.db;
    let sandboxElement;
    if (securityLevel === "sandbox") sandboxElement = (0, _chunkDD37ZF33Mjs.select_default)("#i" + id);
    const root = securityLevel === "sandbox" ? (0, _chunkDD37ZF33Mjs.select_default)(sandboxElement.nodes()[0].contentDocument.body) : (0, _chunkDD37ZF33Mjs.select_default)("body");
    const svg = securityLevel === "sandbox" ? root.select(`[id="${id}"]`) : (0, _chunkDD37ZF33Mjs.select_default)(`[id="${id}"]`);
    const markers = [
        "point",
        "circle",
        "cross"
    ];
    (0, _chunkBAJGW65CMjs.markers_default)(svg, markers, diagObj.type, id);
    const bl = db2.getBlocks();
    const blArr = db2.getBlocksFlat();
    const edges = db2.getEdges();
    const nodes = svg.insert("g").attr("class", "block");
    await calculateBlockSizes(nodes, bl, db2);
    const bounds = layout(db2);
    await insertBlocks(nodes, bl, db2);
    await insertEdges(nodes, edges, blArr, db2, id);
    if (bounds) {
        const bounds2 = bounds;
        const magicFactor = Math.max(1, Math.round(0.125 * (bounds2.width / bounds2.height)));
        const height = bounds2.height + magicFactor + 10;
        const width = bounds2.width + 10;
        const { useMaxWidth } = conf;
        (0, _chunkDD37ZF33Mjs.configureSvgSize)(svg, height, width, !!useMaxWidth);
        (0, _chunkDD37ZF33Mjs.log).debug("Here Bounds", bounds, bounds2);
        svg.attr("viewBox", `${bounds2.x - 5} ${bounds2.y - 5} ${bounds2.width + 10} ${bounds2.height + 10}`);
    }
}, "draw");
var blockRenderer_default = {
    draw,
    getClasses: getClasses2
};
// src/diagrams/block/blockDiagram.ts
var diagram = {
    parser: block_default,
    db: blockDB_default,
    renderer: blockRenderer_default,
    styles: styles_default
};

},{"./chunk-BAJGW65C.mjs":"gf7Uq","./chunk-HKQCUR3C.mjs":"fcjSH","./chunk-KW7S66XI.mjs":"98JMR","./chunk-YP6PVJQ3.mjs":"21NKC","./chunk-ULVYQCHC.mjs":"h2Yj3","./chunk-I7ZFS43C.mjs":"huUtc","./chunk-GKOISANM.mjs":"5yZtl","./chunk-DD37ZF33.mjs":"f4pI5","./chunk-TZBO7MLI.mjs":"fY3tK","./chunk-GRZAG2UZ.mjs":"d1pnj","./chunk-HD3LK5B5.mjs":"lpo2u","./chunk-DLQEHMXD.mjs":"h5YRf","@parcel/transformer-js/src/esmodule-helpers.js":"1cdyk"}],"h2Yj3":[function(require,module,exports,__globalThis) {
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

},{"./chunk-TZBO7MLI.mjs":"fY3tK","./chunk-GRZAG2UZ.mjs":"d1pnj","./chunk-HD3LK5B5.mjs":"lpo2u","./chunk-DLQEHMXD.mjs":"h5YRf","@parcel/transformer-js/src/esmodule-helpers.js":"1cdyk"}]},["9uaM2"], null, "parcelRequire6955", {})

//# sourceMappingURL=blockDiagram-PSHTR7TV.f557bb24.js.map
