// Entry point of the design-system bundle, loaded instead of main.ts when NAVIGATION_PREVIEW is on.
// Everything here is progressive enhancement: pages are complete HTML without it. Enhancements bind to data-* attributes,
// never to styling classes, so a theme can rewrite the CSS without touching behaviour.
import { initTabSync } from './ds/tabs'
import { initThemeToggles } from './ds/theme'

function init(scope: ParentNode = document) {
    initThemeToggles(scope)
    initTabSync(scope)
}

document.addEventListener('DOMContentLoaded', () => init())
// htmx swaps #main-container; re-bind inside the swapped content
document.addEventListener('htmx:load', (event) => {
    const target = event.target
    if (target instanceof Element) init(target)
})
