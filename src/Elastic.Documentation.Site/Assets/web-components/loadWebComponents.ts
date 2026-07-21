type ComponentLoader = () => Promise<unknown>
type ComponentLoaders = Readonly<Record<string, ComponentLoader>>

export function createWebComponentLoader(componentLoaders: ComponentLoaders) {
    const loadingComponents = new Map<string, Promise<void>>()

    return async function loadWebComponents(
        root: ParentNode = document
    ): Promise<void> {
        const loads = Object.entries(componentLoaders).flatMap(
            ([tagName, load]) => {
                if (
                    !root.querySelector(tagName) ||
                    customElements.get(tagName)
                ) {
                    return []
                }

                const existingLoad = loadingComponents.get(tagName)
                if (existingLoad) return [existingLoad]

                const componentLoad = load()
                    .then(() => undefined)
                    .catch((error) => {
                        loadingComponents.delete(tagName)
                        throw error
                    })
                loadingComponents.set(tagName, componentLoad)
                return [componentLoad]
            }
        )

        await Promise.all(loads)
    }
}

export const loadWebComponents = createWebComponentLoader({
    'navigation-search': () =>
        import('./NavigationSearch/NavigationSearchComponent'),
    'ask-ai': () => import('./AskAi/AskAi'),
    'version-dropdown': () => import('./VersionDropdown'),
    'applies-to-popover': () => import('./AppliesToPopover'),
    'full-page-search': () =>
        import('./FullPageSearch/FullPageSearchComponent'),
    'diagnostics-panel': () => import('./Diagnostics/DiagnosticsComponent'),
    'storybook-story': () => import('./StorybookStory/StorybookStoryComponent'),
})
