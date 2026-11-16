import { bannerMarkup, bindApiBetaBanner } from './api-beta-banner'
import { afterEach, beforeAll, describe, expect, it } from 'vitest'

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

describe('API beta banner', () => {
    beforeAll(() => {
        bindApiBetaBanner()
    })

    afterEach(() => {
        document.body.innerHTML = ''
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

describe('bannerMarkup', () => {
    it('cuts the banner out of a page', () => {
        expect(bannerMarkup(apiPage('x'))).toBe(banner('x'))
    })

    it('finds nothing on a docs page', () => {
        expect(bannerMarkup(docsPage)).toBeNull()
    })
})
