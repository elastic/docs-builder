// Boosted API navigation can leave chrome outside the swapped region stale. The response carries the chrome
// for the new page, so the parts that change per page are copied over.

// A swap of #api-content-grid leaves the version picker linking to the first page loaded.
export function syncApiVersionSwitcher(html: string): boolean {
    const current = document.getElementById('api-version-switcher')
    if (!current) return false
    const next = new DOMParser()
        .parseFromString(html, 'text/html')
        .getElementById('api-version-switcher')
    if (!next) return false

    const currentLinks = current.querySelectorAll('a[href]')
    const nextLinks = next.querySelectorAll('a[href]')
    if (currentLinks.length !== nextLinks.length) return false

    currentLinks.forEach((link, i) =>
        link.setAttribute('href', nextLinks[i].getAttribute('href')!)
    )
    return true
}

// The isolated header is hx-preserve'd and shows the API's title, so a move to another API leaves the old title.
export function syncIsolatedHeaderTitle(html: string): boolean {
    const current = document.querySelector(
        '#isolated-header elastic-docs-header'
    )
    if (!current) return false
    const title = new DOMParser()
        .parseFromString(html, 'text/html')
        .querySelector('#isolated-header elastic-docs-header')
        ?.getAttribute('title')
    if (title == null || title === current.getAttribute('title')) return false
    current.setAttribute('title', title)
    return true
}

export function initApiChromeSync() {
    document.addEventListener('htmx:afterSwap', (event) => {
        const xhr = (event as CustomEvent<{ xhr?: XMLHttpRequest }>).detail?.xhr
        if (!xhr || typeof xhr.responseText !== 'string') return
        syncApiVersionSwitcher(xhr.responseText)
        syncIsolatedHeaderTitle(xhr.responseText)
    })
}
