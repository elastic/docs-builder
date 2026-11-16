import { bannerMarkup, bindApiBetaBanner } from './api-beta-banner'
import { afterEach, beforeAll, describe, expect, it, vi } from 'vitest'

// Mirrors _ApiBetaBanner.cshtml, which renders the banner just before #main-container.
function banner(link: string): string {
    return `<aside id="api-beta-banner" class="api-beta-banner"><p><a href="https://example.test/report?link=${link}">let us know</a>.</p></aside>`
}

function apiPage(link: string): string {
    return `<!DOCTYPE html><html><body><header></header>${banner(link)}<div id="main-container"><div id="api-content-grid"></div></div></body></html>`
}

const docsPage =
    '<!DOCTYPE html><html><body><header></header><div id="main-container"><article></article></div></body></html>'

function navigate(response: string, detail: Record<string, unknown> = {}) {
    document.getElementById('main-container')?.dispatchEvent(
        new CustomEvent('htmx:afterSwap', {
            bubbles: true,
            detail: {
                boosted: true,
                successful: true,
                xhr: { response },
                ...detail,
            },
        })
    )
}

function reportLinks(): (string | null)[] {
    return [...document.querySelectorAll('#api-beta-banner a')].map((a) =>
        a.getAttribute('href')
    )
}

const changed = vi.fn()

describe('API beta banner', () => {
    beforeAll(() => {
        bindApiBetaBanner(changed)
        // jsdom has no modal dialogs.
        HTMLDialogElement.prototype.showModal = function (
            this: HTMLDialogElement
        ) {
            this.setAttribute('open', '')
        }
        HTMLDialogElement.prototype.close = function (this: HTMLDialogElement) {
            this.removeAttribute('open')
        }
    })

    afterEach(() => {
        document.body.innerHTML = ''
        changed.mockClear()
    })

    it('links the page reached by a move within the API', () => {
        document.body.innerHTML = `${banner('first')}<div id="main-container"></div>`

        navigate(apiPage('second'))

        expect(reportLinks()).toEqual([
            'https://example.test/report?link=second',
        ])
    })

    it('appears when a docs page swaps in an API page', () => {
        document.body.innerHTML = '<div id="main-container"></div>'

        navigate(apiPage('second'))

        expect(reportLinks()).toEqual([
            'https://example.test/report?link=second',
        ])
        expect(
            document.getElementById('api-beta-banner')?.nextElementSibling?.id
        ).toBe('main-container')
    })

    it('goes away when an API page swaps in a docs page', () => {
        document.body.innerHTML = `${banner('first')}<div id="main-container"></div>`

        navigate(docsPage)

        expect(document.getElementById('api-beta-banner')).toBeNull()
    })

    it('ignores a widget that loads a fragment without the banner', () => {
        document.body.innerHTML = `${banner('first')}<div id="main-container"></div>`

        navigate('<li>result</li>', { boosted: false })

        expect(reportLinks()).toEqual([
            'https://example.test/report?link=first',
        ])
    })

    it('ignores a failed navigation', () => {
        document.body.innerHTML = `${banner('first')}<div id="main-container"></div>`

        navigate(docsPage, { successful: false })

        expect(reportLinks()).toEqual([
            'https://example.test/report?link=first',
        ])
    })
})

// Mirrors _ApiBetaBanner.cshtml where page feedback runs: the link opens a dialog inside the banner.
function bannerWithDialog(link: string): string {
    return `<aside id="api-beta-banner" class="api-beta-banner"><p><a id="open" class="api-beta-banner__link" href="https://example.test/report?link=${link}" data-api-beta-feedback-open="api-beta-feedback">let us know</a></p><dialog id="api-beta-feedback" class="api-beta-feedback"><div class="api-beta-feedback__content"><button id="inside">Yes</button></div></dialog></aside>`
}

function click(
    selector: string,
    init: MouseEventInit = {}
): { prevented: boolean } {
    const target = document.querySelector(selector)!
    const notPrevented = target.dispatchEvent(
        new MouseEvent('click', {
            bubbles: true,
            cancelable: true,
            button: 0,
            ...init,
        })
    )
    return { prevented: !notPrevented }
}

const isOpen = () =>
    document.getElementById('api-beta-feedback')?.hasAttribute('open')

describe('API beta banner feedback dialog', () => {
    afterEach(() => {
        document.body.innerHTML = ''
        changed.mockClear()
    })

    it('opens the feedback dialog instead of following the link', () => {
        document.body.innerHTML = `${bannerWithDialog('first')}<div id="main-container"></div>`

        expect(click('#open').prevented).toBe(true)
        expect(isOpen()).toBe(true)
    })

    it.each([
        ['a modified click', { ctrlKey: true }],
        ['a click with the meta key', { metaKey: true }],
        ['a click with the shift key', { shiftKey: true }],
        ['a middle click', { button: 1 }],
    ])(
        'leaves %s to the browser, so the link opens in a new tab',
        (_, init) => {
            document.body.innerHTML = `${bannerWithDialog('first')}<div id="main-container"></div>`

            expect(click('#open', init).prevented).toBe(false)
            expect(isOpen()).toBe(false)
        }
    )

    it('follows the link where the banner has no dialog', () => {
        document.body.innerHTML = `${banner('first')}<div id="main-container"></div>`

        expect(click('#api-beta-banner a').prevented).toBe(false)
    })

    it('follows the link when the browser cannot show a modal dialog', () => {
        document.body.innerHTML = `${bannerWithDialog('first')}<div id="main-container"></div>`
        const showModal = HTMLDialogElement.prototype.showModal
        // @ts-expect-error an old browser has no showModal
        HTMLDialogElement.prototype.showModal = undefined
        try {
            expect(click('#open').prevented).toBe(false)
        } finally {
            HTMLDialogElement.prototype.showModal = showModal
        }
    })

    it('closes on a click on the backdrop, and stays open for a click inside', () => {
        document.body.innerHTML = `${bannerWithDialog('first')}<div id="main-container"></div>`
        click('#open')

        click('#inside')
        expect(isOpen()).toBe(true)

        click('#api-beta-feedback')
        expect(isOpen()).toBe(false)
    })

    it('opens the dialog of a banner that navigation put in the page', () => {
        document.body.innerHTML = `${banner('first')}<div id="main-container"></div>`

        navigate(
            apiPage('second').replace(
                banner('second'),
                bannerWithDialog('second')
            )
        )
        click('#open')

        expect(isOpen()).toBe(true)
        expect(reportLinks()).toEqual([
            'https://example.test/report?link=second',
        ])
    })

    it('tells the bundle about a banner that replaces or joins the page, not about a removed one', () => {
        document.body.innerHTML = `${banner('first')}<div id="main-container"></div>`

        navigate(apiPage('second'))
        expect(changed).toHaveBeenCalledTimes(1)
        expect(changed.mock.calls[0][0]).toBe(
            document.getElementById('api-beta-banner')
        )

        navigate(docsPage)
        navigate(apiPage('third'))
        expect(changed).toHaveBeenCalledTimes(2)
        expect(reportLinks()).toEqual([
            'https://example.test/report?link=third',
        ])

        navigate(docsPage)
        expect(changed).toHaveBeenCalledTimes(2)
    })
})

describe('bannerMarkup', () => {
    it('cuts the banner out of a page', () => {
        expect(bannerMarkup(apiPage('x'))).toBe(banner('x'))
    })

    it('finds nothing on a docs page', () => {
        expect(bannerMarkup(docsPage)).toBeNull()
    })
})
