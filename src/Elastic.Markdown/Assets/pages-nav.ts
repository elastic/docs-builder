import {$, $$} from "select-dom";

type NavExpandState = { [key: string]: boolean };
const PAGE_NAV_EXPAND_STATE_KEY = 'pagesNavState';
const navState = JSON.parse(sessionStorage.getItem(PAGE_NAV_EXPAND_STATE_KEY) ?? "{}") as NavExpandState

// Initialize the nav state from the session storage
// Return a function to keep the nav state in the session storage that should be called before the page is unloaded
function keepNavState(nav: HTMLElement): () => void {
	const inputs = $$('input[type="checkbox"]', nav);
	if (navState) {
		inputs.forEach(input => {
			const key = input.id;
			if ('shouldExpand' in input.dataset && input.dataset['shouldExpand'] === 'true') {
				input.checked = true;
			} else {
				if (key in navState) {
					input.checked = navState[key];
				}
			}
		});
	}
	
	return () => {
		const inputs = $$('input[type="checkbox"]', nav);
		const state: NavExpandState = inputs.reduce((state: NavExpandState, input) => {
			const key = input.id;
			const value = input.checked;
			return { ...state, [key]: value};
		}, {});
		sessionStorage.setItem(PAGE_NAV_EXPAND_STATE_KEY, JSON.stringify(state));
	}
}

type NavScrollPosition = number;
const PAGE_NAV_SCROLL_POSITION_KEY = 'pagesNavScrollPosition';
const pagesNavScrollPosition: NavScrollPosition = parseInt(
	sessionStorage.getItem(PAGE_NAV_SCROLL_POSITION_KEY) ?? '0'
);


// Initialize the nav scroll position from the session storage
// Return a function to keep the nav scroll position in the session storage that should be called before the page is unloaded
function keepNavPosition(nav: HTMLElement): () => void {
	if (pagesNavScrollPosition) {
		nav.scrollTop = pagesNavScrollPosition;
	}
	return () => {
		sessionStorage.setItem(PAGE_NAV_SCROLL_POSITION_KEY, nav.scrollTop.toString());
	}
}

function scrollCurrentNaviItemIntoView(nav: HTMLElement, delay: number) {
	setTimeout(() => {
		const currentNavItem = $('.current', nav);
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

export function initNav() {
	const pagesNav = $('#pages-nav');
	if (!pagesNav) {
		return;
	}
	const keepNavStateCallback = keepNavState(pagesNav);
	const keepNavPositionCallback = keepNavPosition(pagesNav);
	scrollCurrentNaviItemIntoView(pagesNav, 100);
	window.addEventListener('beforeunload', () => {
		keepNavStateCallback();
		keepNavPositionCallback();
	}, true);
}
