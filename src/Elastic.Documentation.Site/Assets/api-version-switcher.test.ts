import { syncApiVersionSwitcher } from './api-version-switcher'

const picker = (hrefs: string[]) =>
    `<div id="api-version-switcher">${hrefs
        .map((href) => `<a href="${href}">link</a>`)
        .join('')}</div>`

describe('api version switcher sync', () => {
    beforeEach(() => {
        document.body.innerHTML = picker(['/api/doc/es', '/api/doc/es/v8'])
    })

    it('takes the links of the page that was swapped in', () => {
        const changed = syncApiVersionSwitcher(
            `<html><body>${picker([
                '/api/doc/es/operation/operation-ping',
                '/api/doc/es/v8/operation/operation-ping',
            ])}</body></html>`
        )

        expect(changed).toBe(true)
        expect(
            [...document.querySelectorAll('a')].map((a) =>
                a.getAttribute('href')
            )
        ).toEqual([
            '/api/doc/es/operation/operation-ping',
            '/api/doc/es/v8/operation/operation-ping',
        ])
    })

    it('leaves the links alone when the response has no picker', () => {
        const changed = syncApiVersionSwitcher('<html><body></body></html>')

        expect(changed).toBe(false)
        expect(document.querySelectorAll('a')[1].getAttribute('href')).toBe(
            '/api/doc/es/v8'
        )
    })

    it('leaves the links alone when the picker has a different number of versions', () => {
        const changed = syncApiVersionSwitcher(
            `<html><body>${picker(['/api/doc/es'])}</body></html>`
        )

        expect(changed).toBe(false)
        expect(document.querySelectorAll('a')[1].getAttribute('href')).toBe(
            '/api/doc/es/v8'
        )
    })
})
