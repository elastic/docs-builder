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
})({"jhrFz":[function(require,module,exports,__globalThis) {
var global = arguments[3];
var HMR_HOST = null;
var HMR_PORT = 1234;
var HMR_SERVER_PORT = 1234;
var HMR_SECURE = false;
var HMR_ENV_HASH = "febfef3c5b467b16";
var HMR_USE_SSE = false;
module.bundle.HMR_BUNDLE_ID = "6abe7626378e4b07";
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

},{}],"jFNai":[function(require,module,exports,__globalThis) {
var parcelHelpers = require("@parcel/transformer-js/src/esmodule-helpers.js");
parcelHelpers.defineInteropFlag(exports);
parcelHelpers.export(exports, "diagram", ()=>diagram);
var _chunkVNRP4OIWMjs = require("./chunk-VNRP4OIW.mjs");
var _chunkYP6PVJQ3Mjs = require("./chunk-YP6PVJQ3.mjs");
var _chunkI7ZFS43CMjs = require("./chunk-I7ZFS43C.mjs");
var _chunkGKOISANMMjs = require("./chunk-GKOISANM.mjs");
var _chunkDD37ZF33Mjs = require("./chunk-DD37ZF33.mjs");
var _chunkHD3LK5B5Mjs = require("./chunk-HD3LK5B5.mjs");
var _chunkDLQEHMXDMjs = require("./chunk-DLQEHMXD.mjs");
// src/diagrams/xychart/parser/xychart.jison
var parser = function() {
    var o = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(k, v, o2, l) {
        for(o2 = o2 || {}, l = k.length; l--; o2[k[l]] = v);
        return o2;
    }, "o"), $V0 = [
        1,
        10,
        12,
        14,
        16,
        18,
        19,
        21,
        23
    ], $V1 = [
        2,
        6
    ], $V2 = [
        1,
        3
    ], $V3 = [
        1,
        5
    ], $V4 = [
        1,
        6
    ], $V5 = [
        1,
        7
    ], $V6 = [
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
    ], $V7 = [
        1,
        25
    ], $V8 = [
        1,
        26
    ], $V9 = [
        1,
        28
    ], $Va = [
        1,
        29
    ], $Vb = [
        1,
        30
    ], $Vc = [
        1,
        31
    ], $Vd = [
        1,
        32
    ], $Ve = [
        1,
        33
    ], $Vf = [
        1,
        34
    ], $Vg = [
        1,
        35
    ], $Vh = [
        1,
        36
    ], $Vi = [
        1,
        37
    ], $Vj = [
        1,
        43
    ], $Vk = [
        1,
        42
    ], $Vl = [
        1,
        47
    ], $Vm = [
        1,
        50
    ], $Vn = [
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
    ], $Vo = [
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
    ], $Vp = [
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
    ], $Vq = [
        1,
        64
    ];
    var parser2 = {
        trace: /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function trace() {}, "trace"),
        yy: {},
        symbols_: {
            "error": 2,
            "start": 3,
            "eol": 4,
            "XYCHART": 5,
            "chartConfig": 6,
            "document": 7,
            "CHART_ORIENTATION": 8,
            "statement": 9,
            "title": 10,
            "text": 11,
            "X_AXIS": 12,
            "parseXAxis": 13,
            "Y_AXIS": 14,
            "parseYAxis": 15,
            "LINE": 16,
            "plotData": 17,
            "BAR": 18,
            "acc_title": 19,
            "acc_title_value": 20,
            "acc_descr": 21,
            "acc_descr_value": 22,
            "acc_descr_multiline_value": 23,
            "SQUARE_BRACES_START": 24,
            "commaSeparatedNumbers": 25,
            "SQUARE_BRACES_END": 26,
            "NUMBER_WITH_DECIMAL": 27,
            "COMMA": 28,
            "xAxisData": 29,
            "bandData": 30,
            "ARROW_DELIMITER": 31,
            "commaSeparatedTexts": 32,
            "yAxisData": 33,
            "NEWLINE": 34,
            "SEMI": 35,
            "EOF": 36,
            "alphaNum": 37,
            "STR": 38,
            "MD_STR": 39,
            "alphaNumToken": 40,
            "AMP": 41,
            "NUM": 42,
            "ALPHA": 43,
            "PLUS": 44,
            "EQUALS": 45,
            "MULT": 46,
            "DOT": 47,
            "BRKT": 48,
            "MINUS": 49,
            "UNDERSCORE": 50,
            "$accept": 0,
            "$end": 1
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
        performAction: /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function anonymous(yytext, yyleng, yylineno, yy, yystate, $$, _$) {
            var $0 = $$.length - 1;
            switch(yystate){
                case 5:
                    yy.setOrientation($$[$0]);
                    break;
                case 9:
                    yy.setDiagramTitle($$[$0].text.trim());
                    break;
                case 12:
                    yy.setLineData({
                        text: "",
                        type: "text"
                    }, $$[$0]);
                    break;
                case 13:
                    yy.setLineData($$[$0 - 1], $$[$0]);
                    break;
                case 14:
                    yy.setBarData({
                        text: "",
                        type: "text"
                    }, $$[$0]);
                    break;
                case 15:
                    yy.setBarData($$[$0 - 1], $$[$0]);
                    break;
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
                    this.$ = $$[$0 - 1];
                    break;
                case 20:
                    this.$ = [
                        Number($$[$0 - 2]),
                        ...$$[$0]
                    ];
                    break;
                case 21:
                    this.$ = [
                        Number($$[$0])
                    ];
                    break;
                case 22:
                    yy.setXAxisTitle($$[$0]);
                    break;
                case 23:
                    yy.setXAxisTitle($$[$0 - 1]);
                    break;
                case 24:
                    yy.setXAxisTitle({
                        type: "text",
                        text: ""
                    });
                    break;
                case 25:
                    yy.setXAxisBand($$[$0]);
                    break;
                case 26:
                    yy.setXAxisRangeData(Number($$[$0 - 2]), Number($$[$0]));
                    break;
                case 27:
                    this.$ = $$[$0 - 1];
                    break;
                case 28:
                    this.$ = [
                        $$[$0 - 2],
                        ...$$[$0]
                    ];
                    break;
                case 29:
                    this.$ = [
                        $$[$0]
                    ];
                    break;
                case 30:
                    yy.setYAxisTitle($$[$0]);
                    break;
                case 31:
                    yy.setYAxisTitle($$[$0 - 1]);
                    break;
                case 32:
                    yy.setYAxisTitle({
                        type: "text",
                        text: ""
                    });
                    break;
                case 33:
                    yy.setYAxisRangeData(Number($$[$0 - 2]), Number($$[$0]));
                    break;
                case 37:
                    this.$ = {
                        text: $$[$0],
                        type: "text"
                    };
                    break;
                case 38:
                    this.$ = {
                        text: $$[$0],
                        type: "text"
                    };
                    break;
                case 39:
                    this.$ = {
                        text: $$[$0],
                        type: "markdown"
                    };
                    break;
                case 40:
                    this.$ = $$[$0];
                    break;
                case 41:
                    this.$ = $$[$0 - 1] + "" + $$[$0];
                    break;
            }
        }, "anonymous"),
        table: [
            o($V0, $V1, {
                3: 1,
                4: 2,
                7: 4,
                5: $V2,
                34: $V3,
                35: $V4,
                36: $V5
            }),
            {
                1: [
                    3
                ]
            },
            o($V0, $V1, {
                4: 2,
                7: 4,
                3: 8,
                5: $V2,
                34: $V3,
                35: $V4,
                36: $V5
            }),
            o($V0, $V1, {
                4: 2,
                7: 4,
                6: 9,
                3: 10,
                5: $V2,
                8: [
                    1,
                    11
                ],
                34: $V3,
                35: $V4,
                36: $V5
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
            o($V6, [
                2,
                34
            ]),
            o($V6, [
                2,
                35
            ]),
            o($V6, [
                2,
                36
            ]),
            {
                1: [
                    2,
                    1
                ]
            },
            o($V0, $V1, {
                4: 2,
                7: 4,
                3: 21,
                5: $V2,
                34: $V3,
                35: $V4,
                36: $V5
            }),
            {
                1: [
                    2,
                    3
                ]
            },
            o($V6, [
                2,
                5
            ]),
            o($V0, [
                2,
                7
            ], {
                4: 22,
                34: $V3,
                35: $V4,
                36: $V5
            }),
            {
                11: 23,
                37: 24,
                38: $V7,
                39: $V8,
                40: 27,
                41: $V9,
                42: $Va,
                43: $Vb,
                44: $Vc,
                45: $Vd,
                46: $Ve,
                47: $Vf,
                48: $Vg,
                49: $Vh,
                50: $Vi
            },
            {
                11: 39,
                13: 38,
                24: $Vj,
                27: $Vk,
                29: 40,
                30: 41,
                37: 24,
                38: $V7,
                39: $V8,
                40: 27,
                41: $V9,
                42: $Va,
                43: $Vb,
                44: $Vc,
                45: $Vd,
                46: $Ve,
                47: $Vf,
                48: $Vg,
                49: $Vh,
                50: $Vi
            },
            {
                11: 45,
                15: 44,
                27: $Vl,
                33: 46,
                37: 24,
                38: $V7,
                39: $V8,
                40: 27,
                41: $V9,
                42: $Va,
                43: $Vb,
                44: $Vc,
                45: $Vd,
                46: $Ve,
                47: $Vf,
                48: $Vg,
                49: $Vh,
                50: $Vi
            },
            {
                11: 49,
                17: 48,
                24: $Vm,
                37: 24,
                38: $V7,
                39: $V8,
                40: 27,
                41: $V9,
                42: $Va,
                43: $Vb,
                44: $Vc,
                45: $Vd,
                46: $Ve,
                47: $Vf,
                48: $Vg,
                49: $Vh,
                50: $Vi
            },
            {
                11: 52,
                17: 51,
                24: $Vm,
                37: 24,
                38: $V7,
                39: $V8,
                40: 27,
                41: $V9,
                42: $Va,
                43: $Vb,
                44: $Vc,
                45: $Vd,
                46: $Ve,
                47: $Vf,
                48: $Vg,
                49: $Vh,
                50: $Vi
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
            o($Vn, [
                2,
                18
            ]),
            {
                1: [
                    2,
                    2
                ]
            },
            o($Vn, [
                2,
                8
            ]),
            o($Vn, [
                2,
                9
            ]),
            o($Vo, [
                2,
                37
            ], {
                40: 55,
                41: $V9,
                42: $Va,
                43: $Vb,
                44: $Vc,
                45: $Vd,
                46: $Ve,
                47: $Vf,
                48: $Vg,
                49: $Vh,
                50: $Vi
            }),
            o($Vo, [
                2,
                38
            ]),
            o($Vo, [
                2,
                39
            ]),
            o($Vp, [
                2,
                40
            ]),
            o($Vp, [
                2,
                42
            ]),
            o($Vp, [
                2,
                43
            ]),
            o($Vp, [
                2,
                44
            ]),
            o($Vp, [
                2,
                45
            ]),
            o($Vp, [
                2,
                46
            ]),
            o($Vp, [
                2,
                47
            ]),
            o($Vp, [
                2,
                48
            ]),
            o($Vp, [
                2,
                49
            ]),
            o($Vp, [
                2,
                50
            ]),
            o($Vp, [
                2,
                51
            ]),
            o($Vn, [
                2,
                10
            ]),
            o($Vn, [
                2,
                22
            ], {
                30: 41,
                29: 56,
                24: $Vj,
                27: $Vk
            }),
            o($Vn, [
                2,
                24
            ]),
            o($Vn, [
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
                38: $V7,
                39: $V8,
                40: 27,
                41: $V9,
                42: $Va,
                43: $Vb,
                44: $Vc,
                45: $Vd,
                46: $Ve,
                47: $Vf,
                48: $Vg,
                49: $Vh,
                50: $Vi
            },
            o($Vn, [
                2,
                11
            ]),
            o($Vn, [
                2,
                30
            ], {
                33: 60,
                27: $Vl
            }),
            o($Vn, [
                2,
                32
            ]),
            {
                31: [
                    1,
                    61
                ]
            },
            o($Vn, [
                2,
                12
            ]),
            {
                17: 62,
                24: $Vm
            },
            {
                25: 63,
                27: $Vq
            },
            o($Vn, [
                2,
                14
            ]),
            {
                17: 65,
                24: $Vm
            },
            o($Vn, [
                2,
                16
            ]),
            o($Vn, [
                2,
                17
            ]),
            o($Vp, [
                2,
                41
            ]),
            o($Vn, [
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
            o($Vn, [
                2,
                31
            ]),
            {
                27: [
                    1,
                    69
                ]
            },
            o($Vn, [
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
            o($Vn, [
                2,
                15
            ]),
            o($Vn, [
                2,
                26
            ]),
            o($Vn, [
                2,
                27
            ]),
            {
                11: 59,
                32: 72,
                37: 24,
                38: $V7,
                39: $V8,
                40: 27,
                41: $V9,
                42: $Va,
                43: $Vb,
                44: $Vc,
                45: $Vd,
                46: $Ve,
                47: $Vf,
                48: $Vg,
                49: $Vh,
                50: $Vi
            },
            o($Vn, [
                2,
                33
            ]),
            o($Vn, [
                2,
                19
            ]),
            {
                25: 73,
                27: $Vq
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
                        this.popState();
                        return 34;
                    case 3:
                        this.popState();
                        return 34;
                    case 4:
                        return 34;
                    case 5:
                        break;
                    case 6:
                        return 10;
                    case 7:
                        this.pushState("acc_title");
                        return 19;
                    case 8:
                        this.popState();
                        return "acc_title_value";
                    case 9:
                        this.pushState("acc_descr");
                        return 21;
                    case 10:
                        this.popState();
                        return "acc_descr_value";
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
                        this.pushState("axis_data");
                        return "X_AXIS";
                    case 17:
                        this.pushState("axis_data");
                        return "Y_AXIS";
                    case 18:
                        this.pushState("axis_band_data");
                        return 24;
                    case 19:
                        return 31;
                    case 20:
                        this.pushState("data");
                        return 16;
                    case 21:
                        this.pushState("data");
                        return 18;
                    case 22:
                        this.pushState("data_inner");
                        return 24;
                    case 23:
                        return 27;
                    case 24:
                        this.popState();
                        return 26;
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
                "data_inner": {
                    "rules": [
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
                    "inclusive": true
                },
                "data": {
                    "rules": [
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
                    "inclusive": true
                },
                "axis_band_data": {
                    "rules": [
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
                    "inclusive": true
                },
                "axis_data": {
                    "rules": [
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
                    "inclusive": true
                },
                "acc_descr_multiline": {
                    "rules": [
                        12,
                        13
                    ],
                    "inclusive": false
                },
                "acc_descr": {
                    "rules": [
                        10
                    ],
                    "inclusive": false
                },
                "acc_title": {
                    "rules": [
                        8
                    ],
                    "inclusive": false
                },
                "title": {
                    "rules": [],
                    "inclusive": false
                },
                "md_string": {
                    "rules": [],
                    "inclusive": false
                },
                "string": {
                    "rules": [
                        27,
                        28
                    ],
                    "inclusive": false
                },
                "INITIAL": {
                    "rules": [
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
var xychart_default = parser;
// src/diagrams/xychart/chartBuilder/interfaces.ts
function isBarPlot(data) {
    return data.type === "bar";
}
(0, _chunkDLQEHMXDMjs.__name)(isBarPlot, "isBarPlot");
function isBandAxisData(data) {
    return data.type === "band";
}
(0, _chunkDLQEHMXDMjs.__name)(isBandAxisData, "isBandAxisData");
function isLinearAxisData(data) {
    return data.type === "linear";
}
(0, _chunkDLQEHMXDMjs.__name)(isLinearAxisData, "isLinearAxisData");
// src/diagrams/xychart/chartBuilder/textDimensionCalculator.ts
var TextDimensionCalculatorWithFont = class {
    constructor(parentGroup){
        this.parentGroup = parentGroup;
    }
    static #_ = (0, _chunkDLQEHMXDMjs.__name)(this, "TextDimensionCalculatorWithFont");
    getMaxDimension(texts, fontSize) {
        if (!this.parentGroup) return {
            width: texts.reduce((acc, cur)=>Math.max(cur.length, acc), 0) * fontSize,
            height: fontSize
        };
        const dimension = {
            width: 0,
            height: 0
        };
        const elem = this.parentGroup.append("g").attr("visibility", "hidden").attr("font-size", fontSize);
        for (const t of texts){
            const bbox = (0, _chunkYP6PVJQ3Mjs.computeDimensionOfText)(elem, 1, t);
            const width = bbox ? bbox.width : t.length * fontSize;
            const height = bbox ? bbox.height : fontSize;
            dimension.width = Math.max(dimension.width, width);
            dimension.height = Math.max(dimension.height, height);
        }
        elem.remove();
        return dimension;
    }
};
// src/diagrams/xychart/chartBuilder/components/axis/baseAxis.ts
var BAR_WIDTH_TO_TICK_WIDTH_RATIO = 0.7;
var MAX_OUTER_PADDING_PERCENT_FOR_WRT_LABEL = 0.2;
var BaseAxis = class {
    constructor(axisConfig, title, textDimensionCalculator, axisThemeConfig){
        this.axisConfig = axisConfig;
        this.title = title;
        this.textDimensionCalculator = textDimensionCalculator;
        this.axisThemeConfig = axisThemeConfig;
        this.boundingRect = {
            x: 0,
            y: 0,
            width: 0,
            height: 0
        };
        this.axisPosition = "left";
        this.showTitle = false;
        this.showLabel = false;
        this.showTick = false;
        this.showAxisLine = false;
        this.outerPadding = 0;
        this.titleTextHeight = 0;
        this.labelTextHeight = 0;
        this.range = [
            0,
            10
        ];
        this.boundingRect = {
            x: 0,
            y: 0,
            width: 0,
            height: 0
        };
        this.axisPosition = "left";
    }
    static #_ = (0, _chunkDLQEHMXDMjs.__name)(this, "BaseAxis");
    setRange(range) {
        this.range = range;
        if (this.axisPosition === "left" || this.axisPosition === "right") this.boundingRect.height = range[1] - range[0];
        else this.boundingRect.width = range[1] - range[0];
        this.recalculateScale();
    }
    getRange() {
        return [
            this.range[0] + this.outerPadding,
            this.range[1] - this.outerPadding
        ];
    }
    setAxisPosition(axisPosition) {
        this.axisPosition = axisPosition;
        this.setRange(this.range);
    }
    getTickDistance() {
        const range = this.getRange();
        return Math.abs(range[0] - range[1]) / this.getTickValues().length;
    }
    getAxisOuterPadding() {
        return this.outerPadding;
    }
    getLabelDimension() {
        return this.textDimensionCalculator.getMaxDimension(this.getTickValues().map((tick)=>tick.toString()), this.axisConfig.labelFontSize);
    }
    recalculateOuterPaddingToDrawBar() {
        if (BAR_WIDTH_TO_TICK_WIDTH_RATIO * this.getTickDistance() > this.outerPadding * 2) this.outerPadding = Math.floor(BAR_WIDTH_TO_TICK_WIDTH_RATIO * this.getTickDistance() / 2);
        this.recalculateScale();
    }
    calculateSpaceIfDrawnHorizontally(availableSpace) {
        let availableHeight = availableSpace.height;
        if (this.axisConfig.showAxisLine && availableHeight > this.axisConfig.axisLineWidth) {
            availableHeight -= this.axisConfig.axisLineWidth;
            this.showAxisLine = true;
        }
        if (this.axisConfig.showLabel) {
            const spaceRequired = this.getLabelDimension();
            const maxPadding = MAX_OUTER_PADDING_PERCENT_FOR_WRT_LABEL * availableSpace.width;
            this.outerPadding = Math.min(spaceRequired.width / 2, maxPadding);
            const heightRequired = spaceRequired.height + this.axisConfig.labelPadding * 2;
            this.labelTextHeight = spaceRequired.height;
            if (heightRequired <= availableHeight) {
                availableHeight -= heightRequired;
                this.showLabel = true;
            }
        }
        if (this.axisConfig.showTick && availableHeight >= this.axisConfig.tickLength) {
            this.showTick = true;
            availableHeight -= this.axisConfig.tickLength;
        }
        if (this.axisConfig.showTitle && this.title) {
            const spaceRequired = this.textDimensionCalculator.getMaxDimension([
                this.title
            ], this.axisConfig.titleFontSize);
            const heightRequired = spaceRequired.height + this.axisConfig.titlePadding * 2;
            this.titleTextHeight = spaceRequired.height;
            if (heightRequired <= availableHeight) {
                availableHeight -= heightRequired;
                this.showTitle = true;
            }
        }
        this.boundingRect.width = availableSpace.width;
        this.boundingRect.height = availableSpace.height - availableHeight;
    }
    calculateSpaceIfDrawnVertical(availableSpace) {
        let availableWidth = availableSpace.width;
        if (this.axisConfig.showAxisLine && availableWidth > this.axisConfig.axisLineWidth) {
            availableWidth -= this.axisConfig.axisLineWidth;
            this.showAxisLine = true;
        }
        if (this.axisConfig.showLabel) {
            const spaceRequired = this.getLabelDimension();
            const maxPadding = MAX_OUTER_PADDING_PERCENT_FOR_WRT_LABEL * availableSpace.height;
            this.outerPadding = Math.min(spaceRequired.height / 2, maxPadding);
            const widthRequired = spaceRequired.width + this.axisConfig.labelPadding * 2;
            if (widthRequired <= availableWidth) {
                availableWidth -= widthRequired;
                this.showLabel = true;
            }
        }
        if (this.axisConfig.showTick && availableWidth >= this.axisConfig.tickLength) {
            this.showTick = true;
            availableWidth -= this.axisConfig.tickLength;
        }
        if (this.axisConfig.showTitle && this.title) {
            const spaceRequired = this.textDimensionCalculator.getMaxDimension([
                this.title
            ], this.axisConfig.titleFontSize);
            const widthRequired = spaceRequired.height + this.axisConfig.titlePadding * 2;
            this.titleTextHeight = spaceRequired.height;
            if (widthRequired <= availableWidth) {
                availableWidth -= widthRequired;
                this.showTitle = true;
            }
        }
        this.boundingRect.width = availableSpace.width - availableWidth;
        this.boundingRect.height = availableSpace.height;
    }
    calculateSpace(availableSpace) {
        if (this.axisPosition === "left" || this.axisPosition === "right") this.calculateSpaceIfDrawnVertical(availableSpace);
        else this.calculateSpaceIfDrawnHorizontally(availableSpace);
        this.recalculateScale();
        return {
            width: this.boundingRect.width,
            height: this.boundingRect.height
        };
    }
    setBoundingBoxXY(point) {
        this.boundingRect.x = point.x;
        this.boundingRect.y = point.y;
    }
    getDrawableElementsForLeftAxis() {
        const drawableElement = [];
        if (this.showAxisLine) {
            const x = this.boundingRect.x + this.boundingRect.width - this.axisConfig.axisLineWidth / 2;
            drawableElement.push({
                type: "path",
                groupTexts: [
                    "left-axis",
                    "axisl-line"
                ],
                data: [
                    {
                        path: `M ${x},${this.boundingRect.y} L ${x},${this.boundingRect.y + this.boundingRect.height} `,
                        strokeFill: this.axisThemeConfig.axisLineColor,
                        strokeWidth: this.axisConfig.axisLineWidth
                    }
                ]
            });
        }
        if (this.showLabel) drawableElement.push({
            type: "text",
            groupTexts: [
                "left-axis",
                "label"
            ],
            data: this.getTickValues().map((tick)=>({
                    text: tick.toString(),
                    x: this.boundingRect.x + this.boundingRect.width - (this.showLabel ? this.axisConfig.labelPadding : 0) - (this.showTick ? this.axisConfig.tickLength : 0) - (this.showAxisLine ? this.axisConfig.axisLineWidth : 0),
                    y: this.getScaleValue(tick),
                    fill: this.axisThemeConfig.labelColor,
                    fontSize: this.axisConfig.labelFontSize,
                    rotation: 0,
                    verticalPos: "middle",
                    horizontalPos: "right"
                }))
        });
        if (this.showTick) {
            const x = this.boundingRect.x + this.boundingRect.width - (this.showAxisLine ? this.axisConfig.axisLineWidth : 0);
            drawableElement.push({
                type: "path",
                groupTexts: [
                    "left-axis",
                    "ticks"
                ],
                data: this.getTickValues().map((tick)=>({
                        path: `M ${x},${this.getScaleValue(tick)} L ${x - this.axisConfig.tickLength},${this.getScaleValue(tick)}`,
                        strokeFill: this.axisThemeConfig.tickColor,
                        strokeWidth: this.axisConfig.tickWidth
                    }))
            });
        }
        if (this.showTitle) drawableElement.push({
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
        });
        return drawableElement;
    }
    getDrawableElementsForBottomAxis() {
        const drawableElement = [];
        if (this.showAxisLine) {
            const y = this.boundingRect.y + this.axisConfig.axisLineWidth / 2;
            drawableElement.push({
                type: "path",
                groupTexts: [
                    "bottom-axis",
                    "axis-line"
                ],
                data: [
                    {
                        path: `M ${this.boundingRect.x},${y} L ${this.boundingRect.x + this.boundingRect.width},${y}`,
                        strokeFill: this.axisThemeConfig.axisLineColor,
                        strokeWidth: this.axisConfig.axisLineWidth
                    }
                ]
            });
        }
        if (this.showLabel) drawableElement.push({
            type: "text",
            groupTexts: [
                "bottom-axis",
                "label"
            ],
            data: this.getTickValues().map((tick)=>({
                    text: tick.toString(),
                    x: this.getScaleValue(tick),
                    y: this.boundingRect.y + this.axisConfig.labelPadding + (this.showTick ? this.axisConfig.tickLength : 0) + (this.showAxisLine ? this.axisConfig.axisLineWidth : 0),
                    fill: this.axisThemeConfig.labelColor,
                    fontSize: this.axisConfig.labelFontSize,
                    rotation: 0,
                    verticalPos: "top",
                    horizontalPos: "center"
                }))
        });
        if (this.showTick) {
            const y = this.boundingRect.y + (this.showAxisLine ? this.axisConfig.axisLineWidth : 0);
            drawableElement.push({
                type: "path",
                groupTexts: [
                    "bottom-axis",
                    "ticks"
                ],
                data: this.getTickValues().map((tick)=>({
                        path: `M ${this.getScaleValue(tick)},${y} L ${this.getScaleValue(tick)},${y + this.axisConfig.tickLength}`,
                        strokeFill: this.axisThemeConfig.tickColor,
                        strokeWidth: this.axisConfig.tickWidth
                    }))
            });
        }
        if (this.showTitle) drawableElement.push({
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
        });
        return drawableElement;
    }
    getDrawableElementsForTopAxis() {
        const drawableElement = [];
        if (this.showAxisLine) {
            const y = this.boundingRect.y + this.boundingRect.height - this.axisConfig.axisLineWidth / 2;
            drawableElement.push({
                type: "path",
                groupTexts: [
                    "top-axis",
                    "axis-line"
                ],
                data: [
                    {
                        path: `M ${this.boundingRect.x},${y} L ${this.boundingRect.x + this.boundingRect.width},${y}`,
                        strokeFill: this.axisThemeConfig.axisLineColor,
                        strokeWidth: this.axisConfig.axisLineWidth
                    }
                ]
            });
        }
        if (this.showLabel) drawableElement.push({
            type: "text",
            groupTexts: [
                "top-axis",
                "label"
            ],
            data: this.getTickValues().map((tick)=>({
                    text: tick.toString(),
                    x: this.getScaleValue(tick),
                    y: this.boundingRect.y + (this.showTitle ? this.titleTextHeight + this.axisConfig.titlePadding * 2 : 0) + this.axisConfig.labelPadding,
                    fill: this.axisThemeConfig.labelColor,
                    fontSize: this.axisConfig.labelFontSize,
                    rotation: 0,
                    verticalPos: "top",
                    horizontalPos: "center"
                }))
        });
        if (this.showTick) {
            const y = this.boundingRect.y;
            drawableElement.push({
                type: "path",
                groupTexts: [
                    "top-axis",
                    "ticks"
                ],
                data: this.getTickValues().map((tick)=>({
                        path: `M ${this.getScaleValue(tick)},${y + this.boundingRect.height - (this.showAxisLine ? this.axisConfig.axisLineWidth : 0)} L ${this.getScaleValue(tick)},${y + this.boundingRect.height - this.axisConfig.tickLength - (this.showAxisLine ? this.axisConfig.axisLineWidth : 0)}`,
                        strokeFill: this.axisThemeConfig.tickColor,
                        strokeWidth: this.axisConfig.tickWidth
                    }))
            });
        }
        if (this.showTitle) drawableElement.push({
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
        });
        return drawableElement;
    }
    getDrawableElements() {
        if (this.axisPosition === "left") return this.getDrawableElementsForLeftAxis();
        if (this.axisPosition === "right") throw Error("Drawing of right axis is not implemented");
        if (this.axisPosition === "bottom") return this.getDrawableElementsForBottomAxis();
        if (this.axisPosition === "top") return this.getDrawableElementsForTopAxis();
        return [];
    }
};
// src/diagrams/xychart/chartBuilder/components/axis/bandAxis.ts
var BandAxis = class extends BaseAxis {
    static #_ = (0, _chunkDLQEHMXDMjs.__name)(this, "BandAxis");
    constructor(axisConfig, axisThemeConfig, categories, title, textDimensionCalculator){
        super(axisConfig, title, textDimensionCalculator, axisThemeConfig);
        this.categories = categories;
        this.scale = (0, _chunkDD37ZF33Mjs.band)().domain(this.categories).range(this.getRange());
    }
    setRange(range) {
        super.setRange(range);
    }
    recalculateScale() {
        this.scale = (0, _chunkDD37ZF33Mjs.band)().domain(this.categories).range(this.getRange()).paddingInner(1).paddingOuter(0).align(0.5);
        (0, _chunkDD37ZF33Mjs.log).trace("BandAxis axis final categories, range: ", this.categories, this.getRange());
    }
    getTickValues() {
        return this.categories;
    }
    getScaleValue(value) {
        return this.scale(value) ?? this.getRange()[0];
    }
};
// src/diagrams/xychart/chartBuilder/components/axis/linearAxis.ts
var LinearAxis = class extends BaseAxis {
    static #_ = (0, _chunkDLQEHMXDMjs.__name)(this, "LinearAxis");
    constructor(axisConfig, axisThemeConfig, domain, title, textDimensionCalculator){
        super(axisConfig, title, textDimensionCalculator, axisThemeConfig);
        this.domain = domain;
        this.scale = (0, _chunkDD37ZF33Mjs.linear)().domain(this.domain).range(this.getRange());
    }
    getTickValues() {
        return this.scale.ticks();
    }
    recalculateScale() {
        const domain = [
            ...this.domain
        ];
        if (this.axisPosition === "left") domain.reverse();
        this.scale = (0, _chunkDD37ZF33Mjs.linear)().domain(domain).range(this.getRange());
    }
    getScaleValue(value) {
        return this.scale(value);
    }
};
// src/diagrams/xychart/chartBuilder/components/axis/index.ts
function getAxis(data, axisConfig, axisThemeConfig, tmpSVGGroup2) {
    const textDimensionCalculator = new TextDimensionCalculatorWithFont(tmpSVGGroup2);
    if (isBandAxisData(data)) return new BandAxis(axisConfig, axisThemeConfig, data.categories, data.title, textDimensionCalculator);
    return new LinearAxis(axisConfig, axisThemeConfig, [
        data.min,
        data.max
    ], data.title, textDimensionCalculator);
}
(0, _chunkDLQEHMXDMjs.__name)(getAxis, "getAxis");
// src/diagrams/xychart/chartBuilder/components/chartTitle.ts
var ChartTitle = class {
    constructor(textDimensionCalculator, chartConfig, chartData, chartThemeConfig){
        this.textDimensionCalculator = textDimensionCalculator;
        this.chartConfig = chartConfig;
        this.chartData = chartData;
        this.chartThemeConfig = chartThemeConfig;
        this.boundingRect = {
            x: 0,
            y: 0,
            width: 0,
            height: 0
        };
        this.showChartTitle = false;
    }
    static #_ = (0, _chunkDLQEHMXDMjs.__name)(this, "ChartTitle");
    setBoundingBoxXY(point) {
        this.boundingRect.x = point.x;
        this.boundingRect.y = point.y;
    }
    calculateSpace(availableSpace) {
        const titleDimension = this.textDimensionCalculator.getMaxDimension([
            this.chartData.title
        ], this.chartConfig.titleFontSize);
        const widthRequired = Math.max(titleDimension.width, availableSpace.width);
        const heightRequired = titleDimension.height + 2 * this.chartConfig.titlePadding;
        if (titleDimension.width <= widthRequired && titleDimension.height <= heightRequired && this.chartConfig.showTitle && this.chartData.title) {
            this.boundingRect.width = widthRequired;
            this.boundingRect.height = heightRequired;
            this.showChartTitle = true;
        }
        return {
            width: this.boundingRect.width,
            height: this.boundingRect.height
        };
    }
    getDrawableElements() {
        const drawableElem = [];
        if (this.showChartTitle) drawableElem.push({
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
        });
        return drawableElem;
    }
};
function getChartTitleComponent(chartConfig, chartData, chartThemeConfig, tmpSVGGroup2) {
    const textDimensionCalculator = new TextDimensionCalculatorWithFont(tmpSVGGroup2);
    return new ChartTitle(textDimensionCalculator, chartConfig, chartData, chartThemeConfig);
}
(0, _chunkDLQEHMXDMjs.__name)(getChartTitleComponent, "getChartTitleComponent");
// src/diagrams/xychart/chartBuilder/components/plot/linePlot.ts
var LinePlot = class {
    constructor(plotData, xAxis, yAxis, orientation, plotIndex2){
        this.plotData = plotData;
        this.xAxis = xAxis;
        this.yAxis = yAxis;
        this.orientation = orientation;
        this.plotIndex = plotIndex2;
    }
    static #_ = (0, _chunkDLQEHMXDMjs.__name)(this, "LinePlot");
    getDrawableElement() {
        const finalData = this.plotData.data.map((d)=>[
                this.xAxis.getScaleValue(d[0]),
                this.yAxis.getScaleValue(d[1])
            ]);
        let path;
        if (this.orientation === "horizontal") path = (0, _chunkDD37ZF33Mjs.line_default)().y((d)=>d[0]).x((d)=>d[1])(finalData);
        else path = (0, _chunkDD37ZF33Mjs.line_default)().x((d)=>d[0]).y((d)=>d[1])(finalData);
        if (!path) return [];
        return [
            {
                groupTexts: [
                    "plot",
                    `line-plot-${this.plotIndex}`
                ],
                type: "path",
                data: [
                    {
                        path,
                        strokeFill: this.plotData.strokeFill,
                        strokeWidth: this.plotData.strokeWidth
                    }
                ]
            }
        ];
    }
};
// src/diagrams/xychart/chartBuilder/components/plot/barPlot.ts
var BarPlot = class {
    constructor(barData, boundingRect, xAxis, yAxis, orientation, plotIndex2){
        this.barData = barData;
        this.boundingRect = boundingRect;
        this.xAxis = xAxis;
        this.yAxis = yAxis;
        this.orientation = orientation;
        this.plotIndex = plotIndex2;
    }
    static #_ = (0, _chunkDLQEHMXDMjs.__name)(this, "BarPlot");
    getDrawableElement() {
        const finalData = this.barData.data.map((d)=>[
                this.xAxis.getScaleValue(d[0]),
                this.yAxis.getScaleValue(d[1])
            ]);
        const barPaddingPercent = 0.05;
        const barWidth = Math.min(this.xAxis.getAxisOuterPadding() * 2, this.xAxis.getTickDistance()) * (1 - barPaddingPercent);
        const barWidthHalf = barWidth / 2;
        if (this.orientation === "horizontal") return [
            {
                groupTexts: [
                    "plot",
                    `bar-plot-${this.plotIndex}`
                ],
                type: "rect",
                data: finalData.map((data)=>({
                        x: this.boundingRect.x,
                        y: data[0] - barWidthHalf,
                        height: barWidth,
                        width: data[1] - this.boundingRect.x,
                        fill: this.barData.fill,
                        strokeWidth: 0,
                        strokeFill: this.barData.fill
                    }))
            }
        ];
        return [
            {
                groupTexts: [
                    "plot",
                    `bar-plot-${this.plotIndex}`
                ],
                type: "rect",
                data: finalData.map((data)=>({
                        x: data[0] - barWidthHalf,
                        y: data[1],
                        width: barWidth,
                        height: this.boundingRect.y + this.boundingRect.height - data[1],
                        fill: this.barData.fill,
                        strokeWidth: 0,
                        strokeFill: this.barData.fill
                    }))
            }
        ];
    }
};
// src/diagrams/xychart/chartBuilder/components/plot/index.ts
var BasePlot = class {
    constructor(chartConfig, chartData, chartThemeConfig){
        this.chartConfig = chartConfig;
        this.chartData = chartData;
        this.chartThemeConfig = chartThemeConfig;
        this.boundingRect = {
            x: 0,
            y: 0,
            width: 0,
            height: 0
        };
    }
    static #_ = (0, _chunkDLQEHMXDMjs.__name)(this, "BasePlot");
    setAxes(xAxis, yAxis) {
        this.xAxis = xAxis;
        this.yAxis = yAxis;
    }
    setBoundingBoxXY(point) {
        this.boundingRect.x = point.x;
        this.boundingRect.y = point.y;
    }
    calculateSpace(availableSpace) {
        this.boundingRect.width = availableSpace.width;
        this.boundingRect.height = availableSpace.height;
        return {
            width: this.boundingRect.width,
            height: this.boundingRect.height
        };
    }
    getDrawableElements() {
        if (!(this.xAxis && this.yAxis)) throw Error("Axes must be passed to render Plots");
        const drawableElem = [];
        for (const [i, plot] of this.chartData.plots.entries())switch(plot.type){
            case "line":
                {
                    const linePlot = new LinePlot(plot, this.xAxis, this.yAxis, this.chartConfig.chartOrientation, i);
                    drawableElem.push(...linePlot.getDrawableElement());
                }
                break;
            case "bar":
                {
                    const barPlot = new BarPlot(plot, this.boundingRect, this.xAxis, this.yAxis, this.chartConfig.chartOrientation, i);
                    drawableElem.push(...barPlot.getDrawableElement());
                }
                break;
        }
        return drawableElem;
    }
};
function getPlotComponent(chartConfig, chartData, chartThemeConfig) {
    return new BasePlot(chartConfig, chartData, chartThemeConfig);
}
(0, _chunkDLQEHMXDMjs.__name)(getPlotComponent, "getPlotComponent");
// src/diagrams/xychart/chartBuilder/orchestrator.ts
var Orchestrator = class {
    constructor(chartConfig, chartData, chartThemeConfig, tmpSVGGroup2){
        this.chartConfig = chartConfig;
        this.chartData = chartData;
        this.componentStore = {
            title: getChartTitleComponent(chartConfig, chartData, chartThemeConfig, tmpSVGGroup2),
            plot: getPlotComponent(chartConfig, chartData, chartThemeConfig),
            xAxis: getAxis(chartData.xAxis, chartConfig.xAxis, {
                titleColor: chartThemeConfig.xAxisTitleColor,
                labelColor: chartThemeConfig.xAxisLabelColor,
                tickColor: chartThemeConfig.xAxisTickColor,
                axisLineColor: chartThemeConfig.xAxisLineColor
            }, tmpSVGGroup2),
            yAxis: getAxis(chartData.yAxis, chartConfig.yAxis, {
                titleColor: chartThemeConfig.yAxisTitleColor,
                labelColor: chartThemeConfig.yAxisLabelColor,
                tickColor: chartThemeConfig.yAxisTickColor,
                axisLineColor: chartThemeConfig.yAxisLineColor
            }, tmpSVGGroup2)
        };
    }
    static #_ = (0, _chunkDLQEHMXDMjs.__name)(this, "Orchestrator");
    calculateVerticalSpace() {
        let availableWidth = this.chartConfig.width;
        let availableHeight = this.chartConfig.height;
        let plotX = 0;
        let plotY = 0;
        let chartWidth = Math.floor(availableWidth * this.chartConfig.plotReservedSpacePercent / 100);
        let chartHeight = Math.floor(availableHeight * this.chartConfig.plotReservedSpacePercent / 100);
        let spaceUsed = this.componentStore.plot.calculateSpace({
            width: chartWidth,
            height: chartHeight
        });
        availableWidth -= spaceUsed.width;
        availableHeight -= spaceUsed.height;
        spaceUsed = this.componentStore.title.calculateSpace({
            width: this.chartConfig.width,
            height: availableHeight
        });
        plotY = spaceUsed.height;
        availableHeight -= spaceUsed.height;
        this.componentStore.xAxis.setAxisPosition("bottom");
        spaceUsed = this.componentStore.xAxis.calculateSpace({
            width: availableWidth,
            height: availableHeight
        });
        availableHeight -= spaceUsed.height;
        this.componentStore.yAxis.setAxisPosition("left");
        spaceUsed = this.componentStore.yAxis.calculateSpace({
            width: availableWidth,
            height: availableHeight
        });
        plotX = spaceUsed.width;
        availableWidth -= spaceUsed.width;
        if (availableWidth > 0) {
            chartWidth += availableWidth;
            availableWidth = 0;
        }
        if (availableHeight > 0) {
            chartHeight += availableHeight;
            availableHeight = 0;
        }
        this.componentStore.plot.calculateSpace({
            width: chartWidth,
            height: chartHeight
        });
        this.componentStore.plot.setBoundingBoxXY({
            x: plotX,
            y: plotY
        });
        this.componentStore.xAxis.setRange([
            plotX,
            plotX + chartWidth
        ]);
        this.componentStore.xAxis.setBoundingBoxXY({
            x: plotX,
            y: plotY + chartHeight
        });
        this.componentStore.yAxis.setRange([
            plotY,
            plotY + chartHeight
        ]);
        this.componentStore.yAxis.setBoundingBoxXY({
            x: 0,
            y: plotY
        });
        if (this.chartData.plots.some((p)=>isBarPlot(p))) this.componentStore.xAxis.recalculateOuterPaddingToDrawBar();
    }
    calculateHorizontalSpace() {
        let availableWidth = this.chartConfig.width;
        let availableHeight = this.chartConfig.height;
        let titleYEnd = 0;
        let plotX = 0;
        let plotY = 0;
        let chartWidth = Math.floor(availableWidth * this.chartConfig.plotReservedSpacePercent / 100);
        let chartHeight = Math.floor(availableHeight * this.chartConfig.plotReservedSpacePercent / 100);
        let spaceUsed = this.componentStore.plot.calculateSpace({
            width: chartWidth,
            height: chartHeight
        });
        availableWidth -= spaceUsed.width;
        availableHeight -= spaceUsed.height;
        spaceUsed = this.componentStore.title.calculateSpace({
            width: this.chartConfig.width,
            height: availableHeight
        });
        titleYEnd = spaceUsed.height;
        availableHeight -= spaceUsed.height;
        this.componentStore.xAxis.setAxisPosition("left");
        spaceUsed = this.componentStore.xAxis.calculateSpace({
            width: availableWidth,
            height: availableHeight
        });
        availableWidth -= spaceUsed.width;
        plotX = spaceUsed.width;
        this.componentStore.yAxis.setAxisPosition("top");
        spaceUsed = this.componentStore.yAxis.calculateSpace({
            width: availableWidth,
            height: availableHeight
        });
        availableHeight -= spaceUsed.height;
        plotY = titleYEnd + spaceUsed.height;
        if (availableWidth > 0) {
            chartWidth += availableWidth;
            availableWidth = 0;
        }
        if (availableHeight > 0) {
            chartHeight += availableHeight;
            availableHeight = 0;
        }
        this.componentStore.plot.calculateSpace({
            width: chartWidth,
            height: chartHeight
        });
        this.componentStore.plot.setBoundingBoxXY({
            x: plotX,
            y: plotY
        });
        this.componentStore.yAxis.setRange([
            plotX,
            plotX + chartWidth
        ]);
        this.componentStore.yAxis.setBoundingBoxXY({
            x: plotX,
            y: titleYEnd
        });
        this.componentStore.xAxis.setRange([
            plotY,
            plotY + chartHeight
        ]);
        this.componentStore.xAxis.setBoundingBoxXY({
            x: 0,
            y: plotY
        });
        if (this.chartData.plots.some((p)=>isBarPlot(p))) this.componentStore.xAxis.recalculateOuterPaddingToDrawBar();
    }
    calculateSpace() {
        if (this.chartConfig.chartOrientation === "horizontal") this.calculateHorizontalSpace();
        else this.calculateVerticalSpace();
    }
    getDrawableElement() {
        this.calculateSpace();
        const drawableElem = [];
        this.componentStore.plot.setAxes(this.componentStore.xAxis, this.componentStore.yAxis);
        for (const component of Object.values(this.componentStore))drawableElem.push(...component.getDrawableElements());
        return drawableElem;
    }
};
// src/diagrams/xychart/chartBuilder/index.ts
var XYChartBuilder = class {
    static #_ = (0, _chunkDLQEHMXDMjs.__name)(this, "XYChartBuilder");
    static build(config, chartData, chartThemeConfig, tmpSVGGroup2) {
        const orchestrator = new Orchestrator(config, chartData, chartThemeConfig, tmpSVGGroup2);
        return orchestrator.getDrawableElement();
    }
};
// src/diagrams/xychart/xychartDb.ts
var plotIndex = 0;
var tmpSVGGroup;
var xyChartConfig = getChartDefaultConfig();
var xyChartThemeConfig = getChartDefaultThemeConfig();
var xyChartData = getChartDefaultData();
var plotColorPalette = xyChartThemeConfig.plotColorPalette.split(",").map((color)=>color.trim());
var hasSetXAxis = false;
var hasSetYAxis = false;
function getChartDefaultThemeConfig() {
    const defaultThemeVariables = (0, _chunkDD37ZF33Mjs.getThemeVariables)();
    const config = (0, _chunkDD37ZF33Mjs.getConfig)();
    return (0, _chunkI7ZFS43CMjs.cleanAndMerge)(defaultThemeVariables.xyChart, config.themeVariables.xyChart);
}
(0, _chunkDLQEHMXDMjs.__name)(getChartDefaultThemeConfig, "getChartDefaultThemeConfig");
function getChartDefaultConfig() {
    const config = (0, _chunkDD37ZF33Mjs.getConfig)();
    return (0, _chunkI7ZFS43CMjs.cleanAndMerge)((0, _chunkDD37ZF33Mjs.defaultConfig_default).xyChart, config.xyChart);
}
(0, _chunkDLQEHMXDMjs.__name)(getChartDefaultConfig, "getChartDefaultConfig");
function getChartDefaultData() {
    return {
        yAxis: {
            type: "linear",
            title: "",
            min: Infinity,
            max: -Infinity
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
(0, _chunkDLQEHMXDMjs.__name)(getChartDefaultData, "getChartDefaultData");
function textSanitizer(text) {
    const config = (0, _chunkDD37ZF33Mjs.getConfig)();
    return (0, _chunkDD37ZF33Mjs.sanitizeText)(text.trim(), config);
}
(0, _chunkDLQEHMXDMjs.__name)(textSanitizer, "textSanitizer");
function setTmpSVGG(SVGG) {
    tmpSVGGroup = SVGG;
}
(0, _chunkDLQEHMXDMjs.__name)(setTmpSVGG, "setTmpSVGG");
function setOrientation(orientation) {
    if (orientation === "horizontal") xyChartConfig.chartOrientation = "horizontal";
    else xyChartConfig.chartOrientation = "vertical";
}
(0, _chunkDLQEHMXDMjs.__name)(setOrientation, "setOrientation");
function setXAxisTitle(title) {
    xyChartData.xAxis.title = textSanitizer(title.text);
}
(0, _chunkDLQEHMXDMjs.__name)(setXAxisTitle, "setXAxisTitle");
function setXAxisRangeData(min, max) {
    xyChartData.xAxis = {
        type: "linear",
        title: xyChartData.xAxis.title,
        min,
        max
    };
    hasSetXAxis = true;
}
(0, _chunkDLQEHMXDMjs.__name)(setXAxisRangeData, "setXAxisRangeData");
function setXAxisBand(categories) {
    xyChartData.xAxis = {
        type: "band",
        title: xyChartData.xAxis.title,
        categories: categories.map((c)=>textSanitizer(c.text))
    };
    hasSetXAxis = true;
}
(0, _chunkDLQEHMXDMjs.__name)(setXAxisBand, "setXAxisBand");
function setYAxisTitle(title) {
    xyChartData.yAxis.title = textSanitizer(title.text);
}
(0, _chunkDLQEHMXDMjs.__name)(setYAxisTitle, "setYAxisTitle");
function setYAxisRangeData(min, max) {
    xyChartData.yAxis = {
        type: "linear",
        title: xyChartData.yAxis.title,
        min,
        max
    };
    hasSetYAxis = true;
}
(0, _chunkDLQEHMXDMjs.__name)(setYAxisRangeData, "setYAxisRangeData");
function setYAxisRangeFromPlotData(data) {
    const minValue = Math.min(...data);
    const maxValue = Math.max(...data);
    const prevMinValue = isLinearAxisData(xyChartData.yAxis) ? xyChartData.yAxis.min : Infinity;
    const prevMaxValue = isLinearAxisData(xyChartData.yAxis) ? xyChartData.yAxis.max : -Infinity;
    xyChartData.yAxis = {
        type: "linear",
        title: xyChartData.yAxis.title,
        min: Math.min(prevMinValue, minValue),
        max: Math.max(prevMaxValue, maxValue)
    };
}
(0, _chunkDLQEHMXDMjs.__name)(setYAxisRangeFromPlotData, "setYAxisRangeFromPlotData");
function transformDataWithoutCategory(data) {
    let retData = [];
    if (data.length === 0) return retData;
    if (!hasSetXAxis) {
        const prevMinValue = isLinearAxisData(xyChartData.xAxis) ? xyChartData.xAxis.min : Infinity;
        const prevMaxValue = isLinearAxisData(xyChartData.xAxis) ? xyChartData.xAxis.max : -Infinity;
        setXAxisRangeData(Math.min(prevMinValue, 1), Math.max(prevMaxValue, data.length));
    }
    if (!hasSetYAxis) setYAxisRangeFromPlotData(data);
    if (isBandAxisData(xyChartData.xAxis)) retData = xyChartData.xAxis.categories.map((c, i)=>[
            c,
            data[i]
        ]);
    if (isLinearAxisData(xyChartData.xAxis)) {
        const min = xyChartData.xAxis.min;
        const max = xyChartData.xAxis.max;
        const step = (max - min) / (data.length - 1);
        const categories = [];
        for(let i = min; i <= max; i += step)categories.push(`${i}`);
        retData = categories.map((c, i)=>[
                c,
                data[i]
            ]);
    }
    return retData;
}
(0, _chunkDLQEHMXDMjs.__name)(transformDataWithoutCategory, "transformDataWithoutCategory");
function getPlotColorFromPalette(plotIndex2) {
    return plotColorPalette[plotIndex2 === 0 ? 0 : plotIndex2 % plotColorPalette.length];
}
(0, _chunkDLQEHMXDMjs.__name)(getPlotColorFromPalette, "getPlotColorFromPalette");
function setLineData(title, data) {
    const plotData = transformDataWithoutCategory(data);
    xyChartData.plots.push({
        type: "line",
        strokeFill: getPlotColorFromPalette(plotIndex),
        strokeWidth: 2,
        data: plotData
    });
    plotIndex++;
}
(0, _chunkDLQEHMXDMjs.__name)(setLineData, "setLineData");
function setBarData(title, data) {
    const plotData = transformDataWithoutCategory(data);
    xyChartData.plots.push({
        type: "bar",
        fill: getPlotColorFromPalette(plotIndex),
        data: plotData
    });
    plotIndex++;
}
(0, _chunkDLQEHMXDMjs.__name)(setBarData, "setBarData");
function getDrawableElem() {
    if (xyChartData.plots.length === 0) throw Error("No Plot to render, please provide a plot with some data");
    xyChartData.title = (0, _chunkDD37ZF33Mjs.getDiagramTitle)();
    return XYChartBuilder.build(xyChartConfig, xyChartData, xyChartThemeConfig, tmpSVGGroup);
}
(0, _chunkDLQEHMXDMjs.__name)(getDrawableElem, "getDrawableElem");
function getChartThemeConfig() {
    return xyChartThemeConfig;
}
(0, _chunkDLQEHMXDMjs.__name)(getChartThemeConfig, "getChartThemeConfig");
function getChartConfig() {
    return xyChartConfig;
}
(0, _chunkDLQEHMXDMjs.__name)(getChartConfig, "getChartConfig");
var clear2 = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function() {
    (0, _chunkDD37ZF33Mjs.clear)();
    plotIndex = 0;
    xyChartConfig = getChartDefaultConfig();
    xyChartData = getChartDefaultData();
    xyChartThemeConfig = getChartDefaultThemeConfig();
    plotColorPalette = xyChartThemeConfig.plotColorPalette.split(",").map((color)=>color.trim());
    hasSetXAxis = false;
    hasSetYAxis = false;
}, "clear");
var xychartDb_default = {
    getDrawableElem,
    clear: clear2,
    setAccTitle: (0, _chunkDD37ZF33Mjs.setAccTitle),
    getAccTitle: (0, _chunkDD37ZF33Mjs.getAccTitle),
    setDiagramTitle: (0, _chunkDD37ZF33Mjs.setDiagramTitle),
    getDiagramTitle: (0, _chunkDD37ZF33Mjs.getDiagramTitle),
    getAccDescription: (0, _chunkDD37ZF33Mjs.getAccDescription),
    setAccDescription: (0, _chunkDD37ZF33Mjs.setAccDescription),
    setOrientation,
    setXAxisTitle,
    setXAxisRangeData,
    setXAxisBand,
    setYAxisTitle,
    setYAxisRangeData,
    setLineData,
    setBarData,
    setTmpSVGG,
    getChartThemeConfig,
    getChartConfig
};
// src/diagrams/xychart/xychartRenderer.ts
var draw = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((txt, id, _version, diagObj)=>{
    const db = diagObj.db;
    const themeConfig = db.getChartThemeConfig();
    const chartConfig = db.getChartConfig();
    function getDominantBaseLine(horizontalPos) {
        return horizontalPos === "top" ? "text-before-edge" : "middle";
    }
    (0, _chunkDLQEHMXDMjs.__name)(getDominantBaseLine, "getDominantBaseLine");
    function getTextAnchor(verticalPos) {
        return verticalPos === "left" ? "start" : verticalPos === "right" ? "end" : "middle";
    }
    (0, _chunkDLQEHMXDMjs.__name)(getTextAnchor, "getTextAnchor");
    function getTextTransformation(data) {
        return `translate(${data.x}, ${data.y}) rotate(${data.rotation || 0})`;
    }
    (0, _chunkDLQEHMXDMjs.__name)(getTextTransformation, "getTextTransformation");
    (0, _chunkDD37ZF33Mjs.log).debug("Rendering xychart chart\n" + txt);
    const svg = (0, _chunkVNRP4OIWMjs.selectSvgElement)(id);
    const group = svg.append("g").attr("class", "main");
    const background = group.append("rect").attr("width", chartConfig.width).attr("height", chartConfig.height).attr("class", "background");
    (0, _chunkDD37ZF33Mjs.configureSvgSize)(svg, chartConfig.height, chartConfig.width, true);
    svg.attr("viewBox", `0 0 ${chartConfig.width} ${chartConfig.height}`);
    background.attr("fill", themeConfig.backgroundColor);
    db.setTmpSVGG(svg.append("g").attr("class", "mermaid-tmp-group"));
    const shapes = db.getDrawableElem();
    const groups = {};
    function getGroup(gList) {
        let elem = group;
        let prefix = "";
        for (const [i] of gList.entries()){
            let parent = group;
            if (i > 0 && groups[prefix]) parent = groups[prefix];
            prefix += gList[i];
            elem = groups[prefix];
            if (!elem) elem = groups[prefix] = parent.append("g").attr("class", gList[i]);
        }
        return elem;
    }
    (0, _chunkDLQEHMXDMjs.__name)(getGroup, "getGroup");
    for (const shape of shapes){
        if (shape.data.length === 0) continue;
        const shapeGroup = getGroup(shape.groupTexts);
        switch(shape.type){
            case "rect":
                shapeGroup.selectAll("rect").data(shape.data).enter().append("rect").attr("x", (data)=>data.x).attr("y", (data)=>data.y).attr("width", (data)=>data.width).attr("height", (data)=>data.height).attr("fill", (data)=>data.fill).attr("stroke", (data)=>data.strokeFill).attr("stroke-width", (data)=>data.strokeWidth);
                break;
            case "text":
                shapeGroup.selectAll("text").data(shape.data).enter().append("text").attr("x", 0).attr("y", 0).attr("fill", (data)=>data.fill).attr("font-size", (data)=>data.fontSize).attr("dominant-baseline", (data)=>getDominantBaseLine(data.verticalPos)).attr("text-anchor", (data)=>getTextAnchor(data.horizontalPos)).attr("transform", (data)=>getTextTransformation(data)).text((data)=>data.text);
                break;
            case "path":
                shapeGroup.selectAll("path").data(shape.data).enter().append("path").attr("d", (data)=>data.path).attr("fill", (data)=>data.fill ? data.fill : "none").attr("stroke", (data)=>data.strokeFill).attr("stroke-width", (data)=>data.strokeWidth);
                break;
        }
    }
}, "draw");
var xychartRenderer_default = {
    draw
};
// src/diagrams/xychart/xychartDiagram.ts
var diagram = {
    parser: xychart_default,
    db: xychartDb_default,
    renderer: xychartRenderer_default
};

},{"./chunk-VNRP4OIW.mjs":"10Dzu","./chunk-YP6PVJQ3.mjs":"21NKC","./chunk-I7ZFS43C.mjs":"huUtc","./chunk-GKOISANM.mjs":"5yZtl","./chunk-DD37ZF33.mjs":"f4pI5","./chunk-HD3LK5B5.mjs":"lpo2u","./chunk-DLQEHMXD.mjs":"h5YRf","@parcel/transformer-js/src/esmodule-helpers.js":"1cdyk"}]},["jhrFz"], null, "parcelRequire6955", {})

//# sourceMappingURL=xychartDiagram-LJ63XVUT.378e4b07.js.map
