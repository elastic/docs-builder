// @ts-nocheck
import "htmx.org"
import "htmx-ext-preload"
import {initTocNav} from "./toc-nav";
import {initHighlight} from "./hljs";
import {initTabs} from "./tabs";
import {initCopyButton} from "./copybutton";
import {initNav} from "./pages-nav";
import {$$} from "select-dom"

document.addEventListener('htmx:load', function() {
	initTocNav();
	initHighlight();
	initCopyButton();
	initTabs();
	initNav();
});

document.body.addEventListener('htmx:oobBeforeSwap', function(event) {
	// This is needed to scroll to the top of the page when the content is swapped
	if (event.target.id === 'markdown-content' || event.target.id === 'content-container') {
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
	if (event.detail.xhr.status === 404) {
		window.location.assign(event.detail.pathInfo.requestPath);
	}
});
