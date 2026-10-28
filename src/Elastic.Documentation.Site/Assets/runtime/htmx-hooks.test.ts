import { bindHtmxHooks } from './htmx-hooks'

// The real strategy depends on the build type; here only /docs/* paths belong to the docs.
vi.mock('../web-components/shared/htmx/utils', () => ({
    getPathFromUrl: (url: string) => (url.startsWith('/docs') ? url : null),
    isExternalDocsUrl: () => false,
}))

const assign = vi.fn()

function beforeRequest(
    path: string,
    detail: Record<string, unknown> = {}
): CustomEvent {
    const event = new CustomEvent('htmx:beforeRequest', {
        cancelable: true,
        detail: {
            boosted: true,
            requestConfig: { verb: 'get', path, headers: {} },
            ...detail,
        },
    })
    document.dispatchEvent(event)
    return event
}

describe('bindHtmxHooks', () => {
    beforeAll(() =>
        bindHtmxHooks({
            forcesFullPageLoad: (targetPath) =>
                targetPath.startsWith('/docs/forced'),
        })
    )

    beforeEach(() => {
        assign.mockClear()
        vi.stubGlobal('location', {
            href: 'http://localhost/docs/a',
            pathname: '/docs/a',
            assign,
        })
    })

    afterEach(() => vi.unstubAllGlobals())

    it('keeps <style> and Storybook assets in the head', () => {
        const remove = (headElement: object) => {
            const event = new CustomEvent('htmx:removingHeadElement', {
                cancelable: true,
                detail: { headElement },
            })
            document.dispatchEvent(event)
            return event.defaultPrevented
        }

        expect(remove({ tagName: 'STYLE' })).toBe(true)
        expect(
            remove({ tagName: 'SCRIPT', dataset: { storybookScript: '' } })
        ).toBe(true)
        expect(remove({ tagName: 'LINK', dataset: {} })).toBe(false)
    })

    it('sends a link outside the docs to a full page load', () => {
        const event = beforeRequest('/pricing')

        expect(event.defaultPrevented).toBe(true)
        expect(assign).toHaveBeenCalledWith('/pricing')
    })

    it('sends a link to a full page load when the bundle asks for it', () => {
        const event = beforeRequest('/docs/forced/page')

        expect(event.defaultPrevented).toBe(true)
        expect(assign).toHaveBeenCalledWith('/docs/forced/page')
    })

    it('lets an internal docs link navigate through htmx', () => {
        const event = beforeRequest('/docs/b')

        expect(event.defaultPrevented).toBe(false)
        expect(assign).not.toHaveBeenCalled()
    })

    it.each([
        [
            'a prefetch',
            {
                requestConfig: {
                    verb: 'get',
                    path: '/pricing',
                    headers: { 'HX-Preloaded': 'true' },
                },
            },
        ],
        ['a non-boosted request', { boosted: false }],
        [
            'a non-GET request',
            { requestConfig: { verb: 'post', path: '/pricing', headers: {} } },
        ],
    ])('does not intercept %s', (_name, detail) => {
        const event = beforeRequest('/pricing', detail)

        expect(event.defaultPrevented).toBe(false)
    })

    it('opens the link in a new tab on shift-click', () => {
        const open = vi.fn()
        vi.stubGlobal('open', open)

        const event = beforeRequest('/docs/b', {
            requestConfig: {
                verb: 'get',
                path: '/docs/b',
                headers: {},
                triggeringEvent: { shiftKey: true },
            },
        })

        expect(event.defaultPrevented).toBe(true)
        expect(open).toHaveBeenCalledWith(
            '/docs/b',
            '_blank',
            'noopener,noreferrer'
        )
    })

    it('opens a 404 response as a normal page load', () => {
        document.body.dispatchEvent(
            new CustomEvent('htmx:responseError', {
                detail: {
                    xhr: { status: 404 },
                    pathInfo: { requestPath: '/docs/missing' },
                },
            })
        )

        expect(assign).toHaveBeenCalledWith('/docs/missing')
    })
})
