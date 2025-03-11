// @ts-nocheck
import htmx from "htmx.org"
import "htmx-ext-preload"
import {initTocNav} from "./toc-nav";
import {initHighlight} from "./hljs";
import {initTabs} from "./tabs";
import {initCopyButton} from "./copybutton";
import {initNav} from "./pages-nav";
import {$, $$} from "select-dom"

document.addEventListener('htmx:load', function() {
	initTocNav();
	initHighlight();
	initCopyButton();
	initTabs();
	initNav();


	const docsetSelector = $('#docset-selector');

	docsetSelector.addEventListener('change', (event) => {
		console.log(event.target.value)
		htmx.ajax('get', event.target.value, { target: '#pages-nav', select: '#pages-nav' }).then()
	})

});

document.body.addEventListener('htmx:oobBeforeSwap', function(event) {
	// This is needed to scroll to the top of the page when the content is swapped
	if (event.target.id === 'markdown-content' || event.target.id === 'content-container') {
		window.scrollTo(0, 0);
	}
});

document.body.addEventListener('htmx:pushedIntoHistory', function(event) {
	const pagesNav = $('#pages-nav');
	const currentNavItem = $$('.current', pagesNav);
	currentNavItem.forEach(el => {
		el.classList.remove('current');
	})
	const navItems = $$('a[href="' + event.detail.path + '"]', pagesNav);
	navItems.forEach(navItem => {
		navItem.classList.add('current');
	});
});

document.body.addEventListener('htmx:responseError', function(event) {
	// If you get a 404 error while clicking on a hx-get link, actually open the link
	// This is needed because the browser doesn't update the URL when the response is a 404
	// In production, cloudfront handles serving the 404 page.
	// Locally, the DocumentationWebHost handles it.
	// On previews, a generic 404 page is shown.
	if (event.detail.xhr.status === 404) {
		window.location.assign(event.detail.pathInfo.requestPath);
	}
});
