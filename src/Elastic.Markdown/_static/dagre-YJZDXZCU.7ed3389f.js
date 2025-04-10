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
})({"pLKtn":[function(require,module,exports,__globalThis) {
var global = arguments[3];
var HMR_HOST = null;
var HMR_PORT = 1234;
var HMR_SERVER_PORT = 1234;
var HMR_SECURE = false;
var HMR_ENV_HASH = "febfef3c5b467b16";
var HMR_USE_SSE = false;
module.bundle.HMR_BUNDLE_ID = "46bc30067ed3389f";
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

},{}],"c7FQv":[function(require,module,exports,__globalThis) {
var parcelHelpers = require("@parcel/transformer-js/src/esmodule-helpers.js");
parcelHelpers.defineInteropFlag(exports);
parcelHelpers.export(exports, "layout", ()=>layout);
var _chunkULVYQCHCMjs = require("./chunk-ULVYQCHC.mjs");
var _chunkTZBO7MLIMjs = require("./chunk-TZBO7MLI.mjs");
var _chunkHD3LK5B5Mjs = require("./chunk-HD3LK5B5.mjs");
var _chunkDLQEHMXDMjs = require("./chunk-DLQEHMXD.mjs");
// ../../node_modules/.pnpm/dagre-d3-es@7.0.10/node_modules/dagre-d3-es/src/dagre/util.js
function addDummyNode(g, type, attrs, name) {
    var v;
    do v = (0, _chunkTZBO7MLIMjs.uniqueId_default)(name);
    while (g.hasNode(v));
    attrs.dummy = type;
    g.setNode(v, attrs);
    return v;
}
(0, _chunkDLQEHMXDMjs.__name)(addDummyNode, "addDummyNode");
function simplify(g) {
    var simplified = new (0, _chunkULVYQCHCMjs.Graph)().setGraph(g.graph());
    (0, _chunkTZBO7MLIMjs.forEach_default)(g.nodes(), function(v) {
        simplified.setNode(v, g.node(v));
    });
    (0, _chunkTZBO7MLIMjs.forEach_default)(g.edges(), function(e) {
        var simpleLabel = simplified.edge(e.v, e.w) || {
            weight: 0,
            minlen: 1
        };
        var label = g.edge(e);
        simplified.setEdge(e.v, e.w, {
            weight: simpleLabel.weight + label.weight,
            minlen: Math.max(simpleLabel.minlen, label.minlen)
        });
    });
    return simplified;
}
(0, _chunkDLQEHMXDMjs.__name)(simplify, "simplify");
function asNonCompoundGraph(g) {
    var simplified = new (0, _chunkULVYQCHCMjs.Graph)({
        multigraph: g.isMultigraph()
    }).setGraph(g.graph());
    (0, _chunkTZBO7MLIMjs.forEach_default)(g.nodes(), function(v) {
        if (!g.children(v).length) simplified.setNode(v, g.node(v));
    });
    (0, _chunkTZBO7MLIMjs.forEach_default)(g.edges(), function(e) {
        simplified.setEdge(e, g.edge(e));
    });
    return simplified;
}
(0, _chunkDLQEHMXDMjs.__name)(asNonCompoundGraph, "asNonCompoundGraph");
function intersectRect(rect, point) {
    var x = rect.x;
    var y = rect.y;
    var dx = point.x - x;
    var dy = point.y - y;
    var w = rect.width / 2;
    var h = rect.height / 2;
    if (!dx && !dy) throw new Error("Not possible to find intersection inside of the rectangle");
    var sx, sy;
    if (Math.abs(dy) * w > Math.abs(dx) * h) {
        if (dy < 0) h = -h;
        sx = h * dx / dy;
        sy = h;
    } else {
        if (dx < 0) w = -w;
        sx = w;
        sy = w * dy / dx;
    }
    return {
        x: x + sx,
        y: y + sy
    };
}
(0, _chunkDLQEHMXDMjs.__name)(intersectRect, "intersectRect");
function buildLayerMatrix(g) {
    var layering = (0, _chunkTZBO7MLIMjs.map_default)((0, _chunkTZBO7MLIMjs.range_default)(maxRank(g) + 1), function() {
        return [];
    });
    (0, _chunkTZBO7MLIMjs.forEach_default)(g.nodes(), function(v) {
        var node = g.node(v);
        var rank2 = node.rank;
        if (!(0, _chunkTZBO7MLIMjs.isUndefined_default)(rank2)) layering[rank2][node.order] = v;
    });
    return layering;
}
(0, _chunkDLQEHMXDMjs.__name)(buildLayerMatrix, "buildLayerMatrix");
function normalizeRanks(g) {
    var min = (0, _chunkTZBO7MLIMjs.min_default)((0, _chunkTZBO7MLIMjs.map_default)(g.nodes(), function(v) {
        return g.node(v).rank;
    }));
    (0, _chunkTZBO7MLIMjs.forEach_default)(g.nodes(), function(v) {
        var node = g.node(v);
        if ((0, _chunkTZBO7MLIMjs.has_default)(node, "rank")) node.rank -= min;
    });
}
(0, _chunkDLQEHMXDMjs.__name)(normalizeRanks, "normalizeRanks");
function removeEmptyRanks(g) {
    var offset = (0, _chunkTZBO7MLIMjs.min_default)((0, _chunkTZBO7MLIMjs.map_default)(g.nodes(), function(v) {
        return g.node(v).rank;
    }));
    var layers = [];
    (0, _chunkTZBO7MLIMjs.forEach_default)(g.nodes(), function(v) {
        var rank2 = g.node(v).rank - offset;
        if (!layers[rank2]) layers[rank2] = [];
        layers[rank2].push(v);
    });
    var delta = 0;
    var nodeRankFactor = g.graph().nodeRankFactor;
    (0, _chunkTZBO7MLIMjs.forEach_default)(layers, function(vs, i) {
        if ((0, _chunkTZBO7MLIMjs.isUndefined_default)(vs) && i % nodeRankFactor !== 0) --delta;
        else if (delta) (0, _chunkTZBO7MLIMjs.forEach_default)(vs, function(v) {
            g.node(v).rank += delta;
        });
    });
}
(0, _chunkDLQEHMXDMjs.__name)(removeEmptyRanks, "removeEmptyRanks");
function addBorderNode(g, prefix, rank2, order2) {
    var node = {
        width: 0,
        height: 0
    };
    if (arguments.length >= 4) {
        node.rank = rank2;
        node.order = order2;
    }
    return addDummyNode(g, "border", node, prefix);
}
(0, _chunkDLQEHMXDMjs.__name)(addBorderNode, "addBorderNode");
function maxRank(g) {
    return (0, _chunkTZBO7MLIMjs.max_default)((0, _chunkTZBO7MLIMjs.map_default)(g.nodes(), function(v) {
        var rank2 = g.node(v).rank;
        if (!(0, _chunkTZBO7MLIMjs.isUndefined_default)(rank2)) return rank2;
    }));
}
(0, _chunkDLQEHMXDMjs.__name)(maxRank, "maxRank");
function partition(collection, fn) {
    var result = {
        lhs: [],
        rhs: []
    };
    (0, _chunkTZBO7MLIMjs.forEach_default)(collection, function(value) {
        if (fn(value)) result.lhs.push(value);
        else result.rhs.push(value);
    });
    return result;
}
(0, _chunkDLQEHMXDMjs.__name)(partition, "partition");
function time(name, fn) {
    var start = (0, _chunkTZBO7MLIMjs.now_default)();
    try {
        return fn();
    } finally{
        console.log(name + " time: " + ((0, _chunkTZBO7MLIMjs.now_default)() - start) + "ms");
    }
}
(0, _chunkDLQEHMXDMjs.__name)(time, "time");
function notime(name, fn) {
    return fn();
}
(0, _chunkDLQEHMXDMjs.__name)(notime, "notime");
// ../../node_modules/.pnpm/dagre-d3-es@7.0.10/node_modules/dagre-d3-es/src/dagre/add-border-segments.js
function addBorderSegments(g) {
    function dfs3(v) {
        var children = g.children(v);
        var node = g.node(v);
        if (children.length) (0, _chunkTZBO7MLIMjs.forEach_default)(children, dfs3);
        if ((0, _chunkTZBO7MLIMjs.has_default)(node, "minRank")) {
            node.borderLeft = [];
            node.borderRight = [];
            for(var rank2 = node.minRank, maxRank2 = node.maxRank + 1; rank2 < maxRank2; ++rank2){
                addBorderNode2(g, "borderLeft", "_bl", v, node, rank2);
                addBorderNode2(g, "borderRight", "_br", v, node, rank2);
            }
        }
    }
    (0, _chunkDLQEHMXDMjs.__name)(dfs3, "dfs");
    (0, _chunkTZBO7MLIMjs.forEach_default)(g.children(), dfs3);
}
(0, _chunkDLQEHMXDMjs.__name)(addBorderSegments, "addBorderSegments");
function addBorderNode2(g, prop, prefix, sg, sgNode, rank2) {
    var label = {
        width: 0,
        height: 0,
        rank: rank2,
        borderType: prop
    };
    var prev = sgNode[prop][rank2 - 1];
    var curr = addDummyNode(g, "border", label, prefix);
    sgNode[prop][rank2] = curr;
    g.setParent(curr, sg);
    if (prev) g.setEdge(prev, curr, {
        weight: 1
    });
}
(0, _chunkDLQEHMXDMjs.__name)(addBorderNode2, "addBorderNode");
// ../../node_modules/.pnpm/dagre-d3-es@7.0.10/node_modules/dagre-d3-es/src/dagre/coordinate-system.js
function adjust(g) {
    var rankDir = g.graph().rankdir.toLowerCase();
    if (rankDir === "lr" || rankDir === "rl") swapWidthHeight(g);
}
(0, _chunkDLQEHMXDMjs.__name)(adjust, "adjust");
function undo(g) {
    var rankDir = g.graph().rankdir.toLowerCase();
    if (rankDir === "bt" || rankDir === "rl") reverseY(g);
    if (rankDir === "lr" || rankDir === "rl") {
        swapXY(g);
        swapWidthHeight(g);
    }
}
(0, _chunkDLQEHMXDMjs.__name)(undo, "undo");
function swapWidthHeight(g) {
    (0, _chunkTZBO7MLIMjs.forEach_default)(g.nodes(), function(v) {
        swapWidthHeightOne(g.node(v));
    });
    (0, _chunkTZBO7MLIMjs.forEach_default)(g.edges(), function(e) {
        swapWidthHeightOne(g.edge(e));
    });
}
(0, _chunkDLQEHMXDMjs.__name)(swapWidthHeight, "swapWidthHeight");
function swapWidthHeightOne(attrs) {
    var w = attrs.width;
    attrs.width = attrs.height;
    attrs.height = w;
}
(0, _chunkDLQEHMXDMjs.__name)(swapWidthHeightOne, "swapWidthHeightOne");
function reverseY(g) {
    (0, _chunkTZBO7MLIMjs.forEach_default)(g.nodes(), function(v) {
        reverseYOne(g.node(v));
    });
    (0, _chunkTZBO7MLIMjs.forEach_default)(g.edges(), function(e) {
        var edge = g.edge(e);
        (0, _chunkTZBO7MLIMjs.forEach_default)(edge.points, reverseYOne);
        if ((0, _chunkTZBO7MLIMjs.has_default)(edge, "y")) reverseYOne(edge);
    });
}
(0, _chunkDLQEHMXDMjs.__name)(reverseY, "reverseY");
function reverseYOne(attrs) {
    attrs.y = -attrs.y;
}
(0, _chunkDLQEHMXDMjs.__name)(reverseYOne, "reverseYOne");
function swapXY(g) {
    (0, _chunkTZBO7MLIMjs.forEach_default)(g.nodes(), function(v) {
        swapXYOne(g.node(v));
    });
    (0, _chunkTZBO7MLIMjs.forEach_default)(g.edges(), function(e) {
        var edge = g.edge(e);
        (0, _chunkTZBO7MLIMjs.forEach_default)(edge.points, swapXYOne);
        if ((0, _chunkTZBO7MLIMjs.has_default)(edge, "x")) swapXYOne(edge);
    });
}
(0, _chunkDLQEHMXDMjs.__name)(swapXY, "swapXY");
function swapXYOne(attrs) {
    var x = attrs.x;
    attrs.x = attrs.y;
    attrs.y = x;
}
(0, _chunkDLQEHMXDMjs.__name)(swapXYOne, "swapXYOne");
// ../../node_modules/.pnpm/dagre-d3-es@7.0.10/node_modules/dagre-d3-es/src/dagre/data/list.js
var List = class {
    static #_ = (0, _chunkDLQEHMXDMjs.__name)(this, "List");
    constructor(){
        var sentinel = {};
        sentinel._next = sentinel._prev = sentinel;
        this._sentinel = sentinel;
    }
    dequeue() {
        var sentinel = this._sentinel;
        var entry = sentinel._prev;
        if (entry !== sentinel) {
            unlink(entry);
            return entry;
        }
    }
    enqueue(entry) {
        var sentinel = this._sentinel;
        if (entry._prev && entry._next) unlink(entry);
        entry._next = sentinel._next;
        sentinel._next._prev = entry;
        sentinel._next = entry;
        entry._prev = sentinel;
    }
    toString() {
        var strs = [];
        var sentinel = this._sentinel;
        var curr = sentinel._prev;
        while(curr !== sentinel){
            strs.push(JSON.stringify(curr, filterOutLinks));
            curr = curr._prev;
        }
        return "[" + strs.join(", ") + "]";
    }
};
function unlink(entry) {
    entry._prev._next = entry._next;
    entry._next._prev = entry._prev;
    delete entry._next;
    delete entry._prev;
}
(0, _chunkDLQEHMXDMjs.__name)(unlink, "unlink");
function filterOutLinks(k, v) {
    if (k !== "_next" && k !== "_prev") return v;
}
(0, _chunkDLQEHMXDMjs.__name)(filterOutLinks, "filterOutLinks");
// ../../node_modules/.pnpm/dagre-d3-es@7.0.10/node_modules/dagre-d3-es/src/dagre/greedy-fas.js
var DEFAULT_WEIGHT_FN = (0, _chunkHD3LK5B5Mjs.constant_default)(1);
function greedyFAS(g, weightFn) {
    if (g.nodeCount() <= 1) return [];
    var state = buildState(g, weightFn || DEFAULT_WEIGHT_FN);
    var results = doGreedyFAS(state.graph, state.buckets, state.zeroIdx);
    return (0, _chunkTZBO7MLIMjs.flatten_default)((0, _chunkTZBO7MLIMjs.map_default)(results, function(e) {
        return g.outEdges(e.v, e.w);
    }));
}
(0, _chunkDLQEHMXDMjs.__name)(greedyFAS, "greedyFAS");
function doGreedyFAS(g, buckets, zeroIdx) {
    var results = [];
    var sources = buckets[buckets.length - 1];
    var sinks = buckets[0];
    var entry;
    while(g.nodeCount()){
        while(entry = sinks.dequeue())removeNode(g, buckets, zeroIdx, entry);
        while(entry = sources.dequeue())removeNode(g, buckets, zeroIdx, entry);
        if (g.nodeCount()) for(var i = buckets.length - 2; i > 0; --i){
            entry = buckets[i].dequeue();
            if (entry) {
                results = results.concat(removeNode(g, buckets, zeroIdx, entry, true));
                break;
            }
        }
    }
    return results;
}
(0, _chunkDLQEHMXDMjs.__name)(doGreedyFAS, "doGreedyFAS");
function removeNode(g, buckets, zeroIdx, entry, collectPredecessors) {
    var results = collectPredecessors ? [] : void 0;
    (0, _chunkTZBO7MLIMjs.forEach_default)(g.inEdges(entry.v), function(edge) {
        var weight = g.edge(edge);
        var uEntry = g.node(edge.v);
        if (collectPredecessors) results.push({
            v: edge.v,
            w: edge.w
        });
        uEntry.out -= weight;
        assignBucket(buckets, zeroIdx, uEntry);
    });
    (0, _chunkTZBO7MLIMjs.forEach_default)(g.outEdges(entry.v), function(edge) {
        var weight = g.edge(edge);
        var w = edge.w;
        var wEntry = g.node(w);
        wEntry["in"] -= weight;
        assignBucket(buckets, zeroIdx, wEntry);
    });
    g.removeNode(entry.v);
    return results;
}
(0, _chunkDLQEHMXDMjs.__name)(removeNode, "removeNode");
function buildState(g, weightFn) {
    var fasGraph = new (0, _chunkULVYQCHCMjs.Graph)();
    var maxIn = 0;
    var maxOut = 0;
    (0, _chunkTZBO7MLIMjs.forEach_default)(g.nodes(), function(v) {
        fasGraph.setNode(v, {
            v,
            in: 0,
            out: 0
        });
    });
    (0, _chunkTZBO7MLIMjs.forEach_default)(g.edges(), function(e) {
        var prevWeight = fasGraph.edge(e.v, e.w) || 0;
        var weight = weightFn(e);
        var edgeWeight = prevWeight + weight;
        fasGraph.setEdge(e.v, e.w, edgeWeight);
        maxOut = Math.max(maxOut, fasGraph.node(e.v).out += weight);
        maxIn = Math.max(maxIn, fasGraph.node(e.w)["in"] += weight);
    });
    var buckets = (0, _chunkTZBO7MLIMjs.range_default)(maxOut + maxIn + 3).map(function() {
        return new List();
    });
    var zeroIdx = maxIn + 1;
    (0, _chunkTZBO7MLIMjs.forEach_default)(fasGraph.nodes(), function(v) {
        assignBucket(buckets, zeroIdx, fasGraph.node(v));
    });
    return {
        graph: fasGraph,
        buckets,
        zeroIdx
    };
}
(0, _chunkDLQEHMXDMjs.__name)(buildState, "buildState");
function assignBucket(buckets, zeroIdx, entry) {
    if (!entry.out) buckets[0].enqueue(entry);
    else if (!entry["in"]) buckets[buckets.length - 1].enqueue(entry);
    else buckets[entry.out - entry["in"] + zeroIdx].enqueue(entry);
}
(0, _chunkDLQEHMXDMjs.__name)(assignBucket, "assignBucket");
// ../../node_modules/.pnpm/dagre-d3-es@7.0.10/node_modules/dagre-d3-es/src/dagre/acyclic.js
function run(g) {
    var fas = g.graph().acyclicer === "greedy" ? greedyFAS(g, weightFn(g)) : dfsFAS(g);
    (0, _chunkTZBO7MLIMjs.forEach_default)(fas, function(e) {
        var label = g.edge(e);
        g.removeEdge(e);
        label.forwardName = e.name;
        label.reversed = true;
        g.setEdge(e.w, e.v, label, (0, _chunkTZBO7MLIMjs.uniqueId_default)("rev"));
    });
    function weightFn(g2) {
        return function(e) {
            return g2.edge(e).weight;
        };
    }
    (0, _chunkDLQEHMXDMjs.__name)(weightFn, "weightFn");
}
(0, _chunkDLQEHMXDMjs.__name)(run, "run");
function dfsFAS(g) {
    var fas = [];
    var stack = {};
    var visited = {};
    function dfs3(v) {
        if ((0, _chunkTZBO7MLIMjs.has_default)(visited, v)) return;
        visited[v] = true;
        stack[v] = true;
        (0, _chunkTZBO7MLIMjs.forEach_default)(g.outEdges(v), function(e) {
            if ((0, _chunkTZBO7MLIMjs.has_default)(stack, e.w)) fas.push(e);
            else dfs3(e.w);
        });
        delete stack[v];
    }
    (0, _chunkDLQEHMXDMjs.__name)(dfs3, "dfs");
    (0, _chunkTZBO7MLIMjs.forEach_default)(g.nodes(), dfs3);
    return fas;
}
(0, _chunkDLQEHMXDMjs.__name)(dfsFAS, "dfsFAS");
function undo2(g) {
    (0, _chunkTZBO7MLIMjs.forEach_default)(g.edges(), function(e) {
        var label = g.edge(e);
        if (label.reversed) {
            g.removeEdge(e);
            var forwardName = label.forwardName;
            delete label.reversed;
            delete label.forwardName;
            g.setEdge(e.w, e.v, label, forwardName);
        }
    });
}
(0, _chunkDLQEHMXDMjs.__name)(undo2, "undo");
// ../../node_modules/.pnpm/dagre-d3-es@7.0.10/node_modules/dagre-d3-es/src/dagre/normalize.js
function run2(g) {
    g.graph().dummyChains = [];
    (0, _chunkTZBO7MLIMjs.forEach_default)(g.edges(), function(edge) {
        normalizeEdge(g, edge);
    });
}
(0, _chunkDLQEHMXDMjs.__name)(run2, "run");
function normalizeEdge(g, e) {
    var v = e.v;
    var vRank = g.node(v).rank;
    var w = e.w;
    var wRank = g.node(w).rank;
    var name = e.name;
    var edgeLabel = g.edge(e);
    var labelRank = edgeLabel.labelRank;
    if (wRank === vRank + 1) return;
    g.removeEdge(e);
    var dummy, attrs, i;
    for(i = 0, ++vRank; vRank < wRank; ++i, ++vRank){
        edgeLabel.points = [];
        attrs = {
            width: 0,
            height: 0,
            edgeLabel,
            edgeObj: e,
            rank: vRank
        };
        dummy = addDummyNode(g, "edge", attrs, "_d");
        if (vRank === labelRank) {
            attrs.width = edgeLabel.width;
            attrs.height = edgeLabel.height;
            attrs.dummy = "edge-label";
            attrs.labelpos = edgeLabel.labelpos;
        }
        g.setEdge(v, dummy, {
            weight: edgeLabel.weight
        }, name);
        if (i === 0) g.graph().dummyChains.push(dummy);
        v = dummy;
    }
    g.setEdge(v, w, {
        weight: edgeLabel.weight
    }, name);
}
(0, _chunkDLQEHMXDMjs.__name)(normalizeEdge, "normalizeEdge");
function undo3(g) {
    (0, _chunkTZBO7MLIMjs.forEach_default)(g.graph().dummyChains, function(v) {
        var node = g.node(v);
        var origLabel = node.edgeLabel;
        var w;
        g.setEdge(node.edgeObj, origLabel);
        while(node.dummy){
            w = g.successors(v)[0];
            g.removeNode(v);
            origLabel.points.push({
                x: node.x,
                y: node.y
            });
            if (node.dummy === "edge-label") {
                origLabel.x = node.x;
                origLabel.y = node.y;
                origLabel.width = node.width;
                origLabel.height = node.height;
            }
            v = w;
            node = g.node(v);
        }
    });
}
(0, _chunkDLQEHMXDMjs.__name)(undo3, "undo");
// ../../node_modules/.pnpm/dagre-d3-es@7.0.10/node_modules/dagre-d3-es/src/dagre/rank/util.js
function longestPath(g) {
    var visited = {};
    function dfs3(v) {
        var label = g.node(v);
        if ((0, _chunkTZBO7MLIMjs.has_default)(visited, v)) return label.rank;
        visited[v] = true;
        var rank2 = (0, _chunkTZBO7MLIMjs.min_default)((0, _chunkTZBO7MLIMjs.map_default)(g.outEdges(v), function(e) {
            return dfs3(e.w) - g.edge(e).minlen;
        }));
        if (rank2 === Number.POSITIVE_INFINITY || // return value of _.map([]) for Lodash 3
        rank2 === void 0 || // return value of _.map([]) for Lodash 4
        rank2 === null) rank2 = 0;
        return label.rank = rank2;
    }
    (0, _chunkDLQEHMXDMjs.__name)(dfs3, "dfs");
    (0, _chunkTZBO7MLIMjs.forEach_default)(g.sources(), dfs3);
}
(0, _chunkDLQEHMXDMjs.__name)(longestPath, "longestPath");
function slack(g, e) {
    return g.node(e.w).rank - g.node(e.v).rank - g.edge(e).minlen;
}
(0, _chunkDLQEHMXDMjs.__name)(slack, "slack");
// ../../node_modules/.pnpm/dagre-d3-es@7.0.10/node_modules/dagre-d3-es/src/dagre/rank/feasible-tree.js
function feasibleTree(g) {
    var t = new (0, _chunkULVYQCHCMjs.Graph)({
        directed: false
    });
    var start = g.nodes()[0];
    var size = g.nodeCount();
    t.setNode(start, {});
    var edge, delta;
    while(tightTree(t, g) < size){
        edge = findMinSlackEdge(t, g);
        delta = t.hasNode(edge.v) ? slack(g, edge) : -slack(g, edge);
        shiftRanks(t, g, delta);
    }
    return t;
}
(0, _chunkDLQEHMXDMjs.__name)(feasibleTree, "feasibleTree");
function tightTree(t, g) {
    function dfs3(v) {
        (0, _chunkTZBO7MLIMjs.forEach_default)(g.nodeEdges(v), function(e) {
            var edgeV = e.v, w = v === edgeV ? e.w : edgeV;
            if (!t.hasNode(w) && !slack(g, e)) {
                t.setNode(w, {});
                t.setEdge(v, w, {});
                dfs3(w);
            }
        });
    }
    (0, _chunkDLQEHMXDMjs.__name)(dfs3, "dfs");
    (0, _chunkTZBO7MLIMjs.forEach_default)(t.nodes(), dfs3);
    return t.nodeCount();
}
(0, _chunkDLQEHMXDMjs.__name)(tightTree, "tightTree");
function findMinSlackEdge(t, g) {
    return (0, _chunkTZBO7MLIMjs.minBy_default)(g.edges(), function(e) {
        if (t.hasNode(e.v) !== t.hasNode(e.w)) return slack(g, e);
    });
}
(0, _chunkDLQEHMXDMjs.__name)(findMinSlackEdge, "findMinSlackEdge");
function shiftRanks(t, g, delta) {
    (0, _chunkTZBO7MLIMjs.forEach_default)(t.nodes(), function(v) {
        g.node(v).rank += delta;
    });
}
(0, _chunkDLQEHMXDMjs.__name)(shiftRanks, "shiftRanks");
// ../../node_modules/.pnpm/dagre-d3-es@7.0.10/node_modules/dagre-d3-es/src/graphlib/alg/dijkstra.js
var DEFAULT_WEIGHT_FUNC = (0, _chunkHD3LK5B5Mjs.constant_default)(1);
// ../../node_modules/.pnpm/dagre-d3-es@7.0.10/node_modules/dagre-d3-es/src/graphlib/alg/floyd-warshall.js
var DEFAULT_WEIGHT_FUNC2 = (0, _chunkHD3LK5B5Mjs.constant_default)(1);
// ../../node_modules/.pnpm/dagre-d3-es@7.0.10/node_modules/dagre-d3-es/src/graphlib/alg/topsort.js
topsort.CycleException = CycleException;
function topsort(g) {
    var visited = {};
    var stack = {};
    var results = [];
    function visit(node) {
        if ((0, _chunkTZBO7MLIMjs.has_default)(stack, node)) throw new CycleException();
        if (!(0, _chunkTZBO7MLIMjs.has_default)(visited, node)) {
            stack[node] = true;
            visited[node] = true;
            (0, _chunkTZBO7MLIMjs.forEach_default)(g.predecessors(node), visit);
            delete stack[node];
            results.push(node);
        }
    }
    (0, _chunkDLQEHMXDMjs.__name)(visit, "visit");
    (0, _chunkTZBO7MLIMjs.forEach_default)(g.sinks(), visit);
    if ((0, _chunkTZBO7MLIMjs.size_default)(visited) !== g.nodeCount()) throw new CycleException();
    return results;
}
(0, _chunkDLQEHMXDMjs.__name)(topsort, "topsort");
function CycleException() {}
(0, _chunkDLQEHMXDMjs.__name)(CycleException, "CycleException");
CycleException.prototype = new Error();
// ../../node_modules/.pnpm/dagre-d3-es@7.0.10/node_modules/dagre-d3-es/src/graphlib/alg/dfs.js
function dfs(g, vs, order2) {
    if (!(0, _chunkHD3LK5B5Mjs.isArray_default)(vs)) vs = [
        vs
    ];
    var navigation = (g.isDirected() ? g.successors : g.neighbors).bind(g);
    var acc = [];
    var visited = {};
    (0, _chunkTZBO7MLIMjs.forEach_default)(vs, function(v) {
        if (!g.hasNode(v)) throw new Error("Graph does not have node: " + v);
        doDfs(g, v, order2 === "post", visited, navigation, acc);
    });
    return acc;
}
(0, _chunkDLQEHMXDMjs.__name)(dfs, "dfs");
function doDfs(g, v, postorder3, visited, navigation, acc) {
    if (!(0, _chunkTZBO7MLIMjs.has_default)(visited, v)) {
        visited[v] = true;
        if (!postorder3) acc.push(v);
        (0, _chunkTZBO7MLIMjs.forEach_default)(navigation(v), function(w) {
            doDfs(g, w, postorder3, visited, navigation, acc);
        });
        if (postorder3) acc.push(v);
    }
}
(0, _chunkDLQEHMXDMjs.__name)(doDfs, "doDfs");
// ../../node_modules/.pnpm/dagre-d3-es@7.0.10/node_modules/dagre-d3-es/src/graphlib/alg/postorder.js
function postorder(g, vs) {
    return dfs(g, vs, "post");
}
(0, _chunkDLQEHMXDMjs.__name)(postorder, "postorder");
// ../../node_modules/.pnpm/dagre-d3-es@7.0.10/node_modules/dagre-d3-es/src/graphlib/alg/preorder.js
function preorder(g, vs) {
    return dfs(g, vs, "pre");
}
(0, _chunkDLQEHMXDMjs.__name)(preorder, "preorder");
// ../../node_modules/.pnpm/dagre-d3-es@7.0.10/node_modules/dagre-d3-es/src/dagre/rank/network-simplex.js
networkSimplex.initLowLimValues = initLowLimValues;
networkSimplex.initCutValues = initCutValues;
networkSimplex.calcCutValue = calcCutValue;
networkSimplex.leaveEdge = leaveEdge;
networkSimplex.enterEdge = enterEdge;
networkSimplex.exchangeEdges = exchangeEdges;
function networkSimplex(g) {
    g = simplify(g);
    longestPath(g);
    var t = feasibleTree(g);
    initLowLimValues(t);
    initCutValues(t, g);
    var e, f;
    while(e = leaveEdge(t)){
        f = enterEdge(t, g, e);
        exchangeEdges(t, g, e, f);
    }
}
(0, _chunkDLQEHMXDMjs.__name)(networkSimplex, "networkSimplex");
function initCutValues(t, g) {
    var vs = postorder(t, t.nodes());
    vs = vs.slice(0, vs.length - 1);
    (0, _chunkTZBO7MLIMjs.forEach_default)(vs, function(v) {
        assignCutValue(t, g, v);
    });
}
(0, _chunkDLQEHMXDMjs.__name)(initCutValues, "initCutValues");
function assignCutValue(t, g, child) {
    var childLab = t.node(child);
    var parent = childLab.parent;
    t.edge(child, parent).cutvalue = calcCutValue(t, g, child);
}
(0, _chunkDLQEHMXDMjs.__name)(assignCutValue, "assignCutValue");
function calcCutValue(t, g, child) {
    var childLab = t.node(child);
    var parent = childLab.parent;
    var childIsTail = true;
    var graphEdge = g.edge(child, parent);
    var cutValue = 0;
    if (!graphEdge) {
        childIsTail = false;
        graphEdge = g.edge(parent, child);
    }
    cutValue = graphEdge.weight;
    (0, _chunkTZBO7MLIMjs.forEach_default)(g.nodeEdges(child), function(e) {
        var isOutEdge = e.v === child, other = isOutEdge ? e.w : e.v;
        if (other !== parent) {
            var pointsToHead = isOutEdge === childIsTail, otherWeight = g.edge(e).weight;
            cutValue += pointsToHead ? otherWeight : -otherWeight;
            if (isTreeEdge(t, child, other)) {
                var otherCutValue = t.edge(child, other).cutvalue;
                cutValue += pointsToHead ? -otherCutValue : otherCutValue;
            }
        }
    });
    return cutValue;
}
(0, _chunkDLQEHMXDMjs.__name)(calcCutValue, "calcCutValue");
function initLowLimValues(tree, root) {
    if (arguments.length < 2) root = tree.nodes()[0];
    dfsAssignLowLim(tree, {}, 1, root);
}
(0, _chunkDLQEHMXDMjs.__name)(initLowLimValues, "initLowLimValues");
function dfsAssignLowLim(tree, visited, nextLim, v, parent) {
    var low = nextLim;
    var label = tree.node(v);
    visited[v] = true;
    (0, _chunkTZBO7MLIMjs.forEach_default)(tree.neighbors(v), function(w) {
        if (!(0, _chunkTZBO7MLIMjs.has_default)(visited, w)) nextLim = dfsAssignLowLim(tree, visited, nextLim, w, v);
    });
    label.low = low;
    label.lim = nextLim++;
    if (parent) label.parent = parent;
    else delete label.parent;
    return nextLim;
}
(0, _chunkDLQEHMXDMjs.__name)(dfsAssignLowLim, "dfsAssignLowLim");
function leaveEdge(tree) {
    return (0, _chunkTZBO7MLIMjs.find_default)(tree.edges(), function(e) {
        return tree.edge(e).cutvalue < 0;
    });
}
(0, _chunkDLQEHMXDMjs.__name)(leaveEdge, "leaveEdge");
function enterEdge(t, g, edge) {
    var v = edge.v;
    var w = edge.w;
    if (!g.hasEdge(v, w)) {
        v = edge.w;
        w = edge.v;
    }
    var vLabel = t.node(v);
    var wLabel = t.node(w);
    var tailLabel = vLabel;
    var flip = false;
    if (vLabel.lim > wLabel.lim) {
        tailLabel = wLabel;
        flip = true;
    }
    var candidates = (0, _chunkTZBO7MLIMjs.filter_default)(g.edges(), function(edge2) {
        return flip === isDescendant(t, t.node(edge2.v), tailLabel) && flip !== isDescendant(t, t.node(edge2.w), tailLabel);
    });
    return (0, _chunkTZBO7MLIMjs.minBy_default)(candidates, function(edge2) {
        return slack(g, edge2);
    });
}
(0, _chunkDLQEHMXDMjs.__name)(enterEdge, "enterEdge");
function exchangeEdges(t, g, e, f) {
    var v = e.v;
    var w = e.w;
    t.removeEdge(v, w);
    t.setEdge(f.v, f.w, {});
    initLowLimValues(t);
    initCutValues(t, g);
    updateRanks(t, g);
}
(0, _chunkDLQEHMXDMjs.__name)(exchangeEdges, "exchangeEdges");
function updateRanks(t, g) {
    var root = (0, _chunkTZBO7MLIMjs.find_default)(t.nodes(), function(v) {
        return !g.node(v).parent;
    });
    var vs = preorder(t, root);
    vs = vs.slice(1);
    (0, _chunkTZBO7MLIMjs.forEach_default)(vs, function(v) {
        var parent = t.node(v).parent, edge = g.edge(v, parent), flipped = false;
        if (!edge) {
            edge = g.edge(parent, v);
            flipped = true;
        }
        g.node(v).rank = g.node(parent).rank + (flipped ? edge.minlen : -edge.minlen);
    });
}
(0, _chunkDLQEHMXDMjs.__name)(updateRanks, "updateRanks");
function isTreeEdge(tree, u, v) {
    return tree.hasEdge(u, v);
}
(0, _chunkDLQEHMXDMjs.__name)(isTreeEdge, "isTreeEdge");
function isDescendant(tree, vLabel, rootLabel) {
    return rootLabel.low <= vLabel.lim && vLabel.lim <= rootLabel.lim;
}
(0, _chunkDLQEHMXDMjs.__name)(isDescendant, "isDescendant");
// ../../node_modules/.pnpm/dagre-d3-es@7.0.10/node_modules/dagre-d3-es/src/dagre/rank/index.js
function rank(g) {
    switch(g.graph().ranker){
        case "network-simplex":
            networkSimplexRanker(g);
            break;
        case "tight-tree":
            tightTreeRanker(g);
            break;
        case "longest-path":
            longestPathRanker(g);
            break;
        default:
            networkSimplexRanker(g);
    }
}
(0, _chunkDLQEHMXDMjs.__name)(rank, "rank");
var longestPathRanker = longestPath;
function tightTreeRanker(g) {
    longestPath(g);
    feasibleTree(g);
}
(0, _chunkDLQEHMXDMjs.__name)(tightTreeRanker, "tightTreeRanker");
function networkSimplexRanker(g) {
    networkSimplex(g);
}
(0, _chunkDLQEHMXDMjs.__name)(networkSimplexRanker, "networkSimplexRanker");
// ../../node_modules/.pnpm/dagre-d3-es@7.0.10/node_modules/dagre-d3-es/src/dagre/nesting-graph.js
function run3(g) {
    var root = addDummyNode(g, "root", {}, "_root");
    var depths = treeDepths(g);
    var height = (0, _chunkTZBO7MLIMjs.max_default)((0, _chunkTZBO7MLIMjs.values_default)(depths)) - 1;
    var nodeSep = 2 * height + 1;
    g.graph().nestingRoot = root;
    (0, _chunkTZBO7MLIMjs.forEach_default)(g.edges(), function(e) {
        g.edge(e).minlen *= nodeSep;
    });
    var weight = sumWeights(g) + 1;
    (0, _chunkTZBO7MLIMjs.forEach_default)(g.children(), function(child) {
        dfs2(g, root, nodeSep, weight, height, depths, child);
    });
    g.graph().nodeRankFactor = nodeSep;
}
(0, _chunkDLQEHMXDMjs.__name)(run3, "run");
function dfs2(g, root, nodeSep, weight, height, depths, v) {
    var children = g.children(v);
    if (!children.length) {
        if (v !== root) g.setEdge(root, v, {
            weight: 0,
            minlen: nodeSep
        });
        return;
    }
    var top = addBorderNode(g, "_bt");
    var bottom = addBorderNode(g, "_bb");
    var label = g.node(v);
    g.setParent(top, v);
    label.borderTop = top;
    g.setParent(bottom, v);
    label.borderBottom = bottom;
    (0, _chunkTZBO7MLIMjs.forEach_default)(children, function(child) {
        dfs2(g, root, nodeSep, weight, height, depths, child);
        var childNode = g.node(child);
        var childTop = childNode.borderTop ? childNode.borderTop : child;
        var childBottom = childNode.borderBottom ? childNode.borderBottom : child;
        var thisWeight = childNode.borderTop ? weight : 2 * weight;
        var minlen = childTop !== childBottom ? 1 : height - depths[v] + 1;
        g.setEdge(top, childTop, {
            weight: thisWeight,
            minlen,
            nestingEdge: true
        });
        g.setEdge(childBottom, bottom, {
            weight: thisWeight,
            minlen,
            nestingEdge: true
        });
    });
    if (!g.parent(v)) g.setEdge(root, top, {
        weight: 0,
        minlen: height + depths[v]
    });
}
(0, _chunkDLQEHMXDMjs.__name)(dfs2, "dfs");
function treeDepths(g) {
    var depths = {};
    function dfs3(v, depth) {
        var children = g.children(v);
        if (children && children.length) (0, _chunkTZBO7MLIMjs.forEach_default)(children, function(child) {
            dfs3(child, depth + 1);
        });
        depths[v] = depth;
    }
    (0, _chunkDLQEHMXDMjs.__name)(dfs3, "dfs");
    (0, _chunkTZBO7MLIMjs.forEach_default)(g.children(), function(v) {
        dfs3(v, 1);
    });
    return depths;
}
(0, _chunkDLQEHMXDMjs.__name)(treeDepths, "treeDepths");
function sumWeights(g) {
    return (0, _chunkTZBO7MLIMjs.reduce_default)(g.edges(), function(acc, e) {
        return acc + g.edge(e).weight;
    }, 0);
}
(0, _chunkDLQEHMXDMjs.__name)(sumWeights, "sumWeights");
function cleanup(g) {
    var graphLabel = g.graph();
    g.removeNode(graphLabel.nestingRoot);
    delete graphLabel.nestingRoot;
    (0, _chunkTZBO7MLIMjs.forEach_default)(g.edges(), function(e) {
        var edge = g.edge(e);
        if (edge.nestingEdge) g.removeEdge(e);
    });
}
(0, _chunkDLQEHMXDMjs.__name)(cleanup, "cleanup");
// ../../node_modules/.pnpm/dagre-d3-es@7.0.10/node_modules/dagre-d3-es/src/dagre/order/add-subgraph-constraints.js
function addSubgraphConstraints(g, cg, vs) {
    var prev = {}, rootPrev;
    (0, _chunkTZBO7MLIMjs.forEach_default)(vs, function(v) {
        var child = g.parent(v), parent, prevChild;
        while(child){
            parent = g.parent(child);
            if (parent) {
                prevChild = prev[parent];
                prev[parent] = child;
            } else {
                prevChild = rootPrev;
                rootPrev = child;
            }
            if (prevChild && prevChild !== child) {
                cg.setEdge(prevChild, child);
                return;
            }
            child = parent;
        }
    });
}
(0, _chunkDLQEHMXDMjs.__name)(addSubgraphConstraints, "addSubgraphConstraints");
// ../../node_modules/.pnpm/dagre-d3-es@7.0.10/node_modules/dagre-d3-es/src/dagre/order/build-layer-graph.js
function buildLayerGraph(g, rank2, relationship) {
    var root = createRootNode(g), result = new (0, _chunkULVYQCHCMjs.Graph)({
        compound: true
    }).setGraph({
        root
    }).setDefaultNodeLabel(function(v) {
        return g.node(v);
    });
    (0, _chunkTZBO7MLIMjs.forEach_default)(g.nodes(), function(v) {
        var node = g.node(v), parent = g.parent(v);
        if (node.rank === rank2 || node.minRank <= rank2 && rank2 <= node.maxRank) {
            result.setNode(v);
            result.setParent(v, parent || root);
            (0, _chunkTZBO7MLIMjs.forEach_default)(g[relationship](v), function(e) {
                var u = e.v === v ? e.w : e.v, edge = result.edge(u, v), weight = !(0, _chunkTZBO7MLIMjs.isUndefined_default)(edge) ? edge.weight : 0;
                result.setEdge(u, v, {
                    weight: g.edge(e).weight + weight
                });
            });
            if ((0, _chunkTZBO7MLIMjs.has_default)(node, "minRank")) result.setNode(v, {
                borderLeft: node.borderLeft[rank2],
                borderRight: node.borderRight[rank2]
            });
        }
    });
    return result;
}
(0, _chunkDLQEHMXDMjs.__name)(buildLayerGraph, "buildLayerGraph");
function createRootNode(g) {
    var v;
    while(g.hasNode(v = (0, _chunkTZBO7MLIMjs.uniqueId_default)("_root")));
    return v;
}
(0, _chunkDLQEHMXDMjs.__name)(createRootNode, "createRootNode");
// ../../node_modules/.pnpm/dagre-d3-es@7.0.10/node_modules/dagre-d3-es/src/dagre/order/cross-count.js
function crossCount(g, layering) {
    var cc = 0;
    for(var i = 1; i < layering.length; ++i)cc += twoLayerCrossCount(g, layering[i - 1], layering[i]);
    return cc;
}
(0, _chunkDLQEHMXDMjs.__name)(crossCount, "crossCount");
function twoLayerCrossCount(g, northLayer, southLayer) {
    var southPos = (0, _chunkTZBO7MLIMjs.zipObject_default)(southLayer, (0, _chunkTZBO7MLIMjs.map_default)(southLayer, function(v, i) {
        return i;
    }));
    var southEntries = (0, _chunkTZBO7MLIMjs.flatten_default)((0, _chunkTZBO7MLIMjs.map_default)(northLayer, function(v) {
        return (0, _chunkTZBO7MLIMjs.sortBy_default)((0, _chunkTZBO7MLIMjs.map_default)(g.outEdges(v), function(e) {
            return {
                pos: southPos[e.w],
                weight: g.edge(e).weight
            };
        }), "pos");
    }));
    var firstIndex = 1;
    while(firstIndex < southLayer.length)firstIndex <<= 1;
    var treeSize = 2 * firstIndex - 1;
    firstIndex -= 1;
    var tree = (0, _chunkTZBO7MLIMjs.map_default)(new Array(treeSize), function() {
        return 0;
    });
    var cc = 0;
    (0, _chunkTZBO7MLIMjs.forEach_default)(// @ts-expect-error
    southEntries.forEach(function(entry) {
        var index = entry.pos + firstIndex;
        tree[index] += entry.weight;
        var weightSum = 0;
        while(index > 0){
            if (index % 2) weightSum += tree[index + 1];
            index = index - 1 >> 1;
            tree[index] += entry.weight;
        }
        cc += entry.weight * weightSum;
    }));
    return cc;
}
(0, _chunkDLQEHMXDMjs.__name)(twoLayerCrossCount, "twoLayerCrossCount");
// ../../node_modules/.pnpm/dagre-d3-es@7.0.10/node_modules/dagre-d3-es/src/dagre/order/init-order.js
function initOrder(g) {
    var visited = {};
    var simpleNodes = (0, _chunkTZBO7MLIMjs.filter_default)(g.nodes(), function(v) {
        return !g.children(v).length;
    });
    var maxRank2 = (0, _chunkTZBO7MLIMjs.max_default)((0, _chunkTZBO7MLIMjs.map_default)(simpleNodes, function(v) {
        return g.node(v).rank;
    }));
    var layers = (0, _chunkTZBO7MLIMjs.map_default)((0, _chunkTZBO7MLIMjs.range_default)(maxRank2 + 1), function() {
        return [];
    });
    function dfs3(v) {
        if ((0, _chunkTZBO7MLIMjs.has_default)(visited, v)) return;
        visited[v] = true;
        var node = g.node(v);
        layers[node.rank].push(v);
        (0, _chunkTZBO7MLIMjs.forEach_default)(g.successors(v), dfs3);
    }
    (0, _chunkDLQEHMXDMjs.__name)(dfs3, "dfs");
    var orderedVs = (0, _chunkTZBO7MLIMjs.sortBy_default)(simpleNodes, function(v) {
        return g.node(v).rank;
    });
    (0, _chunkTZBO7MLIMjs.forEach_default)(orderedVs, dfs3);
    return layers;
}
(0, _chunkDLQEHMXDMjs.__name)(initOrder, "initOrder");
// ../../node_modules/.pnpm/dagre-d3-es@7.0.10/node_modules/dagre-d3-es/src/dagre/order/barycenter.js
function barycenter(g, movable) {
    return (0, _chunkTZBO7MLIMjs.map_default)(movable, function(v) {
        var inV = g.inEdges(v);
        if (!inV.length) return {
            v
        };
        else {
            var result = (0, _chunkTZBO7MLIMjs.reduce_default)(inV, function(acc, e) {
                var edge = g.edge(e), nodeU = g.node(e.v);
                return {
                    sum: acc.sum + edge.weight * nodeU.order,
                    weight: acc.weight + edge.weight
                };
            }, {
                sum: 0,
                weight: 0
            });
            return {
                v,
                barycenter: result.sum / result.weight,
                weight: result.weight
            };
        }
    });
}
(0, _chunkDLQEHMXDMjs.__name)(barycenter, "barycenter");
// ../../node_modules/.pnpm/dagre-d3-es@7.0.10/node_modules/dagre-d3-es/src/dagre/order/resolve-conflicts.js
function resolveConflicts(entries, cg) {
    var mappedEntries = {};
    (0, _chunkTZBO7MLIMjs.forEach_default)(entries, function(entry, i) {
        var tmp = mappedEntries[entry.v] = {
            indegree: 0,
            in: [],
            out: [],
            vs: [
                entry.v
            ],
            i
        };
        if (!(0, _chunkTZBO7MLIMjs.isUndefined_default)(entry.barycenter)) {
            tmp.barycenter = entry.barycenter;
            tmp.weight = entry.weight;
        }
    });
    (0, _chunkTZBO7MLIMjs.forEach_default)(cg.edges(), function(e) {
        var entryV = mappedEntries[e.v];
        var entryW = mappedEntries[e.w];
        if (!(0, _chunkTZBO7MLIMjs.isUndefined_default)(entryV) && !(0, _chunkTZBO7MLIMjs.isUndefined_default)(entryW)) {
            entryW.indegree++;
            entryV.out.push(mappedEntries[e.w]);
        }
    });
    var sourceSet = (0, _chunkTZBO7MLIMjs.filter_default)(mappedEntries, function(entry) {
        return !entry.indegree;
    });
    return doResolveConflicts(sourceSet);
}
(0, _chunkDLQEHMXDMjs.__name)(resolveConflicts, "resolveConflicts");
function doResolveConflicts(sourceSet) {
    var entries = [];
    function handleIn(vEntry) {
        return function(uEntry) {
            if (uEntry.merged) return;
            if ((0, _chunkTZBO7MLIMjs.isUndefined_default)(uEntry.barycenter) || (0, _chunkTZBO7MLIMjs.isUndefined_default)(vEntry.barycenter) || uEntry.barycenter >= vEntry.barycenter) mergeEntries(vEntry, uEntry);
        };
    }
    (0, _chunkDLQEHMXDMjs.__name)(handleIn, "handleIn");
    function handleOut(vEntry) {
        return function(wEntry) {
            wEntry["in"].push(vEntry);
            if (--wEntry.indegree === 0) sourceSet.push(wEntry);
        };
    }
    (0, _chunkDLQEHMXDMjs.__name)(handleOut, "handleOut");
    while(sourceSet.length){
        var entry = sourceSet.pop();
        entries.push(entry);
        (0, _chunkTZBO7MLIMjs.forEach_default)(entry["in"].reverse(), handleIn(entry));
        (0, _chunkTZBO7MLIMjs.forEach_default)(entry.out, handleOut(entry));
    }
    return (0, _chunkTZBO7MLIMjs.map_default)((0, _chunkTZBO7MLIMjs.filter_default)(entries, function(entry2) {
        return !entry2.merged;
    }), function(entry2) {
        return (0, _chunkTZBO7MLIMjs.pick_default)(entry2, [
            "vs",
            "i",
            "barycenter",
            "weight"
        ]);
    });
}
(0, _chunkDLQEHMXDMjs.__name)(doResolveConflicts, "doResolveConflicts");
function mergeEntries(target, source) {
    var sum = 0;
    var weight = 0;
    if (target.weight) {
        sum += target.barycenter * target.weight;
        weight += target.weight;
    }
    if (source.weight) {
        sum += source.barycenter * source.weight;
        weight += source.weight;
    }
    target.vs = source.vs.concat(target.vs);
    target.barycenter = sum / weight;
    target.weight = weight;
    target.i = Math.min(source.i, target.i);
    source.merged = true;
}
(0, _chunkDLQEHMXDMjs.__name)(mergeEntries, "mergeEntries");
// ../../node_modules/.pnpm/dagre-d3-es@7.0.10/node_modules/dagre-d3-es/src/dagre/order/sort.js
function sort(entries, biasRight) {
    var parts = partition(entries, function(entry) {
        return (0, _chunkTZBO7MLIMjs.has_default)(entry, "barycenter");
    });
    var sortable = parts.lhs, unsortable = (0, _chunkTZBO7MLIMjs.sortBy_default)(parts.rhs, function(entry) {
        return -entry.i;
    }), vs = [], sum = 0, weight = 0, vsIndex = 0;
    sortable.sort(compareWithBias(!!biasRight));
    vsIndex = consumeUnsortable(vs, unsortable, vsIndex);
    (0, _chunkTZBO7MLIMjs.forEach_default)(sortable, function(entry) {
        vsIndex += entry.vs.length;
        vs.push(entry.vs);
        sum += entry.barycenter * entry.weight;
        weight += entry.weight;
        vsIndex = consumeUnsortable(vs, unsortable, vsIndex);
    });
    var result = {
        vs: (0, _chunkTZBO7MLIMjs.flatten_default)(vs)
    };
    if (weight) {
        result.barycenter = sum / weight;
        result.weight = weight;
    }
    return result;
}
(0, _chunkDLQEHMXDMjs.__name)(sort, "sort");
function consumeUnsortable(vs, unsortable, index) {
    var last;
    while(unsortable.length && (last = (0, _chunkTZBO7MLIMjs.last_default)(unsortable)).i <= index){
        unsortable.pop();
        vs.push(last.vs);
        index++;
    }
    return index;
}
(0, _chunkDLQEHMXDMjs.__name)(consumeUnsortable, "consumeUnsortable");
function compareWithBias(bias) {
    return function(entryV, entryW) {
        if (entryV.barycenter < entryW.barycenter) return -1;
        else if (entryV.barycenter > entryW.barycenter) return 1;
        return !bias ? entryV.i - entryW.i : entryW.i - entryV.i;
    };
}
(0, _chunkDLQEHMXDMjs.__name)(compareWithBias, "compareWithBias");
// ../../node_modules/.pnpm/dagre-d3-es@7.0.10/node_modules/dagre-d3-es/src/dagre/order/sort-subgraph.js
function sortSubgraph(g, v, cg, biasRight) {
    var movable = g.children(v);
    var node = g.node(v);
    var bl = node ? node.borderLeft : void 0;
    var br = node ? node.borderRight : void 0;
    var subgraphs = {};
    if (bl) movable = (0, _chunkTZBO7MLIMjs.filter_default)(movable, function(w) {
        return w !== bl && w !== br;
    });
    var barycenters = barycenter(g, movable);
    (0, _chunkTZBO7MLIMjs.forEach_default)(barycenters, function(entry) {
        if (g.children(entry.v).length) {
            var subgraphResult = sortSubgraph(g, entry.v, cg, biasRight);
            subgraphs[entry.v] = subgraphResult;
            if ((0, _chunkTZBO7MLIMjs.has_default)(subgraphResult, "barycenter")) mergeBarycenters(entry, subgraphResult);
        }
    });
    var entries = resolveConflicts(barycenters, cg);
    expandSubgraphs(entries, subgraphs);
    var result = sort(entries, biasRight);
    if (bl) {
        result.vs = (0, _chunkTZBO7MLIMjs.flatten_default)([
            bl,
            result.vs,
            br
        ]);
        if (g.predecessors(bl).length) {
            var blPred = g.node(g.predecessors(bl)[0]), brPred = g.node(g.predecessors(br)[0]);
            if (!(0, _chunkTZBO7MLIMjs.has_default)(result, "barycenter")) {
                result.barycenter = 0;
                result.weight = 0;
            }
            result.barycenter = (result.barycenter * result.weight + blPred.order + brPred.order) / (result.weight + 2);
            result.weight += 2;
        }
    }
    return result;
}
(0, _chunkDLQEHMXDMjs.__name)(sortSubgraph, "sortSubgraph");
function expandSubgraphs(entries, subgraphs) {
    (0, _chunkTZBO7MLIMjs.forEach_default)(entries, function(entry) {
        entry.vs = (0, _chunkTZBO7MLIMjs.flatten_default)(entry.vs.map(function(v) {
            if (subgraphs[v]) return subgraphs[v].vs;
            return v;
        }));
    });
}
(0, _chunkDLQEHMXDMjs.__name)(expandSubgraphs, "expandSubgraphs");
function mergeBarycenters(target, other) {
    if (!(0, _chunkTZBO7MLIMjs.isUndefined_default)(target.barycenter)) {
        target.barycenter = (target.barycenter * target.weight + other.barycenter * other.weight) / (target.weight + other.weight);
        target.weight += other.weight;
    } else {
        target.barycenter = other.barycenter;
        target.weight = other.weight;
    }
}
(0, _chunkDLQEHMXDMjs.__name)(mergeBarycenters, "mergeBarycenters");
// ../../node_modules/.pnpm/dagre-d3-es@7.0.10/node_modules/dagre-d3-es/src/dagre/order/index.js
function order(g) {
    var maxRank2 = maxRank(g), downLayerGraphs = buildLayerGraphs(g, (0, _chunkTZBO7MLIMjs.range_default)(1, maxRank2 + 1), "inEdges"), upLayerGraphs = buildLayerGraphs(g, (0, _chunkTZBO7MLIMjs.range_default)(maxRank2 - 1, -1, -1), "outEdges");
    var layering = initOrder(g);
    assignOrder(g, layering);
    var bestCC = Number.POSITIVE_INFINITY, best;
    for(var i = 0, lastBest = 0; lastBest < 4; ++i, ++lastBest){
        sweepLayerGraphs(i % 2 ? downLayerGraphs : upLayerGraphs, i % 4 >= 2);
        layering = buildLayerMatrix(g);
        var cc = crossCount(g, layering);
        if (cc < bestCC) {
            lastBest = 0;
            best = (0, _chunkTZBO7MLIMjs.cloneDeep_default)(layering);
            bestCC = cc;
        }
    }
    assignOrder(g, best);
}
(0, _chunkDLQEHMXDMjs.__name)(order, "order");
function buildLayerGraphs(g, ranks, relationship) {
    return (0, _chunkTZBO7MLIMjs.map_default)(ranks, function(rank2) {
        return buildLayerGraph(g, rank2, relationship);
    });
}
(0, _chunkDLQEHMXDMjs.__name)(buildLayerGraphs, "buildLayerGraphs");
function sweepLayerGraphs(layerGraphs, biasRight) {
    var cg = new (0, _chunkULVYQCHCMjs.Graph)();
    (0, _chunkTZBO7MLIMjs.forEach_default)(layerGraphs, function(lg) {
        var root = lg.graph().root;
        var sorted = sortSubgraph(lg, root, cg, biasRight);
        (0, _chunkTZBO7MLIMjs.forEach_default)(sorted.vs, function(v, i) {
            lg.node(v).order = i;
        });
        addSubgraphConstraints(lg, cg, sorted.vs);
    });
}
(0, _chunkDLQEHMXDMjs.__name)(sweepLayerGraphs, "sweepLayerGraphs");
function assignOrder(g, layering) {
    (0, _chunkTZBO7MLIMjs.forEach_default)(layering, function(layer) {
        (0, _chunkTZBO7MLIMjs.forEach_default)(layer, function(v, i) {
            g.node(v).order = i;
        });
    });
}
(0, _chunkDLQEHMXDMjs.__name)(assignOrder, "assignOrder");
// ../../node_modules/.pnpm/dagre-d3-es@7.0.10/node_modules/dagre-d3-es/src/dagre/parent-dummy-chains.js
function parentDummyChains(g) {
    var postorderNums = postorder2(g);
    (0, _chunkTZBO7MLIMjs.forEach_default)(g.graph().dummyChains, function(v) {
        var node = g.node(v);
        var edgeObj = node.edgeObj;
        var pathData = findPath(g, postorderNums, edgeObj.v, edgeObj.w);
        var path = pathData.path;
        var lca = pathData.lca;
        var pathIdx = 0;
        var pathV = path[pathIdx];
        var ascending = true;
        while(v !== edgeObj.w){
            node = g.node(v);
            if (ascending) {
                while((pathV = path[pathIdx]) !== lca && g.node(pathV).maxRank < node.rank)pathIdx++;
                if (pathV === lca) ascending = false;
            }
            if (!ascending) {
                while(pathIdx < path.length - 1 && g.node(pathV = path[pathIdx + 1]).minRank <= node.rank)pathIdx++;
                pathV = path[pathIdx];
            }
            g.setParent(v, pathV);
            v = g.successors(v)[0];
        }
    });
}
(0, _chunkDLQEHMXDMjs.__name)(parentDummyChains, "parentDummyChains");
function findPath(g, postorderNums, v, w) {
    var vPath = [];
    var wPath = [];
    var low = Math.min(postorderNums[v].low, postorderNums[w].low);
    var lim = Math.max(postorderNums[v].lim, postorderNums[w].lim);
    var parent;
    var lca;
    parent = v;
    do {
        parent = g.parent(parent);
        vPath.push(parent);
    }while (parent && (postorderNums[parent].low > low || lim > postorderNums[parent].lim));
    lca = parent;
    parent = w;
    while((parent = g.parent(parent)) !== lca)wPath.push(parent);
    return {
        path: vPath.concat(wPath.reverse()),
        lca
    };
}
(0, _chunkDLQEHMXDMjs.__name)(findPath, "findPath");
function postorder2(g) {
    var result = {};
    var lim = 0;
    function dfs3(v) {
        var low = lim;
        (0, _chunkTZBO7MLIMjs.forEach_default)(g.children(v), dfs3);
        result[v] = {
            low,
            lim: lim++
        };
    }
    (0, _chunkDLQEHMXDMjs.__name)(dfs3, "dfs");
    (0, _chunkTZBO7MLIMjs.forEach_default)(g.children(), dfs3);
    return result;
}
(0, _chunkDLQEHMXDMjs.__name)(postorder2, "postorder");
// ../../node_modules/.pnpm/dagre-d3-es@7.0.10/node_modules/dagre-d3-es/src/dagre/position/bk.js
function findType1Conflicts(g, layering) {
    var conflicts = {};
    function visitLayer(prevLayer, layer) {
        var k0 = 0, scanPos = 0, prevLayerLength = prevLayer.length, lastNode = (0, _chunkTZBO7MLIMjs.last_default)(layer);
        (0, _chunkTZBO7MLIMjs.forEach_default)(layer, function(v, i) {
            var w = findOtherInnerSegmentNode(g, v), k1 = w ? g.node(w).order : prevLayerLength;
            if (w || v === lastNode) {
                (0, _chunkTZBO7MLIMjs.forEach_default)(layer.slice(scanPos, i + 1), function(scanNode) {
                    (0, _chunkTZBO7MLIMjs.forEach_default)(g.predecessors(scanNode), function(u) {
                        var uLabel = g.node(u), uPos = uLabel.order;
                        if ((uPos < k0 || k1 < uPos) && !(uLabel.dummy && g.node(scanNode).dummy)) addConflict(conflicts, u, scanNode);
                    });
                });
                scanPos = i + 1;
                k0 = k1;
            }
        });
        return layer;
    }
    (0, _chunkDLQEHMXDMjs.__name)(visitLayer, "visitLayer");
    (0, _chunkTZBO7MLIMjs.reduce_default)(layering, visitLayer);
    return conflicts;
}
(0, _chunkDLQEHMXDMjs.__name)(findType1Conflicts, "findType1Conflicts");
function findType2Conflicts(g, layering) {
    var conflicts = {};
    function scan(south, southPos, southEnd, prevNorthBorder, nextNorthBorder) {
        var v;
        (0, _chunkTZBO7MLIMjs.forEach_default)((0, _chunkTZBO7MLIMjs.range_default)(southPos, southEnd), function(i) {
            v = south[i];
            if (g.node(v).dummy) (0, _chunkTZBO7MLIMjs.forEach_default)(g.predecessors(v), function(u) {
                var uNode = g.node(u);
                if (uNode.dummy && (uNode.order < prevNorthBorder || uNode.order > nextNorthBorder)) addConflict(conflicts, u, v);
            });
        });
    }
    (0, _chunkDLQEHMXDMjs.__name)(scan, "scan");
    function visitLayer(north, south) {
        var prevNorthPos = -1, nextNorthPos, southPos = 0;
        (0, _chunkTZBO7MLIMjs.forEach_default)(south, function(v, southLookahead) {
            if (g.node(v).dummy === "border") {
                var predecessors = g.predecessors(v);
                if (predecessors.length) {
                    nextNorthPos = g.node(predecessors[0]).order;
                    scan(south, southPos, southLookahead, prevNorthPos, nextNorthPos);
                    southPos = southLookahead;
                    prevNorthPos = nextNorthPos;
                }
            }
            scan(south, southPos, south.length, nextNorthPos, north.length);
        });
        return south;
    }
    (0, _chunkDLQEHMXDMjs.__name)(visitLayer, "visitLayer");
    (0, _chunkTZBO7MLIMjs.reduce_default)(layering, visitLayer);
    return conflicts;
}
(0, _chunkDLQEHMXDMjs.__name)(findType2Conflicts, "findType2Conflicts");
function findOtherInnerSegmentNode(g, v) {
    if (g.node(v).dummy) return (0, _chunkTZBO7MLIMjs.find_default)(g.predecessors(v), function(u) {
        return g.node(u).dummy;
    });
}
(0, _chunkDLQEHMXDMjs.__name)(findOtherInnerSegmentNode, "findOtherInnerSegmentNode");
function addConflict(conflicts, v, w) {
    if (v > w) {
        var tmp = v;
        v = w;
        w = tmp;
    }
    var conflictsV = conflicts[v];
    if (!conflictsV) conflicts[v] = conflictsV = {};
    conflictsV[w] = true;
}
(0, _chunkDLQEHMXDMjs.__name)(addConflict, "addConflict");
function hasConflict(conflicts, v, w) {
    if (v > w) {
        var tmp = v;
        v = w;
        w = tmp;
    }
    return (0, _chunkTZBO7MLIMjs.has_default)(conflicts[v], w);
}
(0, _chunkDLQEHMXDMjs.__name)(hasConflict, "hasConflict");
function verticalAlignment(g, layering, conflicts, neighborFn) {
    var root = {}, align = {}, pos = {};
    (0, _chunkTZBO7MLIMjs.forEach_default)(layering, function(layer) {
        (0, _chunkTZBO7MLIMjs.forEach_default)(layer, function(v, order2) {
            root[v] = v;
            align[v] = v;
            pos[v] = order2;
        });
    });
    (0, _chunkTZBO7MLIMjs.forEach_default)(layering, function(layer) {
        var prevIdx = -1;
        (0, _chunkTZBO7MLIMjs.forEach_default)(layer, function(v) {
            var ws = neighborFn(v);
            if (ws.length) {
                ws = (0, _chunkTZBO7MLIMjs.sortBy_default)(ws, function(w2) {
                    return pos[w2];
                });
                var mp = (ws.length - 1) / 2;
                for(var i = Math.floor(mp), il = Math.ceil(mp); i <= il; ++i){
                    var w = ws[i];
                    if (align[v] === v && prevIdx < pos[w] && !hasConflict(conflicts, v, w)) {
                        align[w] = v;
                        align[v] = root[v] = root[w];
                        prevIdx = pos[w];
                    }
                }
            }
        });
    });
    return {
        root,
        align
    };
}
(0, _chunkDLQEHMXDMjs.__name)(verticalAlignment, "verticalAlignment");
function horizontalCompaction(g, layering, root, align, reverseSep) {
    var xs = {}, blockG = buildBlockGraph(g, layering, root, reverseSep), borderType = reverseSep ? "borderLeft" : "borderRight";
    function iterate(setXsFunc, nextNodesFunc) {
        var stack = blockG.nodes();
        var elem = stack.pop();
        var visited = {};
        while(elem){
            if (visited[elem]) setXsFunc(elem);
            else {
                visited[elem] = true;
                stack.push(elem);
                stack = stack.concat(nextNodesFunc(elem));
            }
            elem = stack.pop();
        }
    }
    (0, _chunkDLQEHMXDMjs.__name)(iterate, "iterate");
    function pass1(elem) {
        xs[elem] = blockG.inEdges(elem).reduce(function(acc, e) {
            return Math.max(acc, xs[e.v] + blockG.edge(e));
        }, 0);
    }
    (0, _chunkDLQEHMXDMjs.__name)(pass1, "pass1");
    function pass2(elem) {
        var min = blockG.outEdges(elem).reduce(function(acc, e) {
            return Math.min(acc, xs[e.w] - blockG.edge(e));
        }, Number.POSITIVE_INFINITY);
        var node = g.node(elem);
        if (min !== Number.POSITIVE_INFINITY && node.borderType !== borderType) xs[elem] = Math.max(xs[elem], min);
    }
    (0, _chunkDLQEHMXDMjs.__name)(pass2, "pass2");
    iterate(pass1, blockG.predecessors.bind(blockG));
    iterate(pass2, blockG.successors.bind(blockG));
    (0, _chunkTZBO7MLIMjs.forEach_default)(align, function(v) {
        xs[v] = xs[root[v]];
    });
    return xs;
}
(0, _chunkDLQEHMXDMjs.__name)(horizontalCompaction, "horizontalCompaction");
function buildBlockGraph(g, layering, root, reverseSep) {
    var blockGraph = new (0, _chunkULVYQCHCMjs.Graph)(), graphLabel = g.graph(), sepFn = sep(graphLabel.nodesep, graphLabel.edgesep, reverseSep);
    (0, _chunkTZBO7MLIMjs.forEach_default)(layering, function(layer) {
        var u;
        (0, _chunkTZBO7MLIMjs.forEach_default)(layer, function(v) {
            var vRoot = root[v];
            blockGraph.setNode(vRoot);
            if (u) {
                var uRoot = root[u], prevMax = blockGraph.edge(uRoot, vRoot);
                blockGraph.setEdge(uRoot, vRoot, Math.max(sepFn(g, v, u), prevMax || 0));
            }
            u = v;
        });
    });
    return blockGraph;
}
(0, _chunkDLQEHMXDMjs.__name)(buildBlockGraph, "buildBlockGraph");
function findSmallestWidthAlignment(g, xss) {
    return (0, _chunkTZBO7MLIMjs.minBy_default)((0, _chunkTZBO7MLIMjs.values_default)(xss), function(xs) {
        var max = Number.NEGATIVE_INFINITY;
        var min = Number.POSITIVE_INFINITY;
        (0, _chunkTZBO7MLIMjs.forIn_default)(xs, function(x, v) {
            var halfWidth = width(g, v) / 2;
            max = Math.max(x + halfWidth, max);
            min = Math.min(x - halfWidth, min);
        });
        return max - min;
    });
}
(0, _chunkDLQEHMXDMjs.__name)(findSmallestWidthAlignment, "findSmallestWidthAlignment");
function alignCoordinates(xss, alignTo) {
    var alignToVals = (0, _chunkTZBO7MLIMjs.values_default)(alignTo), alignToMin = (0, _chunkTZBO7MLIMjs.min_default)(alignToVals), alignToMax = (0, _chunkTZBO7MLIMjs.max_default)(alignToVals);
    (0, _chunkTZBO7MLIMjs.forEach_default)([
        "u",
        "d"
    ], function(vert) {
        (0, _chunkTZBO7MLIMjs.forEach_default)([
            "l",
            "r"
        ], function(horiz) {
            var alignment = vert + horiz, xs = xss[alignment], delta;
            if (xs === alignTo) return;
            var xsVals = (0, _chunkTZBO7MLIMjs.values_default)(xs);
            delta = horiz === "l" ? alignToMin - (0, _chunkTZBO7MLIMjs.min_default)(xsVals) : alignToMax - (0, _chunkTZBO7MLIMjs.max_default)(xsVals);
            if (delta) xss[alignment] = (0, _chunkTZBO7MLIMjs.mapValues_default)(xs, function(x) {
                return x + delta;
            });
        });
    });
}
(0, _chunkDLQEHMXDMjs.__name)(alignCoordinates, "alignCoordinates");
function balance(xss, align) {
    return (0, _chunkTZBO7MLIMjs.mapValues_default)(xss.ul, function(ignore, v) {
        if (align) return xss[align.toLowerCase()][v];
        else {
            var xs = (0, _chunkTZBO7MLIMjs.sortBy_default)((0, _chunkTZBO7MLIMjs.map_default)(xss, v));
            return (xs[1] + xs[2]) / 2;
        }
    });
}
(0, _chunkDLQEHMXDMjs.__name)(balance, "balance");
function positionX(g) {
    var layering = buildLayerMatrix(g);
    var conflicts = (0, _chunkHD3LK5B5Mjs.merge_default)(findType1Conflicts(g, layering), findType2Conflicts(g, layering));
    var xss = {};
    var adjustedLayering;
    (0, _chunkTZBO7MLIMjs.forEach_default)([
        "u",
        "d"
    ], function(vert) {
        adjustedLayering = vert === "u" ? layering : (0, _chunkTZBO7MLIMjs.values_default)(layering).reverse();
        (0, _chunkTZBO7MLIMjs.forEach_default)([
            "l",
            "r"
        ], function(horiz) {
            if (horiz === "r") adjustedLayering = (0, _chunkTZBO7MLIMjs.map_default)(adjustedLayering, function(inner) {
                return (0, _chunkTZBO7MLIMjs.values_default)(inner).reverse();
            });
            var neighborFn = (vert === "u" ? g.predecessors : g.successors).bind(g);
            var align = verticalAlignment(g, adjustedLayering, conflicts, neighborFn);
            var xs = horizontalCompaction(g, adjustedLayering, align.root, align.align, horiz === "r");
            if (horiz === "r") xs = (0, _chunkTZBO7MLIMjs.mapValues_default)(xs, function(x) {
                return -x;
            });
            xss[vert + horiz] = xs;
        });
    });
    var smallestWidth = findSmallestWidthAlignment(g, xss);
    alignCoordinates(xss, smallestWidth);
    return balance(xss, g.graph().align);
}
(0, _chunkDLQEHMXDMjs.__name)(positionX, "positionX");
function sep(nodeSep, edgeSep, reverseSep) {
    return function(g, v, w) {
        var vLabel = g.node(v);
        var wLabel = g.node(w);
        var sum = 0;
        var delta;
        sum += vLabel.width / 2;
        if ((0, _chunkTZBO7MLIMjs.has_default)(vLabel, "labelpos")) switch(vLabel.labelpos.toLowerCase()){
            case "l":
                delta = -vLabel.width / 2;
                break;
            case "r":
                delta = vLabel.width / 2;
                break;
        }
        if (delta) sum += reverseSep ? delta : -delta;
        delta = 0;
        sum += (vLabel.dummy ? edgeSep : nodeSep) / 2;
        sum += (wLabel.dummy ? edgeSep : nodeSep) / 2;
        sum += wLabel.width / 2;
        if ((0, _chunkTZBO7MLIMjs.has_default)(wLabel, "labelpos")) switch(wLabel.labelpos.toLowerCase()){
            case "l":
                delta = wLabel.width / 2;
                break;
            case "r":
                delta = -wLabel.width / 2;
                break;
        }
        if (delta) sum += reverseSep ? delta : -delta;
        delta = 0;
        return sum;
    };
}
(0, _chunkDLQEHMXDMjs.__name)(sep, "sep");
function width(g, v) {
    return g.node(v).width;
}
(0, _chunkDLQEHMXDMjs.__name)(width, "width");
// ../../node_modules/.pnpm/dagre-d3-es@7.0.10/node_modules/dagre-d3-es/src/dagre/position/index.js
function position(g) {
    g = asNonCompoundGraph(g);
    positionY(g);
    (0, _chunkTZBO7MLIMjs.forOwn_default)(positionX(g), function(x, v) {
        g.node(v).x = x;
    });
}
(0, _chunkDLQEHMXDMjs.__name)(position, "position");
function positionY(g) {
    var layering = buildLayerMatrix(g);
    var rankSep = g.graph().ranksep;
    var prevY = 0;
    (0, _chunkTZBO7MLIMjs.forEach_default)(layering, function(layer) {
        var maxHeight = (0, _chunkTZBO7MLIMjs.max_default)((0, _chunkTZBO7MLIMjs.map_default)(layer, function(v) {
            return g.node(v).height;
        }));
        (0, _chunkTZBO7MLIMjs.forEach_default)(layer, function(v) {
            g.node(v).y = prevY + maxHeight / 2;
        });
        prevY += maxHeight + rankSep;
    });
}
(0, _chunkDLQEHMXDMjs.__name)(positionY, "positionY");
// ../../node_modules/.pnpm/dagre-d3-es@7.0.10/node_modules/dagre-d3-es/src/dagre/layout.js
function layout(g, opts) {
    var time2 = opts && opts.debugTiming ? time : notime;
    time2("layout", function() {
        var layoutGraph = time2("  buildLayoutGraph", function() {
            return buildLayoutGraph(g);
        });
        time2("  runLayout", function() {
            runLayout(layoutGraph, time2);
        });
        time2("  updateInputGraph", function() {
            updateInputGraph(g, layoutGraph);
        });
    });
}
(0, _chunkDLQEHMXDMjs.__name)(layout, "layout");
function runLayout(g, time2) {
    time2("    makeSpaceForEdgeLabels", function() {
        makeSpaceForEdgeLabels(g);
    });
    time2("    removeSelfEdges", function() {
        removeSelfEdges(g);
    });
    time2("    acyclic", function() {
        run(g);
    });
    time2("    nestingGraph.run", function() {
        run3(g);
    });
    time2("    rank", function() {
        rank(asNonCompoundGraph(g));
    });
    time2("    injectEdgeLabelProxies", function() {
        injectEdgeLabelProxies(g);
    });
    time2("    removeEmptyRanks", function() {
        removeEmptyRanks(g);
    });
    time2("    nestingGraph.cleanup", function() {
        cleanup(g);
    });
    time2("    normalizeRanks", function() {
        normalizeRanks(g);
    });
    time2("    assignRankMinMax", function() {
        assignRankMinMax(g);
    });
    time2("    removeEdgeLabelProxies", function() {
        removeEdgeLabelProxies(g);
    });
    time2("    normalize.run", function() {
        run2(g);
    });
    time2("    parentDummyChains", function() {
        parentDummyChains(g);
    });
    time2("    addBorderSegments", function() {
        addBorderSegments(g);
    });
    time2("    order", function() {
        order(g);
    });
    time2("    insertSelfEdges", function() {
        insertSelfEdges(g);
    });
    time2("    adjustCoordinateSystem", function() {
        adjust(g);
    });
    time2("    position", function() {
        position(g);
    });
    time2("    positionSelfEdges", function() {
        positionSelfEdges(g);
    });
    time2("    removeBorderNodes", function() {
        removeBorderNodes(g);
    });
    time2("    normalize.undo", function() {
        undo3(g);
    });
    time2("    fixupEdgeLabelCoords", function() {
        fixupEdgeLabelCoords(g);
    });
    time2("    undoCoordinateSystem", function() {
        undo(g);
    });
    time2("    translateGraph", function() {
        translateGraph(g);
    });
    time2("    assignNodeIntersects", function() {
        assignNodeIntersects(g);
    });
    time2("    reversePoints", function() {
        reversePointsForReversedEdges(g);
    });
    time2("    acyclic.undo", function() {
        undo2(g);
    });
}
(0, _chunkDLQEHMXDMjs.__name)(runLayout, "runLayout");
function updateInputGraph(inputGraph, layoutGraph) {
    (0, _chunkTZBO7MLIMjs.forEach_default)(inputGraph.nodes(), function(v) {
        var inputLabel = inputGraph.node(v);
        var layoutLabel = layoutGraph.node(v);
        if (inputLabel) {
            inputLabel.x = layoutLabel.x;
            inputLabel.y = layoutLabel.y;
            if (layoutGraph.children(v).length) {
                inputLabel.width = layoutLabel.width;
                inputLabel.height = layoutLabel.height;
            }
        }
    });
    (0, _chunkTZBO7MLIMjs.forEach_default)(inputGraph.edges(), function(e) {
        var inputLabel = inputGraph.edge(e);
        var layoutLabel = layoutGraph.edge(e);
        inputLabel.points = layoutLabel.points;
        if ((0, _chunkTZBO7MLIMjs.has_default)(layoutLabel, "x")) {
            inputLabel.x = layoutLabel.x;
            inputLabel.y = layoutLabel.y;
        }
    });
    inputGraph.graph().width = layoutGraph.graph().width;
    inputGraph.graph().height = layoutGraph.graph().height;
}
(0, _chunkDLQEHMXDMjs.__name)(updateInputGraph, "updateInputGraph");
var graphNumAttrs = [
    "nodesep",
    "edgesep",
    "ranksep",
    "marginx",
    "marginy"
];
var graphDefaults = {
    ranksep: 50,
    edgesep: 20,
    nodesep: 50,
    rankdir: "tb"
};
var graphAttrs = [
    "acyclicer",
    "ranker",
    "rankdir",
    "align"
];
var nodeNumAttrs = [
    "width",
    "height"
];
var nodeDefaults = {
    width: 0,
    height: 0
};
var edgeNumAttrs = [
    "minlen",
    "weight",
    "width",
    "height",
    "labeloffset"
];
var edgeDefaults = {
    minlen: 1,
    weight: 1,
    width: 0,
    height: 0,
    labeloffset: 10,
    labelpos: "r"
};
var edgeAttrs = [
    "labelpos"
];
function buildLayoutGraph(inputGraph) {
    var g = new (0, _chunkULVYQCHCMjs.Graph)({
        multigraph: true,
        compound: true
    });
    var graph = canonicalize(inputGraph.graph());
    g.setGraph((0, _chunkHD3LK5B5Mjs.merge_default)({}, graphDefaults, selectNumberAttrs(graph, graphNumAttrs), (0, _chunkTZBO7MLIMjs.pick_default)(graph, graphAttrs)));
    (0, _chunkTZBO7MLIMjs.forEach_default)(inputGraph.nodes(), function(v) {
        var node = canonicalize(inputGraph.node(v));
        g.setNode(v, (0, _chunkTZBO7MLIMjs.defaults_default)(selectNumberAttrs(node, nodeNumAttrs), nodeDefaults));
        g.setParent(v, inputGraph.parent(v));
    });
    (0, _chunkTZBO7MLIMjs.forEach_default)(inputGraph.edges(), function(e) {
        var edge = canonicalize(inputGraph.edge(e));
        g.setEdge(e, (0, _chunkHD3LK5B5Mjs.merge_default)({}, edgeDefaults, selectNumberAttrs(edge, edgeNumAttrs), (0, _chunkTZBO7MLIMjs.pick_default)(edge, edgeAttrs)));
    });
    return g;
}
(0, _chunkDLQEHMXDMjs.__name)(buildLayoutGraph, "buildLayoutGraph");
function makeSpaceForEdgeLabels(g) {
    var graph = g.graph();
    graph.ranksep /= 2;
    (0, _chunkTZBO7MLIMjs.forEach_default)(g.edges(), function(e) {
        var edge = g.edge(e);
        edge.minlen *= 2;
        if (edge.labelpos.toLowerCase() !== "c") {
            if (graph.rankdir === "TB" || graph.rankdir === "BT") edge.width += edge.labeloffset;
            else edge.height += edge.labeloffset;
        }
    });
}
(0, _chunkDLQEHMXDMjs.__name)(makeSpaceForEdgeLabels, "makeSpaceForEdgeLabels");
function injectEdgeLabelProxies(g) {
    (0, _chunkTZBO7MLIMjs.forEach_default)(g.edges(), function(e) {
        var edge = g.edge(e);
        if (edge.width && edge.height) {
            var v = g.node(e.v);
            var w = g.node(e.w);
            var label = {
                rank: (w.rank - v.rank) / 2 + v.rank,
                e
            };
            addDummyNode(g, "edge-proxy", label, "_ep");
        }
    });
}
(0, _chunkDLQEHMXDMjs.__name)(injectEdgeLabelProxies, "injectEdgeLabelProxies");
function assignRankMinMax(g) {
    var maxRank2 = 0;
    (0, _chunkTZBO7MLIMjs.forEach_default)(g.nodes(), function(v) {
        var node = g.node(v);
        if (node.borderTop) {
            node.minRank = g.node(node.borderTop).rank;
            node.maxRank = g.node(node.borderBottom).rank;
            maxRank2 = (0, _chunkTZBO7MLIMjs.max_default)(maxRank2, node.maxRank);
        }
    });
    g.graph().maxRank = maxRank2;
}
(0, _chunkDLQEHMXDMjs.__name)(assignRankMinMax, "assignRankMinMax");
function removeEdgeLabelProxies(g) {
    (0, _chunkTZBO7MLIMjs.forEach_default)(g.nodes(), function(v) {
        var node = g.node(v);
        if (node.dummy === "edge-proxy") {
            g.edge(node.e).labelRank = node.rank;
            g.removeNode(v);
        }
    });
}
(0, _chunkDLQEHMXDMjs.__name)(removeEdgeLabelProxies, "removeEdgeLabelProxies");
function translateGraph(g) {
    var minX = Number.POSITIVE_INFINITY;
    var maxX = 0;
    var minY = Number.POSITIVE_INFINITY;
    var maxY = 0;
    var graphLabel = g.graph();
    var marginX = graphLabel.marginx || 0;
    var marginY = graphLabel.marginy || 0;
    function getExtremes(attrs) {
        var x = attrs.x;
        var y = attrs.y;
        var w = attrs.width;
        var h = attrs.height;
        minX = Math.min(minX, x - w / 2);
        maxX = Math.max(maxX, x + w / 2);
        minY = Math.min(minY, y - h / 2);
        maxY = Math.max(maxY, y + h / 2);
    }
    (0, _chunkDLQEHMXDMjs.__name)(getExtremes, "getExtremes");
    (0, _chunkTZBO7MLIMjs.forEach_default)(g.nodes(), function(v) {
        getExtremes(g.node(v));
    });
    (0, _chunkTZBO7MLIMjs.forEach_default)(g.edges(), function(e) {
        var edge = g.edge(e);
        if ((0, _chunkTZBO7MLIMjs.has_default)(edge, "x")) getExtremes(edge);
    });
    minX -= marginX;
    minY -= marginY;
    (0, _chunkTZBO7MLIMjs.forEach_default)(g.nodes(), function(v) {
        var node = g.node(v);
        node.x -= minX;
        node.y -= minY;
    });
    (0, _chunkTZBO7MLIMjs.forEach_default)(g.edges(), function(e) {
        var edge = g.edge(e);
        (0, _chunkTZBO7MLIMjs.forEach_default)(edge.points, function(p) {
            p.x -= minX;
            p.y -= minY;
        });
        if ((0, _chunkTZBO7MLIMjs.has_default)(edge, "x")) edge.x -= minX;
        if ((0, _chunkTZBO7MLIMjs.has_default)(edge, "y")) edge.y -= minY;
    });
    graphLabel.width = maxX - minX + marginX;
    graphLabel.height = maxY - minY + marginY;
}
(0, _chunkDLQEHMXDMjs.__name)(translateGraph, "translateGraph");
function assignNodeIntersects(g) {
    (0, _chunkTZBO7MLIMjs.forEach_default)(g.edges(), function(e) {
        var edge = g.edge(e);
        var nodeV = g.node(e.v);
        var nodeW = g.node(e.w);
        var p1, p2;
        if (!edge.points) {
            edge.points = [];
            p1 = nodeW;
            p2 = nodeV;
        } else {
            p1 = edge.points[0];
            p2 = edge.points[edge.points.length - 1];
        }
        edge.points.unshift(intersectRect(nodeV, p1));
        edge.points.push(intersectRect(nodeW, p2));
    });
}
(0, _chunkDLQEHMXDMjs.__name)(assignNodeIntersects, "assignNodeIntersects");
function fixupEdgeLabelCoords(g) {
    (0, _chunkTZBO7MLIMjs.forEach_default)(g.edges(), function(e) {
        var edge = g.edge(e);
        if ((0, _chunkTZBO7MLIMjs.has_default)(edge, "x")) {
            if (edge.labelpos === "l" || edge.labelpos === "r") edge.width -= edge.labeloffset;
            switch(edge.labelpos){
                case "l":
                    edge.x -= edge.width / 2 + edge.labeloffset;
                    break;
                case "r":
                    edge.x += edge.width / 2 + edge.labeloffset;
                    break;
            }
        }
    });
}
(0, _chunkDLQEHMXDMjs.__name)(fixupEdgeLabelCoords, "fixupEdgeLabelCoords");
function reversePointsForReversedEdges(g) {
    (0, _chunkTZBO7MLIMjs.forEach_default)(g.edges(), function(e) {
        var edge = g.edge(e);
        if (edge.reversed) edge.points.reverse();
    });
}
(0, _chunkDLQEHMXDMjs.__name)(reversePointsForReversedEdges, "reversePointsForReversedEdges");
function removeBorderNodes(g) {
    (0, _chunkTZBO7MLIMjs.forEach_default)(g.nodes(), function(v) {
        if (g.children(v).length) {
            var node = g.node(v);
            var t = g.node(node.borderTop);
            var b = g.node(node.borderBottom);
            var l = g.node((0, _chunkTZBO7MLIMjs.last_default)(node.borderLeft));
            var r = g.node((0, _chunkTZBO7MLIMjs.last_default)(node.borderRight));
            node.width = Math.abs(r.x - l.x);
            node.height = Math.abs(b.y - t.y);
            node.x = l.x + node.width / 2;
            node.y = t.y + node.height / 2;
        }
    });
    (0, _chunkTZBO7MLIMjs.forEach_default)(g.nodes(), function(v) {
        if (g.node(v).dummy === "border") g.removeNode(v);
    });
}
(0, _chunkDLQEHMXDMjs.__name)(removeBorderNodes, "removeBorderNodes");
function removeSelfEdges(g) {
    (0, _chunkTZBO7MLIMjs.forEach_default)(g.edges(), function(e) {
        if (e.v === e.w) {
            var node = g.node(e.v);
            if (!node.selfEdges) node.selfEdges = [];
            node.selfEdges.push({
                e,
                label: g.edge(e)
            });
            g.removeEdge(e);
        }
    });
}
(0, _chunkDLQEHMXDMjs.__name)(removeSelfEdges, "removeSelfEdges");
function insertSelfEdges(g) {
    var layers = buildLayerMatrix(g);
    (0, _chunkTZBO7MLIMjs.forEach_default)(layers, function(layer) {
        var orderShift = 0;
        (0, _chunkTZBO7MLIMjs.forEach_default)(layer, function(v, i) {
            var node = g.node(v);
            node.order = i + orderShift;
            (0, _chunkTZBO7MLIMjs.forEach_default)(node.selfEdges, function(selfEdge) {
                addDummyNode(g, "selfedge", {
                    width: selfEdge.label.width,
                    height: selfEdge.label.height,
                    rank: node.rank,
                    order: i + ++orderShift,
                    e: selfEdge.e,
                    label: selfEdge.label
                }, "_se");
            });
            delete node.selfEdges;
        });
    });
}
(0, _chunkDLQEHMXDMjs.__name)(insertSelfEdges, "insertSelfEdges");
function positionSelfEdges(g) {
    (0, _chunkTZBO7MLIMjs.forEach_default)(g.nodes(), function(v) {
        var node = g.node(v);
        if (node.dummy === "selfedge") {
            var selfNode = g.node(node.e.v);
            var x = selfNode.x + selfNode.width / 2;
            var y = selfNode.y;
            var dx = node.x - x;
            var dy = selfNode.height / 2;
            g.setEdge(node.e, node.label);
            g.removeNode(v);
            node.label.points = [
                {
                    x: x + 2 * dx / 3,
                    y: y - dy
                },
                {
                    x: x + 5 * dx / 6,
                    y: y - dy
                },
                {
                    x: x + dx,
                    y
                },
                {
                    x: x + 5 * dx / 6,
                    y: y + dy
                },
                {
                    x: x + 2 * dx / 3,
                    y: y + dy
                }
            ];
            node.label.x = node.x;
            node.label.y = node.y;
        }
    });
}
(0, _chunkDLQEHMXDMjs.__name)(positionSelfEdges, "positionSelfEdges");
function selectNumberAttrs(obj, attrs) {
    return (0, _chunkTZBO7MLIMjs.mapValues_default)((0, _chunkTZBO7MLIMjs.pick_default)(obj, attrs), Number);
}
(0, _chunkDLQEHMXDMjs.__name)(selectNumberAttrs, "selectNumberAttrs");
function canonicalize(attrs) {
    var newAttrs = {};
    (0, _chunkTZBO7MLIMjs.forEach_default)(attrs, function(v, k) {
        newAttrs[k.toLowerCase()] = v;
    });
    return newAttrs;
}
(0, _chunkDLQEHMXDMjs.__name)(canonicalize, "canonicalize");

},{"./chunk-ULVYQCHC.mjs":"h2Yj3","./chunk-TZBO7MLI.mjs":"fY3tK","./chunk-HD3LK5B5.mjs":"lpo2u","./chunk-DLQEHMXD.mjs":"h5YRf","@parcel/transformer-js/src/esmodule-helpers.js":"1cdyk"}]},["pLKtn"], null, "parcelRequire6955", {})

//# sourceMappingURL=dagre-YJZDXZCU.7ed3389f.js.map
