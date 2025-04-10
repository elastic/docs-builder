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
})({"7DFGH":[function(require,module,exports,__globalThis) {
var global = arguments[3];
var HMR_HOST = null;
var HMR_PORT = 1234;
var HMR_SERVER_PORT = 1234;
var HMR_SECURE = false;
var HMR_ENV_HASH = "febfef3c5b467b16";
var HMR_USE_SSE = false;
module.bundle.HMR_BUNDLE_ID = "b9381e6ab276ddec";
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

},{}],"9TqVD":[function(require,module,exports,__globalThis) {
var parcelHelpers = require("@parcel/transformer-js/src/esmodule-helpers.js");
parcelHelpers.defineInteropFlag(exports);
parcelHelpers.export(exports, "diagram", ()=>Rt);
var _chunkF4773GRLMjs = require("./chunk-F4773GRL.mjs");
var _chunk4SRTBRONMjs = require("./chunk-4SRTBRON.mjs");
var _chunkUWHJNN4QMjs = require("./chunk-UWHJNN4Q.mjs");
var _chunkCBSWTUHPMjs = require("./chunk-CBSWTUHP.mjs");
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
var A = {}, Z = (0, _chunkGTKDMUJJMjs.a)((e, n)=>{
    A[e] = n;
}, "set"), K = (0, _chunkGTKDMUJJMjs.a)((e)=>A[e], "get"), Y = (0, _chunkGTKDMUJJMjs.a)(()=>Object.keys(A), "keys"), Q = (0, _chunkGTKDMUJJMjs.a)(()=>Y().length, "size"), v = {
    get: K,
    set: Z,
    keys: Y,
    size: Q
};
var V = (0, _chunkGTKDMUJJMjs.a)((e)=>e.append("circle").attr("class", "start-state").attr("r", (0, _chunkNQURTBEVMjs.X)().state.sizeUnit).attr("cx", (0, _chunkNQURTBEVMjs.X)().state.padding + (0, _chunkNQURTBEVMjs.X)().state.sizeUnit).attr("cy", (0, _chunkNQURTBEVMjs.X)().state.padding + (0, _chunkNQURTBEVMjs.X)().state.sizeUnit), "drawStartState"), D = (0, _chunkGTKDMUJJMjs.a)((e)=>e.append("line").style("stroke", "grey").style("stroke-dasharray", "3").attr("x1", (0, _chunkNQURTBEVMjs.X)().state.textHeight).attr("class", "divider").attr("x2", (0, _chunkNQURTBEVMjs.X)().state.textHeight * 2).attr("y1", 0).attr("y2", 0), "drawDivider"), tt = (0, _chunkGTKDMUJJMjs.a)((e, n)=>{
    let s = e.append("text").attr("x", 2 * (0, _chunkNQURTBEVMjs.X)().state.padding).attr("y", (0, _chunkNQURTBEVMjs.X)().state.textHeight + 2 * (0, _chunkNQURTBEVMjs.X)().state.padding).attr("font-size", (0, _chunkNQURTBEVMjs.X)().state.fontSize).attr("class", "state-title").text(n.id), d = s.node().getBBox();
    return e.insert("rect", ":first-child").attr("x", (0, _chunkNQURTBEVMjs.X)().state.padding).attr("y", (0, _chunkNQURTBEVMjs.X)().state.padding).attr("width", d.width + 2 * (0, _chunkNQURTBEVMjs.X)().state.padding).attr("height", d.height + 2 * (0, _chunkNQURTBEVMjs.X)().state.padding).attr("rx", (0, _chunkNQURTBEVMjs.X)().state.radius), s;
}, "drawSimpleState"), et = (0, _chunkGTKDMUJJMjs.a)((e, n)=>{
    let s = (0, _chunkGTKDMUJJMjs.a)(function(p, y, w) {
        let k = p.append("tspan").attr("x", 2 * (0, _chunkNQURTBEVMjs.X)().state.padding).text(y);
        w || k.attr("dy", (0, _chunkNQURTBEVMjs.X)().state.textHeight);
    }, "addTspan"), r = e.append("text").attr("x", 2 * (0, _chunkNQURTBEVMjs.X)().state.padding).attr("y", (0, _chunkNQURTBEVMjs.X)().state.textHeight + 1.3 * (0, _chunkNQURTBEVMjs.X)().state.padding).attr("font-size", (0, _chunkNQURTBEVMjs.X)().state.fontSize).attr("class", "state-title").text(n.descriptions[0]).node().getBBox(), h = r.height, x = e.append("text").attr("x", (0, _chunkNQURTBEVMjs.X)().state.padding).attr("y", h + (0, _chunkNQURTBEVMjs.X)().state.padding * .4 + (0, _chunkNQURTBEVMjs.X)().state.dividerMargin + (0, _chunkNQURTBEVMjs.X)().state.textHeight).attr("class", "state-description"), i = !0, o = !0;
    n.descriptions.forEach(function(p) {
        i || (s(x, p, o), o = !1), i = !1;
    });
    let m = e.append("line").attr("x1", (0, _chunkNQURTBEVMjs.X)().state.padding).attr("y1", (0, _chunkNQURTBEVMjs.X)().state.padding + h + (0, _chunkNQURTBEVMjs.X)().state.dividerMargin / 2).attr("y2", (0, _chunkNQURTBEVMjs.X)().state.padding + h + (0, _chunkNQURTBEVMjs.X)().state.dividerMargin / 2).attr("class", "descr-divider"), f = x.node().getBBox(), c = Math.max(f.width, r.width);
    return m.attr("x2", c + 3 * (0, _chunkNQURTBEVMjs.X)().state.padding), e.insert("rect", ":first-child").attr("x", (0, _chunkNQURTBEVMjs.X)().state.padding).attr("y", (0, _chunkNQURTBEVMjs.X)().state.padding).attr("width", c + 2 * (0, _chunkNQURTBEVMjs.X)().state.padding).attr("height", f.height + h + 2 * (0, _chunkNQURTBEVMjs.X)().state.padding).attr("rx", (0, _chunkNQURTBEVMjs.X)().state.radius), e;
}, "drawDescrState"), $ = (0, _chunkGTKDMUJJMjs.a)((e, n, s)=>{
    let d = (0, _chunkNQURTBEVMjs.X)().state.padding, r = 2 * (0, _chunkNQURTBEVMjs.X)().state.padding, h = e.node().getBBox(), x = h.width, i = h.x, o = e.append("text").attr("x", 0).attr("y", (0, _chunkNQURTBEVMjs.X)().state.titleShift).attr("font-size", (0, _chunkNQURTBEVMjs.X)().state.fontSize).attr("class", "state-title").text(n.id), f = o.node().getBBox().width + r, c = Math.max(f, x);
    c === x && (c = c + r);
    let p, y = e.node().getBBox();
    n.doc, p = i - d, f > x && (p = (x - c) / 2 + d), Math.abs(i - y.x) < d && f > x && (p = i - (f - x) / 2);
    let w = 1 - (0, _chunkNQURTBEVMjs.X)().state.textHeight;
    return e.insert("rect", ":first-child").attr("x", p).attr("y", w).attr("class", s ? "alt-composit" : "composit").attr("width", c).attr("height", y.height + (0, _chunkNQURTBEVMjs.X)().state.textHeight + (0, _chunkNQURTBEVMjs.X)().state.titleShift + 1).attr("rx", "0"), o.attr("x", p + d), f <= x && o.attr("x", i + (c - r) / 2 - f / 2 + d), e.insert("rect", ":first-child").attr("x", p).attr("y", (0, _chunkNQURTBEVMjs.X)().state.titleShift - (0, _chunkNQURTBEVMjs.X)().state.textHeight - (0, _chunkNQURTBEVMjs.X)().state.padding).attr("width", c).attr("height", (0, _chunkNQURTBEVMjs.X)().state.textHeight * 3).attr("rx", (0, _chunkNQURTBEVMjs.X)().state.radius), e.insert("rect", ":first-child").attr("x", p).attr("y", (0, _chunkNQURTBEVMjs.X)().state.titleShift - (0, _chunkNQURTBEVMjs.X)().state.textHeight - (0, _chunkNQURTBEVMjs.X)().state.padding).attr("width", c).attr("height", y.height + 3 + 2 * (0, _chunkNQURTBEVMjs.X)().state.textHeight).attr("rx", (0, _chunkNQURTBEVMjs.X)().state.radius), e;
}, "addTitleAndBox"), it = (0, _chunkGTKDMUJJMjs.a)((e)=>(e.append("circle").attr("class", "end-state-outer").attr("r", (0, _chunkNQURTBEVMjs.X)().state.sizeUnit + (0, _chunkNQURTBEVMjs.X)().state.miniPadding).attr("cx", (0, _chunkNQURTBEVMjs.X)().state.padding + (0, _chunkNQURTBEVMjs.X)().state.sizeUnit + (0, _chunkNQURTBEVMjs.X)().state.miniPadding).attr("cy", (0, _chunkNQURTBEVMjs.X)().state.padding + (0, _chunkNQURTBEVMjs.X)().state.sizeUnit + (0, _chunkNQURTBEVMjs.X)().state.miniPadding), e.append("circle").attr("class", "end-state-inner").attr("r", (0, _chunkNQURTBEVMjs.X)().state.sizeUnit).attr("cx", (0, _chunkNQURTBEVMjs.X)().state.padding + (0, _chunkNQURTBEVMjs.X)().state.sizeUnit + 2).attr("cy", (0, _chunkNQURTBEVMjs.X)().state.padding + (0, _chunkNQURTBEVMjs.X)().state.sizeUnit + 2)), "drawEndState"), nt = (0, _chunkGTKDMUJJMjs.a)((e, n)=>{
    let s = (0, _chunkNQURTBEVMjs.X)().state.forkWidth, d = (0, _chunkNQURTBEVMjs.X)().state.forkHeight;
    if (n.parentId) {
        let r = s;
        s = d, d = r;
    }
    return e.append("rect").style("stroke", "black").style("fill", "black").attr("width", s).attr("height", d).attr("x", (0, _chunkNQURTBEVMjs.X)().state.padding).attr("y", (0, _chunkNQURTBEVMjs.X)().state.padding);
}, "drawForkJoinState");
var at = (0, _chunkGTKDMUJJMjs.a)((e, n, s, d)=>{
    let r = 0, h = d.append("text");
    h.style("text-anchor", "start"), h.attr("class", "noteText");
    let x = e.replace(/\r\n/g, "<br/>");
    x = x.replace(/\n/g, "<br/>");
    let i = x.split((0, _chunkNQURTBEVMjs.L).lineBreakRegex), o = 1.25 * (0, _chunkNQURTBEVMjs.X)().state.noteMargin;
    for (let m of i){
        let f = m.trim();
        if (f.length > 0) {
            let c = h.append("tspan");
            if (c.text(f), o === 0) {
                let p = c.node().getBBox();
                o += p.height;
            }
            r += o, c.attr("x", n + (0, _chunkNQURTBEVMjs.X)().state.noteMargin), c.attr("y", s + r + 1.25 * (0, _chunkNQURTBEVMjs.X)().state.noteMargin);
        }
    }
    return {
        textWidth: h.node().getBBox().width,
        textHeight: r
    };
}, "_drawLongText"), rt = (0, _chunkGTKDMUJJMjs.a)((e, n)=>{
    n.attr("class", "state-note");
    let s = n.append("rect").attr("x", 0).attr("y", (0, _chunkNQURTBEVMjs.X)().state.padding), d = n.append("g"), { textWidth: r, textHeight: h } = at(e, 0, 0, d);
    return s.attr("height", h + 2 * (0, _chunkNQURTBEVMjs.X)().state.noteMargin), s.attr("width", r + (0, _chunkNQURTBEVMjs.X)().state.noteMargin * 2), s;
}, "drawNote"), C = (0, _chunkGTKDMUJJMjs.a)(function(e, n) {
    let s = n.id, d = {
        id: s,
        label: n.id,
        width: 0,
        height: 0
    }, r = e.append("g").attr("id", s).attr("class", "stateGroup");
    n.type === "start" && V(r), n.type === "end" && it(r), (n.type === "fork" || n.type === "join") && nt(r, n), n.type === "note" && rt(n.note.text, r), n.type === "divider" && D(r), n.type === "default" && n.descriptions.length === 0 && tt(r, n), n.type === "default" && n.descriptions.length > 0 && et(r, n);
    let h = r.node().getBBox();
    return d.width = h.width + 2 * (0, _chunkNQURTBEVMjs.X)().state.padding, d.height = h.height + 2 * (0, _chunkNQURTBEVMjs.X)().state.padding, v.set(s, d), d;
}, "drawState"), I = 0, _ = (0, _chunkGTKDMUJJMjs.a)(function(e, n, s) {
    let d = (0, _chunkGTKDMUJJMjs.a)(function(o) {
        switch(o){
            case (0, _chunkF4773GRLMjs.c).relationType.AGGREGATION:
                return "aggregation";
            case (0, _chunkF4773GRLMjs.c).relationType.EXTENSION:
                return "extension";
            case (0, _chunkF4773GRLMjs.c).relationType.COMPOSITION:
                return "composition";
            case (0, _chunkF4773GRLMjs.c).relationType.DEPENDENCY:
                return "dependency";
        }
    }, "getRelationType");
    n.points = n.points.filter((o)=>!Number.isNaN(o.y));
    let r = n.points, h = (0, _chunkNQURTBEVMjs.Ca)().x(function(o) {
        return o.x;
    }).y(function(o) {
        return o.y;
    }).curve((0, _chunkNQURTBEVMjs.Ga)), x = e.append("path").attr("d", h(r)).attr("id", "edge" + I).attr("class", "transition"), i = "";
    if ((0, _chunkNQURTBEVMjs.X)().state.arrowMarkerAbsolute && (i = window.location.protocol + "//" + window.location.host + window.location.pathname + window.location.search, i = i.replace(/\(/g, "\\("), i = i.replace(/\)/g, "\\)")), x.attr("marker-end", "url(" + i + "#" + d((0, _chunkF4773GRLMjs.c).relationType.DEPENDENCY) + "End)"), s.title !== void 0) {
        let o = e.append("g").attr("class", "stateLabel"), { x: m, y: f } = (0, _chunkAC3VT7B7Mjs.m).calcLabelPosition(n.points), c = (0, _chunkNQURTBEVMjs.L).getRows(s.title), p = 0, y = [], w = 0, k = 0;
        for(let a = 0; a <= c.length; a++){
            let u = o.append("text").attr("text-anchor", "middle").text(c[a]).attr("x", m).attr("y", f + p), l = u.node().getBBox();
            w = Math.max(w, l.width), k = Math.min(k, l.x), (0, _chunkNQURTBEVMjs.b).info(l.x, m, f + p), p === 0 && (p = u.node().getBBox().height, (0, _chunkNQURTBEVMjs.b).info("Title height", p, f)), y.push(u);
        }
        let M = p * c.length;
        if (c.length > 1) {
            let a = (c.length - 1) * p * .5;
            y.forEach((u, l)=>u.attr("y", f + l * p - a)), M = p * c.length;
        }
        let H = o.node().getBBox();
        o.insert("rect", ":first-child").attr("class", "box").attr("x", m - w / 2 - (0, _chunkNQURTBEVMjs.X)().state.padding / 2).attr("y", f - M / 2 - (0, _chunkNQURTBEVMjs.X)().state.padding / 2 - 3.5).attr("width", w + (0, _chunkNQURTBEVMjs.X)().state.padding).attr("height", M + (0, _chunkNQURTBEVMjs.X)().state.padding), (0, _chunkNQURTBEVMjs.b).info(H);
    }
    I++;
}, "drawEdge");
var S, G = {}, ot = (0, _chunkGTKDMUJJMjs.a)(function() {}, "setConf"), st = (0, _chunkGTKDMUJJMjs.a)(function(e) {
    e.append("defs").append("marker").attr("id", "dependencyEnd").attr("refX", 19).attr("refY", 7).attr("markerWidth", 20).attr("markerHeight", 28).attr("orient", "auto").append("path").attr("d", "M 19,7 L9,13 L14,7 L9,1 Z");
}, "insertMarkers"), dt = (0, _chunkGTKDMUJJMjs.a)(function(e, n, s, d) {
    S = (0, _chunkNQURTBEVMjs.X)().state;
    let r = (0, _chunkNQURTBEVMjs.X)().securityLevel, h;
    r === "sandbox" && (h = (0, _chunkNQURTBEVMjs.fa)("#i" + n));
    let x = r === "sandbox" ? (0, _chunkNQURTBEVMjs.fa)(h.nodes()[0].contentDocument.body) : (0, _chunkNQURTBEVMjs.fa)("body"), i = r === "sandbox" ? h.nodes()[0].contentDocument : document;
    (0, _chunkNQURTBEVMjs.b).debug("Rendering diagram " + e);
    let o = x.select(`[id='${n}']`);
    st(o);
    let m = d.db.getRootDoc();
    j(m, o, void 0, !1, x, i, d);
    let f = S.padding, c = o.node().getBBox(), p = c.width + f * 2, y = c.height + f * 2, w = p * 1.75;
    (0, _chunkNQURTBEVMjs.M)(o, y, w, S.useMaxWidth), o.attr("viewBox", `${c.x - S.padding}  ${c.y - S.padding} ` + p + " " + y);
}, "draw"), ct = (0, _chunkGTKDMUJJMjs.a)((e)=>e ? e.length * S.fontSizeFactor : 1, "getLabelWidth"), j = (0, _chunkGTKDMUJJMjs.a)((e, n, s, d, r, h, x)=>{
    let i = new (0, _chunk6XGRHI2AMjs.a)({
        compound: !0,
        multigraph: !0
    }), o, m = !0;
    for(o = 0; o < e.length; o++)if (e[o].stmt === "relation") {
        m = !1;
        break;
    }
    s ? i.setGraph({
        rankdir: "LR",
        multigraph: !0,
        compound: !0,
        ranker: "tight-tree",
        ranksep: m ? 1 : S.edgeLengthFactor,
        nodeSep: m ? 1 : 50,
        isMultiGraph: !0
    }) : i.setGraph({
        rankdir: "TB",
        multigraph: !0,
        compound: !0,
        ranksep: m ? 1 : S.edgeLengthFactor,
        nodeSep: m ? 1 : 50,
        ranker: "tight-tree",
        isMultiGraph: !0
    }), i.setDefaultEdgeLabel(function() {
        return {};
    }), x.db.extract(e);
    let f = x.db.getStates(), c = x.db.getRelations(), p = Object.keys(f), y = !0;
    for (let a of p){
        let u = f[a];
        s && (u.parentId = s);
        let l;
        if (u.doc) {
            let B = n.append("g").attr("id", u.id).attr("class", "stateGroup");
            if (l = j(u.doc, B, u.id, !d, r, h, x), y) {
                B = $(B, u, d);
                let E = B.node().getBBox();
                l.width = E.width, l.height = E.height + S.padding / 2, G[u.id] = {
                    y: S.compositTitleSize
                };
            } else {
                let E = B.node().getBBox();
                l.width = E.width, l.height = E.height;
            }
        } else l = C(n, u, i);
        if (u.note) {
            let B = {
                descriptions: [],
                id: u.id + "-note",
                note: u.note,
                type: "note"
            }, E = C(n, B, i);
            u.note.position === "left of" ? (i.setNode(l.id + "-note", E), i.setNode(l.id, l)) : (i.setNode(l.id, l), i.setNode(l.id + "-note", E)), i.setParent(l.id, l.id + "-group"), i.setParent(l.id + "-note", l.id + "-group");
        } else i.setNode(l.id, l);
    }
    (0, _chunkNQURTBEVMjs.b).debug("Count=", i.nodeCount(), i);
    let w = 0;
    c.forEach(function(a) {
        w++, (0, _chunkNQURTBEVMjs.b).debug("Setting edge", a), i.setEdge(a.id1, a.id2, {
            relation: a,
            width: ct(a.title),
            height: S.labelHeight * (0, _chunkNQURTBEVMjs.L).getRows(a.title).length,
            labelpos: "c"
        }, "id" + w);
    }), (0, _chunkBOP2KBYHMjs.a)(i), (0, _chunkNQURTBEVMjs.b).debug("Graph after layout", i.nodes());
    let k = n.node();
    i.nodes().forEach(function(a) {
        a !== void 0 && i.node(a) !== void 0 ? ((0, _chunkNQURTBEVMjs.b).warn("Node " + a + ": " + JSON.stringify(i.node(a))), r.select("#" + k.id + " #" + a).attr("transform", "translate(" + (i.node(a).x - i.node(a).width / 2) + "," + (i.node(a).y + (G[a] ? G[a].y : 0) - i.node(a).height / 2) + " )"), r.select("#" + k.id + " #" + a).attr("data-x-shift", i.node(a).x - i.node(a).width / 2), h.querySelectorAll("#" + k.id + " #" + a + " .divider").forEach((l)=>{
            let B = l.parentElement, E = 0, T = 0;
            B && (B.parentElement && (E = B.parentElement.getBBox().width), T = parseInt(B.getAttribute("data-x-shift"), 10), Number.isNaN(T) && (T = 0)), l.setAttribute("x1", 0 - T + 8), l.setAttribute("x2", E - T - 8);
        })) : (0, _chunkNQURTBEVMjs.b).debug("No Node " + a + ": " + JSON.stringify(i.node(a)));
    });
    let M = k.getBBox();
    i.edges().forEach(function(a) {
        a !== void 0 && i.edge(a) !== void 0 && ((0, _chunkNQURTBEVMjs.b).debug("Edge " + a.v + " -> " + a.w + ": " + JSON.stringify(i.edge(a))), _(n, i.edge(a), i.edge(a).relation));
    }), M = k.getBBox();
    let H = {
        id: s || "root",
        label: s || "root",
        width: 0,
        height: 0
    };
    return H.width = M.width + 2 * S.padding, H.height = M.height + 2 * S.padding, (0, _chunkNQURTBEVMjs.b).debug("Doc rendered", H, i), H;
}, "renderDoc"), q = {
    setConf: ot,
    draw: dt
};
var Rt = {
    parser: (0, _chunkF4773GRLMjs.a),
    db: (0, _chunkF4773GRLMjs.c),
    renderer: q,
    styles: (0, _chunkF4773GRLMjs.d),
    init: (0, _chunkGTKDMUJJMjs.a)((e)=>{
        e.state || (e.state = {}), e.state.arrowMarkerAbsolute = e.arrowMarkerAbsolute, (0, _chunkF4773GRLMjs.c).clear();
    }, "init")
};

},{"./chunk-F4773GRL.mjs":"cb8LM","./chunk-4SRTBRON.mjs":"51F1J","./chunk-UWHJNN4Q.mjs":"6LAlC","./chunk-CBSWTUHP.mjs":"f84n1","./chunk-RRFB4HDS.mjs":"7zFn0","./chunk-U6LOUQAF.mjs":"v9pSW","./chunk-KMOJB3TB.mjs":"aJH4M","./chunk-BOP2KBYH.mjs":"klimL","./chunk-6XGRHI2A.mjs":"fUQIF","./chunk-AC3VT7B7.mjs":"1eiUz","./chunk-TI4EEUUG.mjs":"8SKrN","./chunk-NQURTBEV.mjs":"iASFe","./chunk-BKDDFIKN.mjs":"hADfH","./chunk-YPUTD6PB.mjs":"aSdv1","./chunk-6BY5RJGC.mjs":"bRXnR","./chunk-GTKDMUJJ.mjs":"fruhx","@parcel/transformer-js/src/esmodule-helpers.js":"1cdyk"}],"51F1J":[function(require,module,exports,__globalThis) {
var parcelHelpers = require("@parcel/transformer-js/src/esmodule-helpers.js");
parcelHelpers.defineInteropFlag(exports);
parcelHelpers.export(exports, "a", ()=>f);
parcelHelpers.export(exports, "b", ()=>B);
var _chunkNQURTBEVMjs = require("./chunk-NQURTBEV.mjs");
var _chunkGTKDMUJJMjs = require("./chunk-GTKDMUJJ.mjs");
var f = (0, _chunkGTKDMUJJMjs.a)((t, e)=>{
    let o;
    return e === "sandbox" && (o = (0, _chunkNQURTBEVMjs.fa)("#i" + t)), (e === "sandbox" ? (0, _chunkNQURTBEVMjs.fa)(o.nodes()[0].contentDocument.body) : (0, _chunkNQURTBEVMjs.fa)("body")).select(`[id="${t}"]`);
}, "getDiagramElement");
var B = (0, _chunkGTKDMUJJMjs.a)((t, e, o, s)=>{
    t.attr("class", o);
    let { width: r, height: a, x: d, y: l } = u(t, e);
    (0, _chunkNQURTBEVMjs.M)(t, a, r, s);
    let c = b(d, l, r, a, e);
    t.attr("viewBox", c), (0, _chunkNQURTBEVMjs.b).debug(`viewBox configured: ${c} with padding: ${e}`);
}, "setupViewPortForSVG"), u = (0, _chunkGTKDMUJJMjs.a)((t, e)=>{
    let o = t.node()?.getBBox() || {
        width: 0,
        height: 0,
        x: 0,
        y: 0
    };
    return {
        width: o.width + e * 2,
        height: o.height + e * 2,
        x: o.x,
        y: o.y
    };
}, "calculateDimensionsWithPadding"), b = (0, _chunkGTKDMUJJMjs.a)((t, e, o, s, r)=>`${t - r} ${e - r} ${o} ${s}`, "createViewBox");

},{"./chunk-NQURTBEV.mjs":"iASFe","./chunk-GTKDMUJJ.mjs":"fruhx","@parcel/transformer-js/src/esmodule-helpers.js":"1cdyk"}],"fUQIF":[function(require,module,exports,__globalThis) {
var parcelHelpers = require("@parcel/transformer-js/src/esmodule-helpers.js");
parcelHelpers.defineInteropFlag(exports);
parcelHelpers.export(exports, "a", ()=>b);
var _chunkBKDDFIKNMjs = require("./chunk-BKDDFIKN.mjs");
var _chunkYPUTD6PBMjs = require("./chunk-YPUTD6PB.mjs");
var _chunk6BY5RJGCMjs = require("./chunk-6BY5RJGC.mjs");
var _chunkGTKDMUJJMjs = require("./chunk-GTKDMUJJ.mjs");
var j = "\0", f = "\0", D = "", b = class {
    static #_ = (0, _chunkGTKDMUJJMjs.a)(this, "Graph");
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

},{"./chunk-BKDDFIKN.mjs":"hADfH","./chunk-YPUTD6PB.mjs":"aSdv1","./chunk-6BY5RJGC.mjs":"bRXnR","./chunk-GTKDMUJJ.mjs":"fruhx","@parcel/transformer-js/src/esmodule-helpers.js":"1cdyk"}]},["7DFGH"], null, "parcelRequire6955", {})

//# sourceMappingURL=stateDiagram-S3XGF733.b276ddec.js.map
