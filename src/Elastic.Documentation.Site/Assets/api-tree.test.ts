import { apiTreeSearchParams, parseApiTree } from './api-tree'

describe('parseApiTree', () => {
    it('reads a released version', () => {
        expect(
            parseApiTree('/docs/api/doc/elasticsearch/v8/operation/x')
        ).toEqual({ product: 'elasticsearch', tree: '8' })
    })

    it('reads the main tree', () => {
        expect(
            parseApiTree('/docs/api/doc/elasticsearch/main/group/x')
        ).toEqual({ product: 'elasticsearch', tree: 'main' })
    })

    it('has no tree on the unversioned path', () => {
        expect(parseApiTree('/docs/api/doc/elasticsearch/operation/x')).toEqual(
            { product: 'elasticsearch', tree: null }
        )
    })

    it('is null outside an API product', () => {
        expect(parseApiTree('/docs/get-started')).toBeNull()
        expect(parseApiTree('/docs/api')).toBeNull()
    })

    it('does not take an operation segment for a tree', () => {
        expect(parseApiTree('/docs/api/doc/kibana/v/operation/x')).toEqual({
            product: 'kibana',
            tree: null,
        })
        expect(parseApiTree('/docs/api/doc/kibana/maintenance/x')).toEqual({
            product: 'kibana',
            tree: null,
        })
    })
})

describe('apiTreeSearchParams', () => {
    it('scopes a versioned path', () => {
        expect(apiTreeSearchParams('/docs/api/doc/elasticsearch/v8')).toEqual({
            api_key: 'elasticsearch',
            api_tree: '8',
        })
        expect(
            apiTreeSearchParams('/docs/api/doc/elasticsearch/main/group/x')
        ).toEqual({ api_key: 'elasticsearch', api_tree: 'main' })
    })

    it('sends nothing elsewhere', () => {
        expect(
            apiTreeSearchParams('/docs/api/doc/elasticsearch/operation/x')
        ).toEqual({})
        expect(apiTreeSearchParams('/docs/get-started')).toEqual({})
    })
})
