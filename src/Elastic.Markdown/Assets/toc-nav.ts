import { $$, $ } from 'select-dom';

const headings = $$('h2, h3');
const tocLinks = $$('#toc-nav li>a');

// Track current section while scrolling
	
// Get the TOC nav element
const tocNav = $('#toc-nav ul');
const markdownContent = $('.markdown-content');

// Create and append progress indicator
const progressIndicator = document.createElement('div');
progressIndicator.className = 'toc-progress-indicator';
tocNav?.appendChild(progressIndicator);

// Style the progress indicator
const style = document.createElement('style');
style.textContent = `
	#toc-nav ul {
		position: relative;
		overflow-y: hidden;
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

progressIndicator.style.height = '0';
progressIndicator.style.top = '0'

// Function to update indicator position and height
const updateIndicator = () => {
    if (!markdownContent || !tocNav) return;



    let currentTocLink: HTMLAnchorElement | undefined = undefined;
    let isAtBottom = window.innerHeight + window.scrollY >= document.documentElement.scrollHeight;
    
    for (let i = 0; i < headings.length; i++) {
        const heading = headings[i];
        const rect = heading.getBoundingClientRect();
        
        // Check if heading is in viewport (with some offset)
        if (rect.top <= 28 * 4 + 6 * 4) {
            const foundLink = tocLinks.find(link => link.getAttribute('href') === `#${heading.closest('section')?.id}`);
			if (foundLink) {
				currentTocLink = foundLink;
			}
        }
    }

    // If we're at the bottom, highlight all visible headings in the viewport
    if (isAtBottom) {
        const visibleHeadings = headings.filter(heading => {
            const rect = heading.getBoundingClientRect();
            return rect.top >= 0 && rect.bottom <= window.innerHeight;
        });
		visibleHeadings.forEach(heading => {
			console.log(heading.textContent?.trim());
		});

        if (visibleHeadings.length > 0) {
            const firstVisibleHeading = visibleHeadings[0];
            const lastVisibleHeading = visibleHeadings[visibleHeadings.length - 1];
            
            const firstLink = tocLinks.find(link => 
                link.getAttribute('href') === `#${firstVisibleHeading.closest('section')?.id}`
            )?.closest('li');
            const lastLink = tocLinks.find(link => 
                link.getAttribute('href') === `#${lastVisibleHeading.closest('section')?.id}`
            )?.closest('li');

			console.log

            if (firstLink && lastLink) {
                const firstRect = firstLink.getBoundingClientRect();
                const lastRect = lastLink.getBoundingClientRect();
                const tocRect = tocNav.getBoundingClientRect();

                progressIndicator.style.top = `${firstRect.top - tocRect.top}px`;
                progressIndicator.style.height = `${(lastRect.top + lastRect.height) - firstRect.top}px`;
            }
        }
    } else if (currentTocLink) {
		const link = currentTocLink.closest('li');
		let linkRect = link?.getBoundingClientRect();
		if (isAtBottom) {
			linkRect = link?.nextElementSibling?.getBoundingClientRect();
		} else {
			linkRect = link?.getBoundingClientRect();
		}
        const tocRect = tocNav.getBoundingClientRect();
    
        if (linkRect) {
			progressIndicator.style.top = `${linkRect.top - tocRect.top}px`;
			progressIndicator.style.height = `${linkRect.height}px`;
        }
    }
};


export function initTocNav() {
	updateIndicator();
	window.addEventListener('scroll', updateIndicator);
	$$('#toc-nav li>a').forEach(link => {
		link.addEventListener('click', (e) => {
			const href = link.getAttribute('href');
			if (href?.charAt(0) === '#') {
				e.preventDefault();
				const target = $(href.replace('.', '\\.'));
				if (target) {
					target.scrollIntoView({ behavior: 'smooth' });
					history.pushState(null, '', href); // Push new state with hash to history
				}
			}
		})
	});
}
