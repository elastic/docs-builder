import {initNav} from "./pages-nav";
import {initHighlight} from "./hljs";
import {$, $$} from "select-dom";

initNav();
initHighlight();

// Get all headings in the markdown content
const headings = $$('h1, h2, h3, h4, h5, h6');
const tocLinks = $$('#toc-nav a');

// Track current section while scrolling
// Create intersection observer to track visible headings
const headingObserver = new IntersectionObserver((entries) => {
  // Find the first visible heading or last heading above viewport
  const currentEntry = entries.find(entry => entry.isIntersecting) || 
    entries.reduce((prev, curr) => {
      if (!curr.isIntersecting && curr.boundingClientRect.top < 0) {
        return !prev || curr.boundingClientRect.top > prev.boundingClientRect.top ? curr : prev;
      }
	  //
      return prev;
    }, null as IntersectionObserverEntry | null);

  // Set current class on the TOC link for this heading
  if (currentEntry) {
    // Only remove current class from other links if we found a new heading
    const heading = currentEntry.target;
    const id = heading.parentElement?.id;
    if (id) {
      const matchingLink = $(`#toc-nav a[href="#${id}"]`);
      if (matchingLink) {
        // Remove current class from all other links first
        tocLinks.forEach(link => {
          if (link !== matchingLink) {
            link.classList.remove('current');
          }
        });
        matchingLink.classList.add('current');
      }
    }
  }
}, {
  rootMargin: "-10% 0px -70% 0px"
});

headings.forEach(heading => headingObserver.observe(heading));

$$('#toc-nav li>a').forEach(link => {
    link.addEventListener('click', (e) => {
        e.preventDefault();
        const href = link.getAttribute('href');
        if (href) {
            const target = document.querySelector(href);
            if (target) {
                target.scrollIntoView({ behavior: 'smooth' });  
            }
        }
    })
});

// Observe all headings

const scrollTopButton = $('#scroll-to-top-button');
if (scrollTopButton) {
  // Create a dummy element at the top of the page to observe
  const topSentinel = document.createElement('div');
  topSentinel.style.position = 'absolute';
  topSentinel.style.top = '300px'; // Show button after this threshold
  topSentinel.style.height = '1px';
  document.body.prepend(topSentinel);

  // Create intersection observer
  const observer = new IntersectionObserver(
    ([entry]) => {
      // Show button when sentinel is out of view (scrolled past)
      if (!entry.isIntersecting) {
        scrollTopButton.classList.remove('hidden');
      } else {
        scrollTopButton.classList.add('hidden');
      }
    },
    { threshold: 1.0 }
  );

  // Start observing the sentinel element
  observer.observe(topSentinel);

  // Scroll to top when clicked
  scrollTopButton.addEventListener('click', () => {
    window.scrollTo({ top: 0, behavior: 'smooth' });
  });
}
