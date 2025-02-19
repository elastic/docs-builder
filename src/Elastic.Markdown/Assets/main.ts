import {initTocNav} from "./toc-nav";
import {initHighlight} from "./hljs";
import {initTabs} from "./tabs";
import {initCopyButton} from "./copybutton";
import {initNav} from "./pages-nav";

document.body.addEventListener('htmx:load', function(evt) {
	document.querySelector("#content").scrollIntoView({ behavior: 'instant', block: 'start' });
	initTocNav();
	initHighlight();
	initCopyButton();
	initTabs();
	initNav();
});

// initNav();
