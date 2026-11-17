import { splitRoutePath } from './RouteLine'
import { describe, expect, it } from 'vitest'

describe('splitRoutePath', () => {
    it.each([
        [
            '/v1/monitoring/{resourceType}/{resourceId}/insights/{insightId}',
            '/v1',
            '/monitoring/{resourceType}/{resourceId}',
            '/insights/{insightId}',
        ],
        ['/{index}/_update_by_query', '/{index}', '', '/_update_by_query'],
        ['/_search', '', '', '/_search'],
        ['/{index}', '', '', '/{index}'],
    ])('splits %s', (path, first, middle, last) => {
        expect(splitRoutePath(path)).toEqual({ first, middle, last })
    })
})
