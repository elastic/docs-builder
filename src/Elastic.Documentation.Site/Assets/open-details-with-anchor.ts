import { UAParser } from 'ua-parser-js'

const { getBrowser } = new UAParser()

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
            // Other browsers need manual scroll handling
            const browser = getBrowser()
            if (browser.name !== 'Chrome') {
                target.scrollIntoView({
                    behavior: 'instant',
                    block: 'start',
                })
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

    // Handle dropdown URL updates
    document.addEventListener(
        'click',
        (event) => {
            const target = event.target as HTMLElement
            const dropdown = target.closest(
                'details.dropdown'
            ) as HTMLDetailsElement
            if (dropdown) {
                const initialState = dropdown.open

                // Check state after toggle completes
                setTimeout(() => {
                    const finalState = dropdown.open
                    const stateChanged = initialState !== finalState

                    // If dropdown opened and doesn't have open-default flag, push URL
                    if (
                        stateChanged &&
                        finalState &&
                        !dropdown.dataset.openDefault
                    ) {
                        window.history.pushState(null, '', `#${dropdown.id}`)
                    }

                    // Remove open-default flag after first interaction
                    if (dropdown.dataset.openDefault === 'true') {
                        delete dropdown.dataset.openDefault
                    }
                }, 10)
            }
        },
        true
    )
}
