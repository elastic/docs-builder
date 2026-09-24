import { parseApiVersionScope } from './parseApiVersionScope'

describe('parseApiVersionScope', () => {
    it.each(['8.5', '8', 'v8', '8.x'])(
        'treats %s as a v8 filter and strips it from the query',
        (token) => {
            expect(
                parseApiVersionScope(`_async_search ${token}`, '/docs/guide')
            ).toEqual({ query: '_async_search', apiVersion: 'v8' })
        }
    )

    it('keeps three-digit numbers such as status codes in the query', () => {
        expect(parseApiVersionScope('status 404', '/docs/guide')).toEqual({
            query: 'status 404',
            apiVersion: 'latest',
        })
    })

    it('leaves an empty query when only a version token is typed', () => {
        expect(parseApiVersionScope('8', '/docs/guide')).toEqual({
            query: '',
            apiVersion: 'v8',
        })
    })

    it('uses the current /v8/ page when the query has no version token', () => {
        expect(
            parseApiVersionScope(
                '_async_search',
                '/docs/api/doc/elasticsearch/v8/operation/operation-_async-search'
            )
        ).toEqual({ query: '_async_search', apiVersion: 'v8' })
    })

    it('uses latest on an unversioned API page', () => {
        expect(
            parseApiVersionScope(
                '_async_search',
                '/docs/api/doc/elasticsearch/operation/operation-_async-search'
            )
        ).toEqual({ query: '_async_search', apiVersion: 'latest' })
    })

    it('uses latest on a non-API page', () => {
        expect(
            parseApiVersionScope('_async_search', '/docs/elasticsearch/guide')
        ).toEqual({ query: '_async_search', apiVersion: 'latest' })
    })
})
