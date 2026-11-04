export {}

type MountFn = (storyId: string, container: HTMLElement) => Promise<void>
type UnmountFn = (container: HTMLElement) => void
type KibanaPublicPath = Record<string, string>

declare global {
    interface Window {
        __kbnPublicPath__?: KibanaPublicPath
        __kbnHardenPrototypes__?: boolean
    }
}

interface StoryEntry {
    mountStory: MountFn
    unmountStory: UnmountFn
}

interface StoryBootstrap {
    publicPath?: string
    scripts?: string[]
    styles?: string[]
}

class StorybookLoadError extends Error {
    constructor(
        public readonly title: string,
        public readonly detail: string
    ) {
        super(detail)
    }
}

const entryCache = new Map<string, Promise<StoryEntry>>()
const bootstrapCache = new Map<string, Promise<void>>()
const scriptCache = new Map<string, Promise<void>>()
const styleCache = new Map<string, Promise<void>>()

const loadEntry = (url: string): Promise<StoryEntry> => {
    if (!entryCache.has(url)) {
        entryCache.set(
            url,
            import(/* @vite-ignore */ url).then(validateEntry).catch((err) => {
                entryCache.delete(url)
                if (err instanceof StorybookLoadError) throw err
                throw new StorybookLoadError(
                    'Storybook entry module could not be imported.',
                    `Check that the module exists and CORS allows this docs origin: ${url}`
                )
            })
        )
    }
    return entryCache.get(url)!
}

const validateEntry = (entry: unknown): StoryEntry => {
    const candidate = entry as Partial<StoryEntry>
    if (
        typeof candidate.mountStory !== 'function' ||
        typeof candidate.unmountStory !== 'function'
    ) {
        throw new StorybookLoadError(
            'Storybook entry module has an unsupported contract.',
            'Expected mountStory(storyId, container) and unmountStory(container).'
        )
    }
    return candidate as StoryEntry
}

const loadStylesheet = (url: string): Promise<void> => {
    if (!styleCache.has(url)) {
        styleCache.set(
            url,
            new Promise((resolve, reject) => {
                const existing = Array.from(
                    document.querySelectorAll<HTMLLinkElement>(
                        'link[data-storybook-style]'
                    )
                ).find((link) => link.dataset.storybookStyle === url)
                if (existing) {
                    resolve()
                    return
                }

                const link = document.createElement('link')
                link.rel = 'stylesheet'
                link.href = url
                link.dataset.storybookStyle = url
                link.onload = () => resolve()
                link.onerror = () => {
                    styleCache.delete(url)
                    reject(
                        new StorybookLoadError(
                            'Storybook stylesheet could not be loaded.',
                            `Missing or blocked stylesheet: ${url}`
                        )
                    )
                }
                document.head.appendChild(link)
            })
        )
    }
    return styleCache.get(url)!
}

const loadScript = (url: string): Promise<void> => {
    if (!scriptCache.has(url)) {
        scriptCache.set(
            url,
            new Promise((resolve, reject) => {
                const existing = Array.from(
                    document.querySelectorAll<HTMLScriptElement>(
                        'script[data-storybook-script]'
                    )
                ).find((script) => script.dataset.storybookScript === url)
                if (existing) {
                    resolve()
                    return
                }

                const script = document.createElement('script')
                script.src = url
                script.async = false
                script.dataset.storybookScript = url
                script.onload = () => resolve()
                script.onerror = () => {
                    scriptCache.delete(url)
                    reject(
                        new StorybookLoadError(
                            'Storybook bootstrap script could not be loaded.',
                            `Missing or blocked script: ${url}`
                        )
                    )
                }
                document.head.appendChild(script)
            })
        )
    }
    return scriptCache.get(url)!
}

const parseBootstrap = (value: string | null): StoryBootstrap | null => {
    if (!value) return null

    try {
        return JSON.parse(value) as StoryBootstrap
    } catch {
        throw new StorybookLoadError(
            'Storybook bootstrap data is invalid.',
            'The registry provided bootstrap data that is not valid JSON.'
        )
    }
}

const setKibanaGlobals = (publicPath?: string) => {
    if (!publicPath) return

    const publicPaths = {
        'kbn-ui-shared-deps-npm': publicPath,
        'kbn-ui-shared-deps-src': publicPath,
        'kbn-monaco': publicPath,
    }

    window.__kbnPublicPath__ = publicPaths
    window.__kbnHardenPrototypes__ = false

    try {
        if (window.top) {
            window.top.__kbnPublicPath__ = publicPaths
            window.top.__kbnHardenPrototypes__ = false
        }
    } catch {
        // Cross-origin frames cannot access `top`.
    }
}

const loadBootstrap = async (bootstrap: StoryBootstrap | null) => {
    if (!bootstrap) return

    setKibanaGlobals(bootstrap.publicPath)

    const cacheKey = JSON.stringify({
        publicPath: bootstrap.publicPath ?? '',
        scripts: bootstrap.scripts ?? [],
        styles: bootstrap.styles ?? [],
    })
    if (!bootstrapCache.has(cacheKey)) {
        bootstrapCache.set(
            cacheKey,
            (async () => {
                await Promise.all((bootstrap.styles ?? []).map(loadStylesheet))
                for (const script of bootstrap.scripts ?? []) {
                    await loadScript(script)
                }
            })()
        )
    }

    await bootstrapCache.get(cacheKey)
}

class StorybookStoryElement extends HTMLElement {
    private container: HTMLDivElement | null = null
    private entry: StoryEntry | null = null
    private mountVersion = 0

    static get observedAttributes() {
        return ['story-id', 'entry', 'bootstrap']
    }

    connectedCallback() {
        if (!this.container) {
            this.container = document.createElement('div')
            this.appendChild(this.container)
        }
        this.mount()
    }

    disconnectedCallback() {
        this.mountVersion++
        this.unmount()
    }

    attributeChangedCallback() {
        if (this.container) {
            this.mount()
        }
    }

    private unmount() {
        if (this.container && this.entry) {
            this.entry.unmountStory(this.container)
            this.entry = null
        }
    }

    private async mount() {
        const storyId = this.getAttribute('story-id')
        const entryUrl = this.getAttribute('entry')
        const currentMount = ++this.mountVersion

        if (!storyId || !entryUrl || !this.container) return

        this.unmount()
        this.container.replaceChildren()

        try {
            await loadBootstrap(parseBootstrap(this.getAttribute('bootstrap')))
            const entry = await loadEntry(entryUrl)
            if (
                currentMount !== this.mountVersion ||
                !this.container ||
                !this.isConnected
            )
                return

            this.entry = entry
            try {
                await entry.mountStory(storyId, this.container)
            } catch (err) {
                throw new StorybookLoadError(
                    'Storybook story failed while mounting.',
                    err instanceof Error ? err.message : String(err)
                )
            }
        } catch (err) {
            if (currentMount === this.mountVersion && this.container) {
                this.container.replaceChildren(createErrorMessage(err))
            }
        }
    }
}

const createErrorMessage = (err: unknown): HTMLDivElement => {
    const message = document.createElement('div')
    message.className = 'storybook-embed-error'

    const title = document.createElement('strong')
    title.textContent =
        err instanceof StorybookLoadError
            ? err.title
            : 'Storybook story failed to load.'
    message.appendChild(title)

    const detail = document.createElement('div')
    detail.textContent =
        err instanceof StorybookLoadError
            ? err.detail
            : err instanceof Error
              ? err.message
              : String(err)
    message.appendChild(detail)

    return message
}

customElements.define('storybook-story', StorybookStoryElement)
