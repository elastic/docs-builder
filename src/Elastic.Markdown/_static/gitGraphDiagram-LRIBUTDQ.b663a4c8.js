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
})({"9p73b":[function(require,module,exports,__globalThis) {
var global = arguments[3];
var HMR_HOST = null;
var HMR_PORT = 1234;
var HMR_SERVER_PORT = 1234;
var HMR_SECURE = false;
var HMR_ENV_HASH = "febfef3c5b467b16";
var HMR_USE_SSE = false;
module.bundle.HMR_BUNDLE_ID = "5cf40d1eb663a4c8";
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

},{}],"6DN5H":[function(require,module,exports,__globalThis) {
var parcelHelpers = require("@parcel/transformer-js/src/esmodule-helpers.js");
parcelHelpers.defineInteropFlag(exports);
parcelHelpers.export(exports, "diagram", ()=>Rt);
var _chunkVSLJSFIPMjs = require("./chunk-VSLJSFIP.mjs");
var _chunk4KE642EDMjs = require("./chunk-4KE642ED.mjs");
var _chunkYFFLADYNMjs = require("./chunk-YFFLADYN.mjs");
var _chunkVRGDDFRAMjs = require("./chunk-VRGDDFRA.mjs");
var _chunkE4AWDUZEMjs = require("./chunk-E4AWDUZE.mjs");
var _chunkAC3VT7B7Mjs = require("./chunk-AC3VT7B7.mjs");
var _chunkTI4EEUUGMjs = require("./chunk-TI4EEUUG.mjs");
var _chunkNQURTBEVMjs = require("./chunk-NQURTBEV.mjs");
var _chunkC7NU23FDMjs = require("./chunk-C7NU23FD.mjs");
var _chunkDZFIHE2JMjs = require("./chunk-DZFIHE2J.mjs");
var _chunkD3PZO57JMjs = require("./chunk-D3PZO57J.mjs");
var _chunkBKDDFIKNMjs = require("./chunk-BKDDFIKN.mjs");
var _chunkYPUTD6PBMjs = require("./chunk-YPUTD6PB.mjs");
var _chunk6BY5RJGCMjs = require("./chunk-6BY5RJGC.mjs");
var _chunkGTKDMUJJMjs = require("./chunk-GTKDMUJJ.mjs");
var u = {
    NORMAL: 0,
    REVERSE: 1,
    HIGHLIGHT: 2,
    MERGE: 3,
    CHERRY_PICK: 4
};
var he = (0, _chunkNQURTBEVMjs.s).gitGraph, D = (0, _chunkGTKDMUJJMjs.a)(()=>(0, _chunkAC3VT7B7Mjs.l)({
        ...he,
        ...(0, _chunkNQURTBEVMjs.A)().gitGraph
    }), "getConfig"), i = new (0, _chunkVSLJSFIPMjs.a)(()=>{
    let r = D(), e = r.mainBranchName, o = r.mainBranchOrder;
    return {
        mainBranchName: e,
        commits: new Map,
        head: null,
        branchConfig: new Map([
            [
                e,
                {
                    name: e,
                    order: o
                }
            ]
        ]),
        branches: new Map([
            [
                e,
                null
            ]
        ]),
        currBranch: e,
        direction: "LR",
        seq: 0,
        options: {}
    };
});
function q() {
    return (0, _chunkAC3VT7B7Mjs.f)({
        length: 7
    });
}
(0, _chunkGTKDMUJJMjs.a)(q, "getID");
function ge(r, e) {
    let o = Object.create(null);
    return r.reduce((a, t)=>{
        let n = e(t);
        return o[n] || (o[n] = !0, a.push(t)), a;
    }, []);
}
(0, _chunkGTKDMUJJMjs.a)(ge, "uniqBy");
var fe = (0, _chunkGTKDMUJJMjs.a)(function(r) {
    i.records.direction = r;
}, "setDirection"), le = (0, _chunkGTKDMUJJMjs.a)(function(r) {
    (0, _chunkNQURTBEVMjs.b).debug("options str", r), r = r?.trim(), r = r || "{}";
    try {
        i.records.options = JSON.parse(r);
    } catch (e) {
        (0, _chunkNQURTBEVMjs.b).error("error while parsing gitGraph options", e.message);
    }
}, "setOptions"), ye = (0, _chunkGTKDMUJJMjs.a)(function() {
    return i.records.options;
}, "getOptions"), ue = (0, _chunkGTKDMUJJMjs.a)(function(r) {
    let e = r.msg, o = r.id, a = r.type, t = r.tags;
    (0, _chunkNQURTBEVMjs.b).info("commit", e, o, a, t), (0, _chunkNQURTBEVMjs.b).debug("Entering commit:", e, o, a, t);
    let n = D();
    o = (0, _chunkNQURTBEVMjs.L).sanitizeText(o, n), e = (0, _chunkNQURTBEVMjs.L).sanitizeText(e, n), t = t?.map((s)=>(0, _chunkNQURTBEVMjs.L).sanitizeText(s, n));
    let m = {
        id: o || i.records.seq + "-" + q(),
        message: e,
        seq: i.records.seq++,
        type: a ?? u.NORMAL,
        tags: t ?? [],
        parents: i.records.head == null ? [] : [
            i.records.head.id
        ],
        branch: i.records.currBranch
    };
    i.records.head = m, (0, _chunkNQURTBEVMjs.b).info("main branch", n.mainBranchName), i.records.commits.set(m.id, m), i.records.branches.set(i.records.currBranch, m.id), (0, _chunkNQURTBEVMjs.b).debug("in pushCommit " + m.id);
}, "commit"), xe = (0, _chunkGTKDMUJJMjs.a)(function(r) {
    let e = r.name, o = r.order;
    if (e = (0, _chunkNQURTBEVMjs.L).sanitizeText(e, D()), i.records.branches.has(e)) throw new Error(`Trying to create an existing branch. (Help: Either use a new name if you want create a new branch or try using "checkout ${e}")`);
    i.records.branches.set(e, i.records.head != null ? i.records.head.id : null), i.records.branchConfig.set(e, {
        name: e,
        order: o
    }), ne(e), (0, _chunkNQURTBEVMjs.b).debug("in createBranch");
}, "branch"), $e = (0, _chunkGTKDMUJJMjs.a)((r)=>{
    let e = r.branch, o = r.id, a = r.type, t = r.tags, n = D();
    e = (0, _chunkNQURTBEVMjs.L).sanitizeText(e, n), o && (o = (0, _chunkNQURTBEVMjs.L).sanitizeText(o, n));
    let m = i.records.branches.get(i.records.currBranch), s = i.records.branches.get(e), g = m ? i.records.commits.get(m) : void 0, p = s ? i.records.commits.get(s) : void 0;
    if (g && p && g.branch === e) throw new Error(`Cannot merge branch '${e}' into itself.`);
    if (i.records.currBranch === e) {
        let c = new Error('Incorrect usage of "merge". Cannot merge a branch to itself');
        throw c.hash = {
            text: `merge ${e}`,
            token: `merge ${e}`,
            expected: [
                "branch abc"
            ]
        }, c;
    }
    if (g === void 0 || !g) {
        let c = new Error(`Incorrect usage of "merge". Current branch (${i.records.currBranch})has no commits`);
        throw c.hash = {
            text: `merge ${e}`,
            token: `merge ${e}`,
            expected: [
                "commit"
            ]
        }, c;
    }
    if (!i.records.branches.has(e)) {
        let c = new Error('Incorrect usage of "merge". Branch to be merged (' + e + ") does not exist");
        throw c.hash = {
            text: `merge ${e}`,
            token: `merge ${e}`,
            expected: [
                `branch ${e}`
            ]
        }, c;
    }
    if (p === void 0 || !p) {
        let c = new Error('Incorrect usage of "merge". Branch to be merged (' + e + ") has no commits");
        throw c.hash = {
            text: `merge ${e}`,
            token: `merge ${e}`,
            expected: [
                '"commit"'
            ]
        }, c;
    }
    if (g === p) {
        let c = new Error('Incorrect usage of "merge". Both branches have same head');
        throw c.hash = {
            text: `merge ${e}`,
            token: `merge ${e}`,
            expected: [
                "branch abc"
            ]
        }, c;
    }
    if (o && i.records.commits.has(o)) {
        let c = new Error('Incorrect usage of "merge". Commit with id:' + o + " already exists, use different custom Id");
        throw c.hash = {
            text: `merge ${e} ${o} ${a} ${t?.join(" ")}`,
            token: `merge ${e} ${o} ${a} ${t?.join(" ")}`,
            expected: [
                `merge ${e} ${o}_UNIQUE ${a} ${t?.join(" ")}`
            ]
        }, c;
    }
    let h = s || "", f = {
        id: o || `${i.records.seq}-${q()}`,
        message: `merged branch ${e} into ${i.records.currBranch}`,
        seq: i.records.seq++,
        parents: i.records.head == null ? [] : [
            i.records.head.id,
            h
        ],
        branch: i.records.currBranch,
        type: u.MERGE,
        customType: a,
        customId: !!o,
        tags: t ?? []
    };
    i.records.head = f, i.records.commits.set(f.id, f), i.records.branches.set(i.records.currBranch, f.id), (0, _chunkNQURTBEVMjs.b).debug(i.records.branches), (0, _chunkNQURTBEVMjs.b).debug("in mergeBranch");
}, "merge"), be = (0, _chunkGTKDMUJJMjs.a)(function(r) {
    let e = r.id, o = r.targetId, a = r.tags, t = r.parent;
    (0, _chunkNQURTBEVMjs.b).debug("Entering cherryPick:", e, o, a);
    let n = D();
    if (e = (0, _chunkNQURTBEVMjs.L).sanitizeText(e, n), o = (0, _chunkNQURTBEVMjs.L).sanitizeText(o, n), a = a?.map((g)=>(0, _chunkNQURTBEVMjs.L).sanitizeText(g, n)), t = (0, _chunkNQURTBEVMjs.L).sanitizeText(t, n), !e || !i.records.commits.has(e)) {
        let g = new Error('Incorrect usage of "cherryPick". Source commit id should exist and provided');
        throw g.hash = {
            text: `cherryPick ${e} ${o}`,
            token: `cherryPick ${e} ${o}`,
            expected: [
                "cherry-pick abc"
            ]
        }, g;
    }
    let m = i.records.commits.get(e);
    if (m === void 0 || !m) throw new Error('Incorrect usage of "cherryPick". Source commit id should exist and provided');
    if (t && !(Array.isArray(m.parents) && m.parents.includes(t))) throw new Error("Invalid operation: The specified parent commit is not an immediate parent of the cherry-picked commit.");
    let s = m.branch;
    if (m.type === u.MERGE && !t) throw new Error("Incorrect usage of cherry-pick: If the source commit is a merge commit, an immediate parent commit must be specified.");
    if (!o || !i.records.commits.has(o)) {
        if (s === i.records.currBranch) {
            let f = new Error('Incorrect usage of "cherryPick". Source commit is already on current branch');
            throw f.hash = {
                text: `cherryPick ${e} ${o}`,
                token: `cherryPick ${e} ${o}`,
                expected: [
                    "cherry-pick abc"
                ]
            }, f;
        }
        let g = i.records.branches.get(i.records.currBranch);
        if (g === void 0 || !g) {
            let f = new Error(`Incorrect usage of "cherry-pick". Current branch (${i.records.currBranch})has no commits`);
            throw f.hash = {
                text: `cherryPick ${e} ${o}`,
                token: `cherryPick ${e} ${o}`,
                expected: [
                    "cherry-pick abc"
                ]
            }, f;
        }
        let p = i.records.commits.get(g);
        if (p === void 0 || !p) {
            let f = new Error(`Incorrect usage of "cherry-pick". Current branch (${i.records.currBranch})has no commits`);
            throw f.hash = {
                text: `cherryPick ${e} ${o}`,
                token: `cherryPick ${e} ${o}`,
                expected: [
                    "cherry-pick abc"
                ]
            }, f;
        }
        let h = {
            id: i.records.seq + "-" + q(),
            message: `cherry-picked ${m?.message} into ${i.records.currBranch}`,
            seq: i.records.seq++,
            parents: i.records.head == null ? [] : [
                i.records.head.id,
                m.id
            ],
            branch: i.records.currBranch,
            type: u.CHERRY_PICK,
            tags: a ? a.filter(Boolean) : [
                `cherry-pick:${m.id}${m.type === u.MERGE ? `|parent:${t}` : ""}`
            ]
        };
        i.records.head = h, i.records.commits.set(h.id, h), i.records.branches.set(i.records.currBranch, h.id), (0, _chunkNQURTBEVMjs.b).debug(i.records.branches), (0, _chunkNQURTBEVMjs.b).debug("in cherryPick");
    }
}, "cherryPick"), ne = (0, _chunkGTKDMUJJMjs.a)(function(r) {
    if (r = (0, _chunkNQURTBEVMjs.L).sanitizeText(r, D()), i.records.branches.has(r)) {
        i.records.currBranch = r;
        let e = i.records.branches.get(i.records.currBranch);
        e === void 0 || !e ? i.records.head = null : i.records.head = i.records.commits.get(e) ?? null;
    } else {
        let e = new Error(`Trying to checkout branch which is not yet created. (Help try using "branch ${r}")`);
        throw e.hash = {
            text: `checkout ${r}`,
            token: `checkout ${r}`,
            expected: [
                `branch ${r}`
            ]
        }, e;
    }
}, "checkout");
function re(r, e, o) {
    let a = r.indexOf(e);
    a === -1 ? r.push(o) : r.splice(a, 1, o);
}
(0, _chunkGTKDMUJJMjs.a)(re, "upsert");
function oe(r) {
    let e = r.reduce((t, n)=>t.seq > n.seq ? t : n, r[0]), o = "";
    r.forEach(function(t) {
        t === e ? o += "	*" : o += "	|";
    });
    let a = [
        o,
        e.id,
        e.seq
    ];
    for(let t in i.records.branches)i.records.branches.get(t) === e.id && a.push(t);
    if ((0, _chunkNQURTBEVMjs.b).debug(a.join(" ")), e.parents && e.parents.length == 2 && e.parents[0] && e.parents[1]) {
        let t = i.records.commits.get(e.parents[0]);
        re(r, e, t), e.parents[1] && r.push(i.records.commits.get(e.parents[1]));
    } else {
        if (e.parents.length == 0) return;
        if (e.parents[0]) {
            let t = i.records.commits.get(e.parents[0]);
            re(r, e, t);
        }
    }
    r = ge(r, (t)=>t.id), oe(r);
}
(0, _chunkGTKDMUJJMjs.a)(oe, "prettyPrintCommitHistory");
var Ce = (0, _chunkGTKDMUJJMjs.a)(function() {
    (0, _chunkNQURTBEVMjs.b).debug(i.records.commits);
    let r = ae()[0];
    oe([
        r
    ]);
}, "prettyPrint"), Be = (0, _chunkGTKDMUJJMjs.a)(function() {
    i.reset(), (0, _chunkNQURTBEVMjs.P)();
}, "clear"), we = (0, _chunkGTKDMUJJMjs.a)(function() {
    return [
        ...i.records.branchConfig.values()
    ].map((e, o)=>e.order !== null && e.order !== void 0 ? e : {
            ...e,
            order: parseFloat(`0.${o}`)
        }).sort((e, o)=>(e.order ?? 0) - (o.order ?? 0)).map(({ name: e })=>({
            name: e
        }));
}, "getBranchesAsObjArray"), ke = (0, _chunkGTKDMUJJMjs.a)(function() {
    return i.records.branches;
}, "getBranches"), Te = (0, _chunkGTKDMUJJMjs.a)(function() {
    return i.records.commits;
}, "getCommits"), ae = (0, _chunkGTKDMUJJMjs.a)(function() {
    let r = [
        ...i.records.commits.values()
    ];
    return r.forEach(function(e) {
        (0, _chunkNQURTBEVMjs.b).debug(e.id);
    }), r.sort((e, o)=>e.seq - o.seq), r;
}, "getCommitsArray"), Ee = (0, _chunkGTKDMUJJMjs.a)(function() {
    return i.records.currBranch;
}, "getCurrentBranch"), Pe = (0, _chunkGTKDMUJJMjs.a)(function() {
    return i.records.direction;
}, "getDirection"), Me = (0, _chunkGTKDMUJJMjs.a)(function() {
    return i.records.head;
}, "getHead"), v = {
    commitType: u,
    getConfig: D,
    setDirection: fe,
    setOptions: le,
    getOptions: ye,
    commit: ue,
    branch: xe,
    merge: $e,
    cherryPick: be,
    checkout: ne,
    prettyPrint: Ce,
    clear: Be,
    getBranchesAsObjArray: we,
    getBranches: ke,
    getCommits: Te,
    getCommitsArray: ae,
    getCurrentBranch: Ee,
    getDirection: Pe,
    getHead: Me,
    setAccTitle: (0, _chunkNQURTBEVMjs.Q),
    getAccTitle: (0, _chunkNQURTBEVMjs.R),
    getAccDescription: (0, _chunkNQURTBEVMjs.T),
    setAccDescription: (0, _chunkNQURTBEVMjs.S),
    setDiagramTitle: (0, _chunkNQURTBEVMjs.U),
    getDiagramTitle: (0, _chunkNQURTBEVMjs.V)
};
var De = (0, _chunkGTKDMUJJMjs.a)((r, e)=>{
    (0, _chunk4KE642EDMjs.a)(r, e), r.dir && e.setDirection(r.dir);
    for (let o of r.statements)Ge(o, e);
}, "populate"), Ge = (0, _chunkGTKDMUJJMjs.a)((r, e)=>{
    let a = {
        Commit: (0, _chunkGTKDMUJJMjs.a)((t)=>e.commit(Le(t)), "Commit"),
        Branch: (0, _chunkGTKDMUJJMjs.a)((t)=>e.branch(Oe(t)), "Branch"),
        Merge: (0, _chunkGTKDMUJJMjs.a)((t)=>e.merge(ve(t)), "Merge"),
        Checkout: (0, _chunkGTKDMUJJMjs.a)((t)=>e.checkout(Re(t)), "Checkout"),
        CherryPicking: (0, _chunkGTKDMUJJMjs.a)((t)=>e.cherryPick(Ae(t)), "CherryPicking")
    }[r.$type];
    a ? a(r) : (0, _chunkNQURTBEVMjs.b).error(`Unknown statement type: ${r.$type}`);
}, "parseStatement"), Le = (0, _chunkGTKDMUJJMjs.a)((r)=>({
        id: r.id,
        msg: r.message ?? "",
        type: r.type !== void 0 ? u[r.type] : u.NORMAL,
        tags: r.tags ?? void 0
    }), "parseCommit"), Oe = (0, _chunkGTKDMUJJMjs.a)((r)=>({
        name: r.name,
        order: r.order ?? 0
    }), "parseBranch"), ve = (0, _chunkGTKDMUJJMjs.a)((r)=>({
        branch: r.branch,
        id: r.id ?? "",
        type: r.type !== void 0 ? u[r.type] : void 0,
        tags: r.tags ?? void 0
    }), "parseMerge"), Re = (0, _chunkGTKDMUJJMjs.a)((r)=>r.branch, "parseCheckout"), Ae = (0, _chunkGTKDMUJJMjs.a)((r)=>({
        id: r.id,
        targetId: "",
        tags: r.tags?.length === 0 ? void 0 : r.tags,
        parent: r.parent
    }), "parseCherryPicking"), se = {
    parse: (0, _chunkGTKDMUJJMjs.a)(async (r)=>{
        let e = await (0, _chunkYFFLADYNMjs.a)("gitGraph", r);
        (0, _chunkNQURTBEVMjs.b).debug(e), De(e, v);
    }, "parse")
};
var Ie = (0, _chunkNQURTBEVMjs.X)(), w = Ie?.gitGraph, P = 10, M = 40, k = 4, T = 2, G = 8, b = new Map, C = new Map, R = 30, L = new Map, A = [], E = 0, y = "LR", qe = (0, _chunkGTKDMUJJMjs.a)(()=>{
    b.clear(), C.clear(), L.clear(), E = 0, A = [], y = "LR";
}, "clear"), ce = (0, _chunkGTKDMUJJMjs.a)((r)=>{
    let e = document.createElementNS("http://www.w3.org/2000/svg", "text");
    return (typeof r == "string" ? r.split(/\\n|\n|<br\s*\/?>/gi) : r).forEach((a)=>{
        let t = document.createElementNS("http://www.w3.org/2000/svg", "tspan");
        t.setAttributeNS("http://www.w3.org/XML/1998/namespace", "xml:space", "preserve"), t.setAttribute("dy", "1em"), t.setAttribute("x", "0"), t.setAttribute("class", "row"), t.textContent = a.trim(), e.appendChild(t);
    }), e;
}, "drawText"), me = (0, _chunkGTKDMUJJMjs.a)((r)=>{
    let e, o, a;
    return y === "BT" ? (o = (0, _chunkGTKDMUJJMjs.a)((t, n)=>t <= n, "comparisonFunc"), a = 1 / 0) : (o = (0, _chunkGTKDMUJJMjs.a)((t, n)=>t >= n, "comparisonFunc"), a = 0), r.forEach((t)=>{
        let n = y === "TB" || y == "BT" ? C.get(t)?.y : C.get(t)?.x;
        n !== void 0 && o(n, a) && (e = t, a = n);
    }), e;
}, "findClosestParent"), He = (0, _chunkGTKDMUJJMjs.a)((r)=>{
    let e = "", o = 1 / 0;
    return r.forEach((a)=>{
        let t = C.get(a).y;
        t <= o && (e = a, o = t);
    }), e || void 0;
}, "findClosestParentBT"), Se = (0, _chunkGTKDMUJJMjs.a)((r, e, o)=>{
    let a = o, t = o, n = [];
    r.forEach((m)=>{
        let s = e.get(m);
        if (!s) throw new Error(`Commit not found for key ${m}`);
        s.parents.length ? (a = Ne(s), t = Math.max(a, t)) : n.push(s), _e(s, a);
    }), a = t, n.forEach((m)=>{
        je(m, a, o);
    }), r.forEach((m)=>{
        let s = e.get(m);
        if (s?.parents.length) {
            let g = He(s.parents);
            a = C.get(g).y - M, a <= t && (t = a);
            let p = b.get(s.branch).pos, h = a - P;
            C.set(s.id, {
                x: p,
                y: h
            });
        }
    });
}, "setParallelBTPos"), We = (0, _chunkGTKDMUJJMjs.a)((r)=>{
    let e = me(r.parents.filter((a)=>a !== null));
    if (!e) throw new Error(`Closest parent not found for commit ${r.id}`);
    let o = C.get(e)?.y;
    if (o === void 0) throw new Error(`Closest parent position not found for commit ${r.id}`);
    return o;
}, "findClosestParentPos"), Ne = (0, _chunkGTKDMUJJMjs.a)((r)=>We(r) + M, "calculateCommitPosition"), _e = (0, _chunkGTKDMUJJMjs.a)((r, e)=>{
    let o = b.get(r.branch);
    if (!o) throw new Error(`Branch not found for commit ${r.id}`);
    let a = o.pos, t = e + P;
    return C.set(r.id, {
        x: a,
        y: t
    }), {
        x: a,
        y: t
    };
}, "setCommitPosition"), je = (0, _chunkGTKDMUJJMjs.a)((r, e, o)=>{
    let a = b.get(r.branch);
    if (!a) throw new Error(`Branch not found for commit ${r.id}`);
    let t = e + o, n = a.pos;
    C.set(r.id, {
        x: n,
        y: t
    });
}, "setRootPosition"), Fe = (0, _chunkGTKDMUJJMjs.a)((r, e, o, a, t, n)=>{
    if (n === u.HIGHLIGHT) r.append("rect").attr("x", o.x - 10).attr("y", o.y - 10).attr("width", 20).attr("height", 20).attr("class", `commit ${e.id} commit-highlight${t % G} ${a}-outer`), r.append("rect").attr("x", o.x - 6).attr("y", o.y - 6).attr("width", 12).attr("height", 12).attr("class", `commit ${e.id} commit${t % G} ${a}-inner`);
    else if (n === u.CHERRY_PICK) r.append("circle").attr("cx", o.x).attr("cy", o.y).attr("r", 10).attr("class", `commit ${e.id} ${a}`), r.append("circle").attr("cx", o.x - 3).attr("cy", o.y + 2).attr("r", 2.75).attr("fill", "#fff").attr("class", `commit ${e.id} ${a}`), r.append("circle").attr("cx", o.x + 3).attr("cy", o.y + 2).attr("r", 2.75).attr("fill", "#fff").attr("class", `commit ${e.id} ${a}`), r.append("line").attr("x1", o.x + 3).attr("y1", o.y + 1).attr("x2", o.x).attr("y2", o.y - 5).attr("stroke", "#fff").attr("class", `commit ${e.id} ${a}`), r.append("line").attr("x1", o.x - 3).attr("y1", o.y + 1).attr("x2", o.x).attr("y2", o.y - 5).attr("stroke", "#fff").attr("class", `commit ${e.id} ${a}`);
    else {
        let m = r.append("circle");
        if (m.attr("cx", o.x), m.attr("cy", o.y), m.attr("r", e.type === u.MERGE ? 9 : 10), m.attr("class", `commit ${e.id} commit${t % G}`), n === u.MERGE) {
            let s = r.append("circle");
            s.attr("cx", o.x), s.attr("cy", o.y), s.attr("r", 6), s.attr("class", `commit ${a} ${e.id} commit${t % G}`);
        }
        n === u.REVERSE && r.append("path").attr("d", `M ${o.x - 5},${o.y - 5}L${o.x + 5},${o.y + 5}M${o.x - 5},${o.y + 5}L${o.x + 5},${o.y - 5}`).attr("class", `commit ${a} ${e.id} commit${t % G}`);
    }
}, "drawCommitBullet"), ze = (0, _chunkGTKDMUJJMjs.a)((r, e, o, a)=>{
    if (e.type !== u.CHERRY_PICK && (e.customId && e.type === u.MERGE || e.type !== u.MERGE) && w?.showCommitLabel) {
        let t = r.append("g"), n = t.insert("rect").attr("class", "commit-label-bkg"), m = t.append("text").attr("x", a).attr("y", o.y + 25).attr("class", "commit-label").text(e.id), s = m.node()?.getBBox();
        if (s && (n.attr("x", o.posWithOffset - s.width / 2 - T).attr("y", o.y + 13.5).attr("width", s.width + 2 * T).attr("height", s.height + 2 * T), y === "TB" || y === "BT" ? (n.attr("x", o.x - (s.width + 4 * k + 5)).attr("y", o.y - 12), m.attr("x", o.x - (s.width + 4 * k)).attr("y", o.y + s.height - 12)) : m.attr("x", o.posWithOffset - s.width / 2), w.rotateCommitLabel)) {
            if (y === "TB" || y === "BT") m.attr("transform", "rotate(-45, " + o.x + ", " + o.y + ")"), n.attr("transform", "rotate(-45, " + o.x + ", " + o.y + ")");
            else {
                let g = -7.5 - (s.width + 10) / 25 * 9.5, p = 10 + s.width / 25 * 8.5;
                t.attr("transform", "translate(" + g + ", " + p + ") rotate(-45, " + a + ", " + o.y + ")");
            }
        }
    }
}, "drawCommitLabel"), Ke = (0, _chunkGTKDMUJJMjs.a)((r, e, o, a)=>{
    if (e.tags.length > 0) {
        let t = 0, n = 0, m = 0, s = [];
        for (let g of e.tags.reverse()){
            let p = r.insert("polygon"), h = r.append("circle"), f = r.append("text").attr("y", o.y - 16 - t).attr("class", "tag-label").text(g), c = f.node()?.getBBox();
            if (!c) throw new Error("Tag bbox not found");
            n = Math.max(n, c.width), m = Math.max(m, c.height), f.attr("x", o.posWithOffset - c.width / 2), s.push({
                tag: f,
                hole: h,
                rect: p,
                yOffset: t
            }), t += 20;
        }
        for (let { tag: g, hole: p, rect: h, yOffset: f } of s){
            let c = m / 2, l = o.y - 19.2 - f;
            if (h.attr("class", "tag-label-bkg").attr("points", `
      ${a - n / 2 - k / 2},${l + T}  
      ${a - n / 2 - k / 2},${l - T}
      ${o.posWithOffset - n / 2 - k},${l - c - T}
      ${o.posWithOffset + n / 2 + k},${l - c - T}
      ${o.posWithOffset + n / 2 + k},${l + c + T}
      ${o.posWithOffset - n / 2 - k},${l + c + T}`), p.attr("cy", l).attr("cx", a - n / 2 + k / 2).attr("r", 1.5).attr("class", "tag-hole"), y === "TB" || y === "BT") {
                let x = a + f;
                h.attr("class", "tag-label-bkg").attr("points", `
        ${o.x},${x + 2}
        ${o.x},${x - 2}
        ${o.x + P},${x - c - 2}
        ${o.x + P + n + 4},${x - c - 2}
        ${o.x + P + n + 4},${x + c + 2}
        ${o.x + P},${x + c + 2}`).attr("transform", "translate(12,12) rotate(45, " + o.x + "," + a + ")"), p.attr("cx", o.x + k / 2).attr("cy", x).attr("transform", "translate(12,12) rotate(45, " + o.x + "," + a + ")"), g.attr("x", o.x + 5).attr("y", x + 3).attr("transform", "translate(14,14) rotate(45, " + o.x + "," + a + ")");
            }
        }
    }
}, "drawCommitTags"), Ue = (0, _chunkGTKDMUJJMjs.a)((r)=>{
    switch(r.customType ?? r.type){
        case u.NORMAL:
            return "commit-normal";
        case u.REVERSE:
            return "commit-reverse";
        case u.HIGHLIGHT:
            return "commit-highlight";
        case u.MERGE:
            return "commit-merge";
        case u.CHERRY_PICK:
            return "commit-cherry-pick";
        default:
            return "commit-normal";
    }
}, "getCommitClassType"), Ve = (0, _chunkGTKDMUJJMjs.a)((r, e, o, a)=>{
    let t = {
        x: 0,
        y: 0
    };
    if (r.parents.length > 0) {
        let n = me(r.parents);
        if (n) {
            let m = a.get(n) ?? t;
            return e === "TB" ? m.y + M : e === "BT" ? (a.get(r.id) ?? t).y - M : m.x + M;
        }
    } else return e === "TB" ? R : e === "BT" ? (a.get(r.id) ?? t).y - M : 0;
    return 0;
}, "calculatePosition"), Ye = (0, _chunkGTKDMUJJMjs.a)((r, e, o)=>{
    let a = y === "BT" && o ? e : e + P, t = y === "TB" || y === "BT" ? a : b.get(r.branch)?.pos, n = y === "TB" || y === "BT" ? b.get(r.branch)?.pos : a;
    if (n === void 0 || t === void 0) throw new Error(`Position were undefined for commit ${r.id}`);
    return {
        x: n,
        y: t,
        posWithOffset: a
    };
}, "getCommitPosition"), ie = (0, _chunkGTKDMUJJMjs.a)((r, e, o)=>{
    if (!w) throw new Error("GitGraph config not found");
    let a = r.append("g").attr("class", "commit-bullets"), t = r.append("g").attr("class", "commit-labels"), n = y === "TB" || y === "BT" ? R : 0, m = [
        ...e.keys()
    ], s = w?.parallelCommits ?? !1, g = (0, _chunkGTKDMUJJMjs.a)((h, f)=>{
        let c = e.get(h)?.seq, l = e.get(f)?.seq;
        return c !== void 0 && l !== void 0 ? c - l : 0;
    }, "sortKeys"), p = m.sort(g);
    y === "BT" && (s && Se(p, e, n), p = p.reverse()), p.forEach((h)=>{
        let f = e.get(h);
        if (!f) throw new Error(`Commit not found for key ${h}`);
        s && (n = Ve(f, y, n, C));
        let c = Ye(f, n, s);
        if (o) {
            let l = Ue(f), x = f.customType ?? f.type, I = b.get(f.branch)?.index ?? 0;
            Fe(a, f, c, l, I, x), ze(t, f, c, n), Ke(t, f, c, n);
        }
        y === "TB" || y === "BT" ? C.set(f.id, {
            x: c.x,
            y: c.posWithOffset
        }) : C.set(f.id, {
            x: c.posWithOffset,
            y: c.y
        }), n = y === "BT" && s ? n + M : n + M + P, n > E && (E = n);
    });
}, "drawCommits"), Ze = (0, _chunkGTKDMUJJMjs.a)((r, e, o, a, t)=>{
    let m = (y === "TB" || y === "BT" ? o.x < a.x : o.y < a.y) ? e.branch : r.branch, s = (0, _chunkGTKDMUJJMjs.a)((p)=>p.branch === m, "isOnBranchToGetCurve"), g = (0, _chunkGTKDMUJJMjs.a)((p)=>p.seq > r.seq && p.seq < e.seq, "isBetweenCommits");
    return [
        ...t.values()
    ].some((p)=>g(p) && s(p));
}, "shouldRerouteArrow"), O = (0, _chunkGTKDMUJJMjs.a)((r, e, o = 0)=>{
    let a = r + Math.abs(r - e) / 2;
    if (o > 5) return a;
    if (A.every((m)=>Math.abs(m - a) >= 10)) return A.push(a), a;
    let n = Math.abs(r - e);
    return O(r, e - n / 5, o + 1);
}, "findLane"), Je = (0, _chunkGTKDMUJJMjs.a)((r, e, o, a)=>{
    let t = C.get(e.id), n = C.get(o.id);
    if (t === void 0 || n === void 0) throw new Error(`Commit positions not found for commits ${e.id} and ${o.id}`);
    let m = Ze(e, o, t, n, a), s = "", g = "", p = 0, h = 0, f = b.get(o.branch)?.index;
    o.type === u.MERGE && e.id !== o.parents[0] && (f = b.get(e.branch)?.index);
    let c;
    if (m) {
        s = "A 10 10, 0, 0, 0,", g = "A 10 10, 0, 0, 1,", p = 10, h = 10;
        let l = t.y < n.y ? O(t.y, n.y) : O(n.y, t.y), x = t.x < n.x ? O(t.x, n.x) : O(n.x, t.x);
        y === "TB" ? t.x < n.x ? c = `M ${t.x} ${t.y} L ${x - p} ${t.y} ${g} ${x} ${t.y + h} L ${x} ${n.y - p} ${s} ${x + h} ${n.y} L ${n.x} ${n.y}` : (f = b.get(e.branch)?.index, c = `M ${t.x} ${t.y} L ${x + p} ${t.y} ${s} ${x} ${t.y + h} L ${x} ${n.y - p} ${g} ${x - h} ${n.y} L ${n.x} ${n.y}`) : y === "BT" ? t.x < n.x ? c = `M ${t.x} ${t.y} L ${x - p} ${t.y} ${s} ${x} ${t.y - h} L ${x} ${n.y + p} ${g} ${x + h} ${n.y} L ${n.x} ${n.y}` : (f = b.get(e.branch)?.index, c = `M ${t.x} ${t.y} L ${x + p} ${t.y} ${g} ${x} ${t.y - h} L ${x} ${n.y + p} ${s} ${x - h} ${n.y} L ${n.x} ${n.y}`) : t.y < n.y ? c = `M ${t.x} ${t.y} L ${t.x} ${l - p} ${s} ${t.x + h} ${l} L ${n.x - p} ${l} ${g} ${n.x} ${l + h} L ${n.x} ${n.y}` : (f = b.get(e.branch)?.index, c = `M ${t.x} ${t.y} L ${t.x} ${l + p} ${g} ${t.x + h} ${l} L ${n.x - p} ${l} ${s} ${n.x} ${l - h} L ${n.x} ${n.y}`);
    } else s = "A 20 20, 0, 0, 0,", g = "A 20 20, 0, 0, 1,", p = 20, h = 20, y === "TB" ? (t.x < n.x && (o.type === u.MERGE && e.id !== o.parents[0] ? c = `M ${t.x} ${t.y} L ${t.x} ${n.y - p} ${s} ${t.x + h} ${n.y} L ${n.x} ${n.y}` : c = `M ${t.x} ${t.y} L ${n.x - p} ${t.y} ${g} ${n.x} ${t.y + h} L ${n.x} ${n.y}`), t.x > n.x && (s = "A 20 20, 0, 0, 0,", g = "A 20 20, 0, 0, 1,", p = 20, h = 20, o.type === u.MERGE && e.id !== o.parents[0] ? c = `M ${t.x} ${t.y} L ${t.x} ${n.y - p} ${g} ${t.x - h} ${n.y} L ${n.x} ${n.y}` : c = `M ${t.x} ${t.y} L ${n.x + p} ${t.y} ${s} ${n.x} ${t.y + h} L ${n.x} ${n.y}`), t.x === n.x && (c = `M ${t.x} ${t.y} L ${n.x} ${n.y}`)) : y === "BT" ? (t.x < n.x && (o.type === u.MERGE && e.id !== o.parents[0] ? c = `M ${t.x} ${t.y} L ${t.x} ${n.y + p} ${g} ${t.x + h} ${n.y} L ${n.x} ${n.y}` : c = `M ${t.x} ${t.y} L ${n.x - p} ${t.y} ${s} ${n.x} ${t.y - h} L ${n.x} ${n.y}`), t.x > n.x && (s = "A 20 20, 0, 0, 0,", g = "A 20 20, 0, 0, 1,", p = 20, h = 20, o.type === u.MERGE && e.id !== o.parents[0] ? c = `M ${t.x} ${t.y} L ${t.x} ${n.y + p} ${s} ${t.x - h} ${n.y} L ${n.x} ${n.y}` : c = `M ${t.x} ${t.y} L ${n.x - p} ${t.y} ${s} ${n.x} ${t.y - h} L ${n.x} ${n.y}`), t.x === n.x && (c = `M ${t.x} ${t.y} L ${n.x} ${n.y}`)) : (t.y < n.y && (o.type === u.MERGE && e.id !== o.parents[0] ? c = `M ${t.x} ${t.y} L ${n.x - p} ${t.y} ${g} ${n.x} ${t.y + h} L ${n.x} ${n.y}` : c = `M ${t.x} ${t.y} L ${t.x} ${n.y - p} ${s} ${t.x + h} ${n.y} L ${n.x} ${n.y}`), t.y > n.y && (o.type === u.MERGE && e.id !== o.parents[0] ? c = `M ${t.x} ${t.y} L ${n.x - p} ${t.y} ${s} ${n.x} ${t.y - h} L ${n.x} ${n.y}` : c = `M ${t.x} ${t.y} L ${t.x} ${n.y + p} ${g} ${t.x + h} ${n.y} L ${n.x} ${n.y}`), t.y === n.y && (c = `M ${t.x} ${t.y} L ${n.x} ${n.y}`));
    if (c === void 0) throw new Error("Line definition not found");
    r.append("path").attr("d", c).attr("class", "arrow arrow" + f % G);
}, "drawArrow"), Xe = (0, _chunkGTKDMUJJMjs.a)((r, e)=>{
    let o = r.append("g").attr("class", "commit-arrows");
    [
        ...e.keys()
    ].forEach((a)=>{
        let t = e.get(a);
        t.parents && t.parents.length > 0 && t.parents.forEach((n)=>{
            Je(o, e.get(n), t, e);
        });
    });
}, "drawArrows"), Qe = (0, _chunkGTKDMUJJMjs.a)((r, e)=>{
    let o = r.append("g");
    e.forEach((a, t)=>{
        let n = t % G, m = b.get(a.name)?.pos;
        if (m === void 0) throw new Error(`Position not found for branch ${a.name}`);
        let s = o.append("line");
        s.attr("x1", 0), s.attr("y1", m), s.attr("x2", E), s.attr("y2", m), s.attr("class", "branch branch" + n), y === "TB" ? (s.attr("y1", R), s.attr("x1", m), s.attr("y2", E), s.attr("x2", m)) : y === "BT" && (s.attr("y1", E), s.attr("x1", m), s.attr("y2", R), s.attr("x2", m)), A.push(m);
        let g = a.name, p = ce(g), h = o.insert("rect"), c = o.insert("g").attr("class", "branchLabel").insert("g").attr("class", "label branch-label" + n);
        c.node().appendChild(p);
        let l = p.getBBox();
        h.attr("class", "branchLabelBkg label" + n).attr("rx", 4).attr("ry", 4).attr("x", -l.width - 4 - (w?.rotateCommitLabel === !0 ? 30 : 0)).attr("y", -l.height / 2 + 8).attr("width", l.width + 18).attr("height", l.height + 4), c.attr("transform", "translate(" + (-l.width - 14 - (w?.rotateCommitLabel === !0 ? 30 : 0)) + ", " + (m - l.height / 2 - 1) + ")"), y === "TB" ? (h.attr("x", m - l.width / 2 - 10).attr("y", 0), c.attr("transform", "translate(" + (m - l.width / 2 - 5) + ", 0)")) : y === "BT" ? (h.attr("x", m - l.width / 2 - 10).attr("y", E), c.attr("transform", "translate(" + (m - l.width / 2 - 5) + ", " + E + ")")) : h.attr("transform", "translate(-19, " + (m - l.height / 2) + ")");
    });
}, "drawBranches"), et = (0, _chunkGTKDMUJJMjs.a)(function(r, e, o, a, t) {
    return b.set(r, {
        pos: e,
        index: o
    }), e += 50 + (t ? 40 : 0) + (y === "TB" || y === "BT" ? a.width / 2 : 0), e;
}, "setBranchPosition"), tt = (0, _chunkGTKDMUJJMjs.a)(function(r, e, o, a) {
    if (qe(), (0, _chunkNQURTBEVMjs.b).debug("in gitgraph renderer", r + `
`, "id:", e, o), !w) throw new Error("GitGraph config not found");
    let t = w.rotateCommitLabel ?? !1, n = a.db;
    L = n.getCommits();
    let m = n.getBranchesAsObjArray();
    y = n.getDirection();
    let s = (0, _chunkNQURTBEVMjs.fa)(`[id="${e}"]`), g = 0;
    m.forEach((p, h)=>{
        let f = ce(p.name), c = s.append("g"), l = c.insert("g").attr("class", "branchLabel"), x = l.insert("g").attr("class", "label branch-label");
        x.node()?.appendChild(f);
        let I = f.getBBox();
        g = et(p.name, g, h, I, t), x.remove(), l.remove(), c.remove();
    }), ie(s, L, !1), w.showBranches && Qe(s, m), Xe(s, L), ie(s, L, !0), (0, _chunkAC3VT7B7Mjs.m).insertTitle(s, "gitTitleText", w.titleTopMargin ?? 0, n.getDiagramTitle()), (0, _chunkNQURTBEVMjs._)(void 0, s, w.diagramPadding, w.useMaxWidth);
}, "draw"), de = {
    draw: tt
};
var rt = (0, _chunkGTKDMUJJMjs.a)((r)=>`
  .commit-id,
  .commit-msg,
  .branch-label {
    fill: lightgrey;
    color: lightgrey;
    font-family: 'trebuchet ms', verdana, arial, sans-serif;
    font-family: var(--mermaid-font-family);
  }
  ${[
        0,
        1,
        2,
        3,
        4,
        5,
        6,
        7
    ].map((e)=>`
        .branch-label${e} { fill: ${r["gitBranchLabel" + e]}; }
        .commit${e} { stroke: ${r["git" + e]}; fill: ${r["git" + e]}; }
        .commit-highlight${e} { stroke: ${r["gitInv" + e]}; fill: ${r["gitInv" + e]}; }
        .label${e}  { fill: ${r["git" + e]}; }
        .arrow${e} { stroke: ${r["git" + e]}; }
        `).join(`
`)}

  .branch {
    stroke-width: 1;
    stroke: ${r.lineColor};
    stroke-dasharray: 2;
  }
  .commit-label { font-size: ${r.commitLabelFontSize}; fill: ${r.commitLabelColor};}
  .commit-label-bkg { font-size: ${r.commitLabelFontSize}; fill: ${r.commitLabelBackground}; opacity: 0.5; }
  .tag-label { font-size: ${r.tagLabelFontSize}; fill: ${r.tagLabelColor};}
  .tag-label-bkg { fill: ${r.tagLabelBackground}; stroke: ${r.tagLabelBorder}; }
  .tag-hole { fill: ${r.textColor}; }

  .commit-merge {
    stroke: ${r.primaryColor};
    fill: ${r.primaryColor};
  }
  .commit-reverse {
    stroke: ${r.primaryColor};
    fill: ${r.primaryColor};
    stroke-width: 3;
  }
  .commit-highlight-outer {
  }
  .commit-highlight-inner {
    stroke: ${r.primaryColor};
    fill: ${r.primaryColor};
  }

  .arrow { stroke-width: 8; stroke-linecap: round; fill: none}
  .gitTitleText {
    text-anchor: middle;
    font-size: 18px;
    fill: ${r.textColor};
  }
`, "getStyles"), pe = rt;
var Rt = {
    parser: se,
    db: v,
    renderer: de,
    styles: pe
};

},{"./chunk-VSLJSFIP.mjs":"4evSm","./chunk-4KE642ED.mjs":"1jwXW","./chunk-YFFLADYN.mjs":"9xcsl","./chunk-VRGDDFRA.mjs":"bifpI","./chunk-E4AWDUZE.mjs":"7HbS4","./chunk-AC3VT7B7.mjs":"1eiUz","./chunk-TI4EEUUG.mjs":"8SKrN","./chunk-NQURTBEV.mjs":"iASFe","./chunk-C7NU23FD.mjs":"ikZMq","./chunk-DZFIHE2J.mjs":"1F4cH","./chunk-D3PZO57J.mjs":"hvYhl","./chunk-BKDDFIKN.mjs":"hADfH","./chunk-YPUTD6PB.mjs":"aSdv1","./chunk-6BY5RJGC.mjs":"bRXnR","./chunk-GTKDMUJJ.mjs":"fruhx","@parcel/transformer-js/src/esmodule-helpers.js":"1cdyk"}],"4evSm":[function(require,module,exports,__globalThis) {
var parcelHelpers = require("@parcel/transformer-js/src/esmodule-helpers.js");
parcelHelpers.defineInteropFlag(exports);
parcelHelpers.export(exports, "a", ()=>s);
var _chunkGTKDMUJJMjs = require("./chunk-GTKDMUJJ.mjs");
var s = class {
    constructor(t){
        this.init = t;
        this.records = this.init();
    }
    static #_ = (0, _chunkGTKDMUJJMjs.a)(this, "ImperativeState");
    reset() {
        this.records = this.init();
    }
};

},{"./chunk-GTKDMUJJ.mjs":"fruhx","@parcel/transformer-js/src/esmodule-helpers.js":"1cdyk"}],"1jwXW":[function(require,module,exports,__globalThis) {
var parcelHelpers = require("@parcel/transformer-js/src/esmodule-helpers.js");
parcelHelpers.defineInteropFlag(exports);
parcelHelpers.export(exports, "a", ()=>c);
var _chunkGTKDMUJJMjs = require("./chunk-GTKDMUJJ.mjs");
function c(i, e) {
    i.accDescr && e.setAccDescription?.(i.accDescr), i.accTitle && e.setAccTitle?.(i.accTitle), i.title && e.setDiagramTitle?.(i.title);
}
(0, _chunkGTKDMUJJMjs.a)(c, "populateCommonDb");

},{"./chunk-GTKDMUJJ.mjs":"fruhx","@parcel/transformer-js/src/esmodule-helpers.js":"1cdyk"}]},["9p73b"], null, "parcelRequire6955", {})

//# sourceMappingURL=gitGraphDiagram-LRIBUTDQ.b663a4c8.js.map
