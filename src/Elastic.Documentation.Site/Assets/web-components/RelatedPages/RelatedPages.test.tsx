import * as logging from '../../telemetry/logging'
import { RelatedPages } from './RelatedPages'
import { EuiProvider } from '@elastic/eui'
import { fireEvent, render, screen, waitFor } from '@testing-library/react'

jest.mock('../../telemetry/logging', () => ({
    logInfo: jest.fn(),
}))

const fetchMock = jest.fn()

const renderRelatedPages = () =>
    render(
        <EuiProvider
            colorMode="light"
            globalStyles={false}
            utilityClasses={false}
        >
            <RelatedPages path="/docs/old/index-lifecycle-management.html" />
        </EuiProvider>
    )

describe('RelatedPages', () => {
    beforeEach(() => {
        jest.clearAllMocks()
        Object.defineProperty(global, 'fetch', {
            configurable: true,
            writable: true,
            value: fetchMock,
        })
    })

    afterEach(() => {
        jest.restoreAllMocks()
    })

    it('renders related pages returned by the API and tracks clicks', async () => {
        fetchMock.mockResolvedValue({
            ok: true,
            json: async () => ({
                query: 'index lifecycle management',
                results: [
                    {
                        url: '/docs/manage-data/lifecycle/index-lifecycle-management',
                        title: 'Manage the index lifecycle',
                        description:
                            'Automate how indices are managed over time.',
                        parents: [
                            { title: 'Manage data', url: '/docs/manage-data' },
                        ],
                    },
                ],
            }),
        } as Response)

        renderRelatedPages()

        const link = await screen.findByRole('link', {
            name: /manage the index lifecycle/i,
        })
        expect(screen.getByText('You might be looking for')).toBeInTheDocument()
        expect(global.fetch).toHaveBeenCalledWith(
            '/docs/_api/v1/related-pages?path=%2Fdocs%2Fold%2Findex-lifecycle-management.html',
            expect.objectContaining({ signal: expect.any(AbortSignal) })
        )

        link.addEventListener('click', (event) => event.preventDefault())
        fireEvent.click(link, { button: 1 })

        expect(logging.logInfo).toHaveBeenCalledWith(
            '404 related page clicked',
            {
                '404.related_pages.result_url':
                    '/docs/manage-data/lifecycle/index-lifecycle-management',
                '404.related_pages.result_position': 0,
            }
        )
    })

    it('renders no section when the API has no confident suggestions', async () => {
        fetchMock.mockResolvedValue({
            ok: true,
            json: async () => ({ query: 'missing', results: [] }),
        } as Response)

        renderRelatedPages()

        await waitFor(() =>
            expect(
                screen.queryByText('Finding related pages')
            ).not.toBeInTheDocument()
        )
        expect(
            screen.queryByText('You might be looking for')
        ).not.toBeInTheDocument()
    })
})
