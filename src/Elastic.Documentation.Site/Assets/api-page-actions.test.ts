import { initApiPageActions } from './api-docs'

function pageActionsMarkup(url: string): string {
    return `
        <div class="api-page-actions">
            <div class="api-page-actions-split">
                <button type="button" class="api-page-actions-button" data-copy-page="${url}">
                    <span class="api-page-actions-label">Copy page</span>
                </button>
                <details class="api-page-actions-dropdown nav-select-dropdown" open>
                    <summary class="api-page-actions-chevron">More</summary>
                    <div class="api-page-actions-menu">
                        <button type="button" class="api-page-actions-option" data-copy-page="${url}">
                            Copy page
                        </button>
                    </div>
                </details>
            </div>
        </div>
    `
}

function flushMicrotasks(times = 6): Promise<void> {
    let chain = Promise.resolve()
    for (let i = 0; i < times; i++) {
        chain = chain.then(() => undefined)
    }
    return chain
}

describe('initApiPageActions', () => {
    const markdown = '# Search\n'
    const writeText = jest.fn().mockResolvedValue(undefined)

    beforeEach(() => {
        jest.useFakeTimers()
        writeText.mockClear()
        Object.assign(navigator, {
            clipboard: { writeText },
        })
        global.fetch = jest.fn().mockResolvedValue({
            ok: true,
            text: async () => markdown,
        }) as unknown as typeof fetch
        document.body.innerHTML = pageActionsMarkup('/page.md')
        initApiPageActions()
    })

    afterEach(() => {
        jest.useRealTimers()
        document.body.innerHTML = ''
    })

    it('copies fetched markdown and shows Copied! on the action button', async () => {
        document
            .querySelector<HTMLButtonElement>('.api-page-actions-button')!
            .click()

        await flushMicrotasks()

        expect(global.fetch).toHaveBeenCalledWith('/page.md')
        expect(writeText).toHaveBeenCalledWith(markdown)
        expect(
            document
                .querySelector('.api-page-actions-label')
                ?.textContent?.trim()
        ).toBe('Copied!')

        jest.advanceTimersByTime(1500)
        expect(
            document
                .querySelector('.api-page-actions-label')
                ?.textContent?.trim()
        ).toBe('Copy page')
    })

    it('closes the dropdown when copying from the menu', async () => {
        const dropdown = document.querySelector<HTMLDetailsElement>(
            '.api-page-actions-dropdown'
        )!
        expect(dropdown.open).toBe(true)

        document
            .querySelector<HTMLButtonElement>('.api-page-actions-option')!
            .click()
        await flushMicrotasks()

        expect(dropdown.open).toBe(false)
        expect(writeText).toHaveBeenCalledWith(markdown)
    })
})
