// Entry point of the design-system bundle, loaded instead of main.ts when NAVIGATION_PREVIEW is on.
// Everything here is progressive enhancement: pages are complete HTML without it. Enhancements bind to data-* attributes,
// never to styling classes, so a theme can rewrite the CSS without touching behaviour.
import { initAskAi } from './ds/ask-ai'
import { initCarousels } from './ds/carousel'
import { initCopyButtons, initCopyText } from './ds/copy'
import { initDiagnostics } from './ds/diagnostics'
import { initFeedback } from './ds/feedback'
import { initLightboxes } from './ds/lightbox'
import { initListings } from './ds/listing'
import { bindOutsideClick, initMenus } from './ds/menu'
import { initMotionToggles } from './ds/motion'
import { initNav } from './ds/nav'
import { initNavFilter } from './ds/nav-filter'
import { initFlyoutResize, initFlyouts, initToasts } from './ds/overlays'
import { initPopovers } from './ds/popover'
import './ds/runtime'
import { bindSearchShortcut, initSearch } from './ds/search'
import { initSearchPage } from './ds/search-page'
import { initTabSync } from './ds/tabs'
import { initThemeToggles } from './ds/theme'
import { initToc } from './ds/toc'
import { initVectorSizing } from './ds/vector-sizing'

// The filterable table is already framework-free, so the design system loads it as it is.
async function loadFilterableTables(scope: ParentNode) {
    if (customElements.get('filterable-table')) return
    if (scope.querySelector('filterable-table'))
        await import('./web-components/FilterableTable')
}

// So is the Storybook embed, which loads a story's own bundle into the page.
async function loadStorybookStories(scope: ParentNode) {
    if (customElements.get('storybook-story')) return
    if (scope.querySelector('storybook-story'))
        await import('./web-components/StorybookStory/StorybookStoryComponent')
}

function init(scope: ParentNode = document) {
    initThemeToggles(scope)
    initMotionToggles(scope)
    initTabSync(scope)
    initCopyButtons(scope)
    initCopyText(scope)
    initMenus(scope)
    initPopovers(scope)
    initFeedback(scope)
    initSearch(scope)
    initSearchPage(scope)
    initNav(scope)
    initNavFilter(scope)
    initToc(scope)
    initVectorSizing(scope)
    initDiagnostics(scope)
    initListings(scope)
    initToasts(scope)
    initFlyouts(scope)
    initFlyoutResize(scope)
    initAskAi(scope)
    initLightboxes(scope)
    initCarousels(scope)
    void loadFilterableTables(scope)
    void loadStorybookStories(scope)
}

bindOutsideClick()
bindSearchShortcut()
document.addEventListener('DOMContentLoaded', () => init())
// htmx swaps #main-container; re-bind inside the swapped content
document.addEventListener('htmx:load', (event) => {
    const target = event.target
    if (target instanceof Element) init(target)
})
