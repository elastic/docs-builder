// Marks the current page in the sidebar, opens its ancestor folders and remembers which folders the reader opened.
// The tree is rendered once per section and shared by every page in it, so "current" can only be decided in the browser.
// Hooks are data attributes and ARIA, never styling classes.
const storagePrefix = 'nav-open:'

function normalize(path: string): string {
    return path.length > 1 ? path.replace(/\/+$/, '') : path
}

function currentPath(): string {
    const fromMeta = document
        .querySelector('meta[name="docs:nav-active"]')
        ?.getAttribute('content')
    return normalize(fromMeta || window.location.pathname)
}

function readOpen(id: string): boolean | null {
    try {
        const stored = window.sessionStorage.getItem(storagePrefix + id)
        return stored === null ? null : stored === '1'
    } catch {
        return null
    }
}

function writeOpen(id: string, open: boolean) {
    try {
        window.sessionStorage.setItem(storagePrefix + id, open ? '1' : '0')
    } catch {
        // storage unavailable: the folder still opens for this page view
    }
}

function toggleOf(group: Element): HTMLInputElement | null {
    return group.querySelector<HTMLInputElement>(':scope > [data-nav-toggle]')
}

function openAncestors(link: Element, tree: Element) {
    for (
        let node = link.parentElement;
        node && node !== tree;
        node = node.parentElement
    ) {
        if (node.classList.contains('nav-tree__group')) {
            const toggle = toggleOf(node)
            if (toggle) toggle.checked = true
        }
    }
}

export function initNav(scope: ParentNode = document) {
    scope
        .querySelectorAll<HTMLElement>('[data-nav-side]:not([data-nav-bound])')
        .forEach((side) => {
            side.dataset.navBound = ''

            // Restore what the reader opened or closed, then make sure the current page is visible.
            side.querySelectorAll<HTMLInputElement>(
                '[data-nav-toggle]'
            ).forEach((toggle) => {
                const stored = readOpen(toggle.id)
                if (stored !== null) toggle.checked = stored
                toggle.addEventListener('change', () =>
                    writeOpen(toggle.id, toggle.checked)
                )
            })

            markCurrent(side)
        })
}

export function markCurrent(side: ParentNode = document) {
    const path = currentPath()
    const tree = side.querySelector('.nav-tree')
    if (!tree) return

    const link = Array.from(
        tree.querySelectorAll<HTMLAnchorElement>('a.nav-tree__link')
    ).find(
        (a) =>
            normalize(new URL(a.href, window.location.href).pathname) === path
    )
    // A page shown under another address, such as a preview, keeps the mark the server put on its own link.
    if (!link) return

    tree.querySelectorAll('[aria-current="page"]').forEach((current) =>
        current.removeAttribute('aria-current')
    )
    link.setAttribute('aria-current', 'page')
    openAncestors(link, tree)

    const scroller = link.closest<HTMLElement>('.nav-side__scroll')
    if (scroller) {
        const offset =
            link.offsetTop - scroller.clientHeight / 2 + link.clientHeight / 2
        scroller.scrollTop = Math.max(0, offset)
    }
}
