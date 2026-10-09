// Entry point of the design-system bundle, loaded instead of main.ts when NAVIGATION_PREVIEW is on.
// Everything here is progressive enhancement: pages are complete HTML without it. Enhancements bind to data-* attributes,
// never to styling classes, so a theme can rewrite the CSS without touching behaviour.
import { initCopyButtons } from './ds/copy'
import { initFeedback } from './ds/feedback'
import { bindOutsideClick, initMenus } from './ds/menu'
import { initNav } from './ds/nav'
import { initPopovers } from './ds/popover'
import { bindSearchShortcut, initSearch } from './ds/search'
import { initTabSync } from './ds/tabs'
import { initThemeToggles } from './ds/theme'
import { initToc } from './ds/toc'
import { initVectorSizing } from './ds/vector-sizing'

function init(scope: ParentNode = document) {
    initThemeToggles(scope)
    initTabSync(scope)
    initCopyButtons(scope)
    initMenus(scope)
    initPopovers(scope)
    initFeedback(scope)
    initSearch(scope)
    initNav(scope)
    initToc(scope)
    initVectorSizing(scope)
}

bindOutsideClick()
bindSearchShortcut()
document.addEventListener('DOMContentLoaded', () => init())
// htmx swaps #main-container; re-bind inside the swapped content
document.addEventListener('htmx:load', (event) => {
    const target = event.target
    if (target instanceof Element) init(target)
})
