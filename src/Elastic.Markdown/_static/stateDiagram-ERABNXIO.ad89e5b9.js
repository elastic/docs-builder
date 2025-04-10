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
})({"dJCjG":[function(require,module,exports,__globalThis) {
var global = arguments[3];
var HMR_HOST = null;
var HMR_PORT = 1234;
var HMR_SERVER_PORT = 1234;
var HMR_SECURE = false;
var HMR_ENV_HASH = "febfef3c5b467b16";
var HMR_USE_SSE = false;
module.bundle.HMR_BUNDLE_ID = "76e397d7ad89e5b9";
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

},{}],"6vHNQ":[function(require,module,exports,__globalThis) {
var parcelHelpers = require("@parcel/transformer-js/src/esmodule-helpers.js");
parcelHelpers.defineInteropFlag(exports);
parcelHelpers.export(exports, "diagram", ()=>diagram);
var _chunk63NMYVOQMjs = require("./chunk-63NMYVOQ.mjs");
var _chunkGVMN75T7Mjs = require("./chunk-GVMN75T7.mjs");
var _chunkHKQCUR3CMjs = require("./chunk-HKQCUR3C.mjs");
var _chunkM7N4Q5GZMjs = require("./chunk-M7N4Q5GZ.mjs");
var _chunkC6CSAIDWMjs = require("./chunk-C6CSAIDW.mjs");
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
// src/diagrams/state/id-cache.js
var idCache = {};
var set = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((key, val)=>{
    idCache[key] = val;
}, "set");
var get = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((k)=>idCache[k], "get");
var keys = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(()=>Object.keys(idCache), "keys");
var size = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(()=>keys().length, "size");
var id_cache_default = {
    get,
    set,
    keys,
    size
};
// src/diagrams/state/shapes.js
var drawStartState = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((g)=>g.append("circle").attr("class", "start-state").attr("r", (0, _chunkDD37ZF33Mjs.getConfig2)().state.sizeUnit).attr("cx", (0, _chunkDD37ZF33Mjs.getConfig2)().state.padding + (0, _chunkDD37ZF33Mjs.getConfig2)().state.sizeUnit).attr("cy", (0, _chunkDD37ZF33Mjs.getConfig2)().state.padding + (0, _chunkDD37ZF33Mjs.getConfig2)().state.sizeUnit), "drawStartState");
var drawDivider = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((g)=>g.append("line").style("stroke", "grey").style("stroke-dasharray", "3").attr("x1", (0, _chunkDD37ZF33Mjs.getConfig2)().state.textHeight).attr("class", "divider").attr("x2", (0, _chunkDD37ZF33Mjs.getConfig2)().state.textHeight * 2).attr("y1", 0).attr("y2", 0), "drawDivider");
var drawSimpleState = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((g, stateDef)=>{
    const state = g.append("text").attr("x", 2 * (0, _chunkDD37ZF33Mjs.getConfig2)().state.padding).attr("y", (0, _chunkDD37ZF33Mjs.getConfig2)().state.textHeight + 2 * (0, _chunkDD37ZF33Mjs.getConfig2)().state.padding).attr("font-size", (0, _chunkDD37ZF33Mjs.getConfig2)().state.fontSize).attr("class", "state-title").text(stateDef.id);
    const classBox = state.node().getBBox();
    g.insert("rect", ":first-child").attr("x", (0, _chunkDD37ZF33Mjs.getConfig2)().state.padding).attr("y", (0, _chunkDD37ZF33Mjs.getConfig2)().state.padding).attr("width", classBox.width + 2 * (0, _chunkDD37ZF33Mjs.getConfig2)().state.padding).attr("height", classBox.height + 2 * (0, _chunkDD37ZF33Mjs.getConfig2)().state.padding).attr("rx", (0, _chunkDD37ZF33Mjs.getConfig2)().state.radius);
    return state;
}, "drawSimpleState");
var drawDescrState = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((g, stateDef)=>{
    const addTspan = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(textEl, txt, isFirst2) {
        const tSpan = textEl.append("tspan").attr("x", 2 * (0, _chunkDD37ZF33Mjs.getConfig2)().state.padding).text(txt);
        if (!isFirst2) tSpan.attr("dy", (0, _chunkDD37ZF33Mjs.getConfig2)().state.textHeight);
    }, "addTspan");
    const title = g.append("text").attr("x", 2 * (0, _chunkDD37ZF33Mjs.getConfig2)().state.padding).attr("y", (0, _chunkDD37ZF33Mjs.getConfig2)().state.textHeight + 1.3 * (0, _chunkDD37ZF33Mjs.getConfig2)().state.padding).attr("font-size", (0, _chunkDD37ZF33Mjs.getConfig2)().state.fontSize).attr("class", "state-title").text(stateDef.descriptions[0]);
    const titleBox = title.node().getBBox();
    const titleHeight = titleBox.height;
    const description = g.append("text").attr("x", (0, _chunkDD37ZF33Mjs.getConfig2)().state.padding).attr("y", titleHeight + (0, _chunkDD37ZF33Mjs.getConfig2)().state.padding * 0.4 + (0, _chunkDD37ZF33Mjs.getConfig2)().state.dividerMargin + (0, _chunkDD37ZF33Mjs.getConfig2)().state.textHeight).attr("class", "state-description");
    let isFirst = true;
    let isSecond = true;
    stateDef.descriptions.forEach(function(descr) {
        if (!isFirst) {
            addTspan(description, descr, isSecond);
            isSecond = false;
        }
        isFirst = false;
    });
    const descrLine = g.append("line").attr("x1", (0, _chunkDD37ZF33Mjs.getConfig2)().state.padding).attr("y1", (0, _chunkDD37ZF33Mjs.getConfig2)().state.padding + titleHeight + (0, _chunkDD37ZF33Mjs.getConfig2)().state.dividerMargin / 2).attr("y2", (0, _chunkDD37ZF33Mjs.getConfig2)().state.padding + titleHeight + (0, _chunkDD37ZF33Mjs.getConfig2)().state.dividerMargin / 2).attr("class", "descr-divider");
    const descrBox = description.node().getBBox();
    const width = Math.max(descrBox.width, titleBox.width);
    descrLine.attr("x2", width + 3 * (0, _chunkDD37ZF33Mjs.getConfig2)().state.padding);
    g.insert("rect", ":first-child").attr("x", (0, _chunkDD37ZF33Mjs.getConfig2)().state.padding).attr("y", (0, _chunkDD37ZF33Mjs.getConfig2)().state.padding).attr("width", width + 2 * (0, _chunkDD37ZF33Mjs.getConfig2)().state.padding).attr("height", descrBox.height + titleHeight + 2 * (0, _chunkDD37ZF33Mjs.getConfig2)().state.padding).attr("rx", (0, _chunkDD37ZF33Mjs.getConfig2)().state.radius);
    return g;
}, "drawDescrState");
var addTitleAndBox = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((g, stateDef, altBkg)=>{
    const pad = (0, _chunkDD37ZF33Mjs.getConfig2)().state.padding;
    const dblPad = 2 * (0, _chunkDD37ZF33Mjs.getConfig2)().state.padding;
    const orgBox = g.node().getBBox();
    const orgWidth = orgBox.width;
    const orgX = orgBox.x;
    const title = g.append("text").attr("x", 0).attr("y", (0, _chunkDD37ZF33Mjs.getConfig2)().state.titleShift).attr("font-size", (0, _chunkDD37ZF33Mjs.getConfig2)().state.fontSize).attr("class", "state-title").text(stateDef.id);
    const titleBox = title.node().getBBox();
    const titleWidth = titleBox.width + dblPad;
    let width = Math.max(titleWidth, orgWidth);
    if (width === orgWidth) width = width + dblPad;
    let startX;
    const graphBox = g.node().getBBox();
    stateDef.doc;
    startX = orgX - pad;
    if (titleWidth > orgWidth) startX = (orgWidth - width) / 2 + pad;
    if (Math.abs(orgX - graphBox.x) < pad && titleWidth > orgWidth) startX = orgX - (titleWidth - orgWidth) / 2;
    const lineY = 1 - (0, _chunkDD37ZF33Mjs.getConfig2)().state.textHeight;
    g.insert("rect", ":first-child").attr("x", startX).attr("y", lineY).attr("class", altBkg ? "alt-composit" : "composit").attr("width", width).attr("height", graphBox.height + (0, _chunkDD37ZF33Mjs.getConfig2)().state.textHeight + (0, _chunkDD37ZF33Mjs.getConfig2)().state.titleShift + 1).attr("rx", "0");
    title.attr("x", startX + pad);
    if (titleWidth <= orgWidth) title.attr("x", orgX + (width - dblPad) / 2 - titleWidth / 2 + pad);
    g.insert("rect", ":first-child").attr("x", startX).attr("y", (0, _chunkDD37ZF33Mjs.getConfig2)().state.titleShift - (0, _chunkDD37ZF33Mjs.getConfig2)().state.textHeight - (0, _chunkDD37ZF33Mjs.getConfig2)().state.padding).attr("width", width).attr("height", (0, _chunkDD37ZF33Mjs.getConfig2)().state.textHeight * 3).attr("rx", (0, _chunkDD37ZF33Mjs.getConfig2)().state.radius);
    g.insert("rect", ":first-child").attr("x", startX).attr("y", (0, _chunkDD37ZF33Mjs.getConfig2)().state.titleShift - (0, _chunkDD37ZF33Mjs.getConfig2)().state.textHeight - (0, _chunkDD37ZF33Mjs.getConfig2)().state.padding).attr("width", width).attr("height", graphBox.height + 3 + 2 * (0, _chunkDD37ZF33Mjs.getConfig2)().state.textHeight).attr("rx", (0, _chunkDD37ZF33Mjs.getConfig2)().state.radius);
    return g;
}, "addTitleAndBox");
var drawEndState = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((g)=>{
    g.append("circle").attr("class", "end-state-outer").attr("r", (0, _chunkDD37ZF33Mjs.getConfig2)().state.sizeUnit + (0, _chunkDD37ZF33Mjs.getConfig2)().state.miniPadding).attr("cx", (0, _chunkDD37ZF33Mjs.getConfig2)().state.padding + (0, _chunkDD37ZF33Mjs.getConfig2)().state.sizeUnit + (0, _chunkDD37ZF33Mjs.getConfig2)().state.miniPadding).attr("cy", (0, _chunkDD37ZF33Mjs.getConfig2)().state.padding + (0, _chunkDD37ZF33Mjs.getConfig2)().state.sizeUnit + (0, _chunkDD37ZF33Mjs.getConfig2)().state.miniPadding);
    return g.append("circle").attr("class", "end-state-inner").attr("r", (0, _chunkDD37ZF33Mjs.getConfig2)().state.sizeUnit).attr("cx", (0, _chunkDD37ZF33Mjs.getConfig2)().state.padding + (0, _chunkDD37ZF33Mjs.getConfig2)().state.sizeUnit + 2).attr("cy", (0, _chunkDD37ZF33Mjs.getConfig2)().state.padding + (0, _chunkDD37ZF33Mjs.getConfig2)().state.sizeUnit + 2);
}, "drawEndState");
var drawForkJoinState = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((g, stateDef)=>{
    let width = (0, _chunkDD37ZF33Mjs.getConfig2)().state.forkWidth;
    let height = (0, _chunkDD37ZF33Mjs.getConfig2)().state.forkHeight;
    if (stateDef.parentId) {
        let tmp = width;
        width = height;
        height = tmp;
    }
    return g.append("rect").style("stroke", "black").style("fill", "black").attr("width", width).attr("height", height).attr("x", (0, _chunkDD37ZF33Mjs.getConfig2)().state.padding).attr("y", (0, _chunkDD37ZF33Mjs.getConfig2)().state.padding);
}, "drawForkJoinState");
var _drawLongText = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((_text, x, y, g)=>{
    let textHeight = 0;
    const textElem = g.append("text");
    textElem.style("text-anchor", "start");
    textElem.attr("class", "noteText");
    let text = _text.replace(/\r\n/g, "<br/>");
    text = text.replace(/\n/g, "<br/>");
    const lines = text.split((0, _chunkDD37ZF33Mjs.common_default).lineBreakRegex);
    let tHeight = 1.25 * (0, _chunkDD37ZF33Mjs.getConfig2)().state.noteMargin;
    for (const line of lines){
        const txt = line.trim();
        if (txt.length > 0) {
            const span = textElem.append("tspan");
            span.text(txt);
            if (tHeight === 0) {
                const textBounds = span.node().getBBox();
                tHeight += textBounds.height;
            }
            textHeight += tHeight;
            span.attr("x", x + (0, _chunkDD37ZF33Mjs.getConfig2)().state.noteMargin);
            span.attr("y", y + textHeight + 1.25 * (0, _chunkDD37ZF33Mjs.getConfig2)().state.noteMargin);
        }
    }
    return {
        textWidth: textElem.node().getBBox().width,
        textHeight
    };
}, "_drawLongText");
var drawNote = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((text, g)=>{
    g.attr("class", "state-note");
    const note = g.append("rect").attr("x", 0).attr("y", (0, _chunkDD37ZF33Mjs.getConfig2)().state.padding);
    const rectElem = g.append("g");
    const { textWidth, textHeight } = _drawLongText(text, 0, 0, rectElem);
    note.attr("height", textHeight + 2 * (0, _chunkDD37ZF33Mjs.getConfig2)().state.noteMargin);
    note.attr("width", textWidth + (0, _chunkDD37ZF33Mjs.getConfig2)().state.noteMargin * 2);
    return note;
}, "drawNote");
var drawState = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(elem, stateDef) {
    const id = stateDef.id;
    const stateInfo = {
        id,
        label: stateDef.id,
        width: 0,
        height: 0
    };
    const g = elem.append("g").attr("id", id).attr("class", "stateGroup");
    if (stateDef.type === "start") drawStartState(g);
    if (stateDef.type === "end") drawEndState(g);
    if (stateDef.type === "fork" || stateDef.type === "join") drawForkJoinState(g, stateDef);
    if (stateDef.type === "note") drawNote(stateDef.note.text, g);
    if (stateDef.type === "divider") drawDivider(g);
    if (stateDef.type === "default" && stateDef.descriptions.length === 0) drawSimpleState(g, stateDef);
    if (stateDef.type === "default" && stateDef.descriptions.length > 0) drawDescrState(g, stateDef);
    const stateBox = g.node().getBBox();
    stateInfo.width = stateBox.width + 2 * (0, _chunkDD37ZF33Mjs.getConfig2)().state.padding;
    stateInfo.height = stateBox.height + 2 * (0, _chunkDD37ZF33Mjs.getConfig2)().state.padding;
    id_cache_default.set(id, stateInfo);
    return stateInfo;
}, "drawState");
var edgeCount = 0;
var drawEdge = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(elem, path, relation) {
    const getRelationType = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(type) {
        switch(type){
            case (0, _chunk63NMYVOQMjs.stateDb_default).relationType.AGGREGATION:
                return "aggregation";
            case (0, _chunk63NMYVOQMjs.stateDb_default).relationType.EXTENSION:
                return "extension";
            case (0, _chunk63NMYVOQMjs.stateDb_default).relationType.COMPOSITION:
                return "composition";
            case (0, _chunk63NMYVOQMjs.stateDb_default).relationType.DEPENDENCY:
                return "dependency";
        }
    }, "getRelationType");
    path.points = path.points.filter((p)=>!Number.isNaN(p.y));
    const lineData = path.points;
    const lineFunction = (0, _chunkDD37ZF33Mjs.line_default)().x(function(d) {
        return d.x;
    }).y(function(d) {
        return d.y;
    }).curve((0, _chunkDD37ZF33Mjs.basis_default));
    const svgPath = elem.append("path").attr("d", lineFunction(lineData)).attr("id", "edge" + edgeCount).attr("class", "transition");
    let url = "";
    if ((0, _chunkDD37ZF33Mjs.getConfig2)().state.arrowMarkerAbsolute) {
        url = window.location.protocol + "//" + window.location.host + window.location.pathname + window.location.search;
        url = url.replace(/\(/g, "\\(");
        url = url.replace(/\)/g, "\\)");
    }
    svgPath.attr("marker-end", "url(" + url + "#" + getRelationType((0, _chunk63NMYVOQMjs.stateDb_default).relationType.DEPENDENCY) + "End)");
    if (relation.title !== void 0) {
        const label = elem.append("g").attr("class", "stateLabel");
        const { x, y } = (0, _chunkI7ZFS43CMjs.utils_default).calcLabelPosition(path.points);
        const rows = (0, _chunkDD37ZF33Mjs.common_default).getRows(relation.title);
        let titleHeight = 0;
        const titleRows = [];
        let maxWidth = 0;
        let minX = 0;
        for(let i = 0; i <= rows.length; i++){
            const title = label.append("text").attr("text-anchor", "middle").text(rows[i]).attr("x", x).attr("y", y + titleHeight);
            const boundsTmp = title.node().getBBox();
            maxWidth = Math.max(maxWidth, boundsTmp.width);
            minX = Math.min(minX, boundsTmp.x);
            (0, _chunkDD37ZF33Mjs.log).info(boundsTmp.x, x, y + titleHeight);
            if (titleHeight === 0) {
                const titleBox = title.node().getBBox();
                titleHeight = titleBox.height;
                (0, _chunkDD37ZF33Mjs.log).info("Title height", titleHeight, y);
            }
            titleRows.push(title);
        }
        let boxHeight = titleHeight * rows.length;
        if (rows.length > 1) {
            const heightAdj = (rows.length - 1) * titleHeight * 0.5;
            titleRows.forEach((title, i)=>title.attr("y", y + i * titleHeight - heightAdj));
            boxHeight = titleHeight * rows.length;
        }
        const bounds = label.node().getBBox();
        label.insert("rect", ":first-child").attr("class", "box").attr("x", x - maxWidth / 2 - (0, _chunkDD37ZF33Mjs.getConfig2)().state.padding / 2).attr("y", y - boxHeight / 2 - (0, _chunkDD37ZF33Mjs.getConfig2)().state.padding / 2 - 3.5).attr("width", maxWidth + (0, _chunkDD37ZF33Mjs.getConfig2)().state.padding).attr("height", boxHeight + (0, _chunkDD37ZF33Mjs.getConfig2)().state.padding);
        (0, _chunkDD37ZF33Mjs.log).info(bounds);
    }
    edgeCount++;
}, "drawEdge");
// src/diagrams/state/stateRenderer.js
var conf;
var transformationLog = {};
var setConf = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function() {}, "setConf");
var insertMarkers = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(elem) {
    elem.append("defs").append("marker").attr("id", "dependencyEnd").attr("refX", 19).attr("refY", 7).attr("markerWidth", 20).attr("markerHeight", 28).attr("orient", "auto").append("path").attr("d", "M 19,7 L9,13 L14,7 L9,1 Z");
}, "insertMarkers");
var draw = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(text, id, _version, diagObj) {
    conf = (0, _chunkDD37ZF33Mjs.getConfig2)().state;
    const securityLevel = (0, _chunkDD37ZF33Mjs.getConfig2)().securityLevel;
    let sandboxElement;
    if (securityLevel === "sandbox") sandboxElement = (0, _chunkDD37ZF33Mjs.select_default)("#i" + id);
    const root = securityLevel === "sandbox" ? (0, _chunkDD37ZF33Mjs.select_default)(sandboxElement.nodes()[0].contentDocument.body) : (0, _chunkDD37ZF33Mjs.select_default)("body");
    const doc = securityLevel === "sandbox" ? sandboxElement.nodes()[0].contentDocument : document;
    (0, _chunkDD37ZF33Mjs.log).debug("Rendering diagram " + text);
    const diagram2 = root.select(`[id='${id}']`);
    insertMarkers(diagram2);
    const rootDoc = diagObj.db.getRootDoc();
    renderDoc(rootDoc, diagram2, void 0, false, root, doc, diagObj);
    const padding = conf.padding;
    const bounds = diagram2.node().getBBox();
    const width = bounds.width + padding * 2;
    const height = bounds.height + padding * 2;
    const svgWidth = width * 1.75;
    (0, _chunkDD37ZF33Mjs.configureSvgSize)(diagram2, height, svgWidth, conf.useMaxWidth);
    diagram2.attr("viewBox", `${bounds.x - conf.padding}  ${bounds.y - conf.padding} ` + width + " " + height);
}, "draw");
var getLabelWidth = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((text)=>{
    return text ? text.length * conf.fontSizeFactor : 1;
}, "getLabelWidth");
var renderDoc = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((doc, diagram2, parentId, altBkg, root, domDocument, diagObj)=>{
    const graph = new (0, _chunkULVYQCHCMjs.Graph)({
        compound: true,
        multigraph: true
    });
    let i;
    let edgeFreeDoc = true;
    for(i = 0; i < doc.length; i++)if (doc[i].stmt === "relation") {
        edgeFreeDoc = false;
        break;
    }
    if (parentId) graph.setGraph({
        rankdir: "LR",
        multigraph: true,
        compound: true,
        // acyclicer: 'greedy',
        ranker: "tight-tree",
        ranksep: edgeFreeDoc ? 1 : conf.edgeLengthFactor,
        nodeSep: edgeFreeDoc ? 1 : 50,
        isMultiGraph: true
    });
    else graph.setGraph({
        rankdir: "TB",
        multigraph: true,
        compound: true,
        // isCompound: true,
        // acyclicer: 'greedy',
        // ranker: 'longest-path'
        ranksep: edgeFreeDoc ? 1 : conf.edgeLengthFactor,
        nodeSep: edgeFreeDoc ? 1 : 50,
        ranker: "tight-tree",
        // ranker: 'network-simplex'
        isMultiGraph: true
    });
    graph.setDefaultEdgeLabel(function() {
        return {};
    });
    diagObj.db.extract(doc);
    const states = diagObj.db.getStates();
    const relations = diagObj.db.getRelations();
    const keys2 = Object.keys(states);
    let first = true;
    for (const key of keys2){
        const stateDef = states[key];
        if (parentId) stateDef.parentId = parentId;
        let node;
        if (stateDef.doc) {
            let sub = diagram2.append("g").attr("id", stateDef.id).attr("class", "stateGroup");
            node = renderDoc(stateDef.doc, sub, stateDef.id, !altBkg, root, domDocument, diagObj);
            if (first) {
                sub = addTitleAndBox(sub, stateDef, altBkg);
                let boxBounds = sub.node().getBBox();
                node.width = boxBounds.width;
                node.height = boxBounds.height + conf.padding / 2;
                transformationLog[stateDef.id] = {
                    y: conf.compositTitleSize
                };
            } else {
                let boxBounds = sub.node().getBBox();
                node.width = boxBounds.width;
                node.height = boxBounds.height;
            }
        } else node = drawState(diagram2, stateDef, graph);
        if (stateDef.note) {
            const noteDef = {
                descriptions: [],
                id: stateDef.id + "-note",
                note: stateDef.note,
                type: "note"
            };
            const note = drawState(diagram2, noteDef, graph);
            if (stateDef.note.position === "left of") {
                graph.setNode(node.id + "-note", note);
                graph.setNode(node.id, node);
            } else {
                graph.setNode(node.id, node);
                graph.setNode(node.id + "-note", note);
            }
            graph.setParent(node.id, node.id + "-group");
            graph.setParent(node.id + "-note", node.id + "-group");
        } else graph.setNode(node.id, node);
    }
    (0, _chunkDD37ZF33Mjs.log).debug("Count=", graph.nodeCount(), graph);
    let cnt = 0;
    relations.forEach(function(relation) {
        cnt++;
        (0, _chunkDD37ZF33Mjs.log).debug("Setting edge", relation);
        graph.setEdge(relation.id1, relation.id2, {
            relation,
            width: getLabelWidth(relation.title),
            height: conf.labelHeight * (0, _chunkDD37ZF33Mjs.common_default).getRows(relation.title).length,
            labelpos: "c"
        }, "id" + cnt);
    });
    (0, _chunkCN5XARC6Mjs.layout)(graph);
    (0, _chunkDD37ZF33Mjs.log).debug("Graph after layout", graph.nodes());
    const svgElem = diagram2.node();
    graph.nodes().forEach(function(v) {
        if (v !== void 0 && graph.node(v) !== void 0) {
            (0, _chunkDD37ZF33Mjs.log).warn("Node " + v + ": " + JSON.stringify(graph.node(v)));
            root.select("#" + svgElem.id + " #" + v).attr("transform", "translate(" + (graph.node(v).x - graph.node(v).width / 2) + "," + (graph.node(v).y + (transformationLog[v] ? transformationLog[v].y : 0) - graph.node(v).height / 2) + " )");
            root.select("#" + svgElem.id + " #" + v).attr("data-x-shift", graph.node(v).x - graph.node(v).width / 2);
            const dividers = domDocument.querySelectorAll("#" + svgElem.id + " #" + v + " .divider");
            dividers.forEach((divider)=>{
                const parent = divider.parentElement;
                let pWidth = 0;
                let pShift = 0;
                if (parent) {
                    if (parent.parentElement) pWidth = parent.parentElement.getBBox().width;
                    pShift = parseInt(parent.getAttribute("data-x-shift"), 10);
                    if (Number.isNaN(pShift)) pShift = 0;
                }
                divider.setAttribute("x1", 0 - pShift + 8);
                divider.setAttribute("x2", pWidth - pShift - 8);
            });
        } else (0, _chunkDD37ZF33Mjs.log).debug("No Node " + v + ": " + JSON.stringify(graph.node(v)));
    });
    let stateBox = svgElem.getBBox();
    graph.edges().forEach(function(e) {
        if (e !== void 0 && graph.edge(e) !== void 0) {
            (0, _chunkDD37ZF33Mjs.log).debug("Edge " + e.v + " -> " + e.w + ": " + JSON.stringify(graph.edge(e)));
            drawEdge(diagram2, graph.edge(e), graph.edge(e).relation);
        }
    });
    stateBox = svgElem.getBBox();
    const stateInfo = {
        id: parentId ? parentId : "root",
        label: parentId ? parentId : "root",
        width: 0,
        height: 0
    };
    stateInfo.width = stateBox.width + 2 * conf.padding;
    stateInfo.height = stateBox.height + 2 * conf.padding;
    (0, _chunkDD37ZF33Mjs.log).debug("Doc rendered", stateInfo, graph);
    return stateInfo;
}, "renderDoc");
var stateRenderer_default = {
    setConf,
    draw
};
// src/diagrams/state/stateDiagram.ts
var diagram = {
    parser: (0, _chunk63NMYVOQMjs.stateDiagram_default),
    db: (0, _chunk63NMYVOQMjs.stateDb_default),
    renderer: stateRenderer_default,
    styles: (0, _chunk63NMYVOQMjs.styles_default),
    init: /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((cnf)=>{
        if (!cnf.state) cnf.state = {};
        cnf.state.arrowMarkerAbsolute = cnf.arrowMarkerAbsolute;
        (0, _chunk63NMYVOQMjs.stateDb_default).clear();
    }, "init")
};

},{"./chunk-63NMYVOQ.mjs":"6kD3k","./chunk-GVMN75T7.mjs":"eQN3e","./chunk-HKQCUR3C.mjs":"fcjSH","./chunk-M7N4Q5GZ.mjs":"haj7Y","./chunk-C6CSAIDW.mjs":"9w36H","./chunk-KW7S66XI.mjs":"98JMR","./chunk-YP6PVJQ3.mjs":"21NKC","./chunk-CN5XARC6.mjs":"c7FQv","./chunk-ULVYQCHC.mjs":"h2Yj3","./chunk-I7ZFS43C.mjs":"huUtc","./chunk-GKOISANM.mjs":"5yZtl","./chunk-DD37ZF33.mjs":"f4pI5","./chunk-TZBO7MLI.mjs":"fY3tK","./chunk-GRZAG2UZ.mjs":"d1pnj","./chunk-HD3LK5B5.mjs":"lpo2u","./chunk-DLQEHMXD.mjs":"h5YRf","@parcel/transformer-js/src/esmodule-helpers.js":"1cdyk"}],"eQN3e":[function(require,module,exports,__globalThis) {
var parcelHelpers = require("@parcel/transformer-js/src/esmodule-helpers.js");
parcelHelpers.defineInteropFlag(exports);
parcelHelpers.export(exports, "getDiagramElement", ()=>getDiagramElement);
parcelHelpers.export(exports, "setupViewPortForSVG", ()=>setupViewPortForSVG);
var _chunkDD37ZF33Mjs = require("./chunk-DD37ZF33.mjs");
var _chunkDLQEHMXDMjs = require("./chunk-DLQEHMXD.mjs");
// src/rendering-util/insertElementsForSize.js
var getDiagramElement = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((id, securityLevel)=>{
    let sandboxElement;
    if (securityLevel === "sandbox") sandboxElement = (0, _chunkDD37ZF33Mjs.select_default)("#i" + id);
    const root = securityLevel === "sandbox" ? (0, _chunkDD37ZF33Mjs.select_default)(sandboxElement.nodes()[0].contentDocument.body) : (0, _chunkDD37ZF33Mjs.select_default)("body");
    const svg = root.select(`[id="${id}"]`);
    return svg;
}, "getDiagramElement");
// src/rendering-util/setupViewPortForSVG.ts
var setupViewPortForSVG = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((svg, padding, cssDiagram, useMaxWidth)=>{
    svg.attr("class", cssDiagram);
    const { width, height, x, y } = calculateDimensionsWithPadding(svg, padding);
    (0, _chunkDD37ZF33Mjs.configureSvgSize)(svg, height, width, useMaxWidth);
    const viewBox = createViewBox(x, y, width, height, padding);
    svg.attr("viewBox", viewBox);
    (0, _chunkDD37ZF33Mjs.log).debug(`viewBox configured: ${viewBox} with padding: ${padding}`);
}, "setupViewPortForSVG");
var calculateDimensionsWithPadding = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((svg, padding)=>{
    const bounds = svg.node()?.getBBox() || {
        width: 0,
        height: 0,
        x: 0,
        y: 0
    };
    return {
        width: bounds.width + padding * 2,
        height: bounds.height + padding * 2,
        x: bounds.x,
        y: bounds.y
    };
}, "calculateDimensionsWithPadding");
var createViewBox = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((x, y, width, height, padding)=>{
    return `${x - padding} ${y - padding} ${width} ${height}`;
}, "createViewBox");

},{"./chunk-DD37ZF33.mjs":"f4pI5","./chunk-DLQEHMXD.mjs":"h5YRf","@parcel/transformer-js/src/esmodule-helpers.js":"1cdyk"}],"h2Yj3":[function(require,module,exports,__globalThis) {
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

},{"./chunk-TZBO7MLI.mjs":"fY3tK","./chunk-GRZAG2UZ.mjs":"d1pnj","./chunk-HD3LK5B5.mjs":"lpo2u","./chunk-DLQEHMXD.mjs":"h5YRf","@parcel/transformer-js/src/esmodule-helpers.js":"1cdyk"}]},["dJCjG"], null, "parcelRequire6955", {})

//# sourceMappingURL=stateDiagram-ERABNXIO.ad89e5b9.js.map
