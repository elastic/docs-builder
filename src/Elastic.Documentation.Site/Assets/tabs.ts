// TODO: refactor to typescript. this was copied from the previous implementation

// @ts-check

// Extra JS capability for selected tabs to be synced
// The selection is stored in local storage so that it persists across page loads.

const sd_id_to_elements: { [key: string]: HTMLElement[] } = {}
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
                const [group, id, key] = data

                // add click event listener
                label.addEventListener('click', onSDLabelClick)

                // store map of key to elements
                if (!sd_id_to_elements[key]) {
                    sd_id_to_elements[key] = []
                }
                sd_id_to_elements[key].push(label)

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
    for (const label of sd_id_to_elements[key]) {
        if (label === this) {
            continue
        }
        if (label.previousElementSibling instanceof HTMLInputElement) {
            label.previousElementSibling.checked = true
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

            // Reuse the label's sync logic so other tab-sets in the same group follow.
            const label = input.nextElementSibling
            if (label instanceof HTMLLabelElement) {
                onSDLabelClick.call(label)
            }
        })
    })
}

export function initTabs() {
    ready()
    initDropdowns()
}
