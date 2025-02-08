import hljs from "highlight.js";
import {mergeHTMLPlugin} from "./hljs-merge-html-plugin";
import {$, $$} from 'select-dom/strict.js'

hljs.registerLanguage('apiheader', function() {
    return {
        case_insensitive: true, // language is case-insensitive
        keywords: 'GET POST PUT DELETE HEAD OPTIONS PATCH',
        contains: [
            hljs.HASH_COMMENT_MODE,
            {
                className: "subst", // (pathname: path1/path2/dothis) color #ab5656
                begin: /(?<=(?:\/|GET |POST |PUT |DELETE |HEAD |OPTIONS |PATH))[^?\n\r\/]+/,
            }
        ],		}
})

hljs.addPlugin(mergeHTMLPlugin);
hljs.highlightAll();

type NavState = { [key: string]: boolean };
const PAGE_NAV_STATE_KEY = 'pagesNavState';
const sessionState = JSON.parse(sessionStorage.getItem(PAGE_NAV_STATE_KEY)) as NavState

function keepNavState(nav: HTMLElement) {
    const inputs = $$('input[type="checkbox"]', nav);
    if (sessionState) {
        inputs.forEach(input => {
            const key = input.id;
			if (input.dataset['shouldExpand'] === 'true') {
				input.checked = true;
			} else {
				input.checked = sessionState[key];
			}
        });
    }
    window.addEventListener('beforeunload', () => {
		const inputs = $$('input[type="checkbox"]', nav);
		const state: NavState = inputs.reduce((state: NavState, input) => {
            const key = input.id;
            const value = input.checked;
            return { ...state, [key]: value};
        }, {});
        sessionStorage.setItem(PAGE_NAV_STATE_KEY, JSON.stringify(state));
    });
}

const PAGE_NAV_SCROLL_POSITION_KEY = 'pagesNavScrollPosition';
const scrollPosition = sessionStorage.getItem(PAGE_NAV_SCROLL_POSITION_KEY);

function keepNavPosition(nav: HTMLElement) {
    if (scrollPosition) {
        nav.scrollTop = parseInt(scrollPosition);
    }
    window.addEventListener('beforeunload', () => {
        sessionStorage.setItem(PAGE_NAV_SCROLL_POSITION_KEY, nav.scrollTop.toString());
    });
}

function scrollCurrentNaviItemIntoView(nav: HTMLElement, delay: number) {
	setTimeout(() => {
		const currentNavItem = $('.current');
		if (currentNavItem && !isElementInViewport(currentNavItem)) {
			currentNavItem.scrollIntoView({ behavior: 'smooth', block: 'center' });
		}
	}, delay);
}
function isElementInViewport(el: HTMLElement): boolean {
	const rect = el.getBoundingClientRect();
	return (
		rect.top >= 0 &&
		rect.left >= 0 &&
		rect.bottom <= (window.innerHeight || document.documentElement.clientHeight) &&
		rect.right <= (window.innerWidth || document.documentElement.clientWidth)
	);
}


const pagesNav = $('#pages-nav');
keepNavPosition(pagesNav);
keepNavState(pagesNav);
scrollCurrentNaviItemIntoView(pagesNav, 100);
