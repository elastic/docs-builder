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
