// Filters the sidebar tree as you type, for the "Jump to API" field. The tree is complete without it, so the field is rendered
// hidden and revealed here. Hooks are data attributes and the nav-tree structure, never styling-only classes.
export function matchesFilter(text: string, query: string): boolean {
    return text.toLowerCase().includes(query.trim().toLowerCase())
}

function applyFilter(tree: Element, query: string): boolean {
    let any = false
    tree.querySelectorAll(':scope > li').forEach((item) => {
        if (!(item instanceof HTMLElement)) return
        if (query === '') {
            item.hidden = false
            item.querySelectorAll(':scope > ul').forEach((children) =>
                applyFilter(children, query)
            )
            return
        }
        const link = item.querySelector(':scope > a, :scope > .nav-tree__row')
        const selfMatches =
            link !== null && matchesFilter(link.textContent ?? '', query)
        const children = item.querySelector(':scope > ul')
        const childrenMatch = children ? applyFilter(children, query) : false
        item.hidden = !(selfMatches || childrenMatch)
        if (childrenMatch) {
            const toggle = item.querySelector<HTMLInputElement>(
                ':scope > [data-nav-toggle]'
            )
            if (toggle) toggle.checked = true
        }
        any ||= selfMatches || childrenMatch
    })
    return query === '' ? true : any
}

export function initNavFilter(scope: ParentNode = document) {
    scope
        .querySelectorAll<HTMLInputElement>(
            '[data-nav-filter]:not([data-nav-filter-bound])'
        )
        .forEach((input) => {
            input.dataset.navFilterBound = ''
            const nav = input.closest('[data-nav-filter-scope]')
            const tree = nav?.querySelector('.nav-tree')
            if (!tree) return
            input.hidden = false
            input.addEventListener('input', () =>
                applyFilter(tree, input.value.trim())
            )
        })
}
