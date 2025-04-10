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
})({"9sOqI":[function(require,module,exports,__globalThis) {
var global = arguments[3];
var HMR_HOST = null;
var HMR_PORT = 1234;
var HMR_SERVER_PORT = 1234;
var HMR_SECURE = false;
var HMR_ENV_HASH = "febfef3c5b467b16";
var HMR_USE_SSE = false;
module.bundle.HMR_BUNDLE_ID = "d47220abcbf852cf";
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

},{}],"cOFjs":[function(require,module,exports,__globalThis) {
var parcelHelpers = require("@parcel/transformer-js/src/esmodule-helpers.js");
parcelHelpers.defineInteropFlag(exports);
parcelHelpers.export(exports, "diagram", ()=>diagram);
var _chunkDD37ZF33Mjs = require("./chunk-DD37ZF33.mjs");
var _chunkDLQEHMXDMjs = require("./chunk-DLQEHMXD.mjs");
// src/diagrams/timeline/parser/timeline.jison
var parser = function() {
    var o = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(k, v, o2, l) {
        for(o2 = o2 || {}, l = k.length; l--; o2[k[l]] = v);
        return o2;
    }, "o"), $V0 = [
        6,
        8,
        10,
        11,
        12,
        14,
        16,
        17,
        20,
        21
    ], $V1 = [
        1,
        9
    ], $V2 = [
        1,
        10
    ], $V3 = [
        1,
        11
    ], $V4 = [
        1,
        12
    ], $V5 = [
        1,
        13
    ], $V6 = [
        1,
        16
    ], $V7 = [
        1,
        17
    ];
    var parser2 = {
        trace: /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function trace() {}, "trace"),
        yy: {},
        symbols_: {
            "error": 2,
            "start": 3,
            "timeline": 4,
            "document": 5,
            "EOF": 6,
            "line": 7,
            "SPACE": 8,
            "statement": 9,
            "NEWLINE": 10,
            "title": 11,
            "acc_title": 12,
            "acc_title_value": 13,
            "acc_descr": 14,
            "acc_descr_value": 15,
            "acc_descr_multiline_value": 16,
            "section": 17,
            "period_statement": 18,
            "event_statement": 19,
            "period": 20,
            "event": 21,
            "$accept": 0,
            "$end": 1
        },
        terminals_: {
            2: "error",
            4: "timeline",
            6: "EOF",
            8: "SPACE",
            10: "NEWLINE",
            11: "title",
            12: "acc_title",
            13: "acc_title_value",
            14: "acc_descr",
            15: "acc_descr_value",
            16: "acc_descr_multiline_value",
            17: "section",
            20: "period",
            21: "event"
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
                18,
                1
            ],
            [
                19,
                1
            ]
        ],
        performAction: /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function anonymous(yytext, yyleng, yylineno, yy, yystate, $$, _$) {
            var $0 = $$.length - 1;
            switch(yystate){
                case 1:
                    return $$[$0 - 1];
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
                    yy.getCommonDb().setDiagramTitle($$[$0].substr(6));
                    this.$ = $$[$0].substr(6);
                    break;
                case 9:
                    this.$ = $$[$0].trim();
                    yy.getCommonDb().setAccTitle(this.$);
                    break;
                case 10:
                case 11:
                    this.$ = $$[$0].trim();
                    yy.getCommonDb().setAccDescription(this.$);
                    break;
                case 12:
                    yy.addSection($$[$0].substr(8));
                    this.$ = $$[$0].substr(8);
                    break;
                case 15:
                    yy.addTask($$[$0], 0, "");
                    this.$ = $$[$0];
                    break;
                case 16:
                    yy.addEvent($$[$0].substr(2));
                    this.$ = $$[$0];
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
                11: $V1,
                12: $V2,
                14: $V3,
                16: $V4,
                17: $V5,
                18: 14,
                19: 15,
                20: $V6,
                21: $V7
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
                9: 18,
                11: $V1,
                12: $V2,
                14: $V3,
                16: $V4,
                17: $V5,
                18: 14,
                19: 15,
                20: $V6,
                21: $V7
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
                8
            ]),
            {
                13: [
                    1,
                    19
                ]
            },
            {
                15: [
                    1,
                    20
                ]
            },
            o($V0, [
                2,
                11
            ]),
            o($V0, [
                2,
                12
            ]),
            o($V0, [
                2,
                13
            ]),
            o($V0, [
                2,
                14
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
                4
            ]),
            o($V0, [
                2,
                9
            ]),
            o($V0, [
                2,
                10
            ])
        ],
        defaultActions: {},
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
                        return 10;
                    case 3:
                        break;
                    case 4:
                        break;
                    case 5:
                        return 4;
                    case 6:
                        return 11;
                    case 7:
                        this.begin("acc_title");
                        return 12;
                    case 8:
                        this.popState();
                        return "acc_title_value";
                    case 9:
                        this.begin("acc_descr");
                        return 14;
                    case 10:
                        this.popState();
                        return "acc_descr_value";
                    case 11:
                        this.begin("acc_descr_multiline");
                        break;
                    case 12:
                        this.popState();
                        break;
                    case 13:
                        return "acc_descr_multiline_value";
                    case 14:
                        return 17;
                    case 15:
                        return 21;
                    case 16:
                        return 20;
                    case 17:
                        return 6;
                    case 18:
                        return "INVALID";
                }
            }, "anonymous"),
            rules: [
                /^(?:%(?!\{)[^\n]*)/i,
                /^(?:[^\}]%%[^\n]*)/i,
                /^(?:[\n]+)/i,
                /^(?:\s+)/i,
                /^(?:#[^\n]*)/i,
                /^(?:timeline\b)/i,
                /^(?:title\s[^\n]+)/i,
                /^(?:accTitle\s*:\s*)/i,
                /^(?:(?!\n||)*[^\n]*)/i,
                /^(?:accDescr\s*:\s*)/i,
                /^(?:(?!\n||)*[^\n]*)/i,
                /^(?:accDescr\s*\{\s*)/i,
                /^(?:[\}])/i,
                /^(?:[^\}]*)/i,
                /^(?:section\s[^:\n]+)/i,
                /^(?::\s[^:\n]+)/i,
                /^(?:[^#:\n]+)/i,
                /^(?:$)/i,
                /^(?:.)/i
            ],
            conditions: {
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
                        9,
                        11,
                        14,
                        15,
                        16,
                        17,
                        18
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
var timeline_default = parser;
// src/diagrams/timeline/timelineDb.js
var timelineDb_exports = {};
(0, _chunkDLQEHMXDMjs.__export)(timelineDb_exports, {
    addEvent: ()=>addEvent,
    addSection: ()=>addSection,
    addTask: ()=>addTask,
    addTaskOrg: ()=>addTaskOrg,
    clear: ()=>clear2,
    default: ()=>timelineDb_default,
    getCommonDb: ()=>getCommonDb,
    getSections: ()=>getSections,
    getTasks: ()=>getTasks
});
var currentSection = "";
var currentTaskId = 0;
var sections = [];
var tasks = [];
var rawTasks = [];
var getCommonDb = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(()=>(0, _chunkDD37ZF33Mjs.commonDb_exports), "getCommonDb");
var clear2 = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function() {
    sections.length = 0;
    tasks.length = 0;
    currentSection = "";
    rawTasks.length = 0;
    (0, _chunkDD37ZF33Mjs.clear)();
}, "clear");
var addSection = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(txt) {
    currentSection = txt;
    sections.push(txt);
}, "addSection");
var getSections = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function() {
    return sections;
}, "getSections");
var getTasks = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function() {
    let allItemsProcessed = compileTasks();
    const maxDepth = 100;
    let iterationCount = 0;
    while(!allItemsProcessed && iterationCount < maxDepth){
        allItemsProcessed = compileTasks();
        iterationCount++;
    }
    tasks.push(...rawTasks);
    return tasks;
}, "getTasks");
var addTask = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(period, length, event) {
    const rawTask = {
        id: currentTaskId++,
        section: currentSection,
        type: currentSection,
        task: period,
        score: length ? length : 0,
        //if event is defined, then add it the events array
        events: event ? [
            event
        ] : []
    };
    rawTasks.push(rawTask);
}, "addTask");
var addEvent = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(event) {
    const currentTask = rawTasks.find((task)=>task.id === currentTaskId - 1);
    currentTask.events.push(event);
}, "addEvent");
var addTaskOrg = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(descr) {
    const newTask = {
        section: currentSection,
        type: currentSection,
        description: descr,
        task: descr,
        classes: []
    };
    tasks.push(newTask);
}, "addTaskOrg");
var compileTasks = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function() {
    const compileTask = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(pos) {
        return rawTasks[pos].processed;
    }, "compileTask");
    let allProcessed = true;
    for (const [i, rawTask] of rawTasks.entries()){
        compileTask(i);
        allProcessed = allProcessed && rawTask.processed;
    }
    return allProcessed;
}, "compileTasks");
var timelineDb_default = {
    clear: clear2,
    getCommonDb,
    addSection,
    getSections,
    getTasks,
    addTask,
    addTaskOrg,
    addEvent
};
// src/diagrams/timeline/svgDraw.js
var MAX_SECTIONS = 12;
var drawRect = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(elem, rectData) {
    const rectElem = elem.append("rect");
    rectElem.attr("x", rectData.x);
    rectElem.attr("y", rectData.y);
    rectElem.attr("fill", rectData.fill);
    rectElem.attr("stroke", rectData.stroke);
    rectElem.attr("width", rectData.width);
    rectElem.attr("height", rectData.height);
    rectElem.attr("rx", rectData.rx);
    rectElem.attr("ry", rectData.ry);
    if (rectData.class !== void 0) rectElem.attr("class", rectData.class);
    return rectElem;
}, "drawRect");
var drawFace = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(element, faceData) {
    const radius = 15;
    const circleElement = element.append("circle").attr("cx", faceData.cx).attr("cy", faceData.cy).attr("class", "face").attr("r", radius).attr("stroke-width", 2).attr("overflow", "visible");
    const face = element.append("g");
    face.append("circle").attr("cx", faceData.cx - radius / 3).attr("cy", faceData.cy - radius / 3).attr("r", 1.5).attr("stroke-width", 2).attr("fill", "#666").attr("stroke", "#666");
    face.append("circle").attr("cx", faceData.cx + radius / 3).attr("cy", faceData.cy - radius / 3).attr("r", 1.5).attr("stroke-width", 2).attr("fill", "#666").attr("stroke", "#666");
    function smile(face2) {
        const arc = (0, _chunkDD37ZF33Mjs.arc_default)().startAngle(Math.PI / 2).endAngle(3 * (Math.PI / 2)).innerRadius(radius / 2).outerRadius(radius / 2.2);
        face2.append("path").attr("class", "mouth").attr("d", arc).attr("transform", "translate(" + faceData.cx + "," + (faceData.cy + 2) + ")");
    }
    (0, _chunkDLQEHMXDMjs.__name)(smile, "smile");
    function sad(face2) {
        const arc = (0, _chunkDD37ZF33Mjs.arc_default)().startAngle(3 * Math.PI / 2).endAngle(5 * (Math.PI / 2)).innerRadius(radius / 2).outerRadius(radius / 2.2);
        face2.append("path").attr("class", "mouth").attr("d", arc).attr("transform", "translate(" + faceData.cx + "," + (faceData.cy + 7) + ")");
    }
    (0, _chunkDLQEHMXDMjs.__name)(sad, "sad");
    function ambivalent(face2) {
        face2.append("line").attr("class", "mouth").attr("stroke", 2).attr("x1", faceData.cx - 5).attr("y1", faceData.cy + 7).attr("x2", faceData.cx + 5).attr("y2", faceData.cy + 7).attr("class", "mouth").attr("stroke-width", "1px").attr("stroke", "#666");
    }
    (0, _chunkDLQEHMXDMjs.__name)(ambivalent, "ambivalent");
    if (faceData.score > 3) smile(face);
    else if (faceData.score < 3) sad(face);
    else ambivalent(face);
    return circleElement;
}, "drawFace");
var drawCircle = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(element, circleData) {
    const circleElement = element.append("circle");
    circleElement.attr("cx", circleData.cx);
    circleElement.attr("cy", circleData.cy);
    circleElement.attr("class", "actor-" + circleData.pos);
    circleElement.attr("fill", circleData.fill);
    circleElement.attr("stroke", circleData.stroke);
    circleElement.attr("r", circleData.r);
    if (circleElement.class !== void 0) circleElement.attr("class", circleElement.class);
    if (circleData.title !== void 0) circleElement.append("title").text(circleData.title);
    return circleElement;
}, "drawCircle");
var drawText = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(elem, textData) {
    const nText = textData.text.replace(/<br\s*\/?>/gi, " ");
    const textElem = elem.append("text");
    textElem.attr("x", textData.x);
    textElem.attr("y", textData.y);
    textElem.attr("class", "legend");
    textElem.style("text-anchor", textData.anchor);
    if (textData.class !== void 0) textElem.attr("class", textData.class);
    const span = textElem.append("tspan");
    span.attr("x", textData.x + textData.textMargin * 2);
    span.text(nText);
    return textElem;
}, "drawText");
var drawLabel = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(elem, txtObject) {
    function genPoints(x, y, width, height, cut) {
        return x + "," + y + " " + (x + width) + "," + y + " " + (x + width) + "," + (y + height - cut) + " " + (x + width - cut * 1.2) + "," + (y + height) + " " + x + "," + (y + height);
    }
    (0, _chunkDLQEHMXDMjs.__name)(genPoints, "genPoints");
    const polygon = elem.append("polygon");
    polygon.attr("points", genPoints(txtObject.x, txtObject.y, 50, 20, 7));
    polygon.attr("class", "labelBox");
    txtObject.y = txtObject.y + txtObject.labelMargin;
    txtObject.x = txtObject.x + 0.5 * txtObject.labelMargin;
    drawText(elem, txtObject);
}, "drawLabel");
var drawSection = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(elem, section, conf) {
    const g = elem.append("g");
    const rect = getNoteRect();
    rect.x = section.x;
    rect.y = section.y;
    rect.fill = section.fill;
    rect.width = conf.width;
    rect.height = conf.height;
    rect.class = "journey-section section-type-" + section.num;
    rect.rx = 3;
    rect.ry = 3;
    drawRect(g, rect);
    _drawTextCandidateFunc(conf)(section.text, g, rect.x, rect.y, rect.width, rect.height, {
        class: "journey-section section-type-" + section.num
    }, conf, section.colour);
}, "drawSection");
var taskCount = -1;
var drawTask = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(elem, task, conf) {
    const center = task.x + conf.width / 2;
    const g = elem.append("g");
    taskCount++;
    const maxHeight = 450;
    g.append("line").attr("id", "task" + taskCount).attr("x1", center).attr("y1", task.y).attr("x2", center).attr("y2", maxHeight).attr("class", "task-line").attr("stroke-width", "1px").attr("stroke-dasharray", "4 2").attr("stroke", "#666");
    drawFace(g, {
        cx: center,
        cy: 300 + (5 - task.score) * 30,
        score: task.score
    });
    const rect = getNoteRect();
    rect.x = task.x;
    rect.y = task.y;
    rect.fill = task.fill;
    rect.width = conf.width;
    rect.height = conf.height;
    rect.class = "task task-type-" + task.num;
    rect.rx = 3;
    rect.ry = 3;
    drawRect(g, rect);
    _drawTextCandidateFunc(conf)(task.task, g, rect.x, rect.y, rect.width, rect.height, {
        class: "task"
    }, conf, task.colour);
}, "drawTask");
var drawBackgroundRect = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(elem, bounds) {
    const rectElem = drawRect(elem, {
        x: bounds.startx,
        y: bounds.starty,
        width: bounds.stopx - bounds.startx,
        height: bounds.stopy - bounds.starty,
        fill: bounds.fill,
        class: "rect"
    });
    rectElem.lower();
}, "drawBackgroundRect");
var getTextObj = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function() {
    return {
        x: 0,
        y: 0,
        fill: void 0,
        "text-anchor": "start",
        width: 100,
        height: 100,
        textMargin: 0,
        rx: 0,
        ry: 0
    };
}, "getTextObj");
var getNoteRect = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function() {
    return {
        x: 0,
        y: 0,
        width: 100,
        anchor: "start",
        height: 100,
        rx: 0,
        ry: 0
    };
}, "getNoteRect");
var _drawTextCandidateFunc = /* @__PURE__ */ function() {
    function byText(content, g, x, y, width, height, textAttrs, colour) {
        const text = g.append("text").attr("x", x + width / 2).attr("y", y + height / 2 + 5).style("font-color", colour).style("text-anchor", "middle").text(content);
        _setTextAttrs(text, textAttrs);
    }
    (0, _chunkDLQEHMXDMjs.__name)(byText, "byText");
    function byTspan(content, g, x, y, width, height, textAttrs, conf, colour) {
        const { taskFontSize, taskFontFamily } = conf;
        const lines = content.split(/<br\s*\/?>/gi);
        for(let i = 0; i < lines.length; i++){
            const dy = i * taskFontSize - taskFontSize * (lines.length - 1) / 2;
            const text = g.append("text").attr("x", x + width / 2).attr("y", y).attr("fill", colour).style("text-anchor", "middle").style("font-size", taskFontSize).style("font-family", taskFontFamily);
            text.append("tspan").attr("x", x + width / 2).attr("dy", dy).text(lines[i]);
            text.attr("y", y + height / 2).attr("dominant-baseline", "central").attr("alignment-baseline", "central");
            _setTextAttrs(text, textAttrs);
        }
    }
    (0, _chunkDLQEHMXDMjs.__name)(byTspan, "byTspan");
    function byFo(content, g, x, y, width, height, textAttrs, conf) {
        const body = g.append("switch");
        const f = body.append("foreignObject").attr("x", x).attr("y", y).attr("width", width).attr("height", height).attr("position", "fixed");
        const text = f.append("xhtml:div").style("display", "table").style("height", "100%").style("width", "100%");
        text.append("div").attr("class", "label").style("display", "table-cell").style("text-align", "center").style("vertical-align", "middle").text(content);
        byTspan(content, body, x, y, width, height, textAttrs, conf);
        _setTextAttrs(text, textAttrs);
    }
    (0, _chunkDLQEHMXDMjs.__name)(byFo, "byFo");
    function _setTextAttrs(toText, fromTextAttrsDict) {
        for(const key in fromTextAttrsDict)if (key in fromTextAttrsDict) toText.attr(key, fromTextAttrsDict[key]);
    }
    (0, _chunkDLQEHMXDMjs.__name)(_setTextAttrs, "_setTextAttrs");
    return function(conf) {
        return conf.textPlacement === "fo" ? byFo : conf.textPlacement === "old" ? byText : byTspan;
    };
}();
var initGraphics = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(graphics) {
    graphics.append("defs").append("marker").attr("id", "arrowhead").attr("refX", 5).attr("refY", 2).attr("markerWidth", 6).attr("markerHeight", 4).attr("orient", "auto").append("path").attr("d", "M 0,0 V 4 L6,2 Z");
}, "initGraphics");
function wrap(text, width) {
    text.each(function() {
        var text2 = (0, _chunkDD37ZF33Mjs.select_default)(this), words = text2.text().split(/(\s+|<br>)/).reverse(), word, line = [], lineHeight = 1.1, y = text2.attr("y"), dy = parseFloat(text2.attr("dy")), tspan = text2.text(null).append("tspan").attr("x", 0).attr("y", y).attr("dy", dy + "em");
        for(let j = 0; j < words.length; j++){
            word = words[words.length - 1 - j];
            line.push(word);
            tspan.text(line.join(" ").trim());
            if (tspan.node().getComputedTextLength() > width || word === "<br>") {
                line.pop();
                tspan.text(line.join(" ").trim());
                if (word === "<br>") line = [
                    ""
                ];
                else line = [
                    word
                ];
                tspan = text2.append("tspan").attr("x", 0).attr("y", y).attr("dy", lineHeight + "em").text(word);
            }
        }
    });
}
(0, _chunkDLQEHMXDMjs.__name)(wrap, "wrap");
var drawNode = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(elem, node, fullSection, conf) {
    const section = fullSection % MAX_SECTIONS - 1;
    const nodeElem = elem.append("g");
    node.section = section;
    nodeElem.attr("class", (node.class ? node.class + " " : "") + "timeline-node " + ("section-" + section));
    const bkgElem = nodeElem.append("g");
    const textElem = nodeElem.append("g");
    const txt = textElem.append("text").text(node.descr).attr("dy", "1em").attr("alignment-baseline", "middle").attr("dominant-baseline", "middle").attr("text-anchor", "middle").call(wrap, node.width);
    const bbox = txt.node().getBBox();
    const fontSize = conf.fontSize?.replace ? conf.fontSize.replace("px", "") : conf.fontSize;
    node.height = bbox.height + fontSize * 0.55 + node.padding;
    node.height = Math.max(node.height, node.maxHeight);
    node.width = node.width + 2 * node.padding;
    textElem.attr("transform", "translate(" + node.width / 2 + ", " + node.padding / 2 + ")");
    defaultBkg(bkgElem, node, section, conf);
    return node;
}, "drawNode");
var getVirtualNodeHeight = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(elem, node, conf) {
    const textElem = elem.append("g");
    const txt = textElem.append("text").text(node.descr).attr("dy", "1em").attr("alignment-baseline", "middle").attr("dominant-baseline", "middle").attr("text-anchor", "middle").call(wrap, node.width);
    const bbox = txt.node().getBBox();
    const fontSize = conf.fontSize?.replace ? conf.fontSize.replace("px", "") : conf.fontSize;
    textElem.remove();
    return bbox.height + fontSize * 0.55 + node.padding;
}, "getVirtualNodeHeight");
var defaultBkg = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(elem, node, section) {
    const rd = 5;
    elem.append("path").attr("id", "node-" + node.id).attr("class", "node-bkg node-" + node.type).attr("d", `M0 ${node.height - rd} v${-node.height + 2 * rd} q0,-5 5,-5 h${node.width - 2 * rd} q5,0 5,5 v${node.height - rd} H0 Z`);
    elem.append("line").attr("class", "node-line-" + section).attr("x1", 0).attr("y1", node.height).attr("x2", node.width).attr("y2", node.height);
}, "defaultBkg");
var svgDraw_default = {
    drawRect,
    drawCircle,
    drawSection,
    drawText,
    drawLabel,
    drawTask,
    drawBackgroundRect,
    getTextObj,
    getNoteRect,
    initGraphics,
    drawNode,
    getVirtualNodeHeight
};
// src/diagrams/timeline/timelineRenderer.ts
var draw = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(text, id, version, diagObj) {
    const conf = (0, _chunkDD37ZF33Mjs.getConfig2)();
    const LEFT_MARGIN = conf.leftMargin ?? 50;
    (0, _chunkDD37ZF33Mjs.log).debug("timeline", diagObj.db);
    const securityLevel = conf.securityLevel;
    let sandboxElement;
    if (securityLevel === "sandbox") sandboxElement = (0, _chunkDD37ZF33Mjs.select_default)("#i" + id);
    const root = securityLevel === "sandbox" ? (0, _chunkDD37ZF33Mjs.select_default)(sandboxElement.nodes()[0].contentDocument.body) : (0, _chunkDD37ZF33Mjs.select_default)("body");
    const svg = root.select("#" + id);
    svg.append("g");
    const tasks2 = diagObj.db.getTasks();
    const title = diagObj.db.getCommonDb().getDiagramTitle();
    (0, _chunkDD37ZF33Mjs.log).debug("task", tasks2);
    svgDraw_default.initGraphics(svg);
    const sections2 = diagObj.db.getSections();
    (0, _chunkDD37ZF33Mjs.log).debug("sections", sections2);
    let maxSectionHeight = 0;
    let maxTaskHeight = 0;
    let depthY = 0;
    let sectionBeginY = 0;
    let masterX = 50 + LEFT_MARGIN;
    let masterY = 50;
    sectionBeginY = 50;
    let sectionNumber = 0;
    let hasSections = true;
    sections2.forEach(function(section) {
        const sectionNode = {
            number: sectionNumber,
            descr: section,
            section: sectionNumber,
            width: 150,
            padding: 20,
            maxHeight: maxSectionHeight
        };
        const sectionHeight = svgDraw_default.getVirtualNodeHeight(svg, sectionNode, conf);
        (0, _chunkDD37ZF33Mjs.log).debug("sectionHeight before draw", sectionHeight);
        maxSectionHeight = Math.max(maxSectionHeight, sectionHeight + 20);
    });
    let maxEventCount = 0;
    let maxEventLineLength = 0;
    (0, _chunkDD37ZF33Mjs.log).debug("tasks.length", tasks2.length);
    for (const [i, task] of tasks2.entries()){
        const taskNode = {
            number: i,
            descr: task,
            section: task.section,
            width: 150,
            padding: 20,
            maxHeight: maxTaskHeight
        };
        const taskHeight = svgDraw_default.getVirtualNodeHeight(svg, taskNode, conf);
        (0, _chunkDD37ZF33Mjs.log).debug("taskHeight before draw", taskHeight);
        maxTaskHeight = Math.max(maxTaskHeight, taskHeight + 20);
        maxEventCount = Math.max(maxEventCount, task.events.length);
        let maxEventLineLengthTemp = 0;
        for (const event of task.events){
            const eventNode = {
                descr: event,
                section: task.section,
                number: task.section,
                width: 150,
                padding: 20,
                maxHeight: 50
            };
            maxEventLineLengthTemp += svgDraw_default.getVirtualNodeHeight(svg, eventNode, conf);
        }
        maxEventLineLength = Math.max(maxEventLineLength, maxEventLineLengthTemp);
    }
    (0, _chunkDD37ZF33Mjs.log).debug("maxSectionHeight before draw", maxSectionHeight);
    (0, _chunkDD37ZF33Mjs.log).debug("maxTaskHeight before draw", maxTaskHeight);
    if (sections2 && sections2.length > 0) sections2.forEach((section)=>{
        const tasksForSection = tasks2.filter((task)=>task.section === section);
        const sectionNode = {
            number: sectionNumber,
            descr: section,
            section: sectionNumber,
            width: 200 * Math.max(tasksForSection.length, 1) - 50,
            padding: 20,
            maxHeight: maxSectionHeight
        };
        (0, _chunkDD37ZF33Mjs.log).debug("sectionNode", sectionNode);
        const sectionNodeWrapper = svg.append("g");
        const node = svgDraw_default.drawNode(sectionNodeWrapper, sectionNode, sectionNumber, conf);
        (0, _chunkDD37ZF33Mjs.log).debug("sectionNode output", node);
        sectionNodeWrapper.attr("transform", `translate(${masterX}, ${sectionBeginY})`);
        masterY += maxSectionHeight + 50;
        if (tasksForSection.length > 0) drawTasks(svg, tasksForSection, sectionNumber, masterX, masterY, maxTaskHeight, conf, maxEventCount, maxEventLineLength, maxSectionHeight, false);
        masterX += 200 * Math.max(tasksForSection.length, 1);
        masterY = sectionBeginY;
        sectionNumber++;
    });
    else {
        hasSections = false;
        drawTasks(svg, tasks2, sectionNumber, masterX, masterY, maxTaskHeight, conf, maxEventCount, maxEventLineLength, maxSectionHeight, true);
    }
    const box = svg.node().getBBox();
    (0, _chunkDD37ZF33Mjs.log).debug("bounds", box);
    if (title) svg.append("text").text(title).attr("x", box.width / 2 - LEFT_MARGIN).attr("font-size", "4ex").attr("font-weight", "bold").attr("y", 20);
    depthY = hasSections ? maxSectionHeight + maxTaskHeight + 150 : maxTaskHeight + 100;
    const lineWrapper = svg.append("g").attr("class", "lineWrapper");
    lineWrapper.append("line").attr("x1", LEFT_MARGIN).attr("y1", depthY).attr("x2", box.width + 3 * LEFT_MARGIN).attr("y2", depthY).attr("stroke-width", 4).attr("stroke", "black").attr("marker-end", "url(#arrowhead)");
    (0, _chunkDD37ZF33Mjs.setupGraphViewbox)(void 0, svg, conf.timeline?.padding ?? 50, conf.timeline?.useMaxWidth ?? false);
}, "draw");
var drawTasks = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(diagram2, tasks2, sectionColor, masterX, masterY, maxTaskHeight, conf, maxEventCount, maxEventLineLength, maxSectionHeight, isWithoutSections) {
    for (const task of tasks2){
        const taskNode = {
            descr: task.task,
            section: sectionColor,
            number: sectionColor,
            width: 150,
            padding: 20,
            maxHeight: maxTaskHeight
        };
        (0, _chunkDD37ZF33Mjs.log).debug("taskNode", taskNode);
        const taskWrapper = diagram2.append("g").attr("class", "taskWrapper");
        const node = svgDraw_default.drawNode(taskWrapper, taskNode, sectionColor, conf);
        const taskHeight = node.height;
        (0, _chunkDD37ZF33Mjs.log).debug("taskHeight after draw", taskHeight);
        taskWrapper.attr("transform", `translate(${masterX}, ${masterY})`);
        maxTaskHeight = Math.max(maxTaskHeight, taskHeight);
        if (task.events) {
            const lineWrapper = diagram2.append("g").attr("class", "lineWrapper");
            let lineLength = maxTaskHeight;
            masterY += 100;
            lineLength = lineLength + drawEvents(diagram2, task.events, sectionColor, masterX, masterY, conf);
            masterY -= 100;
            lineWrapper.append("line").attr("x1", masterX + 95).attr("y1", masterY + maxTaskHeight).attr("x2", masterX + 95).attr("y2", masterY + maxTaskHeight + (isWithoutSections ? maxTaskHeight : maxSectionHeight) + maxEventLineLength + 120).attr("stroke-width", 2).attr("stroke", "black").attr("marker-end", "url(#arrowhead)").attr("stroke-dasharray", "5,5");
        }
        masterX = masterX + 200;
        if (isWithoutSections && !conf.timeline?.disableMulticolor) sectionColor++;
    }
    masterY = masterY - 10;
}, "drawTasks");
var drawEvents = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(diagram2, events, sectionColor, masterX, masterY, conf) {
    let maxEventHeight = 0;
    const eventBeginY = masterY;
    masterY = masterY + 100;
    for (const event of events){
        const eventNode = {
            descr: event,
            section: sectionColor,
            number: sectionColor,
            width: 150,
            padding: 20,
            maxHeight: 50
        };
        (0, _chunkDD37ZF33Mjs.log).debug("eventNode", eventNode);
        const eventWrapper = diagram2.append("g").attr("class", "eventWrapper");
        const node = svgDraw_default.drawNode(eventWrapper, eventNode, sectionColor, conf);
        const eventHeight = node.height;
        maxEventHeight = maxEventHeight + eventHeight;
        eventWrapper.attr("transform", `translate(${masterX}, ${masterY})`);
        masterY = masterY + 10 + eventHeight;
    }
    masterY = eventBeginY;
    return maxEventHeight;
}, "drawEvents");
var timelineRenderer_default = {
    setConf: /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(()=>{}, "setConf"),
    draw
};
// src/diagrams/timeline/styles.js
var genSections = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((options)=>{
    let sections2 = "";
    for(let i = 0; i < options.THEME_COLOR_LIMIT; i++){
        options["lineColor" + i] = options["lineColor" + i] || options["cScaleInv" + i];
        if ((0, _chunkDD37ZF33Mjs.is_dark_default)(options["lineColor" + i])) options["lineColor" + i] = (0, _chunkDD37ZF33Mjs.lighten_default)(options["lineColor" + i], 20);
        else options["lineColor" + i] = (0, _chunkDD37ZF33Mjs.darken_default)(options["lineColor" + i], 20);
    }
    for(let i = 0; i < options.THEME_COLOR_LIMIT; i++){
        const sw = "" + (17 - 3 * i);
        sections2 += `
    .section-${i - 1} rect, .section-${i - 1} path, .section-${i - 1} circle, .section-${i - 1} path  {
      fill: ${options["cScale" + i]};
    }
    .section-${i - 1} text {
     fill: ${options["cScaleLabel" + i]};
    }
    .node-icon-${i - 1} {
      font-size: 40px;
      color: ${options["cScaleLabel" + i]};
    }
    .section-edge-${i - 1}{
      stroke: ${options["cScale" + i]};
    }
    .edge-depth-${i - 1}{
      stroke-width: ${sw};
    }
    .section-${i - 1} line {
      stroke: ${options["cScaleInv" + i]} ;
      stroke-width: 3;
    }

    .lineWrapper line{
      stroke: ${options["cScaleLabel" + i]} ;
    }

    .disabled, .disabled circle, .disabled text {
      fill: lightgray;
    }
    .disabled text {
      fill: #efefef;
    }
    `;
    }
    return sections2;
}, "genSections");
var getStyles = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((options)=>`
  .edge {
    stroke-width: 3;
  }
  ${genSections(options)}
  .section-root rect, .section-root path, .section-root circle  {
    fill: ${options.git0};
  }
  .section-root text {
    fill: ${options.gitBranchLabel0};
  }
  .icon-container {
    height:100%;
    display: flex;
    justify-content: center;
    align-items: center;
  }
  .edge {
    fill: none;
  }
  .eventWrapper  {
   filter: brightness(120%);
  }
`, "getStyles");
var styles_default = getStyles;
// src/diagrams/timeline/timeline-definition.ts
var diagram = {
    db: timelineDb_exports,
    renderer: timelineRenderer_default,
    parser: timeline_default,
    styles: styles_default
};

},{"./chunk-DD37ZF33.mjs":"f4pI5","./chunk-DLQEHMXD.mjs":"h5YRf","@parcel/transformer-js/src/esmodule-helpers.js":"1cdyk"}]},["9sOqI"], null, "parcelRequire6955", {})

//# sourceMappingURL=timeline-definition-3FLJNPJ3.cbf852cf.js.map
