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
})({"dCKyU":[function(require,module,exports,__globalThis) {
var global = arguments[3];
var HMR_HOST = null;
var HMR_PORT = 1234;
var HMR_SERVER_PORT = 1234;
var HMR_SECURE = false;
var HMR_ENV_HASH = "febfef3c5b467b16";
var HMR_USE_SSE = false;
module.bundle.HMR_BUNDLE_ID = "b5c72feacb00cd10";
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

},{}],"5LzSy":[function(require,module,exports,__globalThis) {
var parcelHelpers = require("@parcel/transformer-js/src/esmodule-helpers.js");
parcelHelpers.defineInteropFlag(exports);
parcelHelpers.export(exports, "diagram", ()=>Ce);
var _chunk2RSIMOBZMjs = require("./chunk-2RSIMOBZ.mjs");
var _chunkYJEQJWB7Mjs = require("./chunk-YJEQJWB7.mjs");
var _chunk4BPNZXC3Mjs = require("./chunk-4BPNZXC3.mjs");
var _chunkUWHJNN4QMjs = require("./chunk-UWHJNN4Q.mjs");
var _chunkU6LOUQAFMjs = require("./chunk-U6LOUQAF.mjs");
var _chunkKMOJB3TBMjs = require("./chunk-KMOJB3TB.mjs");
var _chunkBOP2KBYHMjs = require("./chunk-BOP2KBYH.mjs");
var _chunk6XGRHI2AMjs = require("./chunk-6XGRHI2A.mjs");
var _chunkAC3VT7B7Mjs = require("./chunk-AC3VT7B7.mjs");
var _chunkTI4EEUUGMjs = require("./chunk-TI4EEUUG.mjs");
var _chunkNQURTBEVMjs = require("./chunk-NQURTBEV.mjs");
var _chunkBKDDFIKNMjs = require("./chunk-BKDDFIKN.mjs");
var _chunkYPUTD6PBMjs = require("./chunk-YPUTD6PB.mjs");
var _chunk6BY5RJGCMjs = require("./chunk-6BY5RJGC.mjs");
var _chunkGTKDMUJJMjs = require("./chunk-GTKDMUJJ.mjs");
var f = {}, C = {}, at = {}, lt = (0, _chunkGTKDMUJJMjs.a)(()=>{
    C = {}, at = {}, f = {};
}, "clear"), G = (0, _chunkGTKDMUJJMjs.a)((e, t)=>((0, _chunkNQURTBEVMjs.b).trace("In isDescendant", t, " ", e, " = ", C[t].includes(e)), !!C[t].includes(e)), "isDescendant"), vt = (0, _chunkGTKDMUJJMjs.a)((e, t)=>((0, _chunkNQURTBEVMjs.b).info("Descendants of ", t, " is ", C[t]), (0, _chunkNQURTBEVMjs.b).info("Edge is ", e), e.v === t || e.w === t ? !1 : C[t] ? C[t].includes(e.v) || G(e.v, t) || G(e.w, t) || C[t].includes(e.w) : ((0, _chunkNQURTBEVMjs.b).debug("Tilt, ", t, ",not in descendants"), !1)), "edgeInCluster"), ct = (0, _chunkGTKDMUJJMjs.a)((e, t, n, o)=>{
    (0, _chunkNQURTBEVMjs.b).warn("Copying children of ", e, "root", o, "data", t.node(e), o);
    let i = t.children(e) || [];
    e !== o && i.push(e), (0, _chunkNQURTBEVMjs.b).warn("Copying (nodes) clusterId", e, "nodes", i), i.forEach((r)=>{
        if (t.children(r).length > 0) ct(r, t, n, o);
        else {
            let c = t.node(r);
            (0, _chunkNQURTBEVMjs.b).info("cp ", r, " to ", o, " with parent ", e), n.setNode(r, c), o !== t.parent(r) && ((0, _chunkNQURTBEVMjs.b).warn("Setting parent", r, t.parent(r)), n.setParent(r, t.parent(r))), e !== o && r !== e ? ((0, _chunkNQURTBEVMjs.b).debug("Setting parent", r, e), n.setParent(r, e)) : ((0, _chunkNQURTBEVMjs.b).info("In copy ", e, "root", o, "data", t.node(e), o), (0, _chunkNQURTBEVMjs.b).debug("Not Setting parent for node=", r, "cluster!==rootId", e !== o, "node!==clusterId", r !== e));
            let h = t.edges(r);
            (0, _chunkNQURTBEVMjs.b).debug("Copying Edges", h), h.forEach((d)=>{
                (0, _chunkNQURTBEVMjs.b).info("Edge", d);
                let u = t.edge(d.v, d.w, d.name);
                (0, _chunkNQURTBEVMjs.b).info("Edge data", u, o);
                try {
                    vt(d, o) ? ((0, _chunkNQURTBEVMjs.b).info("Copying as ", d.v, d.w, u, d.name), n.setEdge(d.v, d.w, u, d.name), (0, _chunkNQURTBEVMjs.b).info("newGraph edges ", n.edges(), n.edge(n.edges()[0]))) : (0, _chunkNQURTBEVMjs.b).info("Skipping copy of edge ", d.v, "-->", d.w, " rootId: ", o, " clusterId:", e);
                } catch (w) {
                    (0, _chunkNQURTBEVMjs.b).error(w);
                }
            });
        }
        (0, _chunkNQURTBEVMjs.b).debug("Removing node", r), t.removeNode(r);
    });
}, "copy"), dt = (0, _chunkGTKDMUJJMjs.a)((e, t)=>{
    let n = t.children(e), o = [
        ...n
    ];
    for (let i of n)at[i] = e, o = [
        ...o,
        ...dt(i, t)
    ];
    return o;
}, "extractDescendants");
var T = (0, _chunkGTKDMUJJMjs.a)((e, t)=>{
    (0, _chunkNQURTBEVMjs.b).trace("Searching", e);
    let n = t.children(e);
    if ((0, _chunkNQURTBEVMjs.b).trace("Searching children of id ", e, n), n.length < 1) return (0, _chunkNQURTBEVMjs.b).trace("This is a valid node", e), e;
    for (let o of n){
        let i = T(o, t);
        if (i) return (0, _chunkNQURTBEVMjs.b).trace("Found replacement for", e, " => ", i), i;
    }
}, "findNonClusterChild"), R = (0, _chunkGTKDMUJJMjs.a)((e)=>!f[e] || !f[e].externalConnections ? e : f[e] ? f[e].id : e, "getAnchorId"), ft = (0, _chunkGTKDMUJJMjs.a)((e, t)=>{
    if (!e || t > 10) {
        (0, _chunkNQURTBEVMjs.b).debug("Opting out, no graph ");
        return;
    } else (0, _chunkNQURTBEVMjs.b).debug("Opting in, graph ");
    e.nodes().forEach(function(n) {
        e.children(n).length > 0 && ((0, _chunkNQURTBEVMjs.b).warn("Cluster identified", n, " Replacement id in edges: ", T(n, e)), C[n] = dt(n, e), f[n] = {
            id: T(n, e),
            clusterData: e.node(n)
        });
    }), e.nodes().forEach(function(n) {
        let o = e.children(n), i = e.edges();
        o.length > 0 ? ((0, _chunkNQURTBEVMjs.b).debug("Cluster identified", n, C), i.forEach((r)=>{
            if (r.v !== n && r.w !== n) {
                let c = G(r.v, n), h = G(r.w, n);
                c ^ h && ((0, _chunkNQURTBEVMjs.b).warn("Edge: ", r, " leaves cluster ", n), (0, _chunkNQURTBEVMjs.b).warn("Descendants of XXX ", n, ": ", C[n]), f[n].externalConnections = !0);
            }
        })) : (0, _chunkNQURTBEVMjs.b).debug("Not a cluster ", n, C);
    });
    for (let n of Object.keys(f)){
        let o = f[n].id, i = e.parent(o);
        i !== n && f[i] && !f[i].externalConnections && (f[n].id = i);
    }
    e.edges().forEach(function(n) {
        let o = e.edge(n);
        (0, _chunkNQURTBEVMjs.b).warn("Edge " + n.v + " -> " + n.w + ": " + JSON.stringify(n)), (0, _chunkNQURTBEVMjs.b).warn("Edge " + n.v + " -> " + n.w + ": " + JSON.stringify(e.edge(n)));
        let i = n.v, r = n.w;
        if ((0, _chunkNQURTBEVMjs.b).warn("Fix XXX", f, "ids:", n.v, n.w, "Translating: ", f[n.v], " --- ", f[n.w]), f[n.v] && f[n.w] && f[n.v] === f[n.w]) {
            (0, _chunkNQURTBEVMjs.b).warn("Fixing and trixing link to self - removing XXX", n.v, n.w, n.name), (0, _chunkNQURTBEVMjs.b).warn("Fixing and trixing - removing XXX", n.v, n.w, n.name), i = R(n.v), r = R(n.w), e.removeEdge(n.v, n.w, n.name);
            let c = n.w + "---" + n.v;
            e.setNode(c, {
                domId: c,
                id: c,
                labelStyle: "",
                labelText: o.label,
                padding: 0,
                shape: "labelRect",
                style: ""
            });
            let h = structuredClone(o), d = structuredClone(o);
            h.label = "", h.arrowTypeEnd = "none", d.label = "", h.fromCluster = n.v, d.toCluster = n.v, e.setEdge(i, c, h, n.name + "-cyclic-special"), e.setEdge(c, r, d, n.name + "-cyclic-special");
        } else if (f[n.v] || f[n.w]) {
            if ((0, _chunkNQURTBEVMjs.b).warn("Fixing and trixing - removing XXX", n.v, n.w, n.name), i = R(n.v), r = R(n.w), e.removeEdge(n.v, n.w, n.name), i !== n.v) {
                let c = e.parent(i);
                f[c].externalConnections = !0, o.fromCluster = n.v;
            }
            if (r !== n.w) {
                let c = e.parent(r);
                f[c].externalConnections = !0, o.toCluster = n.w;
            }
            (0, _chunkNQURTBEVMjs.b).warn("Fix Replacing with XXX", i, r, n.name), e.setEdge(i, r, o, n.name);
        }
    }), (0, _chunkNQURTBEVMjs.b).warn("Adjusted Graph", (0, _chunk4BPNZXC3Mjs.a)(e)), ht(e, 0), (0, _chunkNQURTBEVMjs.b).trace(f);
}, "adjustClustersAndEdges"), ht = (0, _chunkGTKDMUJJMjs.a)((e, t)=>{
    if ((0, _chunkNQURTBEVMjs.b).warn("extractor - ", t, (0, _chunk4BPNZXC3Mjs.a)(e), e.children("D")), t > 10) {
        (0, _chunkNQURTBEVMjs.b).error("Bailing out");
        return;
    }
    let n = e.nodes(), o = !1;
    for (let i of n){
        let r = e.children(i);
        o = o || r.length > 0;
    }
    if (!o) {
        (0, _chunkNQURTBEVMjs.b).debug("Done, no node has children", e.nodes());
        return;
    }
    (0, _chunkNQURTBEVMjs.b).debug("Nodes = ", n, t);
    for (let i of n)if ((0, _chunkNQURTBEVMjs.b).debug("Extracting node", i, f, f[i] && !f[i].externalConnections, !e.parent(i), e.node(i), e.children("D"), " Depth ", t), !f[i]) (0, _chunkNQURTBEVMjs.b).debug("Not a cluster", i, t);
    else if (!f[i].externalConnections && e.children(i) && e.children(i).length > 0) {
        (0, _chunkNQURTBEVMjs.b).warn("Cluster without external connections, without a parent and with children", i, t);
        let c = e.graph().rankdir === "TB" ? "LR" : "TB";
        f[i]?.clusterData?.dir && (c = f[i].clusterData.dir, (0, _chunkNQURTBEVMjs.b).warn("Fixing dir", f[i].clusterData.dir, c));
        let h = new (0, _chunk6XGRHI2AMjs.a)({
            multigraph: !0,
            compound: !0
        }).setGraph({
            rankdir: c,
            nodesep: 50,
            ranksep: 50,
            marginx: 8,
            marginy: 8
        }).setDefaultEdgeLabel(function() {
            return {};
        });
        (0, _chunkNQURTBEVMjs.b).warn("Old graph before copy", (0, _chunk4BPNZXC3Mjs.a)(e)), ct(i, e, h, i), e.setNode(i, {
            clusterNode: !0,
            id: i,
            clusterData: f[i].clusterData,
            labelText: f[i].labelText,
            graph: h
        }), (0, _chunkNQURTBEVMjs.b).warn("New graph after copy node: (", i, ")", (0, _chunk4BPNZXC3Mjs.a)(h)), (0, _chunkNQURTBEVMjs.b).debug("Old graph after copy", (0, _chunk4BPNZXC3Mjs.a)(e));
    } else (0, _chunkNQURTBEVMjs.b).warn("Cluster ** ", i, " **not meeting the criteria !externalConnections:", !f[i].externalConnections, " no parent: ", !e.parent(i), " children ", e.children(i) && e.children(i).length > 0, e.children("D"), t), (0, _chunkNQURTBEVMjs.b).debug(f);
    n = e.nodes(), (0, _chunkNQURTBEVMjs.b).warn("New list of nodes", n);
    for (let i of n){
        let r = e.node(i);
        (0, _chunkNQURTBEVMjs.b).warn(" Now next level", i, r), r.clusterNode && ht(r.graph, t + 1);
    }
}, "extractor"), gt = (0, _chunkGTKDMUJJMjs.a)((e, t)=>{
    if (t.length === 0) return [];
    let n = Object.assign(t);
    return t.forEach((o)=>{
        let i = e.children(o), r = gt(e, i);
        n = [
            ...n,
            ...r
        ];
    }), n;
}, "sorter"), ut = (0, _chunkGTKDMUJJMjs.a)((e)=>gt(e, e.children()), "sortNodesByHierarchy");
var Tt = (0, _chunkGTKDMUJJMjs.a)((e, t)=>{
    (0, _chunkNQURTBEVMjs.b).info("Creating subgraph rect for ", t.id, t);
    let n = (0, _chunkNQURTBEVMjs.X)(), o = e.insert("g").attr("class", "cluster" + (t.class ? " " + t.class : "")).attr("id", t.id), i = o.insert("rect", ":first-child"), r = (0, _chunkNQURTBEVMjs.G)(n.flowchart.htmlLabels), c = o.insert("g").attr("class", "cluster-label"), h = t.labelType === "markdown" ? (0, _chunkKMOJB3TBMjs.d)(c, t.labelText, {
        style: t.labelStyle,
        useHtmlLabels: r
    }, n) : c.node().appendChild((0, _chunkUWHJNN4QMjs.a)(t.labelText, t.labelStyle, void 0, !0)), d = h.getBBox();
    if ((0, _chunkNQURTBEVMjs.G)(n.flowchart.htmlLabels)) {
        let a = h.children[0], l = (0, _chunkNQURTBEVMjs.fa)(h);
        d = a.getBoundingClientRect(), l.attr("width", d.width), l.attr("height", d.height);
    }
    let u = 0 * t.padding, w = u / 2, m = t.width <= d.width + u ? d.width + u : t.width;
    t.width <= d.width + u ? t.diff = (d.width - t.width) / 2 - t.padding / 2 : t.diff = -t.padding / 2, (0, _chunkNQURTBEVMjs.b).trace("Data ", t, JSON.stringify(t)), i.attr("style", t.style).attr("rx", t.rx).attr("ry", t.ry).attr("x", t.x - m / 2).attr("y", t.y - t.height / 2 - w).attr("width", m).attr("height", t.height + u);
    let { subGraphTitleTopMargin: b } = (0, _chunkU6LOUQAFMjs.a)(n);
    r ? c.attr("transform", `translate(${t.x - d.width / 2}, ${t.y - t.height / 2 + b})`) : c.attr("transform", `translate(${t.x}, ${t.y - t.height / 2 + b})`);
    let y = i.node().getBBox();
    return t.width = y.width, t.height = y.height, t.intersect = function(a) {
        return (0, _chunkUWHJNN4QMjs.b)(t, a);
    }, o;
}, "rect"), Dt = (0, _chunkGTKDMUJJMjs.a)((e, t)=>{
    let n = e.insert("g").attr("class", "note-cluster").attr("id", t.id), o = n.insert("rect", ":first-child"), i = 0 * t.padding, r = i / 2;
    o.attr("rx", t.rx).attr("ry", t.ry).attr("x", t.x - t.width / 2 - r).attr("y", t.y - t.height / 2 - r).attr("width", t.width + i).attr("height", t.height + i).attr("fill", "none");
    let c = o.node().getBBox();
    return t.width = c.width, t.height = c.height, t.intersect = function(h) {
        return (0, _chunkUWHJNN4QMjs.b)(t, h);
    }, n;
}, "noteGroup"), kt = (0, _chunkGTKDMUJJMjs.a)((e, t)=>{
    let n = (0, _chunkNQURTBEVMjs.X)(), o = e.insert("g").attr("class", t.classes).attr("id", t.id), i = o.insert("rect", ":first-child"), r = o.insert("g").attr("class", "cluster-label"), c = o.append("rect"), h = r.node().appendChild((0, _chunkUWHJNN4QMjs.a)(t.labelText, t.labelStyle, void 0, !0)), d = h.getBBox();
    if ((0, _chunkNQURTBEVMjs.G)(n.flowchart.htmlLabels)) {
        let a = h.children[0], l = (0, _chunkNQURTBEVMjs.fa)(h);
        d = a.getBoundingClientRect(), l.attr("width", d.width), l.attr("height", d.height);
    }
    d = h.getBBox();
    let u = 0 * t.padding, w = u / 2, m = t.width <= d.width + t.padding ? d.width + t.padding : t.width;
    t.width <= d.width + t.padding ? t.diff = (d.width + t.padding * 0 - t.width) / 2 : t.diff = -t.padding / 2, i.attr("class", "outer").attr("x", t.x - m / 2 - w).attr("y", t.y - t.height / 2 - w).attr("width", m + u).attr("height", t.height + u), c.attr("class", "inner").attr("x", t.x - m / 2 - w).attr("y", t.y - t.height / 2 - w + d.height - 1).attr("width", m + u).attr("height", t.height + u - d.height - 3);
    let { subGraphTitleTopMargin: b } = (0, _chunkU6LOUQAFMjs.a)(n);
    r.attr("transform", `translate(${t.x - d.width / 2}, ${t.y - t.height / 2 - t.padding / 3 + ((0, _chunkNQURTBEVMjs.G)(n.flowchart.htmlLabels) ? 5 : 3) + b})`);
    let y = i.node().getBBox();
    return t.height = y.height, t.intersect = function(a) {
        return (0, _chunkUWHJNN4QMjs.b)(t, a);
    }, o;
}, "roundedWithTitle"), Xt = (0, _chunkGTKDMUJJMjs.a)((e, t)=>{
    let n = e.insert("g").attr("class", t.classes).attr("id", t.id), o = n.insert("rect", ":first-child"), i = 0 * t.padding, r = i / 2;
    o.attr("class", "divider").attr("x", t.x - t.width / 2 - r).attr("y", t.y - t.height / 2).attr("width", t.width + i).attr("height", t.height + i);
    let c = o.node().getBBox();
    return t.width = c.width, t.height = c.height, t.diff = -t.padding / 2, t.intersect = function(h) {
        return (0, _chunkUWHJNN4QMjs.b)(t, h);
    }, n;
}, "divider"), Bt = {
    rect: Tt,
    roundedWithTitle: kt,
    noteGroup: Dt,
    divider: Xt
}, pt = {}, mt = (0, _chunkGTKDMUJJMjs.a)((e, t)=>{
    (0, _chunkNQURTBEVMjs.b).trace("Inserting cluster");
    let n = t.shape || "rect";
    pt[t.id] = Bt[n](e, t);
}, "insertCluster");
var wt = (0, _chunkGTKDMUJJMjs.a)(()=>{
    pt = {};
}, "clear");
var yt = (0, _chunkGTKDMUJJMjs.a)(async (e, t, n, o, i, r)=>{
    (0, _chunkNQURTBEVMjs.b).info("Graph in recursive render: XXX", (0, _chunk4BPNZXC3Mjs.a)(t), i);
    let c = t.graph().rankdir;
    (0, _chunkNQURTBEVMjs.b).trace("Dir in recursive render - dir:", c);
    let h = e.insert("g").attr("class", "root");
    t.nodes() ? (0, _chunkNQURTBEVMjs.b).info("Recursive render XXX", t.nodes()) : (0, _chunkNQURTBEVMjs.b).info("No nodes found for", t), t.edges().length > 0 && (0, _chunkNQURTBEVMjs.b).trace("Recursive edges", t.edge(t.edges()[0]));
    let d = h.insert("g").attr("class", "clusters"), u = h.insert("g").attr("class", "edgePaths"), w = h.insert("g").attr("class", "edgeLabels"), m = h.insert("g").attr("class", "nodes");
    await Promise.all(t.nodes().map(async function(a) {
        let l = t.node(a);
        if (i !== void 0) {
            let N = JSON.parse(JSON.stringify(i.clusterData));
            (0, _chunkNQURTBEVMjs.b).info("Setting data for cluster XXX (", a, ") ", N, i), t.setNode(i.id, N), t.parent(a) || ((0, _chunkNQURTBEVMjs.b).trace("Setting parent", a, i.id), t.setParent(a, i.id, N));
        }
        if ((0, _chunkNQURTBEVMjs.b).info("(Insert) Node XXX" + a + ": " + JSON.stringify(t.node(a))), l?.clusterNode) {
            (0, _chunkNQURTBEVMjs.b).info("Cluster identified", a, l.width, t.node(a));
            let { ranksep: N, nodesep: v } = t.graph();
            l.graph.setGraph({
                ...l.graph.graph(),
                ranksep: N,
                nodesep: v
            });
            let L = await yt(m, l.graph, n, o, t.node(a), r), S = L.elem;
            (0, _chunkUWHJNN4QMjs.c)(l, S), l.diff = L.diff || 0, (0, _chunkNQURTBEVMjs.b).info("Node bounds (abc123)", a, l, l.width, l.x, l.y), (0, _chunkUWHJNN4QMjs.e)(S, l), (0, _chunkNQURTBEVMjs.b).warn("Recursive render complete ", S, l);
        } else t.children(a).length > 0 ? ((0, _chunkNQURTBEVMjs.b).info("Cluster - the non recursive path XXX", a, l.id, l, t), (0, _chunkNQURTBEVMjs.b).info(T(l.id, t)), f[l.id] = {
            id: T(l.id, t),
            node: l
        }) : ((0, _chunkNQURTBEVMjs.b).info("Node - the non recursive path", a, l.id, l), await (0, _chunkUWHJNN4QMjs.d)(m, t.node(a), c));
    })), t.edges().forEach(async function(a) {
        let l = t.edge(a.v, a.w, a.name);
        (0, _chunkNQURTBEVMjs.b).info("Edge " + a.v + " -> " + a.w + ": " + JSON.stringify(a)), (0, _chunkNQURTBEVMjs.b).info("Edge " + a.v + " -> " + a.w + ": ", a, " ", JSON.stringify(t.edge(a))), (0, _chunkNQURTBEVMjs.b).info("Fix", f, "ids:", a.v, a.w, "Translating: ", f[a.v], f[a.w]), await (0, _chunk2RSIMOBZMjs.c)(w, l);
    }), t.edges().forEach(function(a) {
        (0, _chunkNQURTBEVMjs.b).info("Edge " + a.v + " -> " + a.w + ": " + JSON.stringify(a));
    }), (0, _chunkNQURTBEVMjs.b).info("Graph before layout:", JSON.stringify((0, _chunk4BPNZXC3Mjs.a)(t))), (0, _chunkNQURTBEVMjs.b).info("#############################################"), (0, _chunkNQURTBEVMjs.b).info("###                Layout                 ###"), (0, _chunkNQURTBEVMjs.b).info("#############################################"), (0, _chunkNQURTBEVMjs.b).info(t), (0, _chunkBOP2KBYHMjs.a)(t), (0, _chunkNQURTBEVMjs.b).info("Graph after layout:", JSON.stringify((0, _chunk4BPNZXC3Mjs.a)(t)));
    let b = 0, { subGraphTitleTotalMargin: y } = (0, _chunkU6LOUQAFMjs.a)(r);
    return ut(t).forEach(function(a) {
        let l = t.node(a);
        (0, _chunkNQURTBEVMjs.b).info("Position " + a + ": " + JSON.stringify(t.node(a))), (0, _chunkNQURTBEVMjs.b).info("Position " + a + ": (" + l.x, "," + l.y, ") width: ", l.width, " height: ", l.height), l?.clusterNode ? (l.y += y, (0, _chunkUWHJNN4QMjs.g)(l)) : t.children(a).length > 0 ? (l.height += y, mt(d, l), f[l.id].node = l) : (l.y += y / 2, (0, _chunkUWHJNN4QMjs.g)(l));
    }), t.edges().forEach(function(a) {
        let l = t.edge(a);
        (0, _chunkNQURTBEVMjs.b).info("Edge " + a.v + " -> " + a.w + ": " + JSON.stringify(l), l), l.points.forEach((v)=>v.y += y / 2);
        let N = (0, _chunk2RSIMOBZMjs.e)(u, a, l, f, n, t, o);
        (0, _chunk2RSIMOBZMjs.d)(l, N);
    }), t.nodes().forEach(function(a) {
        let l = t.node(a);
        (0, _chunkNQURTBEVMjs.b).info(a, l.type, l.diff), l.type === "group" && (b = l.diff);
    }), {
        elem: h,
        diff: b
    };
}, "recursiveRender"), bt = (0, _chunkGTKDMUJJMjs.a)(async (e, t, n, o, i)=>{
    (0, _chunk2RSIMOBZMjs.a)(e, n, o, i), (0, _chunkUWHJNN4QMjs.f)(), (0, _chunk2RSIMOBZMjs.b)(), wt(), lt(), (0, _chunkNQURTBEVMjs.b).warn("Graph at first:", JSON.stringify((0, _chunk4BPNZXC3Mjs.a)(t))), ft(t), (0, _chunkNQURTBEVMjs.b).warn("Graph after:", JSON.stringify((0, _chunk4BPNZXC3Mjs.a)(t)));
    let r = (0, _chunkNQURTBEVMjs.X)();
    await yt(e, t, o, i, void 0, r);
}, "render");
var W = (0, _chunkGTKDMUJJMjs.a)((e)=>(0, _chunkNQURTBEVMjs.L).sanitizeText(e, (0, _chunkNQURTBEVMjs.X)()), "sanitizeText"), H = {
    dividerMargin: 10,
    padding: 5,
    textHeight: 10,
    curve: void 0
}, Lt = (0, _chunkGTKDMUJJMjs.a)(function(e, t, n, o) {
    (0, _chunkNQURTBEVMjs.b).info("keys:", [
        ...e.keys()
    ]), (0, _chunkNQURTBEVMjs.b).info(e), e.forEach(function(i) {
        let c = {
            shape: "rect",
            id: i.id,
            domId: i.domId,
            labelText: W(i.id),
            labelStyle: "",
            style: "fill: none; stroke: black",
            padding: (0, _chunkNQURTBEVMjs.X)().flowchart?.padding ?? (0, _chunkNQURTBEVMjs.X)().class?.padding
        };
        t.setNode(i.id, c), Ct(i.classes, t, n, o, i.id), (0, _chunkNQURTBEVMjs.b).info("setNode", c);
    });
}, "addNamespaces"), Ct = (0, _chunkGTKDMUJJMjs.a)(function(e, t, n, o, i) {
    (0, _chunkNQURTBEVMjs.b).info("keys:", [
        ...e.keys()
    ]), (0, _chunkNQURTBEVMjs.b).info(e), [
        ...e.values()
    ].filter((r)=>r.parent === i).forEach(function(r) {
        let c = r.cssClasses.join(" "), h = (0, _chunkAC3VT7B7Mjs.d)(r.styles), d = r.label ?? r.id, u = 0, m = {
            labelStyle: h.labelStyle,
            shape: "class_box",
            labelText: W(d),
            classData: r,
            rx: u,
            ry: u,
            class: c,
            style: h.style,
            id: r.id,
            domId: r.domId,
            tooltip: o.db.getTooltip(r.id, i) || "",
            haveCallback: r.haveCallback,
            link: r.link,
            width: r.type === "group" ? 500 : void 0,
            type: r.type,
            padding: (0, _chunkNQURTBEVMjs.X)().flowchart?.padding ?? (0, _chunkNQURTBEVMjs.X)().class?.padding
        };
        t.setNode(r.id, m), i && t.setParent(r.id, i), (0, _chunkNQURTBEVMjs.b).info("setNode", m);
    });
}, "addClasses"), Jt = (0, _chunkGTKDMUJJMjs.a)(function(e, t, n, o) {
    (0, _chunkNQURTBEVMjs.b).info(e), e.forEach(function(i, r) {
        let c = i, h = "", d = {
            labelStyle: "",
            style: ""
        }, u = c.text, w = 0, b = {
            labelStyle: d.labelStyle,
            shape: "note",
            labelText: W(u),
            noteData: c,
            rx: w,
            ry: w,
            class: h,
            style: d.style,
            id: c.id,
            domId: c.id,
            tooltip: "",
            type: "note",
            padding: (0, _chunkNQURTBEVMjs.X)().flowchart?.padding ?? (0, _chunkNQURTBEVMjs.X)().class?.padding
        };
        if (t.setNode(c.id, b), (0, _chunkNQURTBEVMjs.b).info("setNode", b), !c.class || !o.has(c.class)) return;
        let y = n + r, a = {
            id: `edgeNote${y}`,
            classes: "relation",
            pattern: "dotted",
            arrowhead: "none",
            startLabelRight: "",
            endLabelLeft: "",
            arrowTypeStart: "none",
            arrowTypeEnd: "none",
            style: "fill:none",
            labelStyle: "",
            curve: (0, _chunkAC3VT7B7Mjs.c)(H.curve, (0, _chunkNQURTBEVMjs.Ba))
        };
        t.setEdge(c.id, c.class, a, y);
    });
}, "addNotes"), Rt = (0, _chunkGTKDMUJJMjs.a)(function(e, t) {
    let n = (0, _chunkNQURTBEVMjs.X)().flowchart, o = 0;
    e.forEach(function(i) {
        o++;
        let r = {
            classes: "relation",
            pattern: i.relation.lineType == 1 ? "dashed" : "solid",
            id: (0, _chunkAC3VT7B7Mjs.p)(i.id1, i.id2, {
                prefix: "id",
                counter: o
            }),
            arrowhead: i.type === "arrow_open" ? "none" : "normal",
            startLabelRight: i.relationTitle1 === "none" ? "" : i.relationTitle1,
            endLabelLeft: i.relationTitle2 === "none" ? "" : i.relationTitle2,
            arrowTypeStart: xt(i.relation.type1),
            arrowTypeEnd: xt(i.relation.type2),
            style: "fill:none",
            labelStyle: "",
            curve: (0, _chunkAC3VT7B7Mjs.c)(n?.curve, (0, _chunkNQURTBEVMjs.Ba))
        };
        if ((0, _chunkNQURTBEVMjs.b).info(r, i), i.style !== void 0) {
            let c = (0, _chunkAC3VT7B7Mjs.d)(i.style);
            r.style = c.style, r.labelStyle = c.labelStyle;
        }
        i.text = i.title, i.text === void 0 ? i.style !== void 0 && (r.arrowheadStyle = "fill: #333") : (r.arrowheadStyle = "fill: #333", r.labelpos = "c", (0, _chunkNQURTBEVMjs.X)().flowchart?.htmlLabels ?? (0, _chunkNQURTBEVMjs.X)().htmlLabels ? (r.labelType = "html", r.label = '<span class="edgeLabel">' + i.text + "</span>") : (r.labelType = "text", r.label = i.text.replace((0, _chunkNQURTBEVMjs.L).lineBreakRegex, `
`), i.style === void 0 && (r.style = r.style || "stroke: #333; stroke-width: 1.5px;fill:none"), r.labelStyle = r.labelStyle.replace("color:", "fill:"))), t.setEdge(i.id1, i.id2, r, o);
    });
}, "addRelations"), Gt = (0, _chunkGTKDMUJJMjs.a)(function(e) {
    H = {
        ...H,
        ...e
    };
}, "setConf"), Mt = (0, _chunkGTKDMUJJMjs.a)(async function(e, t, n, o) {
    (0, _chunkNQURTBEVMjs.b).info("Drawing class - ", t);
    let i = (0, _chunkNQURTBEVMjs.X)().flowchart ?? (0, _chunkNQURTBEVMjs.X)().class, r = (0, _chunkNQURTBEVMjs.X)().securityLevel;
    (0, _chunkNQURTBEVMjs.b).info("config:", i);
    let c = i?.nodeSpacing ?? 50, h = i?.rankSpacing ?? 50, d = new (0, _chunk6XGRHI2AMjs.a)({
        multigraph: !0,
        compound: !0
    }).setGraph({
        rankdir: o.db.getDirection(),
        nodesep: c,
        ranksep: h,
        marginx: 8,
        marginy: 8
    }).setDefaultEdgeLabel(function() {
        return {};
    }), u = o.db.getNamespaces(), w = o.db.getClasses(), m = o.db.getRelations(), b = o.db.getNotes();
    (0, _chunkNQURTBEVMjs.b).info(m), Lt(u, d, t, o), Ct(w, d, t, o), Rt(m, d), Jt(b, d, m.length + 1, w);
    let y;
    r === "sandbox" && (y = (0, _chunkNQURTBEVMjs.fa)("#i" + t));
    let a = r === "sandbox" ? (0, _chunkNQURTBEVMjs.fa)(y.nodes()[0].contentDocument.body) : (0, _chunkNQURTBEVMjs.fa)("body"), l = a.select(`[id="${t}"]`), N = a.select("#" + t + " g");
    if (await bt(N, d, [
        "aggregation",
        "extension",
        "composition",
        "dependency",
        "lollipop"
    ], "classDiagram", t), (0, _chunkAC3VT7B7Mjs.m).insertTitle(l, "classTitleText", i?.titleTopMargin ?? 5, o.db.getDiagramTitle()), (0, _chunkNQURTBEVMjs.N)(d, l, i?.diagramPadding, i?.useMaxWidth), !i?.htmlLabels) {
        let v = r === "sandbox" ? y.nodes()[0].contentDocument : document, L = v.querySelectorAll('[id="' + t + '"] .edgeLabel .label');
        for (let S of L){
            let j = S.getBBox(), D = v.createElementNS("http://www.w3.org/2000/svg", "rect");
            D.setAttribute("rx", 0), D.setAttribute("ry", 0), D.setAttribute("width", j.width), D.setAttribute("height", j.height), S.insertBefore(D, S.firstChild);
        }
    }
}, "draw");
function xt(e) {
    let t;
    switch(e){
        case 0:
            t = "aggregation";
            break;
        case 1:
            t = "extension";
            break;
        case 2:
            t = "composition";
            break;
        case 3:
            t = "dependency";
            break;
        case 4:
            t = "lollipop";
            break;
        default:
            t = "none";
    }
    return t;
}
(0, _chunkGTKDMUJJMjs.a)(xt, "getArrowMarker");
var Nt = {
    setConf: Gt,
    draw: Mt
};
var Ce = {
    parser: (0, _chunkYJEQJWB7Mjs.a),
    db: (0, _chunkYJEQJWB7Mjs.b),
    renderer: Nt,
    styles: (0, _chunkYJEQJWB7Mjs.c),
    init: (0, _chunkGTKDMUJJMjs.a)((e)=>{
        e.class || (e.class = {}), e.class.arrowMarkerAbsolute = e.arrowMarkerAbsolute, (0, _chunkYJEQJWB7Mjs.b).clear();
    }, "init")
};

},{"./chunk-2RSIMOBZ.mjs":"6y77U","./chunk-YJEQJWB7.mjs":"bBvyn","./chunk-4BPNZXC3.mjs":"9GO9x","./chunk-UWHJNN4Q.mjs":"6LAlC","./chunk-U6LOUQAF.mjs":"v9pSW","./chunk-KMOJB3TB.mjs":"aJH4M","./chunk-BOP2KBYH.mjs":"klimL","./chunk-6XGRHI2A.mjs":"fUQIF","./chunk-AC3VT7B7.mjs":"1eiUz","./chunk-TI4EEUUG.mjs":"8SKrN","./chunk-NQURTBEV.mjs":"iASFe","./chunk-BKDDFIKN.mjs":"hADfH","./chunk-YPUTD6PB.mjs":"aSdv1","./chunk-6BY5RJGC.mjs":"bRXnR","./chunk-GTKDMUJJ.mjs":"fruhx","@parcel/transformer-js/src/esmodule-helpers.js":"1cdyk"}],"6y77U":[function(require,module,exports,__globalThis) {
var parcelHelpers = require("@parcel/transformer-js/src/esmodule-helpers.js");
parcelHelpers.defineInteropFlag(exports);
parcelHelpers.export(exports, "a", ()=>tt);
parcelHelpers.export(exports, "b", ()=>bt);
parcelHelpers.export(exports, "c", ()=>xt);
parcelHelpers.export(exports, "d", ()=>yt);
parcelHelpers.export(exports, "e", ()=>Lt);
var _chunkUWHJNN4QMjs = require("./chunk-UWHJNN4Q.mjs");
var _chunkU6LOUQAFMjs = require("./chunk-U6LOUQAF.mjs");
var _chunkKMOJB3TBMjs = require("./chunk-KMOJB3TB.mjs");
var _chunkAC3VT7B7Mjs = require("./chunk-AC3VT7B7.mjs");
var _chunkNQURTBEVMjs = require("./chunk-NQURTBEV.mjs");
var _chunkGTKDMUJJMjs = require("./chunk-GTKDMUJJ.mjs");
var Z = (0, _chunkGTKDMUJJMjs.a)((a, t, r, p)=>{
    t.forEach((o)=>{
        F[o](a, r, p);
    });
}, "insertMarkers"), G = (0, _chunkGTKDMUJJMjs.a)((a, t, r)=>{
    (0, _chunkNQURTBEVMjs.b).trace("Making markers for ", r), a.append("defs").append("marker").attr("id", r + "_" + t + "-extensionStart").attr("class", "marker extension " + t).attr("refX", 18).attr("refY", 7).attr("markerWidth", 190).attr("markerHeight", 240).attr("orient", "auto").append("path").attr("d", "M 1,7 L18,13 V 1 Z"), a.append("defs").append("marker").attr("id", r + "_" + t + "-extensionEnd").attr("class", "marker extension " + t).attr("refX", 1).attr("refY", 7).attr("markerWidth", 20).attr("markerHeight", 28).attr("orient", "auto").append("path").attr("d", "M 1,1 V 13 L18,7 Z");
}, "extension"), V = (0, _chunkGTKDMUJJMjs.a)((a, t, r)=>{
    a.append("defs").append("marker").attr("id", r + "_" + t + "-compositionStart").attr("class", "marker composition " + t).attr("refX", 18).attr("refY", 7).attr("markerWidth", 190).attr("markerHeight", 240).attr("orient", "auto").append("path").attr("d", "M 18,7 L9,13 L1,7 L9,1 Z"), a.append("defs").append("marker").attr("id", r + "_" + t + "-compositionEnd").attr("class", "marker composition " + t).attr("refX", 1).attr("refY", 7).attr("markerWidth", 20).attr("markerHeight", 28).attr("orient", "auto").append("path").attr("d", "M 18,7 L9,13 L1,7 L9,1 Z");
}, "composition"), q = (0, _chunkGTKDMUJJMjs.a)((a, t, r)=>{
    a.append("defs").append("marker").attr("id", r + "_" + t + "-aggregationStart").attr("class", "marker aggregation " + t).attr("refX", 18).attr("refY", 7).attr("markerWidth", 190).attr("markerHeight", 240).attr("orient", "auto").append("path").attr("d", "M 18,7 L9,13 L1,7 L9,1 Z"), a.append("defs").append("marker").attr("id", r + "_" + t + "-aggregationEnd").attr("class", "marker aggregation " + t).attr("refX", 1).attr("refY", 7).attr("markerWidth", 20).attr("markerHeight", 28).attr("orient", "auto").append("path").attr("d", "M 18,7 L9,13 L1,7 L9,1 Z");
}, "aggregation"), A = (0, _chunkGTKDMUJJMjs.a)((a, t, r)=>{
    a.append("defs").append("marker").attr("id", r + "_" + t + "-dependencyStart").attr("class", "marker dependency " + t).attr("refX", 6).attr("refY", 7).attr("markerWidth", 190).attr("markerHeight", 240).attr("orient", "auto").append("path").attr("d", "M 5,7 L9,13 L1,7 L9,1 Z"), a.append("defs").append("marker").attr("id", r + "_" + t + "-dependencyEnd").attr("class", "marker dependency " + t).attr("refX", 13).attr("refY", 7).attr("markerWidth", 20).attr("markerHeight", 28).attr("orient", "auto").append("path").attr("d", "M 18,7 L9,13 L14,7 L9,1 Z");
}, "dependency"), I = (0, _chunkGTKDMUJJMjs.a)((a, t, r)=>{
    a.append("defs").append("marker").attr("id", r + "_" + t + "-lollipopStart").attr("class", "marker lollipop " + t).attr("refX", 13).attr("refY", 7).attr("markerWidth", 190).attr("markerHeight", 240).attr("orient", "auto").append("circle").attr("stroke", "black").attr("fill", "transparent").attr("cx", 7).attr("cy", 7).attr("r", 6), a.append("defs").append("marker").attr("id", r + "_" + t + "-lollipopEnd").attr("class", "marker lollipop " + t).attr("refX", 1).attr("refY", 7).attr("markerWidth", 190).attr("markerHeight", 240).attr("orient", "auto").append("circle").attr("stroke", "black").attr("fill", "transparent").attr("cx", 7).attr("cy", 7).attr("r", 6);
}, "lollipop"), N = (0, _chunkGTKDMUJJMjs.a)((a, t, r)=>{
    a.append("marker").attr("id", r + "_" + t + "-pointEnd").attr("class", "marker " + t).attr("viewBox", "0 0 10 10").attr("refX", 6).attr("refY", 5).attr("markerUnits", "userSpaceOnUse").attr("markerWidth", 12).attr("markerHeight", 12).attr("orient", "auto").append("path").attr("d", "M 0 0 L 10 5 L 0 10 z").attr("class", "arrowMarkerPath").style("stroke-width", 1).style("stroke-dasharray", "1,0"), a.append("marker").attr("id", r + "_" + t + "-pointStart").attr("class", "marker " + t).attr("viewBox", "0 0 10 10").attr("refX", 4.5).attr("refY", 5).attr("markerUnits", "userSpaceOnUse").attr("markerWidth", 12).attr("markerHeight", 12).attr("orient", "auto").append("path").attr("d", "M 0 5 L 10 10 L 10 0 z").attr("class", "arrowMarkerPath").style("stroke-width", 1).style("stroke-dasharray", "1,0");
}, "point"), Q = (0, _chunkGTKDMUJJMjs.a)((a, t, r)=>{
    a.append("marker").attr("id", r + "_" + t + "-circleEnd").attr("class", "marker " + t).attr("viewBox", "0 0 10 10").attr("refX", 11).attr("refY", 5).attr("markerUnits", "userSpaceOnUse").attr("markerWidth", 11).attr("markerHeight", 11).attr("orient", "auto").append("circle").attr("cx", "5").attr("cy", "5").attr("r", "5").attr("class", "arrowMarkerPath").style("stroke-width", 1).style("stroke-dasharray", "1,0"), a.append("marker").attr("id", r + "_" + t + "-circleStart").attr("class", "marker " + t).attr("viewBox", "0 0 10 10").attr("refX", -1).attr("refY", 5).attr("markerUnits", "userSpaceOnUse").attr("markerWidth", 11).attr("markerHeight", 11).attr("orient", "auto").append("circle").attr("cx", "5").attr("cy", "5").attr("r", "5").attr("class", "arrowMarkerPath").style("stroke-width", 1).style("stroke-dasharray", "1,0");
}, "circle"), j = (0, _chunkGTKDMUJJMjs.a)((a, t, r)=>{
    a.append("marker").attr("id", r + "_" + t + "-crossEnd").attr("class", "marker cross " + t).attr("viewBox", "0 0 11 11").attr("refX", 12).attr("refY", 5.2).attr("markerUnits", "userSpaceOnUse").attr("markerWidth", 11).attr("markerHeight", 11).attr("orient", "auto").append("path").attr("d", "M 1,1 l 9,9 M 10,1 l -9,9").attr("class", "arrowMarkerPath").style("stroke-width", 2).style("stroke-dasharray", "1,0"), a.append("marker").attr("id", r + "_" + t + "-crossStart").attr("class", "marker cross " + t).attr("viewBox", "0 0 11 11").attr("refX", -1).attr("refY", 5.2).attr("markerUnits", "userSpaceOnUse").attr("markerWidth", 11).attr("markerHeight", 11).attr("orient", "auto").append("path").attr("d", "M 1,1 l 9,9 M 10,1 l -9,9").attr("class", "arrowMarkerPath").style("stroke-width", 2).style("stroke-dasharray", "1,0");
}, "cross"), z = (0, _chunkGTKDMUJJMjs.a)((a, t, r)=>{
    a.append("defs").append("marker").attr("id", r + "_" + t + "-barbEnd").attr("refX", 19).attr("refY", 7).attr("markerWidth", 20).attr("markerHeight", 14).attr("markerUnits", "strokeWidth").attr("orient", "auto").append("path").attr("d", "M 19,7 L9,13 L14,7 L9,1 Z");
}, "barb"), F = {
    extension: G,
    composition: V,
    aggregation: q,
    dependency: A,
    lollipop: I,
    point: N,
    circle: Q,
    cross: j,
    barb: z
}, tt = Z;
var H = (0, _chunkGTKDMUJJMjs.a)((a, t, r, p, o)=>{
    t.arrowTypeStart && C(a, "start", t.arrowTypeStart, r, p, o), t.arrowTypeEnd && C(a, "end", t.arrowTypeEnd, r, p, o);
}, "addEdgeMarkers"), J = {
    arrow_cross: "cross",
    arrow_point: "point",
    arrow_barb: "barb",
    arrow_circle: "circle",
    aggregation: "aggregation",
    extension: "extension",
    composition: "composition",
    dependency: "dependency",
    lollipop: "lollipop"
}, C = (0, _chunkGTKDMUJJMjs.a)((a, t, r, p, o, c)=>{
    let n = J[r];
    if (!n) {
        (0, _chunkNQURTBEVMjs.b).warn(`Unknown arrow type: ${r}`);
        return;
    }
    let e = t === "start" ? "Start" : "End";
    a.attr(`marker-${t}`, `url(${p}#${o}_${c}-${n}${e})`);
}, "addEdgeMarker");
var M = {}, m = {}, bt = (0, _chunkGTKDMUJJMjs.a)(()=>{
    M = {}, m = {};
}, "clear"), xt = (0, _chunkGTKDMUJJMjs.a)((a, t)=>{
    let r = (0, _chunkNQURTBEVMjs.X)(), p = (0, _chunkNQURTBEVMjs.G)(r.flowchart.htmlLabels), o = t.labelType === "markdown" ? (0, _chunkKMOJB3TBMjs.d)(a, t.label, {
        style: t.labelStyle,
        useHtmlLabels: p,
        addSvgBackground: !0
    }, r) : (0, _chunkUWHJNN4QMjs.a)(t.label, t.labelStyle), c = a.insert("g").attr("class", "edgeLabel"), n = c.insert("g").attr("class", "label");
    n.node().appendChild(o);
    let e = o.getBBox();
    if (p) {
        let i = o.children[0], l = (0, _chunkNQURTBEVMjs.fa)(o);
        e = i.getBoundingClientRect(), l.attr("width", e.width), l.attr("height", e.height);
    }
    n.attr("transform", "translate(" + -e.width / 2 + ", " + -e.height / 2 + ")"), M[t.id] = c, t.width = e.width, t.height = e.height;
    let s;
    if (t.startLabelLeft) {
        let i = (0, _chunkUWHJNN4QMjs.a)(t.startLabelLeft, t.labelStyle), l = a.insert("g").attr("class", "edgeTerminals"), f = l.insert("g").attr("class", "inner");
        s = f.node().appendChild(i);
        let h = i.getBBox();
        f.attr("transform", "translate(" + -h.width / 2 + ", " + -h.height / 2 + ")"), m[t.id] || (m[t.id] = {}), m[t.id].startLeft = l, u(s, t.startLabelLeft);
    }
    if (t.startLabelRight) {
        let i = (0, _chunkUWHJNN4QMjs.a)(t.startLabelRight, t.labelStyle), l = a.insert("g").attr("class", "edgeTerminals"), f = l.insert("g").attr("class", "inner");
        s = l.node().appendChild(i), f.node().appendChild(i);
        let h = i.getBBox();
        f.attr("transform", "translate(" + -h.width / 2 + ", " + -h.height / 2 + ")"), m[t.id] || (m[t.id] = {}), m[t.id].startRight = l, u(s, t.startLabelRight);
    }
    if (t.endLabelLeft) {
        let i = (0, _chunkUWHJNN4QMjs.a)(t.endLabelLeft, t.labelStyle), l = a.insert("g").attr("class", "edgeTerminals"), f = l.insert("g").attr("class", "inner");
        s = f.node().appendChild(i);
        let h = i.getBBox();
        f.attr("transform", "translate(" + -h.width / 2 + ", " + -h.height / 2 + ")"), l.node().appendChild(i), m[t.id] || (m[t.id] = {}), m[t.id].endLeft = l, u(s, t.endLabelLeft);
    }
    if (t.endLabelRight) {
        let i = (0, _chunkUWHJNN4QMjs.a)(t.endLabelRight, t.labelStyle), l = a.insert("g").attr("class", "edgeTerminals"), f = l.insert("g").attr("class", "inner");
        s = f.node().appendChild(i);
        let h = i.getBBox();
        f.attr("transform", "translate(" + -h.width / 2 + ", " + -h.height / 2 + ")"), l.node().appendChild(i), m[t.id] || (m[t.id] = {}), m[t.id].endRight = l, u(s, t.endLabelRight);
    }
    return o;
}, "insertEdgeLabel");
function u(a, t) {
    (0, _chunkNQURTBEVMjs.X)().flowchart.htmlLabels && a && (a.style.width = t.length * 9 + "px", a.style.height = "12px");
}
(0, _chunkGTKDMUJJMjs.a)(u, "setTerminalWidth");
var yt = (0, _chunkGTKDMUJJMjs.a)((a, t)=>{
    (0, _chunkNQURTBEVMjs.b).debug("Moving label abc88 ", a.id, a.label, M[a.id], t);
    let r = t.updatedPath ? t.updatedPath : t.originalPath, p = (0, _chunkNQURTBEVMjs.X)(), { subGraphTitleTotalMargin: o } = (0, _chunkU6LOUQAFMjs.a)(p);
    if (a.label) {
        let c = M[a.id], n = a.x, e = a.y;
        if (r) {
            let s = (0, _chunkAC3VT7B7Mjs.m).calcLabelPosition(r);
            (0, _chunkNQURTBEVMjs.b).debug("Moving label " + a.label + " from (", n, ",", e, ") to (", s.x, ",", s.y, ") abc88"), t.updatedPath && (n = s.x, e = s.y);
        }
        c.attr("transform", `translate(${n}, ${e + o / 2})`);
    }
    if (a.startLabelLeft) {
        let c = m[a.id].startLeft, n = a.x, e = a.y;
        if (r) {
            let s = (0, _chunkAC3VT7B7Mjs.m).calcTerminalLabelPosition(a.arrowTypeStart ? 10 : 0, "start_left", r);
            n = s.x, e = s.y;
        }
        c.attr("transform", `translate(${n}, ${e})`);
    }
    if (a.startLabelRight) {
        let c = m[a.id].startRight, n = a.x, e = a.y;
        if (r) {
            let s = (0, _chunkAC3VT7B7Mjs.m).calcTerminalLabelPosition(a.arrowTypeStart ? 10 : 0, "start_right", r);
            n = s.x, e = s.y;
        }
        c.attr("transform", `translate(${n}, ${e})`);
    }
    if (a.endLabelLeft) {
        let c = m[a.id].endLeft, n = a.x, e = a.y;
        if (r) {
            let s = (0, _chunkAC3VT7B7Mjs.m).calcTerminalLabelPosition(a.arrowTypeEnd ? 10 : 0, "end_left", r);
            n = s.x, e = s.y;
        }
        c.attr("transform", `translate(${n}, ${e})`);
    }
    if (a.endLabelRight) {
        let c = m[a.id].endRight, n = a.x, e = a.y;
        if (r) {
            let s = (0, _chunkAC3VT7B7Mjs.m).calcTerminalLabelPosition(a.arrowTypeEnd ? 10 : 0, "end_right", r);
            n = s.x, e = s.y;
        }
        c.attr("transform", `translate(${n}, ${e})`);
    }
}, "positionEdgeLabel"), D = (0, _chunkGTKDMUJJMjs.a)((a, t)=>{
    let r = a.x, p = a.y, o = Math.abs(t.x - r), c = Math.abs(t.y - p), n = a.width / 2, e = a.height / 2;
    return o >= n || c >= e;
}, "outsideNode"), K = (0, _chunkGTKDMUJJMjs.a)((a, t, r)=>{
    (0, _chunkNQURTBEVMjs.b).debug(`intersection calc abc89:
  outsidePoint: ${JSON.stringify(t)}
  insidePoint : ${JSON.stringify(r)}
  node        : x:${a.x} y:${a.y} w:${a.width} h:${a.height}`);
    let p = a.x, o = a.y, c = Math.abs(p - r.x), n = a.width / 2, e = r.x < t.x ? n - c : n + c, s = a.height / 2, i = Math.abs(t.y - r.y), l = Math.abs(t.x - r.x);
    if (Math.abs(o - t.y) * n > Math.abs(p - t.x) * s) {
        let f = r.y < t.y ? t.y - s - o : o - s - t.y;
        e = l * f / i;
        let h = {
            x: r.x < t.x ? r.x + e : r.x - l + e,
            y: r.y < t.y ? r.y + i - f : r.y - i + f
        };
        return e === 0 && (h.x = t.x, h.y = t.y), l === 0 && (h.x = t.x), i === 0 && (h.y = t.y), (0, _chunkNQURTBEVMjs.b).debug(`abc89 topp/bott calc, Q ${i}, q ${f}, R ${l}, r ${e}`, h), h;
    } else {
        r.x < t.x ? e = t.x - n - p : e = p - n - t.x;
        let f = i * e / l, h = r.x < t.x ? r.x + l - e : r.x - l + e, x = r.y < t.y ? r.y + f : r.y - f;
        return (0, _chunkNQURTBEVMjs.b).debug(`sides calc abc89, Q ${i}, q ${f}, R ${l}, r ${e}`, {
            _x: h,
            _y: x
        }), e === 0 && (h = t.x, x = t.y), l === 0 && (h = t.x), i === 0 && (x = t.y), {
            x: h,
            y: x
        };
    }
}, "intersection"), R = (0, _chunkGTKDMUJJMjs.a)((a, t)=>{
    (0, _chunkNQURTBEVMjs.b).debug("abc88 cutPathAtIntersect", a, t);
    let r = [], p = a[0], o = !1;
    return a.forEach((c)=>{
        if (!D(t, c) && !o) {
            let n = K(t, p, c), e = !1;
            r.forEach((s)=>{
                e = e || s.x === n.x && s.y === n.y;
            }), r.some((s)=>s.x === n.x && s.y === n.y) || r.push(n), o = !0;
        } else p = c, o || r.push(c);
    }), r;
}, "cutPathAtIntersect"), Lt = (0, _chunkGTKDMUJJMjs.a)(function(a, t, r, p, o, c, n) {
    let e = r.points;
    (0, _chunkNQURTBEVMjs.b).debug("abc88 InsertEdge: edge=", r, "e=", t);
    let s = !1, i = c.node(t.v);
    var l = c.node(t.w);
    l?.intersect && i?.intersect && (e = e.slice(1, r.points.length - 1), e.unshift(i.intersect(e[0])), e.push(l.intersect(e[e.length - 1]))), r.toCluster && ((0, _chunkNQURTBEVMjs.b).debug("to cluster abc88", p[r.toCluster]), e = R(r.points, p[r.toCluster].node), s = !0), r.fromCluster && ((0, _chunkNQURTBEVMjs.b).debug("from cluster abc88", p[r.fromCluster]), e = R(e.reverse(), p[r.fromCluster].node).reverse(), s = !0);
    let f = e.filter((O)=>!Number.isNaN(O.y)), h = (0, _chunkNQURTBEVMjs.Ga);
    r.curve && (o === "graph" || o === "flowchart") && (h = r.curve);
    let { x, y: X } = (0, _chunkU6LOUQAFMjs.b)(r), Y = (0, _chunkNQURTBEVMjs.Ca)().x(x).y(X).curve(h), b;
    switch(r.thickness){
        case "normal":
            b = "edge-thickness-normal";
            break;
        case "thick":
            b = "edge-thickness-thick";
            break;
        case "invisible":
            b = "edge-thickness-thick";
            break;
        default:
            b = "";
    }
    switch(r.pattern){
        case "solid":
            b += " edge-pattern-solid";
            break;
        case "dotted":
            b += " edge-pattern-dotted";
            break;
        case "dashed":
            b += " edge-pattern-dashed";
            break;
    }
    let U = a.append("path").attr("d", Y(f)).attr("id", r.id).attr("class", " " + b + (r.classes ? " " + r.classes : "")).attr("style", r.style), y = "";
    ((0, _chunkNQURTBEVMjs.X)().flowchart.arrowMarkerAbsolute || (0, _chunkNQURTBEVMjs.X)().state.arrowMarkerAbsolute) && (y = window.location.protocol + "//" + window.location.host + window.location.pathname + window.location.search, y = y.replace(/\(/g, "\\("), y = y.replace(/\)/g, "\\)")), H(U, r, y, n, o);
    let E = {};
    return s && (E.updatedPath = e), E.originalPath = r.points, E;
}, "insertEdge");

},{"./chunk-UWHJNN4Q.mjs":"6LAlC","./chunk-U6LOUQAF.mjs":"v9pSW","./chunk-KMOJB3TB.mjs":"aJH4M","./chunk-AC3VT7B7.mjs":"1eiUz","./chunk-NQURTBEV.mjs":"iASFe","./chunk-GTKDMUJJ.mjs":"fruhx","@parcel/transformer-js/src/esmodule-helpers.js":"1cdyk"}],"9GO9x":[function(require,module,exports,__globalThis) {
var parcelHelpers = require("@parcel/transformer-js/src/esmodule-helpers.js");
parcelHelpers.defineInteropFlag(exports);
parcelHelpers.export(exports, "a", ()=>l);
var _chunkBKDDFIKNMjs = require("./chunk-BKDDFIKN.mjs");
var _chunkGTKDMUJJMjs = require("./chunk-GTKDMUJJ.mjs");
function l(e) {
    var n = {
        options: {
            directed: e.isDirected(),
            multigraph: e.isMultigraph(),
            compound: e.isCompound()
        },
        nodes: f(e),
        edges: s(e)
    };
    return (0, _chunkBKDDFIKNMjs.D)(e.graph()) || (n.value = (0, _chunkBKDDFIKNMjs.e)(e.graph())), n;
}
(0, _chunkGTKDMUJJMjs.a)(l, "write");
function f(e) {
    return (0, _chunkBKDDFIKNMjs.s)(e.nodes(), function(n) {
        var r = e.node(n), a = e.parent(n), t = {
            v: n
        };
        return (0, _chunkBKDDFIKNMjs.D)(r) || (t.value = r), (0, _chunkBKDDFIKNMjs.D)(a) || (t.parent = a), t;
    });
}
(0, _chunkGTKDMUJJMjs.a)(f, "writeNodes");
function s(e) {
    return (0, _chunkBKDDFIKNMjs.s)(e.edges(), function(n) {
        var r = e.edge(n), a = {
            v: n.v,
            w: n.w
        };
        return (0, _chunkBKDDFIKNMjs.D)(n.name) || (a.name = n.name), (0, _chunkBKDDFIKNMjs.D)(r) || (a.value = r), a;
    });
}
(0, _chunkGTKDMUJJMjs.a)(s, "writeEdges");

},{"./chunk-BKDDFIKN.mjs":"hADfH","./chunk-GTKDMUJJ.mjs":"fruhx","@parcel/transformer-js/src/esmodule-helpers.js":"1cdyk"}],"fUQIF":[function(require,module,exports,__globalThis) {
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

},{"./chunk-BKDDFIKN.mjs":"hADfH","./chunk-YPUTD6PB.mjs":"aSdv1","./chunk-6BY5RJGC.mjs":"bRXnR","./chunk-GTKDMUJJ.mjs":"fruhx","@parcel/transformer-js/src/esmodule-helpers.js":"1cdyk"}]},["dCKyU"], null, "parcelRequire6955", {})

//# sourceMappingURL=classDiagram-v2-NO4EPWGV.cb00cd10.js.map
