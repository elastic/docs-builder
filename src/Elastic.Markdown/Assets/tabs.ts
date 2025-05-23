// TODO: refactor to typescript. this was copied from the previous implementation

// @ts-check

// Extra JS capability for selected tabs to be synced
// The selection is stored in local storage so that it persists across page loads.

const sd_id_to_elements = {}
const storageKeyPrefix = 'sphinx-design-tab-id-'

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

    const groups = []

    document.querySelectorAll('.tabs-label').forEach((label) => {
        if (label instanceof HTMLElement) {
            const data = create_key(label)
            if (data) {
                const [group, id, key] = data

                // add click event listener
                label.onclick = onSDLabelClick

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
function onSDLabelClick() {
    const data = create_key(this)
    if (!data) return
    const [group, id, key] = data
    for (const label of sd_id_to_elements[key]) {
        if (label === this) continue
        label.previousElementSibling.checked = true
    }
    window.sessionStorage.setItem(storageKeyPrefix + group, id)
}

export function initTabs() {
    ready()
}
