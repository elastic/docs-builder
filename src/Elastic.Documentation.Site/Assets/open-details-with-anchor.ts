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

// Updates the URL when a dropdown is manually opened/closed
function updateUrlForDropdown(details: HTMLDetailsElement, isOpening: boolean) {
    const dropdownId = details.id
    if (!dropdownId) return

    if (isOpening) {
        // Update URL to show the dropdown anchor (like clicking a heading link)
        window.history.pushState(null, '', `#${dropdownId}`)
    }
    // Note: We don't remove the hash when closing, just like headings don't
    // This keeps the URL consistent with how headings behave
}

// Initialize the anchor handling functionality
export function initOpenDetailsWithAnchor() {
    // Handle initial page load
    openDetailsWithAnchor()

    // Handle hash changes within the same page (e.g., clicking anchor links)
    window.addEventListener('hashchange', openDetailsWithAnchor)

    // Handle manual dropdown toggling to update URL
    // Use event delegation to catch all toggle events
    document.addEventListener(
        'toggle',
        (event) => {
            const target = event.target as HTMLElement

            // Check if the target is a details element with dropdown class
            if (
                target.tagName === 'DETAILS' &&
                target.classList.contains('dropdown')
            ) {
                const details = target as HTMLDetailsElement
                const isOpening = details.open

                // Use setTimeout to ensure the toggle state has been processed
                setTimeout(() => {
                    updateUrlForDropdown(details, isOpening)
                }, 0)
            }
        },
        true
    ) // Use capture phase to ensure we catch the event
}
