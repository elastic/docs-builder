import { apiPageSearchParams, parseApiTree } from './api-tree'

describe('parseApiTree', () => {
    it('reads a released version', () => {
        expect(
            parseApiTree('/docs/api/elasticsearch/v8/operation/x', '/docs')
        ).toEqual({ product: 'elasticsearch', tree: '8' })
    })

    it('reads the next segment as no tree, because next is not indexed', () => {
        expect(
            parseApiTree('/docs/api/elasticsearch/next/group/x', '/docs')
        ).toEqual({ product: 'elasticsearch', tree: null })
    })

    it('takes a main segment for a page, not a tree', () => {
        expect(
            parseApiTree('/docs/api/elasticsearch/main/group/x', '/docs')
        ).toEqual({ product: 'elasticsearch', tree: null })
    })

    it('has no tree on the unversioned path', () => {
        expect(
            parseApiTree('/docs/api/elasticsearch/operation/x', '/docs')
        ).toEqual({ product: 'elasticsearch', tree: null })
    })

    it('reads only paths under the site root', () => {
        expect(
            parseApiTree('/docs/reference/apm/api/agent', '/docs')
        ).toBeNull()
        expect(parseApiTree('/api/elasticsearch/v9', '')).toEqual({
            product: 'elasticsearch',
            tree: '9',
        })
    })

    it('is null outside an API product', () => {
        expect(parseApiTree('/docs/get-started', '/docs')).toBeNull()
        expect(parseApiTree('/docs/api', '/docs')).toBeNull()
    })

    it('does not take an operation segment for a tree', () => {
        expect(parseApiTree('/docs/api/kibana/v/operation/x', '/docs')).toEqual(
            {
                product: 'kibana',
                tree: null,
            }
        )
        expect(parseApiTree('/docs/api/kibana/maintenance/x', '/docs')).toEqual(
            {
                product: 'kibana',
                tree: null,
            }
        )
    })
})

describe('apiPageSearchParams', () => {
    it('scopes a versioned path', () => {
        expect(
            apiPageSearchParams('/docs/api/elasticsearch/v8', '/docs')
        ).toEqual({
            api_key: 'elasticsearch',
            api_tree: '8',
        })
    })

    it('sends the key alone on the next path', () => {
        expect(
            apiPageSearchParams('/docs/api/elasticsearch/next/group/x', '/docs')
        ).toEqual({ api_key: 'elasticsearch' })
    })

    it('sends the key alone on the unversioned path', () => {
        expect(
            apiPageSearchParams('/docs/api/elasticsearch/operation/x', '/docs')
        ).toEqual({ api_key: 'elasticsearch' })
    })

    it('sends nothing outside an API product', () => {
        expect(apiPageSearchParams('/docs/get-started', '/docs')).toEqual({})
        expect(apiPageSearchParams('/docs/api/', '/docs')).toEqual({})
    })
})
