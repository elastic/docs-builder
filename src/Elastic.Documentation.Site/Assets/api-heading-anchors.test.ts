import { initHeadingAnchors, urlWithHash } from './api-docs'

const pagePath = '/docs/api/doc/elasticsearch/operation/operation-create'

describe('heading anchors', () => {
    const writeText = jest.fn().mockResolvedValue(undefined)

    beforeEach(() => {
        writeText.mockClear()
        Object.assign(navigator, {
            clipboard: { writeText },
        })
        window.history.pushState(null, '', pagePath)
        document.body.innerHTML = `
            <h4 id="request-body" data-section="request-body">
                <button type="button" class="api-param-section-toggle">
                    <a data-copy-heading="#request-body" href="#request-body">Request link</a>
                    Request
                </button>
            </h4>
            <h4 id="responses" data-section="responses">
                <a data-copy-heading="#responses" href="#responses">Response link</a>
            </h4>
            <h4 id="parameters" data-section="parameters">Parameters</h4>
            <a class="property-anchor" href="#path-index">
                <span class="property-anchor-icon"></span>
                <code>index</code>
            </a>
        `
        initHeadingAnchors()
        document.addEventListener('click', (event) => {
            const toggle = (event.target as Element | null)?.closest?.(
                '.api-param-section-toggle'
            )
            if (toggle) toggle.setAttribute('data-toggled', 'true')
        })
    })

    afterEach(() => {
        document.body.innerHTML = ''
    })

    it('builds a page url that keeps the heading hash', () => {
        expect(
            urlWithHash(
                'https://www.elastic.co/docs/api/doc/elasticsearch/operation/operation-create',
                '#request-body'
            )
        ).toBe(
            'https://www.elastic.co/docs/api/doc/elasticsearch/operation/operation-create#request-body'
        )
    })

    it('copies the request and response urls when the link icon is activated', () => {
        document
            .querySelector<HTMLAnchorElement>('a[href="#request-body"]')!
            .click()
        document
            .querySelector<HTMLAnchorElement>('a[href="#responses"]')!
            .click()

        expect(writeText).toHaveBeenNthCalledWith(
            1,
            `http://localhost${pagePath}#request-body`
        )
        expect(writeText).toHaveBeenNthCalledWith(
            2,
            `http://localhost${pagePath}#responses`
        )
        expect(document.getElementById('parameters')?.id).toBe('parameters')
        expect(
            document
                .querySelector('.api-param-section-toggle')
                ?.getAttribute('data-toggled')
        ).toBeNull()
    })

    it('copies the property link icon url and leaves the parameters anchor', () => {
        document.querySelector<HTMLElement>('.property-anchor-icon')!.click()

        expect(writeText).toHaveBeenCalledWith(
            `http://localhost${pagePath}#path-index`
        )
        expect(document.getElementById('parameters')).not.toBeNull()

        writeText.mockClear()
        document
            .querySelector('a.property-anchor code')!
            .dispatchEvent(new MouseEvent('click', { bubbles: true }))
        expect(writeText).not.toHaveBeenCalled()
    })
})
