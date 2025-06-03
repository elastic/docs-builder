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
})({"atg02":[function(require,module,exports,__globalThis) {
var global = arguments[3];
var HMR_HOST = null;
var HMR_PORT = 1234;
var HMR_SERVER_PORT = 1234;
var HMR_SECURE = false;
var HMR_ENV_HASH = "febfef3c5b467b16";
var HMR_USE_SSE = false;
module.bundle.HMR_BUNDLE_ID = "c7f5deb0f4793cc4";
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

},{}],"f6bgk":[function(require,module,exports,__globalThis) {
var parcelHelpers = require("@parcel/transformer-js/src/esmodule-helpers.js");
parcelHelpers.defineInteropFlag(exports);
parcelHelpers.export(exports, "diagram", ()=>Bi);
var _chunkWVHPJQMPMjs = require("./chunk-WVHPJQMP.mjs");
var _chunkKMOJB3TBMjs = require("./chunk-KMOJB3TB.mjs");
var _chunkAC3VT7B7Mjs = require("./chunk-AC3VT7B7.mjs");
var _chunkTI4EEUUGMjs = require("./chunk-TI4EEUUG.mjs");
var _chunkNQURTBEVMjs = require("./chunk-NQURTBEV.mjs");
var _chunk6BY5RJGCMjs = require("./chunk-6BY5RJGC.mjs");
var _chunkGTKDMUJJMjs = require("./chunk-GTKDMUJJ.mjs");
var At = function() {
    var i = (0, _chunkGTKDMUJJMjs.a)(function(B, o, c, g) {
        for(c = c || {}, g = B.length; g--; c[B[g]] = o);
        return c;
    }, "o"), t = [
        1,
        10,
        12,
        14,
        16,
        18,
        19,
        21,
        23
    ], e = [
        2,
        6
    ], s = [
        1,
        3
    ], a = [
        1,
        5
    ], h = [
        1,
        6
    ], u = [
        1,
        7
    ], d = [
        1,
        5,
        10,
        12,
        14,
        16,
        18,
        19,
        21,
        23,
        34,
        35,
        36
    ], y = [
        1,
        25
    ], E = [
        1,
        26
    ], w = [
        1,
        28
    ], _ = [
        1,
        29
    ], L = [
        1,
        30
    ], X = [
        1,
        31
    ], k = [
        1,
        32
    ], Y = [
        1,
        33
    ], f = [
        1,
        34
    ], T = [
        1,
        35
    ], l = [
        1,
        36
    ], R = [
        1,
        37
    ], N = [
        1,
        43
    ], Pt = [
        1,
        42
    ], vt = [
        1,
        47
    ], $ = [
        1,
        50
    ], C = [
        1,
        10,
        12,
        14,
        16,
        18,
        19,
        21,
        23,
        34,
        35,
        36
    ], ht = [
        1,
        10,
        12,
        14,
        16,
        18,
        19,
        21,
        23,
        24,
        26,
        27,
        28,
        34,
        35,
        36
    ], P = [
        1,
        10,
        12,
        14,
        16,
        18,
        19,
        21,
        23,
        24,
        26,
        27,
        28,
        34,
        35,
        36,
        41,
        42,
        43,
        44,
        45,
        46,
        47,
        48,
        49,
        50
    ], Et = [
        1,
        64
    ], lt = {
        trace: (0, _chunkGTKDMUJJMjs.a)(function() {}, "trace"),
        yy: {},
        symbols_: {
            error: 2,
            start: 3,
            eol: 4,
            XYCHART: 5,
            chartConfig: 6,
            document: 7,
            CHART_ORIENTATION: 8,
            statement: 9,
            title: 10,
            text: 11,
            X_AXIS: 12,
            parseXAxis: 13,
            Y_AXIS: 14,
            parseYAxis: 15,
            LINE: 16,
            plotData: 17,
            BAR: 18,
            acc_title: 19,
            acc_title_value: 20,
            acc_descr: 21,
            acc_descr_value: 22,
            acc_descr_multiline_value: 23,
            SQUARE_BRACES_START: 24,
            commaSeparatedNumbers: 25,
            SQUARE_BRACES_END: 26,
            NUMBER_WITH_DECIMAL: 27,
            COMMA: 28,
            xAxisData: 29,
            bandData: 30,
            ARROW_DELIMITER: 31,
            commaSeparatedTexts: 32,
            yAxisData: 33,
            NEWLINE: 34,
            SEMI: 35,
            EOF: 36,
            alphaNum: 37,
            STR: 38,
            MD_STR: 39,
            alphaNumToken: 40,
            AMP: 41,
            NUM: 42,
            ALPHA: 43,
            PLUS: 44,
            EQUALS: 45,
            MULT: 46,
            DOT: 47,
            BRKT: 48,
            MINUS: 49,
            UNDERSCORE: 50,
            $accept: 0,
            $end: 1
        },
        terminals_: {
            2: "error",
            5: "XYCHART",
            8: "CHART_ORIENTATION",
            10: "title",
            12: "X_AXIS",
            14: "Y_AXIS",
            16: "LINE",
            18: "BAR",
            19: "acc_title",
            20: "acc_title_value",
            21: "acc_descr",
            22: "acc_descr_value",
            23: "acc_descr_multiline_value",
            24: "SQUARE_BRACES_START",
            26: "SQUARE_BRACES_END",
            27: "NUMBER_WITH_DECIMAL",
            28: "COMMA",
            31: "ARROW_DELIMITER",
            34: "NEWLINE",
            35: "SEMI",
            36: "EOF",
            38: "STR",
            39: "MD_STR",
            41: "AMP",
            42: "NUM",
            43: "ALPHA",
            44: "PLUS",
            45: "EQUALS",
            46: "MULT",
            47: "DOT",
            48: "BRKT",
            49: "MINUS",
            50: "UNDERSCORE"
        },
        productions_: [
            0,
            [
                3,
                2
            ],
            [
                3,
                3
            ],
            [
                3,
                2
            ],
            [
                3,
                1
            ],
            [
                6,
                1
            ],
            [
                7,
                0
            ],
            [
                7,
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
                3
            ],
            [
                9,
                2
            ],
            [
                9,
                3
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
                17,
                3
            ],
            [
                25,
                3
            ],
            [
                25,
                1
            ],
            [
                13,
                1
            ],
            [
                13,
                2
            ],
            [
                13,
                1
            ],
            [
                29,
                1
            ],
            [
                29,
                3
            ],
            [
                30,
                3
            ],
            [
                32,
                3
            ],
            [
                32,
                1
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
                15,
                1
            ],
            [
                33,
                3
            ],
            [
                4,
                1
            ],
            [
                4,
                1
            ],
            [
                4,
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
                11,
                1
            ],
            [
                37,
                1
            ],
            [
                37,
                2
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
                40,
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
                40,
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
                40,
                1
            ],
            [
                40,
                1
            ]
        ],
        performAction: (0, _chunkGTKDMUJJMjs.a)(function(o, c, g, p, b, r, j) {
            var x = r.length - 1;
            switch(b){
                case 5:
                    p.setOrientation(r[x]);
                    break;
                case 9:
                    p.setDiagramTitle(r[x].text.trim());
                    break;
                case 12:
                    p.setLineData({
                        text: "",
                        type: "text"
                    }, r[x]);
                    break;
                case 13:
                    p.setLineData(r[x - 1], r[x]);
                    break;
                case 14:
                    p.setBarData({
                        text: "",
                        type: "text"
                    }, r[x]);
                    break;
                case 15:
                    p.setBarData(r[x - 1], r[x]);
                    break;
                case 16:
                    this.$ = r[x].trim(), p.setAccTitle(this.$);
                    break;
                case 17:
                case 18:
                    this.$ = r[x].trim(), p.setAccDescription(this.$);
                    break;
                case 19:
                    this.$ = r[x - 1];
                    break;
                case 20:
                    this.$ = [
                        Number(r[x - 2]),
                        ...r[x]
                    ];
                    break;
                case 21:
                    this.$ = [
                        Number(r[x])
                    ];
                    break;
                case 22:
                    p.setXAxisTitle(r[x]);
                    break;
                case 23:
                    p.setXAxisTitle(r[x - 1]);
                    break;
                case 24:
                    p.setXAxisTitle({
                        type: "text",
                        text: ""
                    });
                    break;
                case 25:
                    p.setXAxisBand(r[x]);
                    break;
                case 26:
                    p.setXAxisRangeData(Number(r[x - 2]), Number(r[x]));
                    break;
                case 27:
                    this.$ = r[x - 1];
                    break;
                case 28:
                    this.$ = [
                        r[x - 2],
                        ...r[x]
                    ];
                    break;
                case 29:
                    this.$ = [
                        r[x]
                    ];
                    break;
                case 30:
                    p.setYAxisTitle(r[x]);
                    break;
                case 31:
                    p.setYAxisTitle(r[x - 1]);
                    break;
                case 32:
                    p.setYAxisTitle({
                        type: "text",
                        text: ""
                    });
                    break;
                case 33:
                    p.setYAxisRangeData(Number(r[x - 2]), Number(r[x]));
                    break;
                case 37:
                    this.$ = {
                        text: r[x],
                        type: "text"
                    };
                    break;
                case 38:
                    this.$ = {
                        text: r[x],
                        type: "text"
                    };
                    break;
                case 39:
                    this.$ = {
                        text: r[x],
                        type: "markdown"
                    };
                    break;
                case 40:
                    this.$ = r[x];
                    break;
                case 41:
                    this.$ = r[x - 1] + "" + r[x];
                    break;
            }
        }, "anonymous"),
        table: [
            i(t, e, {
                3: 1,
                4: 2,
                7: 4,
                5: s,
                34: a,
                35: h,
                36: u
            }),
            {
                1: [
                    3
                ]
            },
            i(t, e, {
                4: 2,
                7: 4,
                3: 8,
                5: s,
                34: a,
                35: h,
                36: u
            }),
            i(t, e, {
                4: 2,
                7: 4,
                6: 9,
                3: 10,
                5: s,
                8: [
                    1,
                    11
                ],
                34: a,
                35: h,
                36: u
            }),
            {
                1: [
                    2,
                    4
                ],
                9: 12,
                10: [
                    1,
                    13
                ],
                12: [
                    1,
                    14
                ],
                14: [
                    1,
                    15
                ],
                16: [
                    1,
                    16
                ],
                18: [
                    1,
                    17
                ],
                19: [
                    1,
                    18
                ],
                21: [
                    1,
                    19
                ],
                23: [
                    1,
                    20
                ]
            },
            i(d, [
                2,
                34
            ]),
            i(d, [
                2,
                35
            ]),
            i(d, [
                2,
                36
            ]),
            {
                1: [
                    2,
                    1
                ]
            },
            i(t, e, {
                4: 2,
                7: 4,
                3: 21,
                5: s,
                34: a,
                35: h,
                36: u
            }),
            {
                1: [
                    2,
                    3
                ]
            },
            i(d, [
                2,
                5
            ]),
            i(t, [
                2,
                7
            ], {
                4: 22,
                34: a,
                35: h,
                36: u
            }),
            {
                11: 23,
                37: 24,
                38: y,
                39: E,
                40: 27,
                41: w,
                42: _,
                43: L,
                44: X,
                45: k,
                46: Y,
                47: f,
                48: T,
                49: l,
                50: R
            },
            {
                11: 39,
                13: 38,
                24: N,
                27: Pt,
                29: 40,
                30: 41,
                37: 24,
                38: y,
                39: E,
                40: 27,
                41: w,
                42: _,
                43: L,
                44: X,
                45: k,
                46: Y,
                47: f,
                48: T,
                49: l,
                50: R
            },
            {
                11: 45,
                15: 44,
                27: vt,
                33: 46,
                37: 24,
                38: y,
                39: E,
                40: 27,
                41: w,
                42: _,
                43: L,
                44: X,
                45: k,
                46: Y,
                47: f,
                48: T,
                49: l,
                50: R
            },
            {
                11: 49,
                17: 48,
                24: $,
                37: 24,
                38: y,
                39: E,
                40: 27,
                41: w,
                42: _,
                43: L,
                44: X,
                45: k,
                46: Y,
                47: f,
                48: T,
                49: l,
                50: R
            },
            {
                11: 52,
                17: 51,
                24: $,
                37: 24,
                38: y,
                39: E,
                40: 27,
                41: w,
                42: _,
                43: L,
                44: X,
                45: k,
                46: Y,
                47: f,
                48: T,
                49: l,
                50: R
            },
            {
                20: [
                    1,
                    53
                ]
            },
            {
                22: [
                    1,
                    54
                ]
            },
            i(C, [
                2,
                18
            ]),
            {
                1: [
                    2,
                    2
                ]
            },
            i(C, [
                2,
                8
            ]),
            i(C, [
                2,
                9
            ]),
            i(ht, [
                2,
                37
            ], {
                40: 55,
                41: w,
                42: _,
                43: L,
                44: X,
                45: k,
                46: Y,
                47: f,
                48: T,
                49: l,
                50: R
            }),
            i(ht, [
                2,
                38
            ]),
            i(ht, [
                2,
                39
            ]),
            i(P, [
                2,
                40
            ]),
            i(P, [
                2,
                42
            ]),
            i(P, [
                2,
                43
            ]),
            i(P, [
                2,
                44
            ]),
            i(P, [
                2,
                45
            ]),
            i(P, [
                2,
                46
            ]),
            i(P, [
                2,
                47
            ]),
            i(P, [
                2,
                48
            ]),
            i(P, [
                2,
                49
            ]),
            i(P, [
                2,
                50
            ]),
            i(P, [
                2,
                51
            ]),
            i(C, [
                2,
                10
            ]),
            i(C, [
                2,
                22
            ], {
                30: 41,
                29: 56,
                24: N,
                27: Pt
            }),
            i(C, [
                2,
                24
            ]),
            i(C, [
                2,
                25
            ]),
            {
                31: [
                    1,
                    57
                ]
            },
            {
                11: 59,
                32: 58,
                37: 24,
                38: y,
                39: E,
                40: 27,
                41: w,
                42: _,
                43: L,
                44: X,
                45: k,
                46: Y,
                47: f,
                48: T,
                49: l,
                50: R
            },
            i(C, [
                2,
                11
            ]),
            i(C, [
                2,
                30
            ], {
                33: 60,
                27: vt
            }),
            i(C, [
                2,
                32
            ]),
            {
                31: [
                    1,
                    61
                ]
            },
            i(C, [
                2,
                12
            ]),
            {
                17: 62,
                24: $
            },
            {
                25: 63,
                27: Et
            },
            i(C, [
                2,
                14
            ]),
            {
                17: 65,
                24: $
            },
            i(C, [
                2,
                16
            ]),
            i(C, [
                2,
                17
            ]),
            i(P, [
                2,
                41
            ]),
            i(C, [
                2,
                23
            ]),
            {
                27: [
                    1,
                    66
                ]
            },
            {
                26: [
                    1,
                    67
                ]
            },
            {
                26: [
                    2,
                    29
                ],
                28: [
                    1,
                    68
                ]
            },
            i(C, [
                2,
                31
            ]),
            {
                27: [
                    1,
                    69
                ]
            },
            i(C, [
                2,
                13
            ]),
            {
                26: [
                    1,
                    70
                ]
            },
            {
                26: [
                    2,
                    21
                ],
                28: [
                    1,
                    71
                ]
            },
            i(C, [
                2,
                15
            ]),
            i(C, [
                2,
                26
            ]),
            i(C, [
                2,
                27
            ]),
            {
                11: 59,
                32: 72,
                37: 24,
                38: y,
                39: E,
                40: 27,
                41: w,
                42: _,
                43: L,
                44: X,
                45: k,
                46: Y,
                47: f,
                48: T,
                49: l,
                50: R
            },
            i(C, [
                2,
                33
            ]),
            i(C, [
                2,
                19
            ]),
            {
                25: 73,
                27: Et
            },
            {
                26: [
                    2,
                    28
                ]
            },
            {
                26: [
                    2,
                    20
                ]
            }
        ],
        defaultActions: {
            8: [
                2,
                1
            ],
            10: [
                2,
                3
            ],
            21: [
                2,
                2
            ],
            72: [
                2,
                28
            ],
            73: [
                2,
                20
            ]
        },
        parseError: (0, _chunkGTKDMUJJMjs.a)(function(o, c) {
            if (c.recoverable) this.trace(o);
            else {
                var g = new Error(o);
                throw g.hash = c, g;
            }
        }, "parseError"),
        parse: (0, _chunkGTKDMUJJMjs.a)(function(o) {
            var c = this, g = [
                0
            ], p = [], b = [
                null
            ], r = [], j = this.table, x = "", q = 0, Lt = 0, Xt = 0, oe = 2, Yt = 1, he = r.slice.call(arguments, 1), A = Object.create(this.lexer), I = {
                yy: {}
            };
            for(var gt in this.yy)Object.prototype.hasOwnProperty.call(this.yy, gt) && (I.yy[gt] = this.yy[gt]);
            A.setInput(o, I.yy), I.yy.lexer = A, I.yy.parser = this, typeof A.yylloc > "u" && (A.yylloc = {});
            var pt = A.yylloc;
            r.push(pt);
            var le = A.options && A.options.ranges;
            typeof I.yy.parseError == "function" ? this.parseError = I.yy.parseError : this.parseError = Object.getPrototypeOf(this).parseError;
            function ke(S) {
                g.length = g.length - 2 * S, b.length = b.length - S, r.length = r.length - S;
            }
            (0, _chunkGTKDMUJJMjs.a)(ke, "popStack");
            function ce() {
                var S;
                return S = p.pop() || A.lex() || Yt, typeof S != "number" && (S instanceof Array && (p = S, S = p.pop()), S = c.symbols_[S] || S), S;
            }
            (0, _chunkGTKDMUJJMjs.a)(ce, "lex");
            for(var D, ut, M, v, Se, xt, W = {}, Q, V, Vt, K;;){
                if (M = g[g.length - 1], this.defaultActions[M] ? v = this.defaultActions[M] : ((D === null || typeof D > "u") && (D = ce()), v = j[M] && j[M][D]), typeof v > "u" || !v.length || !v[0]) {
                    var mt = "";
                    K = [];
                    for(Q in j[M])this.terminals_[Q] && Q > oe && K.push("'" + this.terminals_[Q] + "'");
                    A.showPosition ? mt = "Parse error on line " + (q + 1) + `:
` + A.showPosition() + `
Expecting ` + K.join(", ") + ", got '" + (this.terminals_[D] || D) + "'" : mt = "Parse error on line " + (q + 1) + ": Unexpected " + (D == Yt ? "end of input" : "'" + (this.terminals_[D] || D) + "'"), this.parseError(mt, {
                        text: A.match,
                        token: this.terminals_[D] || D,
                        line: A.yylineno,
                        loc: pt,
                        expected: K
                    });
                }
                if (v[0] instanceof Array && v.length > 1) throw new Error("Parse Error: multiple actions possible at state: " + M + ", token: " + D);
                switch(v[0]){
                    case 1:
                        g.push(D), b.push(A.yytext), r.push(A.yylloc), g.push(v[1]), D = null, ut ? (D = ut, ut = null) : (Lt = A.yyleng, x = A.yytext, q = A.yylineno, pt = A.yylloc, Xt > 0 && Xt--);
                        break;
                    case 2:
                        if (V = this.productions_[v[1]][1], W.$ = b[b.length - V], W._$ = {
                            first_line: r[r.length - (V || 1)].first_line,
                            last_line: r[r.length - 1].last_line,
                            first_column: r[r.length - (V || 1)].first_column,
                            last_column: r[r.length - 1].last_column
                        }, le && (W._$.range = [
                            r[r.length - (V || 1)].range[0],
                            r[r.length - 1].range[1]
                        ]), xt = this.performAction.apply(W, [
                            x,
                            Lt,
                            q,
                            I.yy,
                            v[1],
                            b,
                            r
                        ].concat(he)), typeof xt < "u") return xt;
                        V && (g = g.slice(0, -1 * V * 2), b = b.slice(0, -1 * V), r = r.slice(0, -1 * V)), g.push(this.productions_[v[1]][0]), b.push(W.$), r.push(W._$), Vt = j[g[g.length - 2]][g[g.length - 1]], g.push(Vt);
                        break;
                    case 3:
                        return !0;
                }
            }
            return !0;
        }, "parse")
    }, re = function() {
        var B = {
            EOF: 1,
            parseError: (0, _chunkGTKDMUJJMjs.a)(function(c, g) {
                if (this.yy.parser) this.yy.parser.parseError(c, g);
                else throw new Error(c);
            }, "parseError"),
            setInput: (0, _chunkGTKDMUJJMjs.a)(function(o, c) {
                return this.yy = c || this.yy || {}, this._input = o, this._more = this._backtrack = this.done = !1, this.yylineno = this.yyleng = 0, this.yytext = this.matched = this.match = "", this.conditionStack = [
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
                var c = o.match(/(?:\r\n?|\n).*/g);
                return c ? (this.yylineno++, this.yylloc.last_line++) : this.yylloc.last_column++, this.options.ranges && this.yylloc.range[1]++, this._input = this._input.slice(1), o;
            }, "input"),
            unput: (0, _chunkGTKDMUJJMjs.a)(function(o) {
                var c = o.length, g = o.split(/(?:\r\n?|\n)/g);
                this._input = o + this._input, this.yytext = this.yytext.substr(0, this.yytext.length - c), this.offset -= c;
                var p = this.match.split(/(?:\r\n?|\n)/g);
                this.match = this.match.substr(0, this.match.length - 1), this.matched = this.matched.substr(0, this.matched.length - 1), g.length - 1 && (this.yylineno -= g.length - 1);
                var b = this.yylloc.range;
                return this.yylloc = {
                    first_line: this.yylloc.first_line,
                    last_line: this.yylineno + 1,
                    first_column: this.yylloc.first_column,
                    last_column: g ? (g.length === p.length ? this.yylloc.first_column : 0) + p[p.length - g.length].length - g[0].length : this.yylloc.first_column - c
                }, this.options.ranges && (this.yylloc.range = [
                    b[0],
                    b[0] + this.yyleng - c
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
                var o = this.pastInput(), c = new Array(o.length + 1).join("-");
                return o + this.upcomingInput() + `
` + c + "^";
            }, "showPosition"),
            test_match: (0, _chunkGTKDMUJJMjs.a)(function(o, c) {
                var g, p, b;
                if (this.options.backtrack_lexer && (b = {
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
                }, this.options.ranges && (b.yylloc.range = this.yylloc.range.slice(0))), p = o[0].match(/(?:\r\n?|\n).*/g), p && (this.yylineno += p.length), this.yylloc = {
                    first_line: this.yylloc.last_line,
                    last_line: this.yylineno + 1,
                    first_column: this.yylloc.last_column,
                    last_column: p ? p[p.length - 1].length - p[p.length - 1].match(/\r?\n?/)[0].length : this.yylloc.last_column + o[0].length
                }, this.yytext += o[0], this.match += o[0], this.matches = o, this.yyleng = this.yytext.length, this.options.ranges && (this.yylloc.range = [
                    this.offset,
                    this.offset += this.yyleng
                ]), this._more = !1, this._backtrack = !1, this._input = this._input.slice(o[0].length), this.matched += o[0], g = this.performAction.call(this, this.yy, this, c, this.conditionStack[this.conditionStack.length - 1]), this.done && this._input && (this.done = !1), g) return g;
                if (this._backtrack) {
                    for(var r in b)this[r] = b[r];
                    return !1;
                }
                return !1;
            }, "test_match"),
            next: (0, _chunkGTKDMUJJMjs.a)(function() {
                if (this.done) return this.EOF;
                this._input || (this.done = !0);
                var o, c, g, p;
                this._more || (this.yytext = "", this.match = "");
                for(var b = this._currentRules(), r = 0; r < b.length; r++)if (g = this._input.match(this.rules[b[r]]), g && (!c || g[0].length > c[0].length)) {
                    if (c = g, p = r, this.options.backtrack_lexer) {
                        if (o = this.test_match(g, b[r]), o !== !1) return o;
                        if (this._backtrack) {
                            c = !1;
                            continue;
                        } else return !1;
                    } else if (!this.options.flex) break;
                }
                return c ? (o = this.test_match(c, b[p]), o !== !1 ? o : !1) : this._input === "" ? this.EOF : this.parseError("Lexical error on line " + (this.yylineno + 1) + `. Unrecognized text.
` + this.showPosition(), {
                    text: "",
                    token: null,
                    line: this.yylineno
                });
            }, "next"),
            lex: (0, _chunkGTKDMUJJMjs.a)(function() {
                var c = this.next();
                return c || this.lex();
            }, "lex"),
            begin: (0, _chunkGTKDMUJJMjs.a)(function(c) {
                this.conditionStack.push(c);
            }, "begin"),
            popState: (0, _chunkGTKDMUJJMjs.a)(function() {
                var c = this.conditionStack.length - 1;
                return c > 0 ? this.conditionStack.pop() : this.conditionStack[0];
            }, "popState"),
            _currentRules: (0, _chunkGTKDMUJJMjs.a)(function() {
                return this.conditionStack.length && this.conditionStack[this.conditionStack.length - 1] ? this.conditions[this.conditionStack[this.conditionStack.length - 1]].rules : this.conditions.INITIAL.rules;
            }, "_currentRules"),
            topState: (0, _chunkGTKDMUJJMjs.a)(function(c) {
                return c = this.conditionStack.length - 1 - Math.abs(c || 0), c >= 0 ? this.conditionStack[c] : "INITIAL";
            }, "topState"),
            pushState: (0, _chunkGTKDMUJJMjs.a)(function(c) {
                this.begin(c);
            }, "pushState"),
            stateStackSize: (0, _chunkGTKDMUJJMjs.a)(function() {
                return this.conditionStack.length;
            }, "stateStackSize"),
            options: {
                "case-insensitive": !0
            },
            performAction: (0, _chunkGTKDMUJJMjs.a)(function(c, g, p, b) {
                var r = b;
                switch(p){
                    case 0:
                        break;
                    case 1:
                        break;
                    case 2:
                        return this.popState(), 34;
                    case 3:
                        return this.popState(), 34;
                    case 4:
                        return 34;
                    case 5:
                        break;
                    case 6:
                        return 10;
                    case 7:
                        return this.pushState("acc_title"), 19;
                    case 8:
                        return this.popState(), "acc_title_value";
                    case 9:
                        return this.pushState("acc_descr"), 21;
                    case 10:
                        return this.popState(), "acc_descr_value";
                    case 11:
                        this.pushState("acc_descr_multiline");
                        break;
                    case 12:
                        this.popState();
                        break;
                    case 13:
                        return "acc_descr_multiline_value";
                    case 14:
                        return 5;
                    case 15:
                        return 8;
                    case 16:
                        return this.pushState("axis_data"), "X_AXIS";
                    case 17:
                        return this.pushState("axis_data"), "Y_AXIS";
                    case 18:
                        return this.pushState("axis_band_data"), 24;
                    case 19:
                        return 31;
                    case 20:
                        return this.pushState("data"), 16;
                    case 21:
                        return this.pushState("data"), 18;
                    case 22:
                        return this.pushState("data_inner"), 24;
                    case 23:
                        return 27;
                    case 24:
                        return this.popState(), 26;
                    case 25:
                        this.popState();
                        break;
                    case 26:
                        this.pushState("string");
                        break;
                    case 27:
                        this.popState();
                        break;
                    case 28:
                        return "STR";
                    case 29:
                        return 24;
                    case 30:
                        return 26;
                    case 31:
                        return 43;
                    case 32:
                        return "COLON";
                    case 33:
                        return 44;
                    case 34:
                        return 28;
                    case 35:
                        return 45;
                    case 36:
                        return 46;
                    case 37:
                        return 48;
                    case 38:
                        return 50;
                    case 39:
                        return 47;
                    case 40:
                        return 41;
                    case 41:
                        return 49;
                    case 42:
                        return 42;
                    case 43:
                        break;
                    case 44:
                        return 35;
                    case 45:
                        return 36;
                }
            }, "anonymous"),
            rules: [
                /^(?:%%(?!\{)[^\n]*)/i,
                /^(?:[^\}]%%[^\n]*)/i,
                /^(?:(\r?\n))/i,
                /^(?:(\r?\n))/i,
                /^(?:[\n\r]+)/i,
                /^(?:%%[^\n]*)/i,
                /^(?:title\b)/i,
                /^(?:accTitle\s*:\s*)/i,
                /^(?:(?!\n||)*[^\n]*)/i,
                /^(?:accDescr\s*:\s*)/i,
                /^(?:(?!\n||)*[^\n]*)/i,
                /^(?:accDescr\s*\{\s*)/i,
                /^(?:\{)/i,
                /^(?:[^\}]*)/i,
                /^(?:xychart-beta\b)/i,
                /^(?:(?:vertical|horizontal))/i,
                /^(?:x-axis\b)/i,
                /^(?:y-axis\b)/i,
                /^(?:\[)/i,
                /^(?:-->)/i,
                /^(?:line\b)/i,
                /^(?:bar\b)/i,
                /^(?:\[)/i,
                /^(?:[+-]?(?:\d+(?:\.\d+)?|\.\d+))/i,
                /^(?:\])/i,
                /^(?:(?:`\)                                    \{ this\.pushState\(md_string\); \}\n<md_string>\(\?:\(\?!`"\)\.\)\+                  \{ return MD_STR; \}\n<md_string>\(\?:`))/i,
                /^(?:["])/i,
                /^(?:["])/i,
                /^(?:[^"]*)/i,
                /^(?:\[)/i,
                /^(?:\])/i,
                /^(?:[A-Za-z]+)/i,
                /^(?::)/i,
                /^(?:\+)/i,
                /^(?:,)/i,
                /^(?:=)/i,
                /^(?:\*)/i,
                /^(?:#)/i,
                /^(?:[\_])/i,
                /^(?:\.)/i,
                /^(?:&)/i,
                /^(?:-)/i,
                /^(?:[0-9]+)/i,
                /^(?:\s+)/i,
                /^(?:;)/i,
                /^(?:$)/i
            ],
            conditions: {
                data_inner: {
                    rules: [
                        0,
                        1,
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
                        20,
                        21,
                        23,
                        24,
                        25,
                        26,
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
                        45
                    ],
                    inclusive: !0
                },
                data: {
                    rules: [
                        0,
                        1,
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
                        20,
                        21,
                        22,
                        25,
                        26,
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
                        45
                    ],
                    inclusive: !0
                },
                axis_band_data: {
                    rules: [
                        0,
                        1,
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
                        20,
                        21,
                        24,
                        25,
                        26,
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
                        45
                    ],
                    inclusive: !0
                },
                axis_data: {
                    rules: [
                        0,
                        1,
                        2,
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
                        19,
                        20,
                        21,
                        23,
                        25,
                        26,
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
                        45
                    ],
                    inclusive: !0
                },
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
                title: {
                    rules: [],
                    inclusive: !1
                },
                md_string: {
                    rules: [],
                    inclusive: !1
                },
                string: {
                    rules: [
                        27,
                        28
                    ],
                    inclusive: !1
                },
                INITIAL: {
                    rules: [
                        0,
                        1,
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
                        20,
                        21,
                        25,
                        26,
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
                        45
                    ],
                    inclusive: !0
                }
            }
        };
        return B;
    }();
    lt.lexer = re;
    function ct() {
        this.yy = {};
    }
    return (0, _chunkGTKDMUJJMjs.a)(ct, "Parser"), ct.prototype = lt, lt.Parser = ct, new ct;
}();
At.parser = At;
var $t = At;
function Tt(i) {
    return i.type === "bar";
}
(0, _chunkGTKDMUJJMjs.a)(Tt, "isBarPlot");
function tt(i) {
    return i.type === "band";
}
(0, _chunkGTKDMUJJMjs.a)(tt, "isBandAxisData");
function O(i) {
    return i.type === "linear";
}
(0, _chunkGTKDMUJJMjs.a)(O, "isLinearAxisData");
var z = class {
    constructor(t){
        this.parentGroup = t;
    }
    static{
        (0, _chunkGTKDMUJJMjs.a)(this, "TextDimensionCalculatorWithFont");
    }
    getMaxDimension(t, e) {
        if (!this.parentGroup) return {
            width: t.reduce((h, u)=>Math.max(u.length, h), 0) * e,
            height: e
        };
        let s = {
            width: 0,
            height: 0
        }, a = this.parentGroup.append("g").attr("visibility", "hidden").attr("font-size", e);
        for (let h of t){
            let u = (0, _chunkKMOJB3TBMjs.b)(a, 1, h), d = u ? u.width : h.length * e, y = u ? u.height : e;
            s.width = Math.max(s.width, d), s.height = Math.max(s.height, y);
        }
        return a.remove(), s;
    }
};
var F = class {
    constructor(t, e, s, a){
        this.axisConfig = t;
        this.title = e;
        this.textDimensionCalculator = s;
        this.axisThemeConfig = a;
        this.boundingRect = {
            x: 0,
            y: 0,
            width: 0,
            height: 0
        };
        this.axisPosition = "left";
        this.showTitle = !1;
        this.showLabel = !1;
        this.showTick = !1;
        this.showAxisLine = !1;
        this.outerPadding = 0;
        this.titleTextHeight = 0;
        this.labelTextHeight = 0;
        this.range = [
            0,
            10
        ], this.boundingRect = {
            x: 0,
            y: 0,
            width: 0,
            height: 0
        }, this.axisPosition = "left";
    }
    static{
        (0, _chunkGTKDMUJJMjs.a)(this, "BaseAxis");
    }
    setRange(t) {
        this.range = t, this.axisPosition === "left" || this.axisPosition === "right" ? this.boundingRect.height = t[1] - t[0] : this.boundingRect.width = t[1] - t[0], this.recalculateScale();
    }
    getRange() {
        return [
            this.range[0] + this.outerPadding,
            this.range[1] - this.outerPadding
        ];
    }
    setAxisPosition(t) {
        this.axisPosition = t, this.setRange(this.range);
    }
    getTickDistance() {
        let t = this.getRange();
        return Math.abs(t[0] - t[1]) / this.getTickValues().length;
    }
    getAxisOuterPadding() {
        return this.outerPadding;
    }
    getLabelDimension() {
        return this.textDimensionCalculator.getMaxDimension(this.getTickValues().map((t)=>t.toString()), this.axisConfig.labelFontSize);
    }
    recalculateOuterPaddingToDrawBar() {
        .7 * this.getTickDistance() > this.outerPadding * 2 && (this.outerPadding = Math.floor(.7 * this.getTickDistance() / 2)), this.recalculateScale();
    }
    calculateSpaceIfDrawnHorizontally(t) {
        let e = t.height;
        if (this.axisConfig.showAxisLine && e > this.axisConfig.axisLineWidth && (e -= this.axisConfig.axisLineWidth, this.showAxisLine = !0), this.axisConfig.showLabel) {
            let s = this.getLabelDimension(), a = .2 * t.width;
            this.outerPadding = Math.min(s.width / 2, a);
            let h = s.height + this.axisConfig.labelPadding * 2;
            this.labelTextHeight = s.height, h <= e && (e -= h, this.showLabel = !0);
        }
        if (this.axisConfig.showTick && e >= this.axisConfig.tickLength && (this.showTick = !0, e -= this.axisConfig.tickLength), this.axisConfig.showTitle && this.title) {
            let s = this.textDimensionCalculator.getMaxDimension([
                this.title
            ], this.axisConfig.titleFontSize), a = s.height + this.axisConfig.titlePadding * 2;
            this.titleTextHeight = s.height, a <= e && (e -= a, this.showTitle = !0);
        }
        this.boundingRect.width = t.width, this.boundingRect.height = t.height - e;
    }
    calculateSpaceIfDrawnVertical(t) {
        let e = t.width;
        if (this.axisConfig.showAxisLine && e > this.axisConfig.axisLineWidth && (e -= this.axisConfig.axisLineWidth, this.showAxisLine = !0), this.axisConfig.showLabel) {
            let s = this.getLabelDimension(), a = .2 * t.height;
            this.outerPadding = Math.min(s.height / 2, a);
            let h = s.width + this.axisConfig.labelPadding * 2;
            h <= e && (e -= h, this.showLabel = !0);
        }
        if (this.axisConfig.showTick && e >= this.axisConfig.tickLength && (this.showTick = !0, e -= this.axisConfig.tickLength), this.axisConfig.showTitle && this.title) {
            let s = this.textDimensionCalculator.getMaxDimension([
                this.title
            ], this.axisConfig.titleFontSize), a = s.height + this.axisConfig.titlePadding * 2;
            this.titleTextHeight = s.height, a <= e && (e -= a, this.showTitle = !0);
        }
        this.boundingRect.width = t.width - e, this.boundingRect.height = t.height;
    }
    calculateSpace(t) {
        return this.axisPosition === "left" || this.axisPosition === "right" ? this.calculateSpaceIfDrawnVertical(t) : this.calculateSpaceIfDrawnHorizontally(t), this.recalculateScale(), {
            width: this.boundingRect.width,
            height: this.boundingRect.height
        };
    }
    setBoundingBoxXY(t) {
        this.boundingRect.x = t.x, this.boundingRect.y = t.y;
    }
    getDrawableElementsForLeftAxis() {
        let t = [];
        if (this.showAxisLine) {
            let e = this.boundingRect.x + this.boundingRect.width - this.axisConfig.axisLineWidth / 2;
            t.push({
                type: "path",
                groupTexts: [
                    "left-axis",
                    "axisl-line"
                ],
                data: [
                    {
                        path: `M ${e},${this.boundingRect.y} L ${e},${this.boundingRect.y + this.boundingRect.height} `,
                        strokeFill: this.axisThemeConfig.axisLineColor,
                        strokeWidth: this.axisConfig.axisLineWidth
                    }
                ]
            });
        }
        if (this.showLabel && t.push({
            type: "text",
            groupTexts: [
                "left-axis",
                "label"
            ],
            data: this.getTickValues().map((e)=>({
                    text: e.toString(),
                    x: this.boundingRect.x + this.boundingRect.width - (this.showLabel ? this.axisConfig.labelPadding : 0) - (this.showTick ? this.axisConfig.tickLength : 0) - (this.showAxisLine ? this.axisConfig.axisLineWidth : 0),
                    y: this.getScaleValue(e),
                    fill: this.axisThemeConfig.labelColor,
                    fontSize: this.axisConfig.labelFontSize,
                    rotation: 0,
                    verticalPos: "middle",
                    horizontalPos: "right"
                }))
        }), this.showTick) {
            let e = this.boundingRect.x + this.boundingRect.width - (this.showAxisLine ? this.axisConfig.axisLineWidth : 0);
            t.push({
                type: "path",
                groupTexts: [
                    "left-axis",
                    "ticks"
                ],
                data: this.getTickValues().map((s)=>({
                        path: `M ${e},${this.getScaleValue(s)} L ${e - this.axisConfig.tickLength},${this.getScaleValue(s)}`,
                        strokeFill: this.axisThemeConfig.tickColor,
                        strokeWidth: this.axisConfig.tickWidth
                    }))
            });
        }
        return this.showTitle && t.push({
            type: "text",
            groupTexts: [
                "left-axis",
                "title"
            ],
            data: [
                {
                    text: this.title,
                    x: this.boundingRect.x + this.axisConfig.titlePadding,
                    y: this.boundingRect.y + this.boundingRect.height / 2,
                    fill: this.axisThemeConfig.titleColor,
                    fontSize: this.axisConfig.titleFontSize,
                    rotation: 270,
                    verticalPos: "top",
                    horizontalPos: "center"
                }
            ]
        }), t;
    }
    getDrawableElementsForBottomAxis() {
        let t = [];
        if (this.showAxisLine) {
            let e = this.boundingRect.y + this.axisConfig.axisLineWidth / 2;
            t.push({
                type: "path",
                groupTexts: [
                    "bottom-axis",
                    "axis-line"
                ],
                data: [
                    {
                        path: `M ${this.boundingRect.x},${e} L ${this.boundingRect.x + this.boundingRect.width},${e}`,
                        strokeFill: this.axisThemeConfig.axisLineColor,
                        strokeWidth: this.axisConfig.axisLineWidth
                    }
                ]
            });
        }
        if (this.showLabel && t.push({
            type: "text",
            groupTexts: [
                "bottom-axis",
                "label"
            ],
            data: this.getTickValues().map((e)=>({
                    text: e.toString(),
                    x: this.getScaleValue(e),
                    y: this.boundingRect.y + this.axisConfig.labelPadding + (this.showTick ? this.axisConfig.tickLength : 0) + (this.showAxisLine ? this.axisConfig.axisLineWidth : 0),
                    fill: this.axisThemeConfig.labelColor,
                    fontSize: this.axisConfig.labelFontSize,
                    rotation: 0,
                    verticalPos: "top",
                    horizontalPos: "center"
                }))
        }), this.showTick) {
            let e = this.boundingRect.y + (this.showAxisLine ? this.axisConfig.axisLineWidth : 0);
            t.push({
                type: "path",
                groupTexts: [
                    "bottom-axis",
                    "ticks"
                ],
                data: this.getTickValues().map((s)=>({
                        path: `M ${this.getScaleValue(s)},${e} L ${this.getScaleValue(s)},${e + this.axisConfig.tickLength}`,
                        strokeFill: this.axisThemeConfig.tickColor,
                        strokeWidth: this.axisConfig.tickWidth
                    }))
            });
        }
        return this.showTitle && t.push({
            type: "text",
            groupTexts: [
                "bottom-axis",
                "title"
            ],
            data: [
                {
                    text: this.title,
                    x: this.range[0] + (this.range[1] - this.range[0]) / 2,
                    y: this.boundingRect.y + this.boundingRect.height - this.axisConfig.titlePadding - this.titleTextHeight,
                    fill: this.axisThemeConfig.titleColor,
                    fontSize: this.axisConfig.titleFontSize,
                    rotation: 0,
                    verticalPos: "top",
                    horizontalPos: "center"
                }
            ]
        }), t;
    }
    getDrawableElementsForTopAxis() {
        let t = [];
        if (this.showAxisLine) {
            let e = this.boundingRect.y + this.boundingRect.height - this.axisConfig.axisLineWidth / 2;
            t.push({
                type: "path",
                groupTexts: [
                    "top-axis",
                    "axis-line"
                ],
                data: [
                    {
                        path: `M ${this.boundingRect.x},${e} L ${this.boundingRect.x + this.boundingRect.width},${e}`,
                        strokeFill: this.axisThemeConfig.axisLineColor,
                        strokeWidth: this.axisConfig.axisLineWidth
                    }
                ]
            });
        }
        if (this.showLabel && t.push({
            type: "text",
            groupTexts: [
                "top-axis",
                "label"
            ],
            data: this.getTickValues().map((e)=>({
                    text: e.toString(),
                    x: this.getScaleValue(e),
                    y: this.boundingRect.y + (this.showTitle ? this.titleTextHeight + this.axisConfig.titlePadding * 2 : 0) + this.axisConfig.labelPadding,
                    fill: this.axisThemeConfig.labelColor,
                    fontSize: this.axisConfig.labelFontSize,
                    rotation: 0,
                    verticalPos: "top",
                    horizontalPos: "center"
                }))
        }), this.showTick) {
            let e = this.boundingRect.y;
            t.push({
                type: "path",
                groupTexts: [
                    "top-axis",
                    "ticks"
                ],
                data: this.getTickValues().map((s)=>({
                        path: `M ${this.getScaleValue(s)},${e + this.boundingRect.height - (this.showAxisLine ? this.axisConfig.axisLineWidth : 0)} L ${this.getScaleValue(s)},${e + this.boundingRect.height - this.axisConfig.tickLength - (this.showAxisLine ? this.axisConfig.axisLineWidth : 0)}`,
                        strokeFill: this.axisThemeConfig.tickColor,
                        strokeWidth: this.axisConfig.tickWidth
                    }))
            });
        }
        return this.showTitle && t.push({
            type: "text",
            groupTexts: [
                "top-axis",
                "title"
            ],
            data: [
                {
                    text: this.title,
                    x: this.boundingRect.x + this.boundingRect.width / 2,
                    y: this.boundingRect.y + this.axisConfig.titlePadding,
                    fill: this.axisThemeConfig.titleColor,
                    fontSize: this.axisConfig.titleFontSize,
                    rotation: 0,
                    verticalPos: "top",
                    horizontalPos: "center"
                }
            ]
        }), t;
    }
    getDrawableElements() {
        if (this.axisPosition === "left") return this.getDrawableElementsForLeftAxis();
        if (this.axisPosition === "right") throw Error("Drawing of right axis is not implemented");
        return this.axisPosition === "bottom" ? this.getDrawableElementsForBottomAxis() : this.axisPosition === "top" ? this.getDrawableElementsForTopAxis() : [];
    }
};
var et = class extends F {
    static{
        (0, _chunkGTKDMUJJMjs.a)(this, "BandAxis");
    }
    constructor(t, e, s, a, h){
        super(t, a, h, e), this.categories = s, this.scale = (0, _chunkNQURTBEVMjs.ia)().domain(this.categories).range(this.getRange());
    }
    setRange(t) {
        super.setRange(t);
    }
    recalculateScale() {
        this.scale = (0, _chunkNQURTBEVMjs.ia)().domain(this.categories).range(this.getRange()).paddingInner(1).paddingOuter(0).align(.5), (0, _chunkNQURTBEVMjs.b).trace("BandAxis axis final categories, range: ", this.categories, this.getRange());
    }
    getTickValues() {
        return this.categories;
    }
    getScaleValue(t) {
        return this.scale(t) ?? this.getRange()[0];
    }
};
var it = class extends F {
    static{
        (0, _chunkGTKDMUJJMjs.a)(this, "LinearAxis");
    }
    constructor(t, e, s, a, h){
        super(t, a, h, e), this.domain = s, this.scale = (0, _chunkNQURTBEVMjs.ja)().domain(this.domain).range(this.getRange());
    }
    getTickValues() {
        return this.scale.ticks();
    }
    recalculateScale() {
        let t = [
            ...this.domain
        ];
        this.axisPosition === "left" && t.reverse(), this.scale = (0, _chunkNQURTBEVMjs.ja)().domain(t).range(this.getRange());
    }
    getScaleValue(t) {
        return this.scale(t);
    }
};
function Dt(i, t, e, s) {
    let a = new z(s);
    return tt(i) ? new et(t, e, i.categories, i.title, a) : new it(t, e, [
        i.min,
        i.max
    ], i.title, a);
}
(0, _chunkGTKDMUJJMjs.a)(Dt, "getAxis");
var wt = class {
    constructor(t, e, s, a){
        this.textDimensionCalculator = t;
        this.chartConfig = e;
        this.chartData = s;
        this.chartThemeConfig = a;
        this.boundingRect = {
            x: 0,
            y: 0,
            width: 0,
            height: 0
        }, this.showChartTitle = !1;
    }
    static{
        (0, _chunkGTKDMUJJMjs.a)(this, "ChartTitle");
    }
    setBoundingBoxXY(t) {
        this.boundingRect.x = t.x, this.boundingRect.y = t.y;
    }
    calculateSpace(t) {
        let e = this.textDimensionCalculator.getMaxDimension([
            this.chartData.title
        ], this.chartConfig.titleFontSize), s = Math.max(e.width, t.width), a = e.height + 2 * this.chartConfig.titlePadding;
        return e.width <= s && e.height <= a && this.chartConfig.showTitle && this.chartData.title && (this.boundingRect.width = s, this.boundingRect.height = a, this.showChartTitle = !0), {
            width: this.boundingRect.width,
            height: this.boundingRect.height
        };
    }
    getDrawableElements() {
        let t = [];
        return this.showChartTitle && t.push({
            groupTexts: [
                "chart-title"
            ],
            type: "text",
            data: [
                {
                    fontSize: this.chartConfig.titleFontSize,
                    text: this.chartData.title,
                    verticalPos: "middle",
                    horizontalPos: "center",
                    x: this.boundingRect.x + this.boundingRect.width / 2,
                    y: this.boundingRect.y + this.boundingRect.height / 2,
                    fill: this.chartThemeConfig.titleColor,
                    rotation: 0
                }
            ]
        }), t;
    }
};
function qt(i, t, e, s) {
    let a = new z(s);
    return new wt(a, i, t, e);
}
(0, _chunkGTKDMUJJMjs.a)(qt, "getChartTitleComponent");
var st = class {
    constructor(t, e, s, a, h){
        this.plotData = t;
        this.xAxis = e;
        this.yAxis = s;
        this.orientation = a;
        this.plotIndex = h;
    }
    static{
        (0, _chunkGTKDMUJJMjs.a)(this, "LinePlot");
    }
    getDrawableElement() {
        let t = this.plotData.data.map((s)=>[
                this.xAxis.getScaleValue(s[0]),
                this.yAxis.getScaleValue(s[1])
            ]), e;
        return this.orientation === "horizontal" ? e = (0, _chunkNQURTBEVMjs.Ca)().y((s)=>s[0]).x((s)=>s[1])(t) : e = (0, _chunkNQURTBEVMjs.Ca)().x((s)=>s[0]).y((s)=>s[1])(t), e ? [
            {
                groupTexts: [
                    "plot",
                    `line-plot-${this.plotIndex}`
                ],
                type: "path",
                data: [
                    {
                        path: e,
                        strokeFill: this.plotData.strokeFill,
                        strokeWidth: this.plotData.strokeWidth
                    }
                ]
            }
        ] : [];
    }
};
var nt = class {
    constructor(t, e, s, a, h, u){
        this.barData = t;
        this.boundingRect = e;
        this.xAxis = s;
        this.yAxis = a;
        this.orientation = h;
        this.plotIndex = u;
    }
    static{
        (0, _chunkGTKDMUJJMjs.a)(this, "BarPlot");
    }
    getDrawableElement() {
        let t = this.barData.data.map((h)=>[
                this.xAxis.getScaleValue(h[0]),
                this.yAxis.getScaleValue(h[1])
            ]), s = Math.min(this.xAxis.getAxisOuterPadding() * 2, this.xAxis.getTickDistance()) * 0.95, a = s / 2;
        return this.orientation === "horizontal" ? [
            {
                groupTexts: [
                    "plot",
                    `bar-plot-${this.plotIndex}`
                ],
                type: "rect",
                data: t.map((h)=>({
                        x: this.boundingRect.x,
                        y: h[0] - a,
                        height: s,
                        width: h[1] - this.boundingRect.x,
                        fill: this.barData.fill,
                        strokeWidth: 0,
                        strokeFill: this.barData.fill
                    }))
            }
        ] : [
            {
                groupTexts: [
                    "plot",
                    `bar-plot-${this.plotIndex}`
                ],
                type: "rect",
                data: t.map((h)=>({
                        x: h[0] - a,
                        y: h[1],
                        width: s,
                        height: this.boundingRect.y + this.boundingRect.height - h[1],
                        fill: this.barData.fill,
                        strokeWidth: 0,
                        strokeFill: this.barData.fill
                    }))
            }
        ];
    }
};
var kt = class {
    constructor(t, e, s){
        this.chartConfig = t;
        this.chartData = e;
        this.chartThemeConfig = s;
        this.boundingRect = {
            x: 0,
            y: 0,
            width: 0,
            height: 0
        };
    }
    static{
        (0, _chunkGTKDMUJJMjs.a)(this, "BasePlot");
    }
    setAxes(t, e) {
        this.xAxis = t, this.yAxis = e;
    }
    setBoundingBoxXY(t) {
        this.boundingRect.x = t.x, this.boundingRect.y = t.y;
    }
    calculateSpace(t) {
        return this.boundingRect.width = t.width, this.boundingRect.height = t.height, {
            width: this.boundingRect.width,
            height: this.boundingRect.height
        };
    }
    getDrawableElements() {
        if (!(this.xAxis && this.yAxis)) throw Error("Axes must be passed to render Plots");
        let t = [];
        for (let [e, s] of this.chartData.plots.entries())switch(s.type){
            case "line":
                {
                    let a = new st(s, this.xAxis, this.yAxis, this.chartConfig.chartOrientation, e);
                    t.push(...a.getDrawableElement());
                }
                break;
            case "bar":
                {
                    let a = new nt(s, this.boundingRect, this.xAxis, this.yAxis, this.chartConfig.chartOrientation, e);
                    t.push(...a.getDrawableElement());
                }
                break;
        }
        return t;
    }
};
function Qt(i, t, e) {
    return new kt(i, t, e);
}
(0, _chunkGTKDMUJJMjs.a)(Qt, "getPlotComponent");
var at = class {
    constructor(t, e, s, a){
        this.chartConfig = t;
        this.chartData = e;
        this.componentStore = {
            title: qt(t, e, s, a),
            plot: Qt(t, e, s),
            xAxis: Dt(e.xAxis, t.xAxis, {
                titleColor: s.xAxisTitleColor,
                labelColor: s.xAxisLabelColor,
                tickColor: s.xAxisTickColor,
                axisLineColor: s.xAxisLineColor
            }, a),
            yAxis: Dt(e.yAxis, t.yAxis, {
                titleColor: s.yAxisTitleColor,
                labelColor: s.yAxisLabelColor,
                tickColor: s.yAxisTickColor,
                axisLineColor: s.yAxisLineColor
            }, a)
        };
    }
    static{
        (0, _chunkGTKDMUJJMjs.a)(this, "Orchestrator");
    }
    calculateVerticalSpace() {
        let t = this.chartConfig.width, e = this.chartConfig.height, s = 0, a = 0, h = Math.floor(t * this.chartConfig.plotReservedSpacePercent / 100), u = Math.floor(e * this.chartConfig.plotReservedSpacePercent / 100), d = this.componentStore.plot.calculateSpace({
            width: h,
            height: u
        });
        t -= d.width, e -= d.height, d = this.componentStore.title.calculateSpace({
            width: this.chartConfig.width,
            height: e
        }), a = d.height, e -= d.height, this.componentStore.xAxis.setAxisPosition("bottom"), d = this.componentStore.xAxis.calculateSpace({
            width: t,
            height: e
        }), e -= d.height, this.componentStore.yAxis.setAxisPosition("left"), d = this.componentStore.yAxis.calculateSpace({
            width: t,
            height: e
        }), s = d.width, t -= d.width, t > 0 && (h += t, t = 0), e > 0 && (u += e, e = 0), this.componentStore.plot.calculateSpace({
            width: h,
            height: u
        }), this.componentStore.plot.setBoundingBoxXY({
            x: s,
            y: a
        }), this.componentStore.xAxis.setRange([
            s,
            s + h
        ]), this.componentStore.xAxis.setBoundingBoxXY({
            x: s,
            y: a + u
        }), this.componentStore.yAxis.setRange([
            a,
            a + u
        ]), this.componentStore.yAxis.setBoundingBoxXY({
            x: 0,
            y: a
        }), this.chartData.plots.some((y)=>Tt(y)) && this.componentStore.xAxis.recalculateOuterPaddingToDrawBar();
    }
    calculateHorizontalSpace() {
        let t = this.chartConfig.width, e = this.chartConfig.height, s = 0, a = 0, h = 0, u = Math.floor(t * this.chartConfig.plotReservedSpacePercent / 100), d = Math.floor(e * this.chartConfig.plotReservedSpacePercent / 100), y = this.componentStore.plot.calculateSpace({
            width: u,
            height: d
        });
        t -= y.width, e -= y.height, y = this.componentStore.title.calculateSpace({
            width: this.chartConfig.width,
            height: e
        }), s = y.height, e -= y.height, this.componentStore.xAxis.setAxisPosition("left"), y = this.componentStore.xAxis.calculateSpace({
            width: t,
            height: e
        }), t -= y.width, a = y.width, this.componentStore.yAxis.setAxisPosition("top"), y = this.componentStore.yAxis.calculateSpace({
            width: t,
            height: e
        }), e -= y.height, h = s + y.height, t > 0 && (u += t, t = 0), e > 0 && (d += e, e = 0), this.componentStore.plot.calculateSpace({
            width: u,
            height: d
        }), this.componentStore.plot.setBoundingBoxXY({
            x: a,
            y: h
        }), this.componentStore.yAxis.setRange([
            a,
            a + u
        ]), this.componentStore.yAxis.setBoundingBoxXY({
            x: a,
            y: s
        }), this.componentStore.xAxis.setRange([
            h,
            h + d
        ]), this.componentStore.xAxis.setBoundingBoxXY({
            x: 0,
            y: h
        }), this.chartData.plots.some((E)=>Tt(E)) && this.componentStore.xAxis.recalculateOuterPaddingToDrawBar();
    }
    calculateSpace() {
        this.chartConfig.chartOrientation === "horizontal" ? this.calculateHorizontalSpace() : this.calculateVerticalSpace();
    }
    getDrawableElement() {
        this.calculateSpace();
        let t = [];
        this.componentStore.plot.setAxes(this.componentStore.xAxis, this.componentStore.yAxis);
        for (let e of Object.values(this.componentStore))t.push(...e.getDrawableElements());
        return t;
    }
};
var rt = class {
    static{
        (0, _chunkGTKDMUJJMjs.a)(this, "XYChartBuilder");
    }
    static build(t, e, s, a) {
        return new at(t, e, s, a).getDrawableElement();
    }
};
var G = 0, Kt, H = Jt(), U = Zt(), m = te(), St = U.plotColorPalette.split(",").map((i)=>i.trim()), ot = !1, _t = !1;
function Zt() {
    let i = (0, _chunkNQURTBEVMjs.q)(), t = (0, _chunkNQURTBEVMjs.A)();
    return (0, _chunkAC3VT7B7Mjs.l)(i.xyChart, t.themeVariables.xyChart);
}
(0, _chunkGTKDMUJJMjs.a)(Zt, "getChartDefaultThemeConfig");
function Jt() {
    let i = (0, _chunkNQURTBEVMjs.A)();
    return (0, _chunkAC3VT7B7Mjs.l)((0, _chunkNQURTBEVMjs.s).xyChart, i.xyChart);
}
(0, _chunkGTKDMUJJMjs.a)(Jt, "getChartDefaultConfig");
function te() {
    return {
        yAxis: {
            type: "linear",
            title: "",
            min: 1 / 0,
            max: -1 / 0
        },
        xAxis: {
            type: "band",
            title: "",
            categories: []
        },
        title: "",
        plots: []
    };
}
(0, _chunkGTKDMUJJMjs.a)(te, "getChartDefaultData");
function Rt(i) {
    let t = (0, _chunkNQURTBEVMjs.A)();
    return (0, _chunkNQURTBEVMjs.F)(i.trim(), t);
}
(0, _chunkGTKDMUJJMjs.a)(Rt, "textSanitizer");
function ge(i) {
    Kt = i;
}
(0, _chunkGTKDMUJJMjs.a)(ge, "setTmpSVGG");
function pe(i) {
    i === "horizontal" ? H.chartOrientation = "horizontal" : H.chartOrientation = "vertical";
}
(0, _chunkGTKDMUJJMjs.a)(pe, "setOrientation");
function ue(i) {
    m.xAxis.title = Rt(i.text);
}
(0, _chunkGTKDMUJJMjs.a)(ue, "setXAxisTitle");
function ee(i, t) {
    m.xAxis = {
        type: "linear",
        title: m.xAxis.title,
        min: i,
        max: t
    }, ot = !0;
}
(0, _chunkGTKDMUJJMjs.a)(ee, "setXAxisRangeData");
function xe(i) {
    m.xAxis = {
        type: "band",
        title: m.xAxis.title,
        categories: i.map((t)=>Rt(t.text))
    }, ot = !0;
}
(0, _chunkGTKDMUJJMjs.a)(xe, "setXAxisBand");
function me(i) {
    m.yAxis.title = Rt(i.text);
}
(0, _chunkGTKDMUJJMjs.a)(me, "setYAxisTitle");
function fe(i, t) {
    m.yAxis = {
        type: "linear",
        title: m.yAxis.title,
        min: i,
        max: t
    }, _t = !0;
}
(0, _chunkGTKDMUJJMjs.a)(fe, "setYAxisRangeData");
function de(i) {
    let t = Math.min(...i), e = Math.max(...i), s = O(m.yAxis) ? m.yAxis.min : 1 / 0, a = O(m.yAxis) ? m.yAxis.max : -1 / 0;
    m.yAxis = {
        type: "linear",
        title: m.yAxis.title,
        min: Math.min(s, t),
        max: Math.max(a, e)
    };
}
(0, _chunkGTKDMUJJMjs.a)(de, "setYAxisRangeFromPlotData");
function ie(i) {
    let t = [];
    if (i.length === 0) return t;
    if (!ot) {
        let e = O(m.xAxis) ? m.xAxis.min : 1 / 0, s = O(m.xAxis) ? m.xAxis.max : -1 / 0;
        ee(Math.min(e, 1), Math.max(s, i.length));
    }
    if (_t || de(i), tt(m.xAxis) && (t = m.xAxis.categories.map((e, s)=>[
            e,
            i[s]
        ])), O(m.xAxis)) {
        let e = m.xAxis.min, s = m.xAxis.max, a = (s - e) / (i.length - 1), h = [];
        for(let u = e; u <= s; u += a)h.push(`${u}`);
        t = h.map((u, d)=>[
                u,
                i[d]
            ]);
    }
    return t;
}
(0, _chunkGTKDMUJJMjs.a)(ie, "transformDataWithoutCategory");
function se(i) {
    return St[i === 0 ? 0 : i % St.length];
}
(0, _chunkGTKDMUJJMjs.a)(se, "getPlotColorFromPalette");
function be(i, t) {
    let e = ie(t);
    m.plots.push({
        type: "line",
        strokeFill: se(G),
        strokeWidth: 2,
        data: e
    }), G++;
}
(0, _chunkGTKDMUJJMjs.a)(be, "setLineData");
function ye(i, t) {
    let e = ie(t);
    m.plots.push({
        type: "bar",
        fill: se(G),
        data: e
    }), G++;
}
(0, _chunkGTKDMUJJMjs.a)(ye, "setBarData");
function Ce() {
    if (m.plots.length === 0) throw Error("No Plot to render, please provide a plot with some data");
    return m.title = (0, _chunkNQURTBEVMjs.V)(), rt.build(H, m, U, Kt);
}
(0, _chunkGTKDMUJJMjs.a)(Ce, "getDrawableElem");
function Ae() {
    return U;
}
(0, _chunkGTKDMUJJMjs.a)(Ae, "getChartThemeConfig");
function Te() {
    return H;
}
(0, _chunkGTKDMUJJMjs.a)(Te, "getChartConfig");
var De = (0, _chunkGTKDMUJJMjs.a)(function() {
    (0, _chunkNQURTBEVMjs.P)(), G = 0, H = Jt(), m = te(), U = Zt(), St = U.plotColorPalette.split(",").map((i)=>i.trim()), ot = !1, _t = !1;
}, "clear"), ne = {
    getDrawableElem: Ce,
    clear: De,
    setAccTitle: (0, _chunkNQURTBEVMjs.Q),
    getAccTitle: (0, _chunkNQURTBEVMjs.R),
    setDiagramTitle: (0, _chunkNQURTBEVMjs.U),
    getDiagramTitle: (0, _chunkNQURTBEVMjs.V),
    getAccDescription: (0, _chunkNQURTBEVMjs.T),
    setAccDescription: (0, _chunkNQURTBEVMjs.S),
    setOrientation: pe,
    setXAxisTitle: ue,
    setXAxisRangeData: ee,
    setXAxisBand: xe,
    setYAxisTitle: me,
    setYAxisRangeData: fe,
    setLineData: be,
    setBarData: ye,
    setTmpSVGG: ge,
    getChartThemeConfig: Ae,
    getChartConfig: Te
};
var we = (0, _chunkGTKDMUJJMjs.a)((i, t, e, s)=>{
    let a = s.db, h = a.getChartThemeConfig(), u = a.getChartConfig();
    function d(f) {
        return f === "top" ? "text-before-edge" : "middle";
    }
    (0, _chunkGTKDMUJJMjs.a)(d, "getDominantBaseLine");
    function y(f) {
        return f === "left" ? "start" : f === "right" ? "end" : "middle";
    }
    (0, _chunkGTKDMUJJMjs.a)(y, "getTextAnchor");
    function E(f) {
        return `translate(${f.x}, ${f.y}) rotate(${f.rotation || 0})`;
    }
    (0, _chunkGTKDMUJJMjs.a)(E, "getTextTransformation"), (0, _chunkNQURTBEVMjs.b).debug(`Rendering xychart chart
` + i);
    let w = (0, _chunkWVHPJQMPMjs.a)(t), _ = w.append("g").attr("class", "main"), L = _.append("rect").attr("width", u.width).attr("height", u.height).attr("class", "background");
    (0, _chunkNQURTBEVMjs.M)(w, u.height, u.width, !0), w.attr("viewBox", `0 0 ${u.width} ${u.height}`), L.attr("fill", h.backgroundColor), a.setTmpSVGG(w.append("g").attr("class", "mermaid-tmp-group"));
    let X = a.getDrawableElem(), k = {};
    function Y(f) {
        let T = _, l = "";
        for (let [R] of f.entries()){
            let N = _;
            R > 0 && k[l] && (N = k[l]), l += f[R], T = k[l], T || (T = k[l] = N.append("g").attr("class", f[R]));
        }
        return T;
    }
    (0, _chunkGTKDMUJJMjs.a)(Y, "getGroup");
    for (let f of X){
        if (f.data.length === 0) continue;
        let T = Y(f.groupTexts);
        switch(f.type){
            case "rect":
                T.selectAll("rect").data(f.data).enter().append("rect").attr("x", (l)=>l.x).attr("y", (l)=>l.y).attr("width", (l)=>l.width).attr("height", (l)=>l.height).attr("fill", (l)=>l.fill).attr("stroke", (l)=>l.strokeFill).attr("stroke-width", (l)=>l.strokeWidth);
                break;
            case "text":
                T.selectAll("text").data(f.data).enter().append("text").attr("x", 0).attr("y", 0).attr("fill", (l)=>l.fill).attr("font-size", (l)=>l.fontSize).attr("dominant-baseline", (l)=>d(l.verticalPos)).attr("text-anchor", (l)=>y(l.horizontalPos)).attr("transform", (l)=>E(l)).text((l)=>l.text);
                break;
            case "path":
                T.selectAll("path").data(f.data).enter().append("path").attr("d", (l)=>l.path).attr("fill", (l)=>l.fill ? l.fill : "none").attr("stroke", (l)=>l.strokeFill).attr("stroke-width", (l)=>l.strokeWidth);
                break;
        }
    }
}, "draw"), ae = {
    draw: we
};
var Bi = {
    parser: $t,
    db: ne,
    renderer: ae
};

},{"./chunk-WVHPJQMP.mjs":"eRkIA","./chunk-KMOJB3TB.mjs":"aJH4M","./chunk-AC3VT7B7.mjs":"1eiUz","./chunk-TI4EEUUG.mjs":"8SKrN","./chunk-NQURTBEV.mjs":"iASFe","./chunk-6BY5RJGC.mjs":"bRXnR","./chunk-GTKDMUJJ.mjs":"fruhx","@parcel/transformer-js/src/esmodule-helpers.js":"1cdyk"}]},["atg02"], null, "parcelRequire6955", {})

//# sourceMappingURL=xychartDiagram-RLS75X5Z.f4793cc4.js.map
