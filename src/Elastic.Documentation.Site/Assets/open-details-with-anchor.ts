// Opens details elements (dropdowns) when navigating to an anchor link within them
// This enables deeplinking to collapsed dropdown content
export function openDetailsWithAnchor() {
    if (window.location.hash) {
        const target = document.querySelector(window.location.hash)
        if (target) {
            const closestDetails = target.closest('details')
            if (closestDetails && !closestDetails.open) {
                // Only open if it's not already open by default
                if (!closestDetails.dataset.openDefault) {
                    closestDetails.open = true
                }
            }

            // Chrome automatically ensures parent content is visible, scroll immediately
            target.scrollIntoView({
                behavior: 'instant',
                block: 'start',
            })
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

    // Remove data-open-default on first click to enable URL updates
    document.addEventListener(
        'click',
        (event) => {
            const target = event.target as HTMLElement
            const dropdown = target.closest(
                'details.dropdown'
            ) as HTMLDetailsElement
            if (dropdown && dropdown.dataset.openDefault === 'true') {
                delete dropdown.dataset.openDefault
            }
        },
        true
    )

    // Handle manual dropdown toggling to update URL
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

                // Only update URL if NOT open by default (until first interaction)
                if (!details.dataset.openDefault) {
                    updateUrlForDropdown(details, isOpening)
                }
            }
        },
        true
    )
}
