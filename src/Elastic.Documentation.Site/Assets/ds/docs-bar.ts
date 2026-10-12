import { initMenus } from './menu'
import { initPreferences } from './preferences'

// htmx swaps #main-container and the docs bar sits above it, so after a boosted navigation the bar would keep the previous page's
// current tab. The response is a whole page, so the bar is taken from it.
export function syncDocsBar(html: string): boolean {
    const current = document.getElementById('docs-bar')
    if (!current) return false
    const next = new DOMParser()
        .parseFromString(html, 'text/html')
        .getElementById('docs-bar')
    // A partial response, such as a search result, has no bar and leaves ours alone.
    if (!next) return false
    current.replaceWith(next)
    // The htmx:load initializers only scan the swapped container, so the new bar's menus and theme switch are bound here.
    initMenus(next)
    initPreferences(next)
    return true
}

export function initDocsBarSync() {
    // After the swap, so htmx has finished with the DOM it is working on; the response text is still on the request.
    document.addEventListener('htmx:afterSwap', (event) => {
        const xhr = (event as CustomEvent<{ xhr?: XMLHttpRequest }>).detail?.xhr
        if (xhr && typeof xhr.responseText === 'string')
            syncDocsBar(xhr.responseText)
    })
}
