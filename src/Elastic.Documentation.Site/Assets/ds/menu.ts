// Dismisses native details menus on outside click, on Escape and after choosing a link.
// The menus open and close without this; it only adds the dismissal that a popup is expected to have.
export function initMenus(scope: ParentNode = document) {
    scope
        .querySelectorAll<HTMLDetailsElement>(
            'details[data-menu]:not([data-menu-bound])'
        )
        .forEach((menu) => {
            menu.dataset.menuBound = ''

            menu.addEventListener('keydown', (event) => {
                if (event.key === 'Escape' && menu.open) {
                    menu.open = false
                    menu.querySelector<HTMLElement>('summary')?.focus()
                }
            })
            menu.addEventListener('click', (event) => {
                if ((event.target as Element).closest('a')) menu.open = false
            })

            // A mouse can open a hover menu by resting on it; touch and keyboard still use the click.
            if (menu.hasAttribute('data-hover-menu')) bindHover(menu)
        })
}

// One document listener closes every open menu when the click lands elsewhere.
let outsideClickBound = false
export function bindOutsideClick() {
    if (outsideClickBound) return
    outsideClickBound = true
    document.addEventListener('click', (event) => {
        document
            .querySelectorAll<HTMLDetailsElement>('details[data-menu][open]')
            .forEach((menu) => {
                if (!menu.contains(event.target as Node)) menu.open = false
            })
    })
}

const hoverCloseDelay = 150

function bindHover(menu: HTMLDetailsElement) {
    let timer = 0
    menu.addEventListener('pointerenter', (event) => {
        if (event.pointerType !== 'mouse') return
        window.clearTimeout(timer)
        menu.open = true
    })
    menu.addEventListener('pointerleave', (event) => {
        if (event.pointerType !== 'mouse') return
        timer = window.setTimeout(() => (menu.open = false), hoverCloseDelay)
    })
}
