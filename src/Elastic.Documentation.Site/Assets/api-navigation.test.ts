import { changesApiVersion, changesPageChrome } from './api-navigation'

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

describe('changesApiVersion', () => {
    const main = '/docs/api/doc/elasticsearch/operation/operation-ping'
    const v9 = '/docs/api/doc/elasticsearch/v9/operation/operation-ping'
    const v8 = '/docs/api/doc/elasticsearch/v8'

    it('is true between main and a released version, in both directions', () => {
        expect(changesApiVersion(v9, main)).toBe(true)
        expect(changesApiVersion(main, v9)).toBe(true)
    })

    it('is true between two released versions', () => {
        expect(changesApiVersion(v8, v9)).toBe(true)
    })

    it('is false within one version', () => {
        expect(
            changesApiVersion(
                '/docs/api/doc/elasticsearch/v9/group/endpoint-search',
                v9
            )
        ).toBe(false)
        expect(
            changesApiVersion(
                '/docs/api/doc/elasticsearch/group/endpoint-search',
                main
            )
        ).toBe(false)
    })

    it('is false when either path is not an API product page', () => {
        expect(changesApiVersion('/docs/get-started', main)).toBe(false)
        expect(changesApiVersion(main, '/docs/api')).toBe(false)
    })
})
