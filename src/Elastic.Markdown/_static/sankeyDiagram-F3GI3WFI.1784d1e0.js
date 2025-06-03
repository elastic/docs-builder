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
})({"7UpjM":[function(require,module,exports,__globalThis) {
var global = arguments[3];
var HMR_HOST = null;
var HMR_PORT = 1234;
var HMR_SERVER_PORT = 1234;
var HMR_SECURE = false;
var HMR_ENV_HASH = "febfef3c5b467b16";
var HMR_USE_SSE = false;
module.bundle.HMR_BUNDLE_ID = "561ed49a1784d1e0";
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

},{}],"afGss":[function(require,module,exports,__globalThis) {
var parcelHelpers = require("@parcel/transformer-js/src/esmodule-helpers.js");
parcelHelpers.defineInteropFlag(exports);
parcelHelpers.export(exports, "diagram", ()=>wn);
var _chunkNQURTBEVMjs = require("./chunk-NQURTBEV.mjs");
var _chunkGTKDMUJJMjs = require("./chunk-GTKDMUJJ.mjs");
var lt = function() {
    var t = (0, _chunkGTKDMUJJMjs.a)(function(x, o, a, l) {
        for(a = a || {}, l = x.length; l--; a[x[l]] = o);
        return a;
    }, "o"), r = [
        1,
        9
    ], i = [
        1,
        10
    ], u = [
        1,
        5,
        10,
        12
    ], c = {
        trace: (0, _chunkGTKDMUJJMjs.a)(function() {}, "trace"),
        yy: {},
        symbols_: {
            error: 2,
            start: 3,
            SANKEY: 4,
            NEWLINE: 5,
            csv: 6,
            opt_eof: 7,
            record: 8,
            csv_tail: 9,
            EOF: 10,
            "field[source]": 11,
            COMMA: 12,
            "field[target]": 13,
            "field[value]": 14,
            field: 15,
            escaped: 16,
            non_escaped: 17,
            DQUOTE: 18,
            ESCAPED_TEXT: 19,
            NON_ESCAPED_TEXT: 20,
            $accept: 0,
            $end: 1
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
        performAction: (0, _chunkGTKDMUJJMjs.a)(function(o, a, l, k, _, p, v) {
            var C = p.length - 1;
            switch(_){
                case 7:
                    let E = k.findOrCreateNode(p[C - 4].trim().replaceAll('""', '"')), M = k.findOrCreateNode(p[C - 2].trim().replaceAll('""', '"')), D = parseFloat(p[C].trim());
                    k.addLink(E, M, D);
                    break;
                case 8:
                case 9:
                case 11:
                    this.$ = p[C];
                    break;
                case 10:
                    this.$ = p[C - 1];
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
                18: r,
                20: i
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
            t(i, [
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
            t(u, [
                2,
                8
            ]),
            t(u, [
                2,
                9
            ]),
            {
                19: [
                    1,
                    16
                ]
            },
            t(u, [
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
            t(i, [
                2,
                2
            ]),
            {
                6: 17,
                8: 5,
                15: 6,
                16: 7,
                17: 8,
                18: r,
                20: i
            },
            {
                15: 18,
                16: 7,
                17: 8,
                18: r,
                20: i
            },
            {
                18: [
                    1,
                    19
                ]
            },
            t(i, [
                2,
                3
            ]),
            {
                12: [
                    1,
                    20
                ]
            },
            t(u, [
                2,
                10
            ]),
            {
                15: 21,
                16: 7,
                17: 8,
                18: r,
                20: i
            },
            t([
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
        parseError: (0, _chunkGTKDMUJJMjs.a)(function(o, a) {
            if (a.recoverable) this.trace(o);
            else {
                var l = new Error(o);
                throw l.hash = a, l;
            }
        }, "parseError"),
        parse: (0, _chunkGTKDMUJJMjs.a)(function(o) {
            var a = this, l = [
                0
            ], k = [], _ = [
                null
            ], p = [], v = this.table, C = "", E = 0, M = 0, D = 0, z = 2, B = 1, R = p.slice.call(arguments, 1), w = Object.create(this.lexer), N = {
                yy: {}
            };
            for(var P in this.yy)Object.prototype.hasOwnProperty.call(this.yy, P) && (N.yy[P] = this.yy[P]);
            w.setInput(o, N.yy), N.yy.lexer = w, N.yy.parser = this, typeof w.yylloc > "u" && (w.yylloc = {});
            var O = w.yylloc;
            p.push(O);
            var y = w.options && w.options.ranges;
            typeof N.yy.parseError == "function" ? this.parseError = N.yy.parseError : this.parseError = Object.getPrototypeOf(this).parseError;
            function T(L) {
                l.length = l.length - 2 * L, _.length = _.length - L, p.length = p.length - L;
            }
            (0, _chunkGTKDMUJJMjs.a)(T, "popStack");
            function st() {
                var L;
                return L = k.pop() || w.lex() || B, typeof L != "number" && (L instanceof Array && (k = L, L = k.pop()), L = a.symbols_[L] || L), L;
            }
            (0, _chunkGTKDMUJJMjs.a)(st, "lex");
            for(var A, Y, n, f, h, d, s = {}, g, b, $, I;;){
                if (n = l[l.length - 1], this.defaultActions[n] ? f = this.defaultActions[n] : ((A === null || typeof A > "u") && (A = st()), f = v[n] && v[n][A]), typeof f > "u" || !f.length || !f[0]) {
                    var j = "";
                    I = [];
                    for(g in v[n])this.terminals_[g] && g > z && I.push("'" + this.terminals_[g] + "'");
                    w.showPosition ? j = "Parse error on line " + (E + 1) + `:
` + w.showPosition() + `
Expecting ` + I.join(", ") + ", got '" + (this.terminals_[A] || A) + "'" : j = "Parse error on line " + (E + 1) + ": Unexpected " + (A == B ? "end of input" : "'" + (this.terminals_[A] || A) + "'"), this.parseError(j, {
                        text: w.match,
                        token: this.terminals_[A] || A,
                        line: w.yylineno,
                        loc: O,
                        expected: I
                    });
                }
                if (f[0] instanceof Array && f.length > 1) throw new Error("Parse Error: multiple actions possible at state: " + n + ", token: " + A);
                switch(f[0]){
                    case 1:
                        l.push(A), _.push(w.yytext), p.push(w.yylloc), l.push(f[1]), A = null, Y ? (A = Y, Y = null) : (M = w.yyleng, C = w.yytext, E = w.yylineno, O = w.yylloc, D > 0 && D--);
                        break;
                    case 2:
                        if (b = this.productions_[f[1]][1], s.$ = _[_.length - b], s._$ = {
                            first_line: p[p.length - (b || 1)].first_line,
                            last_line: p[p.length - 1].last_line,
                            first_column: p[p.length - (b || 1)].first_column,
                            last_column: p[p.length - 1].last_column
                        }, y && (s._$.range = [
                            p[p.length - (b || 1)].range[0],
                            p[p.length - 1].range[1]
                        ]), d = this.performAction.apply(s, [
                            C,
                            M,
                            E,
                            N.yy,
                            f[1],
                            _,
                            p
                        ].concat(R)), typeof d < "u") return d;
                        b && (l = l.slice(0, -1 * b * 2), _ = _.slice(0, -1 * b), p = p.slice(0, -1 * b)), l.push(this.productions_[f[1]][0]), _.push(s.$), p.push(s._$), $ = v[l[l.length - 2]][l[l.length - 1]], l.push($);
                        break;
                    case 3:
                        return !0;
                }
            }
            return !0;
        }, "parse")
    }, S = function() {
        var x = {
            EOF: 1,
            parseError: (0, _chunkGTKDMUJJMjs.a)(function(a, l) {
                if (this.yy.parser) this.yy.parser.parseError(a, l);
                else throw new Error(a);
            }, "parseError"),
            setInput: (0, _chunkGTKDMUJJMjs.a)(function(o, a) {
                return this.yy = a || this.yy || {}, this._input = o, this._more = this._backtrack = this.done = !1, this.yylineno = this.yyleng = 0, this.yytext = this.matched = this.match = "", this.conditionStack = [
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
                var o = this._input[0];
                this.yytext += o, this.yyleng++, this.offset++, this.match += o, this.matched += o;
                var a = o.match(/(?:\r\n?|\n).*/g);
                return a ? (this.yylineno++, this.yylloc.last_line++) : this.yylloc.last_column++, this.options.ranges && this.yylloc.range[1]++, this._input = this._input.slice(1), o;
            }, "input"),
            unput: (0, _chunkGTKDMUJJMjs.a)(function(o) {
                var a = o.length, l = o.split(/(?:\r\n?|\n)/g);
                this._input = o + this._input, this.yytext = this.yytext.substr(0, this.yytext.length - a), this.offset -= a;
                var k = this.match.split(/(?:\r\n?|\n)/g);
                this.match = this.match.substr(0, this.match.length - 1), this.matched = this.matched.substr(0, this.matched.length - 1), l.length - 1 && (this.yylineno -= l.length - 1);
                var _ = this.yylloc.range;
                return this.yylloc = {
                    first_line: this.yylloc.first_line,
                    last_line: this.yylineno + 1,
                    first_column: this.yylloc.first_column,
                    last_column: l ? (l.length === k.length ? this.yylloc.first_column : 0) + k[k.length - l.length].length - l[0].length : this.yylloc.first_column - a
                }, this.options.ranges && (this.yylloc.range = [
                    _[0],
                    _[0] + this.yyleng - a
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
            less: (0, _chunkGTKDMUJJMjs.a)(function(o) {
                this.unput(this.match.slice(o));
            }, "less"),
            pastInput: (0, _chunkGTKDMUJJMjs.a)(function() {
                var o = this.matched.substr(0, this.matched.length - this.match.length);
                return (o.length > 20 ? "..." : "") + o.substr(-20).replace(/\n/g, "");
            }, "pastInput"),
            upcomingInput: (0, _chunkGTKDMUJJMjs.a)(function() {
                var o = this.match;
                return o.length < 20 && (o += this._input.substr(0, 20 - o.length)), (o.substr(0, 20) + (o.length > 20 ? "..." : "")).replace(/\n/g, "");
            }, "upcomingInput"),
            showPosition: (0, _chunkGTKDMUJJMjs.a)(function() {
                var o = this.pastInput(), a = new Array(o.length + 1).join("-");
                return o + this.upcomingInput() + `
` + a + "^";
            }, "showPosition"),
            test_match: (0, _chunkGTKDMUJJMjs.a)(function(o, a) {
                var l, k, _;
                if (this.options.backtrack_lexer && (_ = {
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
                }, this.options.ranges && (_.yylloc.range = this.yylloc.range.slice(0))), k = o[0].match(/(?:\r\n?|\n).*/g), k && (this.yylineno += k.length), this.yylloc = {
                    first_line: this.yylloc.last_line,
                    last_line: this.yylineno + 1,
                    first_column: this.yylloc.last_column,
                    last_column: k ? k[k.length - 1].length - k[k.length - 1].match(/\r?\n?/)[0].length : this.yylloc.last_column + o[0].length
                }, this.yytext += o[0], this.match += o[0], this.matches = o, this.yyleng = this.yytext.length, this.options.ranges && (this.yylloc.range = [
                    this.offset,
                    this.offset += this.yyleng
                ]), this._more = !1, this._backtrack = !1, this._input = this._input.slice(o[0].length), this.matched += o[0], l = this.performAction.call(this, this.yy, this, a, this.conditionStack[this.conditionStack.length - 1]), this.done && this._input && (this.done = !1), l) return l;
                if (this._backtrack) {
                    for(var p in _)this[p] = _[p];
                    return !1;
                }
                return !1;
            }, "test_match"),
            next: (0, _chunkGTKDMUJJMjs.a)(function() {
                if (this.done) return this.EOF;
                this._input || (this.done = !0);
                var o, a, l, k;
                this._more || (this.yytext = "", this.match = "");
                for(var _ = this._currentRules(), p = 0; p < _.length; p++)if (l = this._input.match(this.rules[_[p]]), l && (!a || l[0].length > a[0].length)) {
                    if (a = l, k = p, this.options.backtrack_lexer) {
                        if (o = this.test_match(l, _[p]), o !== !1) return o;
                        if (this._backtrack) {
                            a = !1;
                            continue;
                        } else return !1;
                    } else if (!this.options.flex) break;
                }
                return a ? (o = this.test_match(a, _[k]), o !== !1 ? o : !1) : this._input === "" ? this.EOF : this.parseError("Lexical error on line " + (this.yylineno + 1) + `. Unrecognized text.
` + this.showPosition(), {
                    text: "",
                    token: null,
                    line: this.yylineno
                });
            }, "next"),
            lex: (0, _chunkGTKDMUJJMjs.a)(function() {
                var a = this.next();
                return a || this.lex();
            }, "lex"),
            begin: (0, _chunkGTKDMUJJMjs.a)(function(a) {
                this.conditionStack.push(a);
            }, "begin"),
            popState: (0, _chunkGTKDMUJJMjs.a)(function() {
                var a = this.conditionStack.length - 1;
                return a > 0 ? this.conditionStack.pop() : this.conditionStack[0];
            }, "popState"),
            _currentRules: (0, _chunkGTKDMUJJMjs.a)(function() {
                return this.conditionStack.length && this.conditionStack[this.conditionStack.length - 1] ? this.conditions[this.conditionStack[this.conditionStack.length - 1]].rules : this.conditions.INITIAL.rules;
            }, "_currentRules"),
            topState: (0, _chunkGTKDMUJJMjs.a)(function(a) {
                return a = this.conditionStack.length - 1 - Math.abs(a || 0), a >= 0 ? this.conditionStack[a] : "INITIAL";
            }, "topState"),
            pushState: (0, _chunkGTKDMUJJMjs.a)(function(a) {
                this.begin(a);
            }, "pushState"),
            stateStackSize: (0, _chunkGTKDMUJJMjs.a)(function() {
                return this.conditionStack.length;
            }, "stateStackSize"),
            options: {
                "case-insensitive": !0
            },
            performAction: (0, _chunkGTKDMUJJMjs.a)(function(a, l, k, _) {
                var p = _;
                switch(k){
                    case 0:
                        return this.pushState("csv"), 4;
                    case 1:
                        return 10;
                    case 2:
                        return 5;
                    case 3:
                        return 12;
                    case 4:
                        return this.pushState("escaped_text"), 18;
                    case 5:
                        return 20;
                    case 6:
                        return this.popState("escaped_text"), 18;
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
                csv: {
                    rules: [
                        1,
                        2,
                        3,
                        4,
                        5,
                        6,
                        7
                    ],
                    inclusive: !1
                },
                escaped_text: {
                    rules: [
                        6,
                        7
                    ],
                    inclusive: !1
                },
                INITIAL: {
                    rules: [
                        0,
                        1,
                        2,
                        3,
                        4,
                        5,
                        6,
                        7
                    ],
                    inclusive: !0
                }
            }
        };
        return x;
    }();
    c.lexer = S;
    function m() {
        this.yy = {};
    }
    return (0, _chunkGTKDMUJJMjs.a)(m, "Parser"), m.prototype = c, c.Parser = m, new m;
}();
lt.parser = lt;
var G = lt;
var tt = [], et = [], Z = new Map, Ft = (0, _chunkGTKDMUJJMjs.a)(()=>{
    tt = [], et = [], Z = new Map, (0, _chunkNQURTBEVMjs.P)();
}, "clear"), ut = class {
    constructor(r, i, u = 0){
        this.source = r;
        this.target = i;
        this.value = u;
    }
    static{
        (0, _chunkGTKDMUJJMjs.a)(this, "SankeyLink");
    }
}, Ht = (0, _chunkGTKDMUJJMjs.a)((t, r, i)=>{
    tt.push(new ut(t, r, i));
}, "addLink"), ft = class {
    constructor(r){
        this.ID = r;
    }
    static{
        (0, _chunkGTKDMUJJMjs.a)(this, "SankeyNode");
    }
}, Yt = (0, _chunkGTKDMUJJMjs.a)((t)=>{
    t = (0, _chunkNQURTBEVMjs.L).sanitizeText(t, (0, _chunkNQURTBEVMjs.X)());
    let r = Z.get(t);
    return r === void 0 && (r = new ft(t), Z.set(t, r), et.push(r)), r;
}, "findOrCreateNode"), qt = (0, _chunkGTKDMUJJMjs.a)(()=>et, "getNodes"), Ut = (0, _chunkGTKDMUJJMjs.a)(()=>tt, "getLinks"), Xt = (0, _chunkGTKDMUJJMjs.a)(()=>({
        nodes: et.map((t)=>({
                id: t.ID
            })),
        links: tt.map((t)=>({
                source: t.source.ID,
                target: t.target.ID,
                value: t.value
            }))
    }), "getGraph"), Ot = {
    nodesMap: Z,
    getConfig: (0, _chunkGTKDMUJJMjs.a)(()=>(0, _chunkNQURTBEVMjs.X)().sankey, "getConfig"),
    getNodes: qt,
    getLinks: Ut,
    getGraph: Xt,
    addLink: Ht,
    findOrCreateNode: Yt,
    getAccTitle: (0, _chunkNQURTBEVMjs.R),
    setAccTitle: (0, _chunkNQURTBEVMjs.Q),
    getAccDescription: (0, _chunkNQURTBEVMjs.T),
    setAccDescription: (0, _chunkNQURTBEVMjs.S),
    getDiagramTitle: (0, _chunkNQURTBEVMjs.V),
    setDiagramTitle: (0, _chunkNQURTBEVMjs.U),
    clear: Ft
};
function J(t, r) {
    let i;
    if (r === void 0) for (let u of t)u != null && (i < u || i === void 0 && u >= u) && (i = u);
    else {
        let u = -1;
        for (let c of t)(c = r(c, ++u, t)) != null && (i < c || i === void 0 && c >= c) && (i = c);
    }
    return i;
}
(0, _chunkGTKDMUJJMjs.a)(J, "max");
function W(t, r) {
    let i;
    if (r === void 0) for (let u of t)u != null && (i > u || i === void 0 && u >= u) && (i = u);
    else {
        let u = -1;
        for (let c of t)(c = r(c, ++u, t)) != null && (i > c || i === void 0 && c >= c) && (i = c);
    }
    return i;
}
(0, _chunkGTKDMUJJMjs.a)(W, "min");
function F(t, r) {
    let i = 0;
    if (r === void 0) for (let u of t)(u = +u) && (i += u);
    else {
        let u = -1;
        for (let c of t)(c = +r(c, ++u, t)) && (i += c);
    }
    return i;
}
(0, _chunkGTKDMUJJMjs.a)(F, "sum");
function Gt(t) {
    return t.target.depth;
}
(0, _chunkGTKDMUJJMjs.a)(Gt, "targetDepth");
function ct(t) {
    return t.depth;
}
(0, _chunkGTKDMUJJMjs.a)(ct, "left");
function ht(t, r) {
    return r - 1 - t.height;
}
(0, _chunkGTKDMUJJMjs.a)(ht, "right");
function Q(t, r) {
    return t.sourceLinks.length ? t.depth : r - 1;
}
(0, _chunkGTKDMUJJMjs.a)(Q, "justify");
function dt(t) {
    return t.targetLinks.length ? t.depth : t.sourceLinks.length ? W(t.sourceLinks, Gt) - 1 : 0;
}
(0, _chunkGTKDMUJJMjs.a)(dt, "center");
function H(t) {
    return function() {
        return t;
    };
}
(0, _chunkGTKDMUJJMjs.a)(H, "constant");
function It(t, r) {
    return nt(t.source, r.source) || t.index - r.index;
}
(0, _chunkGTKDMUJJMjs.a)(It, "ascendingSourceBreadth");
function Dt(t, r) {
    return nt(t.target, r.target) || t.index - r.index;
}
(0, _chunkGTKDMUJJMjs.a)(Dt, "ascendingTargetBreadth");
function nt(t, r) {
    return t.y0 - r.y0;
}
(0, _chunkGTKDMUJJMjs.a)(nt, "ascendingBreadth");
function pt(t) {
    return t.value;
}
(0, _chunkGTKDMUJJMjs.a)(pt, "value");
function Jt(t) {
    return t.index;
}
(0, _chunkGTKDMUJJMjs.a)(Jt, "defaultId");
function Qt(t) {
    return t.nodes;
}
(0, _chunkGTKDMUJJMjs.a)(Qt, "defaultNodes");
function Kt(t) {
    return t.links;
}
(0, _chunkGTKDMUJJMjs.a)(Kt, "defaultLinks");
function Pt(t, r) {
    let i = t.get(r);
    if (!i) throw new Error("missing: " + r);
    return i;
}
(0, _chunkGTKDMUJJMjs.a)(Pt, "find");
function Rt({ nodes: t }) {
    for (let r of t){
        let i = r.y0, u = i;
        for (let c of r.sourceLinks)c.y0 = i + c.width / 2, i += c.width;
        for (let c of r.targetLinks)c.y1 = u + c.width / 2, u += c.width;
    }
}
(0, _chunkGTKDMUJJMjs.a)(Rt, "computeLinkBreadths");
function rt() {
    let t = 0, r = 0, i = 1, u = 1, c = 24, S = 8, m, x = Jt, o = Q, a, l, k = Qt, _ = Kt, p = 6;
    function v() {
        let n = {
            nodes: k.apply(null, arguments),
            links: _.apply(null, arguments)
        };
        return C(n), E(n), M(n), D(n), R(n), Rt(n), n;
    }
    (0, _chunkGTKDMUJJMjs.a)(v, "sankey"), v.update = function(n) {
        return Rt(n), n;
    }, v.nodeId = function(n) {
        return arguments.length ? (x = typeof n == "function" ? n : H(n), v) : x;
    }, v.nodeAlign = function(n) {
        return arguments.length ? (o = typeof n == "function" ? n : H(n), v) : o;
    }, v.nodeSort = function(n) {
        return arguments.length ? (a = n, v) : a;
    }, v.nodeWidth = function(n) {
        return arguments.length ? (c = +n, v) : c;
    }, v.nodePadding = function(n) {
        return arguments.length ? (S = m = +n, v) : S;
    }, v.nodes = function(n) {
        return arguments.length ? (k = typeof n == "function" ? n : H(n), v) : k;
    }, v.links = function(n) {
        return arguments.length ? (_ = typeof n == "function" ? n : H(n), v) : _;
    }, v.linkSort = function(n) {
        return arguments.length ? (l = n, v) : l;
    }, v.size = function(n) {
        return arguments.length ? (t = r = 0, i = +n[0], u = +n[1], v) : [
            i - t,
            u - r
        ];
    }, v.extent = function(n) {
        return arguments.length ? (t = +n[0][0], i = +n[1][0], r = +n[0][1], u = +n[1][1], v) : [
            [
                t,
                r
            ],
            [
                i,
                u
            ]
        ];
    }, v.iterations = function(n) {
        return arguments.length ? (p = +n, v) : p;
    };
    function C({ nodes: n, links: f }) {
        for (let [d, s] of n.entries())s.index = d, s.sourceLinks = [], s.targetLinks = [];
        let h = new Map(n.map((d, s)=>[
                x(d, s, n),
                d
            ]));
        for (let [d, s] of f.entries()){
            s.index = d;
            let { source: g, target: b } = s;
            typeof g != "object" && (g = s.source = Pt(h, g)), typeof b != "object" && (b = s.target = Pt(h, b)), g.sourceLinks.push(s), b.targetLinks.push(s);
        }
        if (l != null) for (let { sourceLinks: d, targetLinks: s } of n)d.sort(l), s.sort(l);
    }
    (0, _chunkGTKDMUJJMjs.a)(C, "computeNodeLinks");
    function E({ nodes: n }) {
        for (let f of n)f.value = f.fixedValue === void 0 ? Math.max(F(f.sourceLinks, pt), F(f.targetLinks, pt)) : f.fixedValue;
    }
    (0, _chunkGTKDMUJJMjs.a)(E, "computeNodeValues");
    function M({ nodes: n }) {
        let f = n.length, h = new Set(n), d = new Set, s = 0;
        for(; h.size;){
            for (let g of h){
                g.depth = s;
                for (let { target: b } of g.sourceLinks)d.add(b);
            }
            if (++s > f) throw new Error("circular link");
            h = d, d = new Set;
        }
    }
    (0, _chunkGTKDMUJJMjs.a)(M, "computeNodeDepths");
    function D({ nodes: n }) {
        let f = n.length, h = new Set(n), d = new Set, s = 0;
        for(; h.size;){
            for (let g of h){
                g.height = s;
                for (let { source: b } of g.targetLinks)d.add(b);
            }
            if (++s > f) throw new Error("circular link");
            h = d, d = new Set;
        }
    }
    (0, _chunkGTKDMUJJMjs.a)(D, "computeNodeHeights");
    function z({ nodes: n }) {
        let f = J(n, (s)=>s.depth) + 1, h = (i - t - c) / (f - 1), d = new Array(f);
        for (let s of n){
            let g = Math.max(0, Math.min(f - 1, Math.floor(o.call(null, s, f))));
            s.layer = g, s.x0 = t + g * h, s.x1 = s.x0 + c, d[g] ? d[g].push(s) : d[g] = [
                s
            ];
        }
        if (a) for (let s of d)s.sort(a);
        return d;
    }
    (0, _chunkGTKDMUJJMjs.a)(z, "computeNodeLayers");
    function B(n) {
        let f = W(n, (h)=>(u - r - (h.length - 1) * m) / F(h, pt));
        for (let h of n){
            let d = r;
            for (let s of h){
                s.y0 = d, s.y1 = d + s.value * f, d = s.y1 + m;
                for (let g of s.sourceLinks)g.width = g.value * f;
            }
            d = (u - d + m) / (h.length + 1);
            for(let s = 0; s < h.length; ++s){
                let g = h[s];
                g.y0 += d * (s + 1), g.y1 += d * (s + 1);
            }
            st(h);
        }
    }
    (0, _chunkGTKDMUJJMjs.a)(B, "initializeNodeBreadths");
    function R(n) {
        let f = z(n);
        m = Math.min(S, (u - r) / (J(f, (h)=>h.length) - 1)), B(f);
        for(let h = 0; h < p; ++h){
            let d = Math.pow(.99, h), s = Math.max(1 - d, (h + 1) / p);
            N(f, d, s), w(f, d, s);
        }
    }
    (0, _chunkGTKDMUJJMjs.a)(R, "computeNodeBreadths");
    function w(n, f, h) {
        for(let d = 1, s = n.length; d < s; ++d){
            let g = n[d];
            for (let b of g){
                let $ = 0, I = 0;
                for (let { source: L, value: at } of b.targetLinks){
                    let q = at * (b.layer - L.layer);
                    $ += A(L, b) * q, I += q;
                }
                if (!(I > 0)) continue;
                let j = ($ / I - b.y0) * f;
                b.y0 += j, b.y1 += j, T(b);
            }
            a === void 0 && g.sort(nt), P(g, h);
        }
    }
    (0, _chunkGTKDMUJJMjs.a)(w, "relaxLeftToRight");
    function N(n, f, h) {
        for(let d = n.length, s = d - 2; s >= 0; --s){
            let g = n[s];
            for (let b of g){
                let $ = 0, I = 0;
                for (let { target: L, value: at } of b.sourceLinks){
                    let q = at * (L.layer - b.layer);
                    $ += Y(b, L) * q, I += q;
                }
                if (!(I > 0)) continue;
                let j = ($ / I - b.y0) * f;
                b.y0 += j, b.y1 += j, T(b);
            }
            a === void 0 && g.sort(nt), P(g, h);
        }
    }
    (0, _chunkGTKDMUJJMjs.a)(N, "relaxRightToLeft");
    function P(n, f) {
        let h = n.length >> 1, d = n[h];
        y(n, d.y0 - m, h - 1, f), O(n, d.y1 + m, h + 1, f), y(n, u, n.length - 1, f), O(n, r, 0, f);
    }
    (0, _chunkGTKDMUJJMjs.a)(P, "resolveCollisions");
    function O(n, f, h, d) {
        for(; h < n.length; ++h){
            let s = n[h], g = (f - s.y0) * d;
            g > 1e-6 && (s.y0 += g, s.y1 += g), f = s.y1 + m;
        }
    }
    (0, _chunkGTKDMUJJMjs.a)(O, "resolveCollisionsTopToBottom");
    function y(n, f, h, d) {
        for(; h >= 0; --h){
            let s = n[h], g = (s.y1 - f) * d;
            g > 1e-6 && (s.y0 -= g, s.y1 -= g), f = s.y0 - m;
        }
    }
    (0, _chunkGTKDMUJJMjs.a)(y, "resolveCollisionsBottomToTop");
    function T({ sourceLinks: n, targetLinks: f }) {
        if (l === void 0) {
            for (let { source: { sourceLinks: h } } of f)h.sort(Dt);
            for (let { target: { targetLinks: h } } of n)h.sort(It);
        }
    }
    (0, _chunkGTKDMUJJMjs.a)(T, "reorderNodeLinks");
    function st(n) {
        if (l === void 0) for (let { sourceLinks: f, targetLinks: h } of n)f.sort(Dt), h.sort(It);
    }
    (0, _chunkGTKDMUJJMjs.a)(st, "reorderLinks");
    function A(n, f) {
        let h = n.y0 - (n.sourceLinks.length - 1) * m / 2;
        for (let { target: d, width: s } of n.sourceLinks){
            if (d === f) break;
            h += s + m;
        }
        for (let { source: d, width: s } of f.targetLinks){
            if (d === n) break;
            h -= s;
        }
        return h;
    }
    (0, _chunkGTKDMUJJMjs.a)(A, "targetTop");
    function Y(n, f) {
        let h = f.y0 - (f.targetLinks.length - 1) * m / 2;
        for (let { source: d, width: s } of f.targetLinks){
            if (d === n) break;
            h += s + m;
        }
        for (let { target: d, width: s } of n.sourceLinks){
            if (d === f) break;
            h -= s;
        }
        return h;
    }
    return (0, _chunkGTKDMUJJMjs.a)(Y, "sourceTop"), v;
}
(0, _chunkGTKDMUJJMjs.a)(rt, "Sankey");
var yt = Math.PI, mt = 2 * yt, V = 1e-6, Zt = mt - V;
function gt() {
    this._x0 = this._y0 = this._x1 = this._y1 = null, this._ = "";
}
(0, _chunkGTKDMUJJMjs.a)(gt, "Path");
function jt() {
    return new gt;
}
(0, _chunkGTKDMUJJMjs.a)(jt, "path");
gt.prototype = jt.prototype = {
    constructor: gt,
    moveTo: (0, _chunkGTKDMUJJMjs.a)(function(t, r) {
        this._ += "M" + (this._x0 = this._x1 = +t) + "," + (this._y0 = this._y1 = +r);
    }, "moveTo"),
    closePath: (0, _chunkGTKDMUJJMjs.a)(function() {
        this._x1 !== null && (this._x1 = this._x0, this._y1 = this._y0, this._ += "Z");
    }, "closePath"),
    lineTo: (0, _chunkGTKDMUJJMjs.a)(function(t, r) {
        this._ += "L" + (this._x1 = +t) + "," + (this._y1 = +r);
    }, "lineTo"),
    quadraticCurveTo: (0, _chunkGTKDMUJJMjs.a)(function(t, r, i, u) {
        this._ += "Q" + +t + "," + +r + "," + (this._x1 = +i) + "," + (this._y1 = +u);
    }, "quadraticCurveTo"),
    bezierCurveTo: (0, _chunkGTKDMUJJMjs.a)(function(t, r, i, u, c, S) {
        this._ += "C" + +t + "," + +r + "," + +i + "," + +u + "," + (this._x1 = +c) + "," + (this._y1 = +S);
    }, "bezierCurveTo"),
    arcTo: (0, _chunkGTKDMUJJMjs.a)(function(t, r, i, u, c) {
        t = +t, r = +r, i = +i, u = +u, c = +c;
        var S = this._x1, m = this._y1, x = i - t, o = u - r, a = S - t, l = m - r, k = a * a + l * l;
        if (c < 0) throw new Error("negative radius: " + c);
        if (this._x1 === null) this._ += "M" + (this._x1 = t) + "," + (this._y1 = r);
        else if (k > V) {
            if (!(Math.abs(l * x - o * a) > V) || !c) this._ += "L" + (this._x1 = t) + "," + (this._y1 = r);
            else {
                var _ = i - S, p = u - m, v = x * x + o * o, C = _ * _ + p * p, E = Math.sqrt(v), M = Math.sqrt(k), D = c * Math.tan((yt - Math.acos((v + k - C) / (2 * E * M))) / 2), z = D / M, B = D / E;
                Math.abs(z - 1) > V && (this._ += "L" + (t + z * a) + "," + (r + z * l)), this._ += "A" + c + "," + c + ",0,0," + +(l * _ > a * p) + "," + (this._x1 = t + B * x) + "," + (this._y1 = r + B * o);
            }
        }
    }, "arcTo"),
    arc: (0, _chunkGTKDMUJJMjs.a)(function(t, r, i, u, c, S) {
        t = +t, r = +r, i = +i, S = !!S;
        var m = i * Math.cos(u), x = i * Math.sin(u), o = t + m, a = r + x, l = 1 ^ S, k = S ? u - c : c - u;
        if (i < 0) throw new Error("negative radius: " + i);
        this._x1 === null ? this._ += "M" + o + "," + a : (Math.abs(this._x1 - o) > V || Math.abs(this._y1 - a) > V) && (this._ += "L" + o + "," + a), i && (k < 0 && (k = k % mt + mt), k > Zt ? this._ += "A" + i + "," + i + ",0,1," + l + "," + (t - m) + "," + (r - x) + "A" + i + "," + i + ",0,1," + l + "," + (this._x1 = o) + "," + (this._y1 = a) : k > V && (this._ += "A" + i + "," + i + ",0," + +(k >= yt) + "," + l + "," + (this._x1 = t + i * Math.cos(c)) + "," + (this._y1 = r + i * Math.sin(c))));
    }, "arc"),
    rect: (0, _chunkGTKDMUJJMjs.a)(function(t, r, i, u) {
        this._ += "M" + (this._x0 = this._x1 = +t) + "," + (this._y0 = this._y1 = +r) + "h" + +i + "v" + +u + "h" + -i + "Z";
    }, "rect"),
    toString: (0, _chunkGTKDMUJJMjs.a)(function() {
        return this._;
    }, "toString")
};
var xt = jt;
function ot(t) {
    return (0, _chunkGTKDMUJJMjs.a)(function() {
        return t;
    }, "constant");
}
(0, _chunkGTKDMUJJMjs.a)(ot, "default");
function zt(t) {
    return t[0];
}
(0, _chunkGTKDMUJJMjs.a)(zt, "x");
function Bt(t) {
    return t[1];
}
(0, _chunkGTKDMUJJMjs.a)(Bt, "y");
var $t = Array.prototype.slice;
function te(t) {
    return t.source;
}
(0, _chunkGTKDMUJJMjs.a)(te, "linkSource");
function ee(t) {
    return t.target;
}
(0, _chunkGTKDMUJJMjs.a)(ee, "linkTarget");
function ne(t) {
    var r = te, i = ee, u = zt, c = Bt, S = null;
    function m() {
        var x, o = $t.call(arguments), a = r.apply(this, o), l = i.apply(this, o);
        if (S || (S = x = xt()), t(S, +u.apply(this, (o[0] = a, o)), +c.apply(this, o), +u.apply(this, (o[0] = l, o)), +c.apply(this, o)), x) return S = null, x + "" || null;
    }
    return (0, _chunkGTKDMUJJMjs.a)(m, "link"), m.source = function(x) {
        return arguments.length ? (r = x, m) : r;
    }, m.target = function(x) {
        return arguments.length ? (i = x, m) : i;
    }, m.x = function(x) {
        return arguments.length ? (u = typeof x == "function" ? x : ot(+x), m) : u;
    }, m.y = function(x) {
        return arguments.length ? (c = typeof x == "function" ? x : ot(+x), m) : c;
    }, m.context = function(x) {
        return arguments.length ? (S = x ?? null, m) : S;
    }, m;
}
(0, _chunkGTKDMUJJMjs.a)(ne, "link");
function re(t, r, i, u, c) {
    t.moveTo(r, i), t.bezierCurveTo(r = (r + u) / 2, i, r, c, u, c);
}
(0, _chunkGTKDMUJJMjs.a)(re, "curveHorizontal");
function kt() {
    return ne(re);
}
(0, _chunkGTKDMUJJMjs.a)(kt, "linkHorizontal");
function oe(t) {
    return [
        t.source.x1,
        t.y0
    ];
}
(0, _chunkGTKDMUJJMjs.a)(oe, "horizontalSource");
function ie(t) {
    return [
        t.target.x0,
        t.y1
    ];
}
(0, _chunkGTKDMUJJMjs.a)(ie, "horizontalTarget");
function it() {
    return kt().source(oe).target(ie);
}
(0, _chunkGTKDMUJJMjs.a)(it, "default");
var K = class t {
    static{
        (0, _chunkGTKDMUJJMjs.a)(this, "Uid");
    }
    static{
        this.count = 0;
    }
    static next(r) {
        return new t(r + ++t.count);
    }
    constructor(r){
        this.id = r, this.href = `#${r}`;
    }
    toString() {
        return "url(" + this.href + ")";
    }
};
var se = {
    left: ct,
    right: ht,
    center: dt,
    justify: Q
}, ae = (0, _chunkGTKDMUJJMjs.a)(function(t, r, i, u) {
    let { securityLevel: c, sankey: S } = (0, _chunkNQURTBEVMjs.X)(), m = (0, _chunkNQURTBEVMjs.Z).sankey, x;
    c === "sandbox" && (x = (0, _chunkNQURTBEVMjs.fa)("#i" + r));
    let o = c === "sandbox" ? (0, _chunkNQURTBEVMjs.fa)(x.nodes()[0].contentDocument.body) : (0, _chunkNQURTBEVMjs.fa)("body"), a = c === "sandbox" ? o.select(`[id="${r}"]`) : (0, _chunkNQURTBEVMjs.fa)(`[id="${r}"]`), l = S?.width ?? m.width, k = S?.height ?? m.width, _ = S?.useMaxWidth ?? m.useMaxWidth, p = S?.nodeAlignment ?? m.nodeAlignment, v = S?.prefix ?? m.prefix, C = S?.suffix ?? m.suffix, E = S?.showValues ?? m.showValues, M = u.db.getGraph(), D = se[p];
    rt().nodeId((y)=>y.id).nodeWidth(10).nodePadding(10 + (E ? 15 : 0)).nodeAlign(D).extent([
        [
            0,
            0
        ],
        [
            l,
            k
        ]
    ])(M);
    let R = (0, _chunkNQURTBEVMjs.ha)((0, _chunkNQURTBEVMjs.za));
    a.append("g").attr("class", "nodes").selectAll(".node").data(M.nodes).join("g").attr("class", "node").attr("id", (y)=>(y.uid = K.next("node-")).id).attr("transform", function(y) {
        return "translate(" + y.x0 + "," + y.y0 + ")";
    }).attr("x", (y)=>y.x0).attr("y", (y)=>y.y0).append("rect").attr("height", (y)=>y.y1 - y.y0).attr("width", (y)=>y.x1 - y.x0).attr("fill", (y)=>R(y.id));
    let w = (0, _chunkGTKDMUJJMjs.a)(({ id: y, value: T })=>E ? `${y}
${v}${Math.round(T * 100) / 100}${C}` : y, "getText");
    a.append("g").attr("class", "node-labels").attr("font-family", "sans-serif").attr("font-size", 14).selectAll("text").data(M.nodes).join("text").attr("x", (y)=>y.x0 < l / 2 ? y.x1 + 6 : y.x0 - 6).attr("y", (y)=>(y.y1 + y.y0) / 2).attr("dy", `${E ? "0" : "0.35"}em`).attr("text-anchor", (y)=>y.x0 < l / 2 ? "start" : "end").text(w);
    let N = a.append("g").attr("class", "links").attr("fill", "none").attr("stroke-opacity", .5).selectAll(".link").data(M.links).join("g").attr("class", "link").style("mix-blend-mode", "multiply"), P = S?.linkColor ?? "gradient";
    if (P === "gradient") {
        let y = N.append("linearGradient").attr("id", (T)=>(T.uid = K.next("linearGradient-")).id).attr("gradientUnits", "userSpaceOnUse").attr("x1", (T)=>T.source.x1).attr("x2", (T)=>T.target.x0);
        y.append("stop").attr("offset", "0%").attr("stop-color", (T)=>R(T.source.id)), y.append("stop").attr("offset", "100%").attr("stop-color", (T)=>R(T.target.id));
    }
    let O;
    switch(P){
        case "gradient":
            O = (0, _chunkGTKDMUJJMjs.a)((y)=>y.uid, "coloring");
            break;
        case "source":
            O = (0, _chunkGTKDMUJJMjs.a)((y)=>R(y.source.id), "coloring");
            break;
        case "target":
            O = (0, _chunkGTKDMUJJMjs.a)((y)=>R(y.target.id), "coloring");
            break;
        default:
            O = P;
    }
    N.append("path").attr("d", it()).attr("stroke", O).attr("stroke-width", (y)=>Math.max(1, y.width)), (0, _chunkNQURTBEVMjs.N)(void 0, a, 0, _);
}, "draw"), Vt = {
    draw: ae
};
var Wt = (0, _chunkGTKDMUJJMjs.a)((t)=>t.replaceAll(/^[^\S\n\r]+|[^\S\n\r]+$/g, "").replaceAll(/([\n\r])+/g, `
`).trim(), "prepareTextForParsing");
var le = G.parse.bind(G);
G.parse = (t)=>le(Wt(t));
var wn = {
    parser: G,
    db: Ot,
    renderer: Vt
};

},{"./chunk-NQURTBEV.mjs":"iASFe","./chunk-GTKDMUJJ.mjs":"fruhx","@parcel/transformer-js/src/esmodule-helpers.js":"1cdyk"}]},["7UpjM"], null, "parcelRequire6955", {})

//# sourceMappingURL=sankeyDiagram-F3GI3WFI.1784d1e0.js.map
