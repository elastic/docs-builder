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
})({"kBEHH":[function(require,module,exports,__globalThis) {
var global = arguments[3];
var HMR_HOST = null;
var HMR_PORT = 1234;
var HMR_SERVER_PORT = 1234;
var HMR_SECURE = false;
var HMR_ENV_HASH = "febfef3c5b467b16";
var HMR_USE_SSE = false;
module.bundle.HMR_BUNDLE_ID = "e9f4d685adcb51b1";
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

},{}],"fcjSH":[function(require,module,exports,__globalThis) {
var parcelHelpers = require("@parcel/transformer-js/src/esmodule-helpers.js");
parcelHelpers.defineInteropFlag(exports);
parcelHelpers.export(exports, "createLabel_default", ()=>createLabel_default);
parcelHelpers.export(exports, "intersect_rect_default", ()=>intersect_rect_default);
parcelHelpers.export(exports, "updateNodeBounds", ()=>updateNodeBounds);
parcelHelpers.export(exports, "insertNode", ()=>insertNode);
parcelHelpers.export(exports, "setNodeElem", ()=>setNodeElem);
parcelHelpers.export(exports, "clear", ()=>clear);
parcelHelpers.export(exports, "positionNode", ()=>positionNode);
var _chunkYP6PVJQ3Mjs = require("./chunk-YP6PVJQ3.mjs");
var _chunkI7ZFS43CMjs = require("./chunk-I7ZFS43C.mjs");
var _chunkDD37ZF33Mjs = require("./chunk-DD37ZF33.mjs");
var _chunkDLQEHMXDMjs = require("./chunk-DLQEHMXD.mjs");
// src/dagre-wrapper/createLabel.js
function applyStyle(dom, styleFn) {
    if (styleFn) dom.attr("style", styleFn);
}
(0, _chunkDLQEHMXDMjs.__name)(applyStyle, "applyStyle");
function addHtmlLabel(node) {
    const fo = (0, _chunkDD37ZF33Mjs.select_default)(document.createElementNS("http://www.w3.org/2000/svg", "foreignObject"));
    const div = fo.append("xhtml:div");
    const label = node.label;
    const labelClass = node.isNode ? "nodeLabel" : "edgeLabel";
    const span = div.append("span");
    span.html(label);
    applyStyle(span, node.labelStyle);
    span.attr("class", labelClass);
    applyStyle(div, node.labelStyle);
    div.style("display", "inline-block");
    div.style("white-space", "nowrap");
    div.attr("xmlns", "http://www.w3.org/1999/xhtml");
    return fo.node();
}
(0, _chunkDLQEHMXDMjs.__name)(addHtmlLabel, "addHtmlLabel");
var createLabel = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((_vertexText, style, isTitle, isNode)=>{
    let vertexText = _vertexText || "";
    if (typeof vertexText === "object") vertexText = vertexText[0];
    if ((0, _chunkDD37ZF33Mjs.evaluate)((0, _chunkDD37ZF33Mjs.getConfig2)().flowchart.htmlLabels)) {
        vertexText = vertexText.replace(/\\n|\n/g, "<br />");
        (0, _chunkDD37ZF33Mjs.log).debug("vertexText" + vertexText);
        const node = {
            isNode,
            label: (0, _chunkYP6PVJQ3Mjs.replaceIconSubstring)((0, _chunkI7ZFS43CMjs.decodeEntities)(vertexText)),
            labelStyle: style.replace("fill:", "color:")
        };
        let vertexNode = addHtmlLabel(node);
        return vertexNode;
    } else {
        const svgLabel = document.createElementNS("http://www.w3.org/2000/svg", "text");
        svgLabel.setAttribute("style", style.replace("color:", "fill:"));
        let rows = [];
        if (typeof vertexText === "string") rows = vertexText.split(/\\n|\n|<br\s*\/?>/gi);
        else if (Array.isArray(vertexText)) rows = vertexText;
        else rows = [];
        for (const row of rows){
            const tspan = document.createElementNS("http://www.w3.org/2000/svg", "tspan");
            tspan.setAttributeNS("http://www.w3.org/XML/1998/namespace", "xml:space", "preserve");
            tspan.setAttribute("dy", "1em");
            tspan.setAttribute("x", "0");
            if (isTitle) tspan.setAttribute("class", "title-row");
            else tspan.setAttribute("class", "row");
            tspan.textContent = row.trim();
            svgLabel.appendChild(tspan);
        }
        return svgLabel;
    }
}, "createLabel");
var createLabel_default = createLabel;
// src/dagre-wrapper/shapes/util.js
var labelHelper = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(async (parent, node, _classes, isNode)=>{
    const config = (0, _chunkDD37ZF33Mjs.getConfig2)();
    let classes;
    const useHtmlLabels = node.useHtmlLabels || (0, _chunkDD37ZF33Mjs.evaluate)(config.flowchart.htmlLabels);
    if (!_classes) classes = "node default";
    else classes = _classes;
    const shapeSvg = parent.insert("g").attr("class", classes).attr("id", node.domId || node.id);
    const label = shapeSvg.insert("g").attr("class", "label").attr("style", node.labelStyle);
    let labelText;
    if (node.labelText === void 0) labelText = "";
    else labelText = typeof node.labelText === "string" ? node.labelText : node.labelText[0];
    const textNode = label.node();
    let text;
    if (node.labelType === "markdown") text = (0, _chunkYP6PVJQ3Mjs.createText)(label, (0, _chunkDD37ZF33Mjs.sanitizeText)((0, _chunkI7ZFS43CMjs.decodeEntities)(labelText), config), {
        useHtmlLabels,
        width: node.width || config.flowchart.wrappingWidth,
        classes: "markdown-node-label"
    }, config);
    else text = textNode.appendChild(createLabel_default((0, _chunkDD37ZF33Mjs.sanitizeText)((0, _chunkI7ZFS43CMjs.decodeEntities)(labelText), config), node.labelStyle, false, isNode));
    let bbox = text.getBBox();
    const halfPadding = node.padding / 2;
    if ((0, _chunkDD37ZF33Mjs.evaluate)(config.flowchart.htmlLabels)) {
        const div = text.children[0];
        const dv = (0, _chunkDD37ZF33Mjs.select_default)(text);
        const images = div.getElementsByTagName("img");
        if (images) {
            const noImgText = labelText.replace(/<img[^>]*>/g, "").trim() === "";
            await Promise.all([
                ...images
            ].map((img)=>new Promise((res)=>{
                    function setupImage() {
                        img.style.display = "flex";
                        img.style.flexDirection = "column";
                        if (noImgText) {
                            const bodyFontSize = config.fontSize ? config.fontSize : window.getComputedStyle(document.body).fontSize;
                            const enlargingFactor = 5;
                            const width = parseInt(bodyFontSize, 10) * enlargingFactor + "px";
                            img.style.minWidth = width;
                            img.style.maxWidth = width;
                        } else img.style.width = "100%";
                        res(img);
                    }
                    (0, _chunkDLQEHMXDMjs.__name)(setupImage, "setupImage");
                    setTimeout(()=>{
                        if (img.complete) setupImage();
                    });
                    img.addEventListener("error", setupImage);
                    img.addEventListener("load", setupImage);
                })));
        }
        bbox = div.getBoundingClientRect();
        dv.attr("width", bbox.width);
        dv.attr("height", bbox.height);
    }
    if (useHtmlLabels) label.attr("transform", "translate(" + -bbox.width / 2 + ", " + -bbox.height / 2 + ")");
    else label.attr("transform", "translate(0, " + -bbox.height / 2 + ")");
    if (node.centerLabel) label.attr("transform", "translate(" + -bbox.width / 2 + ", " + -bbox.height / 2 + ")");
    label.insert("rect", ":first-child");
    return {
        shapeSvg,
        bbox,
        halfPadding,
        label
    };
}, "labelHelper");
var updateNodeBounds = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((node, element)=>{
    const bbox = element.node().getBBox();
    node.width = bbox.width;
    node.height = bbox.height;
}, "updateNodeBounds");
function insertPolygonShape(parent, w, h, points) {
    return parent.insert("polygon", ":first-child").attr("points", points.map(function(d) {
        return d.x + "," + d.y;
    }).join(" ")).attr("class", "label-container").attr("transform", "translate(" + -w / 2 + "," + h / 2 + ")");
}
(0, _chunkDLQEHMXDMjs.__name)(insertPolygonShape, "insertPolygonShape");
// src/dagre-wrapper/blockArrowHelper.ts
var expandAndDeduplicateDirections = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((directions)=>{
    const uniqueDirections = /* @__PURE__ */ new Set();
    for (const direction of directions)switch(direction){
        case "x":
            uniqueDirections.add("right");
            uniqueDirections.add("left");
            break;
        case "y":
            uniqueDirections.add("up");
            uniqueDirections.add("down");
            break;
        default:
            uniqueDirections.add(direction);
            break;
    }
    return uniqueDirections;
}, "expandAndDeduplicateDirections");
var getArrowPoints = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((duplicatedDirections, bbox, node)=>{
    const directions = expandAndDeduplicateDirections(duplicatedDirections);
    const f = 2;
    const height = bbox.height + 2 * node.padding;
    const midpoint = height / f;
    const width = bbox.width + 2 * midpoint + node.padding;
    const padding = node.padding / 2;
    if (directions.has("right") && directions.has("left") && directions.has("up") && directions.has("down")) return [
        // Bottom
        {
            x: 0,
            y: 0
        },
        {
            x: midpoint,
            y: 0
        },
        {
            x: width / 2,
            y: 2 * padding
        },
        {
            x: width - midpoint,
            y: 0
        },
        {
            x: width,
            y: 0
        },
        // Right
        {
            x: width,
            y: -height / 3
        },
        {
            x: width + 2 * padding,
            y: -height / 2
        },
        {
            x: width,
            y: -2 * height / 3
        },
        {
            x: width,
            y: -height
        },
        // Top
        {
            x: width - midpoint,
            y: -height
        },
        {
            x: width / 2,
            y: -height - 2 * padding
        },
        {
            x: midpoint,
            y: -height
        },
        // Left
        {
            x: 0,
            y: -height
        },
        {
            x: 0,
            y: -2 * height / 3
        },
        {
            x: -2 * padding,
            y: -height / 2
        },
        {
            x: 0,
            y: -height / 3
        }
    ];
    if (directions.has("right") && directions.has("left") && directions.has("up")) return [
        {
            x: midpoint,
            y: 0
        },
        {
            x: width - midpoint,
            y: 0
        },
        {
            x: width,
            y: -height / 2
        },
        {
            x: width - midpoint,
            y: -height
        },
        {
            x: midpoint,
            y: -height
        },
        {
            x: 0,
            y: -height / 2
        }
    ];
    if (directions.has("right") && directions.has("left") && directions.has("down")) return [
        {
            x: 0,
            y: 0
        },
        {
            x: midpoint,
            y: -height
        },
        {
            x: width - midpoint,
            y: -height
        },
        {
            x: width,
            y: 0
        }
    ];
    if (directions.has("right") && directions.has("up") && directions.has("down")) return [
        {
            x: 0,
            y: 0
        },
        {
            x: width,
            y: -midpoint
        },
        {
            x: width,
            y: -height + midpoint
        },
        {
            x: 0,
            y: -height
        }
    ];
    if (directions.has("left") && directions.has("up") && directions.has("down")) return [
        {
            x: width,
            y: 0
        },
        {
            x: 0,
            y: -midpoint
        },
        {
            x: 0,
            y: -height + midpoint
        },
        {
            x: width,
            y: -height
        }
    ];
    if (directions.has("right") && directions.has("left")) return [
        {
            x: midpoint,
            y: 0
        },
        {
            x: midpoint,
            y: -padding
        },
        {
            x: width - midpoint,
            y: -padding
        },
        {
            x: width - midpoint,
            y: 0
        },
        {
            x: width,
            y: -height / 2
        },
        {
            x: width - midpoint,
            y: -height
        },
        {
            x: width - midpoint,
            y: -height + padding
        },
        {
            x: midpoint,
            y: -height + padding
        },
        {
            x: midpoint,
            y: -height
        },
        {
            x: 0,
            y: -height / 2
        }
    ];
    if (directions.has("up") && directions.has("down")) return [
        // Bottom center
        {
            x: width / 2,
            y: 0
        },
        // Left pont of bottom arrow
        {
            x: 0,
            y: -padding
        },
        {
            x: midpoint,
            y: -padding
        },
        // Left top over vertical section
        {
            x: midpoint,
            y: -height + padding
        },
        {
            x: 0,
            y: -height + padding
        },
        // Top of arrow
        {
            x: width / 2,
            y: -height
        },
        {
            x: width,
            y: -height + padding
        },
        // Top of right vertical bar
        {
            x: width - midpoint,
            y: -height + padding
        },
        {
            x: width - midpoint,
            y: -padding
        },
        {
            x: width,
            y: -padding
        }
    ];
    if (directions.has("right") && directions.has("up")) return [
        {
            x: 0,
            y: 0
        },
        {
            x: width,
            y: -midpoint
        },
        {
            x: 0,
            y: -height
        }
    ];
    if (directions.has("right") && directions.has("down")) return [
        {
            x: 0,
            y: 0
        },
        {
            x: width,
            y: 0
        },
        {
            x: 0,
            y: -height
        }
    ];
    if (directions.has("left") && directions.has("up")) return [
        {
            x: width,
            y: 0
        },
        {
            x: 0,
            y: -midpoint
        },
        {
            x: width,
            y: -height
        }
    ];
    if (directions.has("left") && directions.has("down")) return [
        {
            x: width,
            y: 0
        },
        {
            x: 0,
            y: 0
        },
        {
            x: width,
            y: -height
        }
    ];
    if (directions.has("right")) return [
        {
            x: midpoint,
            y: -padding
        },
        {
            x: midpoint,
            y: -padding
        },
        {
            x: width - midpoint,
            y: -padding
        },
        {
            x: width - midpoint,
            y: 0
        },
        {
            x: width,
            y: -height / 2
        },
        {
            x: width - midpoint,
            y: -height
        },
        {
            x: width - midpoint,
            y: -height + padding
        },
        // top left corner of arrow
        {
            x: midpoint,
            y: -height + padding
        },
        {
            x: midpoint,
            y: -height + padding
        }
    ];
    if (directions.has("left")) return [
        {
            x: midpoint,
            y: 0
        },
        {
            x: midpoint,
            y: -padding
        },
        // Two points, the right corners
        {
            x: width - midpoint,
            y: -padding
        },
        {
            x: width - midpoint,
            y: -height + padding
        },
        {
            x: midpoint,
            y: -height + padding
        },
        {
            x: midpoint,
            y: -height
        },
        {
            x: 0,
            y: -height / 2
        }
    ];
    if (directions.has("up")) return [
        // Bottom center
        {
            x: midpoint,
            y: -padding
        },
        // Left top over vertical section
        {
            x: midpoint,
            y: -height + padding
        },
        {
            x: 0,
            y: -height + padding
        },
        // Top of arrow
        {
            x: width / 2,
            y: -height
        },
        {
            x: width,
            y: -height + padding
        },
        // Top of right vertical bar
        {
            x: width - midpoint,
            y: -height + padding
        },
        {
            x: width - midpoint,
            y: -padding
        }
    ];
    if (directions.has("down")) return [
        // Bottom center
        {
            x: width / 2,
            y: 0
        },
        // Left pont of bottom arrow
        {
            x: 0,
            y: -padding
        },
        {
            x: midpoint,
            y: -padding
        },
        // Left top over vertical section
        {
            x: midpoint,
            y: -height + padding
        },
        {
            x: width - midpoint,
            y: -height + padding
        },
        {
            x: width - midpoint,
            y: -padding
        },
        {
            x: width,
            y: -padding
        }
    ];
    return [
        {
            x: 0,
            y: 0
        }
    ];
}, "getArrowPoints");
// src/dagre-wrapper/intersect/intersect-node.js
function intersectNode(node, point) {
    return node.intersect(point);
}
(0, _chunkDLQEHMXDMjs.__name)(intersectNode, "intersectNode");
var intersect_node_default = intersectNode;
// src/dagre-wrapper/intersect/intersect-ellipse.js
function intersectEllipse(node, rx, ry, point) {
    var cx = node.x;
    var cy = node.y;
    var px = cx - point.x;
    var py = cy - point.y;
    var det = Math.sqrt(rx * rx * py * py + ry * ry * px * px);
    var dx = Math.abs(rx * ry * px / det);
    if (point.x < cx) dx = -dx;
    var dy = Math.abs(rx * ry * py / det);
    if (point.y < cy) dy = -dy;
    return {
        x: cx + dx,
        y: cy + dy
    };
}
(0, _chunkDLQEHMXDMjs.__name)(intersectEllipse, "intersectEllipse");
var intersect_ellipse_default = intersectEllipse;
// src/dagre-wrapper/intersect/intersect-circle.js
function intersectCircle(node, rx, point) {
    return intersect_ellipse_default(node, rx, rx, point);
}
(0, _chunkDLQEHMXDMjs.__name)(intersectCircle, "intersectCircle");
var intersect_circle_default = intersectCircle;
// src/dagre-wrapper/intersect/intersect-line.js
function intersectLine(p1, p2, q1, q2) {
    var a1, a2, b1, b2, c1, c2;
    var r1, r2, r3, r4;
    var denom, offset, num;
    var x, y;
    a1 = p2.y - p1.y;
    b1 = p1.x - p2.x;
    c1 = p2.x * p1.y - p1.x * p2.y;
    r3 = a1 * q1.x + b1 * q1.y + c1;
    r4 = a1 * q2.x + b1 * q2.y + c1;
    if (r3 !== 0 && r4 !== 0 && sameSign(r3, r4)) return;
    a2 = q2.y - q1.y;
    b2 = q1.x - q2.x;
    c2 = q2.x * q1.y - q1.x * q2.y;
    r1 = a2 * p1.x + b2 * p1.y + c2;
    r2 = a2 * p2.x + b2 * p2.y + c2;
    if (r1 !== 0 && r2 !== 0 && sameSign(r1, r2)) return;
    denom = a1 * b2 - a2 * b1;
    if (denom === 0) return;
    offset = Math.abs(denom / 2);
    num = b1 * c2 - b2 * c1;
    x = num < 0 ? (num - offset) / denom : (num + offset) / denom;
    num = a2 * c1 - a1 * c2;
    y = num < 0 ? (num - offset) / denom : (num + offset) / denom;
    return {
        x,
        y
    };
}
(0, _chunkDLQEHMXDMjs.__name)(intersectLine, "intersectLine");
function sameSign(r1, r2) {
    return r1 * r2 > 0;
}
(0, _chunkDLQEHMXDMjs.__name)(sameSign, "sameSign");
var intersect_line_default = intersectLine;
// src/dagre-wrapper/intersect/intersect-polygon.js
var intersect_polygon_default = intersectPolygon;
function intersectPolygon(node, polyPoints, point) {
    var x1 = node.x;
    var y1 = node.y;
    var intersections = [];
    var minX = Number.POSITIVE_INFINITY;
    var minY = Number.POSITIVE_INFINITY;
    if (typeof polyPoints.forEach === "function") polyPoints.forEach(function(entry) {
        minX = Math.min(minX, entry.x);
        minY = Math.min(minY, entry.y);
    });
    else {
        minX = Math.min(minX, polyPoints.x);
        minY = Math.min(minY, polyPoints.y);
    }
    var left = x1 - node.width / 2 - minX;
    var top = y1 - node.height / 2 - minY;
    for(var i = 0; i < polyPoints.length; i++){
        var p1 = polyPoints[i];
        var p2 = polyPoints[i < polyPoints.length - 1 ? i + 1 : 0];
        var intersect = intersect_line_default(node, point, {
            x: left + p1.x,
            y: top + p1.y
        }, {
            x: left + p2.x,
            y: top + p2.y
        });
        if (intersect) intersections.push(intersect);
    }
    if (!intersections.length) return node;
    if (intersections.length > 1) intersections.sort(function(p, q) {
        var pdx = p.x - point.x;
        var pdy = p.y - point.y;
        var distp = Math.sqrt(pdx * pdx + pdy * pdy);
        var qdx = q.x - point.x;
        var qdy = q.y - point.y;
        var distq = Math.sqrt(qdx * qdx + qdy * qdy);
        return distp < distq ? -1 : distp === distq ? 0 : 1;
    });
    return intersections[0];
}
(0, _chunkDLQEHMXDMjs.__name)(intersectPolygon, "intersectPolygon");
// src/dagre-wrapper/intersect/intersect-rect.js
var intersectRect = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((node, point)=>{
    var x = node.x;
    var y = node.y;
    var dx = point.x - x;
    var dy = point.y - y;
    var w = node.width / 2;
    var h = node.height / 2;
    var sx, sy;
    if (Math.abs(dy) * w > Math.abs(dx) * h) {
        if (dy < 0) h = -h;
        sx = dy === 0 ? 0 : h * dx / dy;
        sy = h;
    } else {
        if (dx < 0) w = -w;
        sx = w;
        sy = dx === 0 ? 0 : w * dy / dx;
    }
    return {
        x: x + sx,
        y: y + sy
    };
}, "intersectRect");
var intersect_rect_default = intersectRect;
// src/dagre-wrapper/intersect/index.js
var intersect_default = {
    node: intersect_node_default,
    circle: intersect_circle_default,
    ellipse: intersect_ellipse_default,
    polygon: intersect_polygon_default,
    rect: intersect_rect_default
};
// src/dagre-wrapper/shapes/note.js
var note = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(async (parent, node)=>{
    const useHtmlLabels = node.useHtmlLabels || (0, _chunkDD37ZF33Mjs.getConfig2)().flowchart.htmlLabels;
    if (!useHtmlLabels) node.centerLabel = true;
    const { shapeSvg, bbox, halfPadding } = await labelHelper(parent, node, "node " + node.classes, true);
    (0, _chunkDD37ZF33Mjs.log).info("Classes = ", node.classes);
    const rect2 = shapeSvg.insert("rect", ":first-child");
    rect2.attr("rx", node.rx).attr("ry", node.ry).attr("x", -bbox.width / 2 - halfPadding).attr("y", -bbox.height / 2 - halfPadding).attr("width", bbox.width + node.padding).attr("height", bbox.height + node.padding);
    updateNodeBounds(node, rect2);
    node.intersect = function(point) {
        return intersect_default.rect(node, point);
    };
    return shapeSvg;
}, "note");
var note_default = note;
// src/dagre-wrapper/nodes.js
var formatClass = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((str)=>{
    if (str) return " " + str;
    return "";
}, "formatClass");
var getClassesFromNode = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((node, otherClasses)=>{
    return `${otherClasses ? otherClasses : "node default"}${formatClass(node.classes)} ${formatClass(node.class)}`;
}, "getClassesFromNode");
var question = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(async (parent, node)=>{
    const { shapeSvg, bbox } = await labelHelper(parent, node, getClassesFromNode(node, void 0), true);
    const w = bbox.width + node.padding;
    const h = bbox.height + node.padding;
    const s = w + h;
    const points = [
        {
            x: s / 2,
            y: 0
        },
        {
            x: s,
            y: -s / 2
        },
        {
            x: s / 2,
            y: -s
        },
        {
            x: 0,
            y: -s / 2
        }
    ];
    (0, _chunkDD37ZF33Mjs.log).info("Question main (Circle)");
    const questionElem = insertPolygonShape(shapeSvg, s, s, points);
    questionElem.attr("style", node.style);
    updateNodeBounds(node, questionElem);
    node.intersect = function(point) {
        (0, _chunkDD37ZF33Mjs.log).warn("Intersect called");
        return intersect_default.polygon(node, points, point);
    };
    return shapeSvg;
}, "question");
var choice = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((parent, node)=>{
    const shapeSvg = parent.insert("g").attr("class", "node default").attr("id", node.domId || node.id);
    const s = 28;
    const points = [
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
    const choice2 = shapeSvg.insert("polygon", ":first-child").attr("points", points.map(function(d) {
        return d.x + "," + d.y;
    }).join(" "));
    choice2.attr("class", "state-start").attr("r", 7).attr("width", 28).attr("height", 28);
    node.width = 28;
    node.height = 28;
    node.intersect = function(point) {
        return intersect_default.circle(node, 14, point);
    };
    return shapeSvg;
}, "choice");
var hexagon = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(async (parent, node)=>{
    const { shapeSvg, bbox } = await labelHelper(parent, node, getClassesFromNode(node, void 0), true);
    const f = 4;
    const h = bbox.height + node.padding;
    const m = h / f;
    const w = bbox.width + 2 * m + node.padding;
    const points = [
        {
            x: m,
            y: 0
        },
        {
            x: w - m,
            y: 0
        },
        {
            x: w,
            y: -h / 2
        },
        {
            x: w - m,
            y: -h
        },
        {
            x: m,
            y: -h
        },
        {
            x: 0,
            y: -h / 2
        }
    ];
    const hex = insertPolygonShape(shapeSvg, w, h, points);
    hex.attr("style", node.style);
    updateNodeBounds(node, hex);
    node.intersect = function(point) {
        return intersect_default.polygon(node, points, point);
    };
    return shapeSvg;
}, "hexagon");
var block_arrow = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(async (parent, node)=>{
    const { shapeSvg, bbox } = await labelHelper(parent, node, void 0, true);
    const f = 2;
    const h = bbox.height + 2 * node.padding;
    const m = h / f;
    const w = bbox.width + 2 * m + node.padding;
    const points = getArrowPoints(node.directions, bbox, node);
    const blockArrow = insertPolygonShape(shapeSvg, w, h, points);
    blockArrow.attr("style", node.style);
    updateNodeBounds(node, blockArrow);
    node.intersect = function(point) {
        return intersect_default.polygon(node, points, point);
    };
    return shapeSvg;
}, "block_arrow");
var rect_left_inv_arrow = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(async (parent, node)=>{
    const { shapeSvg, bbox } = await labelHelper(parent, node, getClassesFromNode(node, void 0), true);
    const w = bbox.width + node.padding;
    const h = bbox.height + node.padding;
    const points = [
        {
            x: -h / 2,
            y: 0
        },
        {
            x: w,
            y: 0
        },
        {
            x: w,
            y: -h
        },
        {
            x: -h / 2,
            y: -h
        },
        {
            x: 0,
            y: -h / 2
        }
    ];
    const el = insertPolygonShape(shapeSvg, w, h, points);
    el.attr("style", node.style);
    node.width = w + h;
    node.height = h;
    node.intersect = function(point) {
        return intersect_default.polygon(node, points, point);
    };
    return shapeSvg;
}, "rect_left_inv_arrow");
var lean_right = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(async (parent, node)=>{
    const { shapeSvg, bbox } = await labelHelper(parent, node, getClassesFromNode(node), true);
    const w = bbox.width + node.padding;
    const h = bbox.height + node.padding;
    const points = [
        {
            x: -2 * h / 6,
            y: 0
        },
        {
            x: w - h / 6,
            y: 0
        },
        {
            x: w + 2 * h / 6,
            y: -h
        },
        {
            x: h / 6,
            y: -h
        }
    ];
    const el = insertPolygonShape(shapeSvg, w, h, points);
    el.attr("style", node.style);
    updateNodeBounds(node, el);
    node.intersect = function(point) {
        return intersect_default.polygon(node, points, point);
    };
    return shapeSvg;
}, "lean_right");
var lean_left = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(async (parent, node)=>{
    const { shapeSvg, bbox } = await labelHelper(parent, node, getClassesFromNode(node, void 0), true);
    const w = bbox.width + node.padding;
    const h = bbox.height + node.padding;
    const points = [
        {
            x: 2 * h / 6,
            y: 0
        },
        {
            x: w + h / 6,
            y: 0
        },
        {
            x: w - 2 * h / 6,
            y: -h
        },
        {
            x: -h / 6,
            y: -h
        }
    ];
    const el = insertPolygonShape(shapeSvg, w, h, points);
    el.attr("style", node.style);
    updateNodeBounds(node, el);
    node.intersect = function(point) {
        return intersect_default.polygon(node, points, point);
    };
    return shapeSvg;
}, "lean_left");
var trapezoid = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(async (parent, node)=>{
    const { shapeSvg, bbox } = await labelHelper(parent, node, getClassesFromNode(node, void 0), true);
    const w = bbox.width + node.padding;
    const h = bbox.height + node.padding;
    const points = [
        {
            x: -2 * h / 6,
            y: 0
        },
        {
            x: w + 2 * h / 6,
            y: 0
        },
        {
            x: w - h / 6,
            y: -h
        },
        {
            x: h / 6,
            y: -h
        }
    ];
    const el = insertPolygonShape(shapeSvg, w, h, points);
    el.attr("style", node.style);
    updateNodeBounds(node, el);
    node.intersect = function(point) {
        return intersect_default.polygon(node, points, point);
    };
    return shapeSvg;
}, "trapezoid");
var inv_trapezoid = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(async (parent, node)=>{
    const { shapeSvg, bbox } = await labelHelper(parent, node, getClassesFromNode(node, void 0), true);
    const w = bbox.width + node.padding;
    const h = bbox.height + node.padding;
    const points = [
        {
            x: h / 6,
            y: 0
        },
        {
            x: w - h / 6,
            y: 0
        },
        {
            x: w + 2 * h / 6,
            y: -h
        },
        {
            x: -2 * h / 6,
            y: -h
        }
    ];
    const el = insertPolygonShape(shapeSvg, w, h, points);
    el.attr("style", node.style);
    updateNodeBounds(node, el);
    node.intersect = function(point) {
        return intersect_default.polygon(node, points, point);
    };
    return shapeSvg;
}, "inv_trapezoid");
var rect_right_inv_arrow = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(async (parent, node)=>{
    const { shapeSvg, bbox } = await labelHelper(parent, node, getClassesFromNode(node, void 0), true);
    const w = bbox.width + node.padding;
    const h = bbox.height + node.padding;
    const points = [
        {
            x: 0,
            y: 0
        },
        {
            x: w + h / 2,
            y: 0
        },
        {
            x: w,
            y: -h / 2
        },
        {
            x: w + h / 2,
            y: -h
        },
        {
            x: 0,
            y: -h
        }
    ];
    const el = insertPolygonShape(shapeSvg, w, h, points);
    el.attr("style", node.style);
    updateNodeBounds(node, el);
    node.intersect = function(point) {
        return intersect_default.polygon(node, points, point);
    };
    return shapeSvg;
}, "rect_right_inv_arrow");
var cylinder = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(async (parent, node)=>{
    const { shapeSvg, bbox } = await labelHelper(parent, node, getClassesFromNode(node, void 0), true);
    const w = bbox.width + node.padding;
    const rx = w / 2;
    const ry = rx / (2.5 + w / 50);
    const h = bbox.height + ry + node.padding;
    const shape = "M 0," + ry + " a " + rx + "," + ry + " 0,0,0 " + w + " 0 a " + rx + "," + ry + " 0,0,0 " + -w + " 0 l 0," + h + " a " + rx + "," + ry + " 0,0,0 " + w + " 0 l 0," + -h;
    const el = shapeSvg.attr("label-offset-y", ry).insert("path", ":first-child").attr("style", node.style).attr("d", shape).attr("transform", "translate(" + -w / 2 + "," + -(h / 2 + ry) + ")");
    updateNodeBounds(node, el);
    node.intersect = function(point) {
        const pos = intersect_default.rect(node, point);
        const x = pos.x - node.x;
        if (rx != 0 && (Math.abs(x) < node.width / 2 || Math.abs(x) == node.width / 2 && Math.abs(pos.y - node.y) > node.height / 2 - ry)) {
            let y = ry * ry * (1 - x * x / (rx * rx));
            if (y != 0) y = Math.sqrt(y);
            y = ry - y;
            if (point.y - node.y > 0) y = -y;
            pos.y += y;
        }
        return pos;
    };
    return shapeSvg;
}, "cylinder");
var rect = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(async (parent, node)=>{
    const { shapeSvg, bbox, halfPadding } = await labelHelper(parent, node, "node " + node.classes + " " + node.class, true);
    const rect2 = shapeSvg.insert("rect", ":first-child");
    const totalWidth = node.positioned ? node.width : bbox.width + node.padding;
    const totalHeight = node.positioned ? node.height : bbox.height + node.padding;
    const x = node.positioned ? -totalWidth / 2 : -bbox.width / 2 - halfPadding;
    const y = node.positioned ? -totalHeight / 2 : -bbox.height / 2 - halfPadding;
    rect2.attr("class", "basic label-container").attr("style", node.style).attr("rx", node.rx).attr("ry", node.ry).attr("x", x).attr("y", y).attr("width", totalWidth).attr("height", totalHeight);
    if (node.props) {
        const propKeys = new Set(Object.keys(node.props));
        if (node.props.borders) {
            applyNodePropertyBorders(rect2, node.props.borders, totalWidth, totalHeight);
            propKeys.delete("borders");
        }
        propKeys.forEach((propKey)=>{
            (0, _chunkDD37ZF33Mjs.log).warn(`Unknown node property ${propKey}`);
        });
    }
    updateNodeBounds(node, rect2);
    node.intersect = function(point) {
        return intersect_default.rect(node, point);
    };
    return shapeSvg;
}, "rect");
var composite = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(async (parent, node)=>{
    const { shapeSvg, bbox, halfPadding } = await labelHelper(parent, node, "node " + node.classes, true);
    const rect2 = shapeSvg.insert("rect", ":first-child");
    const totalWidth = node.positioned ? node.width : bbox.width + node.padding;
    const totalHeight = node.positioned ? node.height : bbox.height + node.padding;
    const x = node.positioned ? -totalWidth / 2 : -bbox.width / 2 - halfPadding;
    const y = node.positioned ? -totalHeight / 2 : -bbox.height / 2 - halfPadding;
    rect2.attr("class", "basic cluster composite label-container").attr("style", node.style).attr("rx", node.rx).attr("ry", node.ry).attr("x", x).attr("y", y).attr("width", totalWidth).attr("height", totalHeight);
    if (node.props) {
        const propKeys = new Set(Object.keys(node.props));
        if (node.props.borders) {
            applyNodePropertyBorders(rect2, node.props.borders, totalWidth, totalHeight);
            propKeys.delete("borders");
        }
        propKeys.forEach((propKey)=>{
            (0, _chunkDD37ZF33Mjs.log).warn(`Unknown node property ${propKey}`);
        });
    }
    updateNodeBounds(node, rect2);
    node.intersect = function(point) {
        return intersect_default.rect(node, point);
    };
    return shapeSvg;
}, "composite");
var labelRect = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(async (parent, node)=>{
    const { shapeSvg } = await labelHelper(parent, node, "label", true);
    (0, _chunkDD37ZF33Mjs.log).trace("Classes = ", node.class);
    const rect2 = shapeSvg.insert("rect", ":first-child");
    const totalWidth = 0;
    const totalHeight = 0;
    rect2.attr("width", totalWidth).attr("height", totalHeight);
    shapeSvg.attr("class", "label edgeLabel");
    if (node.props) {
        const propKeys = new Set(Object.keys(node.props));
        if (node.props.borders) {
            applyNodePropertyBorders(rect2, node.props.borders, totalWidth, totalHeight);
            propKeys.delete("borders");
        }
        propKeys.forEach((propKey)=>{
            (0, _chunkDD37ZF33Mjs.log).warn(`Unknown node property ${propKey}`);
        });
    }
    updateNodeBounds(node, rect2);
    node.intersect = function(point) {
        return intersect_default.rect(node, point);
    };
    return shapeSvg;
}, "labelRect");
function applyNodePropertyBorders(rect2, borders, totalWidth, totalHeight) {
    const strokeDashArray = [];
    const addBorder = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((length)=>{
        strokeDashArray.push(length, 0);
    }, "addBorder");
    const skipBorder = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((length)=>{
        strokeDashArray.push(0, length);
    }, "skipBorder");
    if (borders.includes("t")) {
        (0, _chunkDD37ZF33Mjs.log).debug("add top border");
        addBorder(totalWidth);
    } else skipBorder(totalWidth);
    if (borders.includes("r")) {
        (0, _chunkDD37ZF33Mjs.log).debug("add right border");
        addBorder(totalHeight);
    } else skipBorder(totalHeight);
    if (borders.includes("b")) {
        (0, _chunkDD37ZF33Mjs.log).debug("add bottom border");
        addBorder(totalWidth);
    } else skipBorder(totalWidth);
    if (borders.includes("l")) {
        (0, _chunkDD37ZF33Mjs.log).debug("add left border");
        addBorder(totalHeight);
    } else skipBorder(totalHeight);
    rect2.attr("stroke-dasharray", strokeDashArray.join(" "));
}
(0, _chunkDLQEHMXDMjs.__name)(applyNodePropertyBorders, "applyNodePropertyBorders");
var rectWithTitle = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((parent, node)=>{
    let classes;
    if (!node.classes) classes = "node default";
    else classes = "node " + node.classes;
    const shapeSvg = parent.insert("g").attr("class", classes).attr("id", node.domId || node.id);
    const rect2 = shapeSvg.insert("rect", ":first-child");
    const innerLine = shapeSvg.insert("line");
    const label = shapeSvg.insert("g").attr("class", "label");
    const text2 = node.labelText.flat ? node.labelText.flat() : node.labelText;
    let title = "";
    if (typeof text2 === "object") title = text2[0];
    else title = text2;
    (0, _chunkDD37ZF33Mjs.log).info("Label text abc79", title, text2, typeof text2 === "object");
    const text = label.node().appendChild(createLabel_default(title, node.labelStyle, true, true));
    let bbox = {
        width: 0,
        height: 0
    };
    if ((0, _chunkDD37ZF33Mjs.evaluate)((0, _chunkDD37ZF33Mjs.getConfig2)().flowchart.htmlLabels)) {
        const div = text.children[0];
        const dv = (0, _chunkDD37ZF33Mjs.select_default)(text);
        bbox = div.getBoundingClientRect();
        dv.attr("width", bbox.width);
        dv.attr("height", bbox.height);
    }
    (0, _chunkDD37ZF33Mjs.log).info("Text 2", text2);
    const textRows = text2.slice(1, text2.length);
    let titleBox = text.getBBox();
    const descr = label.node().appendChild(createLabel_default(textRows.join ? textRows.join("<br/>") : textRows, node.labelStyle, true, true));
    if ((0, _chunkDD37ZF33Mjs.evaluate)((0, _chunkDD37ZF33Mjs.getConfig2)().flowchart.htmlLabels)) {
        const div = descr.children[0];
        const dv = (0, _chunkDD37ZF33Mjs.select_default)(descr);
        bbox = div.getBoundingClientRect();
        dv.attr("width", bbox.width);
        dv.attr("height", bbox.height);
    }
    const halfPadding = node.padding / 2;
    (0, _chunkDD37ZF33Mjs.select_default)(descr).attr("transform", "translate( " + // (titleBox.width - bbox.width) / 2 +
    (bbox.width > titleBox.width ? 0 : (titleBox.width - bbox.width) / 2) + ", " + (titleBox.height + halfPadding + 5) + ")");
    (0, _chunkDD37ZF33Mjs.select_default)(text).attr("transform", "translate( " + // (titleBox.width - bbox.width) / 2 +
    (bbox.width < titleBox.width ? 0 : -(titleBox.width - bbox.width) / 2) + ", 0)");
    bbox = label.node().getBBox();
    label.attr("transform", "translate(" + -bbox.width / 2 + ", " + (-bbox.height / 2 - halfPadding + 3) + ")");
    rect2.attr("class", "outer title-state").attr("x", -bbox.width / 2 - halfPadding).attr("y", -bbox.height / 2 - halfPadding).attr("width", bbox.width + node.padding).attr("height", bbox.height + node.padding);
    innerLine.attr("class", "divider").attr("x1", -bbox.width / 2 - halfPadding).attr("x2", bbox.width / 2 + halfPadding).attr("y1", -bbox.height / 2 - halfPadding + titleBox.height + halfPadding).attr("y2", -bbox.height / 2 - halfPadding + titleBox.height + halfPadding);
    updateNodeBounds(node, rect2);
    node.intersect = function(point) {
        return intersect_default.rect(node, point);
    };
    return shapeSvg;
}, "rectWithTitle");
var stadium = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(async (parent, node)=>{
    const { shapeSvg, bbox } = await labelHelper(parent, node, getClassesFromNode(node, void 0), true);
    const h = bbox.height + node.padding;
    const w = bbox.width + h / 4 + node.padding;
    const rect2 = shapeSvg.insert("rect", ":first-child").attr("style", node.style).attr("rx", h / 2).attr("ry", h / 2).attr("x", -w / 2).attr("y", -h / 2).attr("width", w).attr("height", h);
    updateNodeBounds(node, rect2);
    node.intersect = function(point) {
        return intersect_default.rect(node, point);
    };
    return shapeSvg;
}, "stadium");
var circle = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(async (parent, node)=>{
    const { shapeSvg, bbox, halfPadding } = await labelHelper(parent, node, getClassesFromNode(node, void 0), true);
    const circle2 = shapeSvg.insert("circle", ":first-child");
    circle2.attr("style", node.style).attr("rx", node.rx).attr("ry", node.ry).attr("r", bbox.width / 2 + halfPadding).attr("width", bbox.width + node.padding).attr("height", bbox.height + node.padding);
    (0, _chunkDD37ZF33Mjs.log).info("Circle main");
    updateNodeBounds(node, circle2);
    node.intersect = function(point) {
        (0, _chunkDD37ZF33Mjs.log).info("Circle intersect", node, bbox.width / 2 + halfPadding, point);
        return intersect_default.circle(node, bbox.width / 2 + halfPadding, point);
    };
    return shapeSvg;
}, "circle");
var doublecircle = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(async (parent, node)=>{
    const { shapeSvg, bbox, halfPadding } = await labelHelper(parent, node, getClassesFromNode(node, void 0), true);
    const gap = 5;
    const circleGroup = shapeSvg.insert("g", ":first-child");
    const outerCircle = circleGroup.insert("circle");
    const innerCircle = circleGroup.insert("circle");
    circleGroup.attr("class", node.class);
    outerCircle.attr("style", node.style).attr("rx", node.rx).attr("ry", node.ry).attr("r", bbox.width / 2 + halfPadding + gap).attr("width", bbox.width + node.padding + gap * 2).attr("height", bbox.height + node.padding + gap * 2);
    innerCircle.attr("style", node.style).attr("rx", node.rx).attr("ry", node.ry).attr("r", bbox.width / 2 + halfPadding).attr("width", bbox.width + node.padding).attr("height", bbox.height + node.padding);
    (0, _chunkDD37ZF33Mjs.log).info("DoubleCircle main");
    updateNodeBounds(node, outerCircle);
    node.intersect = function(point) {
        (0, _chunkDD37ZF33Mjs.log).info("DoubleCircle intersect", node, bbox.width / 2 + halfPadding + gap, point);
        return intersect_default.circle(node, bbox.width / 2 + halfPadding + gap, point);
    };
    return shapeSvg;
}, "doublecircle");
var subroutine = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(async (parent, node)=>{
    const { shapeSvg, bbox } = await labelHelper(parent, node, getClassesFromNode(node, void 0), true);
    const w = bbox.width + node.padding;
    const h = bbox.height + node.padding;
    const points = [
        {
            x: 0,
            y: 0
        },
        {
            x: w,
            y: 0
        },
        {
            x: w,
            y: -h
        },
        {
            x: 0,
            y: -h
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
            x: w + 8,
            y: 0
        },
        {
            x: w + 8,
            y: -h
        },
        {
            x: -8,
            y: -h
        },
        {
            x: -8,
            y: 0
        }
    ];
    const el = insertPolygonShape(shapeSvg, w, h, points);
    el.attr("style", node.style);
    updateNodeBounds(node, el);
    node.intersect = function(point) {
        return intersect_default.polygon(node, points, point);
    };
    return shapeSvg;
}, "subroutine");
var start = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((parent, node)=>{
    const shapeSvg = parent.insert("g").attr("class", "node default").attr("id", node.domId || node.id);
    const circle2 = shapeSvg.insert("circle", ":first-child");
    circle2.attr("class", "state-start").attr("r", 7).attr("width", 14).attr("height", 14);
    updateNodeBounds(node, circle2);
    node.intersect = function(point) {
        return intersect_default.circle(node, 7, point);
    };
    return shapeSvg;
}, "start");
var forkJoin = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((parent, node, dir)=>{
    const shapeSvg = parent.insert("g").attr("class", "node default").attr("id", node.domId || node.id);
    let width = 70;
    let height = 10;
    if (dir === "LR") {
        width = 10;
        height = 70;
    }
    const shape = shapeSvg.append("rect").attr("x", -1 * width / 2).attr("y", -1 * height / 2).attr("width", width).attr("height", height).attr("class", "fork-join");
    updateNodeBounds(node, shape);
    node.height = node.height + node.padding / 2;
    node.width = node.width + node.padding / 2;
    node.intersect = function(point) {
        return intersect_default.rect(node, point);
    };
    return shapeSvg;
}, "forkJoin");
var end = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((parent, node)=>{
    const shapeSvg = parent.insert("g").attr("class", "node default").attr("id", node.domId || node.id);
    const innerCircle = shapeSvg.insert("circle", ":first-child");
    const circle2 = shapeSvg.insert("circle", ":first-child");
    circle2.attr("class", "state-start").attr("r", 7).attr("width", 14).attr("height", 14);
    innerCircle.attr("class", "state-end").attr("r", 5).attr("width", 10).attr("height", 10);
    updateNodeBounds(node, circle2);
    node.intersect = function(point) {
        return intersect_default.circle(node, 7, point);
    };
    return shapeSvg;
}, "end");
var class_box = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((parent, node)=>{
    const halfPadding = node.padding / 2;
    const rowPadding = 4;
    const lineHeight = 8;
    let classes;
    if (!node.classes) classes = "node default";
    else classes = "node " + node.classes;
    const shapeSvg = parent.insert("g").attr("class", classes).attr("id", node.domId || node.id);
    const rect2 = shapeSvg.insert("rect", ":first-child");
    const topLine = shapeSvg.insert("line");
    const bottomLine = shapeSvg.insert("line");
    let maxWidth = 0;
    let maxHeight = rowPadding;
    const labelContainer = shapeSvg.insert("g").attr("class", "label");
    let verticalPos = 0;
    const hasInterface = node.classData.annotations?.[0];
    const interfaceLabelText = node.classData.annotations[0] ? "\xAB" + node.classData.annotations[0] + "\xBB" : "";
    const interfaceLabel = labelContainer.node().appendChild(createLabel_default(interfaceLabelText, node.labelStyle, true, true));
    let interfaceBBox = interfaceLabel.getBBox();
    if ((0, _chunkDD37ZF33Mjs.evaluate)((0, _chunkDD37ZF33Mjs.getConfig2)().flowchart.htmlLabels)) {
        const div = interfaceLabel.children[0];
        const dv = (0, _chunkDD37ZF33Mjs.select_default)(interfaceLabel);
        interfaceBBox = div.getBoundingClientRect();
        dv.attr("width", interfaceBBox.width);
        dv.attr("height", interfaceBBox.height);
    }
    if (node.classData.annotations[0]) {
        maxHeight += interfaceBBox.height + rowPadding;
        maxWidth += interfaceBBox.width;
    }
    let classTitleString = node.classData.label;
    if (node.classData.type !== void 0 && node.classData.type !== "") {
        if ((0, _chunkDD37ZF33Mjs.getConfig2)().flowchart.htmlLabels) classTitleString += "&lt;" + node.classData.type + "&gt;";
        else classTitleString += "<" + node.classData.type + ">";
    }
    const classTitleLabel = labelContainer.node().appendChild(createLabel_default(classTitleString, node.labelStyle, true, true));
    (0, _chunkDD37ZF33Mjs.select_default)(classTitleLabel).attr("class", "classTitle");
    let classTitleBBox = classTitleLabel.getBBox();
    if ((0, _chunkDD37ZF33Mjs.evaluate)((0, _chunkDD37ZF33Mjs.getConfig2)().flowchart.htmlLabels)) {
        const div = classTitleLabel.children[0];
        const dv = (0, _chunkDD37ZF33Mjs.select_default)(classTitleLabel);
        classTitleBBox = div.getBoundingClientRect();
        dv.attr("width", classTitleBBox.width);
        dv.attr("height", classTitleBBox.height);
    }
    maxHeight += classTitleBBox.height + rowPadding;
    if (classTitleBBox.width > maxWidth) maxWidth = classTitleBBox.width;
    const classAttributes = [];
    node.classData.members.forEach((member)=>{
        const parsedInfo = member.getDisplayDetails();
        let parsedText = parsedInfo.displayText;
        if ((0, _chunkDD37ZF33Mjs.getConfig2)().flowchart.htmlLabels) parsedText = parsedText.replace(/</g, "&lt;").replace(/>/g, "&gt;");
        const lbl = labelContainer.node().appendChild(createLabel_default(parsedText, parsedInfo.cssStyle ? parsedInfo.cssStyle : node.labelStyle, true, true));
        let bbox = lbl.getBBox();
        if ((0, _chunkDD37ZF33Mjs.evaluate)((0, _chunkDD37ZF33Mjs.getConfig2)().flowchart.htmlLabels)) {
            const div = lbl.children[0];
            const dv = (0, _chunkDD37ZF33Mjs.select_default)(lbl);
            bbox = div.getBoundingClientRect();
            dv.attr("width", bbox.width);
            dv.attr("height", bbox.height);
        }
        if (bbox.width > maxWidth) maxWidth = bbox.width;
        maxHeight += bbox.height + rowPadding;
        classAttributes.push(lbl);
    });
    maxHeight += lineHeight;
    const classMethods = [];
    node.classData.methods.forEach((member)=>{
        const parsedInfo = member.getDisplayDetails();
        let displayText = parsedInfo.displayText;
        if ((0, _chunkDD37ZF33Mjs.getConfig2)().flowchart.htmlLabels) displayText = displayText.replace(/</g, "&lt;").replace(/>/g, "&gt;");
        const lbl = labelContainer.node().appendChild(createLabel_default(displayText, parsedInfo.cssStyle ? parsedInfo.cssStyle : node.labelStyle, true, true));
        let bbox = lbl.getBBox();
        if ((0, _chunkDD37ZF33Mjs.evaluate)((0, _chunkDD37ZF33Mjs.getConfig2)().flowchart.htmlLabels)) {
            const div = lbl.children[0];
            const dv = (0, _chunkDD37ZF33Mjs.select_default)(lbl);
            bbox = div.getBoundingClientRect();
            dv.attr("width", bbox.width);
            dv.attr("height", bbox.height);
        }
        if (bbox.width > maxWidth) maxWidth = bbox.width;
        maxHeight += bbox.height + rowPadding;
        classMethods.push(lbl);
    });
    maxHeight += lineHeight;
    if (hasInterface) {
        let diffX2 = (maxWidth - interfaceBBox.width) / 2;
        (0, _chunkDD37ZF33Mjs.select_default)(interfaceLabel).attr("transform", "translate( " + (-1 * maxWidth / 2 + diffX2) + ", " + -1 * maxHeight / 2 + ")");
        verticalPos = interfaceBBox.height + rowPadding;
    }
    let diffX = (maxWidth - classTitleBBox.width) / 2;
    (0, _chunkDD37ZF33Mjs.select_default)(classTitleLabel).attr("transform", "translate( " + (-1 * maxWidth / 2 + diffX) + ", " + (-1 * maxHeight / 2 + verticalPos) + ")");
    verticalPos += classTitleBBox.height + rowPadding;
    topLine.attr("class", "divider").attr("x1", -maxWidth / 2 - halfPadding).attr("x2", maxWidth / 2 + halfPadding).attr("y1", -maxHeight / 2 - halfPadding + lineHeight + verticalPos).attr("y2", -maxHeight / 2 - halfPadding + lineHeight + verticalPos);
    verticalPos += lineHeight;
    classAttributes.forEach((lbl)=>{
        (0, _chunkDD37ZF33Mjs.select_default)(lbl).attr("transform", "translate( " + -maxWidth / 2 + ", " + (-1 * maxHeight / 2 + verticalPos + lineHeight / 2) + ")");
        const memberBBox = lbl?.getBBox();
        verticalPos += (memberBBox?.height ?? 0) + rowPadding;
    });
    verticalPos += lineHeight;
    bottomLine.attr("class", "divider").attr("x1", -maxWidth / 2 - halfPadding).attr("x2", maxWidth / 2 + halfPadding).attr("y1", -maxHeight / 2 - halfPadding + lineHeight + verticalPos).attr("y2", -maxHeight / 2 - halfPadding + lineHeight + verticalPos);
    verticalPos += lineHeight;
    classMethods.forEach((lbl)=>{
        (0, _chunkDD37ZF33Mjs.select_default)(lbl).attr("transform", "translate( " + -maxWidth / 2 + ", " + (-1 * maxHeight / 2 + verticalPos) + ")");
        const memberBBox = lbl?.getBBox();
        verticalPos += (memberBBox?.height ?? 0) + rowPadding;
    });
    rect2.attr("style", node.style).attr("class", "outer title-state").attr("x", -maxWidth / 2 - halfPadding).attr("y", -(maxHeight / 2) - halfPadding).attr("width", maxWidth + node.padding).attr("height", maxHeight + node.padding);
    updateNodeBounds(node, rect2);
    node.intersect = function(point) {
        return intersect_default.rect(node, point);
    };
    return shapeSvg;
}, "class_box");
var shapes = {
    rhombus: question,
    composite,
    question,
    rect,
    labelRect,
    rectWithTitle,
    choice,
    circle,
    doublecircle,
    stadium,
    hexagon,
    block_arrow,
    rect_left_inv_arrow,
    lean_right,
    lean_left,
    trapezoid,
    inv_trapezoid,
    rect_right_inv_arrow,
    cylinder,
    start,
    end,
    note: note_default,
    subroutine,
    fork: forkJoin,
    join: forkJoin,
    class_box
};
var nodeElems = {};
var insertNode = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(async (elem, node, dir)=>{
    let newEl;
    let el;
    if (node.link) {
        let target;
        if ((0, _chunkDD37ZF33Mjs.getConfig2)().securityLevel === "sandbox") target = "_top";
        else if (node.linkTarget) target = node.linkTarget || "_blank";
        newEl = elem.insert("svg:a").attr("xlink:href", node.link).attr("target", target);
        el = await shapes[node.shape](newEl, node, dir);
    } else {
        el = await shapes[node.shape](elem, node, dir);
        newEl = el;
    }
    if (node.tooltip) el.attr("title", node.tooltip);
    if (node.class) el.attr("class", "node default " + node.class);
    nodeElems[node.id] = newEl;
    if (node.haveCallback) nodeElems[node.id].attr("class", nodeElems[node.id].attr("class") + " clickable");
    return newEl;
}, "insertNode");
var setNodeElem = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((elem, node)=>{
    nodeElems[node.id] = elem;
}, "setNodeElem");
var clear = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(()=>{
    nodeElems = {};
}, "clear");
var positionNode = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((node)=>{
    const el = nodeElems[node.id];
    (0, _chunkDD37ZF33Mjs.log).trace("Transforming node", node.diff, node, "translate(" + (node.x - node.width / 2 - 5) + ", " + node.width / 2 + ")");
    const padding = 8;
    const diff = node.diff || 0;
    if (node.clusterNode) el.attr("transform", "translate(" + (node.x + diff - node.width / 2) + ", " + (node.y - node.height / 2 - padding) + ")");
    else el.attr("transform", "translate(" + node.x + ", " + node.y + ")");
    return diff;
}, "positionNode");

},{"./chunk-YP6PVJQ3.mjs":"21NKC","./chunk-I7ZFS43C.mjs":"huUtc","./chunk-DD37ZF33.mjs":"f4pI5","./chunk-DLQEHMXD.mjs":"h5YRf","@parcel/transformer-js/src/esmodule-helpers.js":"1cdyk"}]},["kBEHH"], null, "parcelRequire6955", {})

//# sourceMappingURL=flowDiagram-67NZXU7I.adcb51b1.js.map
