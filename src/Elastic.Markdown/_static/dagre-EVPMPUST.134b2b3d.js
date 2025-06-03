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
})({"31wPQ":[function(require,module,exports,__globalThis) {
var global = arguments[3];
var HMR_HOST = null;
var HMR_PORT = 1234;
var HMR_SERVER_PORT = 1234;
var HMR_SECURE = false;
var HMR_ENV_HASH = "febfef3c5b467b16";
var HMR_USE_SSE = false;
module.bundle.HMR_BUNDLE_ID = "306fc682134b2b3d";
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

},{}],"klimL":[function(require,module,exports,__globalThis) {
var parcelHelpers = require("@parcel/transformer-js/src/esmodule-helpers.js");
parcelHelpers.defineInteropFlag(exports);
parcelHelpers.export(exports, "a", ()=>sn);
var _chunk6XGRHI2AMjs = require("./chunk-6XGRHI2A.mjs");
var _chunkBKDDFIKNMjs = require("./chunk-BKDDFIKN.mjs");
var _chunk6BY5RJGCMjs = require("./chunk-6BY5RJGC.mjs");
var _chunkGTKDMUJJMjs = require("./chunk-GTKDMUJJ.mjs");
function N(r, e, n, o) {
    var t;
    do t = (0, _chunkBKDDFIKNMjs.T)(o);
    while (r.hasNode(t));
    return n.dummy = e, r.setNode(t, n), t;
}
(0, _chunkGTKDMUJJMjs.a)(N, "addDummyNode");
function xr(r) {
    var e = new (0, _chunk6XGRHI2AMjs.a)().setGraph(r.graph());
    return (0, _chunkBKDDFIKNMjs.n)(r.nodes(), function(n) {
        e.setNode(n, r.node(n));
    }), (0, _chunkBKDDFIKNMjs.n)(r.edges(), function(n) {
        var o = e.edge(n.v, n.w) || {
            weight: 0,
            minlen: 1
        }, t = r.edge(n);
        e.setEdge(n.v, n.w, {
            weight: o.weight + t.weight,
            minlen: Math.max(o.minlen, t.minlen)
        });
    }), e;
}
(0, _chunkGTKDMUJJMjs.a)(xr, "simplify");
function X(r) {
    var e = new (0, _chunk6XGRHI2AMjs.a)({
        multigraph: r.isMultigraph()
    }).setGraph(r.graph());
    return (0, _chunkBKDDFIKNMjs.n)(r.nodes(), function(n) {
        r.children(n).length || e.setNode(n, r.node(n));
    }), (0, _chunkBKDDFIKNMjs.n)(r.edges(), function(n) {
        e.setEdge(n, r.edge(n));
    }), e;
}
(0, _chunkGTKDMUJJMjs.a)(X, "asNonCompoundGraph");
function $(r, e) {
    var n = r.x, o = r.y, t = e.x - n, i = e.y - o, a = r.width / 2, s = r.height / 2;
    if (!t && !i) throw new Error("Not possible to find intersection inside of the rectangle");
    var d, c;
    return Math.abs(i) * a > Math.abs(t) * s ? (i < 0 && (s = -s), d = s * t / i, c = s) : (t < 0 && (a = -a), d = a, c = a * i / t), {
        x: n + d,
        y: o + c
    };
}
(0, _chunkGTKDMUJJMjs.a)($, "intersectRect");
function S(r) {
    var e = (0, _chunkBKDDFIKNMjs.s)((0, _chunkBKDDFIKNMjs.K)(er(r) + 1), function() {
        return [];
    });
    return (0, _chunkBKDDFIKNMjs.n)(r.nodes(), function(n) {
        var o = r.node(n), t = o.rank;
        (0, _chunkBKDDFIKNMjs.D)(t) || (e[t][o.order] = n);
    }), e;
}
(0, _chunkGTKDMUJJMjs.a)(S, "buildLayerMatrix");
function kr(r) {
    var e = (0, _chunkBKDDFIKNMjs.G)((0, _chunkBKDDFIKNMjs.s)(r.nodes(), function(n) {
        return r.node(n).rank;
    }));
    (0, _chunkBKDDFIKNMjs.n)(r.nodes(), function(n) {
        var o = r.node(n);
        (0, _chunkBKDDFIKNMjs.x)(o, "rank") && (o.rank -= e);
    });
}
(0, _chunkGTKDMUJJMjs.a)(kr, "normalizeRanks");
function gr(r) {
    var e = (0, _chunkBKDDFIKNMjs.G)((0, _chunkBKDDFIKNMjs.s)(r.nodes(), function(i) {
        return r.node(i).rank;
    })), n = [];
    (0, _chunkBKDDFIKNMjs.n)(r.nodes(), function(i) {
        var a = r.node(i).rank - e;
        n[a] || (n[a] = []), n[a].push(i);
    });
    var o = 0, t = r.graph().nodeRankFactor;
    (0, _chunkBKDDFIKNMjs.n)(n, function(i, a) {
        (0, _chunkBKDDFIKNMjs.D)(i) && a % t !== 0 ? --o : o && (0, _chunkBKDDFIKNMjs.n)(i, function(s) {
            r.node(s).rank += o;
        });
    });
}
(0, _chunkGTKDMUJJMjs.a)(gr, "removeEmptyRanks");
function rr(r, e, n, o) {
    var t = {
        width: 0,
        height: 0
    };
    return arguments.length >= 4 && (t.rank = n, t.order = o), N(r, "border", t, e);
}
(0, _chunkGTKDMUJJMjs.a)(rr, "addBorderNode");
function er(r) {
    return (0, _chunkBKDDFIKNMjs.F)((0, _chunkBKDDFIKNMjs.s)(r.nodes(), function(e) {
        var n = r.node(e).rank;
        if (!(0, _chunkBKDDFIKNMjs.D)(n)) return n;
    }));
}
(0, _chunkGTKDMUJJMjs.a)(er, "maxRank");
function Nr(r, e) {
    var n = {
        lhs: [],
        rhs: []
    };
    return (0, _chunkBKDDFIKNMjs.n)(r, function(o) {
        e(o) ? n.lhs.push(o) : n.rhs.push(o);
    }), n;
}
(0, _chunkGTKDMUJJMjs.a)(Nr, "partition");
function Ir(r, e) {
    var n = (0, _chunkBKDDFIKNMjs.h)();
    try {
        return e();
    } finally{
        console.log(r + " time: " + ((0, _chunkBKDDFIKNMjs.h)() - n) + "ms");
    }
}
(0, _chunkGTKDMUJJMjs.a)(Ir, "time");
function Lr(r, e) {
    return e();
}
(0, _chunkGTKDMUJJMjs.a)(Lr, "notime");
function Tr(r) {
    function e(n) {
        var o = r.children(n), t = r.node(n);
        if (o.length && (0, _chunkBKDDFIKNMjs.n)(o, e), (0, _chunkBKDDFIKNMjs.x)(t, "minRank")) {
            t.borderLeft = [], t.borderRight = [];
            for(var i = t.minRank, a = t.maxRank + 1; i < a; ++i)Cr(r, "borderLeft", "_bl", n, t, i), Cr(r, "borderRight", "_br", n, t, i);
        }
    }
    (0, _chunkGTKDMUJJMjs.a)(e, "dfs"), (0, _chunkBKDDFIKNMjs.n)(r.children(), e);
}
(0, _chunkGTKDMUJJMjs.a)(Tr, "addBorderSegments");
function Cr(r, e, n, o, t, i) {
    var a = {
        width: 0,
        height: 0,
        rank: i,
        borderType: e
    }, s = t[e][i - 1], d = N(r, "border", a, n);
    t[e][i] = d, r.setParent(d, o), s && r.setEdge(s, d, {
        weight: 1
    });
}
(0, _chunkGTKDMUJJMjs.a)(Cr, "addBorderNode");
function Sr(r) {
    var e = r.graph().rankdir.toLowerCase();
    (e === "lr" || e === "rl") && Pr(r);
}
(0, _chunkGTKDMUJJMjs.a)(Sr, "adjust");
function Mr(r) {
    var e = r.graph().rankdir.toLowerCase();
    (e === "bt" || e === "rl") && pe(r), (e === "lr" || e === "rl") && (me(r), Pr(r));
}
(0, _chunkGTKDMUJJMjs.a)(Mr, "undo");
function Pr(r) {
    (0, _chunkBKDDFIKNMjs.n)(r.nodes(), function(e) {
        Rr(r.node(e));
    }), (0, _chunkBKDDFIKNMjs.n)(r.edges(), function(e) {
        Rr(r.edge(e));
    });
}
(0, _chunkGTKDMUJJMjs.a)(Pr, "swapWidthHeight");
function Rr(r) {
    var e = r.width;
    r.width = r.height, r.height = e;
}
(0, _chunkGTKDMUJJMjs.a)(Rr, "swapWidthHeightOne");
function pe(r) {
    (0, _chunkBKDDFIKNMjs.n)(r.nodes(), function(e) {
        nr(r.node(e));
    }), (0, _chunkBKDDFIKNMjs.n)(r.edges(), function(e) {
        var n = r.edge(e);
        (0, _chunkBKDDFIKNMjs.n)(n.points, nr), (0, _chunkBKDDFIKNMjs.x)(n, "y") && nr(n);
    });
}
(0, _chunkGTKDMUJJMjs.a)(pe, "reverseY");
function nr(r) {
    r.y = -r.y;
}
(0, _chunkGTKDMUJJMjs.a)(nr, "reverseYOne");
function me(r) {
    (0, _chunkBKDDFIKNMjs.n)(r.nodes(), function(e) {
        or(r.node(e));
    }), (0, _chunkBKDDFIKNMjs.n)(r.edges(), function(e) {
        var n = r.edge(e);
        (0, _chunkBKDDFIKNMjs.n)(n.points, or), (0, _chunkBKDDFIKNMjs.x)(n, "x") && or(n);
    });
}
(0, _chunkGTKDMUJJMjs.a)(me, "swapXY");
function or(r) {
    var e = r.x;
    r.x = r.y, r.y = e;
}
(0, _chunkGTKDMUJJMjs.a)(or, "swapXYOne");
var H = class {
    static{
        (0, _chunkGTKDMUJJMjs.a)(this, "List");
    }
    constructor(){
        var e = {};
        e._next = e._prev = e, this._sentinel = e;
    }
    dequeue() {
        var e = this._sentinel, n = e._prev;
        if (n !== e) return Fr(n), n;
    }
    enqueue(e) {
        var n = this._sentinel;
        e._prev && e._next && Fr(e), e._next = n._next, n._next._prev = e, n._next = e, e._prev = n;
    }
    toString() {
        for(var e = [], n = this._sentinel, o = n._prev; o !== n;)e.push(JSON.stringify(o, _e)), o = o._prev;
        return "[" + e.join(", ") + "]";
    }
};
function Fr(r) {
    r._prev._next = r._next, r._next._prev = r._prev, delete r._next, delete r._prev;
}
(0, _chunkGTKDMUJJMjs.a)(Fr, "unlink");
function _e(r, e) {
    if (r !== "_next" && r !== "_prev") return e;
}
(0, _chunkGTKDMUJJMjs.a)(_e, "filterOutLinks");
var we = (0, _chunk6BY5RJGCMjs.O)(1);
function Or(r, e) {
    if (r.nodeCount() <= 1) return [];
    var n = be(r, e || we), o = Ee(n.graph, n.buckets, n.zeroIdx);
    return (0, _chunkBKDDFIKNMjs.d)((0, _chunkBKDDFIKNMjs.s)(o, function(t) {
        return r.outEdges(t.v, t.w);
    }));
}
(0, _chunkGTKDMUJJMjs.a)(Or, "greedyFAS");
function Ee(r, e, n) {
    for(var o = [], t = e[e.length - 1], i = e[0], a; r.nodeCount();){
        for(; a = i.dequeue();)tr(r, e, n, a);
        for(; a = t.dequeue();)tr(r, e, n, a);
        if (r.nodeCount()) {
            for(var s = e.length - 2; s > 0; --s)if (a = e[s].dequeue(), a) {
                o = o.concat(tr(r, e, n, a, !0));
                break;
            }
        }
    }
    return o;
}
(0, _chunkGTKDMUJJMjs.a)(Ee, "doGreedyFAS");
function tr(r, e, n, o, t) {
    var i = t ? [] : void 0;
    return (0, _chunkBKDDFIKNMjs.n)(r.inEdges(o.v), function(a) {
        var s = r.edge(a), d = r.node(a.v);
        t && i.push({
            v: a.v,
            w: a.w
        }), d.out -= s, ir(e, n, d);
    }), (0, _chunkBKDDFIKNMjs.n)(r.outEdges(o.v), function(a) {
        var s = r.edge(a), d = a.w, c = r.node(d);
        c.in -= s, ir(e, n, c);
    }), r.removeNode(o.v), i;
}
(0, _chunkGTKDMUJJMjs.a)(tr, "removeNode");
function be(r, e) {
    var n = new (0, _chunk6XGRHI2AMjs.a), o = 0, t = 0;
    (0, _chunkBKDDFIKNMjs.n)(r.nodes(), function(s) {
        n.setNode(s, {
            v: s,
            in: 0,
            out: 0
        });
    }), (0, _chunkBKDDFIKNMjs.n)(r.edges(), function(s) {
        var d = n.edge(s.v, s.w) || 0, c = e(s), h = d + c;
        n.setEdge(s.v, s.w, h), t = Math.max(t, n.node(s.v).out += c), o = Math.max(o, n.node(s.w).in += c);
    });
    var i = (0, _chunkBKDDFIKNMjs.K)(t + o + 3).map(function() {
        return new H;
    }), a = o + 1;
    return (0, _chunkBKDDFIKNMjs.n)(n.nodes(), function(s) {
        ir(i, a, n.node(s));
    }), {
        graph: n,
        buckets: i,
        zeroIdx: a
    };
}
(0, _chunkGTKDMUJJMjs.a)(be, "buildState");
function ir(r, e, n) {
    n.out ? n.in ? r[n.out - n.in + e].enqueue(n) : r[r.length - 1].enqueue(n) : r[0].enqueue(n);
}
(0, _chunkGTKDMUJJMjs.a)(ir, "assignBucket");
function Gr(r) {
    var e = r.graph().acyclicer === "greedy" ? Or(r, n(r)) : ye(r);
    (0, _chunkBKDDFIKNMjs.n)(e, function(o) {
        var t = r.edge(o);
        r.removeEdge(o), t.forwardName = o.name, t.reversed = !0, r.setEdge(o.w, o.v, t, (0, _chunkBKDDFIKNMjs.T)("rev"));
    });
    function n(o) {
        return function(t) {
            return o.edge(t).weight;
        };
    }
    (0, _chunkGTKDMUJJMjs.a)(n, "weightFn");
}
(0, _chunkGTKDMUJJMjs.a)(Gr, "run");
function ye(r) {
    var e = [], n = {}, o = {};
    function t(i) {
        (0, _chunkBKDDFIKNMjs.x)(o, i) || (o[i] = !0, n[i] = !0, (0, _chunkBKDDFIKNMjs.n)(r.outEdges(i), function(a) {
            (0, _chunkBKDDFIKNMjs.x)(n, a.w) ? e.push(a) : t(a.w);
        }), delete n[i]);
    }
    return (0, _chunkGTKDMUJJMjs.a)(t, "dfs"), (0, _chunkBKDDFIKNMjs.n)(r.nodes(), t), e;
}
(0, _chunkGTKDMUJJMjs.a)(ye, "dfsFAS");
function Vr(r) {
    (0, _chunkBKDDFIKNMjs.n)(r.edges(), function(e) {
        var n = r.edge(e);
        if (n.reversed) {
            r.removeEdge(e);
            var o = n.forwardName;
            delete n.reversed, delete n.forwardName, r.setEdge(e.w, e.v, n, o);
        }
    });
}
(0, _chunkGTKDMUJJMjs.a)(Vr, "undo");
function Ar(r) {
    r.graph().dummyChains = [], (0, _chunkBKDDFIKNMjs.n)(r.edges(), function(e) {
        xe(r, e);
    });
}
(0, _chunkGTKDMUJJMjs.a)(Ar, "run");
function xe(r, e) {
    var n = e.v, o = r.node(n).rank, t = e.w, i = r.node(t).rank, a = e.name, s = r.edge(e), d = s.labelRank;
    if (i !== o + 1) {
        r.removeEdge(e);
        var c, h, l;
        for(l = 0, ++o; o < i; ++l, ++o)s.points = [], h = {
            width: 0,
            height: 0,
            edgeLabel: s,
            edgeObj: e,
            rank: o
        }, c = N(r, "edge", h, "_d"), o === d && (h.width = s.width, h.height = s.height, h.dummy = "edge-label", h.labelpos = s.labelpos), r.setEdge(n, c, {
            weight: s.weight
        }, a), l === 0 && r.graph().dummyChains.push(c), n = c;
        r.setEdge(n, t, {
            weight: s.weight
        }, a);
    }
}
(0, _chunkGTKDMUJJMjs.a)(xe, "normalizeEdge");
function Dr(r) {
    (0, _chunkBKDDFIKNMjs.n)(r.graph().dummyChains, function(e) {
        var n = r.node(e), o = n.edgeLabel, t;
        for(r.setEdge(n.edgeObj, o); n.dummy;)t = r.successors(e)[0], r.removeNode(e), o.points.push({
            x: n.x,
            y: n.y
        }), n.dummy === "edge-label" && (o.x = n.x, o.y = n.y, o.width = n.width, o.height = n.height), e = t, n = r.node(e);
    });
}
(0, _chunkGTKDMUJJMjs.a)(Dr, "undo");
function W(r) {
    var e = {};
    function n(o) {
        var t = r.node(o);
        if ((0, _chunkBKDDFIKNMjs.x)(e, o)) return t.rank;
        e[o] = !0;
        var i = (0, _chunkBKDDFIKNMjs.G)((0, _chunkBKDDFIKNMjs.s)(r.outEdges(o), function(a) {
            return n(a.w) - r.edge(a).minlen;
        }));
        return (i === Number.POSITIVE_INFINITY || i === void 0 || i === null) && (i = 0), t.rank = i;
    }
    (0, _chunkGTKDMUJJMjs.a)(n, "dfs"), (0, _chunkBKDDFIKNMjs.n)(r.sources(), n);
}
(0, _chunkGTKDMUJJMjs.a)(W, "longestPath");
function G(r, e) {
    return r.node(e.w).rank - r.node(e.v).rank - r.edge(e).minlen;
}
(0, _chunkGTKDMUJJMjs.a)(G, "slack");
function J(r) {
    var e = new (0, _chunk6XGRHI2AMjs.a)({
        directed: !1
    }), n = r.nodes()[0], o = r.nodeCount();
    e.setNode(n, {});
    for(var t, i; ke(e, r) < o;)t = ge(e, r), i = e.hasNode(t.v) ? G(r, t) : -G(r, t), Ne(e, r, i);
    return e;
}
(0, _chunkGTKDMUJJMjs.a)(J, "feasibleTree");
function ke(r, e) {
    function n(o) {
        (0, _chunkBKDDFIKNMjs.n)(e.nodeEdges(o), function(t) {
            var i = t.v, a = o === i ? t.w : i;
            !r.hasNode(a) && !G(e, t) && (r.setNode(a, {}), r.setEdge(o, a, {}), n(a));
        });
    }
    return (0, _chunkGTKDMUJJMjs.a)(n, "dfs"), (0, _chunkBKDDFIKNMjs.n)(r.nodes(), n), r.nodeCount();
}
(0, _chunkGTKDMUJJMjs.a)(ke, "tightTree");
function ge(r, e) {
    return (0, _chunkBKDDFIKNMjs.H)(e.edges(), function(n) {
        if (r.hasNode(n.v) !== r.hasNode(n.w)) return G(e, n);
    });
}
(0, _chunkGTKDMUJJMjs.a)(ge, "findMinSlackEdge");
function Ne(r, e, n) {
    (0, _chunkBKDDFIKNMjs.n)(r.nodes(), function(o) {
        e.node(o).rank += n;
    });
}
(0, _chunkGTKDMUJJMjs.a)(Ne, "shiftRanks");
var oo = (0, _chunk6BY5RJGCMjs.O)(1);
var mo = (0, _chunk6BY5RJGCMjs.O)(1);
ar.CycleException = q;
function ar(r) {
    var e = {}, n = {}, o = [];
    function t(i) {
        if ((0, _chunkBKDDFIKNMjs.x)(n, i)) throw new q;
        (0, _chunkBKDDFIKNMjs.x)(e, i) || (n[i] = !0, e[i] = !0, (0, _chunkBKDDFIKNMjs.n)(r.predecessors(i), t), delete n[i], o.push(i));
    }
    if ((0, _chunkGTKDMUJJMjs.a)(t, "visit"), (0, _chunkBKDDFIKNMjs.n)(r.sinks(), t), (0, _chunkBKDDFIKNMjs.N)(e) !== r.nodeCount()) throw new q;
    return o;
}
(0, _chunkGTKDMUJJMjs.a)(ar, "topsort");
function q() {}
(0, _chunkGTKDMUJJMjs.a)(q, "CycleException");
q.prototype = new Error;
function K(r, e, n) {
    (0, _chunk6BY5RJGCMjs.z)(e) || (e = [
        e
    ]);
    var o = (r.isDirected() ? r.successors : r.neighbors).bind(r), t = [], i = {};
    return (0, _chunkBKDDFIKNMjs.n)(e, function(a) {
        if (!r.hasNode(a)) throw new Error("Graph does not have node: " + a);
        Yr(r, a, n === "post", i, o, t);
    }), t;
}
(0, _chunkGTKDMUJJMjs.a)(K, "dfs");
function Yr(r, e, n, o, t, i) {
    (0, _chunkBKDDFIKNMjs.x)(o, e) || (o[e] = !0, n || i.push(e), (0, _chunkBKDDFIKNMjs.n)(t(e), function(a) {
        Yr(r, a, n, o, t, i);
    }), n && i.push(e));
}
(0, _chunkGTKDMUJJMjs.a)(Yr, "doDfs");
function fr(r, e) {
    return K(r, e, "post");
}
(0, _chunkGTKDMUJJMjs.a)(fr, "postorder");
function sr(r, e) {
    return K(r, e, "pre");
}
(0, _chunkGTKDMUJJMjs.a)(sr, "preorder");
P.initLowLimValues = dr;
P.initCutValues = ur;
P.calcCutValue = Ur;
P.leaveEdge = qr;
P.enterEdge = Xr;
P.exchangeEdges = Hr;
function P(r) {
    r = xr(r), W(r);
    var e = J(r);
    dr(e), ur(e, r);
    for(var n, o; n = qr(e);)o = Xr(e, r, n), Hr(e, r, n, o);
}
(0, _chunkGTKDMUJJMjs.a)(P, "networkSimplex");
function ur(r, e) {
    var n = fr(r, r.nodes());
    n = n.slice(0, n.length - 1), (0, _chunkBKDDFIKNMjs.n)(n, function(o) {
        Re(r, e, o);
    });
}
(0, _chunkGTKDMUJJMjs.a)(ur, "initCutValues");
function Re(r, e, n) {
    var o = r.node(n), t = o.parent;
    r.edge(n, t).cutvalue = Ur(r, e, n);
}
(0, _chunkGTKDMUJJMjs.a)(Re, "assignCutValue");
function Ur(r, e, n) {
    var o = r.node(n), t = o.parent, i = !0, a = e.edge(n, t), s = 0;
    return a || (i = !1, a = e.edge(t, n)), s = a.weight, (0, _chunkBKDDFIKNMjs.n)(e.nodeEdges(n), function(d) {
        var c = d.v === n, h = c ? d.w : d.v;
        if (h !== t) {
            var l = c === i, m = e.edge(d).weight;
            if (s += l ? m : -m, Me(r, n, h)) {
                var v = r.edge(n, h).cutvalue;
                s += l ? -v : v;
            }
        }
    }), s;
}
(0, _chunkGTKDMUJJMjs.a)(Ur, "calcCutValue");
function dr(r, e) {
    arguments.length < 2 && (e = r.nodes()[0]), Wr(r, {}, 1, e);
}
(0, _chunkGTKDMUJJMjs.a)(dr, "initLowLimValues");
function Wr(r, e, n, o, t) {
    var i = n, a = r.node(o);
    return e[o] = !0, (0, _chunkBKDDFIKNMjs.n)(r.neighbors(o), function(s) {
        (0, _chunkBKDDFIKNMjs.x)(e, s) || (n = Wr(r, e, n, s, o));
    }), a.low = i, a.lim = n++, t ? a.parent = t : delete a.parent, n;
}
(0, _chunkGTKDMUJJMjs.a)(Wr, "dfsAssignLowLim");
function qr(r) {
    return (0, _chunkBKDDFIKNMjs.q)(r.edges(), function(e) {
        return r.edge(e).cutvalue < 0;
    });
}
(0, _chunkGTKDMUJJMjs.a)(qr, "leaveEdge");
function Xr(r, e, n) {
    var o = n.v, t = n.w;
    e.hasEdge(o, t) || (o = n.w, t = n.v);
    var i = r.node(o), a = r.node(t), s = i, d = !1;
    i.lim > a.lim && (s = a, d = !0);
    var c = (0, _chunkBKDDFIKNMjs.p)(e.edges(), function(h) {
        return d === zr(r, r.node(h.v), s) && d !== zr(r, r.node(h.w), s);
    });
    return (0, _chunkBKDDFIKNMjs.H)(c, function(h) {
        return G(e, h);
    });
}
(0, _chunkGTKDMUJJMjs.a)(Xr, "enterEdge");
function Hr(r, e, n, o) {
    var t = n.v, i = n.w;
    r.removeEdge(t, i), r.setEdge(o.v, o.w, {}), dr(r), ur(r, e), Se(r, e);
}
(0, _chunkGTKDMUJJMjs.a)(Hr, "exchangeEdges");
function Se(r, e) {
    var n = (0, _chunkBKDDFIKNMjs.q)(r.nodes(), function(t) {
        return !e.node(t).parent;
    }), o = sr(r, n);
    o = o.slice(1), (0, _chunkBKDDFIKNMjs.n)(o, function(t) {
        var i = r.node(t).parent, a = e.edge(t, i), s = !1;
        a || (a = e.edge(i, t), s = !0), e.node(t).rank = e.node(i).rank + (s ? a.minlen : -a.minlen);
    });
}
(0, _chunkGTKDMUJJMjs.a)(Se, "updateRanks");
function Me(r, e, n) {
    return r.hasEdge(e, n);
}
(0, _chunkGTKDMUJJMjs.a)(Me, "isTreeEdge");
function zr(r, e, n) {
    return n.low <= e.lim && e.lim <= n.lim;
}
(0, _chunkGTKDMUJJMjs.a)(zr, "isDescendant");
function cr(r) {
    switch(r.graph().ranker){
        case "network-simplex":
            Jr(r);
            break;
        case "tight-tree":
            Fe(r);
            break;
        case "longest-path":
            Pe(r);
            break;
        default:
            Jr(r);
    }
}
(0, _chunkGTKDMUJJMjs.a)(cr, "rank");
var Pe = W;
function Fe(r) {
    W(r), J(r);
}
(0, _chunkGTKDMUJJMjs.a)(Fe, "tightTreeRanker");
function Jr(r) {
    P(r);
}
(0, _chunkGTKDMUJJMjs.a)(Jr, "networkSimplexRanker");
function Kr(r) {
    var e = N(r, "root", {}, "_root"), n = Oe(r), o = (0, _chunkBKDDFIKNMjs.F)((0, _chunkBKDDFIKNMjs.z)(n)) - 1, t = 2 * o + 1;
    r.graph().nestingRoot = e, (0, _chunkBKDDFIKNMjs.n)(r.edges(), function(a) {
        r.edge(a).minlen *= t;
    });
    var i = Ge(r) + 1;
    (0, _chunkBKDDFIKNMjs.n)(r.children(), function(a) {
        Qr(r, e, t, i, o, n, a);
    }), r.graph().nodeRankFactor = t;
}
(0, _chunkGTKDMUJJMjs.a)(Kr, "run");
function Qr(r, e, n, o, t, i, a) {
    var s = r.children(a);
    if (!s.length) {
        a !== e && r.setEdge(e, a, {
            weight: 0,
            minlen: n
        });
        return;
    }
    var d = rr(r, "_bt"), c = rr(r, "_bb"), h = r.node(a);
    r.setParent(d, a), h.borderTop = d, r.setParent(c, a), h.borderBottom = c, (0, _chunkBKDDFIKNMjs.n)(s, function(l) {
        Qr(r, e, n, o, t, i, l);
        var m = r.node(l), v = m.borderTop ? m.borderTop : l, _ = m.borderBottom ? m.borderBottom : l, b = m.borderTop ? o : 2 * o, Y = v !== _ ? 1 : t - i[a] + 1;
        r.setEdge(d, v, {
            weight: b,
            minlen: Y,
            nestingEdge: !0
        }), r.setEdge(_, c, {
            weight: b,
            minlen: Y,
            nestingEdge: !0
        });
    }), r.parent(a) || r.setEdge(e, d, {
        weight: 0,
        minlen: t + i[a]
    });
}
(0, _chunkGTKDMUJJMjs.a)(Qr, "dfs");
function Oe(r) {
    var e = {};
    function n(o, t) {
        var i = r.children(o);
        i && i.length && (0, _chunkBKDDFIKNMjs.n)(i, function(a) {
            n(a, t + 1);
        }), e[o] = t;
    }
    return (0, _chunkGTKDMUJJMjs.a)(n, "dfs"), (0, _chunkBKDDFIKNMjs.n)(r.children(), function(o) {
        n(o, 1);
    }), e;
}
(0, _chunkGTKDMUJJMjs.a)(Oe, "treeDepths");
function Ge(r) {
    return (0, _chunkBKDDFIKNMjs.L)(r.edges(), function(e, n) {
        return e + r.edge(n).weight;
    }, 0);
}
(0, _chunkGTKDMUJJMjs.a)(Ge, "sumWeights");
function Zr(r) {
    var e = r.graph();
    r.removeNode(e.nestingRoot), delete e.nestingRoot, (0, _chunkBKDDFIKNMjs.n)(r.edges(), function(n) {
        var o = r.edge(n);
        o.nestingEdge && r.removeEdge(n);
    });
}
(0, _chunkGTKDMUJJMjs.a)(Zr, "cleanup");
function $r(r, e, n) {
    var o = {}, t;
    (0, _chunkBKDDFIKNMjs.n)(n, function(i) {
        for(var a = r.parent(i), s, d; a;){
            if (s = r.parent(a), s ? (d = o[s], o[s] = a) : (d = t, t = a), d && d !== a) {
                e.setEdge(d, a);
                return;
            }
            a = s;
        }
    });
}
(0, _chunkGTKDMUJJMjs.a)($r, "addSubgraphConstraints");
function re(r, e, n) {
    var o = Be(r), t = new (0, _chunk6XGRHI2AMjs.a)({
        compound: !0
    }).setGraph({
        root: o
    }).setDefaultNodeLabel(function(i) {
        return r.node(i);
    });
    return (0, _chunkBKDDFIKNMjs.n)(r.nodes(), function(i) {
        var a = r.node(i), s = r.parent(i);
        (a.rank === e || a.minRank <= e && e <= a.maxRank) && (t.setNode(i), t.setParent(i, s || o), (0, _chunkBKDDFIKNMjs.n)(r[n](i), function(d) {
            var c = d.v === i ? d.w : d.v, h = t.edge(c, i), l = (0, _chunkBKDDFIKNMjs.D)(h) ? 0 : h.weight;
            t.setEdge(c, i, {
                weight: r.edge(d).weight + l
            });
        }), (0, _chunkBKDDFIKNMjs.x)(a, "minRank") && t.setNode(i, {
            borderLeft: a.borderLeft[e],
            borderRight: a.borderRight[e]
        }));
    }), t;
}
(0, _chunkGTKDMUJJMjs.a)(re, "buildLayerGraph");
function Be(r) {
    for(var e; r.hasNode(e = (0, _chunkBKDDFIKNMjs.T)("_root")););
    return e;
}
(0, _chunkGTKDMUJJMjs.a)(Be, "createRootNode");
function ee(r, e) {
    for(var n = 0, o = 1; o < e.length; ++o)n += Ae(r, e[o - 1], e[o]);
    return n;
}
(0, _chunkGTKDMUJJMjs.a)(ee, "crossCount");
function Ae(r, e, n) {
    for(var o = (0, _chunkBKDDFIKNMjs.U)(n, (0, _chunkBKDDFIKNMjs.s)(n, function(c, h) {
        return h;
    })), t = (0, _chunkBKDDFIKNMjs.d)((0, _chunkBKDDFIKNMjs.s)(e, function(c) {
        return (0, _chunkBKDDFIKNMjs.P)((0, _chunkBKDDFIKNMjs.s)(r.outEdges(c), function(h) {
            return {
                pos: o[h.w],
                weight: r.edge(h).weight
            };
        }), "pos");
    })), i = 1; i < n.length;)i <<= 1;
    var a = 2 * i - 1;
    i -= 1;
    var s = (0, _chunkBKDDFIKNMjs.s)(new Array(a), function() {
        return 0;
    }), d = 0;
    return (0, _chunkBKDDFIKNMjs.n)(t.forEach(function(c) {
        var h = c.pos + i;
        s[h] += c.weight;
        for(var l = 0; h > 0;)h % 2 && (l += s[h + 1]), h = h - 1 >> 1, s[h] += c.weight;
        d += c.weight * l;
    })), d;
}
(0, _chunkGTKDMUJJMjs.a)(Ae, "twoLayerCrossCount");
function ne(r) {
    var e = {}, n = (0, _chunkBKDDFIKNMjs.p)(r.nodes(), function(s) {
        return !r.children(s).length;
    }), o = (0, _chunkBKDDFIKNMjs.F)((0, _chunkBKDDFIKNMjs.s)(n, function(s) {
        return r.node(s).rank;
    })), t = (0, _chunkBKDDFIKNMjs.s)((0, _chunkBKDDFIKNMjs.K)(o + 1), function() {
        return [];
    });
    function i(s) {
        if (!(0, _chunkBKDDFIKNMjs.x)(e, s)) {
            e[s] = !0;
            var d = r.node(s);
            t[d.rank].push(s), (0, _chunkBKDDFIKNMjs.n)(r.successors(s), i);
        }
    }
    (0, _chunkGTKDMUJJMjs.a)(i, "dfs");
    var a = (0, _chunkBKDDFIKNMjs.P)(n, function(s) {
        return r.node(s).rank;
    });
    return (0, _chunkBKDDFIKNMjs.n)(a, i), t;
}
(0, _chunkGTKDMUJJMjs.a)(ne, "initOrder");
function oe(r, e) {
    return (0, _chunkBKDDFIKNMjs.s)(e, function(n) {
        var o = r.inEdges(n);
        if (o.length) {
            var t = (0, _chunkBKDDFIKNMjs.L)(o, function(i, a) {
                var s = r.edge(a), d = r.node(a.v);
                return {
                    sum: i.sum + s.weight * d.order,
                    weight: i.weight + s.weight
                };
            }, {
                sum: 0,
                weight: 0
            });
            return {
                v: n,
                barycenter: t.sum / t.weight,
                weight: t.weight
            };
        } else return {
            v: n
        };
    });
}
(0, _chunkGTKDMUJJMjs.a)(oe, "barycenter");
function te(r, e) {
    var n = {};
    (0, _chunkBKDDFIKNMjs.n)(r, function(t, i) {
        var a = n[t.v] = {
            indegree: 0,
            in: [],
            out: [],
            vs: [
                t.v
            ],
            i
        };
        (0, _chunkBKDDFIKNMjs.D)(t.barycenter) || (a.barycenter = t.barycenter, a.weight = t.weight);
    }), (0, _chunkBKDDFIKNMjs.n)(e.edges(), function(t) {
        var i = n[t.v], a = n[t.w];
        !(0, _chunkBKDDFIKNMjs.D)(i) && !(0, _chunkBKDDFIKNMjs.D)(a) && (a.indegree++, i.out.push(n[t.w]));
    });
    var o = (0, _chunkBKDDFIKNMjs.p)(n, function(t) {
        return !t.indegree;
    });
    return De(o);
}
(0, _chunkGTKDMUJJMjs.a)(te, "resolveConflicts");
function De(r) {
    var e = [];
    function n(i) {
        return function(a) {
            a.merged || ((0, _chunkBKDDFIKNMjs.D)(a.barycenter) || (0, _chunkBKDDFIKNMjs.D)(i.barycenter) || a.barycenter >= i.barycenter) && je(i, a);
        };
    }
    (0, _chunkGTKDMUJJMjs.a)(n, "handleIn");
    function o(i) {
        return function(a) {
            a.in.push(i), --a.indegree === 0 && r.push(a);
        };
    }
    for((0, _chunkGTKDMUJJMjs.a)(o, "handleOut"); r.length;){
        var t = r.pop();
        e.push(t), (0, _chunkBKDDFIKNMjs.n)(t.in.reverse(), n(t)), (0, _chunkBKDDFIKNMjs.n)(t.out, o(t));
    }
    return (0, _chunkBKDDFIKNMjs.s)((0, _chunkBKDDFIKNMjs.p)(e, function(i) {
        return !i.merged;
    }), function(i) {
        return (0, _chunkBKDDFIKNMjs.J)(i, [
            "vs",
            "i",
            "barycenter",
            "weight"
        ]);
    });
}
(0, _chunkGTKDMUJJMjs.a)(De, "doResolveConflicts");
function je(r, e) {
    var n = 0, o = 0;
    r.weight && (n += r.barycenter * r.weight, o += r.weight), e.weight && (n += e.barycenter * e.weight, o += e.weight), r.vs = e.vs.concat(r.vs), r.barycenter = n / o, r.weight = o, r.i = Math.min(e.i, r.i), e.merged = !0;
}
(0, _chunkGTKDMUJJMjs.a)(je, "mergeEntries");
function ae(r, e) {
    var n = Nr(r, function(h) {
        return (0, _chunkBKDDFIKNMjs.x)(h, "barycenter");
    }), o = n.lhs, t = (0, _chunkBKDDFIKNMjs.P)(n.rhs, function(h) {
        return -h.i;
    }), i = [], a = 0, s = 0, d = 0;
    o.sort(Ye(!!e)), d = ie(i, t, d), (0, _chunkBKDDFIKNMjs.n)(o, function(h) {
        d += h.vs.length, i.push(h.vs), a += h.barycenter * h.weight, s += h.weight, d = ie(i, t, d);
    });
    var c = {
        vs: (0, _chunkBKDDFIKNMjs.d)(i)
    };
    return s && (c.barycenter = a / s, c.weight = s), c;
}
(0, _chunkGTKDMUJJMjs.a)(ae, "sort");
function ie(r, e, n) {
    for(var o; e.length && (o = (0, _chunkBKDDFIKNMjs.k)(e)).i <= n;)e.pop(), r.push(o.vs), n++;
    return n;
}
(0, _chunkGTKDMUJJMjs.a)(ie, "consumeUnsortable");
function Ye(r) {
    return function(e, n) {
        return e.barycenter < n.barycenter ? -1 : e.barycenter > n.barycenter ? 1 : r ? n.i - e.i : e.i - n.i;
    };
}
(0, _chunkGTKDMUJJMjs.a)(Ye, "compareWithBias");
function hr(r, e, n, o) {
    var t = r.children(e), i = r.node(e), a = i ? i.borderLeft : void 0, s = i ? i.borderRight : void 0, d = {};
    a && (t = (0, _chunkBKDDFIKNMjs.p)(t, function(_) {
        return _ !== a && _ !== s;
    }));
    var c = oe(r, t);
    (0, _chunkBKDDFIKNMjs.n)(c, function(_) {
        if (r.children(_.v).length) {
            var b = hr(r, _.v, n, o);
            d[_.v] = b, (0, _chunkBKDDFIKNMjs.x)(b, "barycenter") && Ue(_, b);
        }
    });
    var h = te(c, n);
    ze(h, d);
    var l = ae(h, o);
    if (a && (l.vs = (0, _chunkBKDDFIKNMjs.d)([
        a,
        l.vs,
        s
    ]), r.predecessors(a).length)) {
        var m = r.node(r.predecessors(a)[0]), v = r.node(r.predecessors(s)[0]);
        (0, _chunkBKDDFIKNMjs.x)(l, "barycenter") || (l.barycenter = 0, l.weight = 0), l.barycenter = (l.barycenter * l.weight + m.order + v.order) / (l.weight + 2), l.weight += 2;
    }
    return l;
}
(0, _chunkGTKDMUJJMjs.a)(hr, "sortSubgraph");
function ze(r, e) {
    (0, _chunkBKDDFIKNMjs.n)(r, function(n) {
        n.vs = (0, _chunkBKDDFIKNMjs.d)(n.vs.map(function(o) {
            return e[o] ? e[o].vs : o;
        }));
    });
}
(0, _chunkGTKDMUJJMjs.a)(ze, "expandSubgraphs");
function Ue(r, e) {
    (0, _chunkBKDDFIKNMjs.D)(r.barycenter) ? (r.barycenter = e.barycenter, r.weight = e.weight) : (r.barycenter = (r.barycenter * r.weight + e.barycenter * e.weight) / (r.weight + e.weight), r.weight += e.weight);
}
(0, _chunkGTKDMUJJMjs.a)(Ue, "mergeBarycenters");
function ue(r) {
    var e = er(r), n = fe(r, (0, _chunkBKDDFIKNMjs.K)(1, e + 1), "inEdges"), o = fe(r, (0, _chunkBKDDFIKNMjs.K)(e - 1, -1, -1), "outEdges"), t = ne(r);
    se(r, t);
    for(var i = Number.POSITIVE_INFINITY, a, s = 0, d = 0; d < 4; ++s, ++d){
        We(s % 2 ? n : o, s % 4 >= 2), t = S(r);
        var c = ee(r, t);
        c < i && (d = 0, a = (0, _chunkBKDDFIKNMjs.f)(t), i = c);
    }
    se(r, a);
}
(0, _chunkGTKDMUJJMjs.a)(ue, "order");
function fe(r, e, n) {
    return (0, _chunkBKDDFIKNMjs.s)(e, function(o) {
        return re(r, o, n);
    });
}
(0, _chunkGTKDMUJJMjs.a)(fe, "buildLayerGraphs");
function We(r, e) {
    var n = new (0, _chunk6XGRHI2AMjs.a);
    (0, _chunkBKDDFIKNMjs.n)(r, function(o) {
        var t = o.graph().root, i = hr(o, t, n, e);
        (0, _chunkBKDDFIKNMjs.n)(i.vs, function(a, s) {
            o.node(a).order = s;
        }), $r(o, n, i.vs);
    });
}
(0, _chunkGTKDMUJJMjs.a)(We, "sweepLayerGraphs");
function se(r, e) {
    (0, _chunkBKDDFIKNMjs.n)(e, function(n) {
        (0, _chunkBKDDFIKNMjs.n)(n, function(o, t) {
            r.node(o).order = t;
        });
    });
}
(0, _chunkGTKDMUJJMjs.a)(se, "assignOrder");
function de(r) {
    var e = Xe(r);
    (0, _chunkBKDDFIKNMjs.n)(r.graph().dummyChains, function(n) {
        for(var o = r.node(n), t = o.edgeObj, i = qe(r, e, t.v, t.w), a = i.path, s = i.lca, d = 0, c = a[d], h = !0; n !== t.w;){
            if (o = r.node(n), h) {
                for(; (c = a[d]) !== s && r.node(c).maxRank < o.rank;)d++;
                c === s && (h = !1);
            }
            if (!h) {
                for(; d < a.length - 1 && r.node(c = a[d + 1]).minRank <= o.rank;)d++;
                c = a[d];
            }
            r.setParent(n, c), n = r.successors(n)[0];
        }
    });
}
(0, _chunkGTKDMUJJMjs.a)(de, "parentDummyChains");
function qe(r, e, n, o) {
    var t = [], i = [], a = Math.min(e[n].low, e[o].low), s = Math.max(e[n].lim, e[o].lim), d, c;
    d = n;
    do d = r.parent(d), t.push(d);
    while (d && (e[d].low > a || s > e[d].lim));
    for(c = d, d = o; (d = r.parent(d)) !== c;)i.push(d);
    return {
        path: t.concat(i.reverse()),
        lca: c
    };
}
(0, _chunkGTKDMUJJMjs.a)(qe, "findPath");
function Xe(r) {
    var e = {}, n = 0;
    function o(t) {
        var i = n;
        (0, _chunkBKDDFIKNMjs.n)(r.children(t), o), e[t] = {
            low: i,
            lim: n++
        };
    }
    return (0, _chunkGTKDMUJJMjs.a)(o, "dfs"), (0, _chunkBKDDFIKNMjs.n)(r.children(), o), e;
}
(0, _chunkGTKDMUJJMjs.a)(Xe, "postorder");
function He(r, e) {
    var n = {};
    function o(t, i) {
        var a = 0, s = 0, d = t.length, c = (0, _chunkBKDDFIKNMjs.k)(i);
        return (0, _chunkBKDDFIKNMjs.n)(i, function(h, l) {
            var m = Ke(r, h), v = m ? r.node(m).order : d;
            (m || h === c) && ((0, _chunkBKDDFIKNMjs.n)(i.slice(s, l + 1), function(_) {
                (0, _chunkBKDDFIKNMjs.n)(r.predecessors(_), function(b) {
                    var Y = r.node(b), mr = Y.order;
                    (mr < a || v < mr) && !(Y.dummy && r.node(_).dummy) && ce(n, b, _);
                });
            }), s = l + 1, a = v);
        }), i;
    }
    return (0, _chunkGTKDMUJJMjs.a)(o, "visitLayer"), (0, _chunkBKDDFIKNMjs.L)(e, o), n;
}
(0, _chunkGTKDMUJJMjs.a)(He, "findType1Conflicts");
function Je(r, e) {
    var n = {};
    function o(i, a, s, d, c) {
        var h;
        (0, _chunkBKDDFIKNMjs.n)((0, _chunkBKDDFIKNMjs.K)(a, s), function(l) {
            h = i[l], r.node(h).dummy && (0, _chunkBKDDFIKNMjs.n)(r.predecessors(h), function(m) {
                var v = r.node(m);
                v.dummy && (v.order < d || v.order > c) && ce(n, m, h);
            });
        });
    }
    (0, _chunkGTKDMUJJMjs.a)(o, "scan");
    function t(i, a) {
        var s = -1, d, c = 0;
        return (0, _chunkBKDDFIKNMjs.n)(a, function(h, l) {
            if (r.node(h).dummy === "border") {
                var m = r.predecessors(h);
                m.length && (d = r.node(m[0]).order, o(a, c, l, s, d), c = l, s = d);
            }
            o(a, c, a.length, d, i.length);
        }), a;
    }
    return (0, _chunkGTKDMUJJMjs.a)(t, "visitLayer"), (0, _chunkBKDDFIKNMjs.L)(e, t), n;
}
(0, _chunkGTKDMUJJMjs.a)(Je, "findType2Conflicts");
function Ke(r, e) {
    if (r.node(e).dummy) return (0, _chunkBKDDFIKNMjs.q)(r.predecessors(e), function(n) {
        return r.node(n).dummy;
    });
}
(0, _chunkGTKDMUJJMjs.a)(Ke, "findOtherInnerSegmentNode");
function ce(r, e, n) {
    if (e > n) {
        var o = e;
        e = n, n = o;
    }
    var t = r[e];
    t || (r[e] = t = {}), t[n] = !0;
}
(0, _chunkGTKDMUJJMjs.a)(ce, "addConflict");
function Qe(r, e, n) {
    if (e > n) {
        var o = e;
        e = n, n = o;
    }
    return (0, _chunkBKDDFIKNMjs.x)(r[e], n);
}
(0, _chunkGTKDMUJJMjs.a)(Qe, "hasConflict");
function Ze(r, e, n, o) {
    var t = {}, i = {}, a = {};
    return (0, _chunkBKDDFIKNMjs.n)(e, function(s) {
        (0, _chunkBKDDFIKNMjs.n)(s, function(d, c) {
            t[d] = d, i[d] = d, a[d] = c;
        });
    }), (0, _chunkBKDDFIKNMjs.n)(e, function(s) {
        var d = -1;
        (0, _chunkBKDDFIKNMjs.n)(s, function(c) {
            var h = o(c);
            if (h.length) {
                h = (0, _chunkBKDDFIKNMjs.P)(h, function(b) {
                    return a[b];
                });
                for(var l = (h.length - 1) / 2, m = Math.floor(l), v = Math.ceil(l); m <= v; ++m){
                    var _ = h[m];
                    i[c] === c && d < a[_] && !Qe(n, c, _) && (i[_] = c, i[c] = t[c] = t[_], d = a[_]);
                }
            }
        });
    }), {
        root: t,
        align: i
    };
}
(0, _chunkGTKDMUJJMjs.a)(Ze, "verticalAlignment");
function $e(r, e, n, o, t) {
    var i = {}, a = rn(r, e, n, t), s = t ? "borderLeft" : "borderRight";
    function d(l, m) {
        for(var v = a.nodes(), _ = v.pop(), b = {}; _;)b[_] ? l(_) : (b[_] = !0, v.push(_), v = v.concat(m(_))), _ = v.pop();
    }
    (0, _chunkGTKDMUJJMjs.a)(d, "iterate");
    function c(l) {
        i[l] = a.inEdges(l).reduce(function(m, v) {
            return Math.max(m, i[v.v] + a.edge(v));
        }, 0);
    }
    (0, _chunkGTKDMUJJMjs.a)(c, "pass1");
    function h(l) {
        var m = a.outEdges(l).reduce(function(_, b) {
            return Math.min(_, i[b.w] - a.edge(b));
        }, Number.POSITIVE_INFINITY), v = r.node(l);
        m !== Number.POSITIVE_INFINITY && v.borderType !== s && (i[l] = Math.max(i[l], m));
    }
    return (0, _chunkGTKDMUJJMjs.a)(h, "pass2"), d(c, a.predecessors.bind(a)), d(h, a.successors.bind(a)), (0, _chunkBKDDFIKNMjs.n)(o, function(l) {
        i[l] = i[n[l]];
    }), i;
}
(0, _chunkGTKDMUJJMjs.a)($e, "horizontalCompaction");
function rn(r, e, n, o) {
    var t = new (0, _chunk6XGRHI2AMjs.a), i = r.graph(), a = tn(i.nodesep, i.edgesep, o);
    return (0, _chunkBKDDFIKNMjs.n)(e, function(s) {
        var d;
        (0, _chunkBKDDFIKNMjs.n)(s, function(c) {
            var h = n[c];
            if (t.setNode(h), d) {
                var l = n[d], m = t.edge(l, h);
                t.setEdge(l, h, Math.max(a(r, c, d), m || 0));
            }
            d = c;
        });
    }), t;
}
(0, _chunkGTKDMUJJMjs.a)(rn, "buildBlockGraph");
function en(r, e) {
    return (0, _chunkBKDDFIKNMjs.H)((0, _chunkBKDDFIKNMjs.z)(e), function(n) {
        var o = Number.NEGATIVE_INFINITY, t = Number.POSITIVE_INFINITY;
        return (0, _chunkBKDDFIKNMjs.u)(n, function(i, a) {
            var s = an(r, a) / 2;
            o = Math.max(i + s, o), t = Math.min(i - s, t);
        }), o - t;
    });
}
(0, _chunkGTKDMUJJMjs.a)(en, "findSmallestWidthAlignment");
function nn(r, e) {
    var n = (0, _chunkBKDDFIKNMjs.z)(e), o = (0, _chunkBKDDFIKNMjs.G)(n), t = (0, _chunkBKDDFIKNMjs.F)(n);
    (0, _chunkBKDDFIKNMjs.n)([
        "u",
        "d"
    ], function(i) {
        (0, _chunkBKDDFIKNMjs.n)([
            "l",
            "r"
        ], function(a) {
            var s = i + a, d = r[s], c;
            if (d !== e) {
                var h = (0, _chunkBKDDFIKNMjs.z)(d);
                c = a === "l" ? o - (0, _chunkBKDDFIKNMjs.G)(h) : t - (0, _chunkBKDDFIKNMjs.F)(h), c && (r[s] = (0, _chunkBKDDFIKNMjs.E)(d, function(l) {
                    return l + c;
                }));
            }
        });
    });
}
(0, _chunkGTKDMUJJMjs.a)(nn, "alignCoordinates");
function on(r, e) {
    return (0, _chunkBKDDFIKNMjs.E)(r.ul, function(n, o) {
        if (e) return r[e.toLowerCase()][o];
        var t = (0, _chunkBKDDFIKNMjs.P)((0, _chunkBKDDFIKNMjs.s)(r, o));
        return (t[1] + t[2]) / 2;
    });
}
(0, _chunkGTKDMUJJMjs.a)(on, "balance");
function he(r) {
    var e = S(r), n = (0, _chunk6BY5RJGCMjs.T)(He(r, e), Je(r, e)), o = {}, t;
    (0, _chunkBKDDFIKNMjs.n)([
        "u",
        "d"
    ], function(a) {
        t = a === "u" ? e : (0, _chunkBKDDFIKNMjs.z)(e).reverse(), (0, _chunkBKDDFIKNMjs.n)([
            "l",
            "r"
        ], function(s) {
            s === "r" && (t = (0, _chunkBKDDFIKNMjs.s)(t, function(l) {
                return (0, _chunkBKDDFIKNMjs.z)(l).reverse();
            }));
            var d = (a === "u" ? r.predecessors : r.successors).bind(r), c = Ze(r, t, n, d), h = $e(r, t, c.root, c.align, s === "r");
            s === "r" && (h = (0, _chunkBKDDFIKNMjs.E)(h, function(l) {
                return -l;
            })), o[a + s] = h;
        });
    });
    var i = en(r, o);
    return nn(o, i), on(o, r.graph().align);
}
(0, _chunkGTKDMUJJMjs.a)(he, "positionX");
function tn(r, e, n) {
    return function(o, t, i) {
        var a = o.node(t), s = o.node(i), d = 0, c;
        if (d += a.width / 2, (0, _chunkBKDDFIKNMjs.x)(a, "labelpos")) switch(a.labelpos.toLowerCase()){
            case "l":
                c = -a.width / 2;
                break;
            case "r":
                c = a.width / 2;
                break;
        }
        if (c && (d += n ? c : -c), c = 0, d += (a.dummy ? e : r) / 2, d += (s.dummy ? e : r) / 2, d += s.width / 2, (0, _chunkBKDDFIKNMjs.x)(s, "labelpos")) switch(s.labelpos.toLowerCase()){
            case "l":
                c = s.width / 2;
                break;
            case "r":
                c = -s.width / 2;
                break;
        }
        return c && (d += n ? c : -c), c = 0, d;
    };
}
(0, _chunkGTKDMUJJMjs.a)(tn, "sep");
function an(r, e) {
    return r.node(e).width;
}
(0, _chunkGTKDMUJJMjs.a)(an, "width");
function le(r) {
    r = X(r), fn(r), (0, _chunkBKDDFIKNMjs.v)(he(r), function(e, n) {
        r.node(n).x = e;
    });
}
(0, _chunkGTKDMUJJMjs.a)(le, "position");
function fn(r) {
    var e = S(r), n = r.graph().ranksep, o = 0;
    (0, _chunkBKDDFIKNMjs.n)(e, function(t) {
        var i = (0, _chunkBKDDFIKNMjs.F)((0, _chunkBKDDFIKNMjs.s)(t, function(a) {
            return r.node(a).height;
        }));
        (0, _chunkBKDDFIKNMjs.n)(t, function(a) {
            r.node(a).y = o + i / 2;
        }), o += i + n;
    });
}
(0, _chunkGTKDMUJJMjs.a)(fn, "positionY");
function sn(r, e) {
    var n = e && e.debugTiming ? Ir : Lr;
    n("layout", function() {
        var o = n("  buildLayoutGraph", function() {
            return En(r);
        });
        n("  runLayout", function() {
            un(o, n);
        }), n("  updateInputGraph", function() {
            dn(r, o);
        });
    });
}
(0, _chunkGTKDMUJJMjs.a)(sn, "layout");
function un(r, e) {
    e("    makeSpaceForEdgeLabels", function() {
        bn(r);
    }), e("    removeSelfEdges", function() {
        Tn(r);
    }), e("    acyclic", function() {
        Gr(r);
    }), e("    nestingGraph.run", function() {
        Kr(r);
    }), e("    rank", function() {
        cr(X(r));
    }), e("    injectEdgeLabelProxies", function() {
        yn(r);
    }), e("    removeEmptyRanks", function() {
        gr(r);
    }), e("    nestingGraph.cleanup", function() {
        Zr(r);
    }), e("    normalizeRanks", function() {
        kr(r);
    }), e("    assignRankMinMax", function() {
        xn(r);
    }), e("    removeEdgeLabelProxies", function() {
        kn(r);
    }), e("    normalize.run", function() {
        Ar(r);
    }), e("    parentDummyChains", function() {
        de(r);
    }), e("    addBorderSegments", function() {
        Tr(r);
    }), e("    order", function() {
        ue(r);
    }), e("    insertSelfEdges", function() {
        Rn(r);
    }), e("    adjustCoordinateSystem", function() {
        Sr(r);
    }), e("    position", function() {
        le(r);
    }), e("    positionSelfEdges", function() {
        Sn(r);
    }), e("    removeBorderNodes", function() {
        Cn(r);
    }), e("    normalize.undo", function() {
        Dr(r);
    }), e("    fixupEdgeLabelCoords", function() {
        In(r);
    }), e("    undoCoordinateSystem", function() {
        Mr(r);
    }), e("    translateGraph", function() {
        gn(r);
    }), e("    assignNodeIntersects", function() {
        Nn(r);
    }), e("    reversePoints", function() {
        Ln(r);
    }), e("    acyclic.undo", function() {
        Vr(r);
    });
}
(0, _chunkGTKDMUJJMjs.a)(un, "runLayout");
function dn(r, e) {
    (0, _chunkBKDDFIKNMjs.n)(r.nodes(), function(n) {
        var o = r.node(n), t = e.node(n);
        o && (o.x = t.x, o.y = t.y, e.children(n).length && (o.width = t.width, o.height = t.height));
    }), (0, _chunkBKDDFIKNMjs.n)(r.edges(), function(n) {
        var o = r.edge(n), t = e.edge(n);
        o.points = t.points, (0, _chunkBKDDFIKNMjs.x)(t, "x") && (o.x = t.x, o.y = t.y);
    }), r.graph().width = e.graph().width, r.graph().height = e.graph().height;
}
(0, _chunkGTKDMUJJMjs.a)(dn, "updateInputGraph");
var cn = [
    "nodesep",
    "edgesep",
    "ranksep",
    "marginx",
    "marginy"
], hn = {
    ranksep: 50,
    edgesep: 20,
    nodesep: 50,
    rankdir: "tb"
}, ln = [
    "acyclicer",
    "ranker",
    "rankdir",
    "align"
], pn = [
    "width",
    "height"
], mn = {
    width: 0,
    height: 0
}, vn = [
    "minlen",
    "weight",
    "width",
    "height",
    "labeloffset"
], _n = {
    minlen: 1,
    weight: 1,
    width: 0,
    height: 0,
    labeloffset: 10,
    labelpos: "r"
}, wn = [
    "labelpos"
];
function En(r) {
    var e = new (0, _chunk6XGRHI2AMjs.a)({
        multigraph: !0,
        compound: !0
    }), n = pr(r.graph());
    return e.setGraph((0, _chunk6BY5RJGCMjs.T)({}, hn, lr(n, cn), (0, _chunkBKDDFIKNMjs.J)(n, ln))), (0, _chunkBKDDFIKNMjs.n)(r.nodes(), function(o) {
        var t = pr(r.node(o));
        e.setNode(o, (0, _chunkBKDDFIKNMjs.i)(lr(t, pn), mn)), e.setParent(o, r.parent(o));
    }), (0, _chunkBKDDFIKNMjs.n)(r.edges(), function(o) {
        var t = pr(r.edge(o));
        e.setEdge(o, (0, _chunk6BY5RJGCMjs.T)({}, _n, lr(t, vn), (0, _chunkBKDDFIKNMjs.J)(t, wn)));
    }), e;
}
(0, _chunkGTKDMUJJMjs.a)(En, "buildLayoutGraph");
function bn(r) {
    var e = r.graph();
    e.ranksep /= 2, (0, _chunkBKDDFIKNMjs.n)(r.edges(), function(n) {
        var o = r.edge(n);
        o.minlen *= 2, o.labelpos.toLowerCase() !== "c" && (e.rankdir === "TB" || e.rankdir === "BT" ? o.width += o.labeloffset : o.height += o.labeloffset);
    });
}
(0, _chunkGTKDMUJJMjs.a)(bn, "makeSpaceForEdgeLabels");
function yn(r) {
    (0, _chunkBKDDFIKNMjs.n)(r.edges(), function(e) {
        var n = r.edge(e);
        if (n.width && n.height) {
            var o = r.node(e.v), t = r.node(e.w), i = {
                rank: (t.rank - o.rank) / 2 + o.rank,
                e
            };
            N(r, "edge-proxy", i, "_ep");
        }
    });
}
(0, _chunkGTKDMUJJMjs.a)(yn, "injectEdgeLabelProxies");
function xn(r) {
    var e = 0;
    (0, _chunkBKDDFIKNMjs.n)(r.nodes(), function(n) {
        var o = r.node(n);
        o.borderTop && (o.minRank = r.node(o.borderTop).rank, o.maxRank = r.node(o.borderBottom).rank, e = (0, _chunkBKDDFIKNMjs.F)(e, o.maxRank));
    }), r.graph().maxRank = e;
}
(0, _chunkGTKDMUJJMjs.a)(xn, "assignRankMinMax");
function kn(r) {
    (0, _chunkBKDDFIKNMjs.n)(r.nodes(), function(e) {
        var n = r.node(e);
        n.dummy === "edge-proxy" && (r.edge(n.e).labelRank = n.rank, r.removeNode(e));
    });
}
(0, _chunkGTKDMUJJMjs.a)(kn, "removeEdgeLabelProxies");
function gn(r) {
    var e = Number.POSITIVE_INFINITY, n = 0, o = Number.POSITIVE_INFINITY, t = 0, i = r.graph(), a = i.marginx || 0, s = i.marginy || 0;
    function d(c) {
        var h = c.x, l = c.y, m = c.width, v = c.height;
        e = Math.min(e, h - m / 2), n = Math.max(n, h + m / 2), o = Math.min(o, l - v / 2), t = Math.max(t, l + v / 2);
    }
    (0, _chunkGTKDMUJJMjs.a)(d, "getExtremes"), (0, _chunkBKDDFIKNMjs.n)(r.nodes(), function(c) {
        d(r.node(c));
    }), (0, _chunkBKDDFIKNMjs.n)(r.edges(), function(c) {
        var h = r.edge(c);
        (0, _chunkBKDDFIKNMjs.x)(h, "x") && d(h);
    }), e -= a, o -= s, (0, _chunkBKDDFIKNMjs.n)(r.nodes(), function(c) {
        var h = r.node(c);
        h.x -= e, h.y -= o;
    }), (0, _chunkBKDDFIKNMjs.n)(r.edges(), function(c) {
        var h = r.edge(c);
        (0, _chunkBKDDFIKNMjs.n)(h.points, function(l) {
            l.x -= e, l.y -= o;
        }), (0, _chunkBKDDFIKNMjs.x)(h, "x") && (h.x -= e), (0, _chunkBKDDFIKNMjs.x)(h, "y") && (h.y -= o);
    }), i.width = n - e + a, i.height = t - o + s;
}
(0, _chunkGTKDMUJJMjs.a)(gn, "translateGraph");
function Nn(r) {
    (0, _chunkBKDDFIKNMjs.n)(r.edges(), function(e) {
        var n = r.edge(e), o = r.node(e.v), t = r.node(e.w), i, a;
        n.points ? (i = n.points[0], a = n.points[n.points.length - 1]) : (n.points = [], i = t, a = o), n.points.unshift($(o, i)), n.points.push($(t, a));
    });
}
(0, _chunkGTKDMUJJMjs.a)(Nn, "assignNodeIntersects");
function In(r) {
    (0, _chunkBKDDFIKNMjs.n)(r.edges(), function(e) {
        var n = r.edge(e);
        if ((0, _chunkBKDDFIKNMjs.x)(n, "x")) switch((n.labelpos === "l" || n.labelpos === "r") && (n.width -= n.labeloffset), n.labelpos){
            case "l":
                n.x -= n.width / 2 + n.labeloffset;
                break;
            case "r":
                n.x += n.width / 2 + n.labeloffset;
                break;
        }
    });
}
(0, _chunkGTKDMUJJMjs.a)(In, "fixupEdgeLabelCoords");
function Ln(r) {
    (0, _chunkBKDDFIKNMjs.n)(r.edges(), function(e) {
        var n = r.edge(e);
        n.reversed && n.points.reverse();
    });
}
(0, _chunkGTKDMUJJMjs.a)(Ln, "reversePointsForReversedEdges");
function Cn(r) {
    (0, _chunkBKDDFIKNMjs.n)(r.nodes(), function(e) {
        if (r.children(e).length) {
            var n = r.node(e), o = r.node(n.borderTop), t = r.node(n.borderBottom), i = r.node((0, _chunkBKDDFIKNMjs.k)(n.borderLeft)), a = r.node((0, _chunkBKDDFIKNMjs.k)(n.borderRight));
            n.width = Math.abs(a.x - i.x), n.height = Math.abs(t.y - o.y), n.x = i.x + n.width / 2, n.y = o.y + n.height / 2;
        }
    }), (0, _chunkBKDDFIKNMjs.n)(r.nodes(), function(e) {
        r.node(e).dummy === "border" && r.removeNode(e);
    });
}
(0, _chunkGTKDMUJJMjs.a)(Cn, "removeBorderNodes");
function Tn(r) {
    (0, _chunkBKDDFIKNMjs.n)(r.edges(), function(e) {
        if (e.v === e.w) {
            var n = r.node(e.v);
            n.selfEdges || (n.selfEdges = []), n.selfEdges.push({
                e,
                label: r.edge(e)
            }), r.removeEdge(e);
        }
    });
}
(0, _chunkGTKDMUJJMjs.a)(Tn, "removeSelfEdges");
function Rn(r) {
    var e = S(r);
    (0, _chunkBKDDFIKNMjs.n)(e, function(n) {
        var o = 0;
        (0, _chunkBKDDFIKNMjs.n)(n, function(t, i) {
            var a = r.node(t);
            a.order = i + o, (0, _chunkBKDDFIKNMjs.n)(a.selfEdges, function(s) {
                N(r, "selfedge", {
                    width: s.label.width,
                    height: s.label.height,
                    rank: a.rank,
                    order: i + ++o,
                    e: s.e,
                    label: s.label
                }, "_se");
            }), delete a.selfEdges;
        });
    });
}
(0, _chunkGTKDMUJJMjs.a)(Rn, "insertSelfEdges");
function Sn(r) {
    (0, _chunkBKDDFIKNMjs.n)(r.nodes(), function(e) {
        var n = r.node(e);
        if (n.dummy === "selfedge") {
            var o = r.node(n.e.v), t = o.x + o.width / 2, i = o.y, a = n.x - t, s = o.height / 2;
            r.setEdge(n.e, n.label), r.removeNode(e), n.label.points = [
                {
                    x: t + 2 * a / 3,
                    y: i - s
                },
                {
                    x: t + 5 * a / 6,
                    y: i - s
                },
                {
                    x: t + a,
                    y: i
                },
                {
                    x: t + 5 * a / 6,
                    y: i + s
                },
                {
                    x: t + 2 * a / 3,
                    y: i + s
                }
            ], n.label.x = n.x, n.label.y = n.y;
        }
    });
}
(0, _chunkGTKDMUJJMjs.a)(Sn, "positionSelfEdges");
function lr(r, e) {
    return (0, _chunkBKDDFIKNMjs.E)((0, _chunkBKDDFIKNMjs.J)(r, e), Number);
}
(0, _chunkGTKDMUJJMjs.a)(lr, "selectNumberAttrs");
function pr(r) {
    var e = {};
    return (0, _chunkBKDDFIKNMjs.n)(r, function(n, o) {
        e[o.toLowerCase()] = n;
    }), e;
}
(0, _chunkGTKDMUJJMjs.a)(pr, "canonicalize");

},{"./chunk-6XGRHI2A.mjs":"fUQIF","./chunk-BKDDFIKN.mjs":"hADfH","./chunk-6BY5RJGC.mjs":"bRXnR","./chunk-GTKDMUJJ.mjs":"fruhx","@parcel/transformer-js/src/esmodule-helpers.js":"1cdyk"}]},["31wPQ"], null, "parcelRequire6955", {})

//# sourceMappingURL=dagre-EVPMPUST.134b2b3d.js.map
