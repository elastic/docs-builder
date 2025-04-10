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
})({"deyeW":[function(require,module,exports,__globalThis) {
var global = arguments[3];
var HMR_HOST = null;
var HMR_PORT = 1234;
var HMR_SERVER_PORT = 1234;
var HMR_SECURE = false;
var HMR_ENV_HASH = "febfef3c5b467b16";
var HMR_USE_SSE = false;
module.bundle.HMR_BUNDLE_ID = "76c58420d91f5d92";
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

},{}],"js8iU":[function(require,module,exports,__globalThis) {
var parcelHelpers = require("@parcel/transformer-js/src/esmodule-helpers.js");
parcelHelpers.defineInteropFlag(exports);
parcelHelpers.export(exports, "diagram", ()=>diagram);
var _chunkDD37ZF33Mjs = require("./chunk-DD37ZF33.mjs");
var _chunkDLQEHMXDMjs = require("./chunk-DLQEHMXD.mjs");
// src/diagrams/sankey/parser/sankey.jison
var parser = function() {
    var o = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(k, v, o2, l) {
        for(o2 = o2 || {}, l = k.length; l--; o2[k[l]] = v);
        return o2;
    }, "o"), $V0 = [
        1,
        9
    ], $V1 = [
        1,
        10
    ], $V2 = [
        1,
        5,
        10,
        12
    ];
    var parser2 = {
        trace: /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function trace() {}, "trace"),
        yy: {},
        symbols_: {
            "error": 2,
            "start": 3,
            "SANKEY": 4,
            "NEWLINE": 5,
            "csv": 6,
            "opt_eof": 7,
            "record": 8,
            "csv_tail": 9,
            "EOF": 10,
            "field[source]": 11,
            "COMMA": 12,
            "field[target]": 13,
            "field[value]": 14,
            "field": 15,
            "escaped": 16,
            "non_escaped": 17,
            "DQUOTE": 18,
            "ESCAPED_TEXT": 19,
            "NON_ESCAPED_TEXT": 20,
            "$accept": 0,
            "$end": 1
        },
        terminals_: {
            2: "error",
            4: "SANKEY",
            5: "NEWLINE",
            10: "EOF",
            11: "field[source]",
            12: "COMMA",
            13: "field[target]",
            14: "field[value]",
            18: "DQUOTE",
            19: "ESCAPED_TEXT",
            20: "NON_ESCAPED_TEXT"
        },
        productions_: [
            0,
            [
                3,
                4
            ],
            [
                6,
                2
            ],
            [
                9,
                2
            ],
            [
                9,
                0
            ],
            [
                7,
                1
            ],
            [
                7,
                0
            ],
            [
                8,
                5
            ],
            [
                15,
                1
            ],
            [
                15,
                1
            ],
            [
                16,
                3
            ],
            [
                17,
                1
            ]
        ],
        performAction: /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function anonymous(yytext, yyleng, yylineno, yy, yystate, $$, _$) {
            var $0 = $$.length - 1;
            switch(yystate){
                case 7:
                    const source = yy.findOrCreateNode($$[$0 - 4].trim().replaceAll('""', '"'));
                    const target = yy.findOrCreateNode($$[$0 - 2].trim().replaceAll('""', '"'));
                    const value2 = parseFloat($$[$0].trim());
                    yy.addLink(source, target, value2);
                    break;
                case 8:
                case 9:
                case 11:
                    this.$ = $$[$0];
                    break;
                case 10:
                    this.$ = $$[$0 - 1];
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
            {
                5: [
                    1,
                    3
                ]
            },
            {
                6: 4,
                8: 5,
                15: 6,
                16: 7,
                17: 8,
                18: $V0,
                20: $V1
            },
            {
                1: [
                    2,
                    6
                ],
                7: 11,
                10: [
                    1,
                    12
                ]
            },
            o($V1, [
                2,
                4
            ], {
                9: 13,
                5: [
                    1,
                    14
                ]
            }),
            {
                12: [
                    1,
                    15
                ]
            },
            o($V2, [
                2,
                8
            ]),
            o($V2, [
                2,
                9
            ]),
            {
                19: [
                    1,
                    16
                ]
            },
            o($V2, [
                2,
                11
            ]),
            {
                1: [
                    2,
                    1
                ]
            },
            {
                1: [
                    2,
                    5
                ]
            },
            o($V1, [
                2,
                2
            ]),
            {
                6: 17,
                8: 5,
                15: 6,
                16: 7,
                17: 8,
                18: $V0,
                20: $V1
            },
            {
                15: 18,
                16: 7,
                17: 8,
                18: $V0,
                20: $V1
            },
            {
                18: [
                    1,
                    19
                ]
            },
            o($V1, [
                2,
                3
            ]),
            {
                12: [
                    1,
                    20
                ]
            },
            o($V2, [
                2,
                10
            ]),
            {
                15: 21,
                16: 7,
                17: 8,
                18: $V0,
                20: $V1
            },
            o([
                1,
                5,
                10
            ], [
                2,
                7
            ])
        ],
        defaultActions: {
            11: [
                2,
                1
            ],
            12: [
                2,
                5
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
                        this.pushState("csv");
                        return 4;
                    case 1:
                        return 10;
                    case 2:
                        return 5;
                    case 3:
                        return 12;
                    case 4:
                        this.pushState("escaped_text");
                        return 18;
                    case 5:
                        return 20;
                    case 6:
                        this.popState("escaped_text");
                        return 18;
                    case 7:
                        return 19;
                }
            }, "anonymous"),
            rules: [
                /^(?:sankey-beta\b)/i,
                /^(?:$)/i,
                /^(?:((\u000D\u000A)|(\u000A)))/i,
                /^(?:(\u002C))/i,
                /^(?:(\u0022))/i,
                /^(?:([\u0020-\u0021\u0023-\u002B\u002D-\u007E])*)/i,
                /^(?:(\u0022)(?!(\u0022)))/i,
                /^(?:(([\u0020-\u0021\u0023-\u002B\u002D-\u007E])|(\u002C)|(\u000D)|(\u000A)|(\u0022)(\u0022))*)/i
            ],
            conditions: {
                "csv": {
                    "rules": [
                        1,
                        2,
                        3,
                        4,
                        5,
                        6,
                        7
                    ],
                    "inclusive": false
                },
                "escaped_text": {
                    "rules": [
                        6,
                        7
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
                        7
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
var sankey_default = parser;
// src/diagrams/sankey/sankeyDB.ts
var links = [];
var nodes = [];
var nodesMap = /* @__PURE__ */ new Map();
var clear2 = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(()=>{
    links = [];
    nodes = [];
    nodesMap = /* @__PURE__ */ new Map();
    (0, _chunkDD37ZF33Mjs.clear)();
}, "clear");
var SankeyLink = class {
    constructor(source, target, value2 = 0){
        this.source = source;
        this.target = target;
        this.value = value2;
    }
    static #_ = (0, _chunkDLQEHMXDMjs.__name)(this, "SankeyLink");
};
var addLink = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((source, target, value2)=>{
    links.push(new SankeyLink(source, target, value2));
}, "addLink");
var SankeyNode = class {
    constructor(ID){
        this.ID = ID;
    }
    static #_ = (0, _chunkDLQEHMXDMjs.__name)(this, "SankeyNode");
};
var findOrCreateNode = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((ID)=>{
    ID = (0, _chunkDD37ZF33Mjs.common_default).sanitizeText(ID, (0, _chunkDD37ZF33Mjs.getConfig2)());
    let node = nodesMap.get(ID);
    if (node === void 0) {
        node = new SankeyNode(ID);
        nodesMap.set(ID, node);
        nodes.push(node);
    }
    return node;
}, "findOrCreateNode");
var getNodes = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(()=>nodes, "getNodes");
var getLinks = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(()=>links, "getLinks");
var getGraph = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(()=>({
        nodes: nodes.map((node)=>({
                id: node.ID
            })),
        links: links.map((link2)=>({
                source: link2.source.ID,
                target: link2.target.ID,
                value: link2.value
            }))
    }), "getGraph");
var sankeyDB_default = {
    nodesMap,
    getConfig: /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(()=>(0, _chunkDD37ZF33Mjs.getConfig2)().sankey, "getConfig"),
    getNodes,
    getLinks,
    getGraph,
    addLink,
    findOrCreateNode,
    getAccTitle: (0, _chunkDD37ZF33Mjs.getAccTitle),
    setAccTitle: (0, _chunkDD37ZF33Mjs.setAccTitle),
    getAccDescription: (0, _chunkDD37ZF33Mjs.getAccDescription),
    setAccDescription: (0, _chunkDD37ZF33Mjs.setAccDescription),
    getDiagramTitle: (0, _chunkDD37ZF33Mjs.getDiagramTitle),
    setDiagramTitle: (0, _chunkDD37ZF33Mjs.setDiagramTitle),
    clear: clear2
};
// ../../node_modules/.pnpm/d3-array@2.12.1/node_modules/d3-array/src/max.js
function max(values, valueof) {
    let max2;
    if (valueof === void 0) {
        for (const value2 of values)if (value2 != null && (max2 < value2 || max2 === void 0 && value2 >= value2)) max2 = value2;
    } else {
        let index = -1;
        for (let value2 of values)if ((value2 = valueof(value2, ++index, values)) != null && (max2 < value2 || max2 === void 0 && value2 >= value2)) max2 = value2;
    }
    return max2;
}
(0, _chunkDLQEHMXDMjs.__name)(max, "max");
// ../../node_modules/.pnpm/d3-array@2.12.1/node_modules/d3-array/src/min.js
function min(values, valueof) {
    let min2;
    if (valueof === void 0) {
        for (const value2 of values)if (value2 != null && (min2 > value2 || min2 === void 0 && value2 >= value2)) min2 = value2;
    } else {
        let index = -1;
        for (let value2 of values)if ((value2 = valueof(value2, ++index, values)) != null && (min2 > value2 || min2 === void 0 && value2 >= value2)) min2 = value2;
    }
    return min2;
}
(0, _chunkDLQEHMXDMjs.__name)(min, "min");
// ../../node_modules/.pnpm/d3-array@2.12.1/node_modules/d3-array/src/sum.js
function sum(values, valueof) {
    let sum2 = 0;
    if (valueof === void 0) {
        for (let value2 of values)if (value2 = +value2) sum2 += value2;
    } else {
        let index = -1;
        for (let value2 of values)if (value2 = +valueof(value2, ++index, values)) sum2 += value2;
    }
    return sum2;
}
(0, _chunkDLQEHMXDMjs.__name)(sum, "sum");
// ../../node_modules/.pnpm/d3-sankey@0.12.3/node_modules/d3-sankey/src/align.js
function targetDepth(d) {
    return d.target.depth;
}
(0, _chunkDLQEHMXDMjs.__name)(targetDepth, "targetDepth");
function left(node) {
    return node.depth;
}
(0, _chunkDLQEHMXDMjs.__name)(left, "left");
function right(node, n) {
    return n - 1 - node.height;
}
(0, _chunkDLQEHMXDMjs.__name)(right, "right");
function justify(node, n) {
    return node.sourceLinks.length ? node.depth : n - 1;
}
(0, _chunkDLQEHMXDMjs.__name)(justify, "justify");
function center(node) {
    return node.targetLinks.length ? node.depth : node.sourceLinks.length ? min(node.sourceLinks, targetDepth) - 1 : 0;
}
(0, _chunkDLQEHMXDMjs.__name)(center, "center");
// ../../node_modules/.pnpm/d3-sankey@0.12.3/node_modules/d3-sankey/src/constant.js
function constant(x2) {
    return function() {
        return x2;
    };
}
(0, _chunkDLQEHMXDMjs.__name)(constant, "constant");
// ../../node_modules/.pnpm/d3-sankey@0.12.3/node_modules/d3-sankey/src/sankey.js
function ascendingSourceBreadth(a, b) {
    return ascendingBreadth(a.source, b.source) || a.index - b.index;
}
(0, _chunkDLQEHMXDMjs.__name)(ascendingSourceBreadth, "ascendingSourceBreadth");
function ascendingTargetBreadth(a, b) {
    return ascendingBreadth(a.target, b.target) || a.index - b.index;
}
(0, _chunkDLQEHMXDMjs.__name)(ascendingTargetBreadth, "ascendingTargetBreadth");
function ascendingBreadth(a, b) {
    return a.y0 - b.y0;
}
(0, _chunkDLQEHMXDMjs.__name)(ascendingBreadth, "ascendingBreadth");
function value(d) {
    return d.value;
}
(0, _chunkDLQEHMXDMjs.__name)(value, "value");
function defaultId(d) {
    return d.index;
}
(0, _chunkDLQEHMXDMjs.__name)(defaultId, "defaultId");
function defaultNodes(graph) {
    return graph.nodes;
}
(0, _chunkDLQEHMXDMjs.__name)(defaultNodes, "defaultNodes");
function defaultLinks(graph) {
    return graph.links;
}
(0, _chunkDLQEHMXDMjs.__name)(defaultLinks, "defaultLinks");
function find(nodeById, id) {
    const node = nodeById.get(id);
    if (!node) throw new Error("missing: " + id);
    return node;
}
(0, _chunkDLQEHMXDMjs.__name)(find, "find");
function computeLinkBreadths({ nodes: nodes2 }) {
    for (const node of nodes2){
        let y0 = node.y0;
        let y1 = y0;
        for (const link2 of node.sourceLinks){
            link2.y0 = y0 + link2.width / 2;
            y0 += link2.width;
        }
        for (const link2 of node.targetLinks){
            link2.y1 = y1 + link2.width / 2;
            y1 += link2.width;
        }
    }
}
(0, _chunkDLQEHMXDMjs.__name)(computeLinkBreadths, "computeLinkBreadths");
function Sankey() {
    let x0 = 0, y0 = 0, x1 = 1, y1 = 1;
    let dx = 24;
    let dy = 8, py;
    let id = defaultId;
    let align = justify;
    let sort;
    let linkSort;
    let nodes2 = defaultNodes;
    let links2 = defaultLinks;
    let iterations = 6;
    function sankey() {
        const graph = {
            nodes: nodes2.apply(null, arguments),
            links: links2.apply(null, arguments)
        };
        computeNodeLinks(graph);
        computeNodeValues(graph);
        computeNodeDepths(graph);
        computeNodeHeights(graph);
        computeNodeBreadths(graph);
        computeLinkBreadths(graph);
        return graph;
    }
    (0, _chunkDLQEHMXDMjs.__name)(sankey, "sankey");
    sankey.update = function(graph) {
        computeLinkBreadths(graph);
        return graph;
    };
    sankey.nodeId = function(_) {
        return arguments.length ? (id = typeof _ === "function" ? _ : constant(_), sankey) : id;
    };
    sankey.nodeAlign = function(_) {
        return arguments.length ? (align = typeof _ === "function" ? _ : constant(_), sankey) : align;
    };
    sankey.nodeSort = function(_) {
        return arguments.length ? (sort = _, sankey) : sort;
    };
    sankey.nodeWidth = function(_) {
        return arguments.length ? (dx = +_, sankey) : dx;
    };
    sankey.nodePadding = function(_) {
        return arguments.length ? (dy = py = +_, sankey) : dy;
    };
    sankey.nodes = function(_) {
        return arguments.length ? (nodes2 = typeof _ === "function" ? _ : constant(_), sankey) : nodes2;
    };
    sankey.links = function(_) {
        return arguments.length ? (links2 = typeof _ === "function" ? _ : constant(_), sankey) : links2;
    };
    sankey.linkSort = function(_) {
        return arguments.length ? (linkSort = _, sankey) : linkSort;
    };
    sankey.size = function(_) {
        return arguments.length ? (x0 = y0 = 0, x1 = +_[0], y1 = +_[1], sankey) : [
            x1 - x0,
            y1 - y0
        ];
    };
    sankey.extent = function(_) {
        return arguments.length ? (x0 = +_[0][0], x1 = +_[1][0], y0 = +_[0][1], y1 = +_[1][1], sankey) : [
            [
                x0,
                y0
            ],
            [
                x1,
                y1
            ]
        ];
    };
    sankey.iterations = function(_) {
        return arguments.length ? (iterations = +_, sankey) : iterations;
    };
    function computeNodeLinks({ nodes: nodes3, links: links3 }) {
        for (const [i, node] of nodes3.entries()){
            node.index = i;
            node.sourceLinks = [];
            node.targetLinks = [];
        }
        const nodeById = new Map(nodes3.map((d, i)=>[
                id(d, i, nodes3),
                d
            ]));
        for (const [i, link2] of links3.entries()){
            link2.index = i;
            let { source, target } = link2;
            if (typeof source !== "object") source = link2.source = find(nodeById, source);
            if (typeof target !== "object") target = link2.target = find(nodeById, target);
            source.sourceLinks.push(link2);
            target.targetLinks.push(link2);
        }
        if (linkSort != null) for (const { sourceLinks, targetLinks } of nodes3){
            sourceLinks.sort(linkSort);
            targetLinks.sort(linkSort);
        }
    }
    (0, _chunkDLQEHMXDMjs.__name)(computeNodeLinks, "computeNodeLinks");
    function computeNodeValues({ nodes: nodes3 }) {
        for (const node of nodes3)node.value = node.fixedValue === void 0 ? Math.max(sum(node.sourceLinks, value), sum(node.targetLinks, value)) : node.fixedValue;
    }
    (0, _chunkDLQEHMXDMjs.__name)(computeNodeValues, "computeNodeValues");
    function computeNodeDepths({ nodes: nodes3 }) {
        const n = nodes3.length;
        let current = new Set(nodes3);
        let next = /* @__PURE__ */ new Set();
        let x2 = 0;
        while(current.size){
            for (const node of current){
                node.depth = x2;
                for (const { target } of node.sourceLinks)next.add(target);
            }
            if (++x2 > n) throw new Error("circular link");
            current = next;
            next = /* @__PURE__ */ new Set();
        }
    }
    (0, _chunkDLQEHMXDMjs.__name)(computeNodeDepths, "computeNodeDepths");
    function computeNodeHeights({ nodes: nodes3 }) {
        const n = nodes3.length;
        let current = new Set(nodes3);
        let next = /* @__PURE__ */ new Set();
        let x2 = 0;
        while(current.size){
            for (const node of current){
                node.height = x2;
                for (const { source } of node.targetLinks)next.add(source);
            }
            if (++x2 > n) throw new Error("circular link");
            current = next;
            next = /* @__PURE__ */ new Set();
        }
    }
    (0, _chunkDLQEHMXDMjs.__name)(computeNodeHeights, "computeNodeHeights");
    function computeNodeLayers({ nodes: nodes3 }) {
        const x2 = max(nodes3, (d)=>d.depth) + 1;
        const kx = (x1 - x0 - dx) / (x2 - 1);
        const columns = new Array(x2);
        for (const node of nodes3){
            const i = Math.max(0, Math.min(x2 - 1, Math.floor(align.call(null, node, x2))));
            node.layer = i;
            node.x0 = x0 + i * kx;
            node.x1 = node.x0 + dx;
            if (columns[i]) columns[i].push(node);
            else columns[i] = [
                node
            ];
        }
        if (sort) for (const column of columns)column.sort(sort);
        return columns;
    }
    (0, _chunkDLQEHMXDMjs.__name)(computeNodeLayers, "computeNodeLayers");
    function initializeNodeBreadths(columns) {
        const ky = min(columns, (c)=>(y1 - y0 - (c.length - 1) * py) / sum(c, value));
        for (const nodes3 of columns){
            let y2 = y0;
            for (const node of nodes3){
                node.y0 = y2;
                node.y1 = y2 + node.value * ky;
                y2 = node.y1 + py;
                for (const link2 of node.sourceLinks)link2.width = link2.value * ky;
            }
            y2 = (y1 - y2 + py) / (nodes3.length + 1);
            for(let i = 0; i < nodes3.length; ++i){
                const node = nodes3[i];
                node.y0 += y2 * (i + 1);
                node.y1 += y2 * (i + 1);
            }
            reorderLinks(nodes3);
        }
    }
    (0, _chunkDLQEHMXDMjs.__name)(initializeNodeBreadths, "initializeNodeBreadths");
    function computeNodeBreadths(graph) {
        const columns = computeNodeLayers(graph);
        py = Math.min(dy, (y1 - y0) / (max(columns, (c)=>c.length) - 1));
        initializeNodeBreadths(columns);
        for(let i = 0; i < iterations; ++i){
            const alpha = Math.pow(0.99, i);
            const beta = Math.max(1 - alpha, (i + 1) / iterations);
            relaxRightToLeft(columns, alpha, beta);
            relaxLeftToRight(columns, alpha, beta);
        }
    }
    (0, _chunkDLQEHMXDMjs.__name)(computeNodeBreadths, "computeNodeBreadths");
    function relaxLeftToRight(columns, alpha, beta) {
        for(let i = 1, n = columns.length; i < n; ++i){
            const column = columns[i];
            for (const target of column){
                let y2 = 0;
                let w = 0;
                for (const { source, value: value2 } of target.targetLinks){
                    let v = value2 * (target.layer - source.layer);
                    y2 += targetTop(source, target) * v;
                    w += v;
                }
                if (!(w > 0)) continue;
                let dy2 = (y2 / w - target.y0) * alpha;
                target.y0 += dy2;
                target.y1 += dy2;
                reorderNodeLinks(target);
            }
            if (sort === void 0) column.sort(ascendingBreadth);
            resolveCollisions(column, beta);
        }
    }
    (0, _chunkDLQEHMXDMjs.__name)(relaxLeftToRight, "relaxLeftToRight");
    function relaxRightToLeft(columns, alpha, beta) {
        for(let n = columns.length, i = n - 2; i >= 0; --i){
            const column = columns[i];
            for (const source of column){
                let y2 = 0;
                let w = 0;
                for (const { target, value: value2 } of source.sourceLinks){
                    let v = value2 * (target.layer - source.layer);
                    y2 += sourceTop(source, target) * v;
                    w += v;
                }
                if (!(w > 0)) continue;
                let dy2 = (y2 / w - source.y0) * alpha;
                source.y0 += dy2;
                source.y1 += dy2;
                reorderNodeLinks(source);
            }
            if (sort === void 0) column.sort(ascendingBreadth);
            resolveCollisions(column, beta);
        }
    }
    (0, _chunkDLQEHMXDMjs.__name)(relaxRightToLeft, "relaxRightToLeft");
    function resolveCollisions(nodes3, alpha) {
        const i = nodes3.length >> 1;
        const subject = nodes3[i];
        resolveCollisionsBottomToTop(nodes3, subject.y0 - py, i - 1, alpha);
        resolveCollisionsTopToBottom(nodes3, subject.y1 + py, i + 1, alpha);
        resolveCollisionsBottomToTop(nodes3, y1, nodes3.length - 1, alpha);
        resolveCollisionsTopToBottom(nodes3, y0, 0, alpha);
    }
    (0, _chunkDLQEHMXDMjs.__name)(resolveCollisions, "resolveCollisions");
    function resolveCollisionsTopToBottom(nodes3, y2, i, alpha) {
        for(; i < nodes3.length; ++i){
            const node = nodes3[i];
            const dy2 = (y2 - node.y0) * alpha;
            if (dy2 > 1e-6) node.y0 += dy2, node.y1 += dy2;
            y2 = node.y1 + py;
        }
    }
    (0, _chunkDLQEHMXDMjs.__name)(resolveCollisionsTopToBottom, "resolveCollisionsTopToBottom");
    function resolveCollisionsBottomToTop(nodes3, y2, i, alpha) {
        for(; i >= 0; --i){
            const node = nodes3[i];
            const dy2 = (node.y1 - y2) * alpha;
            if (dy2 > 1e-6) node.y0 -= dy2, node.y1 -= dy2;
            y2 = node.y0 - py;
        }
    }
    (0, _chunkDLQEHMXDMjs.__name)(resolveCollisionsBottomToTop, "resolveCollisionsBottomToTop");
    function reorderNodeLinks({ sourceLinks, targetLinks }) {
        if (linkSort === void 0) {
            for (const { source: { sourceLinks: sourceLinks2 } } of targetLinks)sourceLinks2.sort(ascendingTargetBreadth);
            for (const { target: { targetLinks: targetLinks2 } } of sourceLinks)targetLinks2.sort(ascendingSourceBreadth);
        }
    }
    (0, _chunkDLQEHMXDMjs.__name)(reorderNodeLinks, "reorderNodeLinks");
    function reorderLinks(nodes3) {
        if (linkSort === void 0) for (const { sourceLinks, targetLinks } of nodes3){
            sourceLinks.sort(ascendingTargetBreadth);
            targetLinks.sort(ascendingSourceBreadth);
        }
    }
    (0, _chunkDLQEHMXDMjs.__name)(reorderLinks, "reorderLinks");
    function targetTop(source, target) {
        let y2 = source.y0 - (source.sourceLinks.length - 1) * py / 2;
        for (const { target: node, width } of source.sourceLinks){
            if (node === target) break;
            y2 += width + py;
        }
        for (const { source: node, width } of target.targetLinks){
            if (node === source) break;
            y2 -= width;
        }
        return y2;
    }
    (0, _chunkDLQEHMXDMjs.__name)(targetTop, "targetTop");
    function sourceTop(source, target) {
        let y2 = target.y0 - (target.targetLinks.length - 1) * py / 2;
        for (const { source: node, width } of target.targetLinks){
            if (node === source) break;
            y2 += width + py;
        }
        for (const { target: node, width } of source.sourceLinks){
            if (node === target) break;
            y2 -= width;
        }
        return y2;
    }
    (0, _chunkDLQEHMXDMjs.__name)(sourceTop, "sourceTop");
    return sankey;
}
(0, _chunkDLQEHMXDMjs.__name)(Sankey, "Sankey");
// ../../node_modules/.pnpm/d3-path@1.0.9/node_modules/d3-path/src/path.js
var pi = Math.PI;
var tau = 2 * pi;
var epsilon = 1e-6;
var tauEpsilon = tau - epsilon;
function Path() {
    this._x0 = this._y0 = this._x1 = this._y1 = null;
    this._ = "";
}
(0, _chunkDLQEHMXDMjs.__name)(Path, "Path");
function path() {
    return new Path();
}
(0, _chunkDLQEHMXDMjs.__name)(path, "path");
Path.prototype = path.prototype = {
    constructor: Path,
    moveTo: /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(x2, y2) {
        this._ += "M" + (this._x0 = this._x1 = +x2) + "," + (this._y0 = this._y1 = +y2);
    }, "moveTo"),
    closePath: /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function() {
        if (this._x1 !== null) {
            this._x1 = this._x0, this._y1 = this._y0;
            this._ += "Z";
        }
    }, "closePath"),
    lineTo: /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(x2, y2) {
        this._ += "L" + (this._x1 = +x2) + "," + (this._y1 = +y2);
    }, "lineTo"),
    quadraticCurveTo: /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(x1, y1, x2, y2) {
        this._ += "Q" + +x1 + "," + +y1 + "," + (this._x1 = +x2) + "," + (this._y1 = +y2);
    }, "quadraticCurveTo"),
    bezierCurveTo: /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(x1, y1, x2, y2, x3, y3) {
        this._ += "C" + +x1 + "," + +y1 + "," + +x2 + "," + +y2 + "," + (this._x1 = +x3) + "," + (this._y1 = +y3);
    }, "bezierCurveTo"),
    arcTo: /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(x1, y1, x2, y2, r) {
        x1 = +x1, y1 = +y1, x2 = +x2, y2 = +y2, r = +r;
        var x0 = this._x1, y0 = this._y1, x21 = x2 - x1, y21 = y2 - y1, x01 = x0 - x1, y01 = y0 - y1, l01_2 = x01 * x01 + y01 * y01;
        if (r < 0) throw new Error("negative radius: " + r);
        if (this._x1 === null) this._ += "M" + (this._x1 = x1) + "," + (this._y1 = y1);
        else if (!(l01_2 > epsilon)) ;
        else if (!(Math.abs(y01 * x21 - y21 * x01) > epsilon) || !r) this._ += "L" + (this._x1 = x1) + "," + (this._y1 = y1);
        else {
            var x20 = x2 - x0, y20 = y2 - y0, l21_2 = x21 * x21 + y21 * y21, l20_2 = x20 * x20 + y20 * y20, l21 = Math.sqrt(l21_2), l01 = Math.sqrt(l01_2), l = r * Math.tan((pi - Math.acos((l21_2 + l01_2 - l20_2) / (2 * l21 * l01))) / 2), t01 = l / l01, t21 = l / l21;
            if (Math.abs(t01 - 1) > epsilon) this._ += "L" + (x1 + t01 * x01) + "," + (y1 + t01 * y01);
            this._ += "A" + r + "," + r + ",0,0," + +(y01 * x20 > x01 * y20) + "," + (this._x1 = x1 + t21 * x21) + "," + (this._y1 = y1 + t21 * y21);
        }
    }, "arcTo"),
    arc: /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(x2, y2, r, a0, a1, ccw) {
        x2 = +x2, y2 = +y2, r = +r, ccw = !!ccw;
        var dx = r * Math.cos(a0), dy = r * Math.sin(a0), x0 = x2 + dx, y0 = y2 + dy, cw = 1 ^ ccw, da = ccw ? a0 - a1 : a1 - a0;
        if (r < 0) throw new Error("negative radius: " + r);
        if (this._x1 === null) this._ += "M" + x0 + "," + y0;
        else if (Math.abs(this._x1 - x0) > epsilon || Math.abs(this._y1 - y0) > epsilon) this._ += "L" + x0 + "," + y0;
        if (!r) return;
        if (da < 0) da = da % tau + tau;
        if (da > tauEpsilon) this._ += "A" + r + "," + r + ",0,1," + cw + "," + (x2 - dx) + "," + (y2 - dy) + "A" + r + "," + r + ",0,1," + cw + "," + (this._x1 = x0) + "," + (this._y1 = y0);
        else if (da > epsilon) this._ += "A" + r + "," + r + ",0," + +(da >= pi) + "," + cw + "," + (this._x1 = x2 + r * Math.cos(a1)) + "," + (this._y1 = y2 + r * Math.sin(a1));
    }, "arc"),
    rect: /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(x2, y2, w, h) {
        this._ += "M" + (this._x0 = this._x1 = +x2) + "," + (this._y0 = this._y1 = +y2) + "h" + +w + "v" + +h + "h" + -w + "Z";
    }, "rect"),
    toString: /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function() {
        return this._;
    }, "toString")
};
var path_default = path;
// ../../node_modules/.pnpm/d3-shape@1.3.7/node_modules/d3-shape/src/constant.js
function constant_default(x2) {
    return /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function constant2() {
        return x2;
    }, "constant");
}
(0, _chunkDLQEHMXDMjs.__name)(constant_default, "default");
// ../../node_modules/.pnpm/d3-shape@1.3.7/node_modules/d3-shape/src/point.js
function x(p) {
    return p[0];
}
(0, _chunkDLQEHMXDMjs.__name)(x, "x");
function y(p) {
    return p[1];
}
(0, _chunkDLQEHMXDMjs.__name)(y, "y");
// ../../node_modules/.pnpm/d3-shape@1.3.7/node_modules/d3-shape/src/array.js
var slice = Array.prototype.slice;
// ../../node_modules/.pnpm/d3-shape@1.3.7/node_modules/d3-shape/src/link/index.js
function linkSource(d) {
    return d.source;
}
(0, _chunkDLQEHMXDMjs.__name)(linkSource, "linkSource");
function linkTarget(d) {
    return d.target;
}
(0, _chunkDLQEHMXDMjs.__name)(linkTarget, "linkTarget");
function link(curve) {
    var source = linkSource, target = linkTarget, x2 = x, y2 = y, context = null;
    function link2() {
        var buffer, argv = slice.call(arguments), s = source.apply(this, argv), t = target.apply(this, argv);
        if (!context) context = buffer = path_default();
        curve(context, +x2.apply(this, (argv[0] = s, argv)), +y2.apply(this, argv), +x2.apply(this, (argv[0] = t, argv)), +y2.apply(this, argv));
        if (buffer) return context = null, buffer + "" || null;
    }
    (0, _chunkDLQEHMXDMjs.__name)(link2, "link");
    link2.source = function(_) {
        return arguments.length ? (source = _, link2) : source;
    };
    link2.target = function(_) {
        return arguments.length ? (target = _, link2) : target;
    };
    link2.x = function(_) {
        return arguments.length ? (x2 = typeof _ === "function" ? _ : constant_default(+_), link2) : x2;
    };
    link2.y = function(_) {
        return arguments.length ? (y2 = typeof _ === "function" ? _ : constant_default(+_), link2) : y2;
    };
    link2.context = function(_) {
        return arguments.length ? (context = _ == null ? null : _, link2) : context;
    };
    return link2;
}
(0, _chunkDLQEHMXDMjs.__name)(link, "link");
function curveHorizontal(context, x0, y0, x1, y1) {
    context.moveTo(x0, y0);
    context.bezierCurveTo(x0 = (x0 + x1) / 2, y0, x0, y1, x1, y1);
}
(0, _chunkDLQEHMXDMjs.__name)(curveHorizontal, "curveHorizontal");
function linkHorizontal() {
    return link(curveHorizontal);
}
(0, _chunkDLQEHMXDMjs.__name)(linkHorizontal, "linkHorizontal");
// ../../node_modules/.pnpm/d3-sankey@0.12.3/node_modules/d3-sankey/src/sankeyLinkHorizontal.js
function horizontalSource(d) {
    return [
        d.source.x1,
        d.y0
    ];
}
(0, _chunkDLQEHMXDMjs.__name)(horizontalSource, "horizontalSource");
function horizontalTarget(d) {
    return [
        d.target.x0,
        d.y1
    ];
}
(0, _chunkDLQEHMXDMjs.__name)(horizontalTarget, "horizontalTarget");
function sankeyLinkHorizontal_default() {
    return linkHorizontal().source(horizontalSource).target(horizontalTarget);
}
(0, _chunkDLQEHMXDMjs.__name)(sankeyLinkHorizontal_default, "default");
// src/rendering-util/uid.ts
var Uid = class _Uid {
    static #_ = (0, _chunkDLQEHMXDMjs.__name)(this, "Uid");
    static #_2 = this.count = 0;
    static next(name) {
        return new _Uid(name + ++_Uid.count);
    }
    constructor(id){
        this.id = id;
        this.href = `#${id}`;
    }
    toString() {
        return "url(" + this.href + ")";
    }
};
// src/diagrams/sankey/sankeyRenderer.ts
var alignmentsMap = {
    left,
    right,
    center,
    justify
};
var draw = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(text, id, _version, diagObj) {
    const { securityLevel, sankey: conf } = (0, _chunkDD37ZF33Mjs.getConfig2)();
    const defaultSankeyConfig = (0, _chunkDD37ZF33Mjs.defaultConfig2).sankey;
    let sandboxElement;
    if (securityLevel === "sandbox") sandboxElement = (0, _chunkDD37ZF33Mjs.select_default)("#i" + id);
    const root = securityLevel === "sandbox" ? (0, _chunkDD37ZF33Mjs.select_default)(sandboxElement.nodes()[0].contentDocument.body) : (0, _chunkDD37ZF33Mjs.select_default)("body");
    const svg = securityLevel === "sandbox" ? root.select(`[id="${id}"]`) : (0, _chunkDD37ZF33Mjs.select_default)(`[id="${id}"]`);
    const width = conf?.width ?? defaultSankeyConfig.width;
    const height = conf?.height ?? defaultSankeyConfig.width;
    const useMaxWidth = conf?.useMaxWidth ?? defaultSankeyConfig.useMaxWidth;
    const nodeAlignment = conf?.nodeAlignment ?? defaultSankeyConfig.nodeAlignment;
    const prefix = conf?.prefix ?? defaultSankeyConfig.prefix;
    const suffix = conf?.suffix ?? defaultSankeyConfig.suffix;
    const showValues = conf?.showValues ?? defaultSankeyConfig.showValues;
    const graph = diagObj.db.getGraph();
    const nodeAlign = alignmentsMap[nodeAlignment];
    const nodeWidth = 10;
    const sankey = Sankey().nodeId((d)=>d.id).nodeWidth(nodeWidth).nodePadding(10 + (showValues ? 15 : 0)).nodeAlign(nodeAlign).extent([
        [
            0,
            0
        ],
        [
            width,
            height
        ]
    ]);
    sankey(graph);
    const colorScheme = (0, _chunkDD37ZF33Mjs.ordinal)((0, _chunkDD37ZF33Mjs.Tableau10_default));
    svg.append("g").attr("class", "nodes").selectAll(".node").data(graph.nodes).join("g").attr("class", "node").attr("id", (d)=>(d.uid = Uid.next("node-")).id).attr("transform", function(d) {
        return "translate(" + d.x0 + "," + d.y0 + ")";
    }).attr("x", (d)=>d.x0).attr("y", (d)=>d.y0).append("rect").attr("height", (d)=>{
        return d.y1 - d.y0;
    }).attr("width", (d)=>d.x1 - d.x0).attr("fill", (d)=>colorScheme(d.id));
    const getText = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(({ id: id2, value: value2 })=>{
        if (!showValues) return id2;
        return `${id2}
${prefix}${Math.round(value2 * 100) / 100}${suffix}`;
    }, "getText");
    svg.append("g").attr("class", "node-labels").attr("font-family", "sans-serif").attr("font-size", 14).selectAll("text").data(graph.nodes).join("text").attr("x", (d)=>d.x0 < width / 2 ? d.x1 + 6 : d.x0 - 6).attr("y", (d)=>(d.y1 + d.y0) / 2).attr("dy", `${showValues ? "0" : "0.35"}em`).attr("text-anchor", (d)=>d.x0 < width / 2 ? "start" : "end").text(getText);
    const link2 = svg.append("g").attr("class", "links").attr("fill", "none").attr("stroke-opacity", 0.5).selectAll(".link").data(graph.links).join("g").attr("class", "link").style("mix-blend-mode", "multiply");
    const linkColor = conf?.linkColor ?? "gradient";
    if (linkColor === "gradient") {
        const gradient = link2.append("linearGradient").attr("id", (d)=>(d.uid = Uid.next("linearGradient-")).id).attr("gradientUnits", "userSpaceOnUse").attr("x1", (d)=>d.source.x1).attr("x2", (d)=>d.target.x0);
        gradient.append("stop").attr("offset", "0%").attr("stop-color", (d)=>colorScheme(d.source.id));
        gradient.append("stop").attr("offset", "100%").attr("stop-color", (d)=>colorScheme(d.target.id));
    }
    let coloring;
    switch(linkColor){
        case "gradient":
            coloring = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((d)=>d.uid, "coloring");
            break;
        case "source":
            coloring = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((d)=>colorScheme(d.source.id), "coloring");
            break;
        case "target":
            coloring = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((d)=>colorScheme(d.target.id), "coloring");
            break;
        default:
            coloring = linkColor;
    }
    link2.append("path").attr("d", sankeyLinkHorizontal_default()).attr("stroke", coloring).attr("stroke-width", (d)=>Math.max(1, d.width));
    (0, _chunkDD37ZF33Mjs.setupGraphViewbox)(void 0, svg, 0, useMaxWidth);
}, "draw");
var sankeyRenderer_default = {
    draw
};
// src/diagrams/sankey/sankeyUtils.ts
var prepareTextForParsing = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((text)=>{
    const textToParse = text.replaceAll(/^[^\S\n\r]+|[^\S\n\r]+$/g, "").replaceAll(/([\n\r])+/g, "\n").trim();
    return textToParse;
}, "prepareTextForParsing");
// src/diagrams/sankey/sankeyDiagram.ts
var originalParse = sankey_default.parse.bind(sankey_default);
sankey_default.parse = (text)=>originalParse(prepareTextForParsing(text));
var diagram = {
    parser: sankey_default,
    db: sankeyDB_default,
    renderer: sankeyRenderer_default
};

},{"./chunk-DD37ZF33.mjs":"f4pI5","./chunk-DLQEHMXD.mjs":"h5YRf","@parcel/transformer-js/src/esmodule-helpers.js":"1cdyk"}]},["deyeW"], null, "parcelRequire6955", {})

//# sourceMappingURL=sankeyDiagram-H4IPBKTF.d91f5d92.js.map
