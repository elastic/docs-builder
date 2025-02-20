import {initTocNav} from "./toc-nav";
import {initTabs} from "./tabs";
import {initCopyButton} from "./copybutton";
import {initNav} from "./pages-nav";
import {initHighlight} from "./hljs";



up.link.config.followSelectors.push('a[href]')
up.link.config.preloadSelectors.push('a[href]')


up.compiler('.markdown-content, #toc-nav', () => {
	window.scrollTo(0, 0);
	const destroyTocNav = initTocNav();
	initNav();
	initHighlight();
	initCopyButton();
	initTabs();
	return () => {
		destroyTocNav();
	}
})
