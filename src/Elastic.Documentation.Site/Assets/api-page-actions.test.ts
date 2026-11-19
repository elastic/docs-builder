import { initApiPageActions } from './api-docs'

function pageActionsMarkup(url: string): string {
    return `
        <div class="api-page-actions">
            <div class="api-page-actions-split">
                <button type="button" class="api-page-actions-button" data-copy-page="${url}">
                    <span class="api-page-actions-label">Copy page</span>
                </button>
                <button type="button" class="api-page-actions-chevron" popovertarget="menu">More</button>
            </div>
            <div id="menu" class="api-page-actions-menu" popover>
                <button type="button" class="api-page-actions-option" data-copy-page="${url}">
                    Copy page
                </button>
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
    const writeText = vi.fn().mockResolvedValue(undefined)

    beforeEach(() => {
        vi.useFakeTimers()
        writeText.mockClear()
        Object.assign(navigator, {
            clipboard: { writeText },
        })
        global.fetch = vi.fn().mockResolvedValue({
            ok: true,
            text: async () => markdown,
        }) as unknown as typeof fetch
        document.body.innerHTML = pageActionsMarkup('/page.md')
        initApiPageActions()
    })

    afterEach(() => {
        vi.useRealTimers()
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

        vi.advanceTimersByTime(1500)
        expect(
            document
                .querySelector('.api-page-actions-label')
                ?.textContent?.trim()
        ).toBe('Copy page')
    })

    it('closes the menu when copying from it', async () => {
        const menu = document.getElementById('menu')!
        // jsdom has no popover API: the menu stands open, as the browser would show it.
        menu.hidePopover = vi.fn()
        vi.spyOn(menu, 'matches').mockImplementation(
            (selector) => selector === ':popover-open'
        )

        document
            .querySelector<HTMLButtonElement>('.api-page-actions-option')!
            .click()
        await flushMicrotasks()

        expect(menu.hidePopover).toHaveBeenCalledTimes(1)
        expect(writeText).toHaveBeenCalledWith(markdown)
        expect(
            document
                .querySelector('.api-page-actions-label')
                ?.textContent?.trim()
        ).toBe('Copied!')
    })
})
