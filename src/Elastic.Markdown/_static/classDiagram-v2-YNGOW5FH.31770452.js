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
})({"1JRUe":[function(require,module,exports,__globalThis) {
var global = arguments[3];
var HMR_HOST = null;
var HMR_PORT = 1234;
var HMR_SERVER_PORT = 1234;
var HMR_SECURE = false;
var HMR_ENV_HASH = "febfef3c5b467b16";
var HMR_USE_SSE = false;
module.bundle.HMR_BUNDLE_ID = "3537a1c131770452";
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

},{}],"gf7Uq":[function(require,module,exports,__globalThis) {
var parcelHelpers = require("@parcel/transformer-js/src/esmodule-helpers.js");
parcelHelpers.defineInteropFlag(exports);
parcelHelpers.export(exports, "markers_default", ()=>markers_default);
parcelHelpers.export(exports, "clear", ()=>clear);
parcelHelpers.export(exports, "insertEdgeLabel", ()=>insertEdgeLabel);
parcelHelpers.export(exports, "positionEdgeLabel", ()=>positionEdgeLabel);
parcelHelpers.export(exports, "insertEdge", ()=>insertEdge);
var _chunkHKQCUR3CMjs = require("./chunk-HKQCUR3C.mjs");
var _chunkKW7S66XIMjs = require("./chunk-KW7S66XI.mjs");
var _chunkYP6PVJQ3Mjs = require("./chunk-YP6PVJQ3.mjs");
var _chunkI7ZFS43CMjs = require("./chunk-I7ZFS43C.mjs");
var _chunkDD37ZF33Mjs = require("./chunk-DD37ZF33.mjs");
var _chunkDLQEHMXDMjs = require("./chunk-DLQEHMXD.mjs");
// src/dagre-wrapper/markers.js
var insertMarkers = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((elem, markerArray, type, id)=>{
    markerArray.forEach((markerName)=>{
        markers[markerName](elem, type, id);
    });
}, "insertMarkers");
var extension = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((elem, type, id)=>{
    (0, _chunkDD37ZF33Mjs.log).trace("Making markers for ", id);
    elem.append("defs").append("marker").attr("id", id + "_" + type + "-extensionStart").attr("class", "marker extension " + type).attr("refX", 18).attr("refY", 7).attr("markerWidth", 190).attr("markerHeight", 240).attr("orient", "auto").append("path").attr("d", "M 1,7 L18,13 V 1 Z");
    elem.append("defs").append("marker").attr("id", id + "_" + type + "-extensionEnd").attr("class", "marker extension " + type).attr("refX", 1).attr("refY", 7).attr("markerWidth", 20).attr("markerHeight", 28).attr("orient", "auto").append("path").attr("d", "M 1,1 V 13 L18,7 Z");
}, "extension");
var composition = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((elem, type, id)=>{
    elem.append("defs").append("marker").attr("id", id + "_" + type + "-compositionStart").attr("class", "marker composition " + type).attr("refX", 18).attr("refY", 7).attr("markerWidth", 190).attr("markerHeight", 240).attr("orient", "auto").append("path").attr("d", "M 18,7 L9,13 L1,7 L9,1 Z");
    elem.append("defs").append("marker").attr("id", id + "_" + type + "-compositionEnd").attr("class", "marker composition " + type).attr("refX", 1).attr("refY", 7).attr("markerWidth", 20).attr("markerHeight", 28).attr("orient", "auto").append("path").attr("d", "M 18,7 L9,13 L1,7 L9,1 Z");
}, "composition");
var aggregation = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((elem, type, id)=>{
    elem.append("defs").append("marker").attr("id", id + "_" + type + "-aggregationStart").attr("class", "marker aggregation " + type).attr("refX", 18).attr("refY", 7).attr("markerWidth", 190).attr("markerHeight", 240).attr("orient", "auto").append("path").attr("d", "M 18,7 L9,13 L1,7 L9,1 Z");
    elem.append("defs").append("marker").attr("id", id + "_" + type + "-aggregationEnd").attr("class", "marker aggregation " + type).attr("refX", 1).attr("refY", 7).attr("markerWidth", 20).attr("markerHeight", 28).attr("orient", "auto").append("path").attr("d", "M 18,7 L9,13 L1,7 L9,1 Z");
}, "aggregation");
var dependency = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((elem, type, id)=>{
    elem.append("defs").append("marker").attr("id", id + "_" + type + "-dependencyStart").attr("class", "marker dependency " + type).attr("refX", 6).attr("refY", 7).attr("markerWidth", 190).attr("markerHeight", 240).attr("orient", "auto").append("path").attr("d", "M 5,7 L9,13 L1,7 L9,1 Z");
    elem.append("defs").append("marker").attr("id", id + "_" + type + "-dependencyEnd").attr("class", "marker dependency " + type).attr("refX", 13).attr("refY", 7).attr("markerWidth", 20).attr("markerHeight", 28).attr("orient", "auto").append("path").attr("d", "M 18,7 L9,13 L14,7 L9,1 Z");
}, "dependency");
var lollipop = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((elem, type, id)=>{
    elem.append("defs").append("marker").attr("id", id + "_" + type + "-lollipopStart").attr("class", "marker lollipop " + type).attr("refX", 13).attr("refY", 7).attr("markerWidth", 190).attr("markerHeight", 240).attr("orient", "auto").append("circle").attr("stroke", "black").attr("fill", "transparent").attr("cx", 7).attr("cy", 7).attr("r", 6);
    elem.append("defs").append("marker").attr("id", id + "_" + type + "-lollipopEnd").attr("class", "marker lollipop " + type).attr("refX", 1).attr("refY", 7).attr("markerWidth", 190).attr("markerHeight", 240).attr("orient", "auto").append("circle").attr("stroke", "black").attr("fill", "transparent").attr("cx", 7).attr("cy", 7).attr("r", 6);
}, "lollipop");
var point = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((elem, type, id)=>{
    elem.append("marker").attr("id", id + "_" + type + "-pointEnd").attr("class", "marker " + type).attr("viewBox", "0 0 10 10").attr("refX", 6).attr("refY", 5).attr("markerUnits", "userSpaceOnUse").attr("markerWidth", 12).attr("markerHeight", 12).attr("orient", "auto").append("path").attr("d", "M 0 0 L 10 5 L 0 10 z").attr("class", "arrowMarkerPath").style("stroke-width", 1).style("stroke-dasharray", "1,0");
    elem.append("marker").attr("id", id + "_" + type + "-pointStart").attr("class", "marker " + type).attr("viewBox", "0 0 10 10").attr("refX", 4.5).attr("refY", 5).attr("markerUnits", "userSpaceOnUse").attr("markerWidth", 12).attr("markerHeight", 12).attr("orient", "auto").append("path").attr("d", "M 0 5 L 10 10 L 10 0 z").attr("class", "arrowMarkerPath").style("stroke-width", 1).style("stroke-dasharray", "1,0");
}, "point");
var circle = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((elem, type, id)=>{
    elem.append("marker").attr("id", id + "_" + type + "-circleEnd").attr("class", "marker " + type).attr("viewBox", "0 0 10 10").attr("refX", 11).attr("refY", 5).attr("markerUnits", "userSpaceOnUse").attr("markerWidth", 11).attr("markerHeight", 11).attr("orient", "auto").append("circle").attr("cx", "5").attr("cy", "5").attr("r", "5").attr("class", "arrowMarkerPath").style("stroke-width", 1).style("stroke-dasharray", "1,0");
    elem.append("marker").attr("id", id + "_" + type + "-circleStart").attr("class", "marker " + type).attr("viewBox", "0 0 10 10").attr("refX", -1).attr("refY", 5).attr("markerUnits", "userSpaceOnUse").attr("markerWidth", 11).attr("markerHeight", 11).attr("orient", "auto").append("circle").attr("cx", "5").attr("cy", "5").attr("r", "5").attr("class", "arrowMarkerPath").style("stroke-width", 1).style("stroke-dasharray", "1,0");
}, "circle");
var cross = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((elem, type, id)=>{
    elem.append("marker").attr("id", id + "_" + type + "-crossEnd").attr("class", "marker cross " + type).attr("viewBox", "0 0 11 11").attr("refX", 12).attr("refY", 5.2).attr("markerUnits", "userSpaceOnUse").attr("markerWidth", 11).attr("markerHeight", 11).attr("orient", "auto").append("path").attr("d", "M 1,1 l 9,9 M 10,1 l -9,9").attr("class", "arrowMarkerPath").style("stroke-width", 2).style("stroke-dasharray", "1,0");
    elem.append("marker").attr("id", id + "_" + type + "-crossStart").attr("class", "marker cross " + type).attr("viewBox", "0 0 11 11").attr("refX", -1).attr("refY", 5.2).attr("markerUnits", "userSpaceOnUse").attr("markerWidth", 11).attr("markerHeight", 11).attr("orient", "auto").append("path").attr("d", "M 1,1 l 9,9 M 10,1 l -9,9").attr("class", "arrowMarkerPath").style("stroke-width", 2).style("stroke-dasharray", "1,0");
}, "cross");
var barb = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((elem, type, id)=>{
    elem.append("defs").append("marker").attr("id", id + "_" + type + "-barbEnd").attr("refX", 19).attr("refY", 7).attr("markerWidth", 20).attr("markerHeight", 14).attr("markerUnits", "strokeWidth").attr("orient", "auto").append("path").attr("d", "M 19,7 L9,13 L14,7 L9,1 Z");
}, "barb");
var markers = {
    extension,
    composition,
    aggregation,
    dependency,
    lollipop,
    point,
    circle,
    cross,
    barb
};
var markers_default = insertMarkers;
// src/dagre-wrapper/edgeMarker.ts
var addEdgeMarkers = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((svgPath, edge, url, id, diagramType)=>{
    if (edge.arrowTypeStart) addEdgeMarker(svgPath, "start", edge.arrowTypeStart, url, id, diagramType);
    if (edge.arrowTypeEnd) addEdgeMarker(svgPath, "end", edge.arrowTypeEnd, url, id, diagramType);
}, "addEdgeMarkers");
var arrowTypesMap = {
    arrow_cross: "cross",
    arrow_point: "point",
    arrow_barb: "barb",
    arrow_circle: "circle",
    aggregation: "aggregation",
    extension: "extension",
    composition: "composition",
    dependency: "dependency",
    lollipop: "lollipop"
};
var addEdgeMarker = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((svgPath, position, arrowType, url, id, diagramType)=>{
    const endMarkerType = arrowTypesMap[arrowType];
    if (!endMarkerType) {
        (0, _chunkDD37ZF33Mjs.log).warn(`Unknown arrow type: ${arrowType}`);
        return;
    }
    const suffix = position === "start" ? "Start" : "End";
    svgPath.attr(`marker-${position}`, `url(${url}#${id}_${diagramType}-${endMarkerType}${suffix})`);
}, "addEdgeMarker");
// src/dagre-wrapper/edges.js
var edgeLabels = {};
var terminalLabels = {};
var clear = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(()=>{
    edgeLabels = {};
    terminalLabels = {};
}, "clear");
var insertEdgeLabel = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((elem, edge)=>{
    const config = (0, _chunkDD37ZF33Mjs.getConfig2)();
    const useHtmlLabels = (0, _chunkDD37ZF33Mjs.evaluate)(config.flowchart.htmlLabels);
    const labelElement = edge.labelType === "markdown" ? (0, _chunkYP6PVJQ3Mjs.createText)(elem, edge.label, {
        style: edge.labelStyle,
        useHtmlLabels,
        addSvgBackground: true
    }, config) : (0, _chunkHKQCUR3CMjs.createLabel_default)(edge.label, edge.labelStyle);
    const edgeLabel = elem.insert("g").attr("class", "edgeLabel");
    const label = edgeLabel.insert("g").attr("class", "label");
    label.node().appendChild(labelElement);
    let bbox = labelElement.getBBox();
    if (useHtmlLabels) {
        const div = labelElement.children[0];
        const dv = (0, _chunkDD37ZF33Mjs.select_default)(labelElement);
        bbox = div.getBoundingClientRect();
        dv.attr("width", bbox.width);
        dv.attr("height", bbox.height);
    }
    label.attr("transform", "translate(" + -bbox.width / 2 + ", " + -bbox.height / 2 + ")");
    edgeLabels[edge.id] = edgeLabel;
    edge.width = bbox.width;
    edge.height = bbox.height;
    let fo;
    if (edge.startLabelLeft) {
        const startLabelElement = (0, _chunkHKQCUR3CMjs.createLabel_default)(edge.startLabelLeft, edge.labelStyle);
        const startEdgeLabelLeft = elem.insert("g").attr("class", "edgeTerminals");
        const inner = startEdgeLabelLeft.insert("g").attr("class", "inner");
        fo = inner.node().appendChild(startLabelElement);
        const slBox = startLabelElement.getBBox();
        inner.attr("transform", "translate(" + -slBox.width / 2 + ", " + -slBox.height / 2 + ")");
        if (!terminalLabels[edge.id]) terminalLabels[edge.id] = {};
        terminalLabels[edge.id].startLeft = startEdgeLabelLeft;
        setTerminalWidth(fo, edge.startLabelLeft);
    }
    if (edge.startLabelRight) {
        const startLabelElement = (0, _chunkHKQCUR3CMjs.createLabel_default)(edge.startLabelRight, edge.labelStyle);
        const startEdgeLabelRight = elem.insert("g").attr("class", "edgeTerminals");
        const inner = startEdgeLabelRight.insert("g").attr("class", "inner");
        fo = startEdgeLabelRight.node().appendChild(startLabelElement);
        inner.node().appendChild(startLabelElement);
        const slBox = startLabelElement.getBBox();
        inner.attr("transform", "translate(" + -slBox.width / 2 + ", " + -slBox.height / 2 + ")");
        if (!terminalLabels[edge.id]) terminalLabels[edge.id] = {};
        terminalLabels[edge.id].startRight = startEdgeLabelRight;
        setTerminalWidth(fo, edge.startLabelRight);
    }
    if (edge.endLabelLeft) {
        const endLabelElement = (0, _chunkHKQCUR3CMjs.createLabel_default)(edge.endLabelLeft, edge.labelStyle);
        const endEdgeLabelLeft = elem.insert("g").attr("class", "edgeTerminals");
        const inner = endEdgeLabelLeft.insert("g").attr("class", "inner");
        fo = inner.node().appendChild(endLabelElement);
        const slBox = endLabelElement.getBBox();
        inner.attr("transform", "translate(" + -slBox.width / 2 + ", " + -slBox.height / 2 + ")");
        endEdgeLabelLeft.node().appendChild(endLabelElement);
        if (!terminalLabels[edge.id]) terminalLabels[edge.id] = {};
        terminalLabels[edge.id].endLeft = endEdgeLabelLeft;
        setTerminalWidth(fo, edge.endLabelLeft);
    }
    if (edge.endLabelRight) {
        const endLabelElement = (0, _chunkHKQCUR3CMjs.createLabel_default)(edge.endLabelRight, edge.labelStyle);
        const endEdgeLabelRight = elem.insert("g").attr("class", "edgeTerminals");
        const inner = endEdgeLabelRight.insert("g").attr("class", "inner");
        fo = inner.node().appendChild(endLabelElement);
        const slBox = endLabelElement.getBBox();
        inner.attr("transform", "translate(" + -slBox.width / 2 + ", " + -slBox.height / 2 + ")");
        endEdgeLabelRight.node().appendChild(endLabelElement);
        if (!terminalLabels[edge.id]) terminalLabels[edge.id] = {};
        terminalLabels[edge.id].endRight = endEdgeLabelRight;
        setTerminalWidth(fo, edge.endLabelRight);
    }
    return labelElement;
}, "insertEdgeLabel");
function setTerminalWidth(fo, value) {
    if ((0, _chunkDD37ZF33Mjs.getConfig2)().flowchart.htmlLabels && fo) {
        fo.style.width = value.length * 9 + "px";
        fo.style.height = "12px";
    }
}
(0, _chunkDLQEHMXDMjs.__name)(setTerminalWidth, "setTerminalWidth");
var positionEdgeLabel = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((edge, paths)=>{
    (0, _chunkDD37ZF33Mjs.log).debug("Moving label abc88 ", edge.id, edge.label, edgeLabels[edge.id], paths);
    let path = paths.updatedPath ? paths.updatedPath : paths.originalPath;
    const siteConfig = (0, _chunkDD37ZF33Mjs.getConfig2)();
    const { subGraphTitleTotalMargin } = (0, _chunkKW7S66XIMjs.getSubGraphTitleMargins)(siteConfig);
    if (edge.label) {
        const el = edgeLabels[edge.id];
        let x = edge.x;
        let y = edge.y;
        if (path) {
            const pos = (0, _chunkI7ZFS43CMjs.utils_default).calcLabelPosition(path);
            (0, _chunkDD37ZF33Mjs.log).debug("Moving label " + edge.label + " from (", x, ",", y, ") to (", pos.x, ",", pos.y, ") abc88");
            if (paths.updatedPath) {
                x = pos.x;
                y = pos.y;
            }
        }
        el.attr("transform", `translate(${x}, ${y + subGraphTitleTotalMargin / 2})`);
    }
    if (edge.startLabelLeft) {
        const el = terminalLabels[edge.id].startLeft;
        let x = edge.x;
        let y = edge.y;
        if (path) {
            const pos = (0, _chunkI7ZFS43CMjs.utils_default).calcTerminalLabelPosition(edge.arrowTypeStart ? 10 : 0, "start_left", path);
            x = pos.x;
            y = pos.y;
        }
        el.attr("transform", `translate(${x}, ${y})`);
    }
    if (edge.startLabelRight) {
        const el = terminalLabels[edge.id].startRight;
        let x = edge.x;
        let y = edge.y;
        if (path) {
            const pos = (0, _chunkI7ZFS43CMjs.utils_default).calcTerminalLabelPosition(edge.arrowTypeStart ? 10 : 0, "start_right", path);
            x = pos.x;
            y = pos.y;
        }
        el.attr("transform", `translate(${x}, ${y})`);
    }
    if (edge.endLabelLeft) {
        const el = terminalLabels[edge.id].endLeft;
        let x = edge.x;
        let y = edge.y;
        if (path) {
            const pos = (0, _chunkI7ZFS43CMjs.utils_default).calcTerminalLabelPosition(edge.arrowTypeEnd ? 10 : 0, "end_left", path);
            x = pos.x;
            y = pos.y;
        }
        el.attr("transform", `translate(${x}, ${y})`);
    }
    if (edge.endLabelRight) {
        const el = terminalLabels[edge.id].endRight;
        let x = edge.x;
        let y = edge.y;
        if (path) {
            const pos = (0, _chunkI7ZFS43CMjs.utils_default).calcTerminalLabelPosition(edge.arrowTypeEnd ? 10 : 0, "end_right", path);
            x = pos.x;
            y = pos.y;
        }
        el.attr("transform", `translate(${x}, ${y})`);
    }
}, "positionEdgeLabel");
var outsideNode = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((node, point2)=>{
    const x = node.x;
    const y = node.y;
    const dx = Math.abs(point2.x - x);
    const dy = Math.abs(point2.y - y);
    const w = node.width / 2;
    const h = node.height / 2;
    if (dx >= w || dy >= h) return true;
    return false;
}, "outsideNode");
var intersection = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((node, outsidePoint, insidePoint)=>{
    (0, _chunkDD37ZF33Mjs.log).debug(`intersection calc abc89:
  outsidePoint: ${JSON.stringify(outsidePoint)}
  insidePoint : ${JSON.stringify(insidePoint)}
  node        : x:${node.x} y:${node.y} w:${node.width} h:${node.height}`);
    const x = node.x;
    const y = node.y;
    const dx = Math.abs(x - insidePoint.x);
    const w = node.width / 2;
    let r = insidePoint.x < outsidePoint.x ? w - dx : w + dx;
    const h = node.height / 2;
    const Q = Math.abs(outsidePoint.y - insidePoint.y);
    const R = Math.abs(outsidePoint.x - insidePoint.x);
    if (Math.abs(y - outsidePoint.y) * w > Math.abs(x - outsidePoint.x) * h) {
        let q = insidePoint.y < outsidePoint.y ? outsidePoint.y - h - y : y - h - outsidePoint.y;
        r = R * q / Q;
        const res = {
            x: insidePoint.x < outsidePoint.x ? insidePoint.x + r : insidePoint.x - R + r,
            y: insidePoint.y < outsidePoint.y ? insidePoint.y + Q - q : insidePoint.y - Q + q
        };
        if (r === 0) {
            res.x = outsidePoint.x;
            res.y = outsidePoint.y;
        }
        if (R === 0) res.x = outsidePoint.x;
        if (Q === 0) res.y = outsidePoint.y;
        (0, _chunkDD37ZF33Mjs.log).debug(`abc89 topp/bott calc, Q ${Q}, q ${q}, R ${R}, r ${r}`, res);
        return res;
    } else {
        if (insidePoint.x < outsidePoint.x) r = outsidePoint.x - w - x;
        else r = x - w - outsidePoint.x;
        let q = Q * r / R;
        let _x = insidePoint.x < outsidePoint.x ? insidePoint.x + R - r : insidePoint.x - R + r;
        let _y = insidePoint.y < outsidePoint.y ? insidePoint.y + q : insidePoint.y - q;
        (0, _chunkDD37ZF33Mjs.log).debug(`sides calc abc89, Q ${Q}, q ${q}, R ${R}, r ${r}`, {
            _x,
            _y
        });
        if (r === 0) {
            _x = outsidePoint.x;
            _y = outsidePoint.y;
        }
        if (R === 0) _x = outsidePoint.x;
        if (Q === 0) _y = outsidePoint.y;
        return {
            x: _x,
            y: _y
        };
    }
}, "intersection");
var cutPathAtIntersect = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((_points, boundaryNode)=>{
    (0, _chunkDD37ZF33Mjs.log).debug("abc88 cutPathAtIntersect", _points, boundaryNode);
    let points = [];
    let lastPointOutside = _points[0];
    let isInside = false;
    _points.forEach((point2)=>{
        if (!outsideNode(boundaryNode, point2) && !isInside) {
            const inter = intersection(boundaryNode, lastPointOutside, point2);
            let pointPresent = false;
            points.forEach((p)=>{
                pointPresent = pointPresent || p.x === inter.x && p.y === inter.y;
            });
            if (!points.some((e)=>e.x === inter.x && e.y === inter.y)) points.push(inter);
            isInside = true;
        } else {
            lastPointOutside = point2;
            if (!isInside) points.push(point2);
        }
    });
    return points;
}, "cutPathAtIntersect");
var insertEdge = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(elem, e, edge, clusterDb, diagramType, graph, id) {
    let points = edge.points;
    (0, _chunkDD37ZF33Mjs.log).debug("abc88 InsertEdge: edge=", edge, "e=", e);
    let pointsHasChanged = false;
    const tail = graph.node(e.v);
    var head = graph.node(e.w);
    if (head?.intersect && tail?.intersect) {
        points = points.slice(1, edge.points.length - 1);
        points.unshift(tail.intersect(points[0]));
        points.push(head.intersect(points[points.length - 1]));
    }
    if (edge.toCluster) {
        (0, _chunkDD37ZF33Mjs.log).debug("to cluster abc88", clusterDb[edge.toCluster]);
        points = cutPathAtIntersect(edge.points, clusterDb[edge.toCluster].node);
        pointsHasChanged = true;
    }
    if (edge.fromCluster) {
        (0, _chunkDD37ZF33Mjs.log).debug("from cluster abc88", clusterDb[edge.fromCluster]);
        points = cutPathAtIntersect(points.reverse(), clusterDb[edge.fromCluster].node).reverse();
        pointsHasChanged = true;
    }
    const lineData = points.filter((p)=>!Number.isNaN(p.y));
    let curve = (0, _chunkDD37ZF33Mjs.basis_default);
    if (edge.curve && (diagramType === "graph" || diagramType === "flowchart")) curve = edge.curve;
    const { x, y } = (0, _chunkKW7S66XIMjs.getLineFunctionsWithOffset)(edge);
    const lineFunction = (0, _chunkDD37ZF33Mjs.line_default)().x(x).y(y).curve(curve);
    let strokeClasses;
    switch(edge.thickness){
        case "normal":
            strokeClasses = "edge-thickness-normal";
            break;
        case "thick":
            strokeClasses = "edge-thickness-thick";
            break;
        case "invisible":
            strokeClasses = "edge-thickness-thick";
            break;
        default:
            strokeClasses = "";
    }
    switch(edge.pattern){
        case "solid":
            strokeClasses += " edge-pattern-solid";
            break;
        case "dotted":
            strokeClasses += " edge-pattern-dotted";
            break;
        case "dashed":
            strokeClasses += " edge-pattern-dashed";
            break;
    }
    const svgPath = elem.append("path").attr("d", lineFunction(lineData)).attr("id", edge.id).attr("class", " " + strokeClasses + (edge.classes ? " " + edge.classes : "")).attr("style", edge.style);
    let url = "";
    if ((0, _chunkDD37ZF33Mjs.getConfig2)().flowchart.arrowMarkerAbsolute || (0, _chunkDD37ZF33Mjs.getConfig2)().state.arrowMarkerAbsolute) {
        url = window.location.protocol + "//" + window.location.host + window.location.pathname + window.location.search;
        url = url.replace(/\(/g, "\\(");
        url = url.replace(/\)/g, "\\)");
    }
    addEdgeMarkers(svgPath, edge, url, id, diagramType);
    let paths = {};
    if (pointsHasChanged) paths.updatedPath = points;
    paths.originalPath = edge.points;
    return paths;
}, "insertEdge");

},{"./chunk-HKQCUR3C.mjs":"fcjSH","./chunk-KW7S66XI.mjs":"98JMR","./chunk-YP6PVJQ3.mjs":"21NKC","./chunk-I7ZFS43C.mjs":"huUtc","./chunk-DD37ZF33.mjs":"f4pI5","./chunk-DLQEHMXD.mjs":"h5YRf","@parcel/transformer-js/src/esmodule-helpers.js":"1cdyk"}]},["1JRUe"], null, "parcelRequire6955", {})

//# sourceMappingURL=classDiagram-v2-YNGOW5FH.31770452.js.map
