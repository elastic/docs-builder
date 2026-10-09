// The search dialog. A <dialog> gives focus handling, Escape and the backdrop; this module opens it, queries the right backend
// and draws results from a server-provided <template>. Hooks are data attributes, never styling classes.
import { config } from '../config'
import { searchPagefind } from '../web-components/ModalSearch/pagefind'

export interface SearchResult {
    url: string
    title: string
    description: string
    parents: Array<{ url: string; title: string }>
}

const debounceMs = 300

export async function querySearch(
    term: string,
    signal?: AbortSignal
): Promise<SearchResult[]> {
    if (config.buildType === 'isolated') return searchPagefind(term)

    const params = new URLSearchParams({ q: term, page: '1' })
    const response = await fetch(
        `${config.apiBasePath}/v1/navigation-search?${params}`,
        { signal }
    )
    if (!response.ok)
        throw new Error(`Search failed with status ${response.status}`)
    const data = (await response.json()) as { results?: SearchResult[] }
    return data.results ?? []
}

// Excerpts come with <mark> highlights. Build them from text nodes so nothing from the index is ever parsed as HTML.
export function appendExcerpt(target: HTMLElement, excerpt: string) {
    target.replaceChildren()
    excerpt.split(/(<mark>.*?<\/mark>)/gs).forEach((part) => {
        if (!part) return
        const marked = /^<mark>(.*)<\/mark>$/s.exec(part)
        if (marked) {
            const mark = document.createElement('mark')
            mark.textContent = marked[1].replace(/<[^>]*>/g, '')
            target.append(mark)
        } else {
            target.append(document.createTextNode(part.replace(/<[^>]*>/g, '')))
        }
    })
}

class SearchDialog {
    private readonly input: HTMLInputElement
    private readonly status: HTMLElement
    private readonly list: HTMLElement
    private readonly template: HTMLTemplateElement
    private timer = 0
    private controller: AbortController | null = null
    private active = -1

    constructor(private readonly dialog: HTMLDialogElement) {
        this.input = dialog.querySelector<HTMLInputElement>(
            '[data-search-input]'
        )!
        this.status = dialog.querySelector<HTMLElement>('[data-search-status]')!
        this.list = dialog.querySelector<HTMLElement>('[data-search-results]')!
        this.template = dialog.querySelector<HTMLTemplateElement>(
            'template[data-search-result]'
        )!

        this.input.addEventListener('input', () => this.schedule())
        this.input.addEventListener('keydown', (event) => this.keydown(event))
        // A click on the backdrop lands on the dialog element itself.
        dialog.addEventListener('click', (event) => {
            if (event.target === dialog) dialog.close()
        })
        dialog.addEventListener('close', () => this.reset())
        // Following a result closes the dialog; the page swap then replaces the page behind it.
        this.list.addEventListener('click', (event) => {
            if ((event.target as Element).closest('a')) dialog.close()
        })
    }

    open() {
        if (!this.dialog.open) this.dialog.showModal()
        this.input.focus()
        this.input.select()
    }

    private reset() {
        window.clearTimeout(this.timer)
        this.controller?.abort()
        this.input.value = ''
        this.list.replaceChildren()
        this.input.setAttribute('aria-expanded', 'false')
        this.status.textContent = 'Type to search.'
        this.active = -1
    }

    private schedule() {
        window.clearTimeout(this.timer)
        const term = this.input.value.trim()
        if (!term) {
            this.list.replaceChildren()
            this.status.textContent = 'Type to search.'
            this.input.setAttribute('aria-expanded', 'false')
            return
        }
        this.timer = window.setTimeout(() => void this.run(term), debounceMs)
    }

    private async run(term: string) {
        this.controller?.abort()
        this.controller = new AbortController()
        this.status.textContent = 'Searching…'
        try {
            const results = await querySearch(term, this.controller.signal)
            if (this.input.value.trim() !== term) return
            this.render(results)
        } catch (error) {
            if ((error as Error).name === 'AbortError') return
            this.list.replaceChildren()
            this.status.textContent = 'Search is unavailable right now.'
        }
    }

    private render(results: SearchResult[]) {
        this.list.replaceChildren()
        this.active = -1
        this.input.setAttribute('aria-expanded', String(results.length > 0))
        this.status.textContent =
            results.length === 0
                ? 'No results.'
                : `${results.length} ${results.length === 1 ? 'result' : 'results'}.`

        results.forEach((result, index) => {
            const item = (
                this.template.content.cloneNode(true) as DocumentFragment
            ).querySelector<HTMLElement>('li')!
            item.id = `search-result-${index}`
            const link = item.querySelector<HTMLAnchorElement>('a')!
            link.href = result.url
            item.querySelector<HTMLElement>('[data-search-path]')!.textContent =
                result.parents.map((p) => p.title).join(' / ')
            item.querySelector<HTMLElement>(
                '[data-search-title]'
            )!.textContent = result.title
            appendExcerpt(
                item.querySelector<HTMLElement>('[data-search-excerpt]')!,
                result.description ?? ''
            )
            this.list.append(item)
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
        items[this.active].scrollIntoView?.({ block: 'nearest' })
    }

    private keydown(event: KeyboardEvent) {
        if (event.key === 'ArrowDown') {
            event.preventDefault()
            this.move(this.active + 1)
        } else if (event.key === 'ArrowUp') {
            event.preventDefault()
            this.move(this.active - 1)
        } else if (event.key === 'Enter') {
            const link =
                this.items()[this.active]?.querySelector<HTMLAnchorElement>('a')
            if (link) {
                event.preventDefault()
                link.click()
            }
        }
    }
}

// The dialog lives in the swapped page content, so a new page brings a new dialog element and needs a new controller.
let current: { element: HTMLDialogElement; controller: SearchDialog } | null =
    null

function isTyping(target: EventTarget | null): boolean {
    return (
        target instanceof HTMLElement &&
        (target.isContentEditable ||
            /^(input|textarea|select)$/i.test(target.tagName))
    )
}

function controllerFor(dialog: HTMLDialogElement): SearchDialog {
    if (current?.element !== dialog) {
        current = { element: dialog, controller: new SearchDialog(dialog) }
    }
    return current.controller
}

export function initSearch(scope: ParentNode = document) {
    const dialog = document.querySelector<HTMLDialogElement>(
        'dialog[data-search-dialog]'
    )
    if (!dialog || typeof dialog.showModal !== 'function') return

    const controller = controllerFor(dialog)
    scope
        .querySelectorAll<HTMLElement>(
            '[data-search-open]:not([data-search-bound])'
        )
        .forEach((trigger) => {
            trigger.dataset.searchBound = ''
            trigger.hidden = false
            trigger.addEventListener('click', () => controller.open())
        })
}

let shortcutBound = false
export function bindSearchShortcut() {
    if (shortcutBound) return
    shortcutBound = true
    document.addEventListener('keydown', (event) => {
        const opensWithModifier =
            (event.metaKey || event.ctrlKey) && event.key.toLowerCase() === 'k'
        const opensWithSlash = event.key === '/' && !isTyping(event.target)
        if (!opensWithModifier && !opensWithSlash) return

        const dialog = document.querySelector<HTMLDialogElement>(
            'dialog[data-search-dialog]'
        )
        if (!dialog || typeof dialog.showModal !== 'function') return

        event.preventDefault()
        controllerFor(dialog).open()
    })
}
