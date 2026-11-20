import { changesApiTree, changesPageChrome } from './api-navigation'

const root = '/docs'

describe('changesPageChrome', () => {
    it('reloads between a docs page and an API page, in both directions', () => {
        expect(
            changesPageChrome(
                '/docs/api/elasticsearch',
                '/docs/deploy-manage/',
                root
            )
        ).toBe(true)
        expect(
            changesPageChrome(
                '/docs/get-started/the-stack',
                '/docs/api/elasticsearch/group/search',
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

    it('stays boosted between API products', () => {
        expect(
            changesPageChrome(
                '/docs/api/kibana-serverless/operation/get-actions',
                '/docs/api/elasticsearch/group/endpoint-analytics',
                root
            )
        ).toBe(false)
    })

    it('stays boosted within one API product, across versions too', () => {
        expect(
            changesPageChrome(
                '/docs/api/elasticsearch/operation/search',
                '/docs/api/elasticsearch/group/endpoint-analytics',
                root
            )
        ).toBe(false)
        expect(
            changesPageChrome(
                '/docs/api/elasticsearch/v8/group/search',
                '/docs/api/elasticsearch/group/search',
                root
            )
        ).toBe(false)
    })

    it('stays boosted between the catalog and a product', () => {
        expect(
            changesPageChrome('/docs/api/elasticsearch', '/docs/api', root)
        ).toBe(false)
        expect(
            changesPageChrome(
                '/docs/api',
                '/docs/api/elasticsearch/group/search',
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
                '/docs/api/elasticsearch',
                root
            )
        ).toBe(true)
    })

    it('works without a root path, as in isolated builds', () => {
        expect(changesPageChrome('/api/kibana', '/get-started/', '')).toBe(true)
        expect(changesPageChrome('/api/kibana', '/api/elasticsearch', '')).toBe(
            false
        )
    })
})

describe('changesApiTree', () => {
    const latest = '/api/elasticsearch/operation/operation-ping'
    const v9 = '/api/elasticsearch/v9/operation/operation-ping'
    const v8 = '/api/elasticsearch/v8'
    const next = '/api/elasticsearch/next/operation/operation-ping'

    it('is true between the unversioned path and a released version, in both directions', () => {
        expect(changesApiTree(v8, latest)).toBe(true)
        expect(changesApiTree(latest, v8)).toBe(true)
    })

    it('is true between the unversioned path and the numbered copy of the same major', () => {
        expect(changesApiTree(v9, latest)).toBe(true)
    })

    it('is true between two API products', () => {
        expect(
            changesApiTree('/api/kibana/operation/get-actions', latest)
        ).toBe(true)
    })

    it('is true between next and the unversioned path', () => {
        expect(changesApiTree(next, latest)).toBe(true)
        expect(changesApiTree(latest, next)).toBe(true)
    })

    it('is true between two released versions', () => {
        expect(changesApiTree(v8, v9)).toBe(true)
    })

    it('is false within one version', () => {
        expect(
            changesApiTree('/api/elasticsearch/v9/group/endpoint-search', v9)
        ).toBe(false)
        expect(
            changesApiTree('/api/elasticsearch/group/endpoint-search', latest)
        ).toBe(false)
        expect(
            changesApiTree(
                '/api/elasticsearch/next/group/a',
                '/api/elasticsearch/next/operation/b'
            )
        ).toBe(false)
    })

    it('is false when either path is not an API product page', () => {
        expect(changesApiTree('/docs/get-started', latest)).toBe(false)
        expect(changesApiTree(latest, '/docs/api')).toBe(false)
    })
})
