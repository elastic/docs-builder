// Boosted API navigation swaps only #api-content-grid, so the version picker outside it would keep
// linking to the first page loaded. The response carries the picker for the new page, so its hrefs are copied over.
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

export function initApiVersionSwitcherSync() {
    document.addEventListener('htmx:afterSwap', (event) => {
        const xhr = (event as CustomEvent<{ xhr?: XMLHttpRequest }>).detail?.xhr
        if (xhr && typeof xhr.responseText === 'string')
            syncApiVersionSwitcher(xhr.responseText)
    })
}
