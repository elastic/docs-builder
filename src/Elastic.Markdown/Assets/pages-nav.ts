import {$, $$} from "select-dom";

function expandAllParents(navItem: HTMLElement) {
	let parent = navItem?.closest('li');
	while (parent) {
		const input = parent.querySelector('input');
		if (input) {
			(input as HTMLInputElement).checked = true;
		}
		parent = parent.parentElement?.closest('li');
	}
}

function scrollCurrentNaviItemIntoView(nav: HTMLElement, delay: number) {
	const currentNavItem = $('.current', nav);
	expandAllParents(currentNavItem);
	setTimeout(() => {
		if (currentNavItem && !isElementInViewport(nav, currentNavItem)) {
			const navRect = nav.getBoundingClientRect();
			const currentNavItemRect = currentNavItem.getBoundingClientRect();
			const offset = currentNavItemRect.top - navRect.top - navRect.height / 3 + currentNavItemRect.height / 2;
			nav.scrollTop = nav.scrollTop + offset;
		}
	}, delay);
}

function isElementInViewport(parent: HTMLElement, child: HTMLElement, ): boolean {
	const childRect = child.getBoundingClientRect();
	const parentRect = parent.getBoundingClientRect();
	return (
		childRect.top >= parentRect.top &&
		childRect.left >= parentRect.left &&
		childRect.bottom <= parentRect.bottom &&
		childRect.right <= parentRect.right
	);
}

export function initNav() {
	const pagesNav = $('#pages-nav');
	if (!pagesNav) {
		return;
	}
	const navItems = $$('a[href="' + window.location.pathname + '"], a[href="' + window.location.pathname + '/"]', pagesNav);
	navItems.forEach(el => {
		el.classList.add('current');
	});
	scrollCurrentNaviItemIntoView(pagesNav, 100);
}
