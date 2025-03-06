import "htmx.org"
import "htmx-ext-preload"
import {initTocNav} from "./toc-nav";
import {initHighlight} from "./hljs";
import {initTabs} from "./tabs";
import {initCopyButton} from "./copybutton";
import {initNav} from "./pages-nav";
import {$, $$} from "select-dom"
import htmx from "htmx.org";

document.addEventListener('htmx:load', function() {
	initTocNav();
	initHighlight();
	initCopyButton();
	initTabs();
	initNav();
});

document.body.addEventListener('htmx:oobAfterSwap', function(event) {
	if (event.target.id === 'markdown-content') {
		window.scrollTo(0, 0);
	}
});

document.body.addEventListener('htmx:pushedIntoHistory', function(event) {
	const currentNavItem = $$('.current');
	currentNavItem.forEach(el => {
		el.classList.remove('current');
	})
	// @ts-ignore
	const navItems = $$('a[href="' + event.detail.path + '"]');
	navItems.forEach(navItem => {
		navItem.classList.add('current');
	});
});

document.body.addEventListener('htmx:responseError', function(event) {
	// event.preventDefault();
	
	if (event.detail.xhr.status === 404) {
		window.location.assign(event.detail.pathInfo.requestPath);
	}
	
	// const rootPath = $('body').dataset.rootPath;
	// window.history.pushState({ path: event.detail.pathInfo.requestPath }, '', event.detail.pathInfo.requestPath);
	// htmx.ajax('get', rootPath + 'not-found', { select: '#main-container', target: '#main-container' }).then(() => {
	// });
});
