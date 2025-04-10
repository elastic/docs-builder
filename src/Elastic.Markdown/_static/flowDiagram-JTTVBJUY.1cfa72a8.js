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
})({"87vNq":[function(require,module,exports,__globalThis) {
var global = arguments[3];
var HMR_HOST = null;
var HMR_PORT = 1234;
var HMR_SERVER_PORT = 1234;
var HMR_SECURE = false;
var HMR_ENV_HASH = "febfef3c5b467b16";
var HMR_USE_SSE = false;
module.bundle.HMR_BUNDLE_ID = "1e825db01cfa72a8";
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

},{}],"6LAlC":[function(require,module,exports,__globalThis) {
var parcelHelpers = require("@parcel/transformer-js/src/esmodule-helpers.js");
parcelHelpers.defineInteropFlag(exports);
parcelHelpers.export(exports, "a", ()=>_);
parcelHelpers.export(exports, "b", ()=>et);
parcelHelpers.export(exports, "c", ()=>w);
parcelHelpers.export(exports, "d", ()=>Rr);
parcelHelpers.export(exports, "e", ()=>zr);
parcelHelpers.export(exports, "f", ()=>Or);
parcelHelpers.export(exports, "g", ()=>$r);
var _chunkKMOJB3TBMjs = require("./chunk-KMOJB3TB.mjs");
var _chunkAC3VT7B7Mjs = require("./chunk-AC3VT7B7.mjs");
var _chunkNQURTBEVMjs = require("./chunk-NQURTBEV.mjs");
var _chunkGTKDMUJJMjs = require("./chunk-GTKDMUJJ.mjs");
function G(l, t) {
    t && l.attr("style", t);
}
(0, _chunkGTKDMUJJMjs.a)(G, "applyStyle");
function nt(l) {
    let t = (0, _chunkNQURTBEVMjs.fa)(document.createElementNS("http://www.w3.org/2000/svg", "foreignObject")), c = t.append("xhtml:div"), s = l.label, a = l.isNode ? "nodeLabel" : "edgeLabel", r = c.append("span");
    return r.html(s), G(r, l.labelStyle), r.attr("class", a), G(c, l.labelStyle), c.style("display", "inline-block"), c.style("white-space", "nowrap"), c.attr("xmlns", "http://www.w3.org/1999/xhtml"), t.node();
}
(0, _chunkGTKDMUJJMjs.a)(nt, "addHtmlLabel");
var ht = (0, _chunkGTKDMUJJMjs.a)((l, t, c, s)=>{
    let a = l || "";
    if (typeof a == "object" && (a = a[0]), (0, _chunkNQURTBEVMjs.G)((0, _chunkNQURTBEVMjs.X)().flowchart.htmlLabels)) {
        a = a.replace(/\\n|\n/g, "<br />"), (0, _chunkNQURTBEVMjs.b).debug("vertexText" + a);
        let r = {
            isNode: s,
            label: (0, _chunkKMOJB3TBMjs.c)((0, _chunkAC3VT7B7Mjs.o)(a)),
            labelStyle: t.replace("fill:", "color:")
        };
        return nt(r);
    } else {
        let r = document.createElementNS("http://www.w3.org/2000/svg", "text");
        r.setAttribute("style", t.replace("color:", "fill:"));
        let e = [];
        typeof a == "string" ? e = a.split(/\\n|\n|<br\s*\/?>/gi) : Array.isArray(a) ? e = a : e = [];
        for (let i of e){
            let n = document.createElementNS("http://www.w3.org/2000/svg", "tspan");
            n.setAttributeNS("http://www.w3.org/XML/1998/namespace", "xml:space", "preserve"), n.setAttribute("dy", "1em"), n.setAttribute("x", "0"), c ? n.setAttribute("class", "title-row") : n.setAttribute("class", "row"), n.textContent = i.trim(), r.appendChild(n);
        }
        return r;
    }
}, "createLabel"), _ = ht;
var m = (0, _chunkGTKDMUJJMjs.a)(async (l, t, c, s)=>{
    let a = (0, _chunkNQURTBEVMjs.X)(), r, e = t.useHtmlLabels || (0, _chunkNQURTBEVMjs.G)(a.flowchart.htmlLabels);
    c ? r = c : r = "node default";
    let i = l.insert("g").attr("class", r).attr("id", t.domId || t.id), n = i.insert("g").attr("class", "label").attr("style", t.labelStyle), y;
    t.labelText === void 0 ? y = "" : y = typeof t.labelText == "string" ? t.labelText : t.labelText[0];
    let h = n.node(), f;
    t.labelType === "markdown" ? f = (0, _chunkKMOJB3TBMjs.d)(n, (0, _chunkNQURTBEVMjs.F)((0, _chunkAC3VT7B7Mjs.o)(y), a), {
        useHtmlLabels: e,
        width: t.width || a.flowchart.wrappingWidth,
        classes: "markdown-node-label"
    }, a) : f = h.appendChild(_((0, _chunkNQURTBEVMjs.F)((0, _chunkAC3VT7B7Mjs.o)(y), a), t.labelStyle, !1, s));
    let g = f.getBBox(), x = t.padding / 2;
    if ((0, _chunkNQURTBEVMjs.G)(a.flowchart.htmlLabels)) {
        let p = f.children[0], v = (0, _chunkNQURTBEVMjs.fa)(f), d = p.getElementsByTagName("img");
        if (d) {
            let k = y.replace(/<img[^>]*>/g, "").trim() === "";
            await Promise.all([
                ...d
            ].map((S)=>new Promise((j)=>{
                    function D() {
                        if (S.style.display = "flex", S.style.flexDirection = "column", k) {
                            let P = a.fontSize ? a.fontSize : window.getComputedStyle(document.body).fontSize, $ = parseInt(P, 10) * 5 + "px";
                            S.style.minWidth = $, S.style.maxWidth = $;
                        } else S.style.width = "100%";
                        j(S);
                    }
                    (0, _chunkGTKDMUJJMjs.a)(D, "setupImage"), setTimeout(()=>{
                        S.complete && D();
                    }), S.addEventListener("error", D), S.addEventListener("load", D);
                })));
        }
        g = p.getBoundingClientRect(), v.attr("width", g.width), v.attr("height", g.height);
    }
    return e ? n.attr("transform", "translate(" + -g.width / 2 + ", " + -g.height / 2 + ")") : n.attr("transform", "translate(0, " + -g.height / 2 + ")"), t.centerLabel && n.attr("transform", "translate(" + -g.width / 2 + ", " + -g.height / 2 + ")"), n.insert("rect", ":first-child"), {
        shapeSvg: i,
        bbox: g,
        halfPadding: x,
        label: n
    };
}, "labelHelper"), w = (0, _chunkGTKDMUJJMjs.a)((l, t)=>{
    let c = t.node().getBBox();
    l.width = c.width, l.height = c.height;
}, "updateNodeBounds");
function M(l, t, c, s) {
    return l.insert("polygon", ":first-child").attr("points", s.map(function(a) {
        return a.x + "," + a.y;
    }).join(" ")).attr("class", "label-container").attr("transform", "translate(" + -t / 2 + "," + c / 2 + ")");
}
(0, _chunkGTKDMUJJMjs.a)(M, "insertPolygonShape");
var ot = (0, _chunkGTKDMUJJMjs.a)((l)=>{
    let t = new Set;
    for (let c of l)switch(c){
        case "x":
            t.add("right"), t.add("left");
            break;
        case "y":
            t.add("up"), t.add("down");
            break;
        default:
            t.add(c);
            break;
    }
    return t;
}, "expandAndDeduplicateDirections"), J = (0, _chunkGTKDMUJJMjs.a)((l, t, c)=>{
    let s = ot(l), a = 2, r = t.height + 2 * c.padding, e = r / a, i = t.width + 2 * e + c.padding, n = c.padding / 2;
    return s.has("right") && s.has("left") && s.has("up") && s.has("down") ? [
        {
            x: 0,
            y: 0
        },
        {
            x: e,
            y: 0
        },
        {
            x: i / 2,
            y: 2 * n
        },
        {
            x: i - e,
            y: 0
        },
        {
            x: i,
            y: 0
        },
        {
            x: i,
            y: -r / 3
        },
        {
            x: i + 2 * n,
            y: -r / 2
        },
        {
            x: i,
            y: -2 * r / 3
        },
        {
            x: i,
            y: -r
        },
        {
            x: i - e,
            y: -r
        },
        {
            x: i / 2,
            y: -r - 2 * n
        },
        {
            x: e,
            y: -r
        },
        {
            x: 0,
            y: -r
        },
        {
            x: 0,
            y: -2 * r / 3
        },
        {
            x: -2 * n,
            y: -r / 2
        },
        {
            x: 0,
            y: -r / 3
        }
    ] : s.has("right") && s.has("left") && s.has("up") ? [
        {
            x: e,
            y: 0
        },
        {
            x: i - e,
            y: 0
        },
        {
            x: i,
            y: -r / 2
        },
        {
            x: i - e,
            y: -r
        },
        {
            x: e,
            y: -r
        },
        {
            x: 0,
            y: -r / 2
        }
    ] : s.has("right") && s.has("left") && s.has("down") ? [
        {
            x: 0,
            y: 0
        },
        {
            x: e,
            y: -r
        },
        {
            x: i - e,
            y: -r
        },
        {
            x: i,
            y: 0
        }
    ] : s.has("right") && s.has("up") && s.has("down") ? [
        {
            x: 0,
            y: 0
        },
        {
            x: i,
            y: -e
        },
        {
            x: i,
            y: -r + e
        },
        {
            x: 0,
            y: -r
        }
    ] : s.has("left") && s.has("up") && s.has("down") ? [
        {
            x: i,
            y: 0
        },
        {
            x: 0,
            y: -e
        },
        {
            x: 0,
            y: -r + e
        },
        {
            x: i,
            y: -r
        }
    ] : s.has("right") && s.has("left") ? [
        {
            x: e,
            y: 0
        },
        {
            x: e,
            y: -n
        },
        {
            x: i - e,
            y: -n
        },
        {
            x: i - e,
            y: 0
        },
        {
            x: i,
            y: -r / 2
        },
        {
            x: i - e,
            y: -r
        },
        {
            x: i - e,
            y: -r + n
        },
        {
            x: e,
            y: -r + n
        },
        {
            x: e,
            y: -r
        },
        {
            x: 0,
            y: -r / 2
        }
    ] : s.has("up") && s.has("down") ? [
        {
            x: i / 2,
            y: 0
        },
        {
            x: 0,
            y: -n
        },
        {
            x: e,
            y: -n
        },
        {
            x: e,
            y: -r + n
        },
        {
            x: 0,
            y: -r + n
        },
        {
            x: i / 2,
            y: -r
        },
        {
            x: i,
            y: -r + n
        },
        {
            x: i - e,
            y: -r + n
        },
        {
            x: i - e,
            y: -n
        },
        {
            x: i,
            y: -n
        }
    ] : s.has("right") && s.has("up") ? [
        {
            x: 0,
            y: 0
        },
        {
            x: i,
            y: -e
        },
        {
            x: 0,
            y: -r
        }
    ] : s.has("right") && s.has("down") ? [
        {
            x: 0,
            y: 0
        },
        {
            x: i,
            y: 0
        },
        {
            x: 0,
            y: -r
        }
    ] : s.has("left") && s.has("up") ? [
        {
            x: i,
            y: 0
        },
        {
            x: 0,
            y: -e
        },
        {
            x: i,
            y: -r
        }
    ] : s.has("left") && s.has("down") ? [
        {
            x: i,
            y: 0
        },
        {
            x: 0,
            y: 0
        },
        {
            x: i,
            y: -r
        }
    ] : s.has("right") ? [
        {
            x: e,
            y: -n
        },
        {
            x: e,
            y: -n
        },
        {
            x: i - e,
            y: -n
        },
        {
            x: i - e,
            y: 0
        },
        {
            x: i,
            y: -r / 2
        },
        {
            x: i - e,
            y: -r
        },
        {
            x: i - e,
            y: -r + n
        },
        {
            x: e,
            y: -r + n
        },
        {
            x: e,
            y: -r + n
        }
    ] : s.has("left") ? [
        {
            x: e,
            y: 0
        },
        {
            x: e,
            y: -n
        },
        {
            x: i - e,
            y: -n
        },
        {
            x: i - e,
            y: -r + n
        },
        {
            x: e,
            y: -r + n
        },
        {
            x: e,
            y: -r
        },
        {
            x: 0,
            y: -r / 2
        }
    ] : s.has("up") ? [
        {
            x: e,
            y: -n
        },
        {
            x: e,
            y: -r + n
        },
        {
            x: 0,
            y: -r + n
        },
        {
            x: i / 2,
            y: -r
        },
        {
            x: i,
            y: -r + n
        },
        {
            x: i - e,
            y: -r + n
        },
        {
            x: i - e,
            y: -n
        }
    ] : s.has("down") ? [
        {
            x: i / 2,
            y: 0
        },
        {
            x: 0,
            y: -n
        },
        {
            x: e,
            y: -n
        },
        {
            x: e,
            y: -r + n
        },
        {
            x: i - e,
            y: -r + n
        },
        {
            x: i - e,
            y: -n
        },
        {
            x: i,
            y: -n
        }
    ] : [
        {
            x: 0,
            y: 0
        }
    ];
}, "getArrowPoints");
function yt(l, t) {
    return l.intersect(t);
}
(0, _chunkGTKDMUJJMjs.a)(yt, "intersectNode");
var Q = yt;
function ft(l, t, c, s) {
    var a = l.x, r = l.y, e = a - s.x, i = r - s.y, n = Math.sqrt(t * t * i * i + c * c * e * e), y = Math.abs(t * c * e / n);
    s.x < a && (y = -y);
    var h = Math.abs(t * c * i / n);
    return s.y < r && (h = -h), {
        x: a + y,
        y: r + h
    };
}
(0, _chunkGTKDMUJJMjs.a)(ft, "intersectEllipse");
var F = ft;
function xt(l, t, c) {
    return F(l, t, t, c);
}
(0, _chunkGTKDMUJJMjs.a)(xt, "intersectCircle");
var Z = xt;
function gt(l, t, c, s) {
    var a, r, e, i, n, y, h, f, g, x, p, v, d, k, S;
    if (a = t.y - l.y, e = l.x - t.x, n = t.x * l.y - l.x * t.y, g = a * c.x + e * c.y + n, x = a * s.x + e * s.y + n, !(g !== 0 && x !== 0 && q(g, x)) && (r = s.y - c.y, i = c.x - s.x, y = s.x * c.y - c.x * s.y, h = r * l.x + i * l.y + y, f = r * t.x + i * t.y + y, !(h !== 0 && f !== 0 && q(h, f)) && (p = a * i - r * e, p !== 0))) return v = Math.abs(p / 2), d = e * y - i * n, k = d < 0 ? (d - v) / p : (d + v) / p, d = r * n - a * y, S = d < 0 ? (d - v) / p : (d + v) / p, {
        x: k,
        y: S
    };
}
(0, _chunkGTKDMUJJMjs.a)(gt, "intersectLine");
function q(l, t) {
    return l * t > 0;
}
(0, _chunkGTKDMUJJMjs.a)(q, "sameSign");
var tt = gt;
var rt = pt;
function pt(l, t, c) {
    var s = l.x, a = l.y, r = [], e = Number.POSITIVE_INFINITY, i = Number.POSITIVE_INFINITY;
    typeof t.forEach == "function" ? t.forEach(function(p) {
        e = Math.min(e, p.x), i = Math.min(i, p.y);
    }) : (e = Math.min(e, t.x), i = Math.min(i, t.y));
    for(var n = s - l.width / 2 - e, y = a - l.height / 2 - i, h = 0; h < t.length; h++){
        var f = t[h], g = t[h < t.length - 1 ? h + 1 : 0], x = tt(l, c, {
            x: n + f.x,
            y: y + f.y
        }, {
            x: n + g.x,
            y: y + g.y
        });
        x && r.push(x);
    }
    return r.length ? (r.length > 1 && r.sort(function(p, v) {
        var d = p.x - c.x, k = p.y - c.y, S = Math.sqrt(d * d + k * k), j = v.x - c.x, D = v.y - c.y, P = Math.sqrt(j * j + D * D);
        return S < P ? -1 : S === P ? 0 : 1;
    }), r[0]) : l;
}
(0, _chunkGTKDMUJJMjs.a)(pt, "intersectPolygon");
var ut = (0, _chunkGTKDMUJJMjs.a)((l, t)=>{
    var c = l.x, s = l.y, a = t.x - c, r = t.y - s, e = l.width / 2, i = l.height / 2, n, y;
    return Math.abs(r) * e > Math.abs(a) * i ? (r < 0 && (i = -i), n = r === 0 ? 0 : i * a / r, y = i) : (a < 0 && (e = -e), n = e, y = a === 0 ? 0 : e * r / a), {
        x: c + n,
        y: s + y
    };
}, "intersectRect"), et = ut;
var u = {
    node: Q,
    circle: Z,
    ellipse: F,
    polygon: rt,
    rect: et
};
var dt = (0, _chunkGTKDMUJJMjs.a)(async (l, t)=>{
    t.useHtmlLabels || (0, _chunkNQURTBEVMjs.X)().flowchart.htmlLabels || (t.centerLabel = !0);
    let { shapeSvg: s, bbox: a, halfPadding: r } = await m(l, t, "node " + t.classes, !0);
    (0, _chunkNQURTBEVMjs.b).info("Classes = ", t.classes);
    let e = s.insert("rect", ":first-child");
    return e.attr("rx", t.rx).attr("ry", t.ry).attr("x", -a.width / 2 - r).attr("y", -a.height / 2 - r).attr("width", a.width + t.padding).attr("height", a.height + t.padding), w(t, e), t.intersect = function(i) {
        return u.rect(t, i);
    }, s;
}, "note"), st = dt;
var at = (0, _chunkGTKDMUJJMjs.a)((l)=>l ? " " + l : "", "formatClass"), I = (0, _chunkGTKDMUJJMjs.a)((l, t)=>`${t || "node default"}${at(l.classes)} ${at(l.class)}`, "getClassesFromNode"), it = (0, _chunkGTKDMUJJMjs.a)(async (l, t)=>{
    let { shapeSvg: c, bbox: s } = await m(l, t, I(t, void 0), !0), a = s.width + t.padding, r = s.height + t.padding, e = a + r, i = [
        {
            x: e / 2,
            y: 0
        },
        {
            x: e,
            y: -e / 2
        },
        {
            x: e / 2,
            y: -e
        },
        {
            x: 0,
            y: -e / 2
        }
    ];
    (0, _chunkNQURTBEVMjs.b).info("Question main (Circle)");
    let n = M(c, e, e, i);
    return n.attr("style", t.style), w(t, n), t.intersect = function(y) {
        return (0, _chunkNQURTBEVMjs.b).warn("Intersect called"), u.polygon(t, i, y);
    }, c;
}, "question"), wt = (0, _chunkGTKDMUJJMjs.a)((l, t)=>{
    let c = l.insert("g").attr("class", "node default").attr("id", t.domId || t.id), s = 28, a = [
        {
            x: 0,
            y: s / 2
        },
        {
            x: s / 2,
            y: 0
        },
        {
            x: 0,
            y: -s / 2
        },
        {
            x: -s / 2,
            y: 0
        }
    ];
    return c.insert("polygon", ":first-child").attr("points", a.map(function(e) {
        return e.x + "," + e.y;
    }).join(" ")).attr("class", "state-start").attr("r", 7).attr("width", 28).attr("height", 28), t.width = 28, t.height = 28, t.intersect = function(e) {
        return u.circle(t, 14, e);
    }, c;
}, "choice"), bt = (0, _chunkGTKDMUJJMjs.a)(async (l, t)=>{
    let { shapeSvg: c, bbox: s } = await m(l, t, I(t, void 0), !0), a = 4, r = s.height + t.padding, e = r / a, i = s.width + 2 * e + t.padding, n = [
        {
            x: e,
            y: 0
        },
        {
            x: i - e,
            y: 0
        },
        {
            x: i,
            y: -r / 2
        },
        {
            x: i - e,
            y: -r
        },
        {
            x: e,
            y: -r
        },
        {
            x: 0,
            y: -r / 2
        }
    ], y = M(c, i, r, n);
    return y.attr("style", t.style), w(t, y), t.intersect = function(h) {
        return u.polygon(t, n, h);
    }, c;
}, "hexagon"), mt = (0, _chunkGTKDMUJJMjs.a)(async (l, t)=>{
    let { shapeSvg: c, bbox: s } = await m(l, t, void 0, !0), a = 2, r = s.height + 2 * t.padding, e = r / a, i = s.width + 2 * e + t.padding, n = J(t.directions, s, t), y = M(c, i, r, n);
    return y.attr("style", t.style), w(t, y), t.intersect = function(h) {
        return u.polygon(t, n, h);
    }, c;
}, "block_arrow"), vt = (0, _chunkGTKDMUJJMjs.a)(async (l, t)=>{
    let { shapeSvg: c, bbox: s } = await m(l, t, I(t, void 0), !0), a = s.width + t.padding, r = s.height + t.padding, e = [
        {
            x: -r / 2,
            y: 0
        },
        {
            x: a,
            y: 0
        },
        {
            x: a,
            y: -r
        },
        {
            x: -r / 2,
            y: -r
        },
        {
            x: 0,
            y: -r / 2
        }
    ];
    return M(c, a, r, e).attr("style", t.style), t.width = a + r, t.height = r, t.intersect = function(n) {
        return u.polygon(t, e, n);
    }, c;
}, "rect_left_inv_arrow"), St = (0, _chunkGTKDMUJJMjs.a)(async (l, t)=>{
    let { shapeSvg: c, bbox: s } = await m(l, t, I(t), !0), a = s.width + t.padding, r = s.height + t.padding, e = [
        {
            x: -2 * r / 6,
            y: 0
        },
        {
            x: a - r / 6,
            y: 0
        },
        {
            x: a + 2 * r / 6,
            y: -r
        },
        {
            x: r / 6,
            y: -r
        }
    ], i = M(c, a, r, e);
    return i.attr("style", t.style), w(t, i), t.intersect = function(n) {
        return u.polygon(t, e, n);
    }, c;
}, "lean_right"), Bt = (0, _chunkGTKDMUJJMjs.a)(async (l, t)=>{
    let { shapeSvg: c, bbox: s } = await m(l, t, I(t, void 0), !0), a = s.width + t.padding, r = s.height + t.padding, e = [
        {
            x: 2 * r / 6,
            y: 0
        },
        {
            x: a + r / 6,
            y: 0
        },
        {
            x: a - 2 * r / 6,
            y: -r
        },
        {
            x: -r / 6,
            y: -r
        }
    ], i = M(c, a, r, e);
    return i.attr("style", t.style), w(t, i), t.intersect = function(n) {
        return u.polygon(t, e, n);
    }, c;
}, "lean_left"), Lt = (0, _chunkGTKDMUJJMjs.a)(async (l, t)=>{
    let { shapeSvg: c, bbox: s } = await m(l, t, I(t, void 0), !0), a = s.width + t.padding, r = s.height + t.padding, e = [
        {
            x: -2 * r / 6,
            y: 0
        },
        {
            x: a + 2 * r / 6,
            y: 0
        },
        {
            x: a - r / 6,
            y: -r
        },
        {
            x: r / 6,
            y: -r
        }
    ], i = M(c, a, r, e);
    return i.attr("style", t.style), w(t, i), t.intersect = function(n) {
        return u.polygon(t, e, n);
    }, c;
}, "trapezoid"), Ct = (0, _chunkGTKDMUJJMjs.a)(async (l, t)=>{
    let { shapeSvg: c, bbox: s } = await m(l, t, I(t, void 0), !0), a = s.width + t.padding, r = s.height + t.padding, e = [
        {
            x: r / 6,
            y: 0
        },
        {
            x: a - r / 6,
            y: 0
        },
        {
            x: a + 2 * r / 6,
            y: -r
        },
        {
            x: -2 * r / 6,
            y: -r
        }
    ], i = M(c, a, r, e);
    return i.attr("style", t.style), w(t, i), t.intersect = function(n) {
        return u.polygon(t, e, n);
    }, c;
}, "inv_trapezoid"), Tt = (0, _chunkGTKDMUJJMjs.a)(async (l, t)=>{
    let { shapeSvg: c, bbox: s } = await m(l, t, I(t, void 0), !0), a = s.width + t.padding, r = s.height + t.padding, e = [
        {
            x: 0,
            y: 0
        },
        {
            x: a + r / 2,
            y: 0
        },
        {
            x: a,
            y: -r / 2
        },
        {
            x: a + r / 2,
            y: -r
        },
        {
            x: 0,
            y: -r
        }
    ], i = M(c, a, r, e);
    return i.attr("style", t.style), w(t, i), t.intersect = function(n) {
        return u.polygon(t, e, n);
    }, c;
}, "rect_right_inv_arrow"), kt = (0, _chunkGTKDMUJJMjs.a)(async (l, t)=>{
    let { shapeSvg: c, bbox: s } = await m(l, t, I(t, void 0), !0), a = s.width + t.padding, r = a / 2, e = r / (2.5 + a / 50), i = s.height + e + t.padding, n = "M 0," + e + " a " + r + "," + e + " 0,0,0 " + a + " 0 a " + r + "," + e + " 0,0,0 " + -a + " 0 l 0," + i + " a " + r + "," + e + " 0,0,0 " + a + " 0 l 0," + -i, y = c.attr("label-offset-y", e).insert("path", ":first-child").attr("style", t.style).attr("d", n).attr("transform", "translate(" + -a / 2 + "," + -(i / 2 + e) + ")");
    return w(t, y), t.intersect = function(h) {
        let f = u.rect(t, h), g = f.x - t.x;
        if (r != 0 && (Math.abs(g) < t.width / 2 || Math.abs(g) == t.width / 2 && Math.abs(f.y - t.y) > t.height / 2 - e)) {
            let x = e * e * (1 - g * g / (r * r));
            x != 0 && (x = Math.sqrt(x)), x = e - x, h.y - t.y > 0 && (x = -x), f.y += x;
        }
        return f;
    }, c;
}, "cylinder"), Dt = (0, _chunkGTKDMUJJMjs.a)(async (l, t)=>{
    let { shapeSvg: c, bbox: s, halfPadding: a } = await m(l, t, "node " + t.classes + " " + t.class, !0), r = c.insert("rect", ":first-child"), e = t.positioned ? t.width : s.width + t.padding, i = t.positioned ? t.height : s.height + t.padding, n = t.positioned ? -e / 2 : -s.width / 2 - a, y = t.positioned ? -i / 2 : -s.height / 2 - a;
    if (r.attr("class", "basic label-container").attr("style", t.style).attr("rx", t.rx).attr("ry", t.ry).attr("x", n).attr("y", y).attr("width", e).attr("height", i), t.props) {
        let h = new Set(Object.keys(t.props));
        t.props.borders && (U(r, t.props.borders, e, i), h.delete("borders")), h.forEach((f)=>{
            (0, _chunkNQURTBEVMjs.b).warn(`Unknown node property ${f}`);
        });
    }
    return w(t, r), t.intersect = function(h) {
        return u.rect(t, h);
    }, c;
}, "rect"), Et = (0, _chunkGTKDMUJJMjs.a)(async (l, t)=>{
    let { shapeSvg: c, bbox: s, halfPadding: a } = await m(l, t, "node " + t.classes, !0), r = c.insert("rect", ":first-child"), e = t.positioned ? t.width : s.width + t.padding, i = t.positioned ? t.height : s.height + t.padding, n = t.positioned ? -e / 2 : -s.width / 2 - a, y = t.positioned ? -i / 2 : -s.height / 2 - a;
    if (r.attr("class", "basic cluster composite label-container").attr("style", t.style).attr("rx", t.rx).attr("ry", t.ry).attr("x", n).attr("y", y).attr("width", e).attr("height", i), t.props) {
        let h = new Set(Object.keys(t.props));
        t.props.borders && (U(r, t.props.borders, e, i), h.delete("borders")), h.forEach((f)=>{
            (0, _chunkNQURTBEVMjs.b).warn(`Unknown node property ${f}`);
        });
    }
    return w(t, r), t.intersect = function(h) {
        return u.rect(t, h);
    }, c;
}, "composite"), It = (0, _chunkGTKDMUJJMjs.a)(async (l, t)=>{
    let { shapeSvg: c } = await m(l, t, "label", !0);
    (0, _chunkNQURTBEVMjs.b).trace("Classes = ", t.class);
    let s = c.insert("rect", ":first-child"), a = 0, r = 0;
    if (s.attr("width", a).attr("height", r), c.attr("class", "label edgeLabel"), t.props) {
        let e = new Set(Object.keys(t.props));
        t.props.borders && (U(s, t.props.borders, a, r), e.delete("borders")), e.forEach((i)=>{
            (0, _chunkNQURTBEVMjs.b).warn(`Unknown node property ${i}`);
        });
    }
    return w(t, s), t.intersect = function(e) {
        return u.rect(t, e);
    }, c;
}, "labelRect");
function U(l, t, c, s) {
    let a = [], r = (0, _chunkGTKDMUJJMjs.a)((i)=>{
        a.push(i, 0);
    }, "addBorder"), e = (0, _chunkGTKDMUJJMjs.a)((i)=>{
        a.push(0, i);
    }, "skipBorder");
    t.includes("t") ? ((0, _chunkNQURTBEVMjs.b).debug("add top border"), r(c)) : e(c), t.includes("r") ? ((0, _chunkNQURTBEVMjs.b).debug("add right border"), r(s)) : e(s), t.includes("b") ? ((0, _chunkNQURTBEVMjs.b).debug("add bottom border"), r(c)) : e(c), t.includes("l") ? ((0, _chunkNQURTBEVMjs.b).debug("add left border"), r(s)) : e(s), l.attr("stroke-dasharray", a.join(" "));
}
(0, _chunkGTKDMUJJMjs.a)(U, "applyNodePropertyBorders");
var Nt = (0, _chunkGTKDMUJJMjs.a)((l, t)=>{
    let c;
    t.classes ? c = "node " + t.classes : c = "node default";
    let s = l.insert("g").attr("class", c).attr("id", t.domId || t.id), a = s.insert("rect", ":first-child"), r = s.insert("line"), e = s.insert("g").attr("class", "label"), i = t.labelText.flat ? t.labelText.flat() : t.labelText, n = "";
    typeof i == "object" ? n = i[0] : n = i, (0, _chunkNQURTBEVMjs.b).info("Label text abc79", n, i, typeof i == "object");
    let y = e.node().appendChild(_(n, t.labelStyle, !0, !0)), h = {
        width: 0,
        height: 0
    };
    if ((0, _chunkNQURTBEVMjs.G)((0, _chunkNQURTBEVMjs.X)().flowchart.htmlLabels)) {
        let v = y.children[0], d = (0, _chunkNQURTBEVMjs.fa)(y);
        h = v.getBoundingClientRect(), d.attr("width", h.width), d.attr("height", h.height);
    }
    (0, _chunkNQURTBEVMjs.b).info("Text 2", i);
    let f = i.slice(1, i.length), g = y.getBBox(), x = e.node().appendChild(_(f.join ? f.join("<br/>") : f, t.labelStyle, !0, !0));
    if ((0, _chunkNQURTBEVMjs.G)((0, _chunkNQURTBEVMjs.X)().flowchart.htmlLabels)) {
        let v = x.children[0], d = (0, _chunkNQURTBEVMjs.fa)(x);
        h = v.getBoundingClientRect(), d.attr("width", h.width), d.attr("height", h.height);
    }
    let p = t.padding / 2;
    return (0, _chunkNQURTBEVMjs.fa)(x).attr("transform", "translate( " + (h.width > g.width ? 0 : (g.width - h.width) / 2) + ", " + (g.height + p + 5) + ")"), (0, _chunkNQURTBEVMjs.fa)(y).attr("transform", "translate( " + (h.width < g.width ? 0 : -(g.width - h.width) / 2) + ", 0)"), h = e.node().getBBox(), e.attr("transform", "translate(" + -h.width / 2 + ", " + (-h.height / 2 - p + 3) + ")"), a.attr("class", "outer title-state").attr("x", -h.width / 2 - p).attr("y", -h.height / 2 - p).attr("width", h.width + t.padding).attr("height", h.height + t.padding), r.attr("class", "divider").attr("x1", -h.width / 2 - p).attr("x2", h.width / 2 + p).attr("y1", -h.height / 2 - p + g.height + p).attr("y2", -h.height / 2 - p + g.height + p), w(t, a), t.intersect = function(v) {
        return u.rect(t, v);
    }, s;
}, "rectWithTitle"), Mt = (0, _chunkGTKDMUJJMjs.a)(async (l, t)=>{
    let { shapeSvg: c, bbox: s } = await m(l, t, I(t, void 0), !0), a = s.height + t.padding, r = s.width + a / 4 + t.padding, e = c.insert("rect", ":first-child").attr("style", t.style).attr("rx", a / 2).attr("ry", a / 2).attr("x", -r / 2).attr("y", -a / 2).attr("width", r).attr("height", a);
    return w(t, e), t.intersect = function(i) {
        return u.rect(t, i);
    }, c;
}, "stadium"), jt = (0, _chunkGTKDMUJJMjs.a)(async (l, t)=>{
    let { shapeSvg: c, bbox: s, halfPadding: a } = await m(l, t, I(t, void 0), !0), r = c.insert("circle", ":first-child");
    return r.attr("style", t.style).attr("rx", t.rx).attr("ry", t.ry).attr("r", s.width / 2 + a).attr("width", s.width + t.padding).attr("height", s.height + t.padding), (0, _chunkNQURTBEVMjs.b).info("Circle main"), w(t, r), t.intersect = function(e) {
        return (0, _chunkNQURTBEVMjs.b).info("Circle intersect", t, s.width / 2 + a, e), u.circle(t, s.width / 2 + a, e);
    }, c;
}, "circle"), At = (0, _chunkGTKDMUJJMjs.a)(async (l, t)=>{
    let { shapeSvg: c, bbox: s, halfPadding: a } = await m(l, t, I(t, void 0), !0), r = 5, e = c.insert("g", ":first-child"), i = e.insert("circle"), n = e.insert("circle");
    return e.attr("class", t.class), i.attr("style", t.style).attr("rx", t.rx).attr("ry", t.ry).attr("r", s.width / 2 + a + r).attr("width", s.width + t.padding + r * 2).attr("height", s.height + t.padding + r * 2), n.attr("style", t.style).attr("rx", t.rx).attr("ry", t.ry).attr("r", s.width / 2 + a).attr("width", s.width + t.padding).attr("height", s.height + t.padding), (0, _chunkNQURTBEVMjs.b).info("DoubleCircle main"), w(t, i), t.intersect = function(y) {
        return (0, _chunkNQURTBEVMjs.b).info("DoubleCircle intersect", t, s.width / 2 + a + r, y), u.circle(t, s.width / 2 + a + r, y);
    }, c;
}, "doublecircle"), _t = (0, _chunkGTKDMUJJMjs.a)(async (l, t)=>{
    let { shapeSvg: c, bbox: s } = await m(l, t, I(t, void 0), !0), a = s.width + t.padding, r = s.height + t.padding, e = [
        {
            x: 0,
            y: 0
        },
        {
            x: a,
            y: 0
        },
        {
            x: a,
            y: -r
        },
        {
            x: 0,
            y: -r
        },
        {
            x: 0,
            y: 0
        },
        {
            x: -8,
            y: 0
        },
        {
            x: a + 8,
            y: 0
        },
        {
            x: a + 8,
            y: -r
        },
        {
            x: -8,
            y: -r
        },
        {
            x: -8,
            y: 0
        }
    ], i = M(c, a, r, e);
    return i.attr("style", t.style), w(t, i), t.intersect = function(n) {
        return u.polygon(t, e, n);
    }, c;
}, "subroutine"), Pt = (0, _chunkGTKDMUJJMjs.a)((l, t)=>{
    let c = l.insert("g").attr("class", "node default").attr("id", t.domId || t.id), s = c.insert("circle", ":first-child");
    return s.attr("class", "state-start").attr("r", 7).attr("width", 14).attr("height", 14), w(t, s), t.intersect = function(a) {
        return u.circle(t, 7, a);
    }, c;
}, "start"), ct = (0, _chunkGTKDMUJJMjs.a)((l, t, c)=>{
    let s = l.insert("g").attr("class", "node default").attr("id", t.domId || t.id), a = 70, r = 10;
    c === "LR" && (a = 10, r = 70);
    let e = s.append("rect").attr("x", -1 * a / 2).attr("y", -1 * r / 2).attr("width", a).attr("height", r).attr("class", "fork-join");
    return w(t, e), t.height = t.height + t.padding / 2, t.width = t.width + t.padding / 2, t.intersect = function(i) {
        return u.rect(t, i);
    }, s;
}, "forkJoin"), Ht = (0, _chunkGTKDMUJJMjs.a)((l, t)=>{
    let c = l.insert("g").attr("class", "node default").attr("id", t.domId || t.id), s = c.insert("circle", ":first-child"), a = c.insert("circle", ":first-child");
    return a.attr("class", "state-start").attr("r", 7).attr("width", 14).attr("height", 14), s.attr("class", "state-end").attr("r", 5).attr("width", 10).attr("height", 10), w(t, a), t.intersect = function(r) {
        return u.circle(t, 7, r);
    }, c;
}, "end"), Rt = (0, _chunkGTKDMUJJMjs.a)((l, t)=>{
    let c = t.padding / 2, s = 4, a = 8, r;
    t.classes ? r = "node " + t.classes : r = "node default";
    let e = l.insert("g").attr("class", r).attr("id", t.domId || t.id), i = e.insert("rect", ":first-child"), n = e.insert("line"), y = e.insert("line"), h = 0, f = s, g = e.insert("g").attr("class", "label"), x = 0, p = t.classData.annotations?.[0], v = t.classData.annotations[0] ? "\xAB" + t.classData.annotations[0] + "\xBB" : "", d = g.node().appendChild(_(v, t.labelStyle, !0, !0)), k = d.getBBox();
    if ((0, _chunkNQURTBEVMjs.G)((0, _chunkNQURTBEVMjs.X)().flowchart.htmlLabels)) {
        let L = d.children[0], C = (0, _chunkNQURTBEVMjs.fa)(d);
        k = L.getBoundingClientRect(), C.attr("width", k.width), C.attr("height", k.height);
    }
    t.classData.annotations[0] && (f += k.height + s, h += k.width);
    let S = t.classData.label;
    t.classData.type !== void 0 && t.classData.type !== "" && ((0, _chunkNQURTBEVMjs.X)().flowchart.htmlLabels ? S += "&lt;" + t.classData.type + "&gt;" : S += "<" + t.classData.type + ">");
    let j = g.node().appendChild(_(S, t.labelStyle, !0, !0));
    (0, _chunkNQURTBEVMjs.fa)(j).attr("class", "classTitle");
    let D = j.getBBox();
    if ((0, _chunkNQURTBEVMjs.G)((0, _chunkNQURTBEVMjs.X)().flowchart.htmlLabels)) {
        let L = j.children[0], C = (0, _chunkNQURTBEVMjs.fa)(j);
        D = L.getBoundingClientRect(), C.attr("width", D.width), C.attr("height", D.height);
    }
    f += D.height + s, D.width > h && (h = D.width);
    let P = [];
    t.classData.members.forEach((L)=>{
        let C = L.getDisplayDetails(), H = C.displayText;
        (0, _chunkNQURTBEVMjs.X)().flowchart.htmlLabels && (H = H.replace(/</g, "&lt;").replace(/>/g, "&gt;"));
        let A = g.node().appendChild(_(H, C.cssStyle ? C.cssStyle : t.labelStyle, !0, !0)), E = A.getBBox();
        if ((0, _chunkNQURTBEVMjs.G)((0, _chunkNQURTBEVMjs.X)().flowchart.htmlLabels)) {
            let X = A.children[0], z = (0, _chunkNQURTBEVMjs.fa)(A);
            E = X.getBoundingClientRect(), z.attr("width", E.width), z.attr("height", E.height);
        }
        E.width > h && (h = E.width), f += E.height + s, P.push(A);
    }), f += a;
    let W = [];
    if (t.classData.methods.forEach((L)=>{
        let C = L.getDisplayDetails(), H = C.displayText;
        (0, _chunkNQURTBEVMjs.X)().flowchart.htmlLabels && (H = H.replace(/</g, "&lt;").replace(/>/g, "&gt;"));
        let A = g.node().appendChild(_(H, C.cssStyle ? C.cssStyle : t.labelStyle, !0, !0)), E = A.getBBox();
        if ((0, _chunkNQURTBEVMjs.G)((0, _chunkNQURTBEVMjs.X)().flowchart.htmlLabels)) {
            let X = A.children[0], z = (0, _chunkNQURTBEVMjs.fa)(A);
            E = X.getBoundingClientRect(), z.attr("width", E.width), z.attr("height", E.height);
        }
        E.width > h && (h = E.width), f += E.height + s, W.push(A);
    }), f += a, p) {
        let L = (h - k.width) / 2;
        (0, _chunkNQURTBEVMjs.fa)(d).attr("transform", "translate( " + (-1 * h / 2 + L) + ", " + -1 * f / 2 + ")"), x = k.height + s;
    }
    let $ = (h - D.width) / 2;
    return (0, _chunkNQURTBEVMjs.fa)(j).attr("transform", "translate( " + (-1 * h / 2 + $) + ", " + (-1 * f / 2 + x) + ")"), x += D.height + s, n.attr("class", "divider").attr("x1", -h / 2 - c).attr("x2", h / 2 + c).attr("y1", -f / 2 - c + a + x).attr("y2", -f / 2 - c + a + x), x += a, P.forEach((L)=>{
        (0, _chunkNQURTBEVMjs.fa)(L).attr("transform", "translate( " + -h / 2 + ", " + (-1 * f / 2 + x + a / 2) + ")");
        let C = L?.getBBox();
        x += (C?.height ?? 0) + s;
    }), x += a, y.attr("class", "divider").attr("x1", -h / 2 - c).attr("x2", h / 2 + c).attr("y1", -f / 2 - c + a + x).attr("y2", -f / 2 - c + a + x), x += a, W.forEach((L)=>{
        (0, _chunkNQURTBEVMjs.fa)(L).attr("transform", "translate( " + -h / 2 + ", " + (-1 * f / 2 + x) + ")");
        let C = L?.getBBox();
        x += (C?.height ?? 0) + s;
    }), i.attr("style", t.style).attr("class", "outer title-state").attr("x", -h / 2 - c).attr("y", -(f / 2) - c).attr("width", h + t.padding).attr("height", f + t.padding), w(t, i), t.intersect = function(L) {
        return u.rect(t, L);
    }, e;
}, "class_box"), lt = {
    rhombus: it,
    composite: Et,
    question: it,
    rect: Dt,
    labelRect: It,
    rectWithTitle: Nt,
    choice: wt,
    circle: jt,
    doublecircle: At,
    stadium: Mt,
    hexagon: bt,
    block_arrow: mt,
    rect_left_inv_arrow: vt,
    lean_right: St,
    lean_left: Bt,
    trapezoid: Lt,
    inv_trapezoid: Ct,
    rect_right_inv_arrow: Tt,
    cylinder: kt,
    start: Pt,
    end: Ht,
    note: st,
    subroutine: _t,
    fork: ct,
    join: ct,
    class_box: Rt
}, R = {}, Rr = (0, _chunkGTKDMUJJMjs.a)(async (l, t, c)=>{
    let s, a;
    if (t.link) {
        let r;
        (0, _chunkNQURTBEVMjs.X)().securityLevel === "sandbox" ? r = "_top" : t.linkTarget && (r = t.linkTarget || "_blank"), s = l.insert("svg:a").attr("xlink:href", t.link).attr("target", r), a = await lt[t.shape](s, t, c);
    } else a = await lt[t.shape](l, t, c), s = a;
    return t.tooltip && a.attr("title", t.tooltip), t.class && a.attr("class", "node default " + t.class), R[t.id] = s, t.haveCallback && R[t.id].attr("class", R[t.id].attr("class") + " clickable"), s;
}, "insertNode"), zr = (0, _chunkGTKDMUJJMjs.a)((l, t)=>{
    R[t.id] = l;
}, "setNodeElem"), Or = (0, _chunkGTKDMUJJMjs.a)(()=>{
    R = {};
}, "clear"), $r = (0, _chunkGTKDMUJJMjs.a)((l)=>{
    let t = R[l.id];
    (0, _chunkNQURTBEVMjs.b).trace("Transforming node", l.diff, l, "translate(" + (l.x - l.width / 2 - 5) + ", " + l.width / 2 + ")");
    let c = 8, s = l.diff || 0;
    return l.clusterNode ? t.attr("transform", "translate(" + (l.x + s - l.width / 2) + ", " + (l.y - l.height / 2 - c) + ")") : t.attr("transform", "translate(" + l.x + ", " + l.y + ")"), s;
}, "positionNode");

},{"./chunk-KMOJB3TB.mjs":"aJH4M","./chunk-AC3VT7B7.mjs":"1eiUz","./chunk-NQURTBEV.mjs":"iASFe","./chunk-GTKDMUJJ.mjs":"fruhx","@parcel/transformer-js/src/esmodule-helpers.js":"1cdyk"}]},["87vNq"], null, "parcelRequire6955", {})

//# sourceMappingURL=flowDiagram-JTTVBJUY.1cfa72a8.js.map
