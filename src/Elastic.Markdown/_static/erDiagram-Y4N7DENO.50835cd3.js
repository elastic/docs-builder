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
})({"hdXnv":[function(require,module,exports,__globalThis) {
var global = arguments[3];
var HMR_HOST = null;
var HMR_PORT = 1234;
var HMR_SERVER_PORT = 1234;
var HMR_SECURE = false;
var HMR_ENV_HASH = "febfef3c5b467b16";
var HMR_USE_SSE = false;
module.bundle.HMR_BUNDLE_ID = "f51df51e50835cd3";
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

},{}],"163as":[function(require,module,exports,__globalThis) {
var parcelHelpers = require("@parcel/transformer-js/src/esmodule-helpers.js");
parcelHelpers.defineInteropFlag(exports);
parcelHelpers.export(exports, "diagram", ()=>ur);
var _chunkBOP2KBYHMjs = require("./chunk-BOP2KBYH.mjs");
var _chunk6XGRHI2AMjs = require("./chunk-6XGRHI2A.mjs");
var _chunkAC3VT7B7Mjs = require("./chunk-AC3VT7B7.mjs");
var _chunkTI4EEUUGMjs = require("./chunk-TI4EEUUG.mjs");
var _chunkNQURTBEVMjs = require("./chunk-NQURTBEV.mjs");
var _chunkBKDDFIKNMjs = require("./chunk-BKDDFIKN.mjs");
var _chunkYPUTD6PBMjs = require("./chunk-YPUTD6PB.mjs");
var _chunk6BY5RJGCMjs = require("./chunk-6BY5RJGC.mjs");
var _chunkGTKDMUJJMjs = require("./chunk-GTKDMUJJ.mjs");
var nt = function() {
    var t = (0, _chunkGTKDMUJJMjs.a)(function(I, i, s, c) {
        for(s = s || {}, c = I.length; c--; s[I[c]] = i);
        return s;
    }, "o"), e = [
        6,
        8,
        10,
        20,
        22,
        24,
        26,
        27,
        28
    ], r = [
        1,
        10
    ], u = [
        1,
        11
    ], h = [
        1,
        12
    ], _ = [
        1,
        13
    ], p = [
        1,
        14
    ], l = [
        1,
        15
    ], d = [
        1,
        21
    ], k = [
        1,
        22
    ], E = [
        1,
        23
    ], g = [
        1,
        24
    ], x = [
        1,
        25
    ], y = [
        6,
        8,
        10,
        13,
        15,
        18,
        19,
        20,
        22,
        24,
        26,
        27,
        28,
        41,
        42,
        43,
        44,
        45
    ], T = [
        1,
        34
    ], D = [
        27,
        28,
        46,
        47
    ], W = [
        41,
        42,
        43,
        44,
        45
    ], U = [
        17,
        34
    ], Z = [
        1,
        54
    ], A = [
        1,
        53
    ], S = [
        17,
        34,
        36,
        38
    ], R = {
        trace: (0, _chunkGTKDMUJJMjs.a)(function() {}, "trace"),
        yy: {},
        symbols_: {
            error: 2,
            start: 3,
            ER_DIAGRAM: 4,
            document: 5,
            EOF: 6,
            line: 7,
            SPACE: 8,
            statement: 9,
            NEWLINE: 10,
            entityName: 11,
            relSpec: 12,
            ":": 13,
            role: 14,
            BLOCK_START: 15,
            attributes: 16,
            BLOCK_STOP: 17,
            SQS: 18,
            SQE: 19,
            title: 20,
            title_value: 21,
            acc_title: 22,
            acc_title_value: 23,
            acc_descr: 24,
            acc_descr_value: 25,
            acc_descr_multiline_value: 26,
            ALPHANUM: 27,
            ENTITY_NAME: 28,
            attribute: 29,
            attributeType: 30,
            attributeName: 31,
            attributeKeyTypeList: 32,
            attributeComment: 33,
            ATTRIBUTE_WORD: 34,
            attributeKeyType: 35,
            COMMA: 36,
            ATTRIBUTE_KEY: 37,
            COMMENT: 38,
            cardinality: 39,
            relType: 40,
            ZERO_OR_ONE: 41,
            ZERO_OR_MORE: 42,
            ONE_OR_MORE: 43,
            ONLY_ONE: 44,
            MD_PARENT: 45,
            NON_IDENTIFYING: 46,
            IDENTIFYING: 47,
            WORD: 48,
            $accept: 0,
            $end: 1
        },
        terminals_: {
            2: "error",
            4: "ER_DIAGRAM",
            6: "EOF",
            8: "SPACE",
            10: "NEWLINE",
            13: ":",
            15: "BLOCK_START",
            17: "BLOCK_STOP",
            18: "SQS",
            19: "SQE",
            20: "title",
            21: "title_value",
            22: "acc_title",
            23: "acc_title_value",
            24: "acc_descr",
            25: "acc_descr_value",
            26: "acc_descr_multiline_value",
            27: "ALPHANUM",
            28: "ENTITY_NAME",
            34: "ATTRIBUTE_WORD",
            36: "COMMA",
            37: "ATTRIBUTE_KEY",
            38: "COMMENT",
            41: "ZERO_OR_ONE",
            42: "ZERO_OR_MORE",
            43: "ONE_OR_MORE",
            44: "ONLY_ONE",
            45: "MD_PARENT",
            46: "NON_IDENTIFYING",
            47: "IDENTIFYING",
            48: "WORD"
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
                5
            ],
            [
                9,
                4
            ],
            [
                9,
                3
            ],
            [
                9,
                1
            ],
            [
                9,
                7
            ],
            [
                9,
                6
            ],
            [
                9,
                4
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
                2
            ],
            [
                9,
                1
            ],
            [
                11,
                1
            ],
            [
                11,
                1
            ],
            [
                16,
                1
            ],
            [
                16,
                2
            ],
            [
                29,
                2
            ],
            [
                29,
                3
            ],
            [
                29,
                3
            ],
            [
                29,
                4
            ],
            [
                30,
                1
            ],
            [
                31,
                1
            ],
            [
                32,
                1
            ],
            [
                32,
                3
            ],
            [
                35,
                1
            ],
            [
                33,
                1
            ],
            [
                12,
                3
            ],
            [
                39,
                1
            ],
            [
                39,
                1
            ],
            [
                39,
                1
            ],
            [
                39,
                1
            ],
            [
                39,
                1
            ],
            [
                40,
                1
            ],
            [
                40,
                1
            ],
            [
                14,
                1
            ],
            [
                14,
                1
            ],
            [
                14,
                1
            ]
        ],
        performAction: (0, _chunkGTKDMUJJMjs.a)(function(i, s, c, f, m, a, X) {
            var o = a.length - 1;
            switch(m){
                case 1:
                    break;
                case 2:
                    this.$ = [];
                    break;
                case 3:
                    a[o - 1].push(a[o]), this.$ = a[o - 1];
                    break;
                case 4:
                case 5:
                    this.$ = a[o];
                    break;
                case 6:
                case 7:
                    this.$ = [];
                    break;
                case 8:
                    f.addEntity(a[o - 4]), f.addEntity(a[o - 2]), f.addRelationship(a[o - 4], a[o], a[o - 2], a[o - 3]);
                    break;
                case 9:
                    f.addEntity(a[o - 3]), f.addAttributes(a[o - 3], a[o - 1]);
                    break;
                case 10:
                    f.addEntity(a[o - 2]);
                    break;
                case 11:
                    f.addEntity(a[o]);
                    break;
                case 12:
                    f.addEntity(a[o - 6], a[o - 4]), f.addAttributes(a[o - 6], a[o - 1]);
                    break;
                case 13:
                    f.addEntity(a[o - 5], a[o - 3]);
                    break;
                case 14:
                    f.addEntity(a[o - 3], a[o - 1]);
                    break;
                case 15:
                case 16:
                    this.$ = a[o].trim(), f.setAccTitle(this.$);
                    break;
                case 17:
                case 18:
                    this.$ = a[o].trim(), f.setAccDescription(this.$);
                    break;
                case 19:
                case 43:
                    this.$ = a[o];
                    break;
                case 20:
                case 41:
                case 42:
                    this.$ = a[o].replace(/"/g, "");
                    break;
                case 21:
                case 29:
                    this.$ = [
                        a[o]
                    ];
                    break;
                case 22:
                    a[o].push(a[o - 1]), this.$ = a[o];
                    break;
                case 23:
                    this.$ = {
                        attributeType: a[o - 1],
                        attributeName: a[o]
                    };
                    break;
                case 24:
                    this.$ = {
                        attributeType: a[o - 2],
                        attributeName: a[o - 1],
                        attributeKeyTypeList: a[o]
                    };
                    break;
                case 25:
                    this.$ = {
                        attributeType: a[o - 2],
                        attributeName: a[o - 1],
                        attributeComment: a[o]
                    };
                    break;
                case 26:
                    this.$ = {
                        attributeType: a[o - 3],
                        attributeName: a[o - 2],
                        attributeKeyTypeList: a[o - 1],
                        attributeComment: a[o]
                    };
                    break;
                case 27:
                case 28:
                case 31:
                    this.$ = a[o];
                    break;
                case 30:
                    a[o - 2].push(a[o]), this.$ = a[o - 2];
                    break;
                case 32:
                    this.$ = a[o].replace(/"/g, "");
                    break;
                case 33:
                    this.$ = {
                        cardA: a[o],
                        relType: a[o - 1],
                        cardB: a[o - 2]
                    };
                    break;
                case 34:
                    this.$ = f.Cardinality.ZERO_OR_ONE;
                    break;
                case 35:
                    this.$ = f.Cardinality.ZERO_OR_MORE;
                    break;
                case 36:
                    this.$ = f.Cardinality.ONE_OR_MORE;
                    break;
                case 37:
                    this.$ = f.Cardinality.ONLY_ONE;
                    break;
                case 38:
                    this.$ = f.Cardinality.MD_PARENT;
                    break;
                case 39:
                    this.$ = f.Identification.NON_IDENTIFYING;
                    break;
                case 40:
                    this.$ = f.Identification.IDENTIFYING;
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
                11: 9,
                20: r,
                22: u,
                24: h,
                26: _,
                27: p,
                28: l
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
                9: 16,
                11: 9,
                20: r,
                22: u,
                24: h,
                26: _,
                27: p,
                28: l
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
                11
            ], {
                12: 17,
                39: 20,
                15: [
                    1,
                    18
                ],
                18: [
                    1,
                    19
                ],
                41: d,
                42: k,
                43: E,
                44: g,
                45: x
            }),
            {
                21: [
                    1,
                    26
                ]
            },
            {
                23: [
                    1,
                    27
                ]
            },
            {
                25: [
                    1,
                    28
                ]
            },
            t(e, [
                2,
                18
            ]),
            t(y, [
                2,
                19
            ]),
            t(y, [
                2,
                20
            ]),
            t(e, [
                2,
                4
            ]),
            {
                11: 29,
                27: p,
                28: l
            },
            {
                16: 30,
                17: [
                    1,
                    31
                ],
                29: 32,
                30: 33,
                34: T
            },
            {
                11: 35,
                27: p,
                28: l
            },
            {
                40: 36,
                46: [
                    1,
                    37
                ],
                47: [
                    1,
                    38
                ]
            },
            t(D, [
                2,
                34
            ]),
            t(D, [
                2,
                35
            ]),
            t(D, [
                2,
                36
            ]),
            t(D, [
                2,
                37
            ]),
            t(D, [
                2,
                38
            ]),
            t(e, [
                2,
                15
            ]),
            t(e, [
                2,
                16
            ]),
            t(e, [
                2,
                17
            ]),
            {
                13: [
                    1,
                    39
                ]
            },
            {
                17: [
                    1,
                    40
                ]
            },
            t(e, [
                2,
                10
            ]),
            {
                16: 41,
                17: [
                    2,
                    21
                ],
                29: 32,
                30: 33,
                34: T
            },
            {
                31: 42,
                34: [
                    1,
                    43
                ]
            },
            {
                34: [
                    2,
                    27
                ]
            },
            {
                19: [
                    1,
                    44
                ]
            },
            {
                39: 45,
                41: d,
                42: k,
                43: E,
                44: g,
                45: x
            },
            t(W, [
                2,
                39
            ]),
            t(W, [
                2,
                40
            ]),
            {
                14: 46,
                27: [
                    1,
                    49
                ],
                28: [
                    1,
                    48
                ],
                48: [
                    1,
                    47
                ]
            },
            t(e, [
                2,
                9
            ]),
            {
                17: [
                    2,
                    22
                ]
            },
            t(U, [
                2,
                23
            ], {
                32: 50,
                33: 51,
                35: 52,
                37: Z,
                38: A
            }),
            t([
                17,
                34,
                37,
                38
            ], [
                2,
                28
            ]),
            t(e, [
                2,
                14
            ], {
                15: [
                    1,
                    55
                ]
            }),
            t([
                27,
                28
            ], [
                2,
                33
            ]),
            t(e, [
                2,
                8
            ]),
            t(e, [
                2,
                41
            ]),
            t(e, [
                2,
                42
            ]),
            t(e, [
                2,
                43
            ]),
            t(U, [
                2,
                24
            ], {
                33: 56,
                36: [
                    1,
                    57
                ],
                38: A
            }),
            t(U, [
                2,
                25
            ]),
            t(S, [
                2,
                29
            ]),
            t(U, [
                2,
                32
            ]),
            t(S, [
                2,
                31
            ]),
            {
                16: 58,
                17: [
                    1,
                    59
                ],
                29: 32,
                30: 33,
                34: T
            },
            t(U, [
                2,
                26
            ]),
            {
                35: 60,
                37: Z
            },
            {
                17: [
                    1,
                    61
                ]
            },
            t(e, [
                2,
                13
            ]),
            t(S, [
                2,
                30
            ]),
            t(e, [
                2,
                12
            ])
        ],
        defaultActions: {
            34: [
                2,
                27
            ],
            41: [
                2,
                22
            ]
        },
        parseError: (0, _chunkGTKDMUJJMjs.a)(function(i, s) {
            if (s.recoverable) this.trace(i);
            else {
                var c = new Error(i);
                throw c.hash = s, c;
            }
        }, "parseError"),
        parse: (0, _chunkGTKDMUJJMjs.a)(function(i) {
            var s = this, c = [
                0
            ], f = [], m = [
                null
            ], a = [], X = this.table, o = "", q = 0, ht = 0, dt = 0, Wt = 2, ft = 1, Ut = a.slice.call(arguments, 1), N = Object.create(this.lexer), H = {
                yy: {}
            };
            for(var tt in this.yy)Object.prototype.hasOwnProperty.call(this.yy, tt) && (H.yy[tt] = this.yy[tt]);
            N.setInput(i, H.yy), H.yy.lexer = N, H.yy.parser = this, typeof N.yylloc > "u" && (N.yylloc = {});
            var et = N.yylloc;
            a.push(et);
            var Ht = N.options && N.options.ranges;
            typeof H.yy.parseError == "function" ? this.parseError = H.yy.parseError : this.parseError = Object.getPrototypeOf(this).parseError;
            function ke(v) {
                c.length = c.length - 2 * v, m.length = m.length - v, a.length = a.length - v;
            }
            (0, _chunkGTKDMUJJMjs.a)(ke, "popStack");
            function zt() {
                var v;
                return v = f.pop() || N.lex() || ft, typeof v != "number" && (v instanceof Array && (f = v, v = f.pop()), v = s.symbols_[v] || v), v;
            }
            (0, _chunkGTKDMUJJMjs.a)(zt, "lex");
            for(var w, rt, z, L, Ee, at, G = {}, J, F, ut, j;;){
                if (z = c[c.length - 1], this.defaultActions[z] ? L = this.defaultActions[z] : ((w === null || typeof w > "u") && (w = zt()), L = X[z] && X[z][w]), typeof L > "u" || !L.length || !L[0]) {
                    var it = "";
                    j = [];
                    for(J in X[z])this.terminals_[J] && J > Wt && j.push("'" + this.terminals_[J] + "'");
                    N.showPosition ? it = "Parse error on line " + (q + 1) + `:
` + N.showPosition() + `
Expecting ` + j.join(", ") + ", got '" + (this.terminals_[w] || w) + "'" : it = "Parse error on line " + (q + 1) + ": Unexpected " + (w == ft ? "end of input" : "'" + (this.terminals_[w] || w) + "'"), this.parseError(it, {
                        text: N.match,
                        token: this.terminals_[w] || w,
                        line: N.yylineno,
                        loc: et,
                        expected: j
                    });
                }
                if (L[0] instanceof Array && L.length > 1) throw new Error("Parse Error: multiple actions possible at state: " + z + ", token: " + w);
                switch(L[0]){
                    case 1:
                        c.push(w), m.push(N.yytext), a.push(N.yylloc), c.push(L[1]), w = null, rt ? (w = rt, rt = null) : (ht = N.yyleng, o = N.yytext, q = N.yylineno, et = N.yylloc, dt > 0 && dt--);
                        break;
                    case 2:
                        if (F = this.productions_[L[1]][1], G.$ = m[m.length - F], G._$ = {
                            first_line: a[a.length - (F || 1)].first_line,
                            last_line: a[a.length - 1].last_line,
                            first_column: a[a.length - (F || 1)].first_column,
                            last_column: a[a.length - 1].last_column
                        }, Ht && (G._$.range = [
                            a[a.length - (F || 1)].range[0],
                            a[a.length - 1].range[1]
                        ]), at = this.performAction.apply(G, [
                            o,
                            ht,
                            q,
                            H.yy,
                            L[1],
                            m,
                            a
                        ].concat(Ut)), typeof at < "u") return at;
                        F && (c = c.slice(0, -1 * F * 2), m = m.slice(0, -1 * F), a = a.slice(0, -1 * F)), c.push(this.productions_[L[1]][0]), m.push(G.$), a.push(G._$), ut = X[c[c.length - 2]][c[c.length - 1]], c.push(ut);
                        break;
                    case 3:
                        return !0;
                }
            }
            return !0;
        }, "parse")
    }, O = function() {
        var I = {
            EOF: 1,
            parseError: (0, _chunkGTKDMUJJMjs.a)(function(s, c) {
                if (this.yy.parser) this.yy.parser.parseError(s, c);
                else throw new Error(s);
            }, "parseError"),
            setInput: (0, _chunkGTKDMUJJMjs.a)(function(i, s) {
                return this.yy = s || this.yy || {}, this._input = i, this._more = this._backtrack = this.done = !1, this.yylineno = this.yyleng = 0, this.yytext = this.matched = this.match = "", this.conditionStack = [
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
                var s = i.match(/(?:\r\n?|\n).*/g);
                return s ? (this.yylineno++, this.yylloc.last_line++) : this.yylloc.last_column++, this.options.ranges && this.yylloc.range[1]++, this._input = this._input.slice(1), i;
            }, "input"),
            unput: (0, _chunkGTKDMUJJMjs.a)(function(i) {
                var s = i.length, c = i.split(/(?:\r\n?|\n)/g);
                this._input = i + this._input, this.yytext = this.yytext.substr(0, this.yytext.length - s), this.offset -= s;
                var f = this.match.split(/(?:\r\n?|\n)/g);
                this.match = this.match.substr(0, this.match.length - 1), this.matched = this.matched.substr(0, this.matched.length - 1), c.length - 1 && (this.yylineno -= c.length - 1);
                var m = this.yylloc.range;
                return this.yylloc = {
                    first_line: this.yylloc.first_line,
                    last_line: this.yylineno + 1,
                    first_column: this.yylloc.first_column,
                    last_column: c ? (c.length === f.length ? this.yylloc.first_column : 0) + f[f.length - c.length].length - c[0].length : this.yylloc.first_column - s
                }, this.options.ranges && (this.yylloc.range = [
                    m[0],
                    m[0] + this.yyleng - s
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
                var i = this.pastInput(), s = new Array(i.length + 1).join("-");
                return i + this.upcomingInput() + `
` + s + "^";
            }, "showPosition"),
            test_match: (0, _chunkGTKDMUJJMjs.a)(function(i, s) {
                var c, f, m;
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
                }, this.options.ranges && (m.yylloc.range = this.yylloc.range.slice(0))), f = i[0].match(/(?:\r\n?|\n).*/g), f && (this.yylineno += f.length), this.yylloc = {
                    first_line: this.yylloc.last_line,
                    last_line: this.yylineno + 1,
                    first_column: this.yylloc.last_column,
                    last_column: f ? f[f.length - 1].length - f[f.length - 1].match(/\r?\n?/)[0].length : this.yylloc.last_column + i[0].length
                }, this.yytext += i[0], this.match += i[0], this.matches = i, this.yyleng = this.yytext.length, this.options.ranges && (this.yylloc.range = [
                    this.offset,
                    this.offset += this.yyleng
                ]), this._more = !1, this._backtrack = !1, this._input = this._input.slice(i[0].length), this.matched += i[0], c = this.performAction.call(this, this.yy, this, s, this.conditionStack[this.conditionStack.length - 1]), this.done && this._input && (this.done = !1), c) return c;
                if (this._backtrack) {
                    for(var a in m)this[a] = m[a];
                    return !1;
                }
                return !1;
            }, "test_match"),
            next: (0, _chunkGTKDMUJJMjs.a)(function() {
                if (this.done) return this.EOF;
                this._input || (this.done = !0);
                var i, s, c, f;
                this._more || (this.yytext = "", this.match = "");
                for(var m = this._currentRules(), a = 0; a < m.length; a++)if (c = this._input.match(this.rules[m[a]]), c && (!s || c[0].length > s[0].length)) {
                    if (s = c, f = a, this.options.backtrack_lexer) {
                        if (i = this.test_match(c, m[a]), i !== !1) return i;
                        if (this._backtrack) {
                            s = !1;
                            continue;
                        } else return !1;
                    } else if (!this.options.flex) break;
                }
                return s ? (i = this.test_match(s, m[f]), i !== !1 ? i : !1) : this._input === "" ? this.EOF : this.parseError("Lexical error on line " + (this.yylineno + 1) + `. Unrecognized text.
` + this.showPosition(), {
                    text: "",
                    token: null,
                    line: this.yylineno
                });
            }, "next"),
            lex: (0, _chunkGTKDMUJJMjs.a)(function() {
                var s = this.next();
                return s || this.lex();
            }, "lex"),
            begin: (0, _chunkGTKDMUJJMjs.a)(function(s) {
                this.conditionStack.push(s);
            }, "begin"),
            popState: (0, _chunkGTKDMUJJMjs.a)(function() {
                var s = this.conditionStack.length - 1;
                return s > 0 ? this.conditionStack.pop() : this.conditionStack[0];
            }, "popState"),
            _currentRules: (0, _chunkGTKDMUJJMjs.a)(function() {
                return this.conditionStack.length && this.conditionStack[this.conditionStack.length - 1] ? this.conditions[this.conditionStack[this.conditionStack.length - 1]].rules : this.conditions.INITIAL.rules;
            }, "_currentRules"),
            topState: (0, _chunkGTKDMUJJMjs.a)(function(s) {
                return s = this.conditionStack.length - 1 - Math.abs(s || 0), s >= 0 ? this.conditionStack[s] : "INITIAL";
            }, "topState"),
            pushState: (0, _chunkGTKDMUJJMjs.a)(function(s) {
                this.begin(s);
            }, "pushState"),
            stateStackSize: (0, _chunkGTKDMUJJMjs.a)(function() {
                return this.conditionStack.length;
            }, "stateStackSize"),
            options: {
                "case-insensitive": !0
            },
            performAction: (0, _chunkGTKDMUJJMjs.a)(function(s, c, f, m) {
                var a = m;
                switch(f){
                    case 0:
                        return this.begin("acc_title"), 22;
                    case 1:
                        return this.popState(), "acc_title_value";
                    case 2:
                        return this.begin("acc_descr"), 24;
                    case 3:
                        return this.popState(), "acc_descr_value";
                    case 4:
                        this.begin("acc_descr_multiline");
                        break;
                    case 5:
                        this.popState();
                        break;
                    case 6:
                        return "acc_descr_multiline_value";
                    case 7:
                        return 10;
                    case 8:
                        break;
                    case 9:
                        return 8;
                    case 10:
                        return 28;
                    case 11:
                        return 48;
                    case 12:
                        return 4;
                    case 13:
                        return this.begin("block"), 15;
                    case 14:
                        return 36;
                    case 15:
                        break;
                    case 16:
                        return 37;
                    case 17:
                        return 34;
                    case 18:
                        return 34;
                    case 19:
                        return 38;
                    case 20:
                        break;
                    case 21:
                        return this.popState(), 17;
                    case 22:
                        return c.yytext[0];
                    case 23:
                        return 18;
                    case 24:
                        return 19;
                    case 25:
                        return 41;
                    case 26:
                        return 43;
                    case 27:
                        return 43;
                    case 28:
                        return 43;
                    case 29:
                        return 41;
                    case 30:
                        return 41;
                    case 31:
                        return 42;
                    case 32:
                        return 42;
                    case 33:
                        return 42;
                    case 34:
                        return 42;
                    case 35:
                        return 42;
                    case 36:
                        return 43;
                    case 37:
                        return 42;
                    case 38:
                        return 43;
                    case 39:
                        return 44;
                    case 40:
                        return 44;
                    case 41:
                        return 44;
                    case 42:
                        return 44;
                    case 43:
                        return 41;
                    case 44:
                        return 42;
                    case 45:
                        return 43;
                    case 46:
                        return 45;
                    case 47:
                        return 46;
                    case 48:
                        return 47;
                    case 49:
                        return 47;
                    case 50:
                        return 46;
                    case 51:
                        return 46;
                    case 52:
                        return 46;
                    case 53:
                        return 27;
                    case 54:
                        return c.yytext[0];
                    case 55:
                        return 6;
                }
            }, "anonymous"),
            rules: [
                /^(?:accTitle\s*:\s*)/i,
                /^(?:(?!\n||)*[^\n]*)/i,
                /^(?:accDescr\s*:\s*)/i,
                /^(?:(?!\n||)*[^\n]*)/i,
                /^(?:accDescr\s*\{\s*)/i,
                /^(?:[\}])/i,
                /^(?:[^\}]*)/i,
                /^(?:[\n]+)/i,
                /^(?:\s+)/i,
                /^(?:[\s]+)/i,
                /^(?:"[^"%\r\n\v\b\\]+")/i,
                /^(?:"[^"]*")/i,
                /^(?:erDiagram\b)/i,
                /^(?:\{)/i,
                /^(?:,)/i,
                /^(?:\s+)/i,
                /^(?:\b((?:PK)|(?:FK)|(?:UK))\b)/i,
                /^(?:(.*?)[~](.*?)*[~])/i,
                /^(?:[\*A-Za-z_][A-Za-z0-9\-_\[\]\(\)]*)/i,
                /^(?:"[^"]*")/i,
                /^(?:[\n]+)/i,
                /^(?:\})/i,
                /^(?:.)/i,
                /^(?:\[)/i,
                /^(?:\])/i,
                /^(?:one or zero\b)/i,
                /^(?:one or more\b)/i,
                /^(?:one or many\b)/i,
                /^(?:1\+)/i,
                /^(?:\|o\b)/i,
                /^(?:zero or one\b)/i,
                /^(?:zero or more\b)/i,
                /^(?:zero or many\b)/i,
                /^(?:0\+)/i,
                /^(?:\}o\b)/i,
                /^(?:many\(0\))/i,
                /^(?:many\(1\))/i,
                /^(?:many\b)/i,
                /^(?:\}\|)/i,
                /^(?:one\b)/i,
                /^(?:only one\b)/i,
                /^(?:1\b)/i,
                /^(?:\|\|)/i,
                /^(?:o\|)/i,
                /^(?:o\{)/i,
                /^(?:\|\{)/i,
                /^(?:\s*u\b)/i,
                /^(?:\.\.)/i,
                /^(?:--)/i,
                /^(?:to\b)/i,
                /^(?:optionally to\b)/i,
                /^(?:\.-)/i,
                /^(?:-\.)/i,
                /^(?:[A-Za-z_][A-Za-z0-9\-_]*)/i,
                /^(?:.)/i,
                /^(?:$)/i
            ],
            conditions: {
                acc_descr_multiline: {
                    rules: [
                        5,
                        6
                    ],
                    inclusive: !1
                },
                acc_descr: {
                    rules: [
                        3
                    ],
                    inclusive: !1
                },
                acc_title: {
                    rules: [
                        1
                    ],
                    inclusive: !1
                },
                block: {
                    rules: [
                        14,
                        15,
                        16,
                        17,
                        18,
                        19,
                        20,
                        21,
                        22
                    ],
                    inclusive: !1
                },
                INITIAL: {
                    rules: [
                        0,
                        2,
                        4,
                        7,
                        8,
                        9,
                        10,
                        11,
                        12,
                        13,
                        23,
                        24,
                        25,
                        26,
                        27,
                        28,
                        29,
                        30,
                        31,
                        32,
                        33,
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
                        51,
                        52,
                        53,
                        54,
                        55
                    ],
                    inclusive: !0
                }
            }
        };
        return I;
    }();
    R.lexer = O;
    function C() {
        this.yy = {};
    }
    return (0, _chunkGTKDMUJJMjs.a)(C, "Parser"), C.prototype = R, R.Parser = C, new C;
}();
nt.parser = nt;
var Mt = nt;
var V = new Map, st = [], Kt = {
    ZERO_OR_ONE: "ZERO_OR_ONE",
    ZERO_OR_MORE: "ZERO_OR_MORE",
    ONE_OR_MORE: "ONE_OR_MORE",
    ONLY_ONE: "ONLY_ONE",
    MD_PARENT: "MD_PARENT"
}, Vt = {
    NON_IDENTIFYING: "NON_IDENTIFYING",
    IDENTIFYING: "IDENTIFYING"
}, St = (0, _chunkGTKDMUJJMjs.a)(function(t, e) {
    return V.has(t) ? !V.get(t).alias && e && (V.get(t).alias = e, (0, _chunkNQURTBEVMjs.b).info(`Add alias '${e}' to entity '${t}'`)) : (V.set(t, {
        attributes: [],
        alias: e
    }), (0, _chunkNQURTBEVMjs.b).info("Added new entity :", t)), V.get(t);
}, "addEntity"), Gt = (0, _chunkGTKDMUJJMjs.a)(()=>V, "getEntities"), Xt = (0, _chunkGTKDMUJJMjs.a)(function(t, e) {
    let r = St(t), u;
    for(u = e.length - 1; u >= 0; u--)r.attributes.push(e[u]), (0, _chunkNQURTBEVMjs.b).debug("Added attribute ", e[u].attributeName);
}, "addAttributes"), Qt = (0, _chunkGTKDMUJJMjs.a)(function(t, e, r, u) {
    let h = {
        entityA: t,
        roleA: e,
        entityB: r,
        relSpec: u
    };
    st.push(h), (0, _chunkNQURTBEVMjs.b).debug("Added new relationship :", h);
}, "addRelationship"), qt = (0, _chunkGTKDMUJJMjs.a)(()=>st, "getRelationships"), Jt = (0, _chunkGTKDMUJJMjs.a)(function() {
    V = new Map, st = [], (0, _chunkNQURTBEVMjs.P)();
}, "clear"), wt = {
    Cardinality: Kt,
    Identification: Vt,
    getConfig: (0, _chunkGTKDMUJJMjs.a)(()=>(0, _chunkNQURTBEVMjs.X)().er, "getConfig"),
    addEntity: St,
    addAttributes: Xt,
    getEntities: Gt,
    addRelationship: Qt,
    getRelationships: qt,
    clear: Jt,
    setAccTitle: (0, _chunkNQURTBEVMjs.Q),
    getAccTitle: (0, _chunkNQURTBEVMjs.R),
    setAccDescription: (0, _chunkNQURTBEVMjs.S),
    getAccDescription: (0, _chunkNQURTBEVMjs.T),
    setDiagramTitle: (0, _chunkNQURTBEVMjs.U),
    getDiagramTitle: (0, _chunkNQURTBEVMjs.V)
};
var Y = {
    ONLY_ONE_START: "ONLY_ONE_START",
    ONLY_ONE_END: "ONLY_ONE_END",
    ZERO_OR_ONE_START: "ZERO_OR_ONE_START",
    ZERO_OR_ONE_END: "ZERO_OR_ONE_END",
    ONE_OR_MORE_START: "ONE_OR_MORE_START",
    ONE_OR_MORE_END: "ONE_OR_MORE_END",
    ZERO_OR_MORE_START: "ZERO_OR_MORE_START",
    ZERO_OR_MORE_END: "ZERO_OR_MORE_END",
    MD_PARENT_END: "MD_PARENT_END",
    MD_PARENT_START: "MD_PARENT_START"
}, jt = (0, _chunkGTKDMUJJMjs.a)(function(t, e) {
    let r;
    t.append("defs").append("marker").attr("id", Y.MD_PARENT_START).attr("refX", 0).attr("refY", 7).attr("markerWidth", 190).attr("markerHeight", 240).attr("orient", "auto").append("path").attr("d", "M 18,7 L9,13 L1,7 L9,1 Z"), t.append("defs").append("marker").attr("id", Y.MD_PARENT_END).attr("refX", 19).attr("refY", 7).attr("markerWidth", 20).attr("markerHeight", 28).attr("orient", "auto").append("path").attr("d", "M 18,7 L9,13 L1,7 L9,1 Z"), t.append("defs").append("marker").attr("id", Y.ONLY_ONE_START).attr("refX", 0).attr("refY", 9).attr("markerWidth", 18).attr("markerHeight", 18).attr("orient", "auto").append("path").attr("stroke", e.stroke).attr("fill", "none").attr("d", "M9,0 L9,18 M15,0 L15,18"), t.append("defs").append("marker").attr("id", Y.ONLY_ONE_END).attr("refX", 18).attr("refY", 9).attr("markerWidth", 18).attr("markerHeight", 18).attr("orient", "auto").append("path").attr("stroke", e.stroke).attr("fill", "none").attr("d", "M3,0 L3,18 M9,0 L9,18"), r = t.append("defs").append("marker").attr("id", Y.ZERO_OR_ONE_START).attr("refX", 0).attr("refY", 9).attr("markerWidth", 30).attr("markerHeight", 18).attr("orient", "auto"), r.append("circle").attr("stroke", e.stroke).attr("fill", "white").attr("cx", 21).attr("cy", 9).attr("r", 6), r.append("path").attr("stroke", e.stroke).attr("fill", "none").attr("d", "M9,0 L9,18"), r = t.append("defs").append("marker").attr("id", Y.ZERO_OR_ONE_END).attr("refX", 30).attr("refY", 9).attr("markerWidth", 30).attr("markerHeight", 18).attr("orient", "auto"), r.append("circle").attr("stroke", e.stroke).attr("fill", "white").attr("cx", 9).attr("cy", 9).attr("r", 6), r.append("path").attr("stroke", e.stroke).attr("fill", "none").attr("d", "M21,0 L21,18"), t.append("defs").append("marker").attr("id", Y.ONE_OR_MORE_START).attr("refX", 18).attr("refY", 18).attr("markerWidth", 45).attr("markerHeight", 36).attr("orient", "auto").append("path").attr("stroke", e.stroke).attr("fill", "none").attr("d", "M0,18 Q 18,0 36,18 Q 18,36 0,18 M42,9 L42,27"), t.append("defs").append("marker").attr("id", Y.ONE_OR_MORE_END).attr("refX", 27).attr("refY", 18).attr("markerWidth", 45).attr("markerHeight", 36).attr("orient", "auto").append("path").attr("stroke", e.stroke).attr("fill", "none").attr("d", "M3,9 L3,27 M9,18 Q27,0 45,18 Q27,36 9,18"), r = t.append("defs").append("marker").attr("id", Y.ZERO_OR_MORE_START).attr("refX", 18).attr("refY", 18).attr("markerWidth", 57).attr("markerHeight", 36).attr("orient", "auto"), r.append("circle").attr("stroke", e.stroke).attr("fill", "white").attr("cx", 48).attr("cy", 18).attr("r", 6), r.append("path").attr("stroke", e.stroke).attr("fill", "none").attr("d", "M0,18 Q18,0 36,18 Q18,36 0,18"), r = t.append("defs").append("marker").attr("id", Y.ZERO_OR_MORE_END).attr("refX", 39).attr("refY", 18).attr("markerWidth", 57).attr("markerHeight", 36).attr("orient", "auto"), r.append("circle").attr("stroke", e.stroke).attr("fill", "white").attr("cx", 9).attr("cy", 18).attr("r", 6), r.append("path").attr("stroke", e.stroke).attr("fill", "none").attr("d", "M21,18 Q39,0 57,18 Q39,36 21,18");
}, "insertMarkers"), B = {
    ERMarkers: Y,
    insertMarkers: jt
};
var It = /^(?:[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}|00000000-0000-0000-0000-000000000000)$/i;
function $t(t) {
    return typeof t == "string" && It.test(t);
}
(0, _chunkGTKDMUJJMjs.a)($t, "validate");
var vt = $t;
var M = [];
for(let t = 0; t < 256; ++t)M.push((t + 256).toString(16).slice(1));
function Dt(t, e = 0) {
    return M[t[e + 0]] + M[t[e + 1]] + M[t[e + 2]] + M[t[e + 3]] + "-" + M[t[e + 4]] + M[t[e + 5]] + "-" + M[t[e + 6]] + M[t[e + 7]] + "-" + M[t[e + 8]] + M[t[e + 9]] + "-" + M[t[e + 10]] + M[t[e + 11]] + M[t[e + 12]] + M[t[e + 13]] + M[t[e + 14]] + M[t[e + 15]];
}
(0, _chunkGTKDMUJJMjs.a)(Dt, "unsafeStringify");
function te(t) {
    if (!vt(t)) throw TypeError("Invalid UUID");
    let e, r = new Uint8Array(16);
    return r[0] = (e = parseInt(t.slice(0, 8), 16)) >>> 24, r[1] = e >>> 16 & 255, r[2] = e >>> 8 & 255, r[3] = e & 255, r[4] = (e = parseInt(t.slice(9, 13), 16)) >>> 8, r[5] = e & 255, r[6] = (e = parseInt(t.slice(14, 18), 16)) >>> 8, r[7] = e & 255, r[8] = (e = parseInt(t.slice(19, 23), 16)) >>> 8, r[9] = e & 255, r[10] = (e = parseInt(t.slice(24, 36), 16)) / 1099511627776 & 255, r[11] = e / 4294967296 & 255, r[12] = e >>> 24 & 255, r[13] = e >>> 16 & 255, r[14] = e >>> 8 & 255, r[15] = e & 255, r;
}
(0, _chunkGTKDMUJJMjs.a)(te, "parse");
var Lt = te;
function ee(t) {
    t = unescape(encodeURIComponent(t));
    let e = [];
    for(let r = 0; r < t.length; ++r)e.push(t.charCodeAt(r));
    return e;
}
(0, _chunkGTKDMUJJMjs.a)(ee, "stringToBytes");
var re = "6ba7b810-9dad-11d1-80b4-00c04fd430c8", ae = "6ba7b811-9dad-11d1-80b4-00c04fd430c8";
function ot(t, e, r) {
    function u(h, _, p, l) {
        var d;
        if (typeof h == "string" && (h = ee(h)), typeof _ == "string" && (_ = Lt(_)), ((d = _) === null || d === void 0 ? void 0 : d.length) !== 16) throw TypeError("Namespace must be array-like (16 iterable integer values, 0-255)");
        let k = new Uint8Array(16 + h.length);
        if (k.set(_), k.set(h, _.length), k = r(k), k[6] = k[6] & 15 | e, k[8] = k[8] & 63 | 128, p) {
            l = l || 0;
            for(let E = 0; E < 16; ++E)p[l + E] = k[E];
            return p;
        }
        return Dt(k);
    }
    (0, _chunkGTKDMUJJMjs.a)(u, "generateUUID");
    try {
        u.name = t;
    } catch  {}
    return u.DNS = re, u.URL = ae, u;
}
(0, _chunkGTKDMUJJMjs.a)(ot, "v35");
function ie(t, e, r, u) {
    switch(t){
        case 0:
            return e & r ^ ~e & u;
        case 1:
            return e ^ r ^ u;
        case 2:
            return e & r ^ e & u ^ r & u;
        case 3:
            return e ^ r ^ u;
    }
}
(0, _chunkGTKDMUJJMjs.a)(ie, "f");
function lt(t, e) {
    return t << e | t >>> 32 - e;
}
(0, _chunkGTKDMUJJMjs.a)(lt, "ROTL");
function ne(t) {
    let e = [
        1518500249,
        1859775393,
        2400959708,
        3395469782
    ], r = [
        1732584193,
        4023233417,
        2562383102,
        271733878,
        3285377520
    ];
    if (typeof t == "string") {
        let p = unescape(encodeURIComponent(t));
        t = [];
        for(let l = 0; l < p.length; ++l)t.push(p.charCodeAt(l));
    } else Array.isArray(t) || (t = Array.prototype.slice.call(t));
    t.push(128);
    let u = t.length / 4 + 2, h = Math.ceil(u / 16), _ = new Array(h);
    for(let p = 0; p < h; ++p){
        let l = new Uint32Array(16);
        for(let d = 0; d < 16; ++d)l[d] = t[p * 64 + d * 4] << 24 | t[p * 64 + d * 4 + 1] << 16 | t[p * 64 + d * 4 + 2] << 8 | t[p * 64 + d * 4 + 3];
        _[p] = l;
    }
    _[h - 1][14] = (t.length - 1) * 8 / Math.pow(2, 32), _[h - 1][14] = Math.floor(_[h - 1][14]), _[h - 1][15] = (t.length - 1) * 8 & 4294967295;
    for(let p = 0; p < h; ++p){
        let l = new Uint32Array(80);
        for(let y = 0; y < 16; ++y)l[y] = _[p][y];
        for(let y = 16; y < 80; ++y)l[y] = lt(l[y - 3] ^ l[y - 8] ^ l[y - 14] ^ l[y - 16], 1);
        let d = r[0], k = r[1], E = r[2], g = r[3], x = r[4];
        for(let y = 0; y < 80; ++y){
            let T = Math.floor(y / 20), D = lt(d, 5) + ie(T, k, E, g) + x + e[T] + l[y] >>> 0;
            x = g, g = E, E = lt(k, 30) >>> 0, k = d, d = D;
        }
        r[0] = r[0] + d >>> 0, r[1] = r[1] + k >>> 0, r[2] = r[2] + E >>> 0, r[3] = r[3] + g >>> 0, r[4] = r[4] + x >>> 0;
    }
    return [
        r[0] >> 24 & 255,
        r[0] >> 16 & 255,
        r[0] >> 8 & 255,
        r[0] & 255,
        r[1] >> 24 & 255,
        r[1] >> 16 & 255,
        r[1] >> 8 & 255,
        r[1] & 255,
        r[2] >> 24 & 255,
        r[2] >> 16 & 255,
        r[2] >> 8 & 255,
        r[2] & 255,
        r[3] >> 24 & 255,
        r[3] >> 16 & 255,
        r[3] >> 8 & 255,
        r[3] & 255,
        r[4] >> 24 & 255,
        r[4] >> 16 & 255,
        r[4] >> 8 & 255,
        r[4] & 255
    ];
}
(0, _chunkGTKDMUJJMjs.a)(ne, "sha1");
var Bt = ne;
var se = ot("v5", 80, Bt), ct = se;
var oe = /[^\dA-Za-z](\W)*/g, b = {}, Q = new Map, le = (0, _chunkGTKDMUJJMjs.a)(function(t) {
    let e = Object.keys(t);
    for (let r of e)b[r] = t[r];
}, "setConf"), ce = (0, _chunkGTKDMUJJMjs.a)((t, e, r)=>{
    let u = b.entityPadding / 3, h = b.entityPadding / 3, _ = b.fontSize * .85, p = e.node().getBBox(), l = [], d = !1, k = !1, E = 0, g = 0, x = 0, y = 0, T = p.height + u * 2, D = 1;
    r.forEach((A)=>{
        A.attributeKeyTypeList !== void 0 && A.attributeKeyTypeList.length > 0 && (d = !0), A.attributeComment !== void 0 && (k = !0);
    }), r.forEach((A)=>{
        let S = `${e.node().id}-attr-${D}`, R = 0, O = (0, _chunkNQURTBEVMjs.H)(A.attributeType), C = t.append("text").classed("er entityLabel", !0).attr("id", `${S}-type`).attr("x", 0).attr("y", 0).style("dominant-baseline", "middle").style("text-anchor", "left").style("font-family", (0, _chunkNQURTBEVMjs.X)().fontFamily).style("font-size", _ + "px").text(O), I = t.append("text").classed("er entityLabel", !0).attr("id", `${S}-name`).attr("x", 0).attr("y", 0).style("dominant-baseline", "middle").style("text-anchor", "left").style("font-family", (0, _chunkNQURTBEVMjs.X)().fontFamily).style("font-size", _ + "px").text(A.attributeName), i = {};
        i.tn = C, i.nn = I;
        let s = C.node().getBBox(), c = I.node().getBBox();
        if (E = Math.max(E, s.width), g = Math.max(g, c.width), R = Math.max(s.height, c.height), d) {
            let f = A.attributeKeyTypeList !== void 0 ? A.attributeKeyTypeList.join(",") : "", m = t.append("text").classed("er entityLabel", !0).attr("id", `${S}-key`).attr("x", 0).attr("y", 0).style("dominant-baseline", "middle").style("text-anchor", "left").style("font-family", (0, _chunkNQURTBEVMjs.X)().fontFamily).style("font-size", _ + "px").text(f);
            i.kn = m;
            let a = m.node().getBBox();
            x = Math.max(x, a.width), R = Math.max(R, a.height);
        }
        if (k) {
            let f = t.append("text").classed("er entityLabel", !0).attr("id", `${S}-comment`).attr("x", 0).attr("y", 0).style("dominant-baseline", "middle").style("text-anchor", "left").style("font-family", (0, _chunkNQURTBEVMjs.X)().fontFamily).style("font-size", _ + "px").text(A.attributeComment || "");
            i.cn = f;
            let m = f.node().getBBox();
            y = Math.max(y, m.width), R = Math.max(R, m.height);
        }
        i.height = R, l.push(i), T += R + u * 2, D += 1;
    });
    let W = 4;
    d && (W += 2), k && (W += 2);
    let U = E + g + x + y, Z = {
        width: Math.max(b.minEntityWidth, Math.max(p.width + b.entityPadding * 2, U + h * W)),
        height: r.length > 0 ? T : Math.max(b.minEntityHeight, p.height + b.entityPadding * 2)
    };
    if (r.length > 0) {
        let A = Math.max(0, (Z.width - U - h * W) / (W / 2));
        e.attr("transform", "translate(" + Z.width / 2 + "," + (u + p.height / 2) + ")");
        let S = p.height + u * 2, R = "attributeBoxOdd";
        l.forEach((O)=>{
            let C = S + u + O.height / 2;
            O.tn.attr("transform", "translate(" + h + "," + C + ")");
            let I = t.insert("rect", "#" + O.tn.node().id).classed(`er ${R}`, !0).attr("x", 0).attr("y", S).attr("width", E + h * 2 + A).attr("height", O.height + u * 2), i = parseFloat(I.attr("x")) + parseFloat(I.attr("width"));
            O.nn.attr("transform", "translate(" + (i + h) + "," + C + ")");
            let s = t.insert("rect", "#" + O.nn.node().id).classed(`er ${R}`, !0).attr("x", i).attr("y", S).attr("width", g + h * 2 + A).attr("height", O.height + u * 2), c = parseFloat(s.attr("x")) + parseFloat(s.attr("width"));
            if (d) {
                O.kn.attr("transform", "translate(" + (c + h) + "," + C + ")");
                let f = t.insert("rect", "#" + O.kn.node().id).classed(`er ${R}`, !0).attr("x", c).attr("y", S).attr("width", x + h * 2 + A).attr("height", O.height + u * 2);
                c = parseFloat(f.attr("x")) + parseFloat(f.attr("width"));
            }
            k && (O.cn.attr("transform", "translate(" + (c + h) + "," + C + ")"), t.insert("rect", "#" + O.cn.node().id).classed(`er ${R}`, "true").attr("x", c).attr("y", S).attr("width", y + h * 2 + A).attr("height", O.height + u * 2)), S += O.height + u * 2, R = R === "attributeBoxOdd" ? "attributeBoxEven" : "attributeBoxOdd";
        });
    } else Z.height = Math.max(b.minEntityHeight, T), e.attr("transform", "translate(" + Z.width / 2 + "," + Z.height / 2 + ")");
    return Z;
}, "drawAttributes"), he = (0, _chunkGTKDMUJJMjs.a)(function(t, e, r) {
    let u = [
        ...e.keys()
    ], h;
    return u.forEach(function(_) {
        let p = _e(_, "entity");
        Q.set(_, p);
        let l = t.append("g").attr("id", p);
        h = h === void 0 ? p : h;
        let d = "text-" + p, k = l.append("text").classed("er entityLabel", !0).attr("id", d).attr("x", 0).attr("y", 0).style("dominant-baseline", "middle").style("text-anchor", "middle").style("font-family", (0, _chunkNQURTBEVMjs.X)().fontFamily).style("font-size", b.fontSize + "px").text(e.get(_).alias ?? _), { width: E, height: g } = ce(l, k, e.get(_).attributes), y = l.insert("rect", "#" + d).classed("er entityBox", !0).attr("x", 0).attr("y", 0).attr("width", E).attr("height", g).node().getBBox();
        r.setNode(p, {
            width: y.width,
            height: y.height,
            shape: "rect",
            id: p
        });
    }), h;
}, "drawEntities"), de = (0, _chunkGTKDMUJJMjs.a)(function(t, e) {
    e.nodes().forEach(function(r) {
        r !== void 0 && e.node(r) !== void 0 && t.select("#" + r).attr("transform", "translate(" + (e.node(r).x - e.node(r).width / 2) + "," + (e.node(r).y - e.node(r).height / 2) + " )");
    });
}, "adjustEntities"), Yt = (0, _chunkGTKDMUJJMjs.a)(function(t) {
    return (t.entityA + t.roleA + t.entityB).replace(/\s/g, "");
}, "getEdgeName"), fe = (0, _chunkGTKDMUJJMjs.a)(function(t, e) {
    return t.forEach(function(r) {
        e.setEdge(Q.get(r.entityA), Q.get(r.entityB), {
            relationship: r
        }, Yt(r));
    }), t;
}, "addRelationships"), Ct = 0, ue = (0, _chunkGTKDMUJJMjs.a)(function(t, e, r, u, h) {
    Ct++;
    let _ = r.edge(Q.get(e.entityA), Q.get(e.entityB), Yt(e)), p = (0, _chunkNQURTBEVMjs.Ca)().x(function(T) {
        return T.x;
    }).y(function(T) {
        return T.y;
    }).curve((0, _chunkNQURTBEVMjs.Ga)), l = t.insert("path", "#" + u).classed("er relationshipLine", !0).attr("d", p(_.points)).style("stroke", b.stroke).style("fill", "none");
    e.relSpec.relType === h.db.Identification.NON_IDENTIFYING && l.attr("stroke-dasharray", "8,8");
    let d = "";
    switch(b.arrowMarkerAbsolute && (d = window.location.protocol + "//" + window.location.host + window.location.pathname + window.location.search, d = d.replace(/\(/g, "\\("), d = d.replace(/\)/g, "\\)")), e.relSpec.cardA){
        case h.db.Cardinality.ZERO_OR_ONE:
            l.attr("marker-end", "url(" + d + "#" + B.ERMarkers.ZERO_OR_ONE_END + ")");
            break;
        case h.db.Cardinality.ZERO_OR_MORE:
            l.attr("marker-end", "url(" + d + "#" + B.ERMarkers.ZERO_OR_MORE_END + ")");
            break;
        case h.db.Cardinality.ONE_OR_MORE:
            l.attr("marker-end", "url(" + d + "#" + B.ERMarkers.ONE_OR_MORE_END + ")");
            break;
        case h.db.Cardinality.ONLY_ONE:
            l.attr("marker-end", "url(" + d + "#" + B.ERMarkers.ONLY_ONE_END + ")");
            break;
        case h.db.Cardinality.MD_PARENT:
            l.attr("marker-end", "url(" + d + "#" + B.ERMarkers.MD_PARENT_END + ")");
            break;
    }
    switch(e.relSpec.cardB){
        case h.db.Cardinality.ZERO_OR_ONE:
            l.attr("marker-start", "url(" + d + "#" + B.ERMarkers.ZERO_OR_ONE_START + ")");
            break;
        case h.db.Cardinality.ZERO_OR_MORE:
            l.attr("marker-start", "url(" + d + "#" + B.ERMarkers.ZERO_OR_MORE_START + ")");
            break;
        case h.db.Cardinality.ONE_OR_MORE:
            l.attr("marker-start", "url(" + d + "#" + B.ERMarkers.ONE_OR_MORE_START + ")");
            break;
        case h.db.Cardinality.ONLY_ONE:
            l.attr("marker-start", "url(" + d + "#" + B.ERMarkers.ONLY_ONE_START + ")");
            break;
        case h.db.Cardinality.MD_PARENT:
            l.attr("marker-start", "url(" + d + "#" + B.ERMarkers.MD_PARENT_START + ")");
            break;
    }
    let k = l.node().getTotalLength(), E = l.node().getPointAtLength(k * .5), g = "rel" + Ct, y = t.append("text").classed("er relationshipLabel", !0).attr("id", g).attr("x", E.x).attr("y", E.y).style("text-anchor", "middle").style("dominant-baseline", "middle").style("font-family", (0, _chunkNQURTBEVMjs.X)().fontFamily).style("font-size", b.fontSize + "px").text(e.roleA).node().getBBox();
    t.insert("rect", "#" + g).classed("er relationshipLabelBox", !0).attr("x", E.x - y.width / 2).attr("y", E.y - y.height / 2).attr("width", y.width).attr("height", y.height);
}, "drawRelationshipFromLayout"), pe = (0, _chunkGTKDMUJJMjs.a)(function(t, e, r, u) {
    b = (0, _chunkNQURTBEVMjs.X)().er, (0, _chunkNQURTBEVMjs.b).info("Drawing ER diagram");
    let h = (0, _chunkNQURTBEVMjs.X)().securityLevel, _;
    h === "sandbox" && (_ = (0, _chunkNQURTBEVMjs.fa)("#i" + e));
    let l = (h === "sandbox" ? (0, _chunkNQURTBEVMjs.fa)(_.nodes()[0].contentDocument.body) : (0, _chunkNQURTBEVMjs.fa)("body")).select(`[id='${e}']`);
    B.insertMarkers(l, b);
    let d;
    d = new (0, _chunk6XGRHI2AMjs.a)({
        multigraph: !0,
        directed: !0,
        compound: !1
    }).setGraph({
        rankdir: b.layoutDirection,
        marginx: 20,
        marginy: 20,
        nodesep: 100,
        edgesep: 100,
        ranksep: 100
    }).setDefaultEdgeLabel(function() {
        return {};
    });
    let k = he(l, u.db.getEntities(), d), E = fe(u.db.getRelationships(), d);
    (0, _chunkBOP2KBYHMjs.a)(d), de(l, d), E.forEach(function(D) {
        ue(l, D, d, k, u);
    });
    let g = b.diagramPadding;
    (0, _chunkAC3VT7B7Mjs.m).insertTitle(l, "entityTitleText", b.titleTopMargin, u.db.getDiagramTitle());
    let x = l.node().getBBox(), y = x.width + g * 2, T = x.height + g * 2;
    (0, _chunkNQURTBEVMjs.M)(l, T, y, b.useMaxWidth), l.attr("viewBox", `${x.x - g} ${x.y - g} ${y} ${T}`);
}, "draw"), ye = "28e9f9db-3c8d-5aa5-9faf-44286ae5937c";
function _e(t = "", e = "") {
    let r = t.replace(oe, "");
    return `${Pt(e)}${Pt(r)}${ct(t, ye)}`;
}
(0, _chunkGTKDMUJJMjs.a)(_e, "generateId");
function Pt(t = "") {
    return t.length > 0 ? `${t}-` : "";
}
(0, _chunkGTKDMUJJMjs.a)(Pt, "strWithHyphen");
var Zt = {
    setConf: le,
    draw: pe
};
var me = (0, _chunkGTKDMUJJMjs.a)((t)=>`
  .entityBox {
    fill: ${t.mainBkg};
    stroke: ${t.nodeBorder};
  }

  .attributeBoxOdd {
    fill: ${t.attributeBackgroundColorOdd};
    stroke: ${t.nodeBorder};
  }

  .attributeBoxEven {
    fill:  ${t.attributeBackgroundColorEven};
    stroke: ${t.nodeBorder};
  }

  .relationshipLabelBox {
    fill: ${t.tertiaryColor};
    opacity: 0.7;
    background-color: ${t.tertiaryColor};
      rect {
        opacity: 0.5;
      }
  }

    .relationshipLine {
      stroke: ${t.lineColor};
    }

  .entityTitleText {
    text-anchor: middle;
    font-size: 18px;
    fill: ${t.textColor};
  }    
  #MD_PARENT_START {
    fill: #f5f5f5 !important;
    stroke: ${t.lineColor} !important;
    stroke-width: 1;
  }
  #MD_PARENT_END {
    fill: #f5f5f5 !important;
    stroke: ${t.lineColor} !important;
    stroke-width: 1;
  }
  
`, "getStyles"), Ft = me;
var ur = {
    parser: Mt,
    db: wt,
    renderer: Zt,
    styles: Ft
};

},{"./chunk-BOP2KBYH.mjs":"klimL","./chunk-6XGRHI2A.mjs":"fUQIF","./chunk-AC3VT7B7.mjs":"1eiUz","./chunk-TI4EEUUG.mjs":"8SKrN","./chunk-NQURTBEV.mjs":"iASFe","./chunk-BKDDFIKN.mjs":"hADfH","./chunk-YPUTD6PB.mjs":"aSdv1","./chunk-6BY5RJGC.mjs":"bRXnR","./chunk-GTKDMUJJ.mjs":"fruhx","@parcel/transformer-js/src/esmodule-helpers.js":"1cdyk"}],"fUQIF":[function(require,module,exports,__globalThis) {
var parcelHelpers = require("@parcel/transformer-js/src/esmodule-helpers.js");
parcelHelpers.defineInteropFlag(exports);
parcelHelpers.export(exports, "a", ()=>b);
var _chunkBKDDFIKNMjs = require("./chunk-BKDDFIKN.mjs");
var _chunkYPUTD6PBMjs = require("./chunk-YPUTD6PB.mjs");
var _chunk6BY5RJGCMjs = require("./chunk-6BY5RJGC.mjs");
var _chunkGTKDMUJJMjs = require("./chunk-GTKDMUJJ.mjs");
var j = "\0", f = "\0", D = "", b = class {
    static{
        (0, _chunkGTKDMUJJMjs.a)(this, "Graph");
    }
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

},{"./chunk-BKDDFIKN.mjs":"hADfH","./chunk-YPUTD6PB.mjs":"aSdv1","./chunk-6BY5RJGC.mjs":"bRXnR","./chunk-GTKDMUJJ.mjs":"fruhx","@parcel/transformer-js/src/esmodule-helpers.js":"1cdyk"}]},["hdXnv"], null, "parcelRequire6955", {})

//# sourceMappingURL=erDiagram-Y4N7DENO.50835cd3.js.map
