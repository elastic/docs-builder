// TODO: refactor to typescript. this was copied from the tabs implementation
import { $$optional } from 'select-dom'

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

    $$optional('.applies-switch-label').forEach((label) => {
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

    // Reflect the (possibly restored) selection in dropdown content panes
    $$optional('.applies-switch--dropdown').forEach(updateDropdownContents)
}

/**
 *  Activate other switches with the same sync id.
 *
 * @this {HTMLElement} - The element that was clicked.
 */
function onAppliesSwitchLabelClick(this: HTMLLabelElement) {
    syncSelection(this)
}

/**
 * Activate the same applies_to selection in all other switches on the page
 * and persist it. `label` is the trigger label of the newly selected option.
 */
function syncSelection(label: HTMLElement) {
    const data = create_key(label)
    if (!data) return
    const [group, id, key] = data
    for (const other of as_id_to_elements[key] ?? []) {
        if (other === label) {
            continue
        }
        if (other.previousElementSibling instanceof HTMLInputElement) {
            other.previousElementSibling.checked = true
            // Setting .checked programmatically fires no change event, so
            // synced dropdowns need their content panes updated explicitly
            const dropdown = other.closest('.applies-switch--dropdown')
            if (dropdown) updateDropdownContents(dropdown)
        }
    }
    window.sessionStorage.setItem(storageKeyPrefix + group, id)
}

/**
 * Reflect the checked input of a dropdown switch: show the matching content
 * pane and hide the matching panel row (the current selection is already
 * visible in the chip, the panel only lists the alternatives).
 *
 * Dropdown switches group their inputs and labels in a selector overlay, so
 * the pure-CSS sibling selector used by the tabs appearance cannot reach the
 * content panes.
 */
function updateDropdownContents(dropdown: Element) {
    const checked = dropdown.querySelector('.applies-switch-input:checked')
    if (!checked) return
    const index = checked.getAttribute('data-index')
    dropdown.querySelectorAll('.applies-switch-content').forEach((content) => {
        content.classList.toggle(
            'applies-switch-content--active',
            content.getAttribute('data-index') === index
        )
    })
    dropdown.querySelectorAll('.applies-switch-panel-row').forEach((row) => {
        row.classList.toggle(
            'applies-switch-panel-row--current',
            row.getAttribute('data-index') === index
        )
    })
}

/**
 * Open/close behavior for switches with the dropdown appearance.
 *
 * Delegated document-level listeners registered once at module scope so they
 * survive htmx swaps (initAppliesSwitch re-runs on every htmx:load).
 */
function closeDropdowns(except?: Element | null) {
    document
        .querySelectorAll('.applies-switch--dropdown.open')
        .forEach((dropdown) => {
            if (dropdown !== except) dropdown.classList.remove('open')
        })
}

document.addEventListener('click', (event) => {
    const target = event.target as HTMLElement
    // A click on a label also fires a synthetic click on its radio input;
    // ignore it so it doesn't immediately close the dropdown we just opened.
    if (target.closest('.applies-switch--dropdown .applies-switch-input')) {
        return
    }
    // Selecting a panel row checks its radio natively (label for=); the
    // change listener below does the rest, so only close the menu here.
    if (target.closest('.applies-switch-panel-row')) {
        closeDropdowns()
        return
    }
    const label = target.closest(
        '.applies-switch--dropdown .applies-switch-label'
    )
    if (!label) {
        closeDropdowns()
        return
    }
    const dropdown = label.closest('.applies-switch--dropdown')
    if (!dropdown) return
    dropdown.classList.toggle('open')
    closeDropdowns(dropdown)
})

document.addEventListener('change', (event) => {
    const input = (event.target as HTMLElement).closest(
        '.applies-switch--dropdown .applies-switch-input'
    )
    const dropdown = input?.closest('.applies-switch--dropdown')
    if (!input || !dropdown) return
    updateDropdownContents(dropdown)
    // Panel-row and keyboard selections bypass the trigger-label click
    // handler, so propagate the sync from here.
    const label = input.nextElementSibling
    if (label instanceof HTMLElement) syncSelection(label)
})

document.addEventListener('keydown', (event) => {
    if (event.key === 'Escape') closeDropdowns()
})

export function initAppliesSwitch() {
    ready()
}
