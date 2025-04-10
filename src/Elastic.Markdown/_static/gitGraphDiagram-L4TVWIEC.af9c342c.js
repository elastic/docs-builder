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
})({"7g22s":[function(require,module,exports,__globalThis) {
var global = arguments[3];
var HMR_HOST = null;
var HMR_PORT = 1234;
var HMR_SERVER_PORT = 1234;
var HMR_SECURE = false;
var HMR_ENV_HASH = "febfef3c5b467b16";
var HMR_USE_SSE = false;
module.bundle.HMR_BUNDLE_ID = "2876e7e9af9c342c";
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

},{}],"fRNXf":[function(require,module,exports,__globalThis) {
var parcelHelpers = require("@parcel/transformer-js/src/esmodule-helpers.js");
parcelHelpers.defineInteropFlag(exports);
parcelHelpers.export(exports, "diagram", ()=>diagram);
var _chunkYJGJQOYZMjs = require("./chunk-YJGJQOYZ.mjs");
var _chunkK2ZEYYM2Mjs = require("./chunk-K2ZEYYM2.mjs");
var _chunkM52XIDDUMjs = require("./chunk-M52XIDDU.mjs");
var _chunk76YUXBKWMjs = require("./chunk-76YUXBKW.mjs");
var _chunkW7WFRJCBMjs = require("./chunk-W7WFRJCB.mjs");
var _chunkI7ZFS43CMjs = require("./chunk-I7ZFS43C.mjs");
var _chunkGKOISANMMjs = require("./chunk-GKOISANM.mjs");
var _chunkDD37ZF33Mjs = require("./chunk-DD37ZF33.mjs");
var _chunkHCCMVKPJMjs = require("./chunk-HCCMVKPJ.mjs");
var _chunk3YXWICELMjs = require("./chunk-3YXWICEL.mjs");
var _chunkHBGMPAD7Mjs = require("./chunk-HBGMPAD7.mjs");
var _chunkTZBO7MLIMjs = require("./chunk-TZBO7MLI.mjs");
var _chunkGRZAG2UZMjs = require("./chunk-GRZAG2UZ.mjs");
var _chunkHD3LK5B5Mjs = require("./chunk-HD3LK5B5.mjs");
var _chunkDLQEHMXDMjs = require("./chunk-DLQEHMXD.mjs");
// src/diagrams/git/gitGraphTypes.ts
var commitType = {
    NORMAL: 0,
    REVERSE: 1,
    HIGHLIGHT: 2,
    MERGE: 3,
    CHERRY_PICK: 4
};
// src/diagrams/git/gitGraphAst.ts
var DEFAULT_GITGRAPH_CONFIG = (0, _chunkDD37ZF33Mjs.defaultConfig_default).gitGraph;
var getConfig3 = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(()=>{
    const config = (0, _chunkI7ZFS43CMjs.cleanAndMerge)({
        ...DEFAULT_GITGRAPH_CONFIG,
        ...(0, _chunkDD37ZF33Mjs.getConfig)().gitGraph
    });
    return config;
}, "getConfig");
var state = new (0, _chunkYJGJQOYZMjs.ImperativeState)(()=>{
    const config = getConfig3();
    const mainBranchName = config.mainBranchName;
    const mainBranchOrder = config.mainBranchOrder;
    return {
        mainBranchName,
        commits: /* @__PURE__ */ new Map(),
        head: null,
        branchConfig: /* @__PURE__ */ new Map([
            [
                mainBranchName,
                {
                    name: mainBranchName,
                    order: mainBranchOrder
                }
            ]
        ]),
        branches: /* @__PURE__ */ new Map([
            [
                mainBranchName,
                null
            ]
        ]),
        currBranch: mainBranchName,
        direction: "LR",
        seq: 0,
        options: {}
    };
});
function getID() {
    return (0, _chunkI7ZFS43CMjs.random)({
        length: 7
    });
}
(0, _chunkDLQEHMXDMjs.__name)(getID, "getID");
function uniqBy(list, fn) {
    const recordMap = /* @__PURE__ */ Object.create(null);
    return list.reduce((out, item)=>{
        const key = fn(item);
        if (!recordMap[key]) {
            recordMap[key] = true;
            out.push(item);
        }
        return out;
    }, []);
}
(0, _chunkDLQEHMXDMjs.__name)(uniqBy, "uniqBy");
var setDirection = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(dir2) {
    state.records.direction = dir2;
}, "setDirection");
var setOptions = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(rawOptString) {
    (0, _chunkDD37ZF33Mjs.log).debug("options str", rawOptString);
    rawOptString = rawOptString?.trim();
    rawOptString = rawOptString || "{}";
    try {
        state.records.options = JSON.parse(rawOptString);
    } catch (e) {
        (0, _chunkDD37ZF33Mjs.log).error("error while parsing gitGraph options", e.message);
    }
}, "setOptions");
var getOptions = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function() {
    return state.records.options;
}, "getOptions");
var commit = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(commitDB) {
    let msg = commitDB.msg;
    let id = commitDB.id;
    const type = commitDB.type;
    let tags = commitDB.tags;
    (0, _chunkDD37ZF33Mjs.log).info("commit", msg, id, type, tags);
    (0, _chunkDD37ZF33Mjs.log).debug("Entering commit:", msg, id, type, tags);
    const config = getConfig3();
    id = (0, _chunkDD37ZF33Mjs.common_default).sanitizeText(id, config);
    msg = (0, _chunkDD37ZF33Mjs.common_default).sanitizeText(msg, config);
    tags = tags?.map((tag)=>(0, _chunkDD37ZF33Mjs.common_default).sanitizeText(tag, config));
    const newCommit = {
        id: id ? id : state.records.seq + "-" + getID(),
        message: msg,
        seq: state.records.seq++,
        type: type ?? commitType.NORMAL,
        tags: tags ?? [],
        parents: state.records.head == null ? [] : [
            state.records.head.id
        ],
        branch: state.records.currBranch
    };
    state.records.head = newCommit;
    (0, _chunkDD37ZF33Mjs.log).info("main branch", config.mainBranchName);
    state.records.commits.set(newCommit.id, newCommit);
    state.records.branches.set(state.records.currBranch, newCommit.id);
    (0, _chunkDD37ZF33Mjs.log).debug("in pushCommit " + newCommit.id);
}, "commit");
var branch = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(branchDB) {
    let name = branchDB.name;
    const order = branchDB.order;
    name = (0, _chunkDD37ZF33Mjs.common_default).sanitizeText(name, getConfig3());
    if (state.records.branches.has(name)) throw new Error(`Trying to create an existing branch. (Help: Either use a new name if you want create a new branch or try using "checkout ${name}")`);
    state.records.branches.set(name, state.records.head != null ? state.records.head.id : null);
    state.records.branchConfig.set(name, {
        name,
        order
    });
    checkout(name);
    (0, _chunkDD37ZF33Mjs.log).debug("in createBranch");
}, "branch");
var merge = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((mergeDB)=>{
    let otherBranch = mergeDB.branch;
    let customId = mergeDB.id;
    const overrideType = mergeDB.type;
    const customTags = mergeDB.tags;
    const config = getConfig3();
    otherBranch = (0, _chunkDD37ZF33Mjs.common_default).sanitizeText(otherBranch, config);
    if (customId) customId = (0, _chunkDD37ZF33Mjs.common_default).sanitizeText(customId, config);
    const currentBranchCheck = state.records.branches.get(state.records.currBranch);
    const otherBranchCheck = state.records.branches.get(otherBranch);
    const currentCommit = currentBranchCheck ? state.records.commits.get(currentBranchCheck) : void 0;
    const otherCommit = otherBranchCheck ? state.records.commits.get(otherBranchCheck) : void 0;
    if (currentCommit && otherCommit && currentCommit.branch === otherBranch) throw new Error(`Cannot merge branch '${otherBranch}' into itself.`);
    if (state.records.currBranch === otherBranch) {
        const error = new Error('Incorrect usage of "merge". Cannot merge a branch to itself');
        error.hash = {
            text: `merge ${otherBranch}`,
            token: `merge ${otherBranch}`,
            expected: [
                "branch abc"
            ]
        };
        throw error;
    }
    if (currentCommit === void 0 || !currentCommit) {
        const error = new Error(`Incorrect usage of "merge". Current branch (${state.records.currBranch})has no commits`);
        error.hash = {
            text: `merge ${otherBranch}`,
            token: `merge ${otherBranch}`,
            expected: [
                "commit"
            ]
        };
        throw error;
    }
    if (!state.records.branches.has(otherBranch)) {
        const error = new Error('Incorrect usage of "merge". Branch to be merged (' + otherBranch + ") does not exist");
        error.hash = {
            text: `merge ${otherBranch}`,
            token: `merge ${otherBranch}`,
            expected: [
                `branch ${otherBranch}`
            ]
        };
        throw error;
    }
    if (otherCommit === void 0 || !otherCommit) {
        const error = new Error('Incorrect usage of "merge". Branch to be merged (' + otherBranch + ") has no commits");
        error.hash = {
            text: `merge ${otherBranch}`,
            token: `merge ${otherBranch}`,
            expected: [
                '"commit"'
            ]
        };
        throw error;
    }
    if (currentCommit === otherCommit) {
        const error = new Error('Incorrect usage of "merge". Both branches have same head');
        error.hash = {
            text: `merge ${otherBranch}`,
            token: `merge ${otherBranch}`,
            expected: [
                "branch abc"
            ]
        };
        throw error;
    }
    if (customId && state.records.commits.has(customId)) {
        const error = new Error('Incorrect usage of "merge". Commit with id:' + customId + " already exists, use different custom Id");
        error.hash = {
            text: `merge ${otherBranch} ${customId} ${overrideType} ${customTags?.join(" ")}`,
            token: `merge ${otherBranch} ${customId} ${overrideType} ${customTags?.join(" ")}`,
            expected: [
                `merge ${otherBranch} ${customId}_UNIQUE ${overrideType} ${customTags?.join(" ")}`
            ]
        };
        throw error;
    }
    const verifiedBranch = otherBranchCheck ? otherBranchCheck : "";
    const commit2 = {
        id: customId || `${state.records.seq}-${getID()}`,
        message: `merged branch ${otherBranch} into ${state.records.currBranch}`,
        seq: state.records.seq++,
        parents: state.records.head == null ? [] : [
            state.records.head.id,
            verifiedBranch
        ],
        branch: state.records.currBranch,
        type: commitType.MERGE,
        customType: overrideType,
        customId: customId ? true : false,
        tags: customTags ?? []
    };
    state.records.head = commit2;
    state.records.commits.set(commit2.id, commit2);
    state.records.branches.set(state.records.currBranch, commit2.id);
    (0, _chunkDD37ZF33Mjs.log).debug(state.records.branches);
    (0, _chunkDD37ZF33Mjs.log).debug("in mergeBranch");
}, "merge");
var cherryPick = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(cherryPickDB) {
    let sourceId = cherryPickDB.id;
    let targetId = cherryPickDB.targetId;
    let tags = cherryPickDB.tags;
    let parentCommitId = cherryPickDB.parent;
    (0, _chunkDD37ZF33Mjs.log).debug("Entering cherryPick:", sourceId, targetId, tags);
    const config = getConfig3();
    sourceId = (0, _chunkDD37ZF33Mjs.common_default).sanitizeText(sourceId, config);
    targetId = (0, _chunkDD37ZF33Mjs.common_default).sanitizeText(targetId, config);
    tags = tags?.map((tag)=>(0, _chunkDD37ZF33Mjs.common_default).sanitizeText(tag, config));
    parentCommitId = (0, _chunkDD37ZF33Mjs.common_default).sanitizeText(parentCommitId, config);
    if (!sourceId || !state.records.commits.has(sourceId)) {
        const error = new Error('Incorrect usage of "cherryPick". Source commit id should exist and provided');
        error.hash = {
            text: `cherryPick ${sourceId} ${targetId}`,
            token: `cherryPick ${sourceId} ${targetId}`,
            expected: [
                "cherry-pick abc"
            ]
        };
        throw error;
    }
    const sourceCommit = state.records.commits.get(sourceId);
    if (sourceCommit === void 0 || !sourceCommit) throw new Error('Incorrect usage of "cherryPick". Source commit id should exist and provided');
    if (parentCommitId && !(Array.isArray(sourceCommit.parents) && sourceCommit.parents.includes(parentCommitId))) {
        const error = new Error("Invalid operation: The specified parent commit is not an immediate parent of the cherry-picked commit.");
        throw error;
    }
    const sourceCommitBranch = sourceCommit.branch;
    if (sourceCommit.type === commitType.MERGE && !parentCommitId) {
        const error = new Error("Incorrect usage of cherry-pick: If the source commit is a merge commit, an immediate parent commit must be specified.");
        throw error;
    }
    if (!targetId || !state.records.commits.has(targetId)) {
        if (sourceCommitBranch === state.records.currBranch) {
            const error = new Error('Incorrect usage of "cherryPick". Source commit is already on current branch');
            error.hash = {
                text: `cherryPick ${sourceId} ${targetId}`,
                token: `cherryPick ${sourceId} ${targetId}`,
                expected: [
                    "cherry-pick abc"
                ]
            };
            throw error;
        }
        const currentCommitId = state.records.branches.get(state.records.currBranch);
        if (currentCommitId === void 0 || !currentCommitId) {
            const error = new Error(`Incorrect usage of "cherry-pick". Current branch (${state.records.currBranch})has no commits`);
            error.hash = {
                text: `cherryPick ${sourceId} ${targetId}`,
                token: `cherryPick ${sourceId} ${targetId}`,
                expected: [
                    "cherry-pick abc"
                ]
            };
            throw error;
        }
        const currentCommit = state.records.commits.get(currentCommitId);
        if (currentCommit === void 0 || !currentCommit) {
            const error = new Error(`Incorrect usage of "cherry-pick". Current branch (${state.records.currBranch})has no commits`);
            error.hash = {
                text: `cherryPick ${sourceId} ${targetId}`,
                token: `cherryPick ${sourceId} ${targetId}`,
                expected: [
                    "cherry-pick abc"
                ]
            };
            throw error;
        }
        const commit2 = {
            id: state.records.seq + "-" + getID(),
            message: `cherry-picked ${sourceCommit?.message} into ${state.records.currBranch}`,
            seq: state.records.seq++,
            parents: state.records.head == null ? [] : [
                state.records.head.id,
                sourceCommit.id
            ],
            branch: state.records.currBranch,
            type: commitType.CHERRY_PICK,
            tags: tags ? tags.filter(Boolean) : [
                `cherry-pick:${sourceCommit.id}${sourceCommit.type === commitType.MERGE ? `|parent:${parentCommitId}` : ""}`
            ]
        };
        state.records.head = commit2;
        state.records.commits.set(commit2.id, commit2);
        state.records.branches.set(state.records.currBranch, commit2.id);
        (0, _chunkDD37ZF33Mjs.log).debug(state.records.branches);
        (0, _chunkDD37ZF33Mjs.log).debug("in cherryPick");
    }
}, "cherryPick");
var checkout = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(branch2) {
    branch2 = (0, _chunkDD37ZF33Mjs.common_default).sanitizeText(branch2, getConfig3());
    if (!state.records.branches.has(branch2)) {
        const error = new Error(`Trying to checkout branch which is not yet created. (Help try using "branch ${branch2}")`);
        error.hash = {
            text: `checkout ${branch2}`,
            token: `checkout ${branch2}`,
            expected: [
                `branch ${branch2}`
            ]
        };
        throw error;
    } else {
        state.records.currBranch = branch2;
        const id = state.records.branches.get(state.records.currBranch);
        if (id === void 0 || !id) state.records.head = null;
        else state.records.head = state.records.commits.get(id) ?? null;
    }
}, "checkout");
function upsert(arr, key, newVal) {
    const index = arr.indexOf(key);
    if (index === -1) arr.push(newVal);
    else arr.splice(index, 1, newVal);
}
(0, _chunkDLQEHMXDMjs.__name)(upsert, "upsert");
function prettyPrintCommitHistory(commitArr) {
    const commit2 = commitArr.reduce((out, commit3)=>{
        if (out.seq > commit3.seq) return out;
        return commit3;
    }, commitArr[0]);
    let line = "";
    commitArr.forEach(function(c) {
        if (c === commit2) line += "	*";
        else line += "	|";
    });
    const label = [
        line,
        commit2.id,
        commit2.seq
    ];
    for(const branch2 in state.records.branches)if (state.records.branches.get(branch2) === commit2.id) label.push(branch2);
    (0, _chunkDD37ZF33Mjs.log).debug(label.join(" "));
    if (commit2.parents && commit2.parents.length == 2 && commit2.parents[0] && commit2.parents[1]) {
        const newCommit = state.records.commits.get(commit2.parents[0]);
        upsert(commitArr, commit2, newCommit);
        if (commit2.parents[1]) commitArr.push(state.records.commits.get(commit2.parents[1]));
    } else if (commit2.parents.length == 0) return;
    else if (commit2.parents[0]) {
        const newCommit = state.records.commits.get(commit2.parents[0]);
        upsert(commitArr, commit2, newCommit);
    }
    commitArr = uniqBy(commitArr, (c)=>c.id);
    prettyPrintCommitHistory(commitArr);
}
(0, _chunkDLQEHMXDMjs.__name)(prettyPrintCommitHistory, "prettyPrintCommitHistory");
var prettyPrint = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function() {
    (0, _chunkDD37ZF33Mjs.log).debug(state.records.commits);
    const node = getCommitsArray()[0];
    prettyPrintCommitHistory([
        node
    ]);
}, "prettyPrint");
var clear2 = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function() {
    state.reset();
    (0, _chunkDD37ZF33Mjs.clear)();
}, "clear");
var getBranchesAsObjArray = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function() {
    const branchesArray = [
        ...state.records.branchConfig.values()
    ].map((branchConfig, i)=>{
        if (branchConfig.order !== null && branchConfig.order !== void 0) return branchConfig;
        return {
            ...branchConfig,
            order: parseFloat(`0.${i}`)
        };
    }).sort((a, b)=>(a.order ?? 0) - (b.order ?? 0)).map(({ name })=>({
            name
        }));
    return branchesArray;
}, "getBranchesAsObjArray");
var getBranches = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function() {
    return state.records.branches;
}, "getBranches");
var getCommits = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function() {
    return state.records.commits;
}, "getCommits");
var getCommitsArray = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function() {
    const commitArr = [
        ...state.records.commits.values()
    ];
    commitArr.forEach(function(o) {
        (0, _chunkDD37ZF33Mjs.log).debug(o.id);
    });
    commitArr.sort((a, b)=>a.seq - b.seq);
    return commitArr;
}, "getCommitsArray");
var getCurrentBranch = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function() {
    return state.records.currBranch;
}, "getCurrentBranch");
var getDirection = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function() {
    return state.records.direction;
}, "getDirection");
var getHead = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function() {
    return state.records.head;
}, "getHead");
var db = {
    commitType,
    getConfig: getConfig3,
    setDirection,
    setOptions,
    getOptions,
    commit,
    branch,
    merge,
    cherryPick,
    checkout,
    //reset,
    prettyPrint,
    clear: clear2,
    getBranchesAsObjArray,
    getBranches,
    getCommits,
    getCommitsArray,
    getCurrentBranch,
    getDirection,
    getHead,
    setAccTitle: (0, _chunkDD37ZF33Mjs.setAccTitle),
    getAccTitle: (0, _chunkDD37ZF33Mjs.getAccTitle),
    getAccDescription: (0, _chunkDD37ZF33Mjs.getAccDescription),
    setAccDescription: (0, _chunkDD37ZF33Mjs.setAccDescription),
    setDiagramTitle: (0, _chunkDD37ZF33Mjs.setDiagramTitle),
    getDiagramTitle: (0, _chunkDD37ZF33Mjs.getDiagramTitle)
};
// src/diagrams/git/gitGraphParser.ts
var populate = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((ast, db2)=>{
    (0, _chunkK2ZEYYM2Mjs.populateCommonDb)(ast, db2);
    if (ast.dir) db2.setDirection(ast.dir);
    for (const statement of ast.statements)parseStatement(statement, db2);
}, "populate");
var parseStatement = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((statement, db2)=>{
    const parsers = {
        Commit: /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((stmt)=>db2.commit(parseCommit(stmt)), "Commit"),
        Branch: /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((stmt)=>db2.branch(parseBranch(stmt)), "Branch"),
        Merge: /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((stmt)=>db2.merge(parseMerge(stmt)), "Merge"),
        Checkout: /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((stmt)=>db2.checkout(parseCheckout(stmt)), "Checkout"),
        CherryPicking: /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((stmt)=>db2.cherryPick(parseCherryPicking(stmt)), "CherryPicking")
    };
    const parser2 = parsers[statement.$type];
    if (parser2) parser2(statement);
    else (0, _chunkDD37ZF33Mjs.log).error(`Unknown statement type: ${statement.$type}`);
}, "parseStatement");
var parseCommit = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((commit2)=>{
    const commitDB = {
        id: commit2.id,
        msg: commit2.message ?? "",
        type: commit2.type !== void 0 ? commitType[commit2.type] : commitType.NORMAL,
        tags: commit2.tags ?? void 0
    };
    return commitDB;
}, "parseCommit");
var parseBranch = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((branch2)=>{
    const branchDB = {
        name: branch2.name,
        order: branch2.order ?? 0
    };
    return branchDB;
}, "parseBranch");
var parseMerge = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((merge2)=>{
    const mergeDB = {
        branch: merge2.branch,
        id: merge2.id ?? "",
        type: merge2.type !== void 0 ? commitType[merge2.type] : void 0,
        tags: merge2.tags ?? void 0
    };
    return mergeDB;
}, "parseMerge");
var parseCheckout = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((checkout2)=>{
    const branch2 = checkout2.branch;
    return branch2;
}, "parseCheckout");
var parseCherryPicking = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((cherryPicking)=>{
    const cherryPickDB = {
        id: cherryPicking.id,
        targetId: "",
        tags: cherryPicking.tags?.length === 0 ? void 0 : cherryPicking.tags,
        parent: cherryPicking.parent
    };
    return cherryPickDB;
}, "parseCherryPicking");
var parser = {
    parse: /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(async (input)=>{
        const ast = await (0, _chunkM52XIDDUMjs.parse)("gitGraph", input);
        (0, _chunkDD37ZF33Mjs.log).debug(ast);
        populate(ast, db);
    }, "parse")
};
// src/diagrams/git/gitGraphRenderer.ts
var DEFAULT_CONFIG = (0, _chunkDD37ZF33Mjs.getConfig2)();
var DEFAULT_GITGRAPH_CONFIG2 = DEFAULT_CONFIG?.gitGraph;
var LAYOUT_OFFSET = 10;
var COMMIT_STEP = 40;
var PX = 4;
var PY = 2;
var THEME_COLOR_LIMIT = 8;
var branchPos = /* @__PURE__ */ new Map();
var commitPos = /* @__PURE__ */ new Map();
var defaultPos = 30;
var allCommitsDict = /* @__PURE__ */ new Map();
var lanes = [];
var maxPos = 0;
var dir = "LR";
var clear3 = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(()=>{
    branchPos.clear();
    commitPos.clear();
    allCommitsDict.clear();
    maxPos = 0;
    lanes = [];
    dir = "LR";
}, "clear");
var drawText = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((txt)=>{
    const svgLabel = document.createElementNS("http://www.w3.org/2000/svg", "text");
    const rows = typeof txt === "string" ? txt.split(/\\n|\n|<br\s*\/?>/gi) : txt;
    rows.forEach((row)=>{
        const tspan = document.createElementNS("http://www.w3.org/2000/svg", "tspan");
        tspan.setAttributeNS("http://www.w3.org/XML/1998/namespace", "xml:space", "preserve");
        tspan.setAttribute("dy", "1em");
        tspan.setAttribute("x", "0");
        tspan.setAttribute("class", "row");
        tspan.textContent = row.trim();
        svgLabel.appendChild(tspan);
    });
    return svgLabel;
}, "drawText");
var findClosestParent = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((parents)=>{
    let closestParent;
    let comparisonFunc;
    let targetPosition;
    if (dir === "BT") {
        comparisonFunc = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((a, b)=>a <= b, "comparisonFunc");
        targetPosition = Infinity;
    } else {
        comparisonFunc = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((a, b)=>a >= b, "comparisonFunc");
        targetPosition = 0;
    }
    parents.forEach((parent)=>{
        const parentPosition = dir === "TB" || dir == "BT" ? commitPos.get(parent)?.y : commitPos.get(parent)?.x;
        if (parentPosition !== void 0 && comparisonFunc(parentPosition, targetPosition)) {
            closestParent = parent;
            targetPosition = parentPosition;
        }
    });
    return closestParent;
}, "findClosestParent");
var findClosestParentBT = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((parents)=>{
    let closestParent = "";
    let maxPosition = Infinity;
    parents.forEach((parent)=>{
        const parentPosition = commitPos.get(parent).y;
        if (parentPosition <= maxPosition) {
            closestParent = parent;
            maxPosition = parentPosition;
        }
    });
    return closestParent || void 0;
}, "findClosestParentBT");
var setParallelBTPos = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((sortedKeys, commits, defaultPos2)=>{
    let curPos = defaultPos2;
    let maxPosition = defaultPos2;
    const roots = [];
    sortedKeys.forEach((key)=>{
        const commit2 = commits.get(key);
        if (!commit2) throw new Error(`Commit not found for key ${key}`);
        if (commit2.parents.length) {
            curPos = calculateCommitPosition(commit2);
            maxPosition = Math.max(curPos, maxPosition);
        } else roots.push(commit2);
        setCommitPosition(commit2, curPos);
    });
    curPos = maxPosition;
    roots.forEach((commit2)=>{
        setRootPosition(commit2, curPos, defaultPos2);
    });
    sortedKeys.forEach((key)=>{
        const commit2 = commits.get(key);
        if (commit2?.parents.length) {
            const closestParent = findClosestParentBT(commit2.parents);
            curPos = commitPos.get(closestParent).y - COMMIT_STEP;
            if (curPos <= maxPosition) maxPosition = curPos;
            const x = branchPos.get(commit2.branch).pos;
            const y = curPos - LAYOUT_OFFSET;
            commitPos.set(commit2.id, {
                x,
                y
            });
        }
    });
}, "setParallelBTPos");
var findClosestParentPos = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((commit2)=>{
    const closestParent = findClosestParent(commit2.parents.filter((p)=>p !== null));
    if (!closestParent) throw new Error(`Closest parent not found for commit ${commit2.id}`);
    const closestParentPos = commitPos.get(closestParent)?.y;
    if (closestParentPos === void 0) throw new Error(`Closest parent position not found for commit ${commit2.id}`);
    return closestParentPos;
}, "findClosestParentPos");
var calculateCommitPosition = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((commit2)=>{
    const closestParentPos = findClosestParentPos(commit2);
    return closestParentPos + COMMIT_STEP;
}, "calculateCommitPosition");
var setCommitPosition = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((commit2, curPos)=>{
    const branch2 = branchPos.get(commit2.branch);
    if (!branch2) throw new Error(`Branch not found for commit ${commit2.id}`);
    const x = branch2.pos;
    const y = curPos + LAYOUT_OFFSET;
    commitPos.set(commit2.id, {
        x,
        y
    });
    return {
        x,
        y
    };
}, "setCommitPosition");
var setRootPosition = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((commit2, curPos, defaultPos2)=>{
    const branch2 = branchPos.get(commit2.branch);
    if (!branch2) throw new Error(`Branch not found for commit ${commit2.id}`);
    const y = curPos + defaultPos2;
    const x = branch2.pos;
    commitPos.set(commit2.id, {
        x,
        y
    });
}, "setRootPosition");
var drawCommitBullet = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((gBullets, commit2, commitPosition, typeClass, branchIndex, commitSymbolType)=>{
    if (commitSymbolType === commitType.HIGHLIGHT) {
        gBullets.append("rect").attr("x", commitPosition.x - 10).attr("y", commitPosition.y - 10).attr("width", 20).attr("height", 20).attr("class", `commit ${commit2.id} commit-highlight${branchIndex % THEME_COLOR_LIMIT} ${typeClass}-outer`);
        gBullets.append("rect").attr("x", commitPosition.x - 6).attr("y", commitPosition.y - 6).attr("width", 12).attr("height", 12).attr("class", `commit ${commit2.id} commit${branchIndex % THEME_COLOR_LIMIT} ${typeClass}-inner`);
    } else if (commitSymbolType === commitType.CHERRY_PICK) {
        gBullets.append("circle").attr("cx", commitPosition.x).attr("cy", commitPosition.y).attr("r", 10).attr("class", `commit ${commit2.id} ${typeClass}`);
        gBullets.append("circle").attr("cx", commitPosition.x - 3).attr("cy", commitPosition.y + 2).attr("r", 2.75).attr("fill", "#fff").attr("class", `commit ${commit2.id} ${typeClass}`);
        gBullets.append("circle").attr("cx", commitPosition.x + 3).attr("cy", commitPosition.y + 2).attr("r", 2.75).attr("fill", "#fff").attr("class", `commit ${commit2.id} ${typeClass}`);
        gBullets.append("line").attr("x1", commitPosition.x + 3).attr("y1", commitPosition.y + 1).attr("x2", commitPosition.x).attr("y2", commitPosition.y - 5).attr("stroke", "#fff").attr("class", `commit ${commit2.id} ${typeClass}`);
        gBullets.append("line").attr("x1", commitPosition.x - 3).attr("y1", commitPosition.y + 1).attr("x2", commitPosition.x).attr("y2", commitPosition.y - 5).attr("stroke", "#fff").attr("class", `commit ${commit2.id} ${typeClass}`);
    } else {
        const circle = gBullets.append("circle");
        circle.attr("cx", commitPosition.x);
        circle.attr("cy", commitPosition.y);
        circle.attr("r", commit2.type === commitType.MERGE ? 9 : 10);
        circle.attr("class", `commit ${commit2.id} commit${branchIndex % THEME_COLOR_LIMIT}`);
        if (commitSymbolType === commitType.MERGE) {
            const circle2 = gBullets.append("circle");
            circle2.attr("cx", commitPosition.x);
            circle2.attr("cy", commitPosition.y);
            circle2.attr("r", 6);
            circle2.attr("class", `commit ${typeClass} ${commit2.id} commit${branchIndex % THEME_COLOR_LIMIT}`);
        }
        if (commitSymbolType === commitType.REVERSE) {
            const cross = gBullets.append("path");
            cross.attr("d", `M ${commitPosition.x - 5},${commitPosition.y - 5}L${commitPosition.x + 5},${commitPosition.y + 5}M${commitPosition.x - 5},${commitPosition.y + 5}L${commitPosition.x + 5},${commitPosition.y - 5}`).attr("class", `commit ${typeClass} ${commit2.id} commit${branchIndex % THEME_COLOR_LIMIT}`);
        }
    }
}, "drawCommitBullet");
var drawCommitLabel = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((gLabels, commit2, commitPosition, pos)=>{
    if (commit2.type !== commitType.CHERRY_PICK && (commit2.customId && commit2.type === commitType.MERGE || commit2.type !== commitType.MERGE) && DEFAULT_GITGRAPH_CONFIG2?.showCommitLabel) {
        const wrapper = gLabels.append("g");
        const labelBkg = wrapper.insert("rect").attr("class", "commit-label-bkg");
        const text = wrapper.append("text").attr("x", pos).attr("y", commitPosition.y + 25).attr("class", "commit-label").text(commit2.id);
        const bbox = text.node()?.getBBox();
        if (bbox) {
            labelBkg.attr("x", commitPosition.posWithOffset - bbox.width / 2 - PY).attr("y", commitPosition.y + 13.5).attr("width", bbox.width + 2 * PY).attr("height", bbox.height + 2 * PY);
            if (dir === "TB" || dir === "BT") {
                labelBkg.attr("x", commitPosition.x - (bbox.width + 4 * PX + 5)).attr("y", commitPosition.y - 12);
                text.attr("x", commitPosition.x - (bbox.width + 4 * PX)).attr("y", commitPosition.y + bbox.height - 12);
            } else text.attr("x", commitPosition.posWithOffset - bbox.width / 2);
            if (DEFAULT_GITGRAPH_CONFIG2.rotateCommitLabel) {
                if (dir === "TB" || dir === "BT") {
                    text.attr("transform", "rotate(-45, " + commitPosition.x + ", " + commitPosition.y + ")");
                    labelBkg.attr("transform", "rotate(-45, " + commitPosition.x + ", " + commitPosition.y + ")");
                } else {
                    const r_x = -7.5 - (bbox.width + 10) / 25 * 9.5;
                    const r_y = 10 + bbox.width / 25 * 8.5;
                    wrapper.attr("transform", "translate(" + r_x + ", " + r_y + ") rotate(-45, " + pos + ", " + commitPosition.y + ")");
                }
            }
        }
    }
}, "drawCommitLabel");
var drawCommitTags = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((gLabels, commit2, commitPosition, pos)=>{
    if (commit2.tags.length > 0) {
        let yOffset = 0;
        let maxTagBboxWidth = 0;
        let maxTagBboxHeight = 0;
        const tagElements = [];
        for (const tagValue of commit2.tags.reverse()){
            const rect = gLabels.insert("polygon");
            const hole = gLabels.append("circle");
            const tag = gLabels.append("text").attr("y", commitPosition.y - 16 - yOffset).attr("class", "tag-label").text(tagValue);
            const tagBbox = tag.node()?.getBBox();
            if (!tagBbox) throw new Error("Tag bbox not found");
            maxTagBboxWidth = Math.max(maxTagBboxWidth, tagBbox.width);
            maxTagBboxHeight = Math.max(maxTagBboxHeight, tagBbox.height);
            tag.attr("x", commitPosition.posWithOffset - tagBbox.width / 2);
            tagElements.push({
                tag,
                hole,
                rect,
                yOffset
            });
            yOffset += 20;
        }
        for (const { tag, hole, rect, yOffset: yOffset2 } of tagElements){
            const h2 = maxTagBboxHeight / 2;
            const ly = commitPosition.y - 19.2 - yOffset2;
            rect.attr("class", "tag-label-bkg").attr("points", `
      ${pos - maxTagBboxWidth / 2 - PX / 2},${ly + PY}  
      ${pos - maxTagBboxWidth / 2 - PX / 2},${ly - PY}
      ${commitPosition.posWithOffset - maxTagBboxWidth / 2 - PX},${ly - h2 - PY}
      ${commitPosition.posWithOffset + maxTagBboxWidth / 2 + PX},${ly - h2 - PY}
      ${commitPosition.posWithOffset + maxTagBboxWidth / 2 + PX},${ly + h2 + PY}
      ${commitPosition.posWithOffset - maxTagBboxWidth / 2 - PX},${ly + h2 + PY}`);
            hole.attr("cy", ly).attr("cx", pos - maxTagBboxWidth / 2 + PX / 2).attr("r", 1.5).attr("class", "tag-hole");
            if (dir === "TB" || dir === "BT") {
                const yOrigin = pos + yOffset2;
                rect.attr("class", "tag-label-bkg").attr("points", `
        ${commitPosition.x},${yOrigin + 2}
        ${commitPosition.x},${yOrigin - 2}
        ${commitPosition.x + LAYOUT_OFFSET},${yOrigin - h2 - 2}
        ${commitPosition.x + LAYOUT_OFFSET + maxTagBboxWidth + 4},${yOrigin - h2 - 2}
        ${commitPosition.x + LAYOUT_OFFSET + maxTagBboxWidth + 4},${yOrigin + h2 + 2}
        ${commitPosition.x + LAYOUT_OFFSET},${yOrigin + h2 + 2}`).attr("transform", "translate(12,12) rotate(45, " + commitPosition.x + "," + pos + ")");
                hole.attr("cx", commitPosition.x + PX / 2).attr("cy", yOrigin).attr("transform", "translate(12,12) rotate(45, " + commitPosition.x + "," + pos + ")");
                tag.attr("x", commitPosition.x + 5).attr("y", yOrigin + 3).attr("transform", "translate(14,14) rotate(45, " + commitPosition.x + "," + pos + ")");
            }
        }
    }
}, "drawCommitTags");
var getCommitClassType = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((commit2)=>{
    const commitSymbolType = commit2.customType ?? commit2.type;
    switch(commitSymbolType){
        case commitType.NORMAL:
            return "commit-normal";
        case commitType.REVERSE:
            return "commit-reverse";
        case commitType.HIGHLIGHT:
            return "commit-highlight";
        case commitType.MERGE:
            return "commit-merge";
        case commitType.CHERRY_PICK:
            return "commit-cherry-pick";
        default:
            return "commit-normal";
    }
}, "getCommitClassType");
var calculatePosition = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((commit2, dir2, pos, commitPos2)=>{
    const defaultCommitPosition = {
        x: 0,
        y: 0
    };
    if (commit2.parents.length > 0) {
        const closestParent = findClosestParent(commit2.parents);
        if (closestParent) {
            const parentPosition = commitPos2.get(closestParent) ?? defaultCommitPosition;
            if (dir2 === "TB") return parentPosition.y + COMMIT_STEP;
            else if (dir2 === "BT") {
                const currentPosition = commitPos2.get(commit2.id) ?? defaultCommitPosition;
                return currentPosition.y - COMMIT_STEP;
            } else return parentPosition.x + COMMIT_STEP;
        }
    } else {
        if (dir2 === "TB") return defaultPos;
        else if (dir2 === "BT") {
            const currentPosition = commitPos2.get(commit2.id) ?? defaultCommitPosition;
            return currentPosition.y - COMMIT_STEP;
        } else return 0;
    }
    return 0;
}, "calculatePosition");
var getCommitPosition = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((commit2, pos, isParallelCommits)=>{
    const posWithOffset = dir === "BT" && isParallelCommits ? pos : pos + LAYOUT_OFFSET;
    const y = dir === "TB" || dir === "BT" ? posWithOffset : branchPos.get(commit2.branch)?.pos;
    const x = dir === "TB" || dir === "BT" ? branchPos.get(commit2.branch)?.pos : posWithOffset;
    if (x === void 0 || y === void 0) throw new Error(`Position were undefined for commit ${commit2.id}`);
    return {
        x,
        y,
        posWithOffset
    };
}, "getCommitPosition");
var drawCommits = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((svg, commits, modifyGraph)=>{
    if (!DEFAULT_GITGRAPH_CONFIG2) throw new Error("GitGraph config not found");
    const gBullets = svg.append("g").attr("class", "commit-bullets");
    const gLabels = svg.append("g").attr("class", "commit-labels");
    let pos = dir === "TB" || dir === "BT" ? defaultPos : 0;
    const keys = [
        ...commits.keys()
    ];
    const isParallelCommits = DEFAULT_GITGRAPH_CONFIG2?.parallelCommits ?? false;
    const sortKeys = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((a, b)=>{
        const seqA = commits.get(a)?.seq;
        const seqB = commits.get(b)?.seq;
        return seqA !== void 0 && seqB !== void 0 ? seqA - seqB : 0;
    }, "sortKeys");
    let sortedKeys = keys.sort(sortKeys);
    if (dir === "BT") {
        if (isParallelCommits) setParallelBTPos(sortedKeys, commits, pos);
        sortedKeys = sortedKeys.reverse();
    }
    sortedKeys.forEach((key)=>{
        const commit2 = commits.get(key);
        if (!commit2) throw new Error(`Commit not found for key ${key}`);
        if (isParallelCommits) pos = calculatePosition(commit2, dir, pos, commitPos);
        const commitPosition = getCommitPosition(commit2, pos, isParallelCommits);
        if (modifyGraph) {
            const typeClass = getCommitClassType(commit2);
            const commitSymbolType = commit2.customType ?? commit2.type;
            const branchIndex = branchPos.get(commit2.branch)?.index ?? 0;
            drawCommitBullet(gBullets, commit2, commitPosition, typeClass, branchIndex, commitSymbolType);
            drawCommitLabel(gLabels, commit2, commitPosition, pos);
            drawCommitTags(gLabels, commit2, commitPosition, pos);
        }
        if (dir === "TB" || dir === "BT") commitPos.set(commit2.id, {
            x: commitPosition.x,
            y: commitPosition.posWithOffset
        });
        else commitPos.set(commit2.id, {
            x: commitPosition.posWithOffset,
            y: commitPosition.y
        });
        pos = dir === "BT" && isParallelCommits ? pos + COMMIT_STEP : pos + COMMIT_STEP + LAYOUT_OFFSET;
        if (pos > maxPos) maxPos = pos;
    });
}, "drawCommits");
var shouldRerouteArrow = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((commitA, commitB, p1, p2, allCommits)=>{
    const commitBIsFurthest = dir === "TB" || dir === "BT" ? p1.x < p2.x : p1.y < p2.y;
    const branchToGetCurve = commitBIsFurthest ? commitB.branch : commitA.branch;
    const isOnBranchToGetCurve = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((x)=>x.branch === branchToGetCurve, "isOnBranchToGetCurve");
    const isBetweenCommits = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((x)=>x.seq > commitA.seq && x.seq < commitB.seq, "isBetweenCommits");
    return [
        ...allCommits.values()
    ].some((commitX)=>{
        return isBetweenCommits(commitX) && isOnBranchToGetCurve(commitX);
    });
}, "shouldRerouteArrow");
var findLane = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((y1, y2, depth = 0)=>{
    const candidate = y1 + Math.abs(y1 - y2) / 2;
    if (depth > 5) return candidate;
    const ok = lanes.every((lane)=>Math.abs(lane - candidate) >= 10);
    if (ok) {
        lanes.push(candidate);
        return candidate;
    }
    const diff = Math.abs(y1 - y2);
    return findLane(y1, y2 - diff / 5, depth + 1);
}, "findLane");
var drawArrow = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((svg, commitA, commitB, allCommits)=>{
    const p1 = commitPos.get(commitA.id);
    const p2 = commitPos.get(commitB.id);
    if (p1 === void 0 || p2 === void 0) throw new Error(`Commit positions not found for commits ${commitA.id} and ${commitB.id}`);
    const arrowNeedsRerouting = shouldRerouteArrow(commitA, commitB, p1, p2, allCommits);
    let arc = "";
    let arc2 = "";
    let radius = 0;
    let offset = 0;
    let colorClassNum = branchPos.get(commitB.branch)?.index;
    if (commitB.type === commitType.MERGE && commitA.id !== commitB.parents[0]) colorClassNum = branchPos.get(commitA.branch)?.index;
    let lineDef;
    if (arrowNeedsRerouting) {
        arc = "A 10 10, 0, 0, 0,";
        arc2 = "A 10 10, 0, 0, 1,";
        radius = 10;
        offset = 10;
        const lineY = p1.y < p2.y ? findLane(p1.y, p2.y) : findLane(p2.y, p1.y);
        const lineX = p1.x < p2.x ? findLane(p1.x, p2.x) : findLane(p2.x, p1.x);
        if (dir === "TB") {
            if (p1.x < p2.x) lineDef = `M ${p1.x} ${p1.y} L ${lineX - radius} ${p1.y} ${arc2} ${lineX} ${p1.y + offset} L ${lineX} ${p2.y - radius} ${arc} ${lineX + offset} ${p2.y} L ${p2.x} ${p2.y}`;
            else {
                colorClassNum = branchPos.get(commitA.branch)?.index;
                lineDef = `M ${p1.x} ${p1.y} L ${lineX + radius} ${p1.y} ${arc} ${lineX} ${p1.y + offset} L ${lineX} ${p2.y - radius} ${arc2} ${lineX - offset} ${p2.y} L ${p2.x} ${p2.y}`;
            }
        } else if (dir === "BT") {
            if (p1.x < p2.x) lineDef = `M ${p1.x} ${p1.y} L ${lineX - radius} ${p1.y} ${arc} ${lineX} ${p1.y - offset} L ${lineX} ${p2.y + radius} ${arc2} ${lineX + offset} ${p2.y} L ${p2.x} ${p2.y}`;
            else {
                colorClassNum = branchPos.get(commitA.branch)?.index;
                lineDef = `M ${p1.x} ${p1.y} L ${lineX + radius} ${p1.y} ${arc2} ${lineX} ${p1.y - offset} L ${lineX} ${p2.y + radius} ${arc} ${lineX - offset} ${p2.y} L ${p2.x} ${p2.y}`;
            }
        } else if (p1.y < p2.y) lineDef = `M ${p1.x} ${p1.y} L ${p1.x} ${lineY - radius} ${arc} ${p1.x + offset} ${lineY} L ${p2.x - radius} ${lineY} ${arc2} ${p2.x} ${lineY + offset} L ${p2.x} ${p2.y}`;
        else {
            colorClassNum = branchPos.get(commitA.branch)?.index;
            lineDef = `M ${p1.x} ${p1.y} L ${p1.x} ${lineY + radius} ${arc2} ${p1.x + offset} ${lineY} L ${p2.x - radius} ${lineY} ${arc} ${p2.x} ${lineY - offset} L ${p2.x} ${p2.y}`;
        }
    } else {
        arc = "A 20 20, 0, 0, 0,";
        arc2 = "A 20 20, 0, 0, 1,";
        radius = 20;
        offset = 20;
        if (dir === "TB") {
            if (p1.x < p2.x) {
                if (commitB.type === commitType.MERGE && commitA.id !== commitB.parents[0]) lineDef = `M ${p1.x} ${p1.y} L ${p1.x} ${p2.y - radius} ${arc} ${p1.x + offset} ${p2.y} L ${p2.x} ${p2.y}`;
                else lineDef = `M ${p1.x} ${p1.y} L ${p2.x - radius} ${p1.y} ${arc2} ${p2.x} ${p1.y + offset} L ${p2.x} ${p2.y}`;
            }
            if (p1.x > p2.x) {
                arc = "A 20 20, 0, 0, 0,";
                arc2 = "A 20 20, 0, 0, 1,";
                radius = 20;
                offset = 20;
                if (commitB.type === commitType.MERGE && commitA.id !== commitB.parents[0]) lineDef = `M ${p1.x} ${p1.y} L ${p1.x} ${p2.y - radius} ${arc2} ${p1.x - offset} ${p2.y} L ${p2.x} ${p2.y}`;
                else lineDef = `M ${p1.x} ${p1.y} L ${p2.x + radius} ${p1.y} ${arc} ${p2.x} ${p1.y + offset} L ${p2.x} ${p2.y}`;
            }
            if (p1.x === p2.x) lineDef = `M ${p1.x} ${p1.y} L ${p2.x} ${p2.y}`;
        } else if (dir === "BT") {
            if (p1.x < p2.x) {
                if (commitB.type === commitType.MERGE && commitA.id !== commitB.parents[0]) lineDef = `M ${p1.x} ${p1.y} L ${p1.x} ${p2.y + radius} ${arc2} ${p1.x + offset} ${p2.y} L ${p2.x} ${p2.y}`;
                else lineDef = `M ${p1.x} ${p1.y} L ${p2.x - radius} ${p1.y} ${arc} ${p2.x} ${p1.y - offset} L ${p2.x} ${p2.y}`;
            }
            if (p1.x > p2.x) {
                arc = "A 20 20, 0, 0, 0,";
                arc2 = "A 20 20, 0, 0, 1,";
                radius = 20;
                offset = 20;
                if (commitB.type === commitType.MERGE && commitA.id !== commitB.parents[0]) lineDef = `M ${p1.x} ${p1.y} L ${p1.x} ${p2.y + radius} ${arc} ${p1.x - offset} ${p2.y} L ${p2.x} ${p2.y}`;
                else lineDef = `M ${p1.x} ${p1.y} L ${p2.x - radius} ${p1.y} ${arc} ${p2.x} ${p1.y - offset} L ${p2.x} ${p2.y}`;
            }
            if (p1.x === p2.x) lineDef = `M ${p1.x} ${p1.y} L ${p2.x} ${p2.y}`;
        } else {
            if (p1.y < p2.y) {
                if (commitB.type === commitType.MERGE && commitA.id !== commitB.parents[0]) lineDef = `M ${p1.x} ${p1.y} L ${p2.x - radius} ${p1.y} ${arc2} ${p2.x} ${p1.y + offset} L ${p2.x} ${p2.y}`;
                else lineDef = `M ${p1.x} ${p1.y} L ${p1.x} ${p2.y - radius} ${arc} ${p1.x + offset} ${p2.y} L ${p2.x} ${p2.y}`;
            }
            if (p1.y > p2.y) {
                if (commitB.type === commitType.MERGE && commitA.id !== commitB.parents[0]) lineDef = `M ${p1.x} ${p1.y} L ${p2.x - radius} ${p1.y} ${arc} ${p2.x} ${p1.y - offset} L ${p2.x} ${p2.y}`;
                else lineDef = `M ${p1.x} ${p1.y} L ${p1.x} ${p2.y + radius} ${arc2} ${p1.x + offset} ${p2.y} L ${p2.x} ${p2.y}`;
            }
            if (p1.y === p2.y) lineDef = `M ${p1.x} ${p1.y} L ${p2.x} ${p2.y}`;
        }
    }
    if (lineDef === void 0) throw new Error("Line definition not found");
    svg.append("path").attr("d", lineDef).attr("class", "arrow arrow" + colorClassNum % THEME_COLOR_LIMIT);
}, "drawArrow");
var drawArrows = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((svg, commits)=>{
    const gArrows = svg.append("g").attr("class", "commit-arrows");
    [
        ...commits.keys()
    ].forEach((key)=>{
        const commit2 = commits.get(key);
        if (commit2.parents && commit2.parents.length > 0) commit2.parents.forEach((parent)=>{
            drawArrow(gArrows, commits.get(parent), commit2, commits);
        });
    });
}, "drawArrows");
var drawBranches = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((svg, branches)=>{
    const g = svg.append("g");
    branches.forEach((branch2, index)=>{
        const adjustIndexForTheme = index % THEME_COLOR_LIMIT;
        const pos = branchPos.get(branch2.name)?.pos;
        if (pos === void 0) throw new Error(`Position not found for branch ${branch2.name}`);
        const line = g.append("line");
        line.attr("x1", 0);
        line.attr("y1", pos);
        line.attr("x2", maxPos);
        line.attr("y2", pos);
        line.attr("class", "branch branch" + adjustIndexForTheme);
        if (dir === "TB") {
            line.attr("y1", defaultPos);
            line.attr("x1", pos);
            line.attr("y2", maxPos);
            line.attr("x2", pos);
        } else if (dir === "BT") {
            line.attr("y1", maxPos);
            line.attr("x1", pos);
            line.attr("y2", defaultPos);
            line.attr("x2", pos);
        }
        lanes.push(pos);
        const name = branch2.name;
        const labelElement = drawText(name);
        const bkg = g.insert("rect");
        const branchLabel = g.insert("g").attr("class", "branchLabel");
        const label = branchLabel.insert("g").attr("class", "label branch-label" + adjustIndexForTheme);
        label.node().appendChild(labelElement);
        const bbox = labelElement.getBBox();
        bkg.attr("class", "branchLabelBkg label" + adjustIndexForTheme).attr("rx", 4).attr("ry", 4).attr("x", -bbox.width - 4 - (DEFAULT_GITGRAPH_CONFIG2?.rotateCommitLabel === true ? 30 : 0)).attr("y", -bbox.height / 2 + 8).attr("width", bbox.width + 18).attr("height", bbox.height + 4);
        label.attr("transform", "translate(" + (-bbox.width - 14 - (DEFAULT_GITGRAPH_CONFIG2?.rotateCommitLabel === true ? 30 : 0)) + ", " + (pos - bbox.height / 2 - 1) + ")");
        if (dir === "TB") {
            bkg.attr("x", pos - bbox.width / 2 - 10).attr("y", 0);
            label.attr("transform", "translate(" + (pos - bbox.width / 2 - 5) + ", 0)");
        } else if (dir === "BT") {
            bkg.attr("x", pos - bbox.width / 2 - 10).attr("y", maxPos);
            label.attr("transform", "translate(" + (pos - bbox.width / 2 - 5) + ", " + maxPos + ")");
        } else bkg.attr("transform", "translate(-19, " + (pos - bbox.height / 2) + ")");
    });
}, "drawBranches");
var setBranchPosition = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(name, pos, index, bbox, rotateCommitLabel) {
    branchPos.set(name, {
        pos,
        index
    });
    pos += 50 + (rotateCommitLabel ? 40 : 0) + (dir === "TB" || dir === "BT" ? bbox.width / 2 : 0);
    return pos;
}, "setBranchPosition");
var draw = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)(function(txt, id, ver, diagObj) {
    clear3();
    (0, _chunkDD37ZF33Mjs.log).debug("in gitgraph renderer", txt + "\n", "id:", id, ver);
    if (!DEFAULT_GITGRAPH_CONFIG2) throw new Error("GitGraph config not found");
    const rotateCommitLabel = DEFAULT_GITGRAPH_CONFIG2.rotateCommitLabel ?? false;
    const db2 = diagObj.db;
    allCommitsDict = db2.getCommits();
    const branches = db2.getBranchesAsObjArray();
    dir = db2.getDirection();
    const diagram2 = (0, _chunkDD37ZF33Mjs.select_default)(`[id="${id}"]`);
    let pos = 0;
    branches.forEach((branch2, index)=>{
        const labelElement = drawText(branch2.name);
        const g = diagram2.append("g");
        const branchLabel = g.insert("g").attr("class", "branchLabel");
        const label = branchLabel.insert("g").attr("class", "label branch-label");
        label.node()?.appendChild(labelElement);
        const bbox = labelElement.getBBox();
        pos = setBranchPosition(branch2.name, pos, index, bbox, rotateCommitLabel);
        label.remove();
        branchLabel.remove();
        g.remove();
    });
    drawCommits(diagram2, allCommitsDict, false);
    if (DEFAULT_GITGRAPH_CONFIG2.showBranches) drawBranches(diagram2, branches);
    drawArrows(diagram2, allCommitsDict);
    drawCommits(diagram2, allCommitsDict, true);
    (0, _chunkI7ZFS43CMjs.utils_default).insertTitle(diagram2, "gitTitleText", DEFAULT_GITGRAPH_CONFIG2.titleTopMargin ?? 0, db2.getDiagramTitle());
    (0, _chunkDD37ZF33Mjs.setupGraphViewbox2)(void 0, diagram2, DEFAULT_GITGRAPH_CONFIG2.diagramPadding, DEFAULT_GITGRAPH_CONFIG2.useMaxWidth);
}, "draw");
var gitGraphRenderer_default = {
    draw
};
var commit2, key, commit21, key1, commit22, key2, commit23, key3, commit24;
// src/diagrams/git/styles.js
var getStyles = /* @__PURE__ */ (0, _chunkDLQEHMXDMjs.__name)((options)=>`
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
    ].map((i)=>`
        .branch-label${i} { fill: ${options["gitBranchLabel" + i]}; }
        .commit${i} { stroke: ${options["git" + i]}; fill: ${options["git" + i]}; }
        .commit-highlight${i} { stroke: ${options["gitInv" + i]}; fill: ${options["gitInv" + i]}; }
        .label${i}  { fill: ${options["git" + i]}; }
        .arrow${i} { stroke: ${options["git" + i]}; }
        `).join("\n")}

  .branch {
    stroke-width: 1;
    stroke: ${options.lineColor};
    stroke-dasharray: 2;
  }
  .commit-label { font-size: ${options.commitLabelFontSize}; fill: ${options.commitLabelColor};}
  .commit-label-bkg { font-size: ${options.commitLabelFontSize}; fill: ${options.commitLabelBackground}; opacity: 0.5; }
  .tag-label { font-size: ${options.tagLabelFontSize}; fill: ${options.tagLabelColor};}
  .tag-label-bkg { fill: ${options.tagLabelBackground}; stroke: ${options.tagLabelBorder}; }
  .tag-hole { fill: ${options.textColor}; }

  .commit-merge {
    stroke: ${options.primaryColor};
    fill: ${options.primaryColor};
  }
  .commit-reverse {
    stroke: ${options.primaryColor};
    fill: ${options.primaryColor};
    stroke-width: 3;
  }
  .commit-highlight-outer {
  }
  .commit-highlight-inner {
    stroke: ${options.primaryColor};
    fill: ${options.primaryColor};
  }

  .arrow { stroke-width: 8; stroke-linecap: round; fill: none}
  .gitTitleText {
    text-anchor: middle;
    font-size: 18px;
    fill: ${options.textColor};
  }
`, "getStyles");
var styles_default = getStyles;
// src/diagrams/git/gitGraphDiagram.ts
var diagram = {
    parser,
    db,
    renderer: gitGraphRenderer_default,
    styles: styles_default
};

},{"./chunk-YJGJQOYZ.mjs":"21Dvn","./chunk-K2ZEYYM2.mjs":"dF5aJ","./chunk-M52XIDDU.mjs":"brg65","./chunk-76YUXBKW.mjs":"2wQrm","./chunk-W7WFRJCB.mjs":"gw2Ak","./chunk-I7ZFS43C.mjs":"huUtc","./chunk-GKOISANM.mjs":"5yZtl","./chunk-DD37ZF33.mjs":"f4pI5","./chunk-HCCMVKPJ.mjs":"7LBdV","./chunk-3YXWICEL.mjs":"hbCHI","./chunk-HBGMPAD7.mjs":"i5fzF","./chunk-TZBO7MLI.mjs":"fY3tK","./chunk-GRZAG2UZ.mjs":"d1pnj","./chunk-HD3LK5B5.mjs":"lpo2u","./chunk-DLQEHMXD.mjs":"h5YRf","@parcel/transformer-js/src/esmodule-helpers.js":"1cdyk"}],"21Dvn":[function(require,module,exports,__globalThis) {
var parcelHelpers = require("@parcel/transformer-js/src/esmodule-helpers.js");
parcelHelpers.defineInteropFlag(exports);
parcelHelpers.export(exports, "ImperativeState", ()=>ImperativeState);
var _chunkDLQEHMXDMjs = require("./chunk-DLQEHMXD.mjs");
// src/utils/imperativeState.ts
var ImperativeState = class {
    /**
   * @param init - Function that creates the default state.
   */ constructor(init){
        this.init = init;
        this.records = this.init();
    }
    static #_ = (0, _chunkDLQEHMXDMjs.__name)(this, "ImperativeState");
    reset() {
        this.records = this.init();
    }
};

},{"./chunk-DLQEHMXD.mjs":"h5YRf","@parcel/transformer-js/src/esmodule-helpers.js":"1cdyk"}],"dF5aJ":[function(require,module,exports,__globalThis) {
var parcelHelpers = require("@parcel/transformer-js/src/esmodule-helpers.js");
parcelHelpers.defineInteropFlag(exports);
parcelHelpers.export(exports, "populateCommonDb", ()=>populateCommonDb);
var _chunkDLQEHMXDMjs = require("./chunk-DLQEHMXD.mjs");
// src/diagrams/common/populateCommonDb.ts
function populateCommonDb(ast, db) {
    if (ast.accDescr) db.setAccDescription?.(ast.accDescr);
    if (ast.accTitle) db.setAccTitle?.(ast.accTitle);
    if (ast.title) db.setDiagramTitle?.(ast.title);
}
(0, _chunkDLQEHMXDMjs.__name)(populateCommonDb, "populateCommonDb");

},{"./chunk-DLQEHMXD.mjs":"h5YRf","@parcel/transformer-js/src/esmodule-helpers.js":"1cdyk"}]},["7g22s"], null, "parcelRequire6955", {})

//# sourceMappingURL=gitGraphDiagram-L4TVWIEC.af9c342c.js.map
