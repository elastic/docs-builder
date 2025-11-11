// TODO: refactor to typescript. this was copied from the tabs implementation
import { $$ } from 'select-dom'

// Extra JS capability for selected applies switches to be synced
// The selection is stored in local storage so that it persists across page loads.

const as_id_to_elements: { [key: string]: HTMLElement[] } = {}
const storageKeyPrefix = 'applies-switch-id-'

function create_key(el: HTMLElement) {
    const syncId = el.getAttribute('data-sync-id')
    const syncGroup = el.getAttribute('data-sync-group')
    if (!syncId || !syncGroup) return null
    return [syncGroup, syncId, syncGroup + '--' + syncId]
}

/**
 * Initialize the applies switch selection.
 *
 */
function ready() {
    // Find all applies switches with sync data

    const groups: string[] = []

    $$('.applies-switch-label').forEach((label) => {
        if (label instanceof HTMLElement) {
            const data = create_key(label)
            if (data) {
                const [group, id, key] = data

                // add click event listener
                label.addEventListener('click', onAppliesSwitchLabelClick)

                // store map of key to elements
                if (!as_id_to_elements[key]) {
                    as_id_to_elements[key] = []
                }
                as_id_to_elements[key].push(label)

                if (groups.indexOf(group) === -1) {
                    groups.push(group)
                    // Check if a specific switch has been selected via URL parameter
                    const switchParam = new URLSearchParams(
                        window.location.search
                    ).get(group)
                    if (switchParam) {
                        window.sessionStorage.setItem(
                            storageKeyPrefix + group,
                            switchParam
                        )
                    }
                }

                // Check is a specific switch has been selected previously
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
 *  Activate other switches with the same sync id.
 *
 * @this {HTMLElement} - The element that was clicked.
 */
function onAppliesSwitchLabelClick(this: HTMLLabelElement) {
    const data = create_key(this)
    if (!data) return
    const [group, id, key] = data
    for (const label of as_id_to_elements[key]) {
        if (label === this) {
            continue
        }
        if (label.previousElementSibling instanceof HTMLInputElement) {
            label.previousElementSibling.checked = true
        }
    }
    window.sessionStorage.setItem(storageKeyPrefix + group, id)
}

export function initAppliesSwitch() {
    ready()
}
