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
})({"hYQQL":[function(require,module,exports,__globalThis) {
var global = arguments[3];
var HMR_HOST = null;
var HMR_PORT = 1234;
var HMR_SERVER_PORT = 1234;
var HMR_SECURE = false;
var HMR_ENV_HASH = "febfef3c5b467b16";
var HMR_USE_SSE = false;
module.bundle.HMR_BUNDLE_ID = "cc006fd40ca1e1fe";
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

},{}],"1O5c5":[function(require,module,exports,__globalThis) {
var parcelHelpers = require("@parcel/transformer-js/src/esmodule-helpers.js");
parcelHelpers.defineInteropFlag(exports);
parcelHelpers.export(exports, "diagram", ()=>diagram);
var _chunkDD37ZF33Mjs = require("./chunk-DD37ZF33.mjs");
var _chunkDLQEHMXDMjs = require("./chunk-DLQEHMXD.mjs");
// src/diagrams/quadrant-chart/parser/quadrant.jison
var parser = function() {
    var o = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(k, v, o2, l) {
        for(o2 = o2 || {}, l = k.length; l--; o2[k[l]] = v);
        return o2;
    }, "o"), $V0 = [
        1,
        3
    ], $V1 = [
        1,
        4
    ], $V2 = [
        1,
        5
    ], $V3 = [
        1,
        6
    ], $V4 = [
        1,
        7
    ], $V5 = [
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
    ], $V6 = [
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
    ], $V7 = [
        55,
        56,
        57
    ], $V8 = [
        2,
        36
    ], $V9 = [
        1,
        37
    ], $Va = [
        1,
        36
    ], $Vb = [
        1,
        38
    ], $Vc = [
        1,
        35
    ], $Vd = [
        1,
        43
    ], $Ve = [
        1,
        41
    ], $Vf = [
        1,
        14
    ], $Vg = [
        1,
        23
    ], $Vh = [
        1,
        18
    ], $Vi = [
        1,
        19
    ], $Vj = [
        1,
        20
    ], $Vk = [
        1,
        21
    ], $Vl = [
        1,
        22
    ], $Vm = [
        1,
        24
    ], $Vn = [
        1,
        25
    ], $Vo = [
        1,
        26
    ], $Vp = [
        1,
        27
    ], $Vq = [
        1,
        28
    ], $Vr = [
        1,
        29
    ], $Vs = [
        1,
        32
    ], $Vt = [
        1,
        33
    ], $Vu = [
        1,
        34
    ], $Vv = [
        1,
        39
    ], $Vw = [
        1,
        40
    ], $Vx = [
        1,
        42
    ], $Vy = [
        1,
        44
    ], $Vz = [
        1,
        62
    ], $VA = [
        1,
        61
    ], $VB = [
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
    ], $VC = [
        1,
        65
    ], $VD = [
        1,
        66
    ], $VE = [
        1,
        67
    ], $VF = [
        1,
        68
    ], $VG = [
        1,
        69
    ], $VH = [
        1,
        70
    ], $VI = [
        1,
        71
    ], $VJ = [
        1,
        72
    ], $VK = [
        1,
        73
    ], $VL = [
        1,
        74
    ], $VM = [
        1,
        75
    ], $VN = [
        1,
        76
    ], $VO = [
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
    ], $VP = [
        1,
        90
    ], $VQ = [
        1,
        91
    ], $VR = [
        1,
        92
    ], $VS = [
        1,
        99
    ], $VT = [
        1,
        93
    ], $VU = [
        1,
        96
    ], $VV = [
        1,
        94
    ], $VW = [
        1,
        95
    ], $VX = [
        1,
        97
    ], $VY = [
        1,
        98
    ], $VZ = [
        1,
        102
    ], $V_ = [
        10,
        55,
        56,
        57
    ], $V$ = [
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
    ];
    var parser2 = {
        trace: /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function trace() {}, "trace"),
        yy: {},
        symbols_: {
            "error": 2,
            "idStringToken": 3,
            "ALPHA": 4,
            "NUM": 5,
            "NODE_STRING": 6,
            "DOWN": 7,
            "MINUS": 8,
            "DEFAULT": 9,
            "COMMA": 10,
            "COLON": 11,
            "AMP": 12,
            "BRKT": 13,
            "MULT": 14,
            "UNICODE_TEXT": 15,
            "styleComponent": 16,
            "UNIT": 17,
            "SPACE": 18,
            "STYLE": 19,
            "PCT": 20,
            "idString": 21,
            "style": 22,
            "stylesOpt": 23,
            "classDefStatement": 24,
            "CLASSDEF": 25,
            "start": 26,
            "eol": 27,
            "QUADRANT": 28,
            "document": 29,
            "line": 30,
            "statement": 31,
            "axisDetails": 32,
            "quadrantDetails": 33,
            "points": 34,
            "title": 35,
            "title_value": 36,
            "acc_title": 37,
            "acc_title_value": 38,
            "acc_descr": 39,
            "acc_descr_value": 40,
            "acc_descr_multiline_value": 41,
            "section": 42,
            "text": 43,
            "point_start": 44,
            "point_x": 45,
            "point_y": 46,
            "class_name": 47,
            "X-AXIS": 48,
            "AXIS-TEXT-DELIMITER": 49,
            "Y-AXIS": 50,
            "QUADRANT_1": 51,
            "QUADRANT_2": 52,
            "QUADRANT_3": 53,
            "QUADRANT_4": 54,
            "NEWLINE": 55,
            "SEMI": 56,
            "EOF": 57,
            "alphaNumToken": 58,
            "textNoTagsToken": 59,
            "STR": 60,
            "MD_STR": 61,
            "alphaNum": 62,
            "PUNCTUATION": 63,
            "PLUS": 64,
            "EQUALS": 65,
            "DOT": 66,
            "UNDERSCORE": 67,
            "$accept": 0,
            "$end": 1
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
        performAction: /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function anonymous(yytext, yyleng, yylineno, yy, yystate, $$, _$) {
            var $0 = $$.length - 1;
            switch(yystate){
                case 23:
                    this.$ = $$[$0];
                    break;
                case 24:
                    this.$ = $$[$0 - 1] + "" + $$[$0];
                    break;
                case 26:
                    this.$ = $$[$0 - 1] + $$[$0];
                    break;
                case 27:
                    this.$ = [
                        $$[$0].trim()
                    ];
                    break;
                case 28:
                    $$[$0 - 2].push($$[$0].trim());
                    this.$ = $$[$0 - 2];
                    break;
                case 29:
                    this.$ = $$[$0 - 4];
                    yy.addClass($$[$0 - 2], $$[$0]);
                    break;
                case 37:
                    this.$ = [];
                    break;
                case 42:
                    this.$ = $$[$0].trim();
                    yy.setDiagramTitle(this.$);
                    break;
                case 43:
                    this.$ = $$[$0].trim();
                    yy.setAccTitle(this.$);
                    break;
                case 44:
                case 45:
                    this.$ = $$[$0].trim();
                    yy.setAccDescription(this.$);
                    break;
                case 46:
                    yy.addSection($$[$0].substr(8));
                    this.$ = $$[$0].substr(8);
                    break;
                case 47:
                    yy.addPoint($$[$0 - 3], "", $$[$0 - 1], $$[$0], []);
                    break;
                case 48:
                    yy.addPoint($$[$0 - 4], $$[$0 - 3], $$[$0 - 1], $$[$0], []);
                    break;
                case 49:
                    yy.addPoint($$[$0 - 4], "", $$[$0 - 2], $$[$0 - 1], $$[$0]);
                    break;
                case 50:
                    yy.addPoint($$[$0 - 5], $$[$0 - 4], $$[$0 - 2], $$[$0 - 1], $$[$0]);
                    break;
                case 51:
                    yy.setXAxisLeftText($$[$0 - 2]);
                    yy.setXAxisRightText($$[$0]);
                    break;
                case 52:
                    $$[$0 - 1].text += " \u27F6 ";
                    yy.setXAxisLeftText($$[$0 - 1]);
                    break;
                case 53:
                    yy.setXAxisLeftText($$[$0]);
                    break;
                case 54:
                    yy.setYAxisBottomText($$[$0 - 2]);
                    yy.setYAxisTopText($$[$0]);
                    break;
                case 55:
                    $$[$0 - 1].text += " \u27F6 ";
                    yy.setYAxisBottomText($$[$0 - 1]);
                    break;
                case 56:
                    yy.setYAxisBottomText($$[$0]);
                    break;
                case 57:
                    yy.setQuadrant1Text($$[$0]);
                    break;
                case 58:
                    yy.setQuadrant2Text($$[$0]);
                    break;
                case 59:
                    yy.setQuadrant3Text($$[$0]);
                    break;
                case 60:
                    yy.setQuadrant4Text($$[$0]);
                    break;
                case 64:
                    this.$ = {
                        text: $$[$0],
                        type: "text"
                    };
                    break;
                case 65:
                    this.$ = {
                        text: $$[$0 - 1].text + "" + $$[$0],
                        type: $$[$0 - 1].type
                    };
                    break;
                case 66:
                    this.$ = {
                        text: $$[$0],
                        type: "text"
                    };
                    break;
                case 67:
                    this.$ = {
                        text: $$[$0],
                        type: "markdown"
                    };
                    break;
                case 68:
                    this.$ = $$[$0];
                    break;
                case 69:
                    this.$ = $$[$0 - 1] + "" + $$[$0];
                    break;
            }
        }, "anonymous"),
        table: [
            {
                18: $V0,
                26: 1,
                27: 2,
                28: $V1,
                55: $V2,
                56: $V3,
                57: $V4
            },
            {
                1: [
                    3
                ]
            },
            {
                18: $V0,
                26: 8,
                27: 2,
                28: $V1,
                55: $V2,
                56: $V3,
                57: $V4
            },
            {
                18: $V0,
                26: 9,
                27: 2,
                28: $V1,
                55: $V2,
                56: $V3,
                57: $V4
            },
            o($V5, [
                2,
                33
            ], {
                29: 10
            }),
            o($V6, [
                2,
                61
            ]),
            o($V6, [
                2,
                62
            ]),
            o($V6, [
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
            o($V7, $V8, {
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
                4: $V9,
                5: $Va,
                10: $Vb,
                12: $Vc,
                13: $Vd,
                14: $Ve,
                18: $Vf,
                25: $Vg,
                35: $Vh,
                37: $Vi,
                39: $Vj,
                41: $Vk,
                42: $Vl,
                48: $Vm,
                50: $Vn,
                51: $Vo,
                52: $Vp,
                53: $Vq,
                54: $Vr,
                60: $Vs,
                61: $Vt,
                63: $Vu,
                64: $Vv,
                65: $Vw,
                66: $Vx,
                67: $Vy
            }),
            o($V5, [
                2,
                34
            ]),
            {
                27: 45,
                55: $V2,
                56: $V3,
                57: $V4
            },
            o($V7, [
                2,
                37
            ]),
            o($V7, $V8, {
                24: 13,
                32: 15,
                33: 16,
                34: 17,
                43: 30,
                58: 31,
                31: 46,
                4: $V9,
                5: $Va,
                10: $Vb,
                12: $Vc,
                13: $Vd,
                14: $Ve,
                18: $Vf,
                25: $Vg,
                35: $Vh,
                37: $Vi,
                39: $Vj,
                41: $Vk,
                42: $Vl,
                48: $Vm,
                50: $Vn,
                51: $Vo,
                52: $Vp,
                53: $Vq,
                54: $Vr,
                60: $Vs,
                61: $Vt,
                63: $Vu,
                64: $Vv,
                65: $Vw,
                66: $Vx,
                67: $Vy
            }),
            o($V7, [
                2,
                39
            ]),
            o($V7, [
                2,
                40
            ]),
            o($V7, [
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
            o($V7, [
                2,
                45
            ]),
            o($V7, [
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
                4: $V9,
                5: $Va,
                10: $Vb,
                12: $Vc,
                13: $Vd,
                14: $Ve,
                43: 51,
                58: 31,
                60: $Vs,
                61: $Vt,
                63: $Vu,
                64: $Vv,
                65: $Vw,
                66: $Vx,
                67: $Vy
            },
            {
                4: $V9,
                5: $Va,
                10: $Vb,
                12: $Vc,
                13: $Vd,
                14: $Ve,
                43: 52,
                58: 31,
                60: $Vs,
                61: $Vt,
                63: $Vu,
                64: $Vv,
                65: $Vw,
                66: $Vx,
                67: $Vy
            },
            {
                4: $V9,
                5: $Va,
                10: $Vb,
                12: $Vc,
                13: $Vd,
                14: $Ve,
                43: 53,
                58: 31,
                60: $Vs,
                61: $Vt,
                63: $Vu,
                64: $Vv,
                65: $Vw,
                66: $Vx,
                67: $Vy
            },
            {
                4: $V9,
                5: $Va,
                10: $Vb,
                12: $Vc,
                13: $Vd,
                14: $Ve,
                43: 54,
                58: 31,
                60: $Vs,
                61: $Vt,
                63: $Vu,
                64: $Vv,
                65: $Vw,
                66: $Vx,
                67: $Vy
            },
            {
                4: $V9,
                5: $Va,
                10: $Vb,
                12: $Vc,
                13: $Vd,
                14: $Ve,
                43: 55,
                58: 31,
                60: $Vs,
                61: $Vt,
                63: $Vu,
                64: $Vv,
                65: $Vw,
                66: $Vx,
                67: $Vy
            },
            {
                4: $V9,
                5: $Va,
                10: $Vb,
                12: $Vc,
                13: $Vd,
                14: $Ve,
                43: 56,
                58: 31,
                60: $Vs,
                61: $Vt,
                63: $Vu,
                64: $Vv,
                65: $Vw,
                66: $Vx,
                67: $Vy
            },
            {
                4: $V9,
                5: $Va,
                8: $Vz,
                10: $Vb,
                12: $Vc,
                13: $Vd,
                14: $Ve,
                18: $VA,
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
                63: $Vu,
                64: $Vv,
                65: $Vw,
                66: $Vx,
                67: $Vy
            },
            o($VB, [
                2,
                64
            ]),
            o($VB, [
                2,
                66
            ]),
            o($VB, [
                2,
                67
            ]),
            o($VB, [
                2,
                70
            ]),
            o($VB, [
                2,
                71
            ]),
            o($VB, [
                2,
                72
            ]),
            o($VB, [
                2,
                73
            ]),
            o($VB, [
                2,
                74
            ]),
            o($VB, [
                2,
                75
            ]),
            o($VB, [
                2,
                76
            ]),
            o($VB, [
                2,
                77
            ]),
            o($VB, [
                2,
                78
            ]),
            o($VB, [
                2,
                79
            ]),
            o($VB, [
                2,
                80
            ]),
            o($V5, [
                2,
                35
            ]),
            o($V7, [
                2,
                38
            ]),
            o($V7, [
                2,
                42
            ]),
            o($V7, [
                2,
                43
            ]),
            o($V7, [
                2,
                44
            ]),
            {
                3: 64,
                4: $VC,
                5: $VD,
                6: $VE,
                7: $VF,
                8: $VG,
                9: $VH,
                10: $VI,
                11: $VJ,
                12: $VK,
                13: $VL,
                14: $VM,
                15: $VN,
                21: 63
            },
            o($V7, [
                2,
                53
            ], {
                59: 59,
                58: 60,
                4: $V9,
                5: $Va,
                8: $Vz,
                10: $Vb,
                12: $Vc,
                13: $Vd,
                14: $Ve,
                18: $VA,
                49: [
                    1,
                    77
                ],
                63: $Vu,
                64: $Vv,
                65: $Vw,
                66: $Vx,
                67: $Vy
            }),
            o($V7, [
                2,
                56
            ], {
                59: 59,
                58: 60,
                4: $V9,
                5: $Va,
                8: $Vz,
                10: $Vb,
                12: $Vc,
                13: $Vd,
                14: $Ve,
                18: $VA,
                49: [
                    1,
                    78
                ],
                63: $Vu,
                64: $Vv,
                65: $Vw,
                66: $Vx,
                67: $Vy
            }),
            o($V7, [
                2,
                57
            ], {
                59: 59,
                58: 60,
                4: $V9,
                5: $Va,
                8: $Vz,
                10: $Vb,
                12: $Vc,
                13: $Vd,
                14: $Ve,
                18: $VA,
                63: $Vu,
                64: $Vv,
                65: $Vw,
                66: $Vx,
                67: $Vy
            }),
            o($V7, [
                2,
                58
            ], {
                59: 59,
                58: 60,
                4: $V9,
                5: $Va,
                8: $Vz,
                10: $Vb,
                12: $Vc,
                13: $Vd,
                14: $Ve,
                18: $VA,
                63: $Vu,
                64: $Vv,
                65: $Vw,
                66: $Vx,
                67: $Vy
            }),
            o($V7, [
                2,
                59
            ], {
                59: 59,
                58: 60,
                4: $V9,
                5: $Va,
                8: $Vz,
                10: $Vb,
                12: $Vc,
                13: $Vd,
                14: $Ve,
                18: $VA,
                63: $Vu,
                64: $Vv,
                65: $Vw,
                66: $Vx,
                67: $Vy
            }),
            o($V7, [
                2,
                60
            ], {
                59: 59,
                58: 60,
                4: $V9,
                5: $Va,
                8: $Vz,
                10: $Vb,
                12: $Vc,
                13: $Vd,
                14: $Ve,
                18: $VA,
                63: $Vu,
                64: $Vv,
                65: $Vw,
                66: $Vx,
                67: $Vy
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
            o($VB, [
                2,
                65
            ]),
            o($VB, [
                2,
                81
            ]),
            o($VB, [
                2,
                82
            ]),
            o($VB, [
                2,
                83
            ]),
            {
                3: 82,
                4: $VC,
                5: $VD,
                6: $VE,
                7: $VF,
                8: $VG,
                9: $VH,
                10: $VI,
                11: $VJ,
                12: $VK,
                13: $VL,
                14: $VM,
                15: $VN,
                18: [
                    1,
                    81
                ]
            },
            o($VO, [
                2,
                23
            ]),
            o($VO, [
                2,
                1
            ]),
            o($VO, [
                2,
                2
            ]),
            o($VO, [
                2,
                3
            ]),
            o($VO, [
                2,
                4
            ]),
            o($VO, [
                2,
                5
            ]),
            o($VO, [
                2,
                6
            ]),
            o($VO, [
                2,
                7
            ]),
            o($VO, [
                2,
                8
            ]),
            o($VO, [
                2,
                9
            ]),
            o($VO, [
                2,
                10
            ]),
            o($VO, [
                2,
                11
            ]),
            o($VO, [
                2,
                12
            ]),
            o($V7, [
                2,
                52
            ], {
                58: 31,
                43: 83,
                4: $V9,
                5: $Va,
                10: $Vb,
                12: $Vc,
                13: $Vd,
                14: $Ve,
                60: $Vs,
                61: $Vt,
                63: $Vu,
                64: $Vv,
                65: $Vw,
                66: $Vx,
                67: $Vy
            }),
            o($V7, [
                2,
                55
            ], {
                58: 31,
                43: 84,
                4: $V9,
                5: $Va,
                10: $Vb,
                12: $Vc,
                13: $Vd,
                14: $Ve,
                60: $Vs,
                61: $Vt,
                63: $Vu,
                64: $Vv,
                65: $Vw,
                66: $Vx,
                67: $Vy
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
                4: $VP,
                5: $VQ,
                6: $VR,
                8: $VS,
                11: $VT,
                13: $VU,
                16: 89,
                17: $VV,
                18: $VW,
                19: $VX,
                20: $VY,
                22: 88,
                23: 87
            },
            o($VO, [
                2,
                24
            ]),
            o($V7, [
                2,
                51
            ], {
                59: 59,
                58: 60,
                4: $V9,
                5: $Va,
                8: $Vz,
                10: $Vb,
                12: $Vc,
                13: $Vd,
                14: $Ve,
                18: $VA,
                63: $Vu,
                64: $Vv,
                65: $Vw,
                66: $Vx,
                67: $Vy
            }),
            o($V7, [
                2,
                54
            ], {
                59: 59,
                58: 60,
                4: $V9,
                5: $Va,
                8: $Vz,
                10: $Vb,
                12: $Vc,
                13: $Vd,
                14: $Ve,
                18: $VA,
                63: $Vu,
                64: $Vv,
                65: $Vw,
                66: $Vx,
                67: $Vy
            }),
            o($V7, [
                2,
                47
            ], {
                22: 88,
                16: 89,
                23: 100,
                4: $VP,
                5: $VQ,
                6: $VR,
                8: $VS,
                11: $VT,
                13: $VU,
                17: $VV,
                18: $VW,
                19: $VX,
                20: $VY
            }),
            {
                46: [
                    1,
                    101
                ]
            },
            o($V7, [
                2,
                29
            ], {
                10: $VZ
            }),
            o($V_, [
                2,
                27
            ], {
                16: 103,
                4: $VP,
                5: $VQ,
                6: $VR,
                8: $VS,
                11: $VT,
                13: $VU,
                17: $VV,
                18: $VW,
                19: $VX,
                20: $VY
            }),
            o($V$, [
                2,
                25
            ]),
            o($V$, [
                2,
                13
            ]),
            o($V$, [
                2,
                14
            ]),
            o($V$, [
                2,
                15
            ]),
            o($V$, [
                2,
                16
            ]),
            o($V$, [
                2,
                17
            ]),
            o($V$, [
                2,
                18
            ]),
            o($V$, [
                2,
                19
            ]),
            o($V$, [
                2,
                20
            ]),
            o($V$, [
                2,
                21
            ]),
            o($V$, [
                2,
                22
            ]),
            o($V7, [
                2,
                49
            ], {
                10: $VZ
            }),
            o($V7, [
                2,
                48
            ], {
                22: 88,
                16: 89,
                23: 104,
                4: $VP,
                5: $VQ,
                6: $VR,
                8: $VS,
                11: $VT,
                13: $VU,
                17: $VV,
                18: $VW,
                19: $VX,
                20: $VY
            }),
            {
                4: $VP,
                5: $VQ,
                6: $VR,
                8: $VS,
                11: $VT,
                13: $VU,
                16: 89,
                17: $VV,
                18: $VW,
                19: $VX,
                20: $VY,
                22: 105
            },
            o($V$, [
                2,
                26
            ]),
            o($V7, [
                2,
                50
            ], {
                10: $VZ
            }),
            o($V_, [
                2,
                28
            ], {
                16: 103,
                4: $VP,
                5: $VQ,
                6: $VR,
                8: $VS,
                11: $VT,
                13: $VU,
                17: $VV,
                18: $VW,
                19: $VX,
                20: $VY
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
            options: {
                "case-insensitive": true
            },
            performAction: /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function anonymous(yy, yy_, $avoiding_name_collisions, YY_START) {
                var YYSTATE = YY_START;
                switch($avoiding_name_collisions){
                    case 0:
                        break;
                    case 1:
                        break;
                    case 2:
                        return 55;
                    case 3:
                        break;
                    case 4:
                        this.begin("title");
                        return 35;
                    case 5:
                        this.popState();
                        return "title_value";
                    case 6:
                        this.begin("acc_title");
                        return 37;
                    case 7:
                        this.popState();
                        return "acc_title_value";
                    case 8:
                        this.begin("acc_descr");
                        return 39;
                    case 9:
                        this.popState();
                        return "acc_descr_value";
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
                        this.popState();
                        return 47;
                    case 29:
                        this.begin("point_start");
                        return 44;
                    case 30:
                        this.begin("point_x");
                        return 45;
                    case 31:
                        this.popState();
                        break;
                    case 32:
                        this.popState();
                        this.begin("point_y");
                        break;
                    case 33:
                        this.popState();
                        return 46;
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
                "class_name": {
                    "rules": [
                        28
                    ],
                    "inclusive": false
                },
                "point_y": {
                    "rules": [
                        33
                    ],
                    "inclusive": false
                },
                "point_x": {
                    "rules": [
                        32
                    ],
                    "inclusive": false
                },
                "point_start": {
                    "rules": [
                        30,
                        31
                    ],
                    "inclusive": false
                },
                "acc_descr_multiline": {
                    "rules": [
                        11,
                        12
                    ],
                    "inclusive": false
                },
                "acc_descr": {
                    "rules": [
                        9
                    ],
                    "inclusive": false
                },
                "acc_title": {
                    "rules": [
                        7
                    ],
                    "inclusive": false
                },
                "title": {
                    "rules": [
                        5
                    ],
                    "inclusive": false
                },
                "md_string": {
                    "rules": [
                        22,
                        23
                    ],
                    "inclusive": false
                },
                "string": {
                    "rules": [
                        25,
                        26
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
var quadrant_default = parser;
// src/diagrams/quadrant-chart/quadrantBuilder.ts
var defaultThemeVariables = (0, _chunkDD37ZF33Mjs.getThemeVariables)();
var QuadrantBuilder = class {
    constructor(){
        this.classes = /* @__PURE__ */ new Map();
        this.config = this.getDefaultConfig();
        this.themeConfig = this.getDefaultThemeConfig();
        this.data = this.getDefaultData();
    }
    static #_ = (0, _chunkDLQEHMXDMjs.__name)(this, "QuadrantBuilder");
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
            showXAxis: true,
            showYAxis: true,
            showTitle: true,
            chartHeight: (0, _chunkDD37ZF33Mjs.defaultConfig_default).quadrantChart?.chartWidth || 500,
            chartWidth: (0, _chunkDD37ZF33Mjs.defaultConfig_default).quadrantChart?.chartHeight || 500,
            titlePadding: (0, _chunkDD37ZF33Mjs.defaultConfig_default).quadrantChart?.titlePadding || 10,
            titleFontSize: (0, _chunkDD37ZF33Mjs.defaultConfig_default).quadrantChart?.titleFontSize || 20,
            quadrantPadding: (0, _chunkDD37ZF33Mjs.defaultConfig_default).quadrantChart?.quadrantPadding || 5,
            xAxisLabelPadding: (0, _chunkDD37ZF33Mjs.defaultConfig_default).quadrantChart?.xAxisLabelPadding || 5,
            yAxisLabelPadding: (0, _chunkDD37ZF33Mjs.defaultConfig_default).quadrantChart?.yAxisLabelPadding || 5,
            xAxisLabelFontSize: (0, _chunkDD37ZF33Mjs.defaultConfig_default).quadrantChart?.xAxisLabelFontSize || 16,
            yAxisLabelFontSize: (0, _chunkDD37ZF33Mjs.defaultConfig_default).quadrantChart?.yAxisLabelFontSize || 16,
            quadrantLabelFontSize: (0, _chunkDD37ZF33Mjs.defaultConfig_default).quadrantChart?.quadrantLabelFontSize || 16,
            quadrantTextTopPadding: (0, _chunkDD37ZF33Mjs.defaultConfig_default).quadrantChart?.quadrantTextTopPadding || 5,
            pointTextPadding: (0, _chunkDD37ZF33Mjs.defaultConfig_default).quadrantChart?.pointTextPadding || 5,
            pointLabelFontSize: (0, _chunkDD37ZF33Mjs.defaultConfig_default).quadrantChart?.pointLabelFontSize || 12,
            pointRadius: (0, _chunkDD37ZF33Mjs.defaultConfig_default).quadrantChart?.pointRadius || 5,
            xAxisPosition: (0, _chunkDD37ZF33Mjs.defaultConfig_default).quadrantChart?.xAxisPosition || "top",
            yAxisPosition: (0, _chunkDD37ZF33Mjs.defaultConfig_default).quadrantChart?.yAxisPosition || "left",
            quadrantInternalBorderStrokeWidth: (0, _chunkDD37ZF33Mjs.defaultConfig_default).quadrantChart?.quadrantInternalBorderStrokeWidth || 1,
            quadrantExternalBorderStrokeWidth: (0, _chunkDD37ZF33Mjs.defaultConfig_default).quadrantChart?.quadrantExternalBorderStrokeWidth || 2
        };
    }
    getDefaultThemeConfig() {
        return {
            quadrant1Fill: defaultThemeVariables.quadrant1Fill,
            quadrant2Fill: defaultThemeVariables.quadrant2Fill,
            quadrant3Fill: defaultThemeVariables.quadrant3Fill,
            quadrant4Fill: defaultThemeVariables.quadrant4Fill,
            quadrant1TextFill: defaultThemeVariables.quadrant1TextFill,
            quadrant2TextFill: defaultThemeVariables.quadrant2TextFill,
            quadrant3TextFill: defaultThemeVariables.quadrant3TextFill,
            quadrant4TextFill: defaultThemeVariables.quadrant4TextFill,
            quadrantPointFill: defaultThemeVariables.quadrantPointFill,
            quadrantPointTextFill: defaultThemeVariables.quadrantPointTextFill,
            quadrantXAxisTextFill: defaultThemeVariables.quadrantXAxisTextFill,
            quadrantYAxisTextFill: defaultThemeVariables.quadrantYAxisTextFill,
            quadrantTitleFill: defaultThemeVariables.quadrantTitleFill,
            quadrantInternalBorderStrokeFill: defaultThemeVariables.quadrantInternalBorderStrokeFill,
            quadrantExternalBorderStrokeFill: defaultThemeVariables.quadrantExternalBorderStrokeFill
        };
    }
    clear() {
        this.config = this.getDefaultConfig();
        this.themeConfig = this.getDefaultThemeConfig();
        this.data = this.getDefaultData();
        this.classes = /* @__PURE__ */ new Map();
        (0, _chunkDD37ZF33Mjs.log).info("clear called");
    }
    setData(data) {
        this.data = {
            ...this.data,
            ...data
        };
    }
    addPoints(points) {
        this.data.points = [
            ...points,
            ...this.data.points
        ];
    }
    addClass(className, styles) {
        this.classes.set(className, styles);
    }
    setConfig(config2) {
        (0, _chunkDD37ZF33Mjs.log).trace("setConfig called with: ", config2);
        this.config = {
            ...this.config,
            ...config2
        };
    }
    setThemeConfig(themeConfig) {
        (0, _chunkDD37ZF33Mjs.log).trace("setThemeConfig called with: ", themeConfig);
        this.themeConfig = {
            ...this.themeConfig,
            ...themeConfig
        };
    }
    calculateSpace(xAxisPosition, showXAxis, showYAxis, showTitle) {
        const xAxisSpaceCalculation = this.config.xAxisLabelPadding * 2 + this.config.xAxisLabelFontSize;
        const xAxisSpace = {
            top: xAxisPosition === "top" && showXAxis ? xAxisSpaceCalculation : 0,
            bottom: xAxisPosition === "bottom" && showXAxis ? xAxisSpaceCalculation : 0
        };
        const yAxisSpaceCalculation = this.config.yAxisLabelPadding * 2 + this.config.yAxisLabelFontSize;
        const yAxisSpace = {
            left: this.config.yAxisPosition === "left" && showYAxis ? yAxisSpaceCalculation : 0,
            right: this.config.yAxisPosition === "right" && showYAxis ? yAxisSpaceCalculation : 0
        };
        const titleSpaceCalculation = this.config.titleFontSize + this.config.titlePadding * 2;
        const titleSpace = {
            top: showTitle ? titleSpaceCalculation : 0
        };
        const quadrantLeft = this.config.quadrantPadding + yAxisSpace.left;
        const quadrantTop = this.config.quadrantPadding + xAxisSpace.top + titleSpace.top;
        const quadrantWidth = this.config.chartWidth - this.config.quadrantPadding * 2 - yAxisSpace.left - yAxisSpace.right;
        const quadrantHeight = this.config.chartHeight - this.config.quadrantPadding * 2 - xAxisSpace.top - xAxisSpace.bottom - titleSpace.top;
        const quadrantHalfWidth = quadrantWidth / 2;
        const quadrantHalfHeight = quadrantHeight / 2;
        const quadrantSpace = {
            quadrantLeft,
            quadrantTop,
            quadrantWidth,
            quadrantHalfWidth,
            quadrantHeight,
            quadrantHalfHeight
        };
        return {
            xAxisSpace,
            yAxisSpace,
            titleSpace,
            quadrantSpace
        };
    }
    getAxisLabels(xAxisPosition, showXAxis, showYAxis, spaceData) {
        const { quadrantSpace, titleSpace } = spaceData;
        const { quadrantHalfHeight, quadrantHeight, quadrantLeft, quadrantHalfWidth, quadrantTop, quadrantWidth } = quadrantSpace;
        const drawXAxisLabelsInMiddle = Boolean(this.data.xAxisRightText);
        const drawYAxisLabelsInMiddle = Boolean(this.data.yAxisTopText);
        const axisLabels = [];
        if (this.data.xAxisLeftText && showXAxis) axisLabels.push({
            text: this.data.xAxisLeftText,
            fill: this.themeConfig.quadrantXAxisTextFill,
            x: quadrantLeft + (drawXAxisLabelsInMiddle ? quadrantHalfWidth / 2 : 0),
            y: xAxisPosition === "top" ? this.config.xAxisLabelPadding + titleSpace.top : this.config.xAxisLabelPadding + quadrantTop + quadrantHeight + this.config.quadrantPadding,
            fontSize: this.config.xAxisLabelFontSize,
            verticalPos: drawXAxisLabelsInMiddle ? "center" : "left",
            horizontalPos: "top",
            rotation: 0
        });
        if (this.data.xAxisRightText && showXAxis) axisLabels.push({
            text: this.data.xAxisRightText,
            fill: this.themeConfig.quadrantXAxisTextFill,
            x: quadrantLeft + quadrantHalfWidth + (drawXAxisLabelsInMiddle ? quadrantHalfWidth / 2 : 0),
            y: xAxisPosition === "top" ? this.config.xAxisLabelPadding + titleSpace.top : this.config.xAxisLabelPadding + quadrantTop + quadrantHeight + this.config.quadrantPadding,
            fontSize: this.config.xAxisLabelFontSize,
            verticalPos: drawXAxisLabelsInMiddle ? "center" : "left",
            horizontalPos: "top",
            rotation: 0
        });
        if (this.data.yAxisBottomText && showYAxis) axisLabels.push({
            text: this.data.yAxisBottomText,
            fill: this.themeConfig.quadrantYAxisTextFill,
            x: this.config.yAxisPosition === "left" ? this.config.yAxisLabelPadding : this.config.yAxisLabelPadding + quadrantLeft + quadrantWidth + this.config.quadrantPadding,
            y: quadrantTop + quadrantHeight - (drawYAxisLabelsInMiddle ? quadrantHalfHeight / 2 : 0),
            fontSize: this.config.yAxisLabelFontSize,
            verticalPos: drawYAxisLabelsInMiddle ? "center" : "left",
            horizontalPos: "top",
            rotation: -90
        });
        if (this.data.yAxisTopText && showYAxis) axisLabels.push({
            text: this.data.yAxisTopText,
            fill: this.themeConfig.quadrantYAxisTextFill,
            x: this.config.yAxisPosition === "left" ? this.config.yAxisLabelPadding : this.config.yAxisLabelPadding + quadrantLeft + quadrantWidth + this.config.quadrantPadding,
            y: quadrantTop + quadrantHalfHeight - (drawYAxisLabelsInMiddle ? quadrantHalfHeight / 2 : 0),
            fontSize: this.config.yAxisLabelFontSize,
            verticalPos: drawYAxisLabelsInMiddle ? "center" : "left",
            horizontalPos: "top",
            rotation: -90
        });
        return axisLabels;
    }
    getQuadrants(spaceData) {
        const { quadrantSpace } = spaceData;
        const { quadrantHalfHeight, quadrantLeft, quadrantHalfWidth, quadrantTop } = quadrantSpace;
        const quadrants = [
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
                x: quadrantLeft + quadrantHalfWidth,
                y: quadrantTop,
                width: quadrantHalfWidth,
                height: quadrantHalfHeight,
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
                x: quadrantLeft,
                y: quadrantTop,
                width: quadrantHalfWidth,
                height: quadrantHalfHeight,
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
                x: quadrantLeft,
                y: quadrantTop + quadrantHalfHeight,
                width: quadrantHalfWidth,
                height: quadrantHalfHeight,
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
                x: quadrantLeft + quadrantHalfWidth,
                y: quadrantTop + quadrantHalfHeight,
                width: quadrantHalfWidth,
                height: quadrantHalfHeight,
                fill: this.themeConfig.quadrant4Fill
            }
        ];
        for (const quadrant of quadrants){
            quadrant.text.x = quadrant.x + quadrant.width / 2;
            if (this.data.points.length === 0) {
                quadrant.text.y = quadrant.y + quadrant.height / 2;
                quadrant.text.horizontalPos = "middle";
            } else {
                quadrant.text.y = quadrant.y + this.config.quadrantTextTopPadding;
                quadrant.text.horizontalPos = "top";
            }
        }
        return quadrants;
    }
    getQuadrantPoints(spaceData) {
        const { quadrantSpace } = spaceData;
        const { quadrantHeight, quadrantLeft, quadrantTop, quadrantWidth } = quadrantSpace;
        const xAxis = (0, _chunkDD37ZF33Mjs.linear)().domain([
            0,
            1
        ]).range([
            quadrantLeft,
            quadrantWidth + quadrantLeft
        ]);
        const yAxis = (0, _chunkDD37ZF33Mjs.linear)().domain([
            0,
            1
        ]).range([
            quadrantHeight + quadrantTop,
            quadrantTop
        ]);
        const points = this.data.points.map((point)=>{
            const classStyles = this.classes.get(point.className);
            if (classStyles) point = {
                ...classStyles,
                ...point
            };
            const props = {
                x: xAxis(point.x),
                y: yAxis(point.y),
                fill: point.color ?? this.themeConfig.quadrantPointFill,
                radius: point.radius ?? this.config.pointRadius,
                text: {
                    text: point.text,
                    fill: this.themeConfig.quadrantPointTextFill,
                    x: xAxis(point.x),
                    y: yAxis(point.y) + this.config.pointTextPadding,
                    verticalPos: "center",
                    horizontalPos: "top",
                    fontSize: this.config.pointLabelFontSize,
                    rotation: 0
                },
                strokeColor: point.strokeColor ?? this.themeConfig.quadrantPointFill,
                strokeWidth: point.strokeWidth ?? "0px"
            };
            return props;
        });
        return points;
    }
    getBorders(spaceData) {
        const halfExternalBorderWidth = this.config.quadrantExternalBorderStrokeWidth / 2;
        const { quadrantSpace } = spaceData;
        const { quadrantHalfHeight, quadrantHeight, quadrantLeft, quadrantHalfWidth, quadrantTop, quadrantWidth } = quadrantSpace;
        const borderLines = [
            // top border
            {
                strokeFill: this.themeConfig.quadrantExternalBorderStrokeFill,
                strokeWidth: this.config.quadrantExternalBorderStrokeWidth,
                x1: quadrantLeft - halfExternalBorderWidth,
                y1: quadrantTop,
                x2: quadrantLeft + quadrantWidth + halfExternalBorderWidth,
                y2: quadrantTop
            },
            // right border
            {
                strokeFill: this.themeConfig.quadrantExternalBorderStrokeFill,
                strokeWidth: this.config.quadrantExternalBorderStrokeWidth,
                x1: quadrantLeft + quadrantWidth,
                y1: quadrantTop + halfExternalBorderWidth,
                x2: quadrantLeft + quadrantWidth,
                y2: quadrantTop + quadrantHeight - halfExternalBorderWidth
            },
            // bottom border
            {
                strokeFill: this.themeConfig.quadrantExternalBorderStrokeFill,
                strokeWidth: this.config.quadrantExternalBorderStrokeWidth,
                x1: quadrantLeft - halfExternalBorderWidth,
                y1: quadrantTop + quadrantHeight,
                x2: quadrantLeft + quadrantWidth + halfExternalBorderWidth,
                y2: quadrantTop + quadrantHeight
            },
            // left border
            {
                strokeFill: this.themeConfig.quadrantExternalBorderStrokeFill,
                strokeWidth: this.config.quadrantExternalBorderStrokeWidth,
                x1: quadrantLeft,
                y1: quadrantTop + halfExternalBorderWidth,
                x2: quadrantLeft,
                y2: quadrantTop + quadrantHeight - halfExternalBorderWidth
            },
            // vertical inner border
            {
                strokeFill: this.themeConfig.quadrantInternalBorderStrokeFill,
                strokeWidth: this.config.quadrantInternalBorderStrokeWidth,
                x1: quadrantLeft + quadrantHalfWidth,
                y1: quadrantTop + halfExternalBorderWidth,
                x2: quadrantLeft + quadrantHalfWidth,
                y2: quadrantTop + quadrantHeight - halfExternalBorderWidth
            },
            // horizontal inner border
            {
                strokeFill: this.themeConfig.quadrantInternalBorderStrokeFill,
                strokeWidth: this.config.quadrantInternalBorderStrokeWidth,
                x1: quadrantLeft + halfExternalBorderWidth,
                y1: quadrantTop + quadrantHalfHeight,
                x2: quadrantLeft + quadrantWidth - halfExternalBorderWidth,
                y2: quadrantTop + quadrantHalfHeight
            }
        ];
        return borderLines;
    }
    getTitle(showTitle) {
        if (showTitle) return {
            text: this.data.titleText,
            fill: this.themeConfig.quadrantTitleFill,
            fontSize: this.config.titleFontSize,
            horizontalPos: "top",
            verticalPos: "center",
            rotation: 0,
            y: this.config.titlePadding,
            x: this.config.chartWidth / 2
        };
        return;
    }
    build() {
        const showXAxis = this.config.showXAxis && !!(this.data.xAxisLeftText || this.data.xAxisRightText);
        const showYAxis = this.config.showYAxis && !!(this.data.yAxisTopText || this.data.yAxisBottomText);
        const showTitle = this.config.showTitle && !!this.data.titleText;
        const xAxisPosition = this.data.points.length > 0 ? "bottom" : this.config.xAxisPosition;
        const calculatedSpace = this.calculateSpace(xAxisPosition, showXAxis, showYAxis, showTitle);
        return {
            points: this.getQuadrantPoints(calculatedSpace),
            quadrants: this.getQuadrants(calculatedSpace),
            axisLabels: this.getAxisLabels(xAxisPosition, showXAxis, showYAxis, calculatedSpace),
            borderLines: this.getBorders(calculatedSpace),
            title: this.getTitle(showTitle)
        };
    }
};
// src/diagrams/quadrant-chart/utils.ts
var InvalidStyleError = class extends Error {
    static #_ = (0, _chunkDLQEHMXDMjs.__name)(this, "InvalidStyleError");
    constructor(style, value, type){
        super(`value for ${style} ${value} is invalid, please use a valid ${type}`);
        this.name = "InvalidStyleError";
    }
};
function validateHexCode(value) {
    return !/^#?([\dA-Fa-f]{6}|[\dA-Fa-f]{3})$/.test(value);
}
(0, _chunkDLQEHMXDMjs.__name)(validateHexCode, "validateHexCode");
function validateNumber(value) {
    return !/^\d+$/.test(value);
}
(0, _chunkDLQEHMXDMjs.__name)(validateNumber, "validateNumber");
function validateSizeInPixels(value) {
    return !/^\d+px$/.test(value);
}
(0, _chunkDLQEHMXDMjs.__name)(validateSizeInPixels, "validateSizeInPixels");
// src/diagrams/quadrant-chart/quadrantDb.ts
var config = (0, _chunkDD37ZF33Mjs.getConfig2)();
function textSanitizer(text) {
    return (0, _chunkDD37ZF33Mjs.sanitizeText)(text.trim(), config);
}
(0, _chunkDLQEHMXDMjs.__name)(textSanitizer, "textSanitizer");
var quadrantBuilder = new QuadrantBuilder();
function setQuadrant1Text(textObj) {
    quadrantBuilder.setData({
        quadrant1Text: textSanitizer(textObj.text)
    });
}
(0, _chunkDLQEHMXDMjs.__name)(setQuadrant1Text, "setQuadrant1Text");
function setQuadrant2Text(textObj) {
    quadrantBuilder.setData({
        quadrant2Text: textSanitizer(textObj.text)
    });
}
(0, _chunkDLQEHMXDMjs.__name)(setQuadrant2Text, "setQuadrant2Text");
function setQuadrant3Text(textObj) {
    quadrantBuilder.setData({
        quadrant3Text: textSanitizer(textObj.text)
    });
}
(0, _chunkDLQEHMXDMjs.__name)(setQuadrant3Text, "setQuadrant3Text");
function setQuadrant4Text(textObj) {
    quadrantBuilder.setData({
        quadrant4Text: textSanitizer(textObj.text)
    });
}
(0, _chunkDLQEHMXDMjs.__name)(setQuadrant4Text, "setQuadrant4Text");
function setXAxisLeftText(textObj) {
    quadrantBuilder.setData({
        xAxisLeftText: textSanitizer(textObj.text)
    });
}
(0, _chunkDLQEHMXDMjs.__name)(setXAxisLeftText, "setXAxisLeftText");
function setXAxisRightText(textObj) {
    quadrantBuilder.setData({
        xAxisRightText: textSanitizer(textObj.text)
    });
}
(0, _chunkDLQEHMXDMjs.__name)(setXAxisRightText, "setXAxisRightText");
function setYAxisTopText(textObj) {
    quadrantBuilder.setData({
        yAxisTopText: textSanitizer(textObj.text)
    });
}
(0, _chunkDLQEHMXDMjs.__name)(setYAxisTopText, "setYAxisTopText");
function setYAxisBottomText(textObj) {
    quadrantBuilder.setData({
        yAxisBottomText: textSanitizer(textObj.text)
    });
}
(0, _chunkDLQEHMXDMjs.__name)(setYAxisBottomText, "setYAxisBottomText");
function parseStyles(styles) {
    const stylesObject = {};
    for (const style of styles){
        const [key, value] = style.trim().split(/\s*:\s*/);
        if (key === "radius") {
            if (validateNumber(value)) throw new InvalidStyleError(key, value, "number");
            stylesObject.radius = parseInt(value);
        } else if (key === "color") {
            if (validateHexCode(value)) throw new InvalidStyleError(key, value, "hex code");
            stylesObject.color = value;
        } else if (key === "stroke-color") {
            if (validateHexCode(value)) throw new InvalidStyleError(key, value, "hex code");
            stylesObject.strokeColor = value;
        } else if (key === "stroke-width") {
            if (validateSizeInPixels(value)) throw new InvalidStyleError(key, value, "number of pixels (eg. 10px)");
            stylesObject.strokeWidth = value;
        } else throw new Error(`style named ${key} is not supported.`);
    }
    return stylesObject;
}
(0, _chunkDLQEHMXDMjs.__name)(parseStyles, "parseStyles");
function addPoint(textObj, className, x, y, styles) {
    const stylesObject = parseStyles(styles);
    quadrantBuilder.addPoints([
        {
            x,
            y,
            text: textSanitizer(textObj.text),
            className,
            ...stylesObject
        }
    ]);
}
(0, _chunkDLQEHMXDMjs.__name)(addPoint, "addPoint");
function addClass(className, styles) {
    quadrantBuilder.addClass(className, parseStyles(styles));
}
(0, _chunkDLQEHMXDMjs.__name)(addClass, "addClass");
function setWidth(width) {
    quadrantBuilder.setConfig({
        chartWidth: width
    });
}
(0, _chunkDLQEHMXDMjs.__name)(setWidth, "setWidth");
function setHeight(height) {
    quadrantBuilder.setConfig({
        chartHeight: height
    });
}
(0, _chunkDLQEHMXDMjs.__name)(setHeight, "setHeight");
function getQuadrantData() {
    const config2 = (0, _chunkDD37ZF33Mjs.getConfig2)();
    const { themeVariables, quadrantChart: quadrantChartConfig } = config2;
    if (quadrantChartConfig) quadrantBuilder.setConfig(quadrantChartConfig);
    quadrantBuilder.setThemeConfig({
        quadrant1Fill: themeVariables.quadrant1Fill,
        quadrant2Fill: themeVariables.quadrant2Fill,
        quadrant3Fill: themeVariables.quadrant3Fill,
        quadrant4Fill: themeVariables.quadrant4Fill,
        quadrant1TextFill: themeVariables.quadrant1TextFill,
        quadrant2TextFill: themeVariables.quadrant2TextFill,
        quadrant3TextFill: themeVariables.quadrant3TextFill,
        quadrant4TextFill: themeVariables.quadrant4TextFill,
        quadrantPointFill: themeVariables.quadrantPointFill,
        quadrantPointTextFill: themeVariables.quadrantPointTextFill,
        quadrantXAxisTextFill: themeVariables.quadrantXAxisTextFill,
        quadrantYAxisTextFill: themeVariables.quadrantYAxisTextFill,
        quadrantExternalBorderStrokeFill: themeVariables.quadrantExternalBorderStrokeFill,
        quadrantInternalBorderStrokeFill: themeVariables.quadrantInternalBorderStrokeFill,
        quadrantTitleFill: themeVariables.quadrantTitleFill
    });
    quadrantBuilder.setData({
        titleText: (0, _chunkDD37ZF33Mjs.getDiagramTitle)()
    });
    return quadrantBuilder.build();
}
(0, _chunkDLQEHMXDMjs.__name)(getQuadrantData, "getQuadrantData");
var clear2 = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function() {
    quadrantBuilder.clear();
    (0, _chunkDD37ZF33Mjs.clear)();
}, "clear");
var quadrantDb_default = {
    setWidth,
    setHeight,
    setQuadrant1Text,
    setQuadrant2Text,
    setQuadrant3Text,
    setQuadrant4Text,
    setXAxisLeftText,
    setXAxisRightText,
    setYAxisTopText,
    setYAxisBottomText,
    parseStyles,
    addPoint,
    addClass,
    getQuadrantData,
    clear: clear2,
    setAccTitle: (0, _chunkDD37ZF33Mjs.setAccTitle),
    getAccTitle: (0, _chunkDD37ZF33Mjs.getAccTitle),
    setDiagramTitle: (0, _chunkDD37ZF33Mjs.setDiagramTitle),
    getDiagramTitle: (0, _chunkDD37ZF33Mjs.getDiagramTitle),
    getAccDescription: (0, _chunkDD37ZF33Mjs.getAccDescription),
    setAccDescription: (0, _chunkDD37ZF33Mjs.setAccDescription)
};
// src/diagrams/quadrant-chart/quadrantRenderer.ts
var draw = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((txt, id, _version, diagObj)=>{
    function getDominantBaseLine(horizontalPos) {
        return horizontalPos === "top" ? "hanging" : "middle";
    }
    (0, _chunkDLQEHMXDMjs.__name)(getDominantBaseLine, "getDominantBaseLine");
    function getTextAnchor(verticalPos) {
        return verticalPos === "left" ? "start" : "middle";
    }
    (0, _chunkDLQEHMXDMjs.__name)(getTextAnchor, "getTextAnchor");
    function getTransformation(data) {
        return `translate(${data.x}, ${data.y}) rotate(${data.rotation || 0})`;
    }
    (0, _chunkDLQEHMXDMjs.__name)(getTransformation, "getTransformation");
    const conf = (0, _chunkDD37ZF33Mjs.getConfig2)();
    (0, _chunkDD37ZF33Mjs.log).debug("Rendering quadrant chart\n" + txt);
    const securityLevel = conf.securityLevel;
    let sandboxElement;
    if (securityLevel === "sandbox") sandboxElement = (0, _chunkDD37ZF33Mjs.select_default)("#i" + id);
    const root = securityLevel === "sandbox" ? (0, _chunkDD37ZF33Mjs.select_default)(sandboxElement.nodes()[0].contentDocument.body) : (0, _chunkDD37ZF33Mjs.select_default)("body");
    const svg = root.select(`[id="${id}"]`);
    const group = svg.append("g").attr("class", "main");
    const width = conf.quadrantChart?.chartWidth ?? 500;
    const height = conf.quadrantChart?.chartHeight ?? 500;
    (0, _chunkDD37ZF33Mjs.configureSvgSize)(svg, height, width, conf.quadrantChart?.useMaxWidth ?? true);
    svg.attr("viewBox", "0 0 " + width + " " + height);
    diagObj.db.setHeight(height);
    diagObj.db.setWidth(width);
    const quadrantData = diagObj.db.getQuadrantData();
    const quadrantsGroup = group.append("g").attr("class", "quadrants");
    const borderGroup = group.append("g").attr("class", "border");
    const dataPointGroup = group.append("g").attr("class", "data-points");
    const labelGroup = group.append("g").attr("class", "labels");
    const titleGroup = group.append("g").attr("class", "title");
    if (quadrantData.title) titleGroup.append("text").attr("x", 0).attr("y", 0).attr("fill", quadrantData.title.fill).attr("font-size", quadrantData.title.fontSize).attr("dominant-baseline", getDominantBaseLine(quadrantData.title.horizontalPos)).attr("text-anchor", getTextAnchor(quadrantData.title.verticalPos)).attr("transform", getTransformation(quadrantData.title)).text(quadrantData.title.text);
    if (quadrantData.borderLines) borderGroup.selectAll("line").data(quadrantData.borderLines).enter().append("line").attr("x1", (data)=>data.x1).attr("y1", (data)=>data.y1).attr("x2", (data)=>data.x2).attr("y2", (data)=>data.y2).style("stroke", (data)=>data.strokeFill).style("stroke-width", (data)=>data.strokeWidth);
    const quadrants = quadrantsGroup.selectAll("g.quadrant").data(quadrantData.quadrants).enter().append("g").attr("class", "quadrant");
    quadrants.append("rect").attr("x", (data)=>data.x).attr("y", (data)=>data.y).attr("width", (data)=>data.width).attr("height", (data)=>data.height).attr("fill", (data)=>data.fill);
    quadrants.append("text").attr("x", 0).attr("y", 0).attr("fill", (data)=>data.text.fill).attr("font-size", (data)=>data.text.fontSize).attr("dominant-baseline", (data)=>getDominantBaseLine(data.text.horizontalPos)).attr("text-anchor", (data)=>getTextAnchor(data.text.verticalPos)).attr("transform", (data)=>getTransformation(data.text)).text((data)=>data.text.text);
    const labels = labelGroup.selectAll("g.label").data(quadrantData.axisLabels).enter().append("g").attr("class", "label");
    labels.append("text").attr("x", 0).attr("y", 0).text((data)=>data.text).attr("fill", (data)=>data.fill).attr("font-size", (data)=>data.fontSize).attr("dominant-baseline", (data)=>getDominantBaseLine(data.horizontalPos)).attr("text-anchor", (data)=>getTextAnchor(data.verticalPos)).attr("transform", (data)=>getTransformation(data));
    const dataPoints = dataPointGroup.selectAll("g.data-point").data(quadrantData.points).enter().append("g").attr("class", "data-point");
    dataPoints.append("circle").attr("cx", (data)=>data.x).attr("cy", (data)=>data.y).attr("r", (data)=>data.radius).attr("fill", (data)=>data.fill).attr("stroke", (data)=>data.strokeColor).attr("stroke-width", (data)=>data.strokeWidth);
    dataPoints.append("text").attr("x", 0).attr("y", 0).text((data)=>data.text.text).attr("fill", (data)=>data.text.fill).attr("font-size", (data)=>data.text.fontSize).attr("dominant-baseline", (data)=>getDominantBaseLine(data.text.horizontalPos)).attr("text-anchor", (data)=>getTextAnchor(data.text.verticalPos)).attr("transform", (data)=>getTransformation(data.text));
}, "draw");
var quadrantRenderer_default = {
    draw
};
// src/diagrams/quadrant-chart/quadrantDiagram.ts
var diagram = {
    parser: quadrant_default,
    db: quadrantDb_default,
    renderer: quadrantRenderer_default,
    styles: /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(()=>"", "styles")
};

},{"./chunk-DD37ZF33.mjs":"f4pI5","./chunk-DLQEHMXD.mjs":"h5YRf","@parcel/transformer-js/src/esmodule-helpers.js":"1cdyk"}]},["hYQQL"], null, "parcelRequire6955", {})

//# sourceMappingURL=quadrantDiagram-5GZ6ANOP.0ca1e1fe.js.map
