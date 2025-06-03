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
})({"6VITu":[function(require,module,exports,__globalThis) {
var global = arguments[3];
var HMR_HOST = null;
var HMR_PORT = 1234;
var HMR_SERVER_PORT = 1234;
var HMR_SECURE = false;
var HMR_ENV_HASH = "febfef3c5b467b16";
var HMR_USE_SSE = false;
module.bundle.HMR_BUNDLE_ID = "91b0c0042b3d9266";
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

},{}],"a3nb9":[function(require,module,exports,__globalThis) {
var parcelHelpers = require("@parcel/transformer-js/src/esmodule-helpers.js");
parcelHelpers.defineInteropFlag(exports);
parcelHelpers.export(exports, "render", ()=>be);
var _chunk4BPNZXC3Mjs = require("./chunk-4BPNZXC3.mjs");
var _chunkRRFB4HDSMjs = require("./chunk-RRFB4HDS.mjs");
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
var l = new Map, p = new Map, z = new Map, K = (0, _chunkGTKDMUJJMjs.a)(()=>{
    p.clear(), z.clear(), l.clear();
}, "clear"), S = (0, _chunkGTKDMUJJMjs.a)((n, t)=>{
    let e = p.get(t) || [];
    return (0, _chunkNQURTBEVMjs.b).trace("In isDescendant", t, " ", n, " = ", e.includes(n)), e.includes(n);
}, "isDescendant"), ne = (0, _chunkGTKDMUJJMjs.a)((n, t)=>{
    let e = p.get(t) || [];
    return (0, _chunkNQURTBEVMjs.b).info("Descendants of ", t, " is ", e), (0, _chunkNQURTBEVMjs.b).info("Edge is ", n), n.v === t || n.w === t ? !1 : e ? e.includes(n.v) || S(n.v, t) || S(n.w, t) || e.includes(n.w) : ((0, _chunkNQURTBEVMjs.b).debug("Tilt, ", t, ",not in descendants"), !1);
}, "edgeInCluster"), Q = (0, _chunkGTKDMUJJMjs.a)((n, t, e, c)=>{
    (0, _chunkNQURTBEVMjs.b).warn("Copying children of ", n, "root", c, "data", t.node(n), c);
    let r = t.children(n) || [];
    n !== c && r.push(n), (0, _chunkNQURTBEVMjs.b).warn("Copying (nodes) clusterId", n, "nodes", r), r.forEach((o)=>{
        if (t.children(o).length > 0) Q(o, t, e, c);
        else {
            let f = t.node(o);
            (0, _chunkNQURTBEVMjs.b).info("cp ", o, " to ", c, " with parent ", n), e.setNode(o, f), c !== t.parent(o) && ((0, _chunkNQURTBEVMjs.b).warn("Setting parent", o, t.parent(o)), e.setParent(o, t.parent(o))), n !== c && o !== n ? ((0, _chunkNQURTBEVMjs.b).debug("Setting parent", o, n), e.setParent(o, n)) : ((0, _chunkNQURTBEVMjs.b).info("In copy ", n, "root", c, "data", t.node(n), c), (0, _chunkNQURTBEVMjs.b).debug("Not Setting parent for node=", o, "cluster!==rootId", n !== c, "node!==clusterId", o !== n));
            let g = t.edges(o);
            (0, _chunkNQURTBEVMjs.b).debug("Copying Edges", g), g.forEach((a)=>{
                (0, _chunkNQURTBEVMjs.b).info("Edge", a);
                let m = t.edge(a.v, a.w, a.name);
                (0, _chunkNQURTBEVMjs.b).info("Edge data", m, c);
                try {
                    ne(a, c) ? ((0, _chunkNQURTBEVMjs.b).info("Copying as ", a.v, a.w, m, a.name), e.setEdge(a.v, a.w, m, a.name), (0, _chunkNQURTBEVMjs.b).info("newGraph edges ", e.edges(), e.edge(e.edges()[0]))) : (0, _chunkNQURTBEVMjs.b).info("Skipping copy of edge ", a.v, "-->", a.w, " rootId: ", c, " clusterId:", n);
                } catch (b) {
                    (0, _chunkNQURTBEVMjs.b).error(b);
                }
            });
        }
        (0, _chunkNQURTBEVMjs.b).debug("Removing node", o), t.removeNode(o);
    });
}, "copy"), U = (0, _chunkGTKDMUJJMjs.a)((n, t)=>{
    let e = t.children(n), c = [
        ...e
    ];
    for (let r of e)z.set(r, n), c = [
        ...c,
        ...U(r, t)
    ];
    return c;
}, "extractDescendants");
var te = (0, _chunkGTKDMUJJMjs.a)((n, t, e)=>{
    let c = n.edges().filter((a)=>a.v === t || a.w === t), r = n.edges().filter((a)=>a.v === e || a.w === e), o = c.map((a)=>({
            v: a.v === t ? e : a.v,
            w: a.w === t ? t : a.w
        })), f = r.map((a)=>({
            v: a.v,
            w: a.w
        }));
    return o.filter((a)=>f.some((m)=>a.v === m.v && a.w === m.w));
}, "findCommonEdges"), y = (0, _chunkGTKDMUJJMjs.a)((n, t, e)=>{
    let c = t.children(n);
    if ((0, _chunkNQURTBEVMjs.b).trace("Searching children of id ", n, c), c.length < 1) return n;
    let r;
    for (let o of c){
        let f = y(o, t, e), g = te(t, e, f);
        if (f) {
            if (g.length > 0) r = f;
            else return f;
        }
    }
    return r;
}, "findNonClusterChild"), C = (0, _chunkGTKDMUJJMjs.a)((n)=>!l.has(n) || !l.get(n).externalConnections ? n : l.has(n) ? l.get(n).id : n, "getAnchorId"), V = (0, _chunkGTKDMUJJMjs.a)((n, t)=>{
    if (!n || t > 10) {
        (0, _chunkNQURTBEVMjs.b).debug("Opting out, no graph ");
        return;
    } else (0, _chunkNQURTBEVMjs.b).debug("Opting in, graph ");
    n.nodes().forEach(function(e) {
        n.children(e).length > 0 && ((0, _chunkNQURTBEVMjs.b).warn("Cluster identified", e, " Replacement id in edges: ", y(e, n, e)), p.set(e, U(e, n)), l.set(e, {
            id: y(e, n, e),
            clusterData: n.node(e)
        }));
    }), n.nodes().forEach(function(e) {
        let c = n.children(e), r = n.edges();
        c.length > 0 ? ((0, _chunkNQURTBEVMjs.b).debug("Cluster identified", e, p), r.forEach((o)=>{
            let f = S(o.v, e), g = S(o.w, e);
            f ^ g && ((0, _chunkNQURTBEVMjs.b).warn("Edge: ", o, " leaves cluster ", e), (0, _chunkNQURTBEVMjs.b).warn("Descendants of XXX ", e, ": ", p.get(e)), l.get(e).externalConnections = !0);
        })) : (0, _chunkNQURTBEVMjs.b).debug("Not a cluster ", e, p);
    });
    for (let e of l.keys()){
        let c = l.get(e).id, r = n.parent(c);
        r !== e && l.has(r) && !l.get(r).externalConnections && (l.get(e).id = r);
    }
    n.edges().forEach(function(e) {
        let c = n.edge(e);
        (0, _chunkNQURTBEVMjs.b).warn("Edge " + e.v + " -> " + e.w + ": " + JSON.stringify(e)), (0, _chunkNQURTBEVMjs.b).warn("Edge " + e.v + " -> " + e.w + ": " + JSON.stringify(n.edge(e)));
        let r = e.v, o = e.w;
        if ((0, _chunkNQURTBEVMjs.b).warn("Fix XXX", l, "ids:", e.v, e.w, "Translating: ", l.get(e.v), " --- ", l.get(e.w)), l.get(e.v) && l.get(e.w) && l.get(e.v) === l.get(e.w)) {
            (0, _chunkNQURTBEVMjs.b).warn("Fixing and trying link to self - removing XXX", e.v, e.w, e.name), (0, _chunkNQURTBEVMjs.b).warn("Fixing and trying - removing XXX", e.v, e.w, e.name), r = C(e.v), o = C(e.w), n.removeEdge(e.v, e.w, e.name);
            let f = e.w + "---" + e.v + "---1", g = e.w + "---" + e.v + "---2";
            n.setNode(f, {
                domId: f,
                id: f,
                labelStyle: "",
                label: "",
                padding: 0,
                shape: "labelRect",
                style: "",
                width: 10,
                height: 10
            }), n.setNode(g, {
                domId: g,
                id: g,
                labelStyle: "",
                padding: 0,
                shape: "labelRect",
                style: "",
                width: 10,
                height: 10
            });
            let a = structuredClone(c), m = structuredClone(c), b = structuredClone(c);
            a.label = "", a.arrowTypeEnd = "none", a.id = e.name + "-cyclic-special-1", m.arrowTypeEnd = "none", m.id = e.name + "-cyclic-special-mid", b.label = "", a.fromCluster = e.v, b.toCluster = e.v, b.id = e.name + "-cyclic-special-2", n.setEdge(r, f, a, e.name + "-cyclic-special-0"), n.setEdge(f, g, m, e.name + "-cyclic-special-1"), n.setEdge(g, o, b, e.name + "-cyclic-special-2");
        } else if (l.get(e.v) || l.get(e.w)) {
            if ((0, _chunkNQURTBEVMjs.b).warn("Fixing and trying - removing XXX", e.v, e.w, e.name), r = C(e.v), o = C(e.w), n.removeEdge(e.v, e.w, e.name), r !== e.v) {
                let f = n.parent(r);
                l.get(f).externalConnections = !0, c.fromCluster = e.v;
            }
            if (o !== e.w) {
                let f = n.parent(o);
                l.get(f).externalConnections = !0, c.toCluster = e.w;
            }
            (0, _chunkNQURTBEVMjs.b).warn("Fix Replacing with XXX", r, o, e.name), n.setEdge(r, o, c, e.name);
        }
    }), (0, _chunkNQURTBEVMjs.b).warn("Adjusted Graph", (0, _chunk4BPNZXC3Mjs.a)(n)), W(n, 0), (0, _chunkNQURTBEVMjs.b).trace(l);
}, "adjustClustersAndEdges"), W = (0, _chunkGTKDMUJJMjs.a)((n, t)=>{
    if ((0, _chunkNQURTBEVMjs.b).warn("extractor - ", t, (0, _chunk4BPNZXC3Mjs.a)(n), n.children("D")), t > 10) {
        (0, _chunkNQURTBEVMjs.b).error("Bailing out");
        return;
    }
    let e = n.nodes(), c = !1;
    for (let r of e){
        let o = n.children(r);
        c = c || o.length > 0;
    }
    if (!c) {
        (0, _chunkNQURTBEVMjs.b).debug("Done, no node has children", n.nodes());
        return;
    }
    (0, _chunkNQURTBEVMjs.b).debug("Nodes = ", e, t);
    for (let r of e)if ((0, _chunkNQURTBEVMjs.b).debug("Extracting node", r, l, l.has(r) && !l.get(r).externalConnections, !n.parent(r), n.node(r), n.children("D"), " Depth ", t), !l.has(r)) (0, _chunkNQURTBEVMjs.b).debug("Not a cluster", r, t);
    else if (!l.get(r).externalConnections && n.children(r) && n.children(r).length > 0) {
        (0, _chunkNQURTBEVMjs.b).warn("Cluster without external connections, without a parent and with children", r, t);
        let f = n.graph().rankdir === "TB" ? "LR" : "TB";
        l.get(r)?.clusterData?.dir && (f = l.get(r).clusterData.dir, (0, _chunkNQURTBEVMjs.b).warn("Fixing dir", l.get(r).clusterData.dir, f));
        let g = new (0, _chunk6XGRHI2AMjs.a)({
            multigraph: !0,
            compound: !0
        }).setGraph({
            rankdir: f,
            nodesep: 50,
            ranksep: 50,
            marginx: 8,
            marginy: 8
        }).setDefaultEdgeLabel(function() {
            return {};
        });
        (0, _chunkNQURTBEVMjs.b).warn("Old graph before copy", (0, _chunk4BPNZXC3Mjs.a)(n)), Q(r, n, g, r), n.setNode(r, {
            clusterNode: !0,
            id: r,
            clusterData: l.get(r).clusterData,
            label: l.get(r).label,
            graph: g
        }), (0, _chunkNQURTBEVMjs.b).warn("New graph after copy node: (", r, ")", (0, _chunk4BPNZXC3Mjs.a)(g)), (0, _chunkNQURTBEVMjs.b).debug("Old graph after copy", (0, _chunk4BPNZXC3Mjs.a)(n));
    } else (0, _chunkNQURTBEVMjs.b).warn("Cluster ** ", r, " **not meeting the criteria !externalConnections:", !l.get(r).externalConnections, " no parent: ", !n.parent(r), " children ", n.children(r) && n.children(r).length > 0, n.children("D"), t), (0, _chunkNQURTBEVMjs.b).debug(l);
    e = n.nodes(), (0, _chunkNQURTBEVMjs.b).warn("New list of nodes", e);
    for (let r of e){
        let o = n.node(r);
        (0, _chunkNQURTBEVMjs.b).warn(" Now next level", r, o), o.clusterNode && W(o.graph, t + 1);
    }
}, "extractor"), Z = (0, _chunkGTKDMUJJMjs.a)((n, t)=>{
    if (t.length === 0) return [];
    let e = Object.assign([], t);
    return t.forEach((c)=>{
        let r = n.children(c), o = Z(n, r);
        e = [
            ...e,
            ...o
        ];
    }), e;
}, "sorter"), $ = (0, _chunkGTKDMUJJMjs.a)((n)=>Z(n, n.children()), "sortNodesByHierarchy");
var L = (0, _chunkGTKDMUJJMjs.a)(async (n, t, e, c, r, o)=>{
    (0, _chunkNQURTBEVMjs.b).info("Graph in recursive render: XXX", (0, _chunk4BPNZXC3Mjs.a)(t), r);
    let f = t.graph().rankdir;
    (0, _chunkNQURTBEVMjs.b).trace("Dir in recursive render - dir:", f);
    let g = n.insert("g").attr("class", "root");
    t.nodes() ? (0, _chunkNQURTBEVMjs.b).info("Recursive render XXX", t.nodes()) : (0, _chunkNQURTBEVMjs.b).info("No nodes found for", t), t.edges().length > 0 && (0, _chunkNQURTBEVMjs.b).info("Recursive edges", t.edge(t.edges()[0]));
    let a = g.insert("g").attr("class", "clusters"), m = g.insert("g").attr("class", "edgePaths"), b = g.insert("g").attr("class", "edgeLabels"), O = g.insert("g").attr("class", "nodes");
    await Promise.all(t.nodes().map(async function(d) {
        let s = t.node(d);
        if (r !== void 0) {
            let w = JSON.parse(JSON.stringify(r.clusterData));
            (0, _chunkNQURTBEVMjs.b).trace(`Setting data for parent cluster XXX
 Node.id = `, d, `
 data=`, w.height, `
Parent cluster`, r.height), t.setNode(r.id, w), t.parent(d) || ((0, _chunkNQURTBEVMjs.b).trace("Setting parent", d, r.id), t.setParent(d, r.id, w));
        }
        if ((0, _chunkNQURTBEVMjs.b).info("(Insert) Node XXX" + d + ": " + JSON.stringify(t.node(d))), s?.clusterNode) {
            (0, _chunkNQURTBEVMjs.b).info("Cluster identified XBX", d, s.width, t.node(d));
            let { ranksep: w, nodesep: X } = t.graph();
            s.graph.setGraph({
                ...s.graph.graph(),
                ranksep: w + 25,
                nodesep: X
            });
            let v = await L(O, s.graph, e, c, t.node(d), o), N = v.elem;
            (0, _chunkRRFB4HDSMjs.i)(s, N), s.diff = v.diff || 0, (0, _chunkNQURTBEVMjs.b).info("New compound node after recursive render XAX", d, "width", s.width, "height", s.height), (0, _chunkRRFB4HDSMjs.k)(N, s);
        } else t.children(d).length > 0 ? ((0, _chunkNQURTBEVMjs.b).info("Cluster - the non recursive path XBX", d, s.id, s, s.width, "Graph:", t), (0, _chunkNQURTBEVMjs.b).info(y(s.id, t)), l.set(s.id, {
            id: y(s.id, t),
            node: s
        })) : ((0, _chunkNQURTBEVMjs.b).trace("Node - the non recursive path XAX", d, s.id, s), await (0, _chunkRRFB4HDSMjs.j)(O, t.node(d), f));
    })), await (0, _chunkGTKDMUJJMjs.a)(async ()=>{
        let d = t.edges().map(async function(s) {
            let w = t.edge(s.v, s.w, s.name);
            (0, _chunkNQURTBEVMjs.b).info("Edge " + s.v + " -> " + s.w + ": " + JSON.stringify(s)), (0, _chunkNQURTBEVMjs.b).info("Edge " + s.v + " -> " + s.w + ": ", s, " ", JSON.stringify(t.edge(s))), (0, _chunkNQURTBEVMjs.b).info("Fix", l, "ids:", s.v, s.w, "Translating: ", l.get(s.v), l.get(s.w)), await (0, _chunkRRFB4HDSMjs.d)(b, w);
        });
        await Promise.all(d);
    }, "processEdges")(), (0, _chunkNQURTBEVMjs.b).info("Graph before layout:", JSON.stringify((0, _chunk4BPNZXC3Mjs.a)(t))), (0, _chunkNQURTBEVMjs.b).info("############################################# XXX"), (0, _chunkNQURTBEVMjs.b).info("###                Layout                 ### XXX"), (0, _chunkNQURTBEVMjs.b).info("############################################# XXX"), (0, _chunkBOP2KBYHMjs.a)(t), (0, _chunkNQURTBEVMjs.b).info("Graph after layout:", JSON.stringify((0, _chunk4BPNZXC3Mjs.a)(t)));
    let D = 0, { subGraphTitleTotalMargin: E } = (0, _chunkU6LOUQAFMjs.a)(o);
    return await Promise.all($(t).map(async function(d) {
        let s = t.node(d);
        if ((0, _chunkNQURTBEVMjs.b).info("Position XBX => " + d + ": (" + s.x, "," + s.y, ") width: ", s.width, " height: ", s.height), s?.clusterNode) s.y += E, (0, _chunkNQURTBEVMjs.b).info("A tainted cluster node XBX1", d, s.id, s.width, s.height, s.x, s.y, t.parent(d)), l.get(s.id).node = s, (0, _chunkRRFB4HDSMjs.m)(s);
        else if (t.children(d).length > 0) {
            (0, _chunkNQURTBEVMjs.b).info("A pure cluster node XBX1", d, s.id, s.x, s.y, s.width, s.height, t.parent(d)), s.height += E, t.node(s.parentId);
            let w = s?.padding / 2 || 0, X = s?.labelBBox?.height || 0, v = X - w || 0;
            (0, _chunkNQURTBEVMjs.b).debug("OffsetY", v, "labelHeight", X, "halfPadding", w), await (0, _chunkRRFB4HDSMjs.a)(a, s), l.get(s.id).node = s;
        } else {
            let w = t.node(s.parentId);
            s.y += E / 2, (0, _chunkNQURTBEVMjs.b).info("A regular node XBX1 - using the padding", s.id, "parent", s.parentId, s.width, s.height, s.x, s.y, "offsetY", s.offsetY, "parent", w, w?.offsetY, s), (0, _chunkRRFB4HDSMjs.m)(s);
        }
    })), t.edges().forEach(function(d) {
        let s = t.edge(d);
        (0, _chunkNQURTBEVMjs.b).info("Edge " + d.v + " -> " + d.w + ": " + JSON.stringify(s), s), s.points.forEach((N)=>N.y += E / 2);
        let w = t.node(d.v);
        var X = t.node(d.w);
        let v = (0, _chunkRRFB4HDSMjs.f)(m, s, l, e, w, X, c);
        (0, _chunkRRFB4HDSMjs.e)(s, v);
    }), t.nodes().forEach(function(d) {
        let s = t.node(d);
        (0, _chunkNQURTBEVMjs.b).info(d, s.type, s.diff), s.isGroup && (D = s.diff);
    }), (0, _chunkNQURTBEVMjs.b).warn("Returning from recursive render XAX", g, D), {
        elem: g,
        diff: D
    };
}, "recursiveRender"), be = (0, _chunkGTKDMUJJMjs.a)(async (n, t)=>{
    let e = new (0, _chunk6XGRHI2AMjs.a)({
        multigraph: !0,
        compound: !0
    }).setGraph({
        rankdir: n.direction,
        nodesep: n.config?.nodeSpacing || n.config?.flowchart?.nodeSpacing || n.nodeSpacing,
        ranksep: n.config?.rankSpacing || n.config?.flowchart?.rankSpacing || n.rankSpacing,
        marginx: 8,
        marginy: 8
    }).setDefaultEdgeLabel(function() {
        return {};
    }), c = t.select("g");
    (0, _chunkRRFB4HDSMjs.g)(c, n.markers, n.type, n.diagramId), (0, _chunkRRFB4HDSMjs.l)(), (0, _chunkRRFB4HDSMjs.c)(), (0, _chunkRRFB4HDSMjs.b)(), K(), n.nodes.forEach((o)=>{
        e.setNode(o.id, {
            ...o
        }), o.parentId && e.setParent(o.id, o.parentId);
    }), (0, _chunkNQURTBEVMjs.b).debug("Edges:", n.edges), n.edges.forEach((o)=>{
        e.setEdge(o.start, o.end, {
            ...o
        }, o.id);
    }), (0, _chunkNQURTBEVMjs.b).warn("Graph at first:", JSON.stringify((0, _chunk4BPNZXC3Mjs.a)(e))), V(e), (0, _chunkNQURTBEVMjs.b).warn("Graph after:", JSON.stringify((0, _chunk4BPNZXC3Mjs.a)(e)));
    let r = (0, _chunkNQURTBEVMjs.X)();
    await L(c, e, n.type, n.diagramId, void 0, r);
}, "render");

},{"./chunk-4BPNZXC3.mjs":"9GO9x","./chunk-RRFB4HDS.mjs":"7zFn0","./chunk-U6LOUQAF.mjs":"v9pSW","./chunk-KMOJB3TB.mjs":"aJH4M","./chunk-BOP2KBYH.mjs":"klimL","./chunk-6XGRHI2A.mjs":"fUQIF","./chunk-AC3VT7B7.mjs":"1eiUz","./chunk-TI4EEUUG.mjs":"8SKrN","./chunk-NQURTBEV.mjs":"iASFe","./chunk-BKDDFIKN.mjs":"hADfH","./chunk-YPUTD6PB.mjs":"aSdv1","./chunk-6BY5RJGC.mjs":"bRXnR","./chunk-GTKDMUJJ.mjs":"fruhx","@parcel/transformer-js/src/esmodule-helpers.js":"1cdyk"}],"9GO9x":[function(require,module,exports,__globalThis) {
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

},{"./chunk-BKDDFIKN.mjs":"hADfH","./chunk-YPUTD6PB.mjs":"aSdv1","./chunk-6BY5RJGC.mjs":"bRXnR","./chunk-GTKDMUJJ.mjs":"fruhx","@parcel/transformer-js/src/esmodule-helpers.js":"1cdyk"}]},["6VITu"], null, "parcelRequire6955", {})

//# sourceMappingURL=dagre-EVPMPUST.2b3d9266.js.map
