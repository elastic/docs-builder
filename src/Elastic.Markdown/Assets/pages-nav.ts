import {$} from "select-dom";

function expandAllParents(navItem: HTMLElement) {
	let parent = navItem.closest('li');
	while (parent) {
		const input = parent.querySelector('input');
		if (input) {
			(input as HTMLInputElement).checked = true;
		}
		parent = parent.parentElement?.closest('li');
	}
}

function scrollCurrentNaviItemIntoView(nav: HTMLElement, delay: number) {
	const currentNavItem = $('.up-current', nav);
	expandAllParents(currentNavItem);
	setTimeout(() => {
		
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
	scrollCurrentNaviItemIntoView(pagesNav, 100);
}
