import { logInfo } from '../../telemetry/logging'
import { ATTR_NAVIGATION_SEARCH_RESULT_ANCHOR } from '../../telemetry/semconv'
import { sharedQueryClient } from '../shared/queryClient'
import { NavigationSearchWrapper } from './NavigationSearchComponent'
import { navigationSearchStore } from './navigationSearch.store'
import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'

vi.mock('../../telemetry/logging', async () =>
    (await import('../../test-utils/mocks')).logging()
)

const emptySearch = {
    results: [],
    totalResults: 0,
    pageCount: 0,
    pageNumber: 1,
    pageSize: 20,
}

const searchRequestUrls = () =>
    vi
        .mocked(global.fetch)
        .mock.calls.map(([input]) => String(input))
        .filter((url) => url.includes('/v1/navigation-search'))

describe('NavigationSearchWrapper type attribute', () => {
    beforeEach(() => {
        sharedQueryClient.clear()
        navigationSearchStore.getState().actions.clearSearchTerm()
        global.fetch = vi.fn().mockImplementation((input: RequestInfo) => {
            const url = String(input)
            if (url.includes('/v1/navigation-search')) {
                return Promise.resolve({
                    ok: true,
                    json: () => Promise.resolve(emptySearch),
                })
            }
            return Promise.resolve({ ok: true })
        })
    })

    afterEach(() => {
        vi.restoreAllMocks()
    })

    it('requests navigation-search with type=api when type is api', async () => {
        render(<NavigationSearchWrapper type="api" />)

        const input = await screen.findByPlaceholderText('Explore APIs')
        await userEvent.type(input, '_bulk')

        await waitFor(() => {
            expect(
                searchRequestUrls().some((url) => url.includes('type=api'))
            ).toBe(true)
        })
    })

    it('navigates into a matching property and records its anchor on Enter', async () => {
        const url =
            '/docs/api/doc/elasticsearch/operation/operation-search#req-track_total_hits'
        global.fetch = vi.fn().mockResolvedValue({
            ok: true,
            json: async () => ({
                ...emptySearch,
                totalResults: 1,
                results: [
                    {
                        type: 'api',
                        url: url.split('#')[0],
                        title: 'Search',
                        description: 'Search documents',
                        score: 5,
                        parents: [],
                        properties: [
                            {
                                name: 'track_total_hits',
                                path: 'track_total_hits',
                                type: 'boolean',
                                location: 'request',
                                description: 'Count hits accurately',
                                anchor: 'req-track_total_hits',
                                url,
                            },
                        ],
                    },
                ],
            }),
        })
        render(<NavigationSearchWrapper type="api" />)
        const input = await screen.findByPlaceholderText('Explore APIs')
        await userEvent.type(input, 'track_total')
        const child = await screen.findByRole('link', {
            name: /track_total_hits boolean request Count hits accurately/,
        })
        expect(child).toHaveAttribute('href', url)
        const clicked = vi.fn((event: Event) => event.preventDefault())
        child.addEventListener('click', clicked)
        await userEvent.click(input)
        expect(input).toHaveFocus()
        expect(navigationSearchStore.getState().selectedIndex).toBe(0)
        expect(screen.getByText('Go to operation')).toBeInTheDocument()
        expect(screen.getByText('Go to property')).toBeInTheDocument()
        expect(screen.getByText('Go to page')).toBeInTheDocument()
        await userEvent.keyboard('{ArrowDown}')
        expect(navigationSearchStore.getState().selectedIndex).toBe(1)
        await waitFor(() =>
            expect(child).toHaveAttribute('data-selected', 'true')
        )
        expect(screen.getByText('Go to page')).toBeInTheDocument()
        await userEvent.keyboard('{Enter}')
        expect(clicked).toHaveBeenCalledTimes(1)
        expect(logInfo).toHaveBeenCalledWith(
            'navigation_search_result_clicked',
            expect.objectContaining({
                [ATTR_NAVIGATION_SEARCH_RESULT_ANCHOR]: 'req-track_total_hits',
                'navigation_search.result.position': 0,
            })
        )
    })

    it('renders the sidebar variant without the sticky wrapper or divider', async () => {
        const { container } = render(
            <NavigationSearchWrapper type="api" variant="sidebar" />
        )

        await screen.findByPlaceholderText('Explore APIs')

        expect(container.querySelector('.sticky')).toBeNull()
        expect(container.querySelector('hr')).toBeNull()
    })

    it('filters the first query when a search term is already set', async () => {
        navigationSearchStore.getState().actions.setSearchTerm('_bulk')

        render(<NavigationSearchWrapper type="api" />)

        await waitFor(() => {
            const urls = searchRequestUrls()
            expect(urls.length).toBeGreaterThan(0)
            expect(urls.every((url) => url.includes('type=api'))).toBe(true)
        })
    })

    it('leaves the query unfiltered when type is omitted', async () => {
        render(<NavigationSearchWrapper />)

        const input = await screen.findByPlaceholderText('Jump to page')
        await userEvent.type(input, '_bulk')

        await waitFor(() => {
            const urls = searchRequestUrls()
            expect(urls.length).toBeGreaterThan(0)
            expect(urls.every((url) => !url.includes('type='))).toBe(true)
        })
    })
})

describe('NavigationSearchWrapper result navigation', () => {
    const operationUrl =
        '/docs/api/doc/elasticsearch/operation/operation-search'
    const propertyUrl = `${operationUrl}#req-retriever`

    beforeEach(() => {
        sharedQueryClient.clear()
        navigationSearchStore.getState().actions.clearSearchTerm()
        global.fetch = vi.fn().mockResolvedValue({
            ok: true,
            json: async () => ({
                ...emptySearch,
                totalResults: 1,
                results: [
                    {
                        type: 'api',
                        url: operationUrl,
                        title: 'Search',
                        description: 'Search documents',
                        score: 5,
                        parents: [],
                        properties: [
                            {
                                name: 'retriever',
                                path: 'retriever',
                                type: 'object',
                                location: 'request',
                                description: 'Retriever',
                                anchor: 'req-retriever',
                                url: propertyUrl,
                            },
                        ],
                    },
                ],
            }),
        })
    })

    afterEach(() => {
        window.history.replaceState(null, '', '/')
    })

    const openResults = async () => {
        render(<NavigationSearchWrapper type="api" />)
        const input = await screen.findByPlaceholderText('Explore APIs')
        await userEvent.type(input, 'retriever')
        const row = await screen.findByRole('link', {
            name: /retriever object request Retriever/,
        })
        row.addEventListener('click', (event) => event.preventDefault())
        await waitFor(() => expect(row).toBeVisible())
        return { input, row }
    }

    it('closes and blurs when a result is clicked', async () => {
        const { input, row } = await openResults()

        await userEvent.click(row)

        expect(input).not.toHaveFocus()
        await waitFor(() => expect(row).not.toBeVisible())
    })

    it('closes and blurs when a shift-click navigates through htmx', async () => {
        const { input, row } = await openResults()

        fireEvent.click(row, { shiftKey: true })

        expect(input).not.toHaveFocus()
        await waitFor(() => expect(row).not.toBeVisible())
    })

    it.each([['metaKey'], ['ctrlKey']])(
        'stays open and focused when a %s click opens the result elsewhere',
        async (modifier) => {
            const { input, row } = await openResults()
            vi.mocked(logInfo).mockClear()

            fireEvent.click(row, { [modifier]: true })
            await new Promise((resolve) => setTimeout(resolve, 0))

            expect(input).toHaveFocus()
            expect(row).toBeVisible()
            expect(logInfo).toHaveBeenCalledWith(
                'navigation_search_result_clicked',
                expect.objectContaining({
                    [ATTR_NAVIGATION_SEARCH_RESULT_ANCHOR]: 'req-retriever',
                })
            )
        }
    )

    it('closes and blurs when Enter opens the selected result', async () => {
        const { input, row } = await openResults()
        await userEvent.keyboard('{ArrowDown}{ArrowDown}')
        await waitFor(() =>
            expect(row).toHaveAttribute('data-selected', 'true')
        )

        await userEvent.keyboard('{Enter}')

        expect(input).not.toHaveFocus()
        await waitFor(() => expect(row).not.toBeVisible())
    })

    it('reopens when the box is focused again after a result click', async () => {
        const { input, row } = await openResults()
        await userEvent.click(row)
        await waitFor(() => expect(row).not.toBeVisible())

        await userEvent.click(input)

        expect(input).toHaveFocus()
        await waitFor(() => expect(row).toBeVisible())
    })

    it('a same-page result click still closes and reopens', async () => {
        window.history.replaceState(null, '', operationUrl)
        const { input, row } = await openResults()
        expect(row).toHaveAttribute(
            'href',
            `${window.location.pathname}#req-retriever`
        )

        await userEvent.click(row)
        expect(input).not.toHaveFocus()
        await waitFor(() => expect(row).not.toBeVisible())

        await userEvent.click(input)
        await waitFor(() => expect(row).toBeVisible())
    })
})

describe('NavigationSearchWrapper slash shortcut', () => {
    const apiPlaceholder = 'Explore APIs'

    beforeEach(() => {
        sharedQueryClient.clear()
        navigationSearchStore.getState().actions.clearSearchTerm()
        global.fetch = vi.fn().mockResolvedValue({
            ok: true,
            json: () => Promise.resolve(emptySearch),
        })
    })

    it.each([
        ['/', 'slash'],
        ['s', 'letter'],
    ])('focuses the api input on "%s" without typing the %s', async (key) => {
        render(<NavigationSearchWrapper type="api" />)
        const input = await screen.findByPlaceholderText(apiPlaceholder)

        await userEvent.keyboard(key)

        expect(input).toHaveFocus()
        expect(input).toHaveValue('')
    })

    it('ignores a capital "S"', async () => {
        render(<NavigationSearchWrapper type="api" />)
        const input = await screen.findByPlaceholderText(apiPlaceholder)

        await userEvent.keyboard('S')

        expect(input).not.toHaveFocus()
    })

    it('leaves "s" alone while typing in another field', async () => {
        render(
            <>
                <textarea aria-label="notes" />
                <NavigationSearchWrapper type="api" />
            </>
        )
        const input = await screen.findByPlaceholderText(apiPlaceholder)
        const notes = screen.getByLabelText('notes')

        await userEvent.click(notes)
        await userEvent.keyboard('s')

        expect(notes).toHaveValue('s')
        expect(input).not.toHaveFocus()
    })

    it('blurs the api input on Escape and logs one close', async () => {
        render(<NavigationSearchWrapper type="api" />)
        const input = await screen.findByPlaceholderText(apiPlaceholder)
        await userEvent.click(input)
        await userEvent.type(input, '_bulk')
        vi.mocked(logInfo).mockClear()

        await userEvent.keyboard('{Escape}')

        await waitFor(() => expect(input).not.toHaveFocus())
        const closes = vi
            .mocked(logInfo)
            .mock.calls.filter(([name]) => name === 'navigation_search_closed')
        expect(closes).toHaveLength(1)
    })

    it('keeps focus on Escape when type is omitted', async () => {
        render(<NavigationSearchWrapper />)
        const input = await screen.findByPlaceholderText('Jump to page')
        await userEvent.click(input)

        await userEvent.keyboard('{Escape}')

        expect(input).toHaveFocus()
    })

    it('exposes the shortcut on the api input', async () => {
        render(<NavigationSearchWrapper type="api" />)
        const input = await screen.findByPlaceholderText(apiPlaceholder)

        expect(input).toHaveAttribute('aria-keyshortcuts', '/ s')
    })

    it('shows a "/" key chip on the idle api input', async () => {
        const { container } = render(<NavigationSearchWrapper type="api" />)
        await screen.findByPlaceholderText(apiPlaceholder)

        expect(container.querySelector('kbd')).toHaveTextContent('/')
    })

    it('hides the key chip while the api input is focused or has text', async () => {
        const { container } = render(<NavigationSearchWrapper type="api" />)
        const input = await screen.findByPlaceholderText(apiPlaceholder)

        await userEvent.click(input)
        expect(container.querySelector('kbd')).toBeNull()

        await userEvent.type(input, 'search')
        await userEvent.tab()
        expect(input).toHaveValue('search')
        expect(container.querySelector('kbd')).toBeNull()
    })

    it('renders the hero variant with the same placeholder and the key chip', async () => {
        const { container } = render(
            <NavigationSearchWrapper type="api" variant="hero" />
        )
        await screen.findByPlaceholderText(apiPlaceholder)

        expect(container.querySelector('kbd')).toHaveTextContent('/')
    })

    it('shows no key chip on the docs input', async () => {
        const { container } = render(<NavigationSearchWrapper />)
        await screen.findByPlaceholderText('Jump to page')

        expect(container.querySelector('kbd')).toBeNull()
    })

    it('names the api input and describes it on hover', async () => {
        render(<NavigationSearchWrapper type="api" />)
        await screen.findByPlaceholderText(apiPlaceholder)

        const input = screen.getByRole('textbox', { name: 'Explore APIs' })

        expect(input).toHaveAttribute(
            'title',
            'Explore API operations. Type / to focus.'
        )
    })

    it('leaves "/" alone while typing in another field', async () => {
        render(
            <>
                <textarea aria-label="notes" />
                <NavigationSearchWrapper type="api" />
            </>
        )
        const input = await screen.findByPlaceholderText(apiPlaceholder)
        const notes = screen.getByLabelText('notes')

        await userEvent.click(notes)
        await userEvent.keyboard('/')

        expect(notes).toHaveFocus()
        expect(notes).toHaveValue('/')
        expect(input).not.toHaveFocus()
    })

    it.each([
        '{Control>}/{/Control}',
        '{Meta>}/{/Meta}',
        '{Alt>}/{/Alt}',
        '{Control>}s{/Control}',
    ])('ignores "/" with a modifier (%s)', async (keys) => {
        render(<NavigationSearchWrapper type="api" />)
        const input = await screen.findByPlaceholderText(apiPlaceholder)

        await userEvent.keyboard(keys)

        expect(input).not.toHaveFocus()
    })

    it('does not take over Ctrl+K', async () => {
        render(<NavigationSearchWrapper type="api" />)
        const input = await screen.findByPlaceholderText(apiPlaceholder)

        await userEvent.keyboard('{Control>}k{/Control}')

        expect(input).not.toHaveFocus()
    })

    it('does nothing on a small screen', async () => {
        const originalWidth = window.innerWidth
        window.innerWidth = 500
        render(<NavigationSearchWrapper type="api" />)
        const input = await screen.findByPlaceholderText(apiPlaceholder)

        await userEvent.keyboard('/')

        expect(input).not.toHaveFocus()
        expect(input).not.toHaveAttribute('aria-keyshortcuts')
        window.innerWidth = originalWidth
    })

    it.each([undefined, 'docs', 'all'])(
        'does nothing for type=%s',
        async (type) => {
            render(<NavigationSearchWrapper type={type} />)
            const input = await screen.findByPlaceholderText('Jump to page')

            await userEvent.keyboard('/')

            expect(input).not.toHaveFocus()
            expect(input).not.toHaveAttribute('aria-keyshortcuts')
        }
    )
})
