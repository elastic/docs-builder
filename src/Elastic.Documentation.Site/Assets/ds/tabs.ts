// Keeps tabs that share a sync group and key in step across the page and across page loads.
// The tabs work without this: it only adds the shared selection. Hooks are data attributes, never styling classes.
const storageKeyPrefix = 'tab-id-'

function readSelection(group: string): string | null {
    try {
        return window.sessionStorage.getItem(storageKeyPrefix + group)
    } catch {
        return null
    }
}

function writeSelection(group: string, key: string) {
    try {
        window.sessionStorage.setItem(storageKeyPrefix + group, key)
    } catch {
        // storage unavailable: the selection still applies on this page
    }
}

function inputFor(label: Element): HTMLInputElement | null {
    const input = label.previousElementSibling
    return input instanceof HTMLInputElement ? input : null
}

function selectTab(group: string, key: string, scope: ParentNode) {
    scope
        .querySelectorAll<HTMLElement>(
            '[data-tab-sync-key][data-tab-sync-group]'
        )
        .forEach((label) => {
            if (
                label.dataset.tabSyncGroup === group &&
                label.dataset.tabSyncKey === key
            ) {
                const input = inputFor(label)
                if (input) input.checked = true
            }
        })
}

export function initTabSync(scope: ParentNode = document) {
    const seenGroups = new Set<string>()
    scope
        .querySelectorAll<HTMLElement>(
            '[data-tab-sync-key][data-tab-sync-group]:not([data-tab-sync-bound])'
        )
        .forEach((label) => {
            const group = label.dataset.tabSyncGroup!
            const key = label.dataset.tabSyncKey!
            label.dataset.tabSyncBound = ''

            label.addEventListener('click', () => {
                writeSelection(group, key)
                selectTab(group, key, document)
            })

            if (!seenGroups.has(group)) {
                seenGroups.add(group)
                // A query parameter named after the group preselects a tab.
                const fromUrl = new URLSearchParams(window.location.search).get(
                    group
                )
                if (fromUrl) writeSelection(group, fromUrl)
            }

            if (readSelection(group) === key) {
                const input = inputFor(label)
                if (input) input.checked = true
            }
        })
}
