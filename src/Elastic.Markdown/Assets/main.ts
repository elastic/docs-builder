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


function keepNavState(element: HTMLElement) {
    const inputs = $$('input[type="checkbox"]', element);
    const sessionState = JSON.parse(sessionStorage.getItem('pagesNavState')) as NavState
    if (sessionState) {
        inputs.forEach(input => {
            const key = input.id;
            input.checked = input.checked || sessionState[key];
        });
    }
    window.addEventListener('beforeunload', () => {
        const state = inputs.reduce((state: NavState, input) => {
            const key = input.id;
            const value = input.checked;
            return { ...state, [key]: value};
        }, {});
        sessionStorage.setItem('pagesNavState', JSON.stringify(state));
    });
}

function keepNavPosition(element: HTMLElement) {
    const scrollPosition = sessionStorage.getItem('pagesNavScrollPosition');
    if (scrollPosition) {
        element.scrollTop = parseInt(scrollPosition);
    }
    window.addEventListener('beforeunload', () => {
        sessionStorage.setItem('pagesNavScrollPosition', element.scrollTop.toString());
    });
}

const pagesNav = $('#pages-nav');

keepNavState(pagesNav);
keepNavPosition(pagesNav);
