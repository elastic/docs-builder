// Opens details elements (dropdowns) when navigating to an anchor link within them
// This enables deeplinking to collapsed dropdown content
export function openDetailsWithAnchor() {
    if (window.location.hash) {
        const target = document.querySelector(window.location.hash)
        if (target) {
            const closestDetails = target.closest('details')
            if (closestDetails) {
                closestDetails.open = true
                // Small delay to ensure the details element is open before scrolling
                setTimeout(() => {
                    target.scrollIntoView({
                        behavior: 'smooth',
                        block: 'start',
                    })
                }, 50)
            }
        }
    }
}

// Initialize the anchor handling functionality
export function initOpenDetailsWithAnchor() {
    // Handle initial page load
    openDetailsWithAnchor()
    
    // Handle hash changes within the same page (e.g., clicking anchor links)
    window.addEventListener('hashchange', openDetailsWithAnchor)
}
