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

function keepNavState(element: HTMLElement) {
    const inputs = $$('input[type="checkbox"]', element);
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
		const inputs = $$('input[type="checkbox"]', element);
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

function keepNavPosition(element: HTMLElement) {
    if (scrollPosition) {
        element.scrollTop = parseInt(scrollPosition);
    }
    window.addEventListener('beforeunload', () => {
        sessionStorage.setItem(PAGE_NAV_SCROLL_POSITION_KEY, element.scrollTop.toString());
    });
}

const pagesNav = $('#pages-nav');
keepNavPosition(pagesNav);
keepNavState(pagesNav);
