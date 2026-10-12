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
 * Only the selected tab's panel is shown. The others use hidden="until-found" instead of display: none, so the
 * browser's find-in-page still searches them; `onBeforeMatch` then selects the tab that holds the match.
 */
function syncPanels(root: ParentNode = document) {
    root.querySelectorAll<HTMLInputElement>('.tabs-input').forEach((input) => {
        const panel = panelFor(input)
        if (!panel) return
        if (input.checked) panel.removeAttribute('hidden')
        else panel.setAttribute('hidden', 'until-found')
    })
}

function onTabChange(event: Event) {
    if (
        event.target instanceof HTMLInputElement &&
        event.target.matches('.tabs-input')
    )
        syncPanels()
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
    syncPanels()
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
    }
    syncPanels()
    window.sessionStorage.setItem(storageKeyPrefix + group, id)
}

let delegated = false

export function initTabs() {
    if (!delegated) {
        delegated = true
        document.addEventListener('change', onTabChange)
        document.addEventListener('beforematch', onBeforeMatch)
    }
    ready()
}
