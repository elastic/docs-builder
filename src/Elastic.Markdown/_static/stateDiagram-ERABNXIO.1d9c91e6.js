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
})({"hVlQA":[function(require,module,exports,__globalThis) {
var global = arguments[3];
var HMR_HOST = null;
var HMR_PORT = 1234;
var HMR_SERVER_PORT = 1234;
var HMR_SECURE = false;
var HMR_ENV_HASH = "febfef3c5b467b16";
var HMR_USE_SSE = false;
module.bundle.HMR_BUNDLE_ID = "1eeae7fe1d9c91e6";
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

},{}],"6kD3k":[function(require,module,exports,__globalThis) {
var parcelHelpers = require("@parcel/transformer-js/src/esmodule-helpers.js");
parcelHelpers.defineInteropFlag(exports);
parcelHelpers.export(exports, "stateDiagram_default", ()=>stateDiagram_default);
parcelHelpers.export(exports, "stateRenderer_v3_unified_default", ()=>stateRenderer_v3_unified_default);
parcelHelpers.export(exports, "stateDb_default", ()=>stateDb_default);
parcelHelpers.export(exports, "styles_default", ()=>styles_default);
var _chunkGVMN75T7Mjs = require("./chunk-GVMN75T7.mjs");
var _chunkM7N4Q5GZMjs = require("./chunk-M7N4Q5GZ.mjs");
var _chunkI7ZFS43CMjs = require("./chunk-I7ZFS43C.mjs");
var _chunkDD37ZF33Mjs = require("./chunk-DD37ZF33.mjs");
var _chunkDLQEHMXDMjs = require("./chunk-DLQEHMXD.mjs");
// src/diagrams/state/parser/stateDiagram.jison
var parser = function() {
    var o = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(k, v, o2, l) {
        for(o2 = o2 || {}, l = k.length; l--; o2[k[l]] = v);
        return o2;
    }, "o"), $V0 = [
        1,
        2
    ], $V1 = [
        1,
        3
    ], $V2 = [
        1,
        4
    ], $V3 = [
        2,
        4
    ], $V4 = [
        1,
        9
    ], $V5 = [
        1,
        11
    ], $V6 = [
        1,
        16
    ], $V7 = [
        1,
        17
    ], $V8 = [
        1,
        18
    ], $V9 = [
        1,
        19
    ], $Va = [
        1,
        32
    ], $Vb = [
        1,
        20
    ], $Vc = [
        1,
        21
    ], $Vd = [
        1,
        22
    ], $Ve = [
        1,
        23
    ], $Vf = [
        1,
        24
    ], $Vg = [
        1,
        26
    ], $Vh = [
        1,
        27
    ], $Vi = [
        1,
        28
    ], $Vj = [
        1,
        29
    ], $Vk = [
        1,
        30
    ], $Vl = [
        1,
        31
    ], $Vm = [
        1,
        34
    ], $Vn = [
        1,
        35
    ], $Vo = [
        1,
        36
    ], $Vp = [
        1,
        37
    ], $Vq = [
        1,
        33
    ], $Vr = [
        1,
        4,
        5,
        16,
        17,
        19,
        21,
        22,
        24,
        25,
        26,
        27,
        28,
        29,
        33,
        35,
        37,
        38,
        42,
        45,
        48,
        49,
        50,
        51,
        54
    ], $Vs = [
        1,
        4,
        5,
        14,
        15,
        16,
        17,
        19,
        21,
        22,
        24,
        25,
        26,
        27,
        28,
        29,
        33,
        35,
        37,
        38,
        42,
        45,
        48,
        49,
        50,
        51,
        54
    ], $Vt = [
        4,
        5,
        16,
        17,
        19,
        21,
        22,
        24,
        25,
        26,
        27,
        28,
        29,
        33,
        35,
        37,
        38,
        42,
        45,
        48,
        49,
        50,
        51,
        54
    ];
    var parser2 = {
        trace: /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function trace() {}, "trace"),
        yy: {},
        symbols_: {
            "error": 2,
            "start": 3,
            "SPACE": 4,
            "NL": 5,
            "SD": 6,
            "document": 7,
            "line": 8,
            "statement": 9,
            "classDefStatement": 10,
            "styleStatement": 11,
            "cssClassStatement": 12,
            "idStatement": 13,
            "DESCR": 14,
            "-->": 15,
            "HIDE_EMPTY": 16,
            "scale": 17,
            "WIDTH": 18,
            "COMPOSIT_STATE": 19,
            "STRUCT_START": 20,
            "STRUCT_STOP": 21,
            "STATE_DESCR": 22,
            "AS": 23,
            "ID": 24,
            "FORK": 25,
            "JOIN": 26,
            "CHOICE": 27,
            "CONCURRENT": 28,
            "note": 29,
            "notePosition": 30,
            "NOTE_TEXT": 31,
            "direction": 32,
            "acc_title": 33,
            "acc_title_value": 34,
            "acc_descr": 35,
            "acc_descr_value": 36,
            "acc_descr_multiline_value": 37,
            "classDef": 38,
            "CLASSDEF_ID": 39,
            "CLASSDEF_STYLEOPTS": 40,
            "DEFAULT": 41,
            "style": 42,
            "STYLE_IDS": 43,
            "STYLEDEF_STYLEOPTS": 44,
            "class": 45,
            "CLASSENTITY_IDS": 46,
            "STYLECLASS": 47,
            "direction_tb": 48,
            "direction_bt": 49,
            "direction_rl": 50,
            "direction_lr": 51,
            "eol": 52,
            ";": 53,
            "EDGE_STATE": 54,
            "STYLE_SEPARATOR": 55,
            "left_of": 56,
            "right_of": 57,
            "$accept": 0,
            "$end": 1
        },
        terminals_: {
            2: "error",
            4: "SPACE",
            5: "NL",
            6: "SD",
            14: "DESCR",
            15: "-->",
            16: "HIDE_EMPTY",
            17: "scale",
            18: "WIDTH",
            19: "COMPOSIT_STATE",
            20: "STRUCT_START",
            21: "STRUCT_STOP",
            22: "STATE_DESCR",
            23: "AS",
            24: "ID",
            25: "FORK",
            26: "JOIN",
            27: "CHOICE",
            28: "CONCURRENT",
            29: "note",
            31: "NOTE_TEXT",
            33: "acc_title",
            34: "acc_title_value",
            35: "acc_descr",
            36: "acc_descr_value",
            37: "acc_descr_multiline_value",
            38: "classDef",
            39: "CLASSDEF_ID",
            40: "CLASSDEF_STYLEOPTS",
            41: "DEFAULT",
            42: "style",
            43: "STYLE_IDS",
            44: "STYLEDEF_STYLEOPTS",
            45: "class",
            46: "CLASSENTITY_IDS",
            47: "STYLECLASS",
            48: "direction_tb",
            49: "direction_bt",
            50: "direction_rl",
            51: "direction_lr",
            53: ";",
            54: "EDGE_STATE",
            55: "STYLE_SEPARATOR",
            56: "left_of",
            57: "right_of"
        },
        productions_: [
            0,
            [
                3,
                2
            ],
            [
                3,
                2
            ],
            [
                3,
                2
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
                8,
                2
            ],
            [
                8,
                1
            ],
            [
                8,
                1
            ],
            [
                9,
                1
            ],
            [
                9,
                1
            ],
            [
                9,
                1
            ],
            [
                9,
                1
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
                4
            ],
            [
                9,
                1
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
                9,
                4
            ],
            [
                9,
                3
            ],
            [
                9,
                6
            ],
            [
                9,
                1
            ],
            [
                9,
                1
            ],
            [
                9,
                1
            ],
            [
                9,
                1
            ],
            [
                9,
                4
            ],
            [
                9,
                4
            ],
            [
                9,
                1
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
                10,
                3
            ],
            [
                10,
                3
            ],
            [
                11,
                3
            ],
            [
                12,
                3
            ],
            [
                32,
                1
            ],
            [
                32,
                1
            ],
            [
                32,
                1
            ],
            [
                32,
                1
            ],
            [
                52,
                1
            ],
            [
                52,
                1
            ],
            [
                13,
                1
            ],
            [
                13,
                1
            ],
            [
                13,
                3
            ],
            [
                13,
                3
            ],
            [
                30,
                1
            ],
            [
                30,
                1
            ]
        ],
        performAction: /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function anonymous(yytext, yyleng, yylineno, yy, yystate, $$, _$) {
            var $0 = $$.length - 1;
            switch(yystate){
                case 3:
                    yy.setRootDoc($$[$0]);
                    return $$[$0];
                case 4:
                    this.$ = [];
                    break;
                case 5:
                    if ($$[$0] != "nl") {
                        $$[$0 - 1].push($$[$0]);
                        this.$ = $$[$0 - 1];
                    }
                    break;
                case 6:
                case 7:
                    this.$ = $$[$0];
                    break;
                case 8:
                    this.$ = "nl";
                    break;
                case 12:
                    this.$ = $$[$0];
                    break;
                case 13:
                    const stateStmt = $$[$0 - 1];
                    stateStmt.description = yy.trimColon($$[$0]);
                    this.$ = stateStmt;
                    break;
                case 14:
                    this.$ = {
                        stmt: "relation",
                        state1: $$[$0 - 2],
                        state2: $$[$0]
                    };
                    break;
                case 15:
                    const relDescription = yy.trimColon($$[$0]);
                    this.$ = {
                        stmt: "relation",
                        state1: $$[$0 - 3],
                        state2: $$[$0 - 1],
                        description: relDescription
                    };
                    break;
                case 19:
                    this.$ = {
                        stmt: "state",
                        id: $$[$0 - 3],
                        type: "default",
                        description: "",
                        doc: $$[$0 - 1]
                    };
                    break;
                case 20:
                    var id = $$[$0];
                    var description = $$[$0 - 2].trim();
                    if ($$[$0].match(":")) {
                        var parts = $$[$0].split(":");
                        id = parts[0];
                        description = [
                            description,
                            parts[1]
                        ];
                    }
                    this.$ = {
                        stmt: "state",
                        id,
                        type: "default",
                        description
                    };
                    break;
                case 21:
                    this.$ = {
                        stmt: "state",
                        id: $$[$0 - 3],
                        type: "default",
                        description: $$[$0 - 5],
                        doc: $$[$0 - 1]
                    };
                    break;
                case 22:
                    this.$ = {
                        stmt: "state",
                        id: $$[$0],
                        type: "fork"
                    };
                    break;
                case 23:
                    this.$ = {
                        stmt: "state",
                        id: $$[$0],
                        type: "join"
                    };
                    break;
                case 24:
                    this.$ = {
                        stmt: "state",
                        id: $$[$0],
                        type: "choice"
                    };
                    break;
                case 25:
                    this.$ = {
                        stmt: "state",
                        id: yy.getDividerId(),
                        type: "divider"
                    };
                    break;
                case 26:
                    this.$ = {
                        stmt: "state",
                        id: $$[$0 - 1].trim(),
                        note: {
                            position: $$[$0 - 2].trim(),
                            text: $$[$0].trim()
                        }
                    };
                    break;
                case 29:
                    this.$ = $$[$0].trim();
                    yy.setAccTitle(this.$);
                    break;
                case 30:
                case 31:
                    this.$ = $$[$0].trim();
                    yy.setAccDescription(this.$);
                    break;
                case 32:
                case 33:
                    this.$ = {
                        stmt: "classDef",
                        id: $$[$0 - 1].trim(),
                        classes: $$[$0].trim()
                    };
                    break;
                case 34:
                    this.$ = {
                        stmt: "style",
                        id: $$[$0 - 1].trim(),
                        styleClass: $$[$0].trim()
                    };
                    break;
                case 35:
                    this.$ = {
                        stmt: "applyClass",
                        id: $$[$0 - 1].trim(),
                        styleClass: $$[$0].trim()
                    };
                    break;
                case 36:
                    yy.setDirection("TB");
                    this.$ = {
                        stmt: "dir",
                        value: "TB"
                    };
                    break;
                case 37:
                    yy.setDirection("BT");
                    this.$ = {
                        stmt: "dir",
                        value: "BT"
                    };
                    break;
                case 38:
                    yy.setDirection("RL");
                    this.$ = {
                        stmt: "dir",
                        value: "RL"
                    };
                    break;
                case 39:
                    yy.setDirection("LR");
                    this.$ = {
                        stmt: "dir",
                        value: "LR"
                    };
                    break;
                case 42:
                case 43:
                    this.$ = {
                        stmt: "state",
                        id: $$[$0].trim(),
                        type: "default",
                        description: ""
                    };
                    break;
                case 44:
                    this.$ = {
                        stmt: "state",
                        id: $$[$0 - 2].trim(),
                        classes: [
                            $$[$0].trim()
                        ],
                        type: "default",
                        description: ""
                    };
                    break;
                case 45:
                    this.$ = {
                        stmt: "state",
                        id: $$[$0 - 2].trim(),
                        classes: [
                            $$[$0].trim()
                        ],
                        type: "default",
                        description: ""
                    };
                    break;
            }
        }, "anonymous"),
        table: [
            {
                3: 1,
                4: $V0,
                5: $V1,
                6: $V2
            },
            {
                1: [
                    3
                ]
            },
            {
                3: 5,
                4: $V0,
                5: $V1,
                6: $V2
            },
            {
                3: 6,
                4: $V0,
                5: $V1,
                6: $V2
            },
            o([
                1,
                4,
                5,
                16,
                17,
                19,
                22,
                24,
                25,
                26,
                27,
                28,
                29,
                33,
                35,
                37,
                38,
                42,
                45,
                48,
                49,
                50,
                51,
                54
            ], $V3, {
                7: 7
            }),
            {
                1: [
                    2,
                    1
                ]
            },
            {
                1: [
                    2,
                    2
                ]
            },
            {
                1: [
                    2,
                    3
                ],
                4: $V4,
                5: $V5,
                8: 8,
                9: 10,
                10: 12,
                11: 13,
                12: 14,
                13: 15,
                16: $V6,
                17: $V7,
                19: $V8,
                22: $V9,
                24: $Va,
                25: $Vb,
                26: $Vc,
                27: $Vd,
                28: $Ve,
                29: $Vf,
                32: 25,
                33: $Vg,
                35: $Vh,
                37: $Vi,
                38: $Vj,
                42: $Vk,
                45: $Vl,
                48: $Vm,
                49: $Vn,
                50: $Vo,
                51: $Vp,
                54: $Vq
            },
            o($Vr, [
                2,
                5
            ]),
            {
                9: 38,
                10: 12,
                11: 13,
                12: 14,
                13: 15,
                16: $V6,
                17: $V7,
                19: $V8,
                22: $V9,
                24: $Va,
                25: $Vb,
                26: $Vc,
                27: $Vd,
                28: $Ve,
                29: $Vf,
                32: 25,
                33: $Vg,
                35: $Vh,
                37: $Vi,
                38: $Vj,
                42: $Vk,
                45: $Vl,
                48: $Vm,
                49: $Vn,
                50: $Vo,
                51: $Vp,
                54: $Vq
            },
            o($Vr, [
                2,
                7
            ]),
            o($Vr, [
                2,
                8
            ]),
            o($Vr, [
                2,
                9
            ]),
            o($Vr, [
                2,
                10
            ]),
            o($Vr, [
                2,
                11
            ]),
            o($Vr, [
                2,
                12
            ], {
                14: [
                    1,
                    39
                ],
                15: [
                    1,
                    40
                ]
            }),
            o($Vr, [
                2,
                16
            ]),
            {
                18: [
                    1,
                    41
                ]
            },
            o($Vr, [
                2,
                18
            ], {
                20: [
                    1,
                    42
                ]
            }),
            {
                23: [
                    1,
                    43
                ]
            },
            o($Vr, [
                2,
                22
            ]),
            o($Vr, [
                2,
                23
            ]),
            o($Vr, [
                2,
                24
            ]),
            o($Vr, [
                2,
                25
            ]),
            {
                30: 44,
                31: [
                    1,
                    45
                ],
                56: [
                    1,
                    46
                ],
                57: [
                    1,
                    47
                ]
            },
            o($Vr, [
                2,
                28
            ]),
            {
                34: [
                    1,
                    48
                ]
            },
            {
                36: [
                    1,
                    49
                ]
            },
            o($Vr, [
                2,
                31
            ]),
            {
                39: [
                    1,
                    50
                ],
                41: [
                    1,
                    51
                ]
            },
            {
                43: [
                    1,
                    52
                ]
            },
            {
                46: [
                    1,
                    53
                ]
            },
            o($Vs, [
                2,
                42
            ], {
                55: [
                    1,
                    54
                ]
            }),
            o($Vs, [
                2,
                43
            ], {
                55: [
                    1,
                    55
                ]
            }),
            o($Vr, [
                2,
                36
            ]),
            o($Vr, [
                2,
                37
            ]),
            o($Vr, [
                2,
                38
            ]),
            o($Vr, [
                2,
                39
            ]),
            o($Vr, [
                2,
                6
            ]),
            o($Vr, [
                2,
                13
            ]),
            {
                13: 56,
                24: $Va,
                54: $Vq
            },
            o($Vr, [
                2,
                17
            ]),
            o($Vt, $V3, {
                7: 57
            }),
            {
                24: [
                    1,
                    58
                ]
            },
            {
                24: [
                    1,
                    59
                ]
            },
            {
                23: [
                    1,
                    60
                ]
            },
            {
                24: [
                    2,
                    46
                ]
            },
            {
                24: [
                    2,
                    47
                ]
            },
            o($Vr, [
                2,
                29
            ]),
            o($Vr, [
                2,
                30
            ]),
            {
                40: [
                    1,
                    61
                ]
            },
            {
                40: [
                    1,
                    62
                ]
            },
            {
                44: [
                    1,
                    63
                ]
            },
            {
                47: [
                    1,
                    64
                ]
            },
            {
                24: [
                    1,
                    65
                ]
            },
            {
                24: [
                    1,
                    66
                ]
            },
            o($Vr, [
                2,
                14
            ], {
                14: [
                    1,
                    67
                ]
            }),
            {
                4: $V4,
                5: $V5,
                8: 8,
                9: 10,
                10: 12,
                11: 13,
                12: 14,
                13: 15,
                16: $V6,
                17: $V7,
                19: $V8,
                21: [
                    1,
                    68
                ],
                22: $V9,
                24: $Va,
                25: $Vb,
                26: $Vc,
                27: $Vd,
                28: $Ve,
                29: $Vf,
                32: 25,
                33: $Vg,
                35: $Vh,
                37: $Vi,
                38: $Vj,
                42: $Vk,
                45: $Vl,
                48: $Vm,
                49: $Vn,
                50: $Vo,
                51: $Vp,
                54: $Vq
            },
            o($Vr, [
                2,
                20
            ], {
                20: [
                    1,
                    69
                ]
            }),
            {
                31: [
                    1,
                    70
                ]
            },
            {
                24: [
                    1,
                    71
                ]
            },
            o($Vr, [
                2,
                32
            ]),
            o($Vr, [
                2,
                33
            ]),
            o($Vr, [
                2,
                34
            ]),
            o($Vr, [
                2,
                35
            ]),
            o($Vs, [
                2,
                44
            ]),
            o($Vs, [
                2,
                45
            ]),
            o($Vr, [
                2,
                15
            ]),
            o($Vr, [
                2,
                19
            ]),
            o($Vt, $V3, {
                7: 72
            }),
            o($Vr, [
                2,
                26
            ]),
            o($Vr, [
                2,
                27
            ]),
            {
                4: $V4,
                5: $V5,
                8: 8,
                9: 10,
                10: 12,
                11: 13,
                12: 14,
                13: 15,
                16: $V6,
                17: $V7,
                19: $V8,
                21: [
                    1,
                    73
                ],
                22: $V9,
                24: $Va,
                25: $Vb,
                26: $Vc,
                27: $Vd,
                28: $Ve,
                29: $Vf,
                32: 25,
                33: $Vg,
                35: $Vh,
                37: $Vi,
                38: $Vj,
                42: $Vk,
                45: $Vl,
                48: $Vm,
                49: $Vn,
                50: $Vo,
                51: $Vp,
                54: $Vq
            },
            o($Vr, [
                2,
                21
            ])
        ],
        defaultActions: {
            5: [
                2,
                1
            ],
            6: [
                2,
                2
            ],
            46: [
                2,
                46
            ],
            47: [
                2,
                47
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
                        return 41;
                    case 1:
                        return 48;
                    case 2:
                        return 49;
                    case 3:
                        return 50;
                    case 4:
                        return 51;
                    case 5:
                        break;
                    case 6:
                        break;
                    case 7:
                        return 5;
                    case 8:
                        break;
                    case 9:
                        break;
                    case 10:
                        break;
                    case 11:
                        break;
                    case 12:
                        this.pushState("SCALE");
                        return 17;
                    case 13:
                        return 18;
                    case 14:
                        this.popState();
                        break;
                    case 15:
                        this.begin("acc_title");
                        return 33;
                    case 16:
                        this.popState();
                        return "acc_title_value";
                    case 17:
                        this.begin("acc_descr");
                        return 35;
                    case 18:
                        this.popState();
                        return "acc_descr_value";
                    case 19:
                        this.begin("acc_descr_multiline");
                        break;
                    case 20:
                        this.popState();
                        break;
                    case 21:
                        return "acc_descr_multiline_value";
                    case 22:
                        this.pushState("CLASSDEF");
                        return 38;
                    case 23:
                        this.popState();
                        this.pushState("CLASSDEFID");
                        return "DEFAULT_CLASSDEF_ID";
                    case 24:
                        this.popState();
                        this.pushState("CLASSDEFID");
                        return 39;
                    case 25:
                        this.popState();
                        return 40;
                    case 26:
                        this.pushState("CLASS");
                        return 45;
                    case 27:
                        this.popState();
                        this.pushState("CLASS_STYLE");
                        return 46;
                    case 28:
                        this.popState();
                        return 47;
                    case 29:
                        this.pushState("STYLE");
                        return 42;
                    case 30:
                        this.popState();
                        this.pushState("STYLEDEF_STYLES");
                        return 43;
                    case 31:
                        this.popState();
                        return 44;
                    case 32:
                        this.pushState("SCALE");
                        return 17;
                    case 33:
                        return 18;
                    case 34:
                        this.popState();
                        break;
                    case 35:
                        this.pushState("STATE");
                        break;
                    case 36:
                        this.popState();
                        yy_.yytext = yy_.yytext.slice(0, -8).trim();
                        return 25;
                    case 37:
                        this.popState();
                        yy_.yytext = yy_.yytext.slice(0, -8).trim();
                        return 26;
                    case 38:
                        this.popState();
                        yy_.yytext = yy_.yytext.slice(0, -10).trim();
                        return 27;
                    case 39:
                        this.popState();
                        yy_.yytext = yy_.yytext.slice(0, -8).trim();
                        return 25;
                    case 40:
                        this.popState();
                        yy_.yytext = yy_.yytext.slice(0, -8).trim();
                        return 26;
                    case 41:
                        this.popState();
                        yy_.yytext = yy_.yytext.slice(0, -10).trim();
                        return 27;
                    case 42:
                        return 48;
                    case 43:
                        return 49;
                    case 44:
                        return 50;
                    case 45:
                        return 51;
                    case 46:
                        this.pushState("STATE_STRING");
                        break;
                    case 47:
                        this.pushState("STATE_ID");
                        return "AS";
                    case 48:
                        this.popState();
                        return "ID";
                    case 49:
                        this.popState();
                        break;
                    case 50:
                        return "STATE_DESCR";
                    case 51:
                        return 19;
                    case 52:
                        this.popState();
                        break;
                    case 53:
                        this.popState();
                        this.pushState("struct");
                        return 20;
                    case 54:
                        break;
                    case 55:
                        this.popState();
                        return 21;
                    case 56:
                        break;
                    case 57:
                        this.begin("NOTE");
                        return 29;
                    case 58:
                        this.popState();
                        this.pushState("NOTE_ID");
                        return 56;
                    case 59:
                        this.popState();
                        this.pushState("NOTE_ID");
                        return 57;
                    case 60:
                        this.popState();
                        this.pushState("FLOATING_NOTE");
                        break;
                    case 61:
                        this.popState();
                        this.pushState("FLOATING_NOTE_ID");
                        return "AS";
                    case 62:
                        break;
                    case 63:
                        return "NOTE_TEXT";
                    case 64:
                        this.popState();
                        return "ID";
                    case 65:
                        this.popState();
                        this.pushState("NOTE_TEXT");
                        return 24;
                    case 66:
                        this.popState();
                        yy_.yytext = yy_.yytext.substr(2).trim();
                        return 31;
                    case 67:
                        this.popState();
                        yy_.yytext = yy_.yytext.slice(0, -8).trim();
                        return 31;
                    case 68:
                        return 6;
                    case 69:
                        return 6;
                    case 70:
                        return 16;
                    case 71:
                        return 54;
                    case 72:
                        return 24;
                    case 73:
                        yy_.yytext = yy_.yytext.trim();
                        return 14;
                    case 74:
                        return 15;
                    case 75:
                        return 28;
                    case 76:
                        return 55;
                    case 77:
                        return 5;
                    case 78:
                        return "INVALID";
                }
            }, "anonymous"),
            rules: [
                /^(?:default\b)/i,
                /^(?:.*direction\s+TB[^\n]*)/i,
                /^(?:.*direction\s+BT[^\n]*)/i,
                /^(?:.*direction\s+RL[^\n]*)/i,
                /^(?:.*direction\s+LR[^\n]*)/i,
                /^(?:%%(?!\{)[^\n]*)/i,
                /^(?:[^\}]%%[^\n]*)/i,
                /^(?:[\n]+)/i,
                /^(?:[\s]+)/i,
                /^(?:((?!\n)\s)+)/i,
                /^(?:#[^\n]*)/i,
                /^(?:%[^\n]*)/i,
                /^(?:scale\s+)/i,
                /^(?:\d+)/i,
                /^(?:\s+width\b)/i,
                /^(?:accTitle\s*:\s*)/i,
                /^(?:(?!\n||)*[^\n]*)/i,
                /^(?:accDescr\s*:\s*)/i,
                /^(?:(?!\n||)*[^\n]*)/i,
                /^(?:accDescr\s*\{\s*)/i,
                /^(?:[\}])/i,
                /^(?:[^\}]*)/i,
                /^(?:classDef\s+)/i,
                /^(?:DEFAULT\s+)/i,
                /^(?:\w+\s+)/i,
                /^(?:[^\n]*)/i,
                /^(?:class\s+)/i,
                /^(?:(\w+)+((,\s*\w+)*))/i,
                /^(?:[^\n]*)/i,
                /^(?:style\s+)/i,
                /^(?:[\w,]+\s+)/i,
                /^(?:[^\n]*)/i,
                /^(?:scale\s+)/i,
                /^(?:\d+)/i,
                /^(?:\s+width\b)/i,
                /^(?:state\s+)/i,
                /^(?:.*<<fork>>)/i,
                /^(?:.*<<join>>)/i,
                /^(?:.*<<choice>>)/i,
                /^(?:.*\[\[fork\]\])/i,
                /^(?:.*\[\[join\]\])/i,
                /^(?:.*\[\[choice\]\])/i,
                /^(?:.*direction\s+TB[^\n]*)/i,
                /^(?:.*direction\s+BT[^\n]*)/i,
                /^(?:.*direction\s+RL[^\n]*)/i,
                /^(?:.*direction\s+LR[^\n]*)/i,
                /^(?:["])/i,
                /^(?:\s*as\s+)/i,
                /^(?:[^\n\{]*)/i,
                /^(?:["])/i,
                /^(?:[^"]*)/i,
                /^(?:[^\n\s\{]+)/i,
                /^(?:\n)/i,
                /^(?:\{)/i,
                /^(?:%%(?!\{)[^\n]*)/i,
                /^(?:\})/i,
                /^(?:[\n])/i,
                /^(?:note\s+)/i,
                /^(?:left of\b)/i,
                /^(?:right of\b)/i,
                /^(?:")/i,
                /^(?:\s*as\s*)/i,
                /^(?:["])/i,
                /^(?:[^"]*)/i,
                /^(?:[^\n]*)/i,
                /^(?:\s*[^:\n\s\-]+)/i,
                /^(?:\s*:[^:\n;]+)/i,
                /^(?:[\s\S]*?end note\b)/i,
                /^(?:stateDiagram\s+)/i,
                /^(?:stateDiagram-v2\s+)/i,
                /^(?:hide empty description\b)/i,
                /^(?:\[\*\])/i,
                /^(?:[^:\n\s\-\{]+)/i,
                /^(?:\s*:[^:\n;]+)/i,
                /^(?:-->)/i,
                /^(?:--)/i,
                /^(?::::)/i,
                /^(?:$)/i,
                /^(?:.)/i
            ],
            conditions: {
                "LINE": {
                    "rules": [
                        9,
                        10
                    ],
                    "inclusive": false
                },
                "struct": {
                    "rules": [
                        9,
                        10,
                        22,
                        26,
                        29,
                        35,
                        42,
                        43,
                        44,
                        45,
                        54,
                        55,
                        56,
                        57,
                        71,
                        72,
                        73,
                        74,
                        75
                    ],
                    "inclusive": false
                },
                "FLOATING_NOTE_ID": {
                    "rules": [
                        64
                    ],
                    "inclusive": false
                },
                "FLOATING_NOTE": {
                    "rules": [
                        61,
                        62,
                        63
                    ],
                    "inclusive": false
                },
                "NOTE_TEXT": {
                    "rules": [
                        66,
                        67
                    ],
                    "inclusive": false
                },
                "NOTE_ID": {
                    "rules": [
                        65
                    ],
                    "inclusive": false
                },
                "NOTE": {
                    "rules": [
                        58,
                        59,
                        60
                    ],
                    "inclusive": false
                },
                "STYLEDEF_STYLEOPTS": {
                    "rules": [],
                    "inclusive": false
                },
                "STYLEDEF_STYLES": {
                    "rules": [
                        31
                    ],
                    "inclusive": false
                },
                "STYLE_IDS": {
                    "rules": [],
                    "inclusive": false
                },
                "STYLE": {
                    "rules": [
                        30
                    ],
                    "inclusive": false
                },
                "CLASS_STYLE": {
                    "rules": [
                        28
                    ],
                    "inclusive": false
                },
                "CLASS": {
                    "rules": [
                        27
                    ],
                    "inclusive": false
                },
                "CLASSDEFID": {
                    "rules": [
                        25
                    ],
                    "inclusive": false
                },
                "CLASSDEF": {
                    "rules": [
                        23,
                        24
                    ],
                    "inclusive": false
                },
                "acc_descr_multiline": {
                    "rules": [
                        20,
                        21
                    ],
                    "inclusive": false
                },
                "acc_descr": {
                    "rules": [
                        18
                    ],
                    "inclusive": false
                },
                "acc_title": {
                    "rules": [
                        16
                    ],
                    "inclusive": false
                },
                "SCALE": {
                    "rules": [
                        13,
                        14,
                        33,
                        34
                    ],
                    "inclusive": false
                },
                "ALIAS": {
                    "rules": [],
                    "inclusive": false
                },
                "STATE_ID": {
                    "rules": [
                        48
                    ],
                    "inclusive": false
                },
                "STATE_STRING": {
                    "rules": [
                        49,
                        50
                    ],
                    "inclusive": false
                },
                "FORK_STATE": {
                    "rules": [],
                    "inclusive": false
                },
                "STATE": {
                    "rules": [
                        9,
                        10,
                        36,
                        37,
                        38,
                        39,
                        40,
                        41,
                        46,
                        47,
                        51,
                        52,
                        53
                    ],
                    "inclusive": false
                },
                "ID": {
                    "rules": [
                        9,
                        10
                    ],
                    "inclusive": false
                },
                "INITIAL": {
                    "rules": [
                        0,
                        1,
                        2,
                        3,
                        4,
                        5,
                        6,
                        7,
                        8,
                        10,
                        11,
                        12,
                        15,
                        17,
                        19,
                        22,
                        26,
                        29,
                        32,
                        35,
                        53,
                        57,
                        68,
                        69,
                        70,
                        71,
                        72,
                        73,
                        74,
                        76,
                        77,
                        78
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
var stateDiagram_default = parser;
// src/diagrams/state/stateCommon.ts
var DEFAULT_DIAGRAM_DIRECTION = "LR";
var DEFAULT_NESTED_DOC_DIR = "TB";
var STMT_STATE = "state";
var STMT_RELATION = "relation";
var STMT_CLASSDEF = "classDef";
var STMT_STYLEDEF = "style";
var STMT_APPLYCLASS = "applyClass";
var DEFAULT_STATE_TYPE = "default";
var DIVIDER_TYPE = "divider";
var G_EDGE_STYLE = "fill:none";
var G_EDGE_ARROWHEADSTYLE = "fill: #333";
var G_EDGE_LABELPOS = "c";
var G_EDGE_LABELTYPE = "text";
var G_EDGE_THICKNESS = "normal";
var SHAPE_STATE = "rect";
var SHAPE_STATE_WITH_DESC = "rectWithTitle";
var SHAPE_START = "stateStart";
var SHAPE_END = "stateEnd";
var SHAPE_DIVIDER = "divider";
var SHAPE_GROUP = "roundedWithTitle";
var SHAPE_NOTE = "note";
var SHAPE_NOTEGROUP = "noteGroup";
var CSS_DIAGRAM = "statediagram";
var CSS_STATE = "state";
var CSS_DIAGRAM_STATE = `${CSS_DIAGRAM}-${CSS_STATE}`;
var CSS_EDGE = "transition";
var CSS_NOTE = "note";
var CSS_NOTE_EDGE = "note-edge";
var CSS_EDGE_NOTE_EDGE = `${CSS_EDGE} ${CSS_NOTE_EDGE}`;
var CSS_DIAGRAM_NOTE = `${CSS_DIAGRAM}-${CSS_NOTE}`;
var CSS_CLUSTER = "cluster";
var CSS_DIAGRAM_CLUSTER = `${CSS_DIAGRAM}-${CSS_CLUSTER}`;
var CSS_CLUSTER_ALT = "cluster-alt";
var CSS_DIAGRAM_CLUSTER_ALT = `${CSS_DIAGRAM}-${CSS_CLUSTER_ALT}`;
var PARENT = "parent";
var NOTE = "note";
var DOMID_STATE = "state";
var DOMID_TYPE_SPACER = "----";
var NOTE_ID = `${DOMID_TYPE_SPACER}${NOTE}`;
var PARENT_ID = `${DOMID_TYPE_SPACER}${PARENT}`;
// src/diagrams/state/stateRenderer-v3-unified.ts
var getDir = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((parsedItem, defaultDir = DEFAULT_NESTED_DOC_DIR)=>{
    if (!parsedItem.doc) return defaultDir;
    let dir = defaultDir;
    for (const parsedItemDoc of parsedItem.doc)if (parsedItemDoc.stmt === "dir") dir = parsedItemDoc.value;
    return dir;
}, "getDir");
var getClasses = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(text, diagramObj) {
    diagramObj.db.extract(diagramObj.db.getRootDocV2());
    return diagramObj.db.getClasses();
}, "getClasses");
var draw = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(async function(text, id, _version, diag) {
    (0, _chunkDD37ZF33Mjs.log).info("REF0:");
    (0, _chunkDD37ZF33Mjs.log).info("Drawing state diagram (v2)", id);
    const { securityLevel, state: conf, layout } = (0, _chunkDD37ZF33Mjs.getConfig2)();
    diag.db.extract(diag.db.getRootDocV2());
    const data4Layout = diag.db.getData();
    const svg = (0, _chunkGVMN75T7Mjs.getDiagramElement)(id, securityLevel);
    data4Layout.type = diag.type;
    data4Layout.layoutAlgorithm = layout;
    data4Layout.nodeSpacing = conf?.nodeSpacing || 50;
    data4Layout.rankSpacing = conf?.rankSpacing || 50;
    data4Layout.markers = [
        "barb"
    ];
    data4Layout.diagramId = id;
    await (0, _chunkM7N4Q5GZMjs.render)(data4Layout, svg);
    const padding = 8;
    (0, _chunkI7ZFS43CMjs.utils_default).insertTitle(svg, "statediagramTitleText", conf?.titleTopMargin ?? 25, diag.db.getDiagramTitle());
    (0, _chunkGVMN75T7Mjs.setupViewPortForSVG)(svg, padding, CSS_DIAGRAM, conf?.useMaxWidth ?? true);
}, "draw");
var stateRenderer_v3_unified_default = {
    getClasses,
    draw,
    getDir
};
// src/diagrams/state/dataFetcher.js
var nodeDb = /* @__PURE__ */ new Map();
var graphItemCount = 0;
function stateDomId(itemId = "", counter = 0, type = "", typeSpacer = DOMID_TYPE_SPACER) {
    const typeStr = type !== null && type.length > 0 ? `${typeSpacer}${type}` : "";
    return `${DOMID_STATE}-${itemId}${typeStr}-${counter}`;
}
(0, _chunkDLQEHMXDMjs.__name)(stateDomId, "stateDomId");
var setupDoc = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((parentParsedItem, doc, diagramStates, nodes2, edges2, altFlag, look, classes2)=>{
    (0, _chunkDD37ZF33Mjs.log).trace("items", doc);
    doc.forEach((item)=>{
        switch(item.stmt){
            case STMT_STATE:
                dataFetcher(parentParsedItem, item, diagramStates, nodes2, edges2, altFlag, look, classes2);
                break;
            case DEFAULT_STATE_TYPE:
                dataFetcher(parentParsedItem, item, diagramStates, nodes2, edges2, altFlag, look, classes2);
                break;
            case STMT_RELATION:
                {
                    dataFetcher(parentParsedItem, item.state1, diagramStates, nodes2, edges2, altFlag, look, classes2);
                    dataFetcher(parentParsedItem, item.state2, diagramStates, nodes2, edges2, altFlag, look, classes2);
                    const edgeData = {
                        id: "edge" + graphItemCount,
                        start: item.state1.id,
                        end: item.state2.id,
                        arrowhead: "normal",
                        arrowTypeEnd: "arrow_barb",
                        style: G_EDGE_STYLE,
                        labelStyle: "",
                        label: (0, _chunkDD37ZF33Mjs.common_default).sanitizeText(item.description, (0, _chunkDD37ZF33Mjs.getConfig2)()),
                        arrowheadStyle: G_EDGE_ARROWHEADSTYLE,
                        labelpos: G_EDGE_LABELPOS,
                        labelType: G_EDGE_LABELTYPE,
                        thickness: G_EDGE_THICKNESS,
                        classes: CSS_EDGE,
                        look
                    };
                    edges2.push(edgeData);
                    graphItemCount++;
                }
                break;
        }
    });
}, "setupDoc");
var getDir2 = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((parsedItem, defaultDir = DEFAULT_NESTED_DOC_DIR)=>{
    let dir = defaultDir;
    if (parsedItem.doc) {
        for (const parsedItemDoc of parsedItem.doc)if (parsedItemDoc.stmt === "dir") dir = parsedItemDoc.value;
    }
    return dir;
}, "getDir");
function insertOrUpdateNode(nodes2, nodeData, classes2) {
    if (!nodeData.id || nodeData.id === "</join></fork>" || nodeData.id === "</choice>") return;
    if (nodeData.cssClasses) {
        if (!Array.isArray(nodeData.cssCompiledStyles)) nodeData.cssCompiledStyles = [];
        nodeData.cssClasses.split(" ").forEach((cssClass)=>{
            if (classes2.get(cssClass)) {
                const classDef = classes2.get(cssClass);
                nodeData.cssCompiledStyles = [
                    ...nodeData.cssCompiledStyles,
                    ...classDef.styles
                ];
            }
        });
    }
    const existingNodeData = nodes2.find((node)=>node.id === nodeData.id);
    if (existingNodeData) Object.assign(existingNodeData, nodeData);
    else nodes2.push(nodeData);
}
(0, _chunkDLQEHMXDMjs.__name)(insertOrUpdateNode, "insertOrUpdateNode");
function getClassesFromDbInfo(dbInfoItem) {
    return dbInfoItem?.classes?.join(" ") ?? "";
}
(0, _chunkDLQEHMXDMjs.__name)(getClassesFromDbInfo, "getClassesFromDbInfo");
function getStylesFromDbInfo(dbInfoItem) {
    return dbInfoItem?.styles ?? [];
}
(0, _chunkDLQEHMXDMjs.__name)(getStylesFromDbInfo, "getStylesFromDbInfo");
var dataFetcher = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((parent, parsedItem, diagramStates, nodes2, edges2, altFlag, look, classes2)=>{
    const itemId = parsedItem.id;
    const dbState = diagramStates.get(itemId);
    const classStr = getClassesFromDbInfo(dbState);
    const style = getStylesFromDbInfo(dbState);
    (0, _chunkDD37ZF33Mjs.log).info("dataFetcher parsedItem", parsedItem, dbState, style);
    if (itemId !== "root") {
        let shape = SHAPE_STATE;
        if (parsedItem.start === true) shape = SHAPE_START;
        else if (parsedItem.start === false) shape = SHAPE_END;
        if (parsedItem.type !== DEFAULT_STATE_TYPE) shape = parsedItem.type;
        if (!nodeDb.get(itemId)) nodeDb.set(itemId, {
            id: itemId,
            shape,
            description: (0, _chunkDD37ZF33Mjs.common_default).sanitizeText(itemId, (0, _chunkDD37ZF33Mjs.getConfig2)()),
            cssClasses: `${classStr} ${CSS_DIAGRAM_STATE}`,
            cssStyles: style
        });
        const newNode = nodeDb.get(itemId);
        if (parsedItem.description) {
            if (Array.isArray(newNode.description)) {
                newNode.shape = SHAPE_STATE_WITH_DESC;
                newNode.description.push(parsedItem.description);
            } else if (newNode.description?.length > 0) {
                newNode.shape = SHAPE_STATE_WITH_DESC;
                if (newNode.description === itemId) newNode.description = [
                    parsedItem.description
                ];
                else newNode.description = [
                    newNode.description,
                    parsedItem.description
                ];
            } else {
                newNode.shape = SHAPE_STATE;
                newNode.description = parsedItem.description;
            }
            newNode.description = (0, _chunkDD37ZF33Mjs.common_default).sanitizeTextOrArray(newNode.description, (0, _chunkDD37ZF33Mjs.getConfig2)());
        }
        if (newNode.description?.length === 1 && newNode.shape === SHAPE_STATE_WITH_DESC) {
            if (newNode.type === "group") newNode.shape = SHAPE_GROUP;
            else newNode.shape = SHAPE_STATE;
        }
        if (!newNode.type && parsedItem.doc) {
            (0, _chunkDD37ZF33Mjs.log).info("Setting cluster for XCX", itemId, getDir2(parsedItem));
            newNode.type = "group";
            newNode.isGroup = true;
            newNode.dir = getDir2(parsedItem);
            newNode.shape = parsedItem.type === DIVIDER_TYPE ? SHAPE_DIVIDER : SHAPE_GROUP;
            newNode.cssClasses = `${newNode.cssClasses} ${CSS_DIAGRAM_CLUSTER} ${altFlag ? CSS_DIAGRAM_CLUSTER_ALT : ""}`;
        }
        const nodeData = {
            labelStyle: "",
            shape: newNode.shape,
            label: newNode.description,
            cssClasses: newNode.cssClasses,
            cssCompiledStyles: [],
            cssStyles: newNode.cssStyles,
            id: itemId,
            dir: newNode.dir,
            domId: stateDomId(itemId, graphItemCount),
            type: newNode.type,
            isGroup: newNode.type === "group",
            padding: 8,
            rx: 10,
            ry: 10,
            look
        };
        if (nodeData.shape === SHAPE_DIVIDER) nodeData.label = "";
        if (parent && parent.id !== "root") {
            (0, _chunkDD37ZF33Mjs.log).trace("Setting node ", itemId, " to be child of its parent ", parent.id);
            nodeData.parentId = parent.id;
        }
        nodeData.centerLabel = true;
        if (parsedItem.note) {
            const noteData = {
                labelStyle: "",
                shape: SHAPE_NOTE,
                label: parsedItem.note.text,
                cssClasses: CSS_DIAGRAM_NOTE,
                // useHtmlLabels: false,
                cssStyles: [],
                cssCompilesStyles: [],
                id: itemId + NOTE_ID + "-" + graphItemCount,
                domId: stateDomId(itemId, graphItemCount, NOTE),
                type: newNode.type,
                isGroup: newNode.type === "group",
                padding: (0, _chunkDD37ZF33Mjs.getConfig2)().flowchart.padding,
                look,
                position: parsedItem.note.position
            };
            const parentNodeId = itemId + PARENT_ID;
            const groupData = {
                labelStyle: "",
                shape: SHAPE_NOTEGROUP,
                label: parsedItem.note.text,
                cssClasses: newNode.cssClasses,
                cssStyles: [],
                id: itemId + PARENT_ID,
                domId: stateDomId(itemId, graphItemCount, PARENT),
                type: "group",
                isGroup: true,
                padding: 16,
                //getConfig().flowchart.padding
                look,
                position: parsedItem.note.position
            };
            graphItemCount++;
            groupData.id = parentNodeId;
            noteData.parentId = parentNodeId;
            insertOrUpdateNode(nodes2, groupData, classes2);
            insertOrUpdateNode(nodes2, noteData, classes2);
            insertOrUpdateNode(nodes2, nodeData, classes2);
            let from = itemId;
            let to = noteData.id;
            if (parsedItem.note.position === "left of") {
                from = noteData.id;
                to = itemId;
            }
            edges2.push({
                id: from + "-" + to,
                start: from,
                end: to,
                arrowhead: "none",
                arrowTypeEnd: "",
                style: G_EDGE_STYLE,
                labelStyle: "",
                classes: CSS_EDGE_NOTE_EDGE,
                arrowheadStyle: G_EDGE_ARROWHEADSTYLE,
                labelpos: G_EDGE_LABELPOS,
                labelType: G_EDGE_LABELTYPE,
                thickness: G_EDGE_THICKNESS,
                look
            });
        } else insertOrUpdateNode(nodes2, nodeData, classes2);
    }
    if (parsedItem.doc) {
        (0, _chunkDD37ZF33Mjs.log).trace("Adding nodes children ");
        setupDoc(parsedItem, parsedItem.doc, diagramStates, nodes2, edges2, !altFlag, look, classes2);
    }
}, "dataFetcher");
var reset = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(()=>{
    nodeDb.clear();
    graphItemCount = 0;
}, "reset");
// src/diagrams/state/stateDb.js
var START_NODE = "[*]";
var START_TYPE = "start";
var END_NODE = START_NODE;
var END_TYPE = "end";
var COLOR_KEYWORD = "color";
var FILL_KEYWORD = "fill";
var BG_FILL = "bgFill";
var STYLECLASS_SEP = ",";
function newClassesList() {
    return /* @__PURE__ */ new Map();
}
(0, _chunkDLQEHMXDMjs.__name)(newClassesList, "newClassesList");
var nodes = [];
var edges = [];
var direction = DEFAULT_DIAGRAM_DIRECTION;
var rootDoc = [];
var classes = newClassesList();
var newDoc = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(()=>{
    return {
        /** @type {{ id1: string, id2: string, relationTitle: string }[]} */ relations: [],
        states: /* @__PURE__ */ new Map(),
        documents: {}
    };
}, "newDoc");
var documents = {
    root: newDoc()
};
var currentDocument = documents.root;
var startEndCount = 0;
var dividerCnt = 0;
var lineType = {
    LINE: 0,
    DOTTED_LINE: 1
};
var relationType = {
    AGGREGATION: 0,
    EXTENSION: 1,
    COMPOSITION: 2,
    DEPENDENCY: 3
};
var clone = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((o)=>JSON.parse(JSON.stringify(o)), "clone");
var setRootDoc = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((o)=>{
    (0, _chunkDD37ZF33Mjs.log).info("Setting root doc", o);
    rootDoc = o;
}, "setRootDoc");
var getRootDoc = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(()=>rootDoc, "getRootDoc");
var docTranslator = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((parent, node, first)=>{
    if (node.stmt === STMT_RELATION) {
        docTranslator(parent, node.state1, true);
        docTranslator(parent, node.state2, false);
    } else {
        if (node.stmt === STMT_STATE) {
            if (node.id === "[*]") {
                node.id = first ? parent.id + "_start" : parent.id + "_end";
                node.start = first;
            } else node.id = node.id.trim();
        }
        if (node.doc) {
            const doc = [];
            let currentDoc = [];
            let i;
            for(i = 0; i < node.doc.length; i++)if (node.doc[i].type === DIVIDER_TYPE) {
                const newNode = clone(node.doc[i]);
                newNode.doc = clone(currentDoc);
                doc.push(newNode);
                currentDoc = [];
            } else currentDoc.push(node.doc[i]);
            if (doc.length > 0 && currentDoc.length > 0) {
                const newNode = {
                    stmt: STMT_STATE,
                    id: (0, _chunkI7ZFS43CMjs.generateId)(),
                    type: "divider",
                    doc: clone(currentDoc)
                };
                doc.push(clone(newNode));
                node.doc = doc;
            }
            node.doc.forEach((docNode)=>docTranslator(node, docNode, true));
        }
    }
}, "docTranslator");
var getRootDocV2 = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(()=>{
    docTranslator({
        id: "root"
    }, {
        id: "root",
        doc: rootDoc
    }, true);
    return {
        id: "root",
        doc: rootDoc
    };
}, "getRootDocV2");
var extract = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((_doc)=>{
    let doc;
    if (_doc.doc) doc = _doc.doc;
    else doc = _doc;
    (0, _chunkDD37ZF33Mjs.log).info(doc);
    clear2(true);
    (0, _chunkDD37ZF33Mjs.log).info("Extract initial document:", doc);
    doc.forEach((item)=>{
        (0, _chunkDD37ZF33Mjs.log).warn("Statement", item.stmt);
        switch(item.stmt){
            case STMT_STATE:
                addState(item.id.trim(), item.type, item.doc, item.description, item.note, item.classes, item.styles, item.textStyles);
                break;
            case STMT_RELATION:
                addRelation(item.state1, item.state2, item.description);
                break;
            case STMT_CLASSDEF:
                addStyleClass(item.id.trim(), item.classes);
                break;
            case STMT_STYLEDEF:
                {
                    const ids = item.id.trim().split(",");
                    const styles = item.styleClass.split(",");
                    ids.forEach((id)=>{
                        let foundState = getState(id);
                        if (foundState === void 0) {
                            const trimmedId = id.trim();
                            addState(trimmedId);
                            foundState = getState(trimmedId);
                        }
                        foundState.styles = styles.map((s)=>s.replace(/;/g, "")?.trim());
                    });
                }
                break;
            case STMT_APPLYCLASS:
                setCssClass(item.id.trim(), item.styleClass);
                break;
        }
    });
    const diagramStates = getStates();
    const config = (0, _chunkDD37ZF33Mjs.getConfig2)();
    const look = config.look;
    reset();
    dataFetcher(void 0, getRootDocV2(), diagramStates, nodes, edges, true, look, classes);
    nodes.forEach((node)=>{
        if (Array.isArray(node.label)) {
            node.description = node.label.slice(1);
            if (node.isGroup && node.description.length > 0) throw new Error("Group nodes can only have label. Remove the additional description for node [" + node.id + "]");
            node.label = node.label[0];
        }
    });
}, "extract");
var addState = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(id, type = DEFAULT_STATE_TYPE, doc = null, descr = null, note = null, classes2 = null, styles = null, textStyles = null) {
    const trimmedId = id?.trim();
    if (!currentDocument.states.has(trimmedId)) {
        (0, _chunkDD37ZF33Mjs.log).info("Adding state ", trimmedId, descr);
        currentDocument.states.set(trimmedId, {
            id: trimmedId,
            descriptions: [],
            type,
            doc,
            note,
            classes: [],
            styles: [],
            textStyles: []
        });
    } else {
        if (!currentDocument.states.get(trimmedId).doc) currentDocument.states.get(trimmedId).doc = doc;
        if (!currentDocument.states.get(trimmedId).type) currentDocument.states.get(trimmedId).type = type;
    }
    if (descr) {
        (0, _chunkDD37ZF33Mjs.log).info("Setting state description", trimmedId, descr);
        if (typeof descr === "string") addDescription(trimmedId, descr.trim());
        if (typeof descr === "object") descr.forEach((des)=>addDescription(trimmedId, des.trim()));
    }
    if (note) {
        const doc2 = currentDocument.states.get(trimmedId);
        doc2.note = note;
        doc2.note.text = (0, _chunkDD37ZF33Mjs.common_default).sanitizeText(doc2.note.text, (0, _chunkDD37ZF33Mjs.getConfig2)());
    }
    if (classes2) {
        (0, _chunkDD37ZF33Mjs.log).info("Setting state classes", trimmedId, classes2);
        const classesList = typeof classes2 === "string" ? [
            classes2
        ] : classes2;
        classesList.forEach((cssClass)=>setCssClass(trimmedId, cssClass.trim()));
    }
    if (styles) {
        (0, _chunkDD37ZF33Mjs.log).info("Setting state styles", trimmedId, styles);
        const stylesList = typeof styles === "string" ? [
            styles
        ] : styles;
        stylesList.forEach((style)=>setStyle(trimmedId, style.trim()));
    }
    if (textStyles) {
        (0, _chunkDD37ZF33Mjs.log).info("Setting state styles", trimmedId, styles);
        const textStylesList = typeof textStyles === "string" ? [
            textStyles
        ] : textStyles;
        textStylesList.forEach((textStyle)=>setTextStyle(trimmedId, textStyle.trim()));
    }
}, "addState");
var clear2 = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(saveCommon) {
    nodes = [];
    edges = [];
    documents = {
        root: newDoc()
    };
    currentDocument = documents.root;
    startEndCount = 0;
    classes = newClassesList();
    if (!saveCommon) (0, _chunkDD37ZF33Mjs.clear)();
}, "clear");
var getState = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(id) {
    return currentDocument.states.get(id);
}, "getState");
var getStates = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function() {
    return currentDocument.states;
}, "getStates");
var logDocuments = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function() {
    (0, _chunkDD37ZF33Mjs.log).info("Documents = ", documents);
}, "logDocuments");
var getRelations = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function() {
    return currentDocument.relations;
}, "getRelations");
function startIdIfNeeded(id = "") {
    let fixedId = id;
    if (id === START_NODE) {
        startEndCount++;
        fixedId = `${START_TYPE}${startEndCount}`;
    }
    return fixedId;
}
(0, _chunkDLQEHMXDMjs.__name)(startIdIfNeeded, "startIdIfNeeded");
function startTypeIfNeeded(id = "", type = DEFAULT_STATE_TYPE) {
    return id === START_NODE ? START_TYPE : type;
}
(0, _chunkDLQEHMXDMjs.__name)(startTypeIfNeeded, "startTypeIfNeeded");
function endIdIfNeeded(id = "") {
    let fixedId = id;
    if (id === END_NODE) {
        startEndCount++;
        fixedId = `${END_TYPE}${startEndCount}`;
    }
    return fixedId;
}
(0, _chunkDLQEHMXDMjs.__name)(endIdIfNeeded, "endIdIfNeeded");
function endTypeIfNeeded(id = "", type = DEFAULT_STATE_TYPE) {
    return id === END_NODE ? END_TYPE : type;
}
(0, _chunkDLQEHMXDMjs.__name)(endTypeIfNeeded, "endTypeIfNeeded");
function addRelationObjs(item1, item2, relationTitle) {
    let id1 = startIdIfNeeded(item1.id.trim());
    let type1 = startTypeIfNeeded(item1.id.trim(), item1.type);
    let id2 = startIdIfNeeded(item2.id.trim());
    let type2 = startTypeIfNeeded(item2.id.trim(), item2.type);
    addState(id1, type1, item1.doc, item1.description, item1.note, item1.classes, item1.styles, item1.textStyles);
    addState(id2, type2, item2.doc, item2.description, item2.note, item2.classes, item2.styles, item2.textStyles);
    currentDocument.relations.push({
        id1,
        id2,
        relationTitle: (0, _chunkDD37ZF33Mjs.common_default).sanitizeText(relationTitle, (0, _chunkDD37ZF33Mjs.getConfig2)())
    });
}
(0, _chunkDLQEHMXDMjs.__name)(addRelationObjs, "addRelationObjs");
var addRelation = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(item1, item2, title) {
    if (typeof item1 === "object") addRelationObjs(item1, item2, title);
    else {
        const id1 = startIdIfNeeded(item1.trim());
        const type1 = startTypeIfNeeded(item1);
        const id2 = endIdIfNeeded(item2.trim());
        const type2 = endTypeIfNeeded(item2);
        addState(id1, type1);
        addState(id2, type2);
        currentDocument.relations.push({
            id1,
            id2,
            title: (0, _chunkDD37ZF33Mjs.common_default).sanitizeText(title, (0, _chunkDD37ZF33Mjs.getConfig2)())
        });
    }
}, "addRelation");
var addDescription = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(id, descr) {
    const theState = currentDocument.states.get(id);
    const _descr = descr.startsWith(":") ? descr.replace(":", "").trim() : descr;
    theState.descriptions.push((0, _chunkDD37ZF33Mjs.common_default).sanitizeText(_descr, (0, _chunkDD37ZF33Mjs.getConfig2)()));
}, "addDescription");
var cleanupLabel = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(label) {
    if (label.substring(0, 1) === ":") return label.substr(2).trim();
    else return label.trim();
}, "cleanupLabel");
var getDividerId = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(()=>{
    dividerCnt++;
    return "divider-id-" + dividerCnt;
}, "getDividerId");
var addStyleClass = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(id, styleAttributes = "") {
    if (!classes.has(id)) classes.set(id, {
        id,
        styles: [],
        textStyles: []
    });
    const foundClass = classes.get(id);
    if (styleAttributes !== void 0 && styleAttributes !== null) styleAttributes.split(STYLECLASS_SEP).forEach((attrib)=>{
        const fixedAttrib = attrib.replace(/([^;]*);/, "$1").trim();
        if (RegExp(COLOR_KEYWORD).exec(attrib)) {
            const newStyle1 = fixedAttrib.replace(FILL_KEYWORD, BG_FILL);
            const newStyle2 = newStyle1.replace(COLOR_KEYWORD, FILL_KEYWORD);
            foundClass.textStyles.push(newStyle2);
        }
        foundClass.styles.push(fixedAttrib);
    });
}, "addStyleClass");
var getClasses2 = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function() {
    return classes;
}, "getClasses");
var setCssClass = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(itemIds, cssClassName) {
    itemIds.split(",").forEach(function(id) {
        let foundState = getState(id);
        if (foundState === void 0) {
            const trimmedId = id.trim();
            addState(trimmedId);
            foundState = getState(trimmedId);
        }
        foundState.classes.push(cssClassName);
    });
}, "setCssClass");
var setStyle = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(itemId, styleText) {
    const item = getState(itemId);
    if (item !== void 0) item.styles.push(styleText);
}, "setStyle");
var setTextStyle = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(itemId, cssClassName) {
    const item = getState(itemId);
    if (item !== void 0) item.textStyles.push(cssClassName);
}, "setTextStyle");
var getDirection = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(()=>direction, "getDirection");
var setDirection = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((dir)=>{
    direction = dir;
}, "setDirection");
var trimColon = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((str)=>str && str[0] === ":" ? str.substr(1).trim() : str.trim(), "trimColon");
var getData = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(()=>{
    const config = (0, _chunkDD37ZF33Mjs.getConfig2)();
    return {
        nodes,
        edges,
        other: {},
        config,
        direction: getDir(getRootDocV2())
    };
}, "getData");
var stateDb_default = {
    getConfig: /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(()=>(0, _chunkDD37ZF33Mjs.getConfig2)().state, "getConfig"),
    getData,
    addState,
    clear: clear2,
    getState,
    getStates,
    getRelations,
    getClasses: getClasses2,
    getDirection,
    addRelation,
    getDividerId,
    setDirection,
    cleanupLabel,
    lineType,
    relationType,
    logDocuments,
    getRootDoc,
    setRootDoc,
    getRootDocV2,
    extract,
    trimColon,
    getAccTitle: (0, _chunkDD37ZF33Mjs.getAccTitle),
    setAccTitle: (0, _chunkDD37ZF33Mjs.setAccTitle),
    getAccDescription: (0, _chunkDD37ZF33Mjs.getAccDescription),
    setAccDescription: (0, _chunkDD37ZF33Mjs.setAccDescription),
    addStyleClass,
    setCssClass,
    addDescription,
    setDiagramTitle: (0, _chunkDD37ZF33Mjs.setDiagramTitle),
    getDiagramTitle: (0, _chunkDD37ZF33Mjs.getDiagramTitle)
};
// src/diagrams/state/styles.js
var getStyles = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((options)=>`
defs #statediagram-barbEnd {
    fill: ${options.transitionColor};
    stroke: ${options.transitionColor};
  }
g.stateGroup text {
  fill: ${options.nodeBorder};
  stroke: none;
  font-size: 10px;
}
g.stateGroup text {
  fill: ${options.textColor};
  stroke: none;
  font-size: 10px;

}
g.stateGroup .state-title {
  font-weight: bolder;
  fill: ${options.stateLabelColor};
}

g.stateGroup rect {
  fill: ${options.mainBkg};
  stroke: ${options.nodeBorder};
}

g.stateGroup line {
  stroke: ${options.lineColor};
  stroke-width: 1;
}

.transition {
  stroke: ${options.transitionColor};
  stroke-width: 1;
  fill: none;
}

.stateGroup .composit {
  fill: ${options.background};
  border-bottom: 1px
}

.stateGroup .alt-composit {
  fill: #e0e0e0;
  border-bottom: 1px
}

.state-note {
  stroke: ${options.noteBorderColor};
  fill: ${options.noteBkgColor};

  text {
    fill: ${options.noteTextColor};
    stroke: none;
    font-size: 10px;
  }
}

.stateLabel .box {
  stroke: none;
  stroke-width: 0;
  fill: ${options.mainBkg};
  opacity: 0.5;
}

.edgeLabel .label rect {
  fill: ${options.labelBackgroundColor};
  opacity: 0.5;
}
.edgeLabel {
  background-color: ${options.edgeLabelBackground};
  p {
    background-color: ${options.edgeLabelBackground};
  }
  rect {
    opacity: 0.5;
    background-color: ${options.edgeLabelBackground};
    fill: ${options.edgeLabelBackground};
  }
  text-align: center;
}
.edgeLabel .label text {
  fill: ${options.transitionLabelColor || options.tertiaryTextColor};
}
.label div .edgeLabel {
  color: ${options.transitionLabelColor || options.tertiaryTextColor};
}

.stateLabel text {
  fill: ${options.stateLabelColor};
  font-size: 10px;
  font-weight: bold;
}

.node circle.state-start {
  fill: ${options.specialStateColor};
  stroke: ${options.specialStateColor};
}

.node .fork-join {
  fill: ${options.specialStateColor};
  stroke: ${options.specialStateColor};
}

.node circle.state-end {
  fill: ${options.innerEndBackground};
  stroke: ${options.background};
  stroke-width: 1.5
}
.end-state-inner {
  fill: ${options.compositeBackground || options.background};
  // stroke: ${options.background};
  stroke-width: 1.5
}

.node rect {
  fill: ${options.stateBkg || options.mainBkg};
  stroke: ${options.stateBorder || options.nodeBorder};
  stroke-width: 1px;
}
.node polygon {
  fill: ${options.mainBkg};
  stroke: ${options.stateBorder || options.nodeBorder};;
  stroke-width: 1px;
}
#statediagram-barbEnd {
  fill: ${options.lineColor};
}

.statediagram-cluster rect {
  fill: ${options.compositeTitleBackground};
  stroke: ${options.stateBorder || options.nodeBorder};
  stroke-width: 1px;
}

.cluster-label, .nodeLabel {
  color: ${options.stateLabelColor};
  // line-height: 1;
}

.statediagram-cluster rect.outer {
  rx: 5px;
  ry: 5px;
}
.statediagram-state .divider {
  stroke: ${options.stateBorder || options.nodeBorder};
}

.statediagram-state .title-state {
  rx: 5px;
  ry: 5px;
}
.statediagram-cluster.statediagram-cluster .inner {
  fill: ${options.compositeBackground || options.background};
}
.statediagram-cluster.statediagram-cluster-alt .inner {
  fill: ${options.altBackground ? options.altBackground : "#efefef"};
}

.statediagram-cluster .inner {
  rx:0;
  ry:0;
}

.statediagram-state rect.basic {
  rx: 5px;
  ry: 5px;
}
.statediagram-state rect.divider {
  stroke-dasharray: 10,10;
  fill: ${options.altBackground ? options.altBackground : "#efefef"};
}

.note-edge {
  stroke-dasharray: 5;
}

.statediagram-note rect {
  fill: ${options.noteBkgColor};
  stroke: ${options.noteBorderColor};
  stroke-width: 1px;
  rx: 0;
  ry: 0;
}
.statediagram-note rect {
  fill: ${options.noteBkgColor};
  stroke: ${options.noteBorderColor};
  stroke-width: 1px;
  rx: 0;
  ry: 0;
}

.statediagram-note text {
  fill: ${options.noteTextColor};
}

.statediagram-note .nodeLabel {
  color: ${options.noteTextColor};
}
.statediagram .edgeLabel {
  color: red; // ${options.noteTextColor};
}

#dependencyStart, #dependencyEnd {
  fill: ${options.lineColor};
  stroke: ${options.lineColor};
  stroke-width: 1;
}

.statediagramTitleText {
  text-anchor: middle;
  font-size: 18px;
  fill: ${options.textColor};
}
`, "getStyles");
var styles_default = getStyles;

},{"./chunk-GVMN75T7.mjs":"eQN3e","./chunk-M7N4Q5GZ.mjs":"haj7Y","./chunk-I7ZFS43C.mjs":"huUtc","./chunk-DD37ZF33.mjs":"f4pI5","./chunk-DLQEHMXD.mjs":"h5YRf","@parcel/transformer-js/src/esmodule-helpers.js":"1cdyk"}]},["hVlQA"], null, "parcelRequire6955", {})

//# sourceMappingURL=stateDiagram-ERABNXIO.1d9c91e6.js.map
