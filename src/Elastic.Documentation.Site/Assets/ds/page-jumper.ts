// The page jumper: a search field that lists matching pages in a panel under it as you type, with no dialog.
// Without script the field is a plain search form, so this only adds the listing. Hooks are data attributes.
import { drawResults, querySearch } from './search'

const debounceMs = 250

class PageJumper {
    private readonly input: HTMLInputElement
    private readonly panel: HTMLElement
    private readonly status: HTMLElement
    private readonly list: HTMLElement
    private readonly loadingBar: HTMLElement | null
    private readonly template: HTMLTemplateElement
    private readonly prefix: string
    private timer = 0
    private controller: AbortController | null = null
    private active = -1

    constructor(private readonly root: HTMLElement) {
        this.input = root.querySelector<HTMLInputElement>(
            '[data-page-jumper-input]'
        )!
        this.panel = root.querySelector<HTMLElement>(
            '[data-page-jumper-panel]'
        )!
        this.status = root.querySelector<HTMLElement>(
            '[data-page-jumper-status]'
        )!
        this.list = root.querySelector<HTMLElement>(
            '[data-page-jumper-results]'
        )!
        this.loadingBar = root.querySelector<HTMLElement>(
            '[data-page-jumper-loading]'
        )
        this.template = root.querySelector<HTMLTemplateElement>(
            'template[data-search-result]'
        )!
        this.prefix = `${this.list.id || 'page-jumper-results'}-result`

        this.input.addEventListener('input', () => this.schedule())
        this.input.addEventListener('keydown', (event) => this.keydown(event))
        this.input.addEventListener('focus', () => {
            if (this.list.children.length > 0) this.show(true)
        })
        document.addEventListener('click', (event) => {
            if (!root.contains(event.target as Node)) this.show(false)
        })
        this.list.addEventListener('click', () => this.show(false))
    }

    private show(open: boolean) {
        this.panel.hidden = !open
        this.input.setAttribute('aria-expanded', String(open))
    }

    private loading(on: boolean) {
        if (this.loadingBar) this.loadingBar.hidden = !on
    }

    private schedule() {
        window.clearTimeout(this.timer)
        this.controller?.abort()
        const term = this.input.value.trim()
        if (!term) {
            this.loading(false)
            this.list.replaceChildren()
            this.show(false)
            return
        }
        this.timer = window.setTimeout(() => void this.run(term), debounceMs)
    }

    private async run(term: string) {
        this.controller = new AbortController()
        this.status.textContent = 'Searching…'
        this.loading(true)
        this.show(true)
        try {
            const results = await querySearch(term, this.controller.signal)
            if (this.input.value.trim() !== term) return
            this.loading(false)
            this.render(results.slice(0, 8))
        } catch (error) {
            if ((error as Error).name === 'AbortError') return
            this.loading(false)
            this.list.replaceChildren()
            this.status.textContent = 'Search is unavailable right now.'
        }
    }

    private render(results: Parameters<typeof drawResults>[2]) {
        this.active = -1
        this.status.textContent =
            results.length === 0
                ? 'No pages match.'
                : `${results.length} ${results.length === 1 ? 'page' : 'pages'}.`
        drawResults(this.list, this.template, results, {
            groupClass: 'page-jumper__group',
            idPrefix: this.prefix,
        })
        if (results.length > 0) this.move(0)
    }

    private items(): HTMLElement[] {
        return Array.from(
            this.list.querySelectorAll<HTMLElement>('[role="option"]')
        )
    }

    private move(index: number) {
        const items = this.items()
        if (items.length === 0) return
        this.active = (index + items.length) % items.length
        items.forEach((item, i) =>
            item.setAttribute('aria-selected', String(i === this.active))
        )
        this.input.setAttribute('aria-activedescendant', items[this.active].id)
    }

    private keydown(event: KeyboardEvent) {
        if (event.key === 'Escape') {
            this.show(false)
        } else if (event.key === 'ArrowDown') {
            event.preventDefault()
            this.show(true)
            this.move(this.active + 1)
        } else if (event.key === 'ArrowUp') {
            event.preventDefault()
            this.move(this.active - 1)
        } else if (event.key === 'Enter' && this.active >= 0) {
            const link =
                this.items()[this.active]?.querySelector<HTMLAnchorElement>('a')
            if (link && !this.panel.hidden) {
                event.preventDefault()
                link.click()
            }
        }
    }
}

export function initPageJumpers(scope: ParentNode = document) {
    scope
        .querySelectorAll<HTMLElement>(
            '[data-page-jumper]:not([data-page-jumper-bound])'
        )
        .forEach((root) => {
            root.dataset.pageJumperBound = ''
            new PageJumper(root)
        })
}
