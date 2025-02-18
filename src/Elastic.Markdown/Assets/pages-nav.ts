import {$, $$} from "select-dom";

type NavExpandState = {
	current:string,
	selected: Record<string, boolean>
};
const PAGE_NAV_EXPAND_STATE_KEY = 'pagesNavState';

// Initialize the nav state from the session storage
// Return a function to keep the nav state in the session storage that should be called before the page is unloaded
function keepNavState(nav: HTMLElement): () => void {

	const currentNavigation = nav.dataset.currentNavigation;
	const currentPageId = nav.dataset.currentPageId;

	let navState = JSON.parse(sessionStorage.getItem(PAGE_NAV_EXPAND_STATE_KEY) ?? "{}") as NavExpandState
	if (navState.current !== currentNavigation)
	{
		sessionStorage.removeItem(PAGE_NAV_EXPAND_STATE_KEY);
		navState = { current: currentNavigation } as NavExpandState;
	}
	if (currentPageId)
	{
		const currentPageLink = $('a[id="page-' + currentPageId + '"]', nav);
		currentPageLink.classList.add('current');
		currentPageLink.classList.add('pointer-events-none');
		currentPageLink.classList.add('text-blue-elastic!');
		currentPageLink.classList.add('font-semibold');

		const parentIds = nav.dataset.currentPageParentIds?.split(',') ?? [];
		for (const parentId of parentIds)
		{
			const input = $('input[type="checkbox"][id=\"'+parentId+'\"]', nav) as HTMLInputElement;
			if (input) {
				input.checked = true;
				const link = input.nextElementSibling as HTMLAnchorElement;
				link.classList.add('font-semibold');
			}
		}
	}

	// expand items previously selected
	for (const groupId in navState.selected)
	{
		const input = $('input[type="checkbox"][id=\"'+groupId+'\"]', nav) as HTMLInputElement;
		input.checked = navState.selected[groupId];
	}

	return () => {
		// store all expanded groups
		const inputs = $$('input[type="checkbox"]:checked', nav);
		const selectedMap: Record<string, boolean> = inputs
			.filter(input => input.checked)
			.reduce((state: Record<string, boolean>, input) => {
			const key = input.id;
			const value = input.checked;
			return { ...state, [key]: value};
		}, {});
		const state = { current: currentNavigation, selected: selectedMap };
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

initNav();
