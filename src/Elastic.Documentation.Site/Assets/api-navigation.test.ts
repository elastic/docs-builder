import { changesPageChrome } from './api-navigation'

const root = '/docs'

describe('changesPageChrome', () => {
    it('reloads when the target is a different API product', () => {
        expect(
            changesPageChrome(
                '/docs/api/doc/kibana-serverless/operation/get-actions',
                '/docs/api/doc/elasticsearch/group/endpoint-analytics',
                root
            )
        ).toBe(true)
    })

    it('reloads between a docs page and an API page, in both directions', () => {
        expect(
            changesPageChrome(
                '/docs/api/doc/elasticsearch',
                '/docs/deploy-manage/',
                root
            )
        ).toBe(true)
        expect(
            changesPageChrome(
                '/docs/get-started/the-stack',
                '/docs/api/doc/elasticsearch/group/search',
                root
            )
        ).toBe(true)
        expect(changesPageChrome('/docs/api', '/docs/', root)).toBe(true)
    })

    it('stays boosted between two docs pages', () => {
        expect(
            changesPageChrome(
                '/docs/get-started/the-stack',
                '/docs/deploy-manage/',
                root
            )
        ).toBe(false)
    })

    it('stays boosted within one API product, across versions too', () => {
        expect(
            changesPageChrome(
                '/docs/api/doc/elasticsearch/operation/search',
                '/docs/api/doc/elasticsearch/group/endpoint-analytics',
                root
            )
        ).toBe(false)
        expect(
            changesPageChrome(
                '/docs/api/doc/elasticsearch/v8/group/search',
                '/docs/api/doc/elasticsearch/group/search',
                root
            )
        ).toBe(false)
    })

    it('stays boosted between the catalog and a product', () => {
        expect(
            changesPageChrome('/docs/api/doc/elasticsearch', '/docs/api', root)
        ).toBe(false)
        expect(
            changesPageChrome(
                '/docs/api',
                '/docs/api/doc/elasticsearch/group/search',
                root
            )
        ).toBe(false)
    })

    it('treats a path that only starts with api as a docs page', () => {
        expect(
            changesPageChrome(
                '/docs/api-keys/doc/x',
                '/docs/deploy-manage/',
                root
            )
        ).toBe(false)
        expect(
            changesPageChrome(
                '/docs/api-keys/doc/x',
                '/docs/api/doc/elasticsearch',
                root
            )
        ).toBe(true)
    })

    it('works without a root path, as in isolated builds', () => {
        expect(
            changesPageChrome('/api/doc/kibana', '/api/doc/elasticsearch', '')
        ).toBe(true)
    })
})
