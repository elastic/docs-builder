import { $$, $ } from 'select-dom';

interface TocElements {
	headings: Element[];
	tocLinks: HTMLAnchorElement[];
	tocContainer: HTMLUListElement | null;
	progressIndicator: HTMLDivElement;
}

// 34 is the height of the header + some padding
// 4 is the base spacing unit
const HEADING_OFFSET = 34 * 4; 

function initializeTocElements(): TocElements {
	const headings = $$('h2, h3');
	const tocLinks = $$('#toc-nav li>a') as HTMLAnchorElement[];
	const tocContainer = $('#toc-nav ul') as HTMLUListElement;
	const progressIndicator = $('.toc-progress-indicator', tocContainer) as HTMLDivElement;
	return { headings, tocLinks, tocContainer,progressIndicator };
}

// Find the current TOC links based on visible headings
// It can return multiple links because headings in a tab can have the same position
function findCurrentTocLinks(elements: TocElements): HTMLAnchorElement[] {
	let currentTocLinks: HTMLAnchorElement[] = [];
	let currentTop: number | null = null;
	for (const heading of elements.headings) {
		const rect = heading.getBoundingClientRect();
		if (rect.top <= HEADING_OFFSET) {
			if (currentTop !== null && Math.abs(rect.top - currentTop) > 1) {
				currentTocLinks = [];
			}
			currentTop = rect.top;
			const foundLink = elements.tocLinks.find(link => 
				link.getAttribute('href') === `#${heading.closest('.heading-wrapper')?.id}`
			);
			if (foundLink) {
				currentTocLinks.push(foundLink);
			}
		}
	}
	return currentTocLinks;
}

// Get visible headings in viewport
function getVisibleHeadings(elements: TocElements) {
	return elements.headings.filter(heading => {
		const rect = heading.getBoundingClientRect();
		return rect.top - HEADING_OFFSET + 64 >= 0 && rect.top <= window.innerHeight;
	});
}

// If the user has scrolled to the bottom of the page,
// and there are still multiple headings visible, we need to
// handle the progress indicator differently.
// In this case it sets the indicator for all visible headings.
function handleBottomScroll(elements: TocElements) {
	const visibleHeadings = getVisibleHeadings(elements);
	if (visibleHeadings.length === 0) return;
	const firstHeading = visibleHeadings[0];
	const lastHeading = visibleHeadings[visibleHeadings.length - 1];
	const firstLink = elements.tocLinks.find(link => 
		link.getAttribute('href') === `#${firstHeading.parentElement?.id}`
	)?.closest('li');
	const lastLink = elements.tocLinks.find(link => 
		link.getAttribute('href') === `#${lastHeading.parentElement?.id}`
	)?.closest('li');
	if (firstLink && lastLink && elements.tocContainer) {
		const tocRect = elements.tocContainer.getBoundingClientRect();
		const firstRect = firstLink.getBoundingClientRect();
		const lastRect = lastLink.getBoundingClientRect();
		updateProgressIndicatorPosition(
			elements.progressIndicator,
			firstRect.top - tocRect.top,
			(lastRect.top + lastRect.height) - firstRect.top
		);
	}
}

function updateProgressIndicatorPosition(
	indicator: HTMLDivElement,
	top: number,
	height: number
) {
	indicator.style.top = `${top}px`;
	indicator.style.height = `${height}px`;
}

function updateIndicator(elements: TocElements) {
	if (!elements.tocContainer) return;

	const isAtBottom = window.innerHeight + window.scrollY >= document.documentElement.scrollHeight - 10;
	const currentTocLinks = findCurrentTocLinks(elements);

	if (isAtBottom) {
		handleBottomScroll(elements);
	} else if (currentTocLinks.length > 0) {
		const tocRect = elements.tocContainer.getBoundingClientRect();
		const linkElements = currentTocLinks
			.map(link => link.closest('li'))
			.filter((li): li is HTMLLIElement => li !== null);
		if (linkElements.length === 0) return;
		const firstLinkRect = linkElements[0].getBoundingClientRect();
		const lastLinkRect = linkElements[linkElements.length - 1].getBoundingClientRect();
		updateProgressIndicatorPosition(
			elements.progressIndicator,
			firstLinkRect.top - tocRect.top,
			(lastLinkRect.top + lastLinkRect.height) - firstLinkRect.top
		);
	}
}

function setupSmoothScrolling(elements: TocElements): () => void {
	const clickHandler = (e: Event) => {
		const link = e.currentTarget as HTMLAnchorElement;
		const href = link.getAttribute('href');
		if (href?.charAt(0) === '#') {
			e.preventDefault();
			const target = document.getElementById(href.slice(1));
			if (target) {
				target.scrollIntoView({ behavior: 'smooth' });
				history.pushState(null, '', href);
			}
		}
	};

	elements.tocLinks.forEach(link => {
		link.addEventListener('click', clickHandler);
	});

	return () => {
		elements.tocLinks.forEach(link => {
			link.removeEventListener('click', clickHandler);
		});
	};
}

export function initTocNav(): () => void {
	const elements = initializeTocElements();
	if (elements.progressIndicator != null) {
		elements.progressIndicator.style.height = '0';
		elements.progressIndicator.style.top = '0';
	}
	const update = () => updateIndicator(elements);
	update();
	window.addEventListener('scroll', update);
	window.addEventListener('resize', update);
	const removeSmoothScrolling = setupSmoothScrolling(elements);
	return () => {
		window.removeEventListener('scroll', update);
		window.removeEventListener('resize', update);
		removeSmoothScrolling();
	};
}
