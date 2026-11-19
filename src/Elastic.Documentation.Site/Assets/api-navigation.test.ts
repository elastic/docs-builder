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
    const main = '/api/elasticsearch/operation/operation-ping'
    const v9 = '/api/elasticsearch/v9/operation/operation-ping'
    const v8 = '/api/elasticsearch/v8'

    it('is true between main and a released version, in both directions', () => {
        expect(changesApiTree(v9, main)).toBe(true)
        expect(changesApiTree(main, v9)).toBe(true)
    })

    it('is true between two API products', () => {
        expect(changesApiTree('/api/kibana/operation/get-actions', main)).toBe(
            true
        )
    })

    it('is true between main and the unversioned path', () => {
        expect(
            changesApiTree(
                '/api/elasticsearch/main/group/endpoint-search',
                main
            )
        ).toBe(true)
        expect(
            changesApiTree(
                '/api/elasticsearch/main/group/a',
                '/api/elasticsearch/main/operation/b'
            )
        ).toBe(false)
    })

    it('is true between two released versions', () => {
        expect(changesApiTree(v8, v9)).toBe(true)
    })

    it('is false within one version', () => {
        expect(
            changesApiTree('/api/elasticsearch/v9/group/endpoint-search', v9)
        ).toBe(false)
        expect(
            changesApiTree('/api/elasticsearch/group/endpoint-search', main)
        ).toBe(false)
    })

    it('is false when either path is not an API product page', () => {
        expect(changesApiTree('/docs/get-started', main)).toBe(false)
        expect(changesApiTree(main, '/docs/api')).toBe(false)
    })
})
