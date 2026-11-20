import {
    syncApiVersionSwitcher,
    syncIsolatedHeaderTitle,
} from './api-chrome-sync'
import htmx from 'htmx.org'

const picker = (hrefs: string[]) =>
    `<div id="api-version-switcher">${hrefs
        .map((href) => `<a href="${href}">link</a>`)
        .join('')}</div>`

const hrefs = () =>
    [...document.querySelectorAll('#api-version-switcher a')].map((a) =>
        a.getAttribute('href')
    )

// htmx reads a boosted link's href once, when it processes the link. Rewriting the attribute leaves the link
// requesting the page it first pointed at, so the synced picker has to be a new element that htmx processes.
// htmx is mocked in this suite (jsdom cannot run it), so the click itself is covered by the browser journey.
describe('api version switcher sync', () => {
    beforeEach(() => {
        vi.mocked(htmx.process).mockClear()
        document.body.innerHTML = picker(['/api/es', '/api/es/v8'])
    })

    it('replaces the picker with the one of the page that was swapped in and has htmx process it', () => {
        const before = document.getElementById('api-version-switcher')
        // A detached picker would miss the `hx-boost` it inherits from `<body>`, so htmx must see it in the live page.
        let processedInLivePage = false
        vi.mocked(htmx.process).mockImplementationOnce((element) => {
            processedInLivePage =
                element instanceof Element && document.contains(element)
        })

        const changed = syncApiVersionSwitcher(
            `<html><body>${picker([
                '/api/es/operation/operation-ping',
                '/api/es/v8/operation/operation-ping',
            ])}</body></html>`
        )

        const after = document.getElementById('api-version-switcher')
        expect(changed).toBe(true)
        expect(hrefs()).toEqual([
            '/api/es/operation/operation-ping',
            '/api/es/v8/operation/operation-ping',
        ])
        expect(after).not.toBe(before)
        expect(htmx.process).toHaveBeenCalledExactlyOnceWith(after)
        expect(processedInLivePage).toBe(true)
    })

    it('leaves the picker alone when the response has no picker', () => {
        const before = document.getElementById('api-version-switcher')

        const changed = syncApiVersionSwitcher('<html><body></body></html>')

        expect(changed).toBe(false)
        expect(document.getElementById('api-version-switcher')).toBe(before)
        expect(htmx.process).not.toHaveBeenCalled()
    })

    it('takes the picker of the response when it lists a different number of versions', () => {
        const changed = syncApiVersionSwitcher(
            `<html><body>${picker(['/api/es/operation/operation-ping'])}</body></html>`
        )

        expect(changed).toBe(true)
        expect(hrefs()).toEqual(['/api/es/operation/operation-ping'])
    })
})

describe('isolated header title sync', () => {
    const header = (title: string) =>
        `<div id="isolated-header"><elastic-docs-header title="${title}"></elastic-docs-header></div>`

    beforeEach(() => {
        document.body.innerHTML = header('Elasticsearch API')
    })

    it('takes the title of the API that was swapped in', () => {
        const changed = syncIsolatedHeaderTitle(
            `<html><body>${header('Kibana API')}</body></html>`
        )

        expect(changed).toBe(true)
        expect(
            document.querySelector('elastic-docs-header')?.getAttribute('title')
        ).toBe('Kibana API')
    })

    it('leaves the title alone when the response has no header', () => {
        const changed = syncIsolatedHeaderTitle('<html><body></body></html>')

        expect(changed).toBe(false)
        expect(
            document.querySelector('elastic-docs-header')?.getAttribute('title')
        ).toBe('Elasticsearch API')
    })
})
