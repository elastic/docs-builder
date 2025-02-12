import { $$, $ } from 'select-dom';

interface TocElements {
	headings: Element[];
	tocLinks: HTMLAnchorElement[];
	tocNav: HTMLUListElement | null;
	markdownContent: Element | null;
	progressIndicator: HTMLDivElement;
}

const HEADING_OFFSET = 28 * 4 + 6 * 4;

// Initialize and get all required DOM elements
function initializeTocElements(): TocElements {
	const headings = $$('h2, h3');
	const tocLinks = $$('#toc-nav li>a') as HTMLAnchorElement[];
	const tocNav = $('#toc-nav ul') as HTMLUListElement;
	const markdownContent = $('.markdown-content') || null;
	
	// Create progress indicator
	const progressIndicator = document.createElement('div');
	progressIndicator.className = 'toc-progress-indicator';
	tocNav?.appendChild(progressIndicator);

	return { headings, tocLinks, tocNav, markdownContent, progressIndicator };
}

// Add required styles for the progress indicator
function addProgressIndicatorStyles() {
	const style = document.createElement('style');
	style.textContent = `
		#toc-nav ul {
			position: relative;
		}
		.toc-progress-indicator {
			position: absolute;
			left: calc(var(--spacing) * 2);
			width: 1px;
			background: var(--color-blue-elastic);
			transition: top 250ms ease-out, height 250ms ease-out;
		}
	`;
	document.head.appendChild(style);
}

// Find the current TOC link based on visible headings
function findCurrentTocLink(elements: TocElements): HTMLAnchorElement | undefined {
	let currentTocLink: HTMLAnchorElement | undefined;
	
	for (const heading of elements.headings) {
		const rect = heading.getBoundingClientRect();
		if (rect.top <= HEADING_OFFSET) {
			const foundLink = elements.tocLinks.find(link => 
				link.getAttribute('href') === `#${heading.closest('section')?.id}`
			);
			if (foundLink) {
				currentTocLink = foundLink;
			}
		}
	}
	
	return currentTocLink;
}

// Get visible headings in viewport
function getVisibleHeadings(elements: TocElements) {
	return elements.headings.filter(heading => {
		const rect = heading.getBoundingClientRect();
		return rect.top - HEADING_OFFSET >= 0 && rect.bottom <= window.innerHeight;
	});
}

// Handle bottom of page scroll behavior
function handleBottomScroll(elements: TocElements) {
	const visibleHeadings = getVisibleHeadings(elements);
	if (visibleHeadings.length === 0) return;

	const firstHeading = visibleHeadings[0];
	const lastHeading = visibleHeadings[visibleHeadings.length - 1];
	
	const firstLink = elements.tocLinks.find(link => 
		link.getAttribute('href') === `#${firstHeading.closest('section')?.id}`
	)?.closest('li');
	
	const lastLink = elements.tocLinks.find(link => 
		link.getAttribute('href') === `#${lastHeading.closest('section')?.id}`
	)?.closest('li');

	if (firstLink && lastLink && elements.tocNav) {
		const tocRect = elements.tocNav.getBoundingClientRect();
		const firstRect = firstLink.getBoundingClientRect();
		const lastRect = lastLink.getBoundingClientRect();

		updateProgressIndicatorPosition(
			elements.progressIndicator,
			firstRect.top - tocRect.top,
			(lastRect.top + lastRect.height) - firstRect.top
		);
	}
}

// Update progress indicator position and height
function updateProgressIndicatorPosition(
	indicator: HTMLDivElement,
	top: number,
	height: number
) {
	indicator.style.top = `${top}px`;
	indicator.style.height = `${height}px`;
}

// Main update function for the indicator
function updateIndicator(elements: TocElements) {
	if (!elements.markdownContent || !elements.tocNav) return;

	const isAtBottom = window.innerHeight + window.scrollY >= document.documentElement.scrollHeight - 10;
	const currentTocLink = findCurrentTocLink(elements);

	if (isAtBottom) {
		handleBottomScroll(elements);
	} else if (currentTocLink) {
		const link = currentTocLink.closest('li');
		if (!link) return;

		const tocRect = elements.tocNav.getBoundingClientRect();
		const linkRect = link.getBoundingClientRect();
		
		updateProgressIndicatorPosition(
			elements.progressIndicator,
			linkRect.top - tocRect.top,
			linkRect.height
		);
	}
}

// Handle smooth scrolling for TOC links
function setupTocLinkHandlers(elements: TocElements) {
	elements.tocLinks.forEach(link => {
		link.addEventListener('click', (e) => {
			const href = link.getAttribute('href');
			if (href?.charAt(0) === '#') {
				e.preventDefault();
				const target = $(href.replace('.', '\\.'));
				if (target) {
					target.scrollIntoView({ behavior: 'smooth' });
					history.pushState(null, '', href);
				}
			}
		});
	});
}

export function initTocNav() {
	const elements = initializeTocElements();
	addProgressIndicatorStyles();
	
	// Initialize indicator position
	elements.progressIndicator.style.height = '0';
	elements.progressIndicator.style.top = '0';
	
	// Set up event listeners
	const boundUpdateIndicator = () => updateIndicator(elements);
	window.addEventListener('scroll', boundUpdateIndicator);
	window.addEventListener('resize', boundUpdateIndicator);
	
	setupTocLinkHandlers(elements);
	boundUpdateIndicator();
}
