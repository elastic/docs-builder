import { renderWithProviders } from '../../test-utils/render'
import { RouteLine, splitRoutePath } from './RouteLine'

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
        expect(first + middle + last).toBe(path)
    })
})

describe('RouteLine highlighting', () => {
    const marks = (path: string, tokens: string[]) => {
        const { container } = renderWithProviders(
            <RouteLine route={{ path, methods: ['GET'] }} tokens={tokens} />
        )
        return [...container.querySelectorAll('mark')].map((m) => m.textContent)
    }

    it('keeps a match that crosses a split boundary', () => {
        expect(marks('/api/fleet/agents', ['/api/fleet'])).toEqual([
            '/api',
            '/fleet',
        ])
    })

    it('marks every case-insensitive match once', () => {
        expect(
            marks(
                '/v1/monitoring/{resourceType}/{resourceId}/insights/{insightId}',
                ['id', 'insights']
            )
        ).toEqual(['Id', 'insights', 'Id'])
    })

    it('merges overlapping tokens into one mark', () => {
        expect(marks('/_search', ['_sea', 'search'])).toEqual(['_search'])
    })
})

describe('RouteLine methods', () => {
    const chips = (methods: string[]) => {
        const { container } = renderWithProviders(
            <RouteLine route={{ path: '/_search', methods }} tokens={[]} />
        )
        return {
            labels: [...container.querySelectorAll('.api-method-label')].map(
                (l) => l.textContent
            ),
            joined: container.querySelector('.api-method-group') !== null,
        }
    }

    it('shows one method as a lone chip', () => {
        expect(chips(['GET'])).toEqual({ labels: ['GET'], joined: false })
    })

    it('joins every method into one chip, the dominant method first', () => {
        expect(chips(['POST', 'GET'])).toEqual({
            labels: ['POST', 'GET'],
            joined: true,
        })
    })
})
