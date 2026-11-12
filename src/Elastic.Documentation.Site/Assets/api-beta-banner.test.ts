import type Htmx from 'htmx.org'
import { afterEach, beforeAll, describe, expect, it, vi } from 'vitest'

// setupTests.ts mocks htmx.org; these tests need the real swap, out-of-band handling included.
// jsdom's XPath cannot run htmx's hx-on query, and no fixture here uses hx-on, so the query finds nothing.
vi.stubGlobal(
    'XPathEvaluator',
    class {
        createExpression() {
            return { evaluate: () => ({ iterateNext: () => null }) }
        }
    }
)
let htmx: typeof Htmx

beforeAll(async () => {
    htmx = (await vi.importActual<{ default: typeof Htmx }>('htmx.org')).default
})

const outerHtml = {
    swapStyle: 'outerHTML',
    swapDelay: 0,
    settleDelay: 0,
} as const

// Mirrors _ApiBetaBanner.cshtml: the banner sits outside #main-container and replaces itself out of band.
function banner(link: string): string {
    return `<aside id="api-beta-banner" hx-swap-oob="true"><a href="https://example.test/report?link=${link}">let us know</a></aside>`
}

function page(link: string, article: string): string {
    return `${banner(link)}<div id="main-container"><div id="api-content-grid"><article>${article}</article></div></div>`
}

function reportLink(): string | null {
    return (
        document.querySelector('#api-beta-banner a')?.getAttribute('href') ??
        null
    )
}

describe('API beta banner', () => {
    afterEach(() => {
        document.body.innerHTML = ''
    })

    it('reports the page reached by a swap of the content grid alone', () => {
        document.body.innerHTML = page('first', 'First')

        // pages-nav.ts retargets a move within one API to #api-content-grid and selects only that element.
        htmx.swap('#api-content-grid', page('second', 'Second'), outerHtml, {
            select: '#api-content-grid',
        })

        expect(document.querySelector('article')?.textContent).toBe('Second')
        expect(reportLink()).toBe('https://example.test/report?link=second')
        expect(document.querySelectorAll('#api-beta-banner')).toHaveLength(1)
    })

    it('reports the page reached by a swap of the whole main container', () => {
        document.body.innerHTML = page('first', 'First')

        htmx.swap('#main-container', page('second', 'Second'), outerHtml, {
            select: '#main-container',
        })

        expect(reportLink()).toBe('https://example.test/report?link=second')
        expect(document.querySelectorAll('#api-beta-banner')).toHaveLength(1)
    })
})
