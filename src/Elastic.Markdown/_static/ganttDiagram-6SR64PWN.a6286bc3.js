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
})({"jL2qi":[function(require,module,exports,__globalThis) {
var global = arguments[3];
var HMR_HOST = null;
var HMR_PORT = 1234;
var HMR_SERVER_PORT = 1234;
var HMR_SECURE = false;
var HMR_ENV_HASH = "febfef3c5b467b16";
var HMR_USE_SSE = false;
module.bundle.HMR_BUNDLE_ID = "b9fcebeaa6286bc3";
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

},{}],"58bFi":[function(require,module,exports,__globalThis) {
var parcelHelpers = require("@parcel/transformer-js/src/esmodule-helpers.js");
parcelHelpers.defineInteropFlag(exports);
parcelHelpers.export(exports, "diagram", ()=>oi);
var _chunkAC3VT7B7Mjs = require("./chunk-AC3VT7B7.mjs");
var _chunkTI4EEUUGMjs = require("./chunk-TI4EEUUG.mjs");
var _chunkNQURTBEVMjs = require("./chunk-NQURTBEV.mjs");
var _chunk6BY5RJGCMjs = require("./chunk-6BY5RJGC.mjs");
var _chunkGTKDMUJJMjs = require("./chunk-GTKDMUJJ.mjs");
var Et = (0, _chunkGTKDMUJJMjs.b)((Ve, Oe)=>{
    "use strict";
    (function(e, t) {
        typeof Ve == "object" && typeof Oe < "u" ? Oe.exports = t() : typeof define == "function" && define.amd ? define(t) : (e = typeof globalThis < "u" ? globalThis : e || self).dayjs_plugin_isoWeek = t();
    })(Ve, function() {
        "use strict";
        var e = "day";
        return function(t, r, i) {
            var a = (0, _chunkGTKDMUJJMjs.a)(function(T) {
                return T.add(4 - T.isoWeekday(), e);
            }, "a"), f = r.prototype;
            f.isoWeekYear = function() {
                return a(this).year();
            }, f.isoWeek = function(T) {
                if (!this.$utils().u(T)) return this.add(7 * (T - this.isoWeek()), e);
                var W, A, E, F, j = a(this), y = (W = this.isoWeekYear(), A = this.$u, E = (A ? i.utc : i)().year(W).startOf("year"), F = 4 - E.isoWeekday(), E.isoWeekday() > 4 && (F += 7), E.add(F, e));
                return j.diff(y, "week") + 1;
            }, f.isoWeekday = function(T) {
                return this.$utils().u(T) ? this.day() || 7 : this.day(this.day() % 7 ? T : T - 7);
            };
            var m = f.startOf;
            f.startOf = function(T, W) {
                var A = this.$utils(), E = !!A.u(W) || W;
                return A.p(T) === "isoweek" ? E ? this.date(this.date() - (this.isoWeekday() - 1)).startOf("day") : this.date(this.date() - 1 - (this.isoWeekday() - 1) + 7).endOf("day") : m.bind(this)(T, W);
            };
        };
    });
});
var Mt = (0, _chunkGTKDMUJJMjs.b)((Pe, ze)=>{
    "use strict";
    (function(e, t) {
        typeof Pe == "object" && typeof ze < "u" ? ze.exports = t() : typeof define == "function" && define.amd ? define(t) : (e = typeof globalThis < "u" ? globalThis : e || self).dayjs_plugin_customParseFormat = t();
    })(Pe, function() {
        "use strict";
        var e = {
            LTS: "h:mm:ss A",
            LT: "h:mm A",
            L: "MM/DD/YYYY",
            LL: "MMMM D, YYYY",
            LLL: "MMMM D, YYYY h:mm A",
            LLLL: "dddd, MMMM D, YYYY h:mm A"
        }, t = /(\[[^[]*\])|([-_:/.,()\s]+)|(A|a|YYYY|YY?|MM?M?M?|Do|DD?|hh?|HH?|mm?|ss?|S{1,3}|z|ZZ?)/g, r = /\d\d/, i = /\d\d?/, a = /\d*[^-_:/,()\s\d]+/, f = {}, m = (0, _chunkGTKDMUJJMjs.a)(function(y) {
            return (y = +y) + (y > 68 ? 1900 : 2e3);
        }, "s"), T = (0, _chunkGTKDMUJJMjs.a)(function(y) {
            return function(_) {
                this[y] = +_;
            };
        }, "a"), W = [
            /[+-]\d\d:?(\d\d)?|Z/,
            function(y) {
                (this.zone || (this.zone = {})).offset = function(_) {
                    if (!_ || _ === "Z") return 0;
                    var g = _.match(/([+-]|\d\d)/g), I = 60 * g[1] + (+g[2] || 0);
                    return I === 0 ? 0 : g[0] === "+" ? -I : I;
                }(y);
            }
        ], A = (0, _chunkGTKDMUJJMjs.a)(function(y) {
            var _ = f[y];
            return _ && (_.indexOf ? _ : _.s.concat(_.f));
        }, "h"), E = (0, _chunkGTKDMUJJMjs.a)(function(y, _) {
            var g, I = f.meridiem;
            if (I) {
                for(var z = 1; z <= 24; z += 1)if (y.indexOf(I(z, 0, _)) > -1) {
                    g = z > 12;
                    break;
                }
            } else g = y === (_ ? "pm" : "PM");
            return g;
        }, "u"), F = {
            A: [
                a,
                function(y) {
                    this.afternoon = E(y, !1);
                }
            ],
            a: [
                a,
                function(y) {
                    this.afternoon = E(y, !0);
                }
            ],
            S: [
                /\d/,
                function(y) {
                    this.milliseconds = 100 * +y;
                }
            ],
            SS: [
                r,
                function(y) {
                    this.milliseconds = 10 * +y;
                }
            ],
            SSS: [
                /\d{3}/,
                function(y) {
                    this.milliseconds = +y;
                }
            ],
            s: [
                i,
                T("seconds")
            ],
            ss: [
                i,
                T("seconds")
            ],
            m: [
                i,
                T("minutes")
            ],
            mm: [
                i,
                T("minutes")
            ],
            H: [
                i,
                T("hours")
            ],
            h: [
                i,
                T("hours")
            ],
            HH: [
                i,
                T("hours")
            ],
            hh: [
                i,
                T("hours")
            ],
            D: [
                i,
                T("day")
            ],
            DD: [
                r,
                T("day")
            ],
            Do: [
                a,
                function(y) {
                    var _ = f.ordinal, g = y.match(/\d+/);
                    if (this.day = g[0], _) for(var I = 1; I <= 31; I += 1)_(I).replace(/\[|\]/g, "") === y && (this.day = I);
                }
            ],
            M: [
                i,
                T("month")
            ],
            MM: [
                r,
                T("month")
            ],
            MMM: [
                a,
                function(y) {
                    var _ = A("months"), g = (A("monthsShort") || _.map(function(I) {
                        return I.slice(0, 3);
                    })).indexOf(y) + 1;
                    if (g < 1) throw new Error;
                    this.month = g % 12 || g;
                }
            ],
            MMMM: [
                a,
                function(y) {
                    var _ = A("months").indexOf(y) + 1;
                    if (_ < 1) throw new Error;
                    this.month = _ % 12 || _;
                }
            ],
            Y: [
                /[+-]?\d+/,
                T("year")
            ],
            YY: [
                r,
                function(y) {
                    this.year = m(y);
                }
            ],
            YYYY: [
                /\d{4}/,
                T("year")
            ],
            Z: W,
            ZZ: W
        };
        function j(y) {
            var _, g;
            _ = y, g = f && f.formats;
            for(var I = (y = _.replace(/(\[[^\]]+])|(LTS?|l{1,4}|L{1,4})/g, function(q, p, v) {
                var b = v && v.toUpperCase();
                return p || g[v] || e[v] || g[b].replace(/(\[[^\]]+])|(MMMM|MM|DD|dddd)/g, function(w, k, D) {
                    return k || D.slice(1);
                });
            })).match(t), z = I.length, N = 0; N < z; N += 1){
                var Q = I[N], X = F[Q], R = X && X[0], B = X && X[1];
                I[N] = B ? {
                    regex: R,
                    parser: B
                } : Q.replace(/^\[|\]$/g, "");
            }
            return function(q) {
                for(var p = {}, v = 0, b = 0; v < z; v += 1){
                    var w = I[v];
                    if (typeof w == "string") b += w.length;
                    else {
                        var k = w.regex, D = w.parser, c = q.slice(b), l = k.exec(c)[0];
                        D.call(p, l), q = q.replace(l, "");
                    }
                }
                return function(h) {
                    var u = h.afternoon;
                    if (u !== void 0) {
                        var x = h.hours;
                        u ? x < 12 && (h.hours += 12) : x === 12 && (h.hours = 0), delete h.afternoon;
                    }
                }(p), p;
            };
        }
        return (0, _chunkGTKDMUJJMjs.a)(j, "c"), function(y, _, g) {
            g.p.customParseFormat = !0, y && y.parseTwoDigitYear && (m = y.parseTwoDigitYear);
            var I = _.prototype, z = I.parse;
            I.parse = function(N) {
                var Q = N.date, X = N.utc, R = N.args;
                this.$u = X;
                var B = R[1];
                if (typeof B == "string") {
                    var q = R[2] === !0, p = R[3] === !0, v = q || p, b = R[2];
                    p && (b = R[2]), f = this.$locale(), !q && b && (f = g.Ls[b]), this.$d = function(c, l, h) {
                        try {
                            if ([
                                "x",
                                "X"
                            ].indexOf(l) > -1) return new Date((l === "X" ? 1e3 : 1) * c);
                            var u = j(l)(c), x = u.year, s = u.month, d = u.day, n = u.hours, M = u.minutes, C = u.seconds, S = u.milliseconds, O = u.zone, L = new Date, te = d || (x || s ? 1 : L.getDate()), Y = x || L.getFullYear(), U = 0;
                            x && !s || (U = s > 0 ? s - 1 : L.getMonth());
                            var ne = n || 0, ie = M || 0, de = C || 0, ye = S || 0;
                            return O ? new Date(Date.UTC(Y, U, te, ne, ie, de, ye + 60 * O.offset * 1e3)) : h ? new Date(Date.UTC(Y, U, te, ne, ie, de, ye)) : new Date(Y, U, te, ne, ie, de, ye);
                        } catch  {
                            return new Date("");
                        }
                    }(Q, B, X), this.init(), b && b !== !0 && (this.$L = this.locale(b).$L), v && Q != this.format(B) && (this.$d = new Date("")), f = {};
                } else if (B instanceof Array) for(var w = B.length, k = 1; k <= w; k += 1){
                    R[1] = B[k - 1];
                    var D = g.apply(this, R);
                    if (D.isValid()) {
                        this.$d = D.$d, this.$L = D.$L, this.init();
                        break;
                    }
                    k === w && (this.$d = new Date(""));
                }
                else z.call(this, N);
            };
        };
    });
});
var At = (0, _chunkGTKDMUJJMjs.b)((Ne, Re)=>{
    "use strict";
    (function(e, t) {
        typeof Ne == "object" && typeof Re < "u" ? Re.exports = t() : typeof define == "function" && define.amd ? define(t) : (e = typeof globalThis < "u" ? globalThis : e || self).dayjs_plugin_advancedFormat = t();
    })(Ne, function() {
        "use strict";
        return function(e, t) {
            var r = t.prototype, i = r.format;
            r.format = function(a) {
                var f = this, m = this.$locale();
                if (!this.isValid()) return i.bind(this)(a);
                var T = this.$utils(), W = (a || "YYYY-MM-DDTHH:mm:ssZ").replace(/\[([^\]]+)]|Q|wo|ww|w|WW|W|zzz|z|gggg|GGGG|Do|X|x|k{1,2}|S/g, function(A) {
                    switch(A){
                        case "Q":
                            return Math.ceil((f.$M + 1) / 3);
                        case "Do":
                            return m.ordinal(f.$D);
                        case "gggg":
                            return f.weekYear();
                        case "GGGG":
                            return f.isoWeekYear();
                        case "wo":
                            return m.ordinal(f.week(), "W");
                        case "w":
                        case "ww":
                            return T.s(f.week(), A === "w" ? 1 : 2, "0");
                        case "W":
                        case "WW":
                            return T.s(f.isoWeek(), A === "W" ? 1 : 2, "0");
                        case "k":
                        case "kk":
                            return T.s(String(f.$H === 0 ? 24 : f.$H), A === "k" ? 1 : 2, "0");
                        case "X":
                            return Math.floor(f.$d.getTime() / 1e3);
                        case "x":
                            return f.$d.getTime();
                        case "z":
                            return "[" + f.offsetName() + "]";
                        case "zzz":
                            return "[" + f.offsetName("long") + "]";
                        default:
                            return A;
                    }
                });
                return i.bind(this)(W);
            };
        };
    });
});
var Fe = function() {
    var e = (0, _chunkGTKDMUJJMjs.a)(function(D, c, l, h) {
        for(l = l || {}, h = D.length; h--; l[D[h]] = c);
        return l;
    }, "o"), t = [
        6,
        8,
        10,
        12,
        13,
        14,
        15,
        16,
        17,
        18,
        20,
        21,
        22,
        23,
        24,
        25,
        26,
        27,
        28,
        29,
        30,
        31,
        33,
        35,
        36,
        38,
        40
    ], r = [
        1,
        26
    ], i = [
        1,
        27
    ], a = [
        1,
        28
    ], f = [
        1,
        29
    ], m = [
        1,
        30
    ], T = [
        1,
        31
    ], W = [
        1,
        32
    ], A = [
        1,
        33
    ], E = [
        1,
        34
    ], F = [
        1,
        9
    ], j = [
        1,
        10
    ], y = [
        1,
        11
    ], _ = [
        1,
        12
    ], g = [
        1,
        13
    ], I = [
        1,
        14
    ], z = [
        1,
        15
    ], N = [
        1,
        16
    ], Q = [
        1,
        19
    ], X = [
        1,
        20
    ], R = [
        1,
        21
    ], B = [
        1,
        22
    ], q = [
        1,
        23
    ], p = [
        1,
        25
    ], v = [
        1,
        35
    ], b = {
        trace: (0, _chunkGTKDMUJJMjs.a)(function() {}, "trace"),
        yy: {},
        symbols_: {
            error: 2,
            start: 3,
            gantt: 4,
            document: 5,
            EOF: 6,
            line: 7,
            SPACE: 8,
            statement: 9,
            NL: 10,
            weekday: 11,
            weekday_monday: 12,
            weekday_tuesday: 13,
            weekday_wednesday: 14,
            weekday_thursday: 15,
            weekday_friday: 16,
            weekday_saturday: 17,
            weekday_sunday: 18,
            weekend: 19,
            weekend_friday: 20,
            weekend_saturday: 21,
            dateFormat: 22,
            inclusiveEndDates: 23,
            topAxis: 24,
            axisFormat: 25,
            tickInterval: 26,
            excludes: 27,
            includes: 28,
            todayMarker: 29,
            title: 30,
            acc_title: 31,
            acc_title_value: 32,
            acc_descr: 33,
            acc_descr_value: 34,
            acc_descr_multiline_value: 35,
            section: 36,
            clickStatement: 37,
            taskTxt: 38,
            taskData: 39,
            click: 40,
            callbackname: 41,
            callbackargs: 42,
            href: 43,
            clickStatementDebug: 44,
            $accept: 0,
            $end: 1
        },
        terminals_: {
            2: "error",
            4: "gantt",
            6: "EOF",
            8: "SPACE",
            10: "NL",
            12: "weekday_monday",
            13: "weekday_tuesday",
            14: "weekday_wednesday",
            15: "weekday_thursday",
            16: "weekday_friday",
            17: "weekday_saturday",
            18: "weekday_sunday",
            20: "weekend_friday",
            21: "weekend_saturday",
            22: "dateFormat",
            23: "inclusiveEndDates",
            24: "topAxis",
            25: "axisFormat",
            26: "tickInterval",
            27: "excludes",
            28: "includes",
            29: "todayMarker",
            30: "title",
            31: "acc_title",
            32: "acc_title_value",
            33: "acc_descr",
            34: "acc_descr_value",
            35: "acc_descr_multiline_value",
            36: "section",
            38: "taskTxt",
            39: "taskData",
            40: "click",
            41: "callbackname",
            42: "callbackargs",
            43: "href"
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
                11,
                1
            ],
            [
                19,
                1
            ],
            [
                19,
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
                2
            ],
            [
                37,
                2
            ],
            [
                37,
                3
            ],
            [
                37,
                3
            ],
            [
                37,
                4
            ],
            [
                37,
                3
            ],
            [
                37,
                4
            ],
            [
                37,
                2
            ],
            [
                44,
                2
            ],
            [
                44,
                3
            ],
            [
                44,
                3
            ],
            [
                44,
                4
            ],
            [
                44,
                3
            ],
            [
                44,
                4
            ],
            [
                44,
                2
            ]
        ],
        performAction: (0, _chunkGTKDMUJJMjs.a)(function(c, l, h, u, x, s, d) {
            var n = s.length - 1;
            switch(x){
                case 1:
                    return s[n - 1];
                case 2:
                    this.$ = [];
                    break;
                case 3:
                    s[n - 1].push(s[n]), this.$ = s[n - 1];
                    break;
                case 4:
                case 5:
                    this.$ = s[n];
                    break;
                case 6:
                case 7:
                    this.$ = [];
                    break;
                case 8:
                    u.setWeekday("monday");
                    break;
                case 9:
                    u.setWeekday("tuesday");
                    break;
                case 10:
                    u.setWeekday("wednesday");
                    break;
                case 11:
                    u.setWeekday("thursday");
                    break;
                case 12:
                    u.setWeekday("friday");
                    break;
                case 13:
                    u.setWeekday("saturday");
                    break;
                case 14:
                    u.setWeekday("sunday");
                    break;
                case 15:
                    u.setWeekend("friday");
                    break;
                case 16:
                    u.setWeekend("saturday");
                    break;
                case 17:
                    u.setDateFormat(s[n].substr(11)), this.$ = s[n].substr(11);
                    break;
                case 18:
                    u.enableInclusiveEndDates(), this.$ = s[n].substr(18);
                    break;
                case 19:
                    u.TopAxis(), this.$ = s[n].substr(8);
                    break;
                case 20:
                    u.setAxisFormat(s[n].substr(11)), this.$ = s[n].substr(11);
                    break;
                case 21:
                    u.setTickInterval(s[n].substr(13)), this.$ = s[n].substr(13);
                    break;
                case 22:
                    u.setExcludes(s[n].substr(9)), this.$ = s[n].substr(9);
                    break;
                case 23:
                    u.setIncludes(s[n].substr(9)), this.$ = s[n].substr(9);
                    break;
                case 24:
                    u.setTodayMarker(s[n].substr(12)), this.$ = s[n].substr(12);
                    break;
                case 27:
                    u.setDiagramTitle(s[n].substr(6)), this.$ = s[n].substr(6);
                    break;
                case 28:
                    this.$ = s[n].trim(), u.setAccTitle(this.$);
                    break;
                case 29:
                case 30:
                    this.$ = s[n].trim(), u.setAccDescription(this.$);
                    break;
                case 31:
                    u.addSection(s[n].substr(8)), this.$ = s[n].substr(8);
                    break;
                case 33:
                    u.addTask(s[n - 1], s[n]), this.$ = "task";
                    break;
                case 34:
                    this.$ = s[n - 1], u.setClickEvent(s[n - 1], s[n], null);
                    break;
                case 35:
                    this.$ = s[n - 2], u.setClickEvent(s[n - 2], s[n - 1], s[n]);
                    break;
                case 36:
                    this.$ = s[n - 2], u.setClickEvent(s[n - 2], s[n - 1], null), u.setLink(s[n - 2], s[n]);
                    break;
                case 37:
                    this.$ = s[n - 3], u.setClickEvent(s[n - 3], s[n - 2], s[n - 1]), u.setLink(s[n - 3], s[n]);
                    break;
                case 38:
                    this.$ = s[n - 2], u.setClickEvent(s[n - 2], s[n], null), u.setLink(s[n - 2], s[n - 1]);
                    break;
                case 39:
                    this.$ = s[n - 3], u.setClickEvent(s[n - 3], s[n - 1], s[n]), u.setLink(s[n - 3], s[n - 2]);
                    break;
                case 40:
                    this.$ = s[n - 1], u.setLink(s[n - 1], s[n]);
                    break;
                case 41:
                case 47:
                    this.$ = s[n - 1] + " " + s[n];
                    break;
                case 42:
                case 43:
                case 45:
                    this.$ = s[n - 2] + " " + s[n - 1] + " " + s[n];
                    break;
                case 44:
                case 46:
                    this.$ = s[n - 3] + " " + s[n - 2] + " " + s[n - 1] + " " + s[n];
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
            e(t, [
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
                11: 17,
                12: r,
                13: i,
                14: a,
                15: f,
                16: m,
                17: T,
                18: W,
                19: 18,
                20: A,
                21: E,
                22: F,
                23: j,
                24: y,
                25: _,
                26: g,
                27: I,
                28: z,
                29: N,
                30: Q,
                31: X,
                33: R,
                35: B,
                36: q,
                37: 24,
                38: p,
                40: v
            },
            e(t, [
                2,
                7
            ], {
                1: [
                    2,
                    1
                ]
            }),
            e(t, [
                2,
                3
            ]),
            {
                9: 36,
                11: 17,
                12: r,
                13: i,
                14: a,
                15: f,
                16: m,
                17: T,
                18: W,
                19: 18,
                20: A,
                21: E,
                22: F,
                23: j,
                24: y,
                25: _,
                26: g,
                27: I,
                28: z,
                29: N,
                30: Q,
                31: X,
                33: R,
                35: B,
                36: q,
                37: 24,
                38: p,
                40: v
            },
            e(t, [
                2,
                5
            ]),
            e(t, [
                2,
                6
            ]),
            e(t, [
                2,
                17
            ]),
            e(t, [
                2,
                18
            ]),
            e(t, [
                2,
                19
            ]),
            e(t, [
                2,
                20
            ]),
            e(t, [
                2,
                21
            ]),
            e(t, [
                2,
                22
            ]),
            e(t, [
                2,
                23
            ]),
            e(t, [
                2,
                24
            ]),
            e(t, [
                2,
                25
            ]),
            e(t, [
                2,
                26
            ]),
            e(t, [
                2,
                27
            ]),
            {
                32: [
                    1,
                    37
                ]
            },
            {
                34: [
                    1,
                    38
                ]
            },
            e(t, [
                2,
                30
            ]),
            e(t, [
                2,
                31
            ]),
            e(t, [
                2,
                32
            ]),
            {
                39: [
                    1,
                    39
                ]
            },
            e(t, [
                2,
                8
            ]),
            e(t, [
                2,
                9
            ]),
            e(t, [
                2,
                10
            ]),
            e(t, [
                2,
                11
            ]),
            e(t, [
                2,
                12
            ]),
            e(t, [
                2,
                13
            ]),
            e(t, [
                2,
                14
            ]),
            e(t, [
                2,
                15
            ]),
            e(t, [
                2,
                16
            ]),
            {
                41: [
                    1,
                    40
                ],
                43: [
                    1,
                    41
                ]
            },
            e(t, [
                2,
                4
            ]),
            e(t, [
                2,
                28
            ]),
            e(t, [
                2,
                29
            ]),
            e(t, [
                2,
                33
            ]),
            e(t, [
                2,
                34
            ], {
                42: [
                    1,
                    42
                ],
                43: [
                    1,
                    43
                ]
            }),
            e(t, [
                2,
                40
            ], {
                41: [
                    1,
                    44
                ]
            }),
            e(t, [
                2,
                35
            ], {
                43: [
                    1,
                    45
                ]
            }),
            e(t, [
                2,
                36
            ]),
            e(t, [
                2,
                38
            ], {
                42: [
                    1,
                    46
                ]
            }),
            e(t, [
                2,
                37
            ]),
            e(t, [
                2,
                39
            ])
        ],
        defaultActions: {},
        parseError: (0, _chunkGTKDMUJJMjs.a)(function(c, l) {
            if (l.recoverable) this.trace(c);
            else {
                var h = new Error(c);
                throw h.hash = l, h;
            }
        }, "parseError"),
        parse: (0, _chunkGTKDMUJJMjs.a)(function(c) {
            var l = this, h = [
                0
            ], u = [], x = [
                null
            ], s = [], d = this.table, n = "", M = 0, C = 0, S = 0, O = 2, L = 1, te = s.slice.call(arguments, 1), Y = Object.create(this.lexer), U = {
                yy: {}
            };
            for(var ne in this.yy)Object.prototype.hasOwnProperty.call(this.yy, ne) && (U.yy[ne] = this.yy[ne]);
            Y.setInput(c, U.yy), U.yy.lexer = Y, U.yy.parser = this, typeof Y.yylloc > "u" && (Y.yylloc = {});
            var ie = Y.yylloc;
            s.push(ie);
            var de = Y.options && Y.options.ranges;
            typeof U.yy.parseError == "function" ? this.parseError = U.yy.parseError : this.parseError = Object.getPrototypeOf(this).parseError;
            function ye(G) {
                h.length = h.length - 2 * G, x.length = x.length - G, s.length = s.length - G;
            }
            (0, _chunkGTKDMUJJMjs.a)(ye, "popStack");
            function et() {
                var G;
                return G = u.pop() || Y.lex() || L, typeof G != "number" && (G instanceof Array && (u = G, G = u.pop()), G = l.symbols_[G] || G), G;
            }
            (0, _chunkGTKDMUJJMjs.a)(et, "lex");
            for(var P, _e, re, Z, On, De, ae = {}, pe, J, tt, ge;;){
                if (re = h[h.length - 1], this.defaultActions[re] ? Z = this.defaultActions[re] : ((P === null || typeof P > "u") && (P = et()), Z = d[re] && d[re][P]), typeof Z > "u" || !Z.length || !Z[0]) {
                    var Ce = "";
                    ge = [];
                    for(pe in d[re])this.terminals_[pe] && pe > O && ge.push("'" + this.terminals_[pe] + "'");
                    Y.showPosition ? Ce = "Parse error on line " + (M + 1) + `:
` + Y.showPosition() + `
Expecting ` + ge.join(", ") + ", got '" + (this.terminals_[P] || P) + "'" : Ce = "Parse error on line " + (M + 1) + ": Unexpected " + (P == L ? "end of input" : "'" + (this.terminals_[P] || P) + "'"), this.parseError(Ce, {
                        text: Y.match,
                        token: this.terminals_[P] || P,
                        line: Y.yylineno,
                        loc: ie,
                        expected: ge
                    });
                }
                if (Z[0] instanceof Array && Z.length > 1) throw new Error("Parse Error: multiple actions possible at state: " + re + ", token: " + P);
                switch(Z[0]){
                    case 1:
                        h.push(P), x.push(Y.yytext), s.push(Y.yylloc), h.push(Z[1]), P = null, _e ? (P = _e, _e = null) : (C = Y.yyleng, n = Y.yytext, M = Y.yylineno, ie = Y.yylloc, S > 0 && S--);
                        break;
                    case 2:
                        if (J = this.productions_[Z[1]][1], ae.$ = x[x.length - J], ae._$ = {
                            first_line: s[s.length - (J || 1)].first_line,
                            last_line: s[s.length - 1].last_line,
                            first_column: s[s.length - (J || 1)].first_column,
                            last_column: s[s.length - 1].last_column
                        }, de && (ae._$.range = [
                            s[s.length - (J || 1)].range[0],
                            s[s.length - 1].range[1]
                        ]), De = this.performAction.apply(ae, [
                            n,
                            C,
                            M,
                            U.yy,
                            Z[1],
                            x,
                            s
                        ].concat(te)), typeof De < "u") return De;
                        J && (h = h.slice(0, -1 * J * 2), x = x.slice(0, -1 * J), s = s.slice(0, -1 * J)), h.push(this.productions_[Z[1]][0]), x.push(ae.$), s.push(ae._$), tt = d[h[h.length - 2]][h[h.length - 1]], h.push(tt);
                        break;
                    case 3:
                        return !0;
                }
            }
            return !0;
        }, "parse")
    }, w = function() {
        var D = {
            EOF: 1,
            parseError: (0, _chunkGTKDMUJJMjs.a)(function(l, h) {
                if (this.yy.parser) this.yy.parser.parseError(l, h);
                else throw new Error(l);
            }, "parseError"),
            setInput: (0, _chunkGTKDMUJJMjs.a)(function(c, l) {
                return this.yy = l || this.yy || {}, this._input = c, this._more = this._backtrack = this.done = !1, this.yylineno = this.yyleng = 0, this.yytext = this.matched = this.match = "", this.conditionStack = [
                    "INITIAL"
                ], this.yylloc = {
                    first_line: 1,
                    first_column: 0,
                    last_line: 1,
                    last_column: 0
                }, this.options.ranges && (this.yylloc.range = [
                    0,
                    0
                ]), this.offset = 0, this;
            }, "setInput"),
            input: (0, _chunkGTKDMUJJMjs.a)(function() {
                var c = this._input[0];
                this.yytext += c, this.yyleng++, this.offset++, this.match += c, this.matched += c;
                var l = c.match(/(?:\r\n?|\n).*/g);
                return l ? (this.yylineno++, this.yylloc.last_line++) : this.yylloc.last_column++, this.options.ranges && this.yylloc.range[1]++, this._input = this._input.slice(1), c;
            }, "input"),
            unput: (0, _chunkGTKDMUJJMjs.a)(function(c) {
                var l = c.length, h = c.split(/(?:\r\n?|\n)/g);
                this._input = c + this._input, this.yytext = this.yytext.substr(0, this.yytext.length - l), this.offset -= l;
                var u = this.match.split(/(?:\r\n?|\n)/g);
                this.match = this.match.substr(0, this.match.length - 1), this.matched = this.matched.substr(0, this.matched.length - 1), h.length - 1 && (this.yylineno -= h.length - 1);
                var x = this.yylloc.range;
                return this.yylloc = {
                    first_line: this.yylloc.first_line,
                    last_line: this.yylineno + 1,
                    first_column: this.yylloc.first_column,
                    last_column: h ? (h.length === u.length ? this.yylloc.first_column : 0) + u[u.length - h.length].length - h[0].length : this.yylloc.first_column - l
                }, this.options.ranges && (this.yylloc.range = [
                    x[0],
                    x[0] + this.yyleng - l
                ]), this.yyleng = this.yytext.length, this;
            }, "unput"),
            more: (0, _chunkGTKDMUJJMjs.a)(function() {
                return this._more = !0, this;
            }, "more"),
            reject: (0, _chunkGTKDMUJJMjs.a)(function() {
                if (this.options.backtrack_lexer) this._backtrack = !0;
                else return this.parseError("Lexical error on line " + (this.yylineno + 1) + `. You can only invoke reject() in the lexer when the lexer is of the backtracking persuasion (options.backtrack_lexer = true).
` + this.showPosition(), {
                    text: "",
                    token: null,
                    line: this.yylineno
                });
                return this;
            }, "reject"),
            less: (0, _chunkGTKDMUJJMjs.a)(function(c) {
                this.unput(this.match.slice(c));
            }, "less"),
            pastInput: (0, _chunkGTKDMUJJMjs.a)(function() {
                var c = this.matched.substr(0, this.matched.length - this.match.length);
                return (c.length > 20 ? "..." : "") + c.substr(-20).replace(/\n/g, "");
            }, "pastInput"),
            upcomingInput: (0, _chunkGTKDMUJJMjs.a)(function() {
                var c = this.match;
                return c.length < 20 && (c += this._input.substr(0, 20 - c.length)), (c.substr(0, 20) + (c.length > 20 ? "..." : "")).replace(/\n/g, "");
            }, "upcomingInput"),
            showPosition: (0, _chunkGTKDMUJJMjs.a)(function() {
                var c = this.pastInput(), l = new Array(c.length + 1).join("-");
                return c + this.upcomingInput() + `
` + l + "^";
            }, "showPosition"),
            test_match: (0, _chunkGTKDMUJJMjs.a)(function(c, l) {
                var h, u, x;
                if (this.options.backtrack_lexer && (x = {
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
                }, this.options.ranges && (x.yylloc.range = this.yylloc.range.slice(0))), u = c[0].match(/(?:\r\n?|\n).*/g), u && (this.yylineno += u.length), this.yylloc = {
                    first_line: this.yylloc.last_line,
                    last_line: this.yylineno + 1,
                    first_column: this.yylloc.last_column,
                    last_column: u ? u[u.length - 1].length - u[u.length - 1].match(/\r?\n?/)[0].length : this.yylloc.last_column + c[0].length
                }, this.yytext += c[0], this.match += c[0], this.matches = c, this.yyleng = this.yytext.length, this.options.ranges && (this.yylloc.range = [
                    this.offset,
                    this.offset += this.yyleng
                ]), this._more = !1, this._backtrack = !1, this._input = this._input.slice(c[0].length), this.matched += c[0], h = this.performAction.call(this, this.yy, this, l, this.conditionStack[this.conditionStack.length - 1]), this.done && this._input && (this.done = !1), h) return h;
                if (this._backtrack) {
                    for(var s in x)this[s] = x[s];
                    return !1;
                }
                return !1;
            }, "test_match"),
            next: (0, _chunkGTKDMUJJMjs.a)(function() {
                if (this.done) return this.EOF;
                this._input || (this.done = !0);
                var c, l, h, u;
                this._more || (this.yytext = "", this.match = "");
                for(var x = this._currentRules(), s = 0; s < x.length; s++)if (h = this._input.match(this.rules[x[s]]), h && (!l || h[0].length > l[0].length)) {
                    if (l = h, u = s, this.options.backtrack_lexer) {
                        if (c = this.test_match(h, x[s]), c !== !1) return c;
                        if (this._backtrack) {
                            l = !1;
                            continue;
                        } else return !1;
                    } else if (!this.options.flex) break;
                }
                return l ? (c = this.test_match(l, x[u]), c !== !1 ? c : !1) : this._input === "" ? this.EOF : this.parseError("Lexical error on line " + (this.yylineno + 1) + `. Unrecognized text.
` + this.showPosition(), {
                    text: "",
                    token: null,
                    line: this.yylineno
                });
            }, "next"),
            lex: (0, _chunkGTKDMUJJMjs.a)(function() {
                var l = this.next();
                return l || this.lex();
            }, "lex"),
            begin: (0, _chunkGTKDMUJJMjs.a)(function(l) {
                this.conditionStack.push(l);
            }, "begin"),
            popState: (0, _chunkGTKDMUJJMjs.a)(function() {
                var l = this.conditionStack.length - 1;
                return l > 0 ? this.conditionStack.pop() : this.conditionStack[0];
            }, "popState"),
            _currentRules: (0, _chunkGTKDMUJJMjs.a)(function() {
                return this.conditionStack.length && this.conditionStack[this.conditionStack.length - 1] ? this.conditions[this.conditionStack[this.conditionStack.length - 1]].rules : this.conditions.INITIAL.rules;
            }, "_currentRules"),
            topState: (0, _chunkGTKDMUJJMjs.a)(function(l) {
                return l = this.conditionStack.length - 1 - Math.abs(l || 0), l >= 0 ? this.conditionStack[l] : "INITIAL";
            }, "topState"),
            pushState: (0, _chunkGTKDMUJJMjs.a)(function(l) {
                this.begin(l);
            }, "pushState"),
            stateStackSize: (0, _chunkGTKDMUJJMjs.a)(function() {
                return this.conditionStack.length;
            }, "stateStackSize"),
            options: {
                "case-insensitive": !0
            },
            performAction: (0, _chunkGTKDMUJJMjs.a)(function(l, h, u, x) {
                var s = x;
                switch(u){
                    case 0:
                        return this.begin("open_directive"), "open_directive";
                    case 1:
                        return this.begin("acc_title"), 31;
                    case 2:
                        return this.popState(), "acc_title_value";
                    case 3:
                        return this.begin("acc_descr"), 33;
                    case 4:
                        return this.popState(), "acc_descr_value";
                    case 5:
                        this.begin("acc_descr_multiline");
                        break;
                    case 6:
                        this.popState();
                        break;
                    case 7:
                        return "acc_descr_multiline_value";
                    case 8:
                        break;
                    case 9:
                        break;
                    case 10:
                        break;
                    case 11:
                        return 10;
                    case 12:
                        break;
                    case 13:
                        break;
                    case 14:
                        this.begin("href");
                        break;
                    case 15:
                        this.popState();
                        break;
                    case 16:
                        return 43;
                    case 17:
                        this.begin("callbackname");
                        break;
                    case 18:
                        this.popState();
                        break;
                    case 19:
                        this.popState(), this.begin("callbackargs");
                        break;
                    case 20:
                        return 41;
                    case 21:
                        this.popState();
                        break;
                    case 22:
                        return 42;
                    case 23:
                        this.begin("click");
                        break;
                    case 24:
                        this.popState();
                        break;
                    case 25:
                        return 40;
                    case 26:
                        return 4;
                    case 27:
                        return 22;
                    case 28:
                        return 23;
                    case 29:
                        return 24;
                    case 30:
                        return 25;
                    case 31:
                        return 26;
                    case 32:
                        return 28;
                    case 33:
                        return 27;
                    case 34:
                        return 29;
                    case 35:
                        return 12;
                    case 36:
                        return 13;
                    case 37:
                        return 14;
                    case 38:
                        return 15;
                    case 39:
                        return 16;
                    case 40:
                        return 17;
                    case 41:
                        return 18;
                    case 42:
                        return 20;
                    case 43:
                        return 21;
                    case 44:
                        return "date";
                    case 45:
                        return 30;
                    case 46:
                        return "accDescription";
                    case 47:
                        return 36;
                    case 48:
                        return 38;
                    case 49:
                        return 39;
                    case 50:
                        return ":";
                    case 51:
                        return 6;
                    case 52:
                        return "INVALID";
                }
            }, "anonymous"),
            rules: [
                /^(?:%%\{)/i,
                /^(?:accTitle\s*:\s*)/i,
                /^(?:(?!\n||)*[^\n]*)/i,
                /^(?:accDescr\s*:\s*)/i,
                /^(?:(?!\n||)*[^\n]*)/i,
                /^(?:accDescr\s*\{\s*)/i,
                /^(?:[\}])/i,
                /^(?:[^\}]*)/i,
                /^(?:%%(?!\{)*[^\n]*)/i,
                /^(?:[^\}]%%*[^\n]*)/i,
                /^(?:%%*[^\n]*[\n]*)/i,
                /^(?:[\n]+)/i,
                /^(?:\s+)/i,
                /^(?:%[^\n]*)/i,
                /^(?:href[\s]+["])/i,
                /^(?:["])/i,
                /^(?:[^"]*)/i,
                /^(?:call[\s]+)/i,
                /^(?:\([\s]*\))/i,
                /^(?:\()/i,
                /^(?:[^(]*)/i,
                /^(?:\))/i,
                /^(?:[^)]*)/i,
                /^(?:click[\s]+)/i,
                /^(?:[\s\n])/i,
                /^(?:[^\s\n]*)/i,
                /^(?:gantt\b)/i,
                /^(?:dateFormat\s[^#\n;]+)/i,
                /^(?:inclusiveEndDates\b)/i,
                /^(?:topAxis\b)/i,
                /^(?:axisFormat\s[^#\n;]+)/i,
                /^(?:tickInterval\s[^#\n;]+)/i,
                /^(?:includes\s[^#\n;]+)/i,
                /^(?:excludes\s[^#\n;]+)/i,
                /^(?:todayMarker\s[^\n;]+)/i,
                /^(?:weekday\s+monday\b)/i,
                /^(?:weekday\s+tuesday\b)/i,
                /^(?:weekday\s+wednesday\b)/i,
                /^(?:weekday\s+thursday\b)/i,
                /^(?:weekday\s+friday\b)/i,
                /^(?:weekday\s+saturday\b)/i,
                /^(?:weekday\s+sunday\b)/i,
                /^(?:weekend\s+friday\b)/i,
                /^(?:weekend\s+saturday\b)/i,
                /^(?:\d\d\d\d-\d\d-\d\d\b)/i,
                /^(?:title\s[^\n]+)/i,
                /^(?:accDescription\s[^#\n;]+)/i,
                /^(?:section\s[^\n]+)/i,
                /^(?:[^:\n]+)/i,
                /^(?::[^#\n;]+)/i,
                /^(?::)/i,
                /^(?:$)/i,
                /^(?:.)/i
            ],
            conditions: {
                acc_descr_multiline: {
                    rules: [
                        6,
                        7
                    ],
                    inclusive: !1
                },
                acc_descr: {
                    rules: [
                        4
                    ],
                    inclusive: !1
                },
                acc_title: {
                    rules: [
                        2
                    ],
                    inclusive: !1
                },
                callbackargs: {
                    rules: [
                        21,
                        22
                    ],
                    inclusive: !1
                },
                callbackname: {
                    rules: [
                        18,
                        19,
                        20
                    ],
                    inclusive: !1
                },
                href: {
                    rules: [
                        15,
                        16
                    ],
                    inclusive: !1
                },
                click: {
                    rules: [
                        24,
                        25
                    ],
                    inclusive: !1
                },
                INITIAL: {
                    rules: [
                        0,
                        1,
                        3,
                        5,
                        8,
                        9,
                        10,
                        11,
                        12,
                        13,
                        14,
                        17,
                        23,
                        26,
                        27,
                        28,
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
                        45,
                        46,
                        47,
                        48,
                        49,
                        50,
                        51,
                        52
                    ],
                    inclusive: !0
                }
            }
        };
        return D;
    }();
    b.lexer = w;
    function k() {
        this.yy = {};
    }
    return (0, _chunkGTKDMUJJMjs.a)(k, "Parser"), k.prototype = b, b.Parser = k, new k;
}();
Fe.parser = Fe;
var St = Fe;
var Yt = (0, _chunkGTKDMUJJMjs.e)((0, _chunkTI4EEUUGMjs.a)(), 1), H = (0, _chunkGTKDMUJJMjs.e)((0, _chunkNQURTBEVMjs.a)(), 1), Wt = (0, _chunkGTKDMUJJMjs.e)(Et(), 1), Ft = (0, _chunkGTKDMUJJMjs.e)(Mt(), 1), Vt = (0, _chunkGTKDMUJJMjs.e)(At(), 1);
H.default.extend(Wt.default);
H.default.extend(Ft.default);
H.default.extend(Vt.default);
var Lt = {
    friday: 5,
    saturday: 6
}, K = "", He = "", Xe, qe = "", he = [], me = [], Ue = new Map, Ze = [], Te = [], ue = "", Qe = "", Ot = [
    "active",
    "done",
    "crit",
    "milestone"
], Ke = [], ke = !1, Je = !1, $e = "sunday", ve = "saturday", Be = 0, Kt = (0, _chunkGTKDMUJJMjs.a)(function() {
    Ze = [], Te = [], ue = "", Ke = [], be = 0, Ge = void 0, xe = void 0, V = [], K = "", He = "", Qe = "", Xe = void 0, qe = "", he = [], me = [], ke = !1, Je = !1, Be = 0, Ue = new Map, (0, _chunkNQURTBEVMjs.P)(), $e = "sunday", ve = "saturday";
}, "clear"), Jt = (0, _chunkGTKDMUJJMjs.a)(function(e) {
    He = e;
}, "setAxisFormat"), $t = (0, _chunkGTKDMUJJMjs.a)(function() {
    return He;
}, "getAxisFormat"), en = (0, _chunkGTKDMUJJMjs.a)(function(e) {
    Xe = e;
}, "setTickInterval"), tn = (0, _chunkGTKDMUJJMjs.a)(function() {
    return Xe;
}, "getTickInterval"), nn = (0, _chunkGTKDMUJJMjs.a)(function(e) {
    qe = e;
}, "setTodayMarker"), rn = (0, _chunkGTKDMUJJMjs.a)(function() {
    return qe;
}, "getTodayMarker"), sn = (0, _chunkGTKDMUJJMjs.a)(function(e) {
    K = e;
}, "setDateFormat"), an = (0, _chunkGTKDMUJJMjs.a)(function() {
    ke = !0;
}, "enableInclusiveEndDates"), on = (0, _chunkGTKDMUJJMjs.a)(function() {
    return ke;
}, "endDatesAreInclusive"), cn = (0, _chunkGTKDMUJJMjs.a)(function() {
    Je = !0;
}, "enableTopAxis"), ln = (0, _chunkGTKDMUJJMjs.a)(function() {
    return Je;
}, "topAxisEnabled"), un = (0, _chunkGTKDMUJJMjs.a)(function(e) {
    Qe = e;
}, "setDisplayMode"), dn = (0, _chunkGTKDMUJJMjs.a)(function() {
    return Qe;
}, "getDisplayMode"), fn = (0, _chunkGTKDMUJJMjs.a)(function() {
    return K;
}, "getDateFormat"), hn = (0, _chunkGTKDMUJJMjs.a)(function(e) {
    he = e.toLowerCase().split(/[\s,]+/);
}, "setIncludes"), mn = (0, _chunkGTKDMUJJMjs.a)(function() {
    return he;
}, "getIncludes"), kn = (0, _chunkGTKDMUJJMjs.a)(function(e) {
    me = e.toLowerCase().split(/[\s,]+/);
}, "setExcludes"), yn = (0, _chunkGTKDMUJJMjs.a)(function() {
    return me;
}, "getExcludes"), pn = (0, _chunkGTKDMUJJMjs.a)(function() {
    return Ue;
}, "getLinks"), gn = (0, _chunkGTKDMUJJMjs.a)(function(e) {
    ue = e, Ze.push(e);
}, "addSection"), bn = (0, _chunkGTKDMUJJMjs.a)(function() {
    return Ze;
}, "getSections"), xn = (0, _chunkGTKDMUJJMjs.a)(function() {
    let e = It(), t = 10, r = 0;
    for(; !e && r < t;)e = It(), r++;
    return Te = V, Te;
}, "getTasks"), Pt = (0, _chunkGTKDMUJJMjs.a)(function(e, t, r, i) {
    return i.includes(e.format(t.trim())) ? !1 : r.includes("weekends") && (e.isoWeekday() === Lt[ve] || e.isoWeekday() === Lt[ve] + 1) || r.includes(e.format("dddd").toLowerCase()) ? !0 : r.includes(e.format(t.trim()));
}, "isInvalidDate"), Tn = (0, _chunkGTKDMUJJMjs.a)(function(e) {
    $e = e;
}, "setWeekday"), vn = (0, _chunkGTKDMUJJMjs.a)(function() {
    return $e;
}, "getWeekday"), wn = (0, _chunkGTKDMUJJMjs.a)(function(e) {
    ve = e;
}, "setWeekend"), zt = (0, _chunkGTKDMUJJMjs.a)(function(e, t, r, i) {
    if (!r.length || e.manualEndTime) return;
    let a;
    e.startTime instanceof Date ? a = (0, H.default)(e.startTime) : a = (0, H.default)(e.startTime, t, !0), a = a.add(1, "d");
    let f;
    e.endTime instanceof Date ? f = (0, H.default)(e.endTime) : f = (0, H.default)(e.endTime, t, !0);
    let [m, T] = _n(a, f, t, r, i);
    e.endTime = m.toDate(), e.renderEndTime = T;
}, "checkTaskDates"), _n = (0, _chunkGTKDMUJJMjs.a)(function(e, t, r, i, a) {
    let f = !1, m = null;
    for(; e <= t;)f || (m = t.toDate()), f = Pt(e, r, i, a), f && (t = t.add(1, "d")), e = e.add(1, "d");
    return [
        t,
        m
    ];
}, "fixTaskDates"), je = (0, _chunkGTKDMUJJMjs.a)(function(e, t, r) {
    r = r.trim();
    let a = /^after\s+(?<ids>[\d\w- ]+)/.exec(r);
    if (a !== null) {
        let m = null;
        for (let W of a.groups.ids.split(" ")){
            let A = se(W);
            A !== void 0 && (!m || A.endTime > m.endTime) && (m = A);
        }
        if (m) return m.endTime;
        let T = new Date;
        return T.setHours(0, 0, 0, 0), T;
    }
    let f = (0, H.default)(r, t.trim(), !0);
    if (f.isValid()) return f.toDate();
    {
        (0, _chunkNQURTBEVMjs.b).debug("Invalid date:" + r), (0, _chunkNQURTBEVMjs.b).debug("With date format:" + t.trim());
        let m = new Date(r);
        if (m === void 0 || isNaN(m.getTime()) || m.getFullYear() < -10000 || m.getFullYear() > 1e4) throw new Error("Invalid date:" + r);
        return m;
    }
}, "getStartDate"), Nt = (0, _chunkGTKDMUJJMjs.a)(function(e) {
    let t = /^(\d+(?:\.\d+)?)([Mdhmswy]|ms)$/.exec(e.trim());
    return t !== null ? [
        Number.parseFloat(t[1]),
        t[2]
    ] : [
        NaN,
        "ms"
    ];
}, "parseDuration"), Rt = (0, _chunkGTKDMUJJMjs.a)(function(e, t, r, i = !1) {
    r = r.trim();
    let f = /^until\s+(?<ids>[\d\w- ]+)/.exec(r);
    if (f !== null) {
        let E = null;
        for (let j of f.groups.ids.split(" ")){
            let y = se(j);
            y !== void 0 && (!E || y.startTime < E.startTime) && (E = y);
        }
        if (E) return E.startTime;
        let F = new Date;
        return F.setHours(0, 0, 0, 0), F;
    }
    let m = (0, H.default)(r, t.trim(), !0);
    if (m.isValid()) return i && (m = m.add(1, "d")), m.toDate();
    let T = (0, H.default)(e), [W, A] = Nt(r);
    if (!Number.isNaN(W)) {
        let E = T.add(W, A);
        E.isValid() && (T = E);
    }
    return T.toDate();
}, "getEndDate"), be = 0, le = (0, _chunkGTKDMUJJMjs.a)(function(e) {
    return e === void 0 ? (be = be + 1, "task" + be) : e;
}, "parseId"), Dn = (0, _chunkGTKDMUJJMjs.a)(function(e, t) {
    let r;
    t.substr(0, 1) === ":" ? r = t.substr(1, t.length) : r = t;
    let i = r.split(","), a = {};
    Xt(i, a, Ot);
    for(let m = 0; m < i.length; m++)i[m] = i[m].trim();
    let f = "";
    switch(i.length){
        case 1:
            a.id = le(), a.startTime = e.endTime, f = i[0];
            break;
        case 2:
            a.id = le(), a.startTime = je(void 0, K, i[0]), f = i[1];
            break;
        case 3:
            a.id = le(i[0]), a.startTime = je(void 0, K, i[1]), f = i[2];
            break;
        default:
    }
    return f && (a.endTime = Rt(a.startTime, K, f, ke), a.manualEndTime = (0, H.default)(f, "YYYY-MM-DD", !0).isValid(), zt(a, K, me, he)), a;
}, "compileData"), Cn = (0, _chunkGTKDMUJJMjs.a)(function(e, t) {
    let r;
    t.substr(0, 1) === ":" ? r = t.substr(1, t.length) : r = t;
    let i = r.split(","), a = {};
    Xt(i, a, Ot);
    for(let f = 0; f < i.length; f++)i[f] = i[f].trim();
    switch(i.length){
        case 1:
            a.id = le(), a.startTime = {
                type: "prevTaskEnd",
                id: e
            }, a.endTime = {
                data: i[0]
            };
            break;
        case 2:
            a.id = le(), a.startTime = {
                type: "getStartDate",
                startData: i[0]
            }, a.endTime = {
                data: i[1]
            };
            break;
        case 3:
            a.id = le(i[0]), a.startTime = {
                type: "getStartDate",
                startData: i[1]
            }, a.endTime = {
                data: i[2]
            };
            break;
        default:
    }
    return a;
}, "parseData"), Ge, xe, V = [], Bt = {}, Sn = (0, _chunkGTKDMUJJMjs.a)(function(e, t) {
    let r = {
        section: ue,
        type: ue,
        processed: !1,
        manualEndTime: !1,
        renderEndTime: null,
        raw: {
            data: t
        },
        task: e,
        classes: []
    }, i = Cn(xe, t);
    r.raw.startTime = i.startTime, r.raw.endTime = i.endTime, r.id = i.id, r.prevTaskId = xe, r.active = i.active, r.done = i.done, r.crit = i.crit, r.milestone = i.milestone, r.order = Be, Be++;
    let a = V.push(r);
    xe = r.id, Bt[r.id] = a - 1;
}, "addTask"), se = (0, _chunkGTKDMUJJMjs.a)(function(e) {
    let t = Bt[e];
    return V[t];
}, "findTaskById"), En = (0, _chunkGTKDMUJJMjs.a)(function(e, t) {
    let r = {
        section: ue,
        type: ue,
        description: e,
        task: e,
        classes: []
    }, i = Dn(Ge, t);
    r.startTime = i.startTime, r.endTime = i.endTime, r.id = i.id, r.active = i.active, r.done = i.done, r.crit = i.crit, r.milestone = i.milestone, Ge = r, Te.push(r);
}, "addTaskOrg"), It = (0, _chunkGTKDMUJJMjs.a)(function() {
    let e = (0, _chunkGTKDMUJJMjs.a)(function(r) {
        let i = V[r], a = "";
        switch(V[r].raw.startTime.type){
            case "prevTaskEnd":
                {
                    let f = se(i.prevTaskId);
                    i.startTime = f.endTime;
                    break;
                }
            case "getStartDate":
                a = je(void 0, K, V[r].raw.startTime.startData), a && (V[r].startTime = a);
                break;
        }
        return V[r].startTime && (V[r].endTime = Rt(V[r].startTime, K, V[r].raw.endTime.data, ke), V[r].endTime && (V[r].processed = !0, V[r].manualEndTime = (0, H.default)(V[r].raw.endTime.data, "YYYY-MM-DD", !0).isValid(), zt(V[r], K, me, he))), V[r].processed;
    }, "compileTask"), t = !0;
    for (let [r, i] of V.entries())e(r), t = t && i.processed;
    return t;
}, "compileTasks"), Mn = (0, _chunkGTKDMUJJMjs.a)(function(e, t) {
    let r = t;
    (0, _chunkNQURTBEVMjs.X)().securityLevel !== "loose" && (r = (0, Yt.sanitizeUrl)(t)), e.split(",").forEach(function(i) {
        se(i) !== void 0 && (Gt(i, ()=>{
            window.open(r, "_self");
        }), Ue.set(i, r));
    }), jt(e, "clickable");
}, "setLink"), jt = (0, _chunkGTKDMUJJMjs.a)(function(e, t) {
    e.split(",").forEach(function(r) {
        let i = se(r);
        i !== void 0 && i.classes.push(t);
    });
}, "setClass"), An = (0, _chunkGTKDMUJJMjs.a)(function(e, t, r) {
    if ((0, _chunkNQURTBEVMjs.X)().securityLevel !== "loose" || t === void 0) return;
    let i = [];
    if (typeof r == "string") {
        i = r.split(/,(?=(?:(?:[^"]*"){2})*[^"]*$)/);
        for(let f = 0; f < i.length; f++){
            let m = i[f].trim();
            m.startsWith('"') && m.endsWith('"') && (m = m.substr(1, m.length - 2)), i[f] = m;
        }
    }
    i.length === 0 && i.push(e), se(e) !== void 0 && Gt(e, ()=>{
        (0, _chunkAC3VT7B7Mjs.m).runFunc(t, ...i);
    });
}, "setClickFun"), Gt = (0, _chunkGTKDMUJJMjs.a)(function(e, t) {
    Ke.push(function() {
        let r = document.querySelector(`[id="${e}"]`);
        r !== null && r.addEventListener("click", function() {
            t();
        });
    }, function() {
        let r = document.querySelector(`[id="${e}-text"]`);
        r !== null && r.addEventListener("click", function() {
            t();
        });
    });
}, "pushFun"), Ln = (0, _chunkGTKDMUJJMjs.a)(function(e, t, r) {
    e.split(",").forEach(function(i) {
        An(i, t, r);
    }), jt(e, "clickable");
}, "setClickEvent"), In = (0, _chunkGTKDMUJJMjs.a)(function(e) {
    Ke.forEach(function(t) {
        t(e);
    });
}, "bindFunctions"), Ht = {
    getConfig: (0, _chunkGTKDMUJJMjs.a)(()=>(0, _chunkNQURTBEVMjs.X)().gantt, "getConfig"),
    clear: Kt,
    setDateFormat: sn,
    getDateFormat: fn,
    enableInclusiveEndDates: an,
    endDatesAreInclusive: on,
    enableTopAxis: cn,
    topAxisEnabled: ln,
    setAxisFormat: Jt,
    getAxisFormat: $t,
    setTickInterval: en,
    getTickInterval: tn,
    setTodayMarker: nn,
    getTodayMarker: rn,
    setAccTitle: (0, _chunkNQURTBEVMjs.Q),
    getAccTitle: (0, _chunkNQURTBEVMjs.R),
    setDiagramTitle: (0, _chunkNQURTBEVMjs.U),
    getDiagramTitle: (0, _chunkNQURTBEVMjs.V),
    setDisplayMode: un,
    getDisplayMode: dn,
    setAccDescription: (0, _chunkNQURTBEVMjs.S),
    getAccDescription: (0, _chunkNQURTBEVMjs.T),
    addSection: gn,
    getSections: bn,
    getTasks: xn,
    addTask: Sn,
    findTaskById: se,
    addTaskOrg: En,
    setIncludes: hn,
    getIncludes: mn,
    setExcludes: kn,
    getExcludes: yn,
    setClickEvent: Ln,
    setLink: Mn,
    getLinks: pn,
    bindFunctions: In,
    parseDuration: Nt,
    isInvalidDate: Pt,
    setWeekday: Tn,
    getWeekday: vn,
    setWeekend: wn
};
function Xt(e, t, r) {
    let i = !0;
    for(; i;)i = !1, r.forEach(function(a) {
        let f = "^\\s*" + a + "\\s*$", m = new RegExp(f);
        e[0].match(m) && (t[a] = !0, e.shift(1), i = !0);
    });
}
(0, _chunkGTKDMUJJMjs.a)(Xt, "getTaskTags");
var we = (0, _chunkGTKDMUJJMjs.e)((0, _chunkNQURTBEVMjs.a)(), 1);
var Yn = (0, _chunkGTKDMUJJMjs.a)(function() {
    (0, _chunkNQURTBEVMjs.b).debug("Something is calling, setConf, remove the call");
}, "setConf"), qt = {
    monday: (0, _chunkNQURTBEVMjs.qa),
    tuesday: (0, _chunkNQURTBEVMjs.ra),
    wednesday: (0, _chunkNQURTBEVMjs.sa),
    thursday: (0, _chunkNQURTBEVMjs.ta),
    friday: (0, _chunkNQURTBEVMjs.ua),
    saturday: (0, _chunkNQURTBEVMjs.va),
    sunday: (0, _chunkNQURTBEVMjs.pa)
}, Wn = (0, _chunkGTKDMUJJMjs.a)((e, t)=>{
    let r = [
        ...e
    ].map(()=>-1 / 0), i = [
        ...e
    ].sort((f, m)=>f.startTime - m.startTime || f.order - m.order), a = 0;
    for (let f of i)for(let m = 0; m < r.length; m++)if (f.startTime >= r[m]) {
        r[m] = f.endTime, f.order = m + t, m > a && (a = m);
        break;
    }
    return a;
}, "getMaxIntersections"), $, Fn = (0, _chunkGTKDMUJJMjs.a)(function(e, t, r, i) {
    let a = (0, _chunkNQURTBEVMjs.X)().gantt, f = (0, _chunkNQURTBEVMjs.X)().securityLevel, m;
    f === "sandbox" && (m = (0, _chunkNQURTBEVMjs.fa)("#i" + t));
    let T = f === "sandbox" ? (0, _chunkNQURTBEVMjs.fa)(m.nodes()[0].contentDocument.body) : (0, _chunkNQURTBEVMjs.fa)("body"), W = f === "sandbox" ? m.nodes()[0].contentDocument : document, A = W.getElementById(t);
    $ = A.parentElement.offsetWidth, $ === void 0 && ($ = 1200), a.useWidth !== void 0 && ($ = a.useWidth);
    let E = i.db.getTasks(), F = [];
    for (let p of E)F.push(p.type);
    F = q(F);
    let j = {}, y = 2 * a.topPadding;
    if (i.db.getDisplayMode() === "compact" || a.displayMode === "compact") {
        let p = {};
        for (let b of E)p[b.section] === void 0 ? p[b.section] = [
            b
        ] : p[b.section].push(b);
        let v = 0;
        for (let b of Object.keys(p)){
            let w = Wn(p[b], v) + 1;
            v += w, y += w * (a.barHeight + a.barGap), j[b] = w;
        }
    } else {
        y += E.length * (a.barHeight + a.barGap);
        for (let p of F)j[p] = E.filter((v)=>v.type === p).length;
    }
    A.setAttribute("viewBox", "0 0 " + $ + " " + y);
    let _ = T.select(`[id="${t}"]`), g = (0, _chunkNQURTBEVMjs.ya)().domain([
        (0, _chunkNQURTBEVMjs.ca)(E, function(p) {
            return p.startTime;
        }),
        (0, _chunkNQURTBEVMjs.ba)(E, function(p) {
            return p.endTime;
        })
    ]).rangeRound([
        0,
        $ - a.leftPadding - a.rightPadding
    ]);
    function I(p, v) {
        let b = p.startTime, w = v.startTime, k = 0;
        return b > w ? k = 1 : b < w && (k = -1), k;
    }
    (0, _chunkGTKDMUJJMjs.a)(I, "taskCompare"), E.sort(I), z(E, $, y), (0, _chunkNQURTBEVMjs.M)(_, y, $, a.useMaxWidth), _.append("text").text(i.db.getDiagramTitle()).attr("x", $ / 2).attr("y", a.titleTopMargin).attr("class", "titleText");
    function z(p, v, b) {
        let w = a.barHeight, k = w + a.barGap, D = a.topPadding, c = a.leftPadding, l = (0, _chunkNQURTBEVMjs.ja)().domain([
            0,
            F.length
        ]).range([
            "#00B9FA",
            "#F95002"
        ]).interpolate((0, _chunkNQURTBEVMjs.ga));
        Q(k, D, c, v, b, p, i.db.getExcludes(), i.db.getIncludes()), X(c, D, v, b), N(p, k, D, c, w, l, v, b), R(k, D, c, w, l), B(c, D, v, b);
    }
    (0, _chunkGTKDMUJJMjs.a)(z, "makeGantt");
    function N(p, v, b, w, k, D, c) {
        let h = [
            ...new Set(p.map((d)=>d.order))
        ].map((d)=>p.find((n)=>n.order === d));
        _.append("g").selectAll("rect").data(h).enter().append("rect").attr("x", 0).attr("y", function(d, n) {
            return n = d.order, n * v + b - 2;
        }).attr("width", function() {
            return c - a.rightPadding / 2;
        }).attr("height", v).attr("class", function(d) {
            for (let [n, M] of F.entries())if (d.type === M) return "section section" + n % a.numberSectionStyles;
            return "section section0";
        });
        let u = _.append("g").selectAll("rect").data(p).enter(), x = i.db.getLinks();
        if (u.append("rect").attr("id", function(d) {
            return d.id;
        }).attr("rx", 3).attr("ry", 3).attr("x", function(d) {
            return d.milestone ? g(d.startTime) + w + .5 * (g(d.endTime) - g(d.startTime)) - .5 * k : g(d.startTime) + w;
        }).attr("y", function(d, n) {
            return n = d.order, n * v + b;
        }).attr("width", function(d) {
            return d.milestone ? k : g(d.renderEndTime || d.endTime) - g(d.startTime);
        }).attr("height", k).attr("transform-origin", function(d, n) {
            return n = d.order, (g(d.startTime) + w + .5 * (g(d.endTime) - g(d.startTime))).toString() + "px " + (n * v + b + .5 * k).toString() + "px";
        }).attr("class", function(d) {
            let n = "task", M = "";
            d.classes.length > 0 && (M = d.classes.join(" "));
            let C = 0;
            for (let [O, L] of F.entries())d.type === L && (C = O % a.numberSectionStyles);
            let S = "";
            return d.active ? d.crit ? S += " activeCrit" : S = " active" : d.done ? d.crit ? S = " doneCrit" : S = " done" : d.crit && (S += " crit"), S.length === 0 && (S = " task"), d.milestone && (S = " milestone " + S), S += C, S += " " + M, n + S;
        }), u.append("text").attr("id", function(d) {
            return d.id + "-text";
        }).text(function(d) {
            return d.task;
        }).attr("font-size", a.fontSize).attr("x", function(d) {
            let n = g(d.startTime), M = g(d.renderEndTime || d.endTime);
            d.milestone && (n += .5 * (g(d.endTime) - g(d.startTime)) - .5 * k), d.milestone && (M = n + k);
            let C = this.getBBox().width;
            return C > M - n ? M + C + 1.5 * a.leftPadding > c ? n + w - 5 : M + w + 5 : (M - n) / 2 + n + w;
        }).attr("y", function(d, n) {
            return n = d.order, n * v + a.barHeight / 2 + (a.fontSize / 2 - 2) + b;
        }).attr("text-height", k).attr("class", function(d) {
            let n = g(d.startTime), M = g(d.endTime);
            d.milestone && (M = n + k);
            let C = this.getBBox().width, S = "";
            d.classes.length > 0 && (S = d.classes.join(" "));
            let O = 0;
            for (let [te, Y] of F.entries())d.type === Y && (O = te % a.numberSectionStyles);
            let L = "";
            return d.active && (d.crit ? L = "activeCritText" + O : L = "activeText" + O), d.done ? d.crit ? L = L + " doneCritText" + O : L = L + " doneText" + O : d.crit && (L = L + " critText" + O), d.milestone && (L += " milestoneText"), C > M - n ? M + C + 1.5 * a.leftPadding > c ? S + " taskTextOutsideLeft taskTextOutside" + O + " " + L : S + " taskTextOutsideRight taskTextOutside" + O + " " + L + " width-" + C : S + " taskText taskText" + O + " " + L + " width-" + C;
        }), (0, _chunkNQURTBEVMjs.X)().securityLevel === "sandbox") {
            let d;
            d = (0, _chunkNQURTBEVMjs.fa)("#i" + t);
            let n = d.nodes()[0].contentDocument;
            u.filter(function(M) {
                return x.has(M.id);
            }).each(function(M) {
                var C = n.querySelector("#" + M.id), S = n.querySelector("#" + M.id + "-text");
                let O = C.parentNode;
                var L = n.createElement("a");
                L.setAttribute("xlink:href", x.get(M.id)), L.setAttribute("target", "_top"), O.appendChild(L), L.appendChild(C), L.appendChild(S);
            });
        }
    }
    (0, _chunkGTKDMUJJMjs.a)(N, "drawRects");
    function Q(p, v, b, w, k, D, c, l) {
        if (c.length === 0 && l.length === 0) return;
        let h, u;
        for (let { startTime: C, endTime: S } of D)(h === void 0 || C < h) && (h = C), (u === void 0 || S > u) && (u = S);
        if (!h || !u) return;
        if ((0, we.default)(u).diff((0, we.default)(h), "year") > 5) {
            (0, _chunkNQURTBEVMjs.b).warn("The difference between the min and max time is more than 5 years. This will cause performance issues. Skipping drawing exclude days.");
            return;
        }
        let x = i.db.getDateFormat(), s = [], d = null, n = (0, we.default)(h);
        for(; n.valueOf() <= u;)i.db.isInvalidDate(n, x, c, l) ? d ? d.end = n : d = {
            start: n,
            end: n
        } : d && (s.push(d), d = null), n = n.add(1, "d");
        _.append("g").selectAll("rect").data(s).enter().append("rect").attr("id", function(C) {
            return "exclude-" + C.start.format("YYYY-MM-DD");
        }).attr("x", function(C) {
            return g(C.start) + b;
        }).attr("y", a.gridLineStartPadding).attr("width", function(C) {
            let S = C.end.add(1, "day");
            return g(S) - g(C.start);
        }).attr("height", k - v - a.gridLineStartPadding).attr("transform-origin", function(C, S) {
            return (g(C.start) + b + .5 * (g(C.end) - g(C.start))).toString() + "px " + (S * p + .5 * k).toString() + "px";
        }).attr("class", "exclude-range");
    }
    (0, _chunkGTKDMUJJMjs.a)(Q, "drawExcludeDays");
    function X(p, v, b, w) {
        let k = (0, _chunkNQURTBEVMjs.ea)(g).tickSize(-w + v + a.gridLineStartPadding).tickFormat((0, _chunkNQURTBEVMjs.xa)(i.db.getAxisFormat() || a.axisFormat || "%Y-%m-%d")), c = /^([1-9]\d*)(millisecond|second|minute|hour|day|week|month)$/.exec(i.db.getTickInterval() || a.tickInterval);
        if (c !== null) {
            let l = c[1], h = c[2], u = i.db.getWeekday() || a.weekday;
            switch(h){
                case "millisecond":
                    k.ticks((0, _chunkNQURTBEVMjs.ka).every(l));
                    break;
                case "second":
                    k.ticks((0, _chunkNQURTBEVMjs.la).every(l));
                    break;
                case "minute":
                    k.ticks((0, _chunkNQURTBEVMjs.ma).every(l));
                    break;
                case "hour":
                    k.ticks((0, _chunkNQURTBEVMjs.na).every(l));
                    break;
                case "day":
                    k.ticks((0, _chunkNQURTBEVMjs.oa).every(l));
                    break;
                case "week":
                    k.ticks(qt[u].every(l));
                    break;
                case "month":
                    k.ticks((0, _chunkNQURTBEVMjs.wa).every(l));
                    break;
            }
        }
        if (_.append("g").attr("class", "grid").attr("transform", "translate(" + p + ", " + (w - 50) + ")").call(k).selectAll("text").style("text-anchor", "middle").attr("fill", "#000").attr("stroke", "none").attr("font-size", 10).attr("dy", "1em"), i.db.topAxisEnabled() || a.topAxis) {
            let l = (0, _chunkNQURTBEVMjs.da)(g).tickSize(-w + v + a.gridLineStartPadding).tickFormat((0, _chunkNQURTBEVMjs.xa)(i.db.getAxisFormat() || a.axisFormat || "%Y-%m-%d"));
            if (c !== null) {
                let h = c[1], u = c[2], x = i.db.getWeekday() || a.weekday;
                switch(u){
                    case "millisecond":
                        l.ticks((0, _chunkNQURTBEVMjs.ka).every(h));
                        break;
                    case "second":
                        l.ticks((0, _chunkNQURTBEVMjs.la).every(h));
                        break;
                    case "minute":
                        l.ticks((0, _chunkNQURTBEVMjs.ma).every(h));
                        break;
                    case "hour":
                        l.ticks((0, _chunkNQURTBEVMjs.na).every(h));
                        break;
                    case "day":
                        l.ticks((0, _chunkNQURTBEVMjs.oa).every(h));
                        break;
                    case "week":
                        l.ticks(qt[x].every(h));
                        break;
                    case "month":
                        l.ticks((0, _chunkNQURTBEVMjs.wa).every(h));
                        break;
                }
            }
            _.append("g").attr("class", "grid").attr("transform", "translate(" + p + ", " + v + ")").call(l).selectAll("text").style("text-anchor", "middle").attr("fill", "#000").attr("stroke", "none").attr("font-size", 10);
        }
    }
    (0, _chunkGTKDMUJJMjs.a)(X, "makeGrid");
    function R(p, v) {
        let b = 0, w = Object.keys(j).map((k)=>[
                k,
                j[k]
            ]);
        _.append("g").selectAll("text").data(w).enter().append(function(k) {
            let D = k[0].split((0, _chunkNQURTBEVMjs.L).lineBreakRegex), c = -(D.length - 1) / 2, l = W.createElementNS("http://www.w3.org/2000/svg", "text");
            l.setAttribute("dy", c + "em");
            for (let [h, u] of D.entries()){
                let x = W.createElementNS("http://www.w3.org/2000/svg", "tspan");
                x.setAttribute("alignment-baseline", "central"), x.setAttribute("x", "10"), h > 0 && x.setAttribute("dy", "1em"), x.textContent = u, l.appendChild(x);
            }
            return l;
        }).attr("x", 10).attr("y", function(k, D) {
            if (D > 0) for(let c = 0; c < D; c++)return b += w[D - 1][1], k[1] * p / 2 + b * p + v;
            else return k[1] * p / 2 + v;
        }).attr("font-size", a.sectionFontSize).attr("class", function(k) {
            for (let [D, c] of F.entries())if (k[0] === c) return "sectionTitle sectionTitle" + D % a.numberSectionStyles;
            return "sectionTitle";
        });
    }
    (0, _chunkGTKDMUJJMjs.a)(R, "vertLabels");
    function B(p, v, b, w) {
        let k = i.db.getTodayMarker();
        if (k === "off") return;
        let D = _.append("g").attr("class", "today"), c = new Date, l = D.append("line");
        l.attr("x1", g(c) + p).attr("x2", g(c) + p).attr("y1", a.titleTopMargin).attr("y2", w - a.titleTopMargin).attr("class", "today"), k !== "" && l.attr("style", k.replace(/,/g, ";"));
    }
    (0, _chunkGTKDMUJJMjs.a)(B, "drawToday");
    function q(p) {
        let v = {}, b = [];
        for(let w = 0, k = p.length; w < k; ++w)Object.prototype.hasOwnProperty.call(v, p[w]) || (v[p[w]] = !0, b.push(p[w]));
        return b;
    }
    (0, _chunkGTKDMUJJMjs.a)(q, "checkUnique");
}, "draw"), Ut = {
    setConf: Yn,
    draw: Fn
};
var Vn = (0, _chunkGTKDMUJJMjs.a)((e)=>`
  .mermaid-main-font {
    font-family: var(--mermaid-font-family, "trebuchet ms", verdana, arial, sans-serif);
  }

  .exclude-range {
    fill: ${e.excludeBkgColor};
  }

  .section {
    stroke: none;
    opacity: 0.2;
  }

  .section0 {
    fill: ${e.sectionBkgColor};
  }

  .section2 {
    fill: ${e.sectionBkgColor2};
  }

  .section1,
  .section3 {
    fill: ${e.altSectionBkgColor};
    opacity: 0.2;
  }

  .sectionTitle0 {
    fill: ${e.titleColor};
  }

  .sectionTitle1 {
    fill: ${e.titleColor};
  }

  .sectionTitle2 {
    fill: ${e.titleColor};
  }

  .sectionTitle3 {
    fill: ${e.titleColor};
  }

  .sectionTitle {
    text-anchor: start;
    font-family: var(--mermaid-font-family, "trebuchet ms", verdana, arial, sans-serif);
  }


  /* Grid and axis */

  .grid .tick {
    stroke: ${e.gridColor};
    opacity: 0.8;
    shape-rendering: crispEdges;
  }

  .grid .tick text {
    font-family: ${e.fontFamily};
    fill: ${e.textColor};
  }

  .grid path {
    stroke-width: 0;
  }


  /* Today line */

  .today {
    fill: none;
    stroke: ${e.todayLineColor};
    stroke-width: 2px;
  }


  /* Task styling */

  /* Default task */

  .task {
    stroke-width: 2;
  }

  .taskText {
    text-anchor: middle;
    font-family: var(--mermaid-font-family, "trebuchet ms", verdana, arial, sans-serif);
  }

  .taskTextOutsideRight {
    fill: ${e.taskTextDarkColor};
    text-anchor: start;
    font-family: var(--mermaid-font-family, "trebuchet ms", verdana, arial, sans-serif);
  }

  .taskTextOutsideLeft {
    fill: ${e.taskTextDarkColor};
    text-anchor: end;
  }


  /* Special case clickable */

  .task.clickable {
    cursor: pointer;
  }

  .taskText.clickable {
    cursor: pointer;
    fill: ${e.taskTextClickableColor} !important;
    font-weight: bold;
  }

  .taskTextOutsideLeft.clickable {
    cursor: pointer;
    fill: ${e.taskTextClickableColor} !important;
    font-weight: bold;
  }

  .taskTextOutsideRight.clickable {
    cursor: pointer;
    fill: ${e.taskTextClickableColor} !important;
    font-weight: bold;
  }


  /* Specific task settings for the sections*/

  .taskText0,
  .taskText1,
  .taskText2,
  .taskText3 {
    fill: ${e.taskTextColor};
  }

  .task0,
  .task1,
  .task2,
  .task3 {
    fill: ${e.taskBkgColor};
    stroke: ${e.taskBorderColor};
  }

  .taskTextOutside0,
  .taskTextOutside2
  {
    fill: ${e.taskTextOutsideColor};
  }

  .taskTextOutside1,
  .taskTextOutside3 {
    fill: ${e.taskTextOutsideColor};
  }


  /* Active task */

  .active0,
  .active1,
  .active2,
  .active3 {
    fill: ${e.activeTaskBkgColor};
    stroke: ${e.activeTaskBorderColor};
  }

  .activeText0,
  .activeText1,
  .activeText2,
  .activeText3 {
    fill: ${e.taskTextDarkColor} !important;
  }


  /* Completed task */

  .done0,
  .done1,
  .done2,
  .done3 {
    stroke: ${e.doneTaskBorderColor};
    fill: ${e.doneTaskBkgColor};
    stroke-width: 2;
  }

  .doneText0,
  .doneText1,
  .doneText2,
  .doneText3 {
    fill: ${e.taskTextDarkColor} !important;
  }


  /* Tasks on the critical line */

  .crit0,
  .crit1,
  .crit2,
  .crit3 {
    stroke: ${e.critBorderColor};
    fill: ${e.critBkgColor};
    stroke-width: 2;
  }

  .activeCrit0,
  .activeCrit1,
  .activeCrit2,
  .activeCrit3 {
    stroke: ${e.critBorderColor};
    fill: ${e.activeTaskBkgColor};
    stroke-width: 2;
  }

  .doneCrit0,
  .doneCrit1,
  .doneCrit2,
  .doneCrit3 {
    stroke: ${e.critBorderColor};
    fill: ${e.doneTaskBkgColor};
    stroke-width: 2;
    cursor: pointer;
    shape-rendering: crispEdges;
  }

  .milestone {
    transform: rotate(45deg) scale(0.8,0.8);
  }

  .milestoneText {
    font-style: italic;
  }
  .doneCritText0,
  .doneCritText1,
  .doneCritText2,
  .doneCritText3 {
    fill: ${e.taskTextDarkColor} !important;
  }

  .activeCritText0,
  .activeCritText1,
  .activeCritText2,
  .activeCritText3 {
    fill: ${e.taskTextDarkColor} !important;
  }

  .titleText {
    text-anchor: middle;
    font-size: 18px;
    fill: ${e.titleColor || e.textColor};
    font-family: var(--mermaid-font-family, "trebuchet ms", verdana, arial, sans-serif);
  }
`, "getStyles"), Zt = Vn;
var oi = {
    parser: St,
    db: Ht,
    renderer: Ut,
    styles: Zt
};

},{"./chunk-AC3VT7B7.mjs":"1eiUz","./chunk-TI4EEUUG.mjs":"8SKrN","./chunk-NQURTBEV.mjs":"iASFe","./chunk-6BY5RJGC.mjs":"bRXnR","./chunk-GTKDMUJJ.mjs":"fruhx","@parcel/transformer-js/src/esmodule-helpers.js":"1cdyk"}]},["jL2qi"], null, "parcelRequire6955", {})

//# sourceMappingURL=ganttDiagram-6SR64PWN.a6286bc3.js.map
