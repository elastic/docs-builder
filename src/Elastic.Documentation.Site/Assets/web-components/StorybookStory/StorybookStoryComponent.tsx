/**
 * <storybook-story> custom element.
 *
 * Loads a story bundle and mounts a composed Storybook story into this element.
 * The bundle is fully self-contained (owns React, ReactDOM, EuiProvider).
 * No React dependency on the docs-builder side.
 *
 * Usage:
 *   <storybook-story story-id="components-button--regular" bundle="/_static/stories/registry.js" />
 */

type MountFn = (storyId: string, container: HTMLElement) => Promise<void>
type UnmountFn = (container: HTMLElement) => void

interface StoryBundle {
    mountStory: MountFn
    unmountStory: UnmountFn
}

// Cache: bundle URL → loaded module
const bundleCache = new Map<string, Promise<StoryBundle>>()

function loadBundle(url: string): Promise<StoryBundle> {
    if (!bundleCache.has(url)) {
        bundleCache.set(url, import(/* @vite-ignore */ url))
    }
    return bundleCache.get(url)!
}

class StorybookStoryElement extends HTMLElement {
    private container: HTMLDivElement | null = null
    private bundle: StoryBundle | null = null

    static get observedAttributes() {
        return ['story-id', 'bundle']
    }

    connectedCallback() {
        this.container = document.createElement('div')
        this.appendChild(this.container)
        this.mount()
    }

    disconnectedCallback() {
        if (this.container && this.bundle) {
            this.bundle.unmountStory(this.container)
        }
    }

    attributeChangedCallback() {
        if (this.container) {
            this.mount()
        }
    }

    private async mount() {
        const storyId = this.getAttribute('story-id')
        const bundleUrl = this.getAttribute('bundle')

        if (!storyId || !bundleUrl || !this.container) return

        try {
            this.bundle = await loadBundle(bundleUrl)
            await this.bundle.mountStory(storyId, this.container)
        } catch (err) {
            if (this.container) {
                this.container.innerHTML = `<div style="padding:1rem;color:#BD271E;font-size:14px">Failed to load story: ${err instanceof Error ? err.message : String(err)}</div>`
            }
        }
    }
}

customElements.define('storybook-story', StorybookStoryElement)
