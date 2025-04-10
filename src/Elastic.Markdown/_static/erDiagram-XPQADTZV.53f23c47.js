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
})({"03rCu":[function(require,module,exports,__globalThis) {
var global = arguments[3];
var HMR_HOST = null;
var HMR_PORT = 1234;
var HMR_SERVER_PORT = 1234;
var HMR_SECURE = false;
var HMR_ENV_HASH = "febfef3c5b467b16";
var HMR_USE_SSE = false;
module.bundle.HMR_BUNDLE_ID = "1c1df0bc53f23c47";
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

},{}],"4Xjfq":[function(require,module,exports,__globalThis) {
var parcelHelpers = require("@parcel/transformer-js/src/esmodule-helpers.js");
parcelHelpers.defineInteropFlag(exports);
parcelHelpers.export(exports, "diagram", ()=>diagram);
var _chunkCN5XARC6Mjs = require("./chunk-CN5XARC6.mjs");
var _chunkULVYQCHCMjs = require("./chunk-ULVYQCHC.mjs");
var _chunkI7ZFS43CMjs = require("./chunk-I7ZFS43C.mjs");
var _chunkGKOISANMMjs = require("./chunk-GKOISANM.mjs");
var _chunkDD37ZF33Mjs = require("./chunk-DD37ZF33.mjs");
var _chunkTZBO7MLIMjs = require("./chunk-TZBO7MLI.mjs");
var _chunkGRZAG2UZMjs = require("./chunk-GRZAG2UZ.mjs");
var _chunkHD3LK5B5Mjs = require("./chunk-HD3LK5B5.mjs");
var _chunkDLQEHMXDMjs = require("./chunk-DLQEHMXD.mjs");
// src/diagrams/er/parser/erDiagram.jison
var parser = function() {
    var o = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(k, v, o2, l) {
        for(o2 = o2 || {}, l = k.length; l--; o2[k[l]] = v);
        return o2;
    }, "o"), $V0 = [
        6,
        8,
        10,
        20,
        22,
        24,
        26,
        27,
        28
    ], $V1 = [
        1,
        10
    ], $V2 = [
        1,
        11
    ], $V3 = [
        1,
        12
    ], $V4 = [
        1,
        13
    ], $V5 = [
        1,
        14
    ], $V6 = [
        1,
        15
    ], $V7 = [
        1,
        21
    ], $V8 = [
        1,
        22
    ], $V9 = [
        1,
        23
    ], $Va = [
        1,
        24
    ], $Vb = [
        1,
        25
    ], $Vc = [
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
    ], $Vd = [
        1,
        34
    ], $Ve = [
        27,
        28,
        46,
        47
    ], $Vf = [
        41,
        42,
        43,
        44,
        45
    ], $Vg = [
        17,
        34
    ], $Vh = [
        1,
        54
    ], $Vi = [
        1,
        53
    ], $Vj = [
        17,
        34,
        36,
        38
    ];
    var parser2 = {
        trace: /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function trace() {}, "trace"),
        yy: {},
        symbols_: {
            "error": 2,
            "start": 3,
            "ER_DIAGRAM": 4,
            "document": 5,
            "EOF": 6,
            "line": 7,
            "SPACE": 8,
            "statement": 9,
            "NEWLINE": 10,
            "entityName": 11,
            "relSpec": 12,
            ":": 13,
            "role": 14,
            "BLOCK_START": 15,
            "attributes": 16,
            "BLOCK_STOP": 17,
            "SQS": 18,
            "SQE": 19,
            "title": 20,
            "title_value": 21,
            "acc_title": 22,
            "acc_title_value": 23,
            "acc_descr": 24,
            "acc_descr_value": 25,
            "acc_descr_multiline_value": 26,
            "ALPHANUM": 27,
            "ENTITY_NAME": 28,
            "attribute": 29,
            "attributeType": 30,
            "attributeName": 31,
            "attributeKeyTypeList": 32,
            "attributeComment": 33,
            "ATTRIBUTE_WORD": 34,
            "attributeKeyType": 35,
            "COMMA": 36,
            "ATTRIBUTE_KEY": 37,
            "COMMENT": 38,
            "cardinality": 39,
            "relType": 40,
            "ZERO_OR_ONE": 41,
            "ZERO_OR_MORE": 42,
            "ONE_OR_MORE": 43,
            "ONLY_ONE": 44,
            "MD_PARENT": 45,
            "NON_IDENTIFYING": 46,
            "IDENTIFYING": 47,
            "WORD": 48,
            "$accept": 0,
            "$end": 1
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
        performAction: /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function anonymous(yytext, yyleng, yylineno, yy, yystate, $$, _$) {
            var $0 = $$.length - 1;
            switch(yystate){
                case 1:
                    break;
                case 2:
                    this.$ = [];
                    break;
                case 3:
                    $$[$0 - 1].push($$[$0]);
                    this.$ = $$[$0 - 1];
                    break;
                case 4:
                case 5:
                    this.$ = $$[$0];
                    break;
                case 6:
                case 7:
                    this.$ = [];
                    break;
                case 8:
                    yy.addEntity($$[$0 - 4]);
                    yy.addEntity($$[$0 - 2]);
                    yy.addRelationship($$[$0 - 4], $$[$0], $$[$0 - 2], $$[$0 - 3]);
                    break;
                case 9:
                    yy.addEntity($$[$0 - 3]);
                    yy.addAttributes($$[$0 - 3], $$[$0 - 1]);
                    break;
                case 10:
                    yy.addEntity($$[$0 - 2]);
                    break;
                case 11:
                    yy.addEntity($$[$0]);
                    break;
                case 12:
                    yy.addEntity($$[$0 - 6], $$[$0 - 4]);
                    yy.addAttributes($$[$0 - 6], $$[$0 - 1]);
                    break;
                case 13:
                    yy.addEntity($$[$0 - 5], $$[$0 - 3]);
                    break;
                case 14:
                    yy.addEntity($$[$0 - 3], $$[$0 - 1]);
                    break;
                case 15:
                case 16:
                    this.$ = $$[$0].trim();
                    yy.setAccTitle(this.$);
                    break;
                case 17:
                case 18:
                    this.$ = $$[$0].trim();
                    yy.setAccDescription(this.$);
                    break;
                case 19:
                case 43:
                    this.$ = $$[$0];
                    break;
                case 20:
                case 41:
                case 42:
                    this.$ = $$[$0].replace(/"/g, "");
                    break;
                case 21:
                case 29:
                    this.$ = [
                        $$[$0]
                    ];
                    break;
                case 22:
                    $$[$0].push($$[$0 - 1]);
                    this.$ = $$[$0];
                    break;
                case 23:
                    this.$ = {
                        attributeType: $$[$0 - 1],
                        attributeName: $$[$0]
                    };
                    break;
                case 24:
                    this.$ = {
                        attributeType: $$[$0 - 2],
                        attributeName: $$[$0 - 1],
                        attributeKeyTypeList: $$[$0]
                    };
                    break;
                case 25:
                    this.$ = {
                        attributeType: $$[$0 - 2],
                        attributeName: $$[$0 - 1],
                        attributeComment: $$[$0]
                    };
                    break;
                case 26:
                    this.$ = {
                        attributeType: $$[$0 - 3],
                        attributeName: $$[$0 - 2],
                        attributeKeyTypeList: $$[$0 - 1],
                        attributeComment: $$[$0]
                    };
                    break;
                case 27:
                case 28:
                case 31:
                    this.$ = $$[$0];
                    break;
                case 30:
                    $$[$0 - 2].push($$[$0]);
                    this.$ = $$[$0 - 2];
                    break;
                case 32:
                    this.$ = $$[$0].replace(/"/g, "");
                    break;
                case 33:
                    this.$ = {
                        cardA: $$[$0],
                        relType: $$[$0 - 1],
                        cardB: $$[$0 - 2]
                    };
                    break;
                case 34:
                    this.$ = yy.Cardinality.ZERO_OR_ONE;
                    break;
                case 35:
                    this.$ = yy.Cardinality.ZERO_OR_MORE;
                    break;
                case 36:
                    this.$ = yy.Cardinality.ONE_OR_MORE;
                    break;
                case 37:
                    this.$ = yy.Cardinality.ONLY_ONE;
                    break;
                case 38:
                    this.$ = yy.Cardinality.MD_PARENT;
                    break;
                case 39:
                    this.$ = yy.Identification.NON_IDENTIFYING;
                    break;
                case 40:
                    this.$ = yy.Identification.IDENTIFYING;
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
            o($V0, [
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
                20: $V1,
                22: $V2,
                24: $V3,
                26: $V4,
                27: $V5,
                28: $V6
            },
            o($V0, [
                2,
                7
            ], {
                1: [
                    2,
                    1
                ]
            }),
            o($V0, [
                2,
                3
            ]),
            {
                9: 16,
                11: 9,
                20: $V1,
                22: $V2,
                24: $V3,
                26: $V4,
                27: $V5,
                28: $V6
            },
            o($V0, [
                2,
                5
            ]),
            o($V0, [
                2,
                6
            ]),
            o($V0, [
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
                41: $V7,
                42: $V8,
                43: $V9,
                44: $Va,
                45: $Vb
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
            o($V0, [
                2,
                18
            ]),
            o($Vc, [
                2,
                19
            ]),
            o($Vc, [
                2,
                20
            ]),
            o($V0, [
                2,
                4
            ]),
            {
                11: 29,
                27: $V5,
                28: $V6
            },
            {
                16: 30,
                17: [
                    1,
                    31
                ],
                29: 32,
                30: 33,
                34: $Vd
            },
            {
                11: 35,
                27: $V5,
                28: $V6
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
            o($Ve, [
                2,
                34
            ]),
            o($Ve, [
                2,
                35
            ]),
            o($Ve, [
                2,
                36
            ]),
            o($Ve, [
                2,
                37
            ]),
            o($Ve, [
                2,
                38
            ]),
            o($V0, [
                2,
                15
            ]),
            o($V0, [
                2,
                16
            ]),
            o($V0, [
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
            o($V0, [
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
                34: $Vd
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
                41: $V7,
                42: $V8,
                43: $V9,
                44: $Va,
                45: $Vb
            },
            o($Vf, [
                2,
                39
            ]),
            o($Vf, [
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
            o($V0, [
                2,
                9
            ]),
            {
                17: [
                    2,
                    22
                ]
            },
            o($Vg, [
                2,
                23
            ], {
                32: 50,
                33: 51,
                35: 52,
                37: $Vh,
                38: $Vi
            }),
            o([
                17,
                34,
                37,
                38
            ], [
                2,
                28
            ]),
            o($V0, [
                2,
                14
            ], {
                15: [
                    1,
                    55
                ]
            }),
            o([
                27,
                28
            ], [
                2,
                33
            ]),
            o($V0, [
                2,
                8
            ]),
            o($V0, [
                2,
                41
            ]),
            o($V0, [
                2,
                42
            ]),
            o($V0, [
                2,
                43
            ]),
            o($Vg, [
                2,
                24
            ], {
                33: 56,
                36: [
                    1,
                    57
                ],
                38: $Vi
            }),
            o($Vg, [
                2,
                25
            ]),
            o($Vj, [
                2,
                29
            ]),
            o($Vg, [
                2,
                32
            ]),
            o($Vj, [
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
                34: $Vd
            },
            o($Vg, [
                2,
                26
            ]),
            {
                35: 60,
                37: $Vh
            },
            {
                17: [
                    1,
                    61
                ]
            },
            o($V0, [
                2,
                13
            ]),
            o($Vj, [
                2,
                30
            ]),
            o($V0, [
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
        parseError: /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function parseError(str, hash) {
            if (hash.recoverable) this.trace(str);
            else {
                var error = new Error(str);
                error.hash = hash;
                throw error;
            }
        }, "parseError"),
        parse: /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function parse2(input) {
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
                        this.begin("acc_title");
                        return 22;
                    case 1:
                        this.popState();
                        return "acc_title_value";
                    case 2:
                        this.begin("acc_descr");
                        return 24;
                    case 3:
                        this.popState();
                        return "acc_descr_value";
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
                        this.begin("block");
                        return 15;
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
                        this.popState();
                        return 17;
                    case 22:
                        return yy_.yytext[0];
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
                        return yy_.yytext[0];
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
                "acc_descr_multiline": {
                    "rules": [
                        5,
                        6
                    ],
                    "inclusive": false
                },
                "acc_descr": {
                    "rules": [
                        3
                    ],
                    "inclusive": false
                },
                "acc_title": {
                    "rules": [
                        1
                    ],
                    "inclusive": false
                },
                "block": {
                    "rules": [
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
                    "inclusive": false
                },
                "INITIAL": {
                    "rules": [
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
var erDiagram_default = parser;
// src/diagrams/er/erDb.js
var entities = /* @__PURE__ */ new Map();
var relationships = [];
var Cardinality = {
    ZERO_OR_ONE: "ZERO_OR_ONE",
    ZERO_OR_MORE: "ZERO_OR_MORE",
    ONE_OR_MORE: "ONE_OR_MORE",
    ONLY_ONE: "ONLY_ONE",
    MD_PARENT: "MD_PARENT"
};
var Identification = {
    NON_IDENTIFYING: "NON_IDENTIFYING",
    IDENTIFYING: "IDENTIFYING"
};
var addEntity = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(name, alias) {
    if (!entities.has(name)) {
        entities.set(name, {
            attributes: [],
            alias
        });
        (0, _chunkDD37ZF33Mjs.log).info("Added new entity :", name);
    } else if (!entities.get(name).alias && alias) {
        entities.get(name).alias = alias;
        (0, _chunkDD37ZF33Mjs.log).info(`Add alias '${alias}' to entity '${name}'`);
    }
    return entities.get(name);
}, "addEntity");
var getEntities = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(()=>entities, "getEntities");
var addAttributes = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(entityName, attribs) {
    let entity = addEntity(entityName);
    let i;
    for(i = attribs.length - 1; i >= 0; i--){
        entity.attributes.push(attribs[i]);
        (0, _chunkDD37ZF33Mjs.log).debug("Added attribute ", attribs[i].attributeName);
    }
}, "addAttributes");
var addRelationship = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(entA, rolA, entB, rSpec) {
    let rel = {
        entityA: entA,
        roleA: rolA,
        entityB: entB,
        relSpec: rSpec
    };
    relationships.push(rel);
    (0, _chunkDD37ZF33Mjs.log).debug("Added new relationship :", rel);
}, "addRelationship");
var getRelationships = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(()=>relationships, "getRelationships");
var clear2 = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function() {
    entities = /* @__PURE__ */ new Map();
    relationships = [];
    (0, _chunkDD37ZF33Mjs.clear)();
}, "clear");
var erDb_default = {
    Cardinality,
    Identification,
    getConfig: /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(()=>(0, _chunkDD37ZF33Mjs.getConfig2)().er, "getConfig"),
    addEntity,
    addAttributes,
    getEntities,
    addRelationship,
    getRelationships,
    clear: clear2,
    setAccTitle: (0, _chunkDD37ZF33Mjs.setAccTitle),
    getAccTitle: (0, _chunkDD37ZF33Mjs.getAccTitle),
    setAccDescription: (0, _chunkDD37ZF33Mjs.setAccDescription),
    getAccDescription: (0, _chunkDD37ZF33Mjs.getAccDescription),
    setDiagramTitle: (0, _chunkDD37ZF33Mjs.setDiagramTitle),
    getDiagramTitle: (0, _chunkDD37ZF33Mjs.getDiagramTitle)
};
// src/diagrams/er/erMarkers.js
var ERMarkers = {
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
};
var insertMarkers = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(elem, conf2) {
    let marker;
    elem.append("defs").append("marker").attr("id", ERMarkers.MD_PARENT_START).attr("refX", 0).attr("refY", 7).attr("markerWidth", 190).attr("markerHeight", 240).attr("orient", "auto").append("path").attr("d", "M 18,7 L9,13 L1,7 L9,1 Z");
    elem.append("defs").append("marker").attr("id", ERMarkers.MD_PARENT_END).attr("refX", 19).attr("refY", 7).attr("markerWidth", 20).attr("markerHeight", 28).attr("orient", "auto").append("path").attr("d", "M 18,7 L9,13 L1,7 L9,1 Z");
    elem.append("defs").append("marker").attr("id", ERMarkers.ONLY_ONE_START).attr("refX", 0).attr("refY", 9).attr("markerWidth", 18).attr("markerHeight", 18).attr("orient", "auto").append("path").attr("stroke", conf2.stroke).attr("fill", "none").attr("d", "M9,0 L9,18 M15,0 L15,18");
    elem.append("defs").append("marker").attr("id", ERMarkers.ONLY_ONE_END).attr("refX", 18).attr("refY", 9).attr("markerWidth", 18).attr("markerHeight", 18).attr("orient", "auto").append("path").attr("stroke", conf2.stroke).attr("fill", "none").attr("d", "M3,0 L3,18 M9,0 L9,18");
    marker = elem.append("defs").append("marker").attr("id", ERMarkers.ZERO_OR_ONE_START).attr("refX", 0).attr("refY", 9).attr("markerWidth", 30).attr("markerHeight", 18).attr("orient", "auto");
    marker.append("circle").attr("stroke", conf2.stroke).attr("fill", "white").attr("cx", 21).attr("cy", 9).attr("r", 6);
    marker.append("path").attr("stroke", conf2.stroke).attr("fill", "none").attr("d", "M9,0 L9,18");
    marker = elem.append("defs").append("marker").attr("id", ERMarkers.ZERO_OR_ONE_END).attr("refX", 30).attr("refY", 9).attr("markerWidth", 30).attr("markerHeight", 18).attr("orient", "auto");
    marker.append("circle").attr("stroke", conf2.stroke).attr("fill", "white").attr("cx", 9).attr("cy", 9).attr("r", 6);
    marker.append("path").attr("stroke", conf2.stroke).attr("fill", "none").attr("d", "M21,0 L21,18");
    elem.append("defs").append("marker").attr("id", ERMarkers.ONE_OR_MORE_START).attr("refX", 18).attr("refY", 18).attr("markerWidth", 45).attr("markerHeight", 36).attr("orient", "auto").append("path").attr("stroke", conf2.stroke).attr("fill", "none").attr("d", "M0,18 Q 18,0 36,18 Q 18,36 0,18 M42,9 L42,27");
    elem.append("defs").append("marker").attr("id", ERMarkers.ONE_OR_MORE_END).attr("refX", 27).attr("refY", 18).attr("markerWidth", 45).attr("markerHeight", 36).attr("orient", "auto").append("path").attr("stroke", conf2.stroke).attr("fill", "none").attr("d", "M3,9 L3,27 M9,18 Q27,0 45,18 Q27,36 9,18");
    marker = elem.append("defs").append("marker").attr("id", ERMarkers.ZERO_OR_MORE_START).attr("refX", 18).attr("refY", 18).attr("markerWidth", 57).attr("markerHeight", 36).attr("orient", "auto");
    marker.append("circle").attr("stroke", conf2.stroke).attr("fill", "white").attr("cx", 48).attr("cy", 18).attr("r", 6);
    marker.append("path").attr("stroke", conf2.stroke).attr("fill", "none").attr("d", "M0,18 Q18,0 36,18 Q18,36 0,18");
    marker = elem.append("defs").append("marker").attr("id", ERMarkers.ZERO_OR_MORE_END).attr("refX", 39).attr("refY", 18).attr("markerWidth", 57).attr("markerHeight", 36).attr("orient", "auto");
    marker.append("circle").attr("stroke", conf2.stroke).attr("fill", "white").attr("cx", 9).attr("cy", 18).attr("r", 6);
    marker.append("path").attr("stroke", conf2.stroke).attr("fill", "none").attr("d", "M21,18 Q39,0 57,18 Q39,36 21,18");
    return;
}, "insertMarkers");
var erMarkers_default = {
    ERMarkers,
    insertMarkers
};
// ../../node_modules/.pnpm/uuid@9.0.1/node_modules/uuid/dist/esm-browser/regex.js
var regex_default = /^(?:[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}|00000000-0000-0000-0000-000000000000)$/i;
// ../../node_modules/.pnpm/uuid@9.0.1/node_modules/uuid/dist/esm-browser/validate.js
function validate(uuid) {
    return typeof uuid === "string" && regex_default.test(uuid);
}
(0, _chunkDLQEHMXDMjs.__name)(validate, "validate");
var validate_default = validate;
// ../../node_modules/.pnpm/uuid@9.0.1/node_modules/uuid/dist/esm-browser/stringify.js
var byteToHex = [];
for(let i = 0; i < 256; ++i)byteToHex.push((i + 256).toString(16).slice(1));
function unsafeStringify(arr, offset = 0) {
    return byteToHex[arr[offset + 0]] + byteToHex[arr[offset + 1]] + byteToHex[arr[offset + 2]] + byteToHex[arr[offset + 3]] + "-" + byteToHex[arr[offset + 4]] + byteToHex[arr[offset + 5]] + "-" + byteToHex[arr[offset + 6]] + byteToHex[arr[offset + 7]] + "-" + byteToHex[arr[offset + 8]] + byteToHex[arr[offset + 9]] + "-" + byteToHex[arr[offset + 10]] + byteToHex[arr[offset + 11]] + byteToHex[arr[offset + 12]] + byteToHex[arr[offset + 13]] + byteToHex[arr[offset + 14]] + byteToHex[arr[offset + 15]];
}
(0, _chunkDLQEHMXDMjs.__name)(unsafeStringify, "unsafeStringify");
// ../../node_modules/.pnpm/uuid@9.0.1/node_modules/uuid/dist/esm-browser/parse.js
function parse(uuid) {
    if (!validate_default(uuid)) throw TypeError("Invalid UUID");
    let v;
    const arr = new Uint8Array(16);
    arr[0] = (v = parseInt(uuid.slice(0, 8), 16)) >>> 24;
    arr[1] = v >>> 16 & 255;
    arr[2] = v >>> 8 & 255;
    arr[3] = v & 255;
    arr[4] = (v = parseInt(uuid.slice(9, 13), 16)) >>> 8;
    arr[5] = v & 255;
    arr[6] = (v = parseInt(uuid.slice(14, 18), 16)) >>> 8;
    arr[7] = v & 255;
    arr[8] = (v = parseInt(uuid.slice(19, 23), 16)) >>> 8;
    arr[9] = v & 255;
    arr[10] = (v = parseInt(uuid.slice(24, 36), 16)) / 1099511627776 & 255;
    arr[11] = v / 4294967296 & 255;
    arr[12] = v >>> 24 & 255;
    arr[13] = v >>> 16 & 255;
    arr[14] = v >>> 8 & 255;
    arr[15] = v & 255;
    return arr;
}
(0, _chunkDLQEHMXDMjs.__name)(parse, "parse");
var parse_default = parse;
// ../../node_modules/.pnpm/uuid@9.0.1/node_modules/uuid/dist/esm-browser/v35.js
function stringToBytes(str) {
    str = unescape(encodeURIComponent(str));
    const bytes = [];
    for(let i = 0; i < str.length; ++i)bytes.push(str.charCodeAt(i));
    return bytes;
}
(0, _chunkDLQEHMXDMjs.__name)(stringToBytes, "stringToBytes");
var DNS = "6ba7b810-9dad-11d1-80b4-00c04fd430c8";
var URL = "6ba7b811-9dad-11d1-80b4-00c04fd430c8";
function v35(name, version, hashfunc) {
    function generateUUID(value, namespace, buf, offset) {
        var _namespace;
        if (typeof value === "string") value = stringToBytes(value);
        if (typeof namespace === "string") namespace = parse_default(namespace);
        if (((_namespace = namespace) === null || _namespace === void 0 ? void 0 : _namespace.length) !== 16) throw TypeError("Namespace must be array-like (16 iterable integer values, 0-255)");
        let bytes = new Uint8Array(16 + value.length);
        bytes.set(namespace);
        bytes.set(value, namespace.length);
        bytes = hashfunc(bytes);
        bytes[6] = bytes[6] & 15 | version;
        bytes[8] = bytes[8] & 63 | 128;
        if (buf) {
            offset = offset || 0;
            for(let i = 0; i < 16; ++i)buf[offset + i] = bytes[i];
            return buf;
        }
        return unsafeStringify(bytes);
    }
    (0, _chunkDLQEHMXDMjs.__name)(generateUUID, "generateUUID");
    try {
        generateUUID.name = name;
    } catch (err) {}
    generateUUID.DNS = DNS;
    generateUUID.URL = URL;
    return generateUUID;
}
(0, _chunkDLQEHMXDMjs.__name)(v35, "v35");
// ../../node_modules/.pnpm/uuid@9.0.1/node_modules/uuid/dist/esm-browser/sha1.js
function f(s, x, y, z) {
    switch(s){
        case 0:
            return x & y ^ ~x & z;
        case 1:
            return x ^ y ^ z;
        case 2:
            return x & y ^ x & z ^ y & z;
        case 3:
            return x ^ y ^ z;
    }
}
(0, _chunkDLQEHMXDMjs.__name)(f, "f");
function ROTL(x, n) {
    return x << n | x >>> 32 - n;
}
(0, _chunkDLQEHMXDMjs.__name)(ROTL, "ROTL");
function sha1(bytes) {
    const K = [
        1518500249,
        1859775393,
        2400959708,
        3395469782
    ];
    const H = [
        1732584193,
        4023233417,
        2562383102,
        271733878,
        3285377520
    ];
    if (typeof bytes === "string") {
        const msg = unescape(encodeURIComponent(bytes));
        bytes = [];
        for(let i = 0; i < msg.length; ++i)bytes.push(msg.charCodeAt(i));
    } else if (!Array.isArray(bytes)) bytes = Array.prototype.slice.call(bytes);
    bytes.push(128);
    const l = bytes.length / 4 + 2;
    const N = Math.ceil(l / 16);
    const M = new Array(N);
    for(let i = 0; i < N; ++i){
        const arr = new Uint32Array(16);
        for(let j = 0; j < 16; ++j)arr[j] = bytes[i * 64 + j * 4] << 24 | bytes[i * 64 + j * 4 + 1] << 16 | bytes[i * 64 + j * 4 + 2] << 8 | bytes[i * 64 + j * 4 + 3];
        M[i] = arr;
    }
    M[N - 1][14] = (bytes.length - 1) * 8 / Math.pow(2, 32);
    M[N - 1][14] = Math.floor(M[N - 1][14]);
    M[N - 1][15] = (bytes.length - 1) * 8 & 4294967295;
    for(let i = 0; i < N; ++i){
        const W = new Uint32Array(80);
        for(let t = 0; t < 16; ++t)W[t] = M[i][t];
        for(let t = 16; t < 80; ++t)W[t] = ROTL(W[t - 3] ^ W[t - 8] ^ W[t - 14] ^ W[t - 16], 1);
        let a = H[0];
        let b = H[1];
        let c = H[2];
        let d = H[3];
        let e = H[4];
        for(let t = 0; t < 80; ++t){
            const s = Math.floor(t / 20);
            const T = ROTL(a, 5) + f(s, b, c, d) + e + K[s] + W[t] >>> 0;
            e = d;
            d = c;
            c = ROTL(b, 30) >>> 0;
            b = a;
            a = T;
        }
        H[0] = H[0] + a >>> 0;
        H[1] = H[1] + b >>> 0;
        H[2] = H[2] + c >>> 0;
        H[3] = H[3] + d >>> 0;
        H[4] = H[4] + e >>> 0;
    }
    return [
        H[0] >> 24 & 255,
        H[0] >> 16 & 255,
        H[0] >> 8 & 255,
        H[0] & 255,
        H[1] >> 24 & 255,
        H[1] >> 16 & 255,
        H[1] >> 8 & 255,
        H[1] & 255,
        H[2] >> 24 & 255,
        H[2] >> 16 & 255,
        H[2] >> 8 & 255,
        H[2] & 255,
        H[3] >> 24 & 255,
        H[3] >> 16 & 255,
        H[3] >> 8 & 255,
        H[3] & 255,
        H[4] >> 24 & 255,
        H[4] >> 16 & 255,
        H[4] >> 8 & 255,
        H[4] & 255
    ];
}
(0, _chunkDLQEHMXDMjs.__name)(sha1, "sha1");
var sha1_default = sha1;
// ../../node_modules/.pnpm/uuid@9.0.1/node_modules/uuid/dist/esm-browser/v5.js
var v5 = v35("v5", 80, sha1_default);
var v5_default = v5;
// src/diagrams/er/erRenderer.js
var BAD_ID_CHARS_REGEXP = /[^\dA-Za-z](\W)*/g;
var conf = {};
var entityNameIds = /* @__PURE__ */ new Map();
var setConf = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(cnf) {
    const keys = Object.keys(cnf);
    for (const key of keys)conf[key] = cnf[key];
}, "setConf");
var drawAttributes = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((groupNode, entityTextNode, attributes)=>{
    const heightPadding = conf.entityPadding / 3;
    const widthPadding = conf.entityPadding / 3;
    const attrFontSize = conf.fontSize * 0.85;
    const labelBBox = entityTextNode.node().getBBox();
    const attributeNodes = [];
    let hasKeyType = false;
    let hasComment = false;
    let maxTypeWidth = 0;
    let maxNameWidth = 0;
    let maxKeyWidth = 0;
    let maxCommentWidth = 0;
    let cumulativeHeight = labelBBox.height + heightPadding * 2;
    let attrNum = 1;
    attributes.forEach((item)=>{
        if (item.attributeKeyTypeList !== void 0 && item.attributeKeyTypeList.length > 0) hasKeyType = true;
        if (item.attributeComment !== void 0) hasComment = true;
    });
    attributes.forEach((item)=>{
        const attrPrefix = `${entityTextNode.node().id}-attr-${attrNum}`;
        let nodeHeight = 0;
        const attributeType = (0, _chunkDD37ZF33Mjs.parseGenericTypes)(item.attributeType);
        const typeNode = groupNode.append("text").classed("er entityLabel", true).attr("id", `${attrPrefix}-type`).attr("x", 0).attr("y", 0).style("dominant-baseline", "middle").style("text-anchor", "left").style("font-family", (0, _chunkDD37ZF33Mjs.getConfig2)().fontFamily).style("font-size", attrFontSize + "px").text(attributeType);
        const nameNode = groupNode.append("text").classed("er entityLabel", true).attr("id", `${attrPrefix}-name`).attr("x", 0).attr("y", 0).style("dominant-baseline", "middle").style("text-anchor", "left").style("font-family", (0, _chunkDD37ZF33Mjs.getConfig2)().fontFamily).style("font-size", attrFontSize + "px").text(item.attributeName);
        const attributeNode = {};
        attributeNode.tn = typeNode;
        attributeNode.nn = nameNode;
        const typeBBox = typeNode.node().getBBox();
        const nameBBox = nameNode.node().getBBox();
        maxTypeWidth = Math.max(maxTypeWidth, typeBBox.width);
        maxNameWidth = Math.max(maxNameWidth, nameBBox.width);
        nodeHeight = Math.max(typeBBox.height, nameBBox.height);
        if (hasKeyType) {
            const keyTypeNodeText = item.attributeKeyTypeList !== void 0 ? item.attributeKeyTypeList.join(",") : "";
            const keyTypeNode = groupNode.append("text").classed("er entityLabel", true).attr("id", `${attrPrefix}-key`).attr("x", 0).attr("y", 0).style("dominant-baseline", "middle").style("text-anchor", "left").style("font-family", (0, _chunkDD37ZF33Mjs.getConfig2)().fontFamily).style("font-size", attrFontSize + "px").text(keyTypeNodeText);
            attributeNode.kn = keyTypeNode;
            const keyTypeBBox = keyTypeNode.node().getBBox();
            maxKeyWidth = Math.max(maxKeyWidth, keyTypeBBox.width);
            nodeHeight = Math.max(nodeHeight, keyTypeBBox.height);
        }
        if (hasComment) {
            const commentNode = groupNode.append("text").classed("er entityLabel", true).attr("id", `${attrPrefix}-comment`).attr("x", 0).attr("y", 0).style("dominant-baseline", "middle").style("text-anchor", "left").style("font-family", (0, _chunkDD37ZF33Mjs.getConfig2)().fontFamily).style("font-size", attrFontSize + "px").text(item.attributeComment || "");
            attributeNode.cn = commentNode;
            const commentNodeBBox = commentNode.node().getBBox();
            maxCommentWidth = Math.max(maxCommentWidth, commentNodeBBox.width);
            nodeHeight = Math.max(nodeHeight, commentNodeBBox.height);
        }
        attributeNode.height = nodeHeight;
        attributeNodes.push(attributeNode);
        cumulativeHeight += nodeHeight + heightPadding * 2;
        attrNum += 1;
    });
    let widthPaddingFactor = 4;
    if (hasKeyType) widthPaddingFactor += 2;
    if (hasComment) widthPaddingFactor += 2;
    const maxWidth = maxTypeWidth + maxNameWidth + maxKeyWidth + maxCommentWidth;
    const bBox = {
        width: Math.max(conf.minEntityWidth, Math.max(labelBBox.width + conf.entityPadding * 2, maxWidth + widthPadding * widthPaddingFactor)),
        height: attributes.length > 0 ? cumulativeHeight : Math.max(conf.minEntityHeight, labelBBox.height + conf.entityPadding * 2)
    };
    if (attributes.length > 0) {
        const spareColumnWidth = Math.max(0, (bBox.width - maxWidth - widthPadding * widthPaddingFactor) / (widthPaddingFactor / 2));
        entityTextNode.attr("transform", "translate(" + bBox.width / 2 + "," + (heightPadding + labelBBox.height / 2) + ")");
        let heightOffset = labelBBox.height + heightPadding * 2;
        let attribStyle = "attributeBoxOdd";
        attributeNodes.forEach((attributeNode)=>{
            const alignY = heightOffset + heightPadding + attributeNode.height / 2;
            attributeNode.tn.attr("transform", "translate(" + widthPadding + "," + alignY + ")");
            const typeRect = groupNode.insert("rect", "#" + attributeNode.tn.node().id).classed(`er ${attribStyle}`, true).attr("x", 0).attr("y", heightOffset).attr("width", maxTypeWidth + widthPadding * 2 + spareColumnWidth).attr("height", attributeNode.height + heightPadding * 2);
            const nameXOffset = parseFloat(typeRect.attr("x")) + parseFloat(typeRect.attr("width"));
            attributeNode.nn.attr("transform", "translate(" + (nameXOffset + widthPadding) + "," + alignY + ")");
            const nameRect = groupNode.insert("rect", "#" + attributeNode.nn.node().id).classed(`er ${attribStyle}`, true).attr("x", nameXOffset).attr("y", heightOffset).attr("width", maxNameWidth + widthPadding * 2 + spareColumnWidth).attr("height", attributeNode.height + heightPadding * 2);
            let keyTypeAndCommentXOffset = parseFloat(nameRect.attr("x")) + parseFloat(nameRect.attr("width"));
            if (hasKeyType) {
                attributeNode.kn.attr("transform", "translate(" + (keyTypeAndCommentXOffset + widthPadding) + "," + alignY + ")");
                const keyTypeRect = groupNode.insert("rect", "#" + attributeNode.kn.node().id).classed(`er ${attribStyle}`, true).attr("x", keyTypeAndCommentXOffset).attr("y", heightOffset).attr("width", maxKeyWidth + widthPadding * 2 + spareColumnWidth).attr("height", attributeNode.height + heightPadding * 2);
                keyTypeAndCommentXOffset = parseFloat(keyTypeRect.attr("x")) + parseFloat(keyTypeRect.attr("width"));
            }
            if (hasComment) {
                attributeNode.cn.attr("transform", "translate(" + (keyTypeAndCommentXOffset + widthPadding) + "," + alignY + ")");
                groupNode.insert("rect", "#" + attributeNode.cn.node().id).classed(`er ${attribStyle}`, "true").attr("x", keyTypeAndCommentXOffset).attr("y", heightOffset).attr("width", maxCommentWidth + widthPadding * 2 + spareColumnWidth).attr("height", attributeNode.height + heightPadding * 2);
            }
            heightOffset += attributeNode.height + heightPadding * 2;
            attribStyle = attribStyle === "attributeBoxOdd" ? "attributeBoxEven" : "attributeBoxOdd";
        });
    } else {
        bBox.height = Math.max(conf.minEntityHeight, cumulativeHeight);
        entityTextNode.attr("transform", "translate(" + bBox.width / 2 + "," + bBox.height / 2 + ")");
    }
    return bBox;
}, "drawAttributes");
var drawEntities = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(svgNode, entities2, graph) {
    const keys = [
        ...entities2.keys()
    ];
    let firstOne;
    keys.forEach(function(entityName) {
        const entityId = generateId(entityName, "entity");
        entityNameIds.set(entityName, entityId);
        const groupNode = svgNode.append("g").attr("id", entityId);
        firstOne = firstOne === void 0 ? entityId : firstOne;
        const textId = "text-" + entityId;
        const textNode = groupNode.append("text").classed("er entityLabel", true).attr("id", textId).attr("x", 0).attr("y", 0).style("dominant-baseline", "middle").style("text-anchor", "middle").style("font-family", (0, _chunkDD37ZF33Mjs.getConfig2)().fontFamily).style("font-size", conf.fontSize + "px").text(entities2.get(entityName).alias ?? entityName);
        const { width: entityWidth, height: entityHeight } = drawAttributes(groupNode, textNode, entities2.get(entityName).attributes);
        const rectNode = groupNode.insert("rect", "#" + textId).classed("er entityBox", true).attr("x", 0).attr("y", 0).attr("width", entityWidth).attr("height", entityHeight);
        const rectBBox = rectNode.node().getBBox();
        graph.setNode(entityId, {
            width: rectBBox.width,
            height: rectBBox.height,
            shape: "rect",
            id: entityId
        });
    });
    return firstOne;
}, "drawEntities");
var adjustEntities = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(svgNode, graph) {
    graph.nodes().forEach(function(v) {
        if (v !== void 0 && graph.node(v) !== void 0) svgNode.select("#" + v).attr("transform", "translate(" + (graph.node(v).x - graph.node(v).width / 2) + "," + (graph.node(v).y - graph.node(v).height / 2) + " )");
    });
}, "adjustEntities");
var getEdgeName = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(rel) {
    return (rel.entityA + rel.roleA + rel.entityB).replace(/\s/g, "");
}, "getEdgeName");
var addRelationships = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(relationships2, g) {
    relationships2.forEach(function(r) {
        g.setEdge(entityNameIds.get(r.entityA), entityNameIds.get(r.entityB), {
            relationship: r
        }, getEdgeName(r));
    });
    return relationships2;
}, "addRelationships");
var relCnt = 0;
var drawRelationshipFromLayout = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(svg, rel, g, insert, diagObj) {
    relCnt++;
    const edge = g.edge(entityNameIds.get(rel.entityA), entityNameIds.get(rel.entityB), getEdgeName(rel));
    const lineFunction = (0, _chunkDD37ZF33Mjs.line_default)().x(function(d) {
        return d.x;
    }).y(function(d) {
        return d.y;
    }).curve((0, _chunkDD37ZF33Mjs.basis_default));
    const svgPath = svg.insert("path", "#" + insert).classed("er relationshipLine", true).attr("d", lineFunction(edge.points)).style("stroke", conf.stroke).style("fill", "none");
    if (rel.relSpec.relType === diagObj.db.Identification.NON_IDENTIFYING) svgPath.attr("stroke-dasharray", "8,8");
    let url = "";
    if (conf.arrowMarkerAbsolute) {
        url = window.location.protocol + "//" + window.location.host + window.location.pathname + window.location.search;
        url = url.replace(/\(/g, "\\(");
        url = url.replace(/\)/g, "\\)");
    }
    switch(rel.relSpec.cardA){
        case diagObj.db.Cardinality.ZERO_OR_ONE:
            svgPath.attr("marker-end", "url(" + url + "#" + erMarkers_default.ERMarkers.ZERO_OR_ONE_END + ")");
            break;
        case diagObj.db.Cardinality.ZERO_OR_MORE:
            svgPath.attr("marker-end", "url(" + url + "#" + erMarkers_default.ERMarkers.ZERO_OR_MORE_END + ")");
            break;
        case diagObj.db.Cardinality.ONE_OR_MORE:
            svgPath.attr("marker-end", "url(" + url + "#" + erMarkers_default.ERMarkers.ONE_OR_MORE_END + ")");
            break;
        case diagObj.db.Cardinality.ONLY_ONE:
            svgPath.attr("marker-end", "url(" + url + "#" + erMarkers_default.ERMarkers.ONLY_ONE_END + ")");
            break;
        case diagObj.db.Cardinality.MD_PARENT:
            svgPath.attr("marker-end", "url(" + url + "#" + erMarkers_default.ERMarkers.MD_PARENT_END + ")");
            break;
    }
    switch(rel.relSpec.cardB){
        case diagObj.db.Cardinality.ZERO_OR_ONE:
            svgPath.attr("marker-start", "url(" + url + "#" + erMarkers_default.ERMarkers.ZERO_OR_ONE_START + ")");
            break;
        case diagObj.db.Cardinality.ZERO_OR_MORE:
            svgPath.attr("marker-start", "url(" + url + "#" + erMarkers_default.ERMarkers.ZERO_OR_MORE_START + ")");
            break;
        case diagObj.db.Cardinality.ONE_OR_MORE:
            svgPath.attr("marker-start", "url(" + url + "#" + erMarkers_default.ERMarkers.ONE_OR_MORE_START + ")");
            break;
        case diagObj.db.Cardinality.ONLY_ONE:
            svgPath.attr("marker-start", "url(" + url + "#" + erMarkers_default.ERMarkers.ONLY_ONE_START + ")");
            break;
        case diagObj.db.Cardinality.MD_PARENT:
            svgPath.attr("marker-start", "url(" + url + "#" + erMarkers_default.ERMarkers.MD_PARENT_START + ")");
            break;
    }
    const len = svgPath.node().getTotalLength();
    const labelPoint = svgPath.node().getPointAtLength(len * 0.5);
    const labelId = "rel" + relCnt;
    const labelNode = svg.append("text").classed("er relationshipLabel", true).attr("id", labelId).attr("x", labelPoint.x).attr("y", labelPoint.y).style("text-anchor", "middle").style("dominant-baseline", "middle").style("font-family", (0, _chunkDD37ZF33Mjs.getConfig2)().fontFamily).style("font-size", conf.fontSize + "px").text(rel.roleA);
    const labelBBox = labelNode.node().getBBox();
    svg.insert("rect", "#" + labelId).classed("er relationshipLabelBox", true).attr("x", labelPoint.x - labelBBox.width / 2).attr("y", labelPoint.y - labelBBox.height / 2).attr("width", labelBBox.width).attr("height", labelBBox.height);
}, "drawRelationshipFromLayout");
var draw = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(text, id, _version, diagObj) {
    conf = (0, _chunkDD37ZF33Mjs.getConfig2)().er;
    (0, _chunkDD37ZF33Mjs.log).info("Drawing ER diagram");
    const securityLevel = (0, _chunkDD37ZF33Mjs.getConfig2)().securityLevel;
    let sandboxElement;
    if (securityLevel === "sandbox") sandboxElement = (0, _chunkDD37ZF33Mjs.select_default)("#i" + id);
    const root = securityLevel === "sandbox" ? (0, _chunkDD37ZF33Mjs.select_default)(sandboxElement.nodes()[0].contentDocument.body) : (0, _chunkDD37ZF33Mjs.select_default)("body");
    const svg = root.select(`[id='${id}']`);
    erMarkers_default.insertMarkers(svg, conf);
    let g;
    g = new (0, _chunkULVYQCHCMjs.Graph)({
        multigraph: true,
        directed: true,
        compound: false
    }).setGraph({
        rankdir: conf.layoutDirection,
        marginx: 20,
        marginy: 20,
        nodesep: 100,
        edgesep: 100,
        ranksep: 100
    }).setDefaultEdgeLabel(function() {
        return {};
    });
    const firstEntity = drawEntities(svg, diagObj.db.getEntities(), g);
    const relationships2 = addRelationships(diagObj.db.getRelationships(), g);
    (0, _chunkCN5XARC6Mjs.layout)(g);
    adjustEntities(svg, g);
    relationships2.forEach(function(rel) {
        drawRelationshipFromLayout(svg, rel, g, firstEntity, diagObj);
    });
    const padding = conf.diagramPadding;
    (0, _chunkI7ZFS43CMjs.utils_default).insertTitle(svg, "entityTitleText", conf.titleTopMargin, diagObj.db.getDiagramTitle());
    const svgBounds = svg.node().getBBox();
    const width = svgBounds.width + padding * 2;
    const height = svgBounds.height + padding * 2;
    (0, _chunkDD37ZF33Mjs.configureSvgSize)(svg, height, width, conf.useMaxWidth);
    svg.attr("viewBox", `${svgBounds.x - padding} ${svgBounds.y - padding} ${width} ${height}`);
}, "draw");
var MERMAID_ERDIAGRAM_UUID = "28e9f9db-3c8d-5aa5-9faf-44286ae5937c";
function generateId(str = "", prefix = "") {
    const simplifiedStr = str.replace(BAD_ID_CHARS_REGEXP, "");
    return `${strWithHyphen(prefix)}${strWithHyphen(simplifiedStr)}${v5_default(str, MERMAID_ERDIAGRAM_UUID)}`;
}
(0, _chunkDLQEHMXDMjs.__name)(generateId, "generateId");
function strWithHyphen(str = "") {
    return str.length > 0 ? `${str}-` : "";
}
(0, _chunkDLQEHMXDMjs.__name)(strWithHyphen, "strWithHyphen");
var erRenderer_default = {
    setConf,
    draw
};
// src/diagrams/er/styles.js
var getStyles = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((options)=>`
  .entityBox {
    fill: ${options.mainBkg};
    stroke: ${options.nodeBorder};
  }

  .attributeBoxOdd {
    fill: ${options.attributeBackgroundColorOdd};
    stroke: ${options.nodeBorder};
  }

  .attributeBoxEven {
    fill:  ${options.attributeBackgroundColorEven};
    stroke: ${options.nodeBorder};
  }

  .relationshipLabelBox {
    fill: ${options.tertiaryColor};
    opacity: 0.7;
    background-color: ${options.tertiaryColor};
      rect {
        opacity: 0.5;
      }
  }

    .relationshipLine {
      stroke: ${options.lineColor};
    }

  .entityTitleText {
    text-anchor: middle;
    font-size: 18px;
    fill: ${options.textColor};
  }    
  #MD_PARENT_START {
    fill: #f5f5f5 !important;
    stroke: ${options.lineColor} !important;
    stroke-width: 1;
  }
  #MD_PARENT_END {
    fill: #f5f5f5 !important;
    stroke: ${options.lineColor} !important;
    stroke-width: 1;
  }
  
`, "getStyles");
var styles_default = getStyles;
// src/diagrams/er/erDiagram.ts
var diagram = {
    parser: erDiagram_default,
    db: erDb_default,
    renderer: erRenderer_default,
    styles: styles_default
};

},{"./chunk-CN5XARC6.mjs":"c7FQv","./chunk-ULVYQCHC.mjs":"h2Yj3","./chunk-I7ZFS43C.mjs":"huUtc","./chunk-GKOISANM.mjs":"5yZtl","./chunk-DD37ZF33.mjs":"f4pI5","./chunk-TZBO7MLI.mjs":"fY3tK","./chunk-GRZAG2UZ.mjs":"d1pnj","./chunk-HD3LK5B5.mjs":"lpo2u","./chunk-DLQEHMXD.mjs":"h5YRf","@parcel/transformer-js/src/esmodule-helpers.js":"1cdyk"}],"h2Yj3":[function(require,module,exports,__globalThis) {
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

},{"./chunk-TZBO7MLI.mjs":"fY3tK","./chunk-GRZAG2UZ.mjs":"d1pnj","./chunk-HD3LK5B5.mjs":"lpo2u","./chunk-DLQEHMXD.mjs":"h5YRf","@parcel/transformer-js/src/esmodule-helpers.js":"1cdyk"}]},["03rCu"], null, "parcelRequire6955", {})

//# sourceMappingURL=erDiagram-XPQADTZV.53f23c47.js.map
