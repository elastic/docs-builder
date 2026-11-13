// TODO: refactor to typescript. this was copied from the previous implementation

// @ts-check

// Extra JS capability for selected tabs to be synced
// The selection is stored in local storage so that it persists across page loads.

const storageKeyPrefix = 'tab-id-'

function create_key(el: HTMLElement) {
    const syncId = el.getAttribute('data-sync-id')
    const syncGroup = el.getAttribute('data-sync-group')
    if (!syncId || !syncGroup) return null
    return [syncGroup, syncId, syncGroup + '--' + syncId]
}

/**
 * Initialize the tab selection.
 *
 */
function ready() {
    // Find all tabs with sync data

    const groups: string[] = []

    document.querySelectorAll('.tabs-label').forEach((label) => {
        if (label instanceof HTMLElement) {
            const data = create_key(label)
            if (data) {
                const [group, id] = data

                // add click event listener
                label.addEventListener('click', onSDLabelClick)

                if (groups.indexOf(group) === -1) {
                    groups.push(group)
                    // Check if a specific tab has been selected via URL parameter
                    const tabParam = new URLSearchParams(
                        window.location.search
                    ).get(group)
                    if (tabParam) {
                        window.sessionStorage.setItem(
                            storageKeyPrefix + group,
                            tabParam
                        )
                    }
                }

                // Check is a specific tab has been selected previously
                const previousId = window.sessionStorage.getItem(
                    storageKeyPrefix + group
                )
                if (previousId === id) {
                    ;(
                        label.previousElementSibling as HTMLInputElement
                    ).checked = true
                }
            }
        }
    })
    syncPanels()
}

/** The content panel that follows a tab's input and label. */
function panelFor(input: HTMLInputElement): HTMLElement | null {
    const panel = input.nextElementSibling?.nextElementSibling
    return panel instanceof HTMLElement &&
        panel.classList.contains('tabs-content')
        ? panel
        : null
}

/**
 * A browser that does not know hidden="until-found" treats it as plain hidden and the CSS that undoes display: none
 * would then show every panel, so those browsers keep the CSS-only tabs (the checked tab's panel is the one shown).
 */
function supportsUntilFound(): boolean {
    return 'onbeforematch' in document
}

/**
 * Only the selected tab's panel is shown. The others use hidden="until-found" instead of display: none, so the
 * browser's find-in-page still searches them; `onBeforeMatch` then selects the tab that holds the match.
 */
function syncPanels(root: ParentNode = document) {
    if (!supportsUntilFound()) return
    root.querySelectorAll<HTMLInputElement>('.tabs-input').forEach((input) => {
        const panel = panelFor(input)
        const hide = !input.checked
        if (!panel || panel.hasAttribute('hidden') === hide) return
        if (hide) panel.setAttribute('hidden', 'until-found')
        else panel.removeAttribute('hidden')
    })
}

/** A tab's input and its siblings are one tab set; a change in one never touches another. */
function syncSetOf(input: HTMLInputElement) {
    syncPanels(input.parentElement ?? document)
}

function onTabChange(event: Event) {
    if (
        event.target instanceof HTMLInputElement &&
        event.target.matches('.tabs-input')
    )
        syncSetOf(event.target)
}

/** Find-in-page matched text in a hidden tab: select that tab before the browser reveals it. */
function onBeforeMatch(event: Event) {
    const panel = (event.target as HTMLElement | null)?.closest<HTMLElement>(
        '.tabs-content'
    )
    const input = panel?.previousElementSibling?.previousElementSibling
    if (!(input instanceof HTMLInputElement) || !input.matches('.tabs-input'))
        return
    input.checked = true
    syncSetOf(input)
    const label = input.nextElementSibling
    if (label instanceof HTMLElement) syncSelectToLabel(label)
}

/**
 *  Activate other tabs with the same sync id.
 *
 * @this {HTMLElement} - The element that was clicked.
 */
function onSDLabelClick(this: HTMLLabelElement) {
    const data = create_key(this)
    if (!data) return
    const [group, id, key] = data
    const labels = document.querySelectorAll<HTMLElement>(
        '.tabs-label[data-sync-group][data-sync-id]'
    )
    for (const label of labels) {
        if (create_key(label)?.[2] !== key) continue
        if (label === this) {
            continue
        }
        if (label.previousElementSibling instanceof HTMLInputElement) {
            label.previousElementSibling.checked = true
            // Programmatic checks fire no change event. The clicked tab's own set is synced by its change event.
            syncSetOf(label.previousElementSibling)
        }
        // A synced tab-set may be rendered as a dropdown; keep its <select> aligned.
        syncSelectToLabel(label)
    }
    syncSelectToLabel(this)
    window.sessionStorage.setItem(storageKeyPrefix + group, id)
}

/**
 * Point the tab-set's <select> (if it has one) at the tab the label belongs to.
 */
function syncSelectToLabel(label: HTMLElement) {
    const select = label
        .closest('.tabs')
        ?.querySelector<HTMLSelectElement>('.tabs-select')
    const target = label.getAttribute('for')
    if (select && target) {
        select.value = target
    }
}

/**
 * Reveal the <select> rendered for dropdown tab-sets and wire it to the radio
 * inputs that actually drive the panels. The tab strip is the no-JS fallback,
 * so the dropdown only takes over once this runs.
 */
function initDropdowns() {
    document.querySelectorAll<HTMLElement>('.tabs-dropdown').forEach((tabs) => {
        const select = tabs.querySelector<HTMLSelectElement>('.tabs-select')
        if (!select) return

        const wrapper = select.closest<HTMLElement>('.tabs-select-wrapper')
        if (wrapper) {
            wrapper.hidden = false
        }
        tabs.classList.add('tabs-dropdown-active')

        // The radios still drive the panels, but the <select> is now the only
        // control a user should reach. They are opacity-0 rather than removed,
        // so without this a keyboard user can tab onto an invisible radio and
        // arrow through it, flipping the panel behind the select's back.
        tabs.querySelectorAll<HTMLInputElement>(':scope > .tabs-input').forEach(
            (input) => {
                input.tabIndex = -1
                input.setAttribute('aria-hidden', 'true')
            }
        )

        // The checked input is the source of truth — it may have been restored
        // from sessionStorage or a URL param by ready(). Scoped to this tab
        // set's own radios: a nested tab set's checked radio can come first in
        // document order, and its id matches none of our options.
        const checked = tabs.querySelector<HTMLInputElement>(
            ':scope > .tabs-input:checked'
        )
        if (checked) {
            select.value = checked.id
        }

        select.addEventListener('change', () => {
            const input = document.getElementById(select.value)
            if (!(input instanceof HTMLInputElement)) return
            input.checked = true
            // Programmatic checks fire no change event, so sync this set's panels here.
            syncSetOf(input)

            // Reuse the label's sync logic so other tab-sets in the same group follow.
            const label = input.nextElementSibling
            if (label instanceof HTMLLabelElement) {
                onSDLabelClick.call(label)
            }
        })
    })
}
let delegated = false

export function initTabs() {
    if (!delegated) {
        delegated = true
        document.addEventListener('change', onTabChange)
        document.addEventListener('beforematch', onBeforeMatch)
    }
    ready()
    initDropdowns()
}
