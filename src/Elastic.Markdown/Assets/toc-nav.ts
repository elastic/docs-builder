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

// Find the current TOC links based on visible headings
function findCurrentTocLinks(elements: TocElements): HTMLAnchorElement[] {
	let currentTocLinks: HTMLAnchorElement[] = [];
	let currentTop: number | null = null;
	
	for (const heading of elements.headings) {
		const rect = heading.getBoundingClientRect();
		const headingText = heading.textContent?.trim() ?? '';
		
		if (rect.top <= HEADING_OFFSET) {
			// If we find a heading at a new height, clear previous links
			if (currentTop !== null && Math.abs(rect.top - currentTop) > 1) {
				currentTocLinks = [];
			}
			currentTop = rect.top;
			
			const foundLink = elements.tocLinks.find(link => 
				link.getAttribute('href') === `#${heading.closest('section')?.id}`
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

// Handle bottom of page scroll behavior
function handleBottomScroll(elements: TocElements) {
	const visibleHeadings = getVisibleHeadings(elements);
	
	visibleHeadings.forEach(heading => {
		console.log(heading.textContent.trim());
	})
	
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
	const currentTocLinks = findCurrentTocLinks(elements);

	if (isAtBottom) {
		handleBottomScroll(elements);
	} else if (currentTocLinks.length > 0) {
		const tocRect = elements.tocNav.getBoundingClientRect();
		
		// Find the topmost and bottommost link positions
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
