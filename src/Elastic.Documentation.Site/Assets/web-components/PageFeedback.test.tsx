import { PageFeedback } from './PageFeedback'
import { act, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import * as React from 'react'

const feedbackId = '00000000-0000-4000-8000-000000000001'
const successfulResponse = { ok: true, status: 204 } as Response
const failedResponse = { ok: false, status: 503 } as Response

describe('PageFeedback', () => {
    beforeEach(() => {
        sessionStorage.clear()
        jest.spyOn(crypto, 'randomUUID').mockReturnValue(feedbackId)
        global.fetch = jest.fn().mockResolvedValue(successfulResponse)
    })

    afterEach(() => {
        sessionStorage.clear()
        jest.restoreAllMocks()
    })

    it('records a positive reaction after the debounce and shows positive reasons', async () => {
        const user = userEvent.setup()
        render(<PageFeedback pageUrl="/docs/test-page" pageTitle="Test page" />)

        await user.click(
            screen.getByRole('button', {
                name: 'Yes, this page was helpful',
            })
        )

        await waitFor(() =>
            expect(global.fetch).toHaveBeenCalledWith(
                `/docs/_api/v1/page-feedback/${feedbackId}`,
                expect.objectContaining({
                    method: 'PUT',
                    body: JSON.stringify({
                        pageUrl: '/docs/test-page',
                        pageTitle: 'Test page',
                        reaction: 'thumbsUp',
                    }),
                })
            )
        )
        expect(
            screen.getByRole('group', { name: 'What did you like?' })
        ).toBeInTheDocument()
        expect(
            screen.getByRole('checkbox', { name: /Solved my problem/ })
        ).toHaveFocus()
        expect(
            screen.queryByRole('checkbox', { name: /Technically incorrect/ })
        ).not.toBeInTheDocument()
        expect(screen.getByRole('button', { name: 'Submit' })).toBeDisabled()
    })

    it('shows negative reasons after a negative reaction', async () => {
        const user = userEvent.setup()
        render(<PageFeedback pageUrl="/docs/test-page" pageTitle="Test page" />)

        await user.click(
            screen.getByRole('button', {
                name: 'No, this page was not helpful',
            })
        )

        expect(
            screen.getByRole('group', { name: 'What went wrong?' })
        ).toBeInTheDocument()
        expect(
            screen.getByRole('checkbox', {
                name: /Couldn't find what I needed/,
            })
        ).toBeInTheDocument()
        expect(
            screen.getByRole('checkbox', { name: /Out of date/ })
        ).toBeInTheDocument()
        expect(
            screen.queryByRole('checkbox', { name: /Solved my problem/ })
        ).not.toBeInTheDocument()
    })

    it('shows positive findability option after a positive reaction', async () => {
        const user = userEvent.setup()
        render(<PageFeedback pageUrl="/docs/test-page" pageTitle="Test page" />)

        await user.click(
            screen.getByRole('button', { name: 'Yes, this page was helpful' })
        )

        expect(
            screen.getByRole('checkbox', { name: /Easy to find/ })
        ).toBeInTheDocument()
        expect(
            screen.queryByRole('checkbox', { name: /Out of date/ })
        ).not.toBeInTheDocument()
    })

    it('debounces reaction changes and records the latest choice', async () => {
        const user = userEvent.setup()
        render(<PageFeedback pageUrl="/docs/test-page" pageTitle="Test page" />)

        const yes = screen.getByRole('button', {
            name: 'Yes, this page was helpful',
        })
        const no = screen.getByRole('button', {
            name: 'No, this page was not helpful',
        })

        await user.click(yes)
        await user.click(screen.getByRole('checkbox', { name: /Accurate/ }))
        await user.click(no)

        expect(yes).toHaveAttribute('aria-pressed', 'false')
        expect(no).toHaveAttribute('aria-pressed', 'true')
        expect(
            screen.getByRole('group', { name: 'What went wrong?' })
        ).toBeInTheDocument()
        expect(
            screen.queryByRole('checkbox', { name: /Accurate/ })
        ).not.toBeInTheDocument()
        await waitFor(() => {
            expect(global.fetch).toHaveBeenCalledTimes(1)
            expect(global.fetch).toHaveBeenLastCalledWith(
                `/docs/_api/v1/page-feedback/${feedbackId}`,
                expect.objectContaining({
                    body: expect.stringContaining('"reaction":"thumbsDown"'),
                })
            )
        })
    })

    it('does not retry a failed immediate reaction', async () => {
        const user = userEvent.setup()
        jest.mocked(global.fetch).mockResolvedValueOnce(failedResponse)
        render(<PageFeedback pageUrl="/docs/test-page" pageTitle="Test page" />)

        await user.click(
            screen.getByRole('button', {
                name: 'Yes, this page was helpful',
            })
        )

        await waitFor(() => expect(global.fetch).toHaveBeenCalledTimes(1))
        expect(
            screen.queryByText("We couldn't save your feedback.")
        ).not.toBeInTheDocument()
    })

    it('allows selecting multiple reasons and submits them all', async () => {
        const user = userEvent.setup()
        render(<PageFeedback pageUrl="/docs/test-page" pageTitle="Test page" />)

        await user.click(
            screen.getByRole('button', {
                name: 'No, this page was not helpful',
            })
        )

        await user.click(
            screen.getByRole('checkbox', { name: /Technically incorrect/ })
        )
        await user.click(screen.getByRole('checkbox', { name: /Out of date/ }))
        expect(
            screen.getByRole('button', { name: 'Submit' })
        ).not.toBeDisabled()
        await user.click(screen.getByRole('button', { name: 'Submit' }))

        await waitFor(() => expect(global.fetch).toHaveBeenCalledTimes(1))
        expect(global.fetch).toHaveBeenLastCalledWith(
            `/docs/_api/v1/page-feedback/${feedbackId}`,
            expect.objectContaining({
                body: JSON.stringify({
                    pageUrl: '/docs/test-page',
                    pageTitle: 'Test page',
                    reaction: 'thumbsDown',
                    reasons: ['inaccurate', 'outOfDate'],
                    reasonSetVersion: 2,
                }),
            })
        )
        expect(
            await screen.findByText('Thank you for your feedback.')
        ).toBeInTheDocument()
    })

    it('submits a single reason without a comment', async () => {
        const user = userEvent.setup()
        render(<PageFeedback pageUrl="/docs/test-page" pageTitle="Test page" />)

        await user.click(
            screen.getByRole('button', {
                name: 'No, this page was not helpful',
            })
        )
        await user.click(
            screen.getByRole('checkbox', {
                name: /Couldn't find what I needed/,
            })
        )
        expect(
            screen.getByRole('textbox', { name: 'Tell us more (optional)' })
        ).toHaveAttribute('maxlength', '2000')
        await user.click(screen.getByRole('button', { name: 'Submit' }))

        await waitFor(() => expect(global.fetch).toHaveBeenCalledTimes(1))
        expect(global.fetch).toHaveBeenLastCalledWith(
            `/docs/_api/v1/page-feedback/${feedbackId}`,
            expect.objectContaining({
                body: JSON.stringify({
                    pageUrl: '/docs/test-page',
                    pageTitle: 'Test page',
                    reaction: 'thumbsDown',
                    reasons: ['missingInformation'],
                    reasonSetVersion: 2,
                }),
            })
        )
        expect(
            await screen.findByText('Thank you for your feedback.')
        ).toBeInTheDocument()
    })

    it('shows a single shared comment field once the reaction is selected', async () => {
        const user = userEvent.setup()
        render(<PageFeedback pageUrl="/docs/test-page" pageTitle="Test page" />)

        expect(
            screen.queryByRole('textbox', { name: 'Tell us more (optional)' })
        ).not.toBeInTheDocument()

        await user.click(
            screen.getByRole('button', { name: 'Yes, this page was helpful' })
        )

        const textarea = screen.getByRole('textbox', {
            name: 'Tell us more (optional)',
        })
        expect(textarea).toBeInTheDocument()

        await user.click(screen.getByRole('checkbox', { name: /Accurate/ }))
        await user.click(
            screen.getByRole('checkbox', { name: /Helpful examples/ })
        )
        expect(screen.getAllByRole('checkbox', { checked: true })).toHaveLength(
            2
        )
        expect(textarea).toBeInTheDocument()
        expect(
            screen.getAllByRole('textbox', { name: 'Tell us more (optional)' })
        ).toHaveLength(1)
    })

    it('retains the comment when switching between reactions', async () => {
        const user = userEvent.setup()
        render(<PageFeedback pageUrl="/docs/test-page" pageTitle="Test page" />)

        await user.click(
            screen.getByRole('button', { name: 'Yes, this page was helpful' })
        )
        await user.type(
            screen.getByRole('textbox', { name: 'Tell us more (optional)' }),
            'Great page.'
        )

        await user.click(
            screen.getByRole('button', {
                name: 'No, this page was not helpful',
            })
        )

        expect(
            screen.getByRole('textbox', { name: 'Tell us more (optional)' })
        ).toHaveValue('Great page.')
    })

    it('clears reasons that are invalid for the new reaction when switching', async () => {
        const user = userEvent.setup()
        render(<PageFeedback pageUrl="/docs/test-page" pageTitle="Test page" />)

        await user.click(
            screen.getByRole('button', { name: 'Yes, this page was helpful' })
        )
        await user.click(screen.getByRole('checkbox', { name: /Accurate/ }))
        await user.click(
            screen.getByRole('button', {
                name: 'No, this page was not helpful',
            })
        )

        expect(
            screen.queryByRole('checkbox', { checked: true })
        ).not.toBeInTheDocument()
        expect(screen.getByRole('button', { name: 'Submit' })).toBeDisabled()
    })

    it('waits for an in-flight reaction save before storing richer feedback', async () => {
        const user = userEvent.setup()
        let resolveInitialSave: (response: Response) => void = () => {}
        const initialSave = new Promise<Response>((resolve) => {
            resolveInitialSave = resolve
        })
        jest.mocked(global.fetch)
            .mockReturnValueOnce(initialSave)
            .mockResolvedValueOnce(successfulResponse)
        render(<PageFeedback pageUrl="/docs/test-page" pageTitle="Test page" />)

        await user.click(
            screen.getByRole('button', {
                name: 'Yes, this page was helpful',
            })
        )
        await waitFor(() => expect(global.fetch).toHaveBeenCalledTimes(1))
        await user.click(screen.getByRole('checkbox', { name: /Accurate/ }))
        await user.click(screen.getByRole('button', { name: 'Submit' }))

        expect(global.fetch).toHaveBeenCalledTimes(1)
        await act(async () => resolveInitialSave(successfulResponse))
        await waitFor(() => expect(global.fetch).toHaveBeenCalledTimes(2))
        expect(global.fetch).toHaveBeenLastCalledWith(
            `/docs/_api/v1/page-feedback/${feedbackId}`,
            expect.objectContaining({
                body: expect.stringContaining('"reasons":["accurate"]'),
            })
        )
    })

    it('preserves selected reasons and comment when submission is retried', async () => {
        const user = userEvent.setup()
        jest.mocked(global.fetch)
            .mockResolvedValueOnce(successfulResponse)
            .mockResolvedValueOnce(failedResponse)
            .mockResolvedValueOnce(successfulResponse)
        render(<PageFeedback pageUrl="/docs/test-page" pageTitle="Test page" />)

        await user.click(
            screen.getByRole('button', {
                name: 'No, this page was not helpful',
            })
        )
        await waitFor(() => expect(global.fetch).toHaveBeenCalledTimes(1))
        const reason = screen.getByRole('checkbox', {
            name: /Code sample errors/,
        })
        await user.click(reason)
        const commentField = screen.getByRole('textbox', {
            name: 'Tell us more (optional)',
        })
        await user.type(commentField, 'The Python example fails.')
        await user.click(screen.getByRole('button', { name: 'Submit' }))

        expect(
            await screen.findByText("We couldn't save your feedback.")
        ).toBeInTheDocument()
        expect(reason).toBeChecked()
        expect(commentField).toHaveValue('The Python example fails.')

        await user.click(screen.getByRole('button', { name: 'Try again' }))

        await waitFor(() => expect(global.fetch).toHaveBeenCalledTimes(3))
        expect(
            await screen.findByText('Thank you for your feedback.')
        ).toBeInTheDocument()
    })

    it('restores reasons and comment from draft after remount and clears on submit', async () => {
        const user = userEvent.setup()
        const { unmount } = render(
            <PageFeedback pageUrl="/docs/test-page" pageTitle="Test page" />
        )

        await user.click(
            screen.getByRole('button', {
                name: 'Yes, this page was helpful',
            })
        )
        await user.click(screen.getByRole('checkbox', { name: /Accurate/ }))
        await user.click(
            screen.getByRole('checkbox', { name: /Helpful examples/ })
        )
        await user.type(
            screen.getByRole('textbox', { name: 'Tell us more (optional)' }),
            'Great content.'
        )
        await waitFor(() =>
            expect(
                sessionStorage.getItem('docs-page-feedback:/docs/test-page')
            ).toContain('Great content.')
        )

        unmount()
        render(<PageFeedback pageUrl="/docs/test-page" pageTitle="Test page" />)

        expect(
            screen.getByRole('button', { name: 'Yes, this page was helpful' })
        ).toHaveAttribute('aria-pressed', 'true')
        expect(screen.getByRole('checkbox', { name: /Accurate/ })).toBeChecked()
        expect(
            screen.getByRole('checkbox', { name: /Helpful examples/ })
        ).toBeChecked()
        expect(
            screen.getByRole('textbox', { name: 'Tell us more (optional)' })
        ).toHaveValue('Great content.')

        await user.click(screen.getByRole('button', { name: 'Submit' }))

        expect(
            await screen.findByText('Thank you for your feedback.')
        ).toBeInTheDocument()
        expect(
            sessionStorage.getItem('docs-page-feedback:/docs/test-page')
        ).toBeNull()
    })

    it('keeps stored reason values on API pages and uses API copy', async () => {
        const user = userEvent.setup()
        render(
            <PageFeedback
                pageUrl="/api/doc/elasticsearch/search"
                pageTitle="Search"
                surface="api"
            />
        )

        await user.click(
            screen.getByRole('button', {
                name: 'No, this page was not helpful',
            })
        )
        expect(
            screen.getByText(
                'Missing a parameter, field, status, or auth detail.'
            )
        ).toBeInTheDocument()
        await user.click(
            screen.getByRole('checkbox', { name: /Example errors/ })
        )
        await user.click(screen.getByRole('button', { name: 'Submit' }))

        await waitFor(() =>
            expect(global.fetch).toHaveBeenLastCalledWith(
                `/docs/_api/v1/page-feedback/${feedbackId}`,
                expect.objectContaining({
                    body: expect.stringContaining(
                        '"reasons":["codeSampleErrors"]'
                    ),
                })
            )
        )
    })
})
