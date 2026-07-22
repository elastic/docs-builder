type ComponentLoader = () => Promise<unknown>
type ComponentLoaders = Readonly<Record<string, ComponentLoader>>

export function createWebComponentLoader(componentLoaders: ComponentLoaders) {
    const loadingComponents = new Map<string, Promise<unknown>>()

    return async function loadWebComponents(
        root: ParentNode = document
    ): Promise<void> {
        const loads: Promise<unknown>[] = []

        for (const [tagName, load] of Object.entries(componentLoaders)) {
            if (!root.querySelector(tagName)) continue
            if (customElements.get(tagName)) continue

            const existingLoad = loadingComponents.get(tagName)
            if (existingLoad) {
                loads.push(existingLoad)
                continue
            }

            const componentLoad = load().catch((error) => {
                loadingComponents.delete(tagName)
                throw error
            })
            loadingComponents.set(tagName, componentLoad)
            loads.push(componentLoad)
        }

        await Promise.all(loads)
    }
}

export const loadWebComponents = createWebComponentLoader({
    'version-dropdown': () => import('./VersionDropdown'),
    'applies-to-popover': () => import('./AppliesToPopover'),
    'diagnostics-panel': () => import('./Diagnostics/DiagnosticsComponent'),
    'storybook-story': () => import('./StorybookStory/StorybookStoryComponent'),
})
