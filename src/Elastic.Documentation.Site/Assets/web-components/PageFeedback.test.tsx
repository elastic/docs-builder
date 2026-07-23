import { PageFeedback } from './PageFeedback'
import { act, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import * as React from 'react'

const feedbackId = '00000000-0000-4000-8000-000000000001'

describe('PageFeedback', () => {
    beforeEach(() => {
        jest.spyOn(crypto, 'randomUUID').mockReturnValue(feedbackId)
        global.fetch = jest.fn().mockResolvedValue({
            ok: true,
            status: 204,
        })
    })

    afterEach(() => {
        jest.restoreAllMocks()
    })

    it('opens immediately and only records the last debounced reaction', async () => {
        const user = userEvent.setup()
        render(<PageFeedback pageUrl="/docs/test-page" pageTitle="Test page" />)

        await user.click(
            screen.getByRole('button', { name: 'This page was helpful' })
        )

        const comment = screen.getByRole('textbox', {
            name: 'Tell us more (optional)',
        })
        expect(comment).toHaveAttribute('maxlength', '2000')
        expect(comment).toHaveFocus()

        await user.click(
            screen.getByRole('button', { name: 'This page was not helpful' })
        )

        expect(global.fetch).not.toHaveBeenCalled()
        await waitFor(() =>
            expect(global.fetch).toHaveBeenCalledWith(
                `/docs/_api/v1/page-feedback/${feedbackId}`,
                expect.objectContaining({
                    method: 'PUT',
                    body: JSON.stringify({
                        pageUrl: '/docs/test-page',
                        pageTitle: 'Test page',
                        reaction: 'thumbsDown',
                    }),
                })
            )
        )
    })

    it('collapses the comment form when focus leaves the feedback', async () => {
        const user = userEvent.setup()
        render(
            <>
                <PageFeedback pageUrl="/docs/test-page" pageTitle="Test page" />
                <button type="button">Outside</button>
            </>
        )

        await user.click(
            screen.getByRole('button', { name: 'This page was helpful' })
        )
        await user.click(screen.getByRole('button', { name: 'Outside' }))

        expect(
            screen
                .getByRole('textbox', { name: 'Tell us more (optional)' })
                .closest('form')
        ).toHaveClass('page-feedback__form--closing')
        await waitFor(() =>
            expect(
                screen.queryByRole('textbox', {
                    name: 'Tell us more (optional)',
                })
            ).not.toBeInTheDocument()
        )

        await user.click(screen.getByText('Was this page helpful?'))

        expect(
            screen.getByRole('textbox', { name: 'Tell us more (optional)' })
        ).toBeInTheDocument()
    })

    it('submits optional text with the same feedback id', async () => {
        const user = userEvent.setup()
        render(<PageFeedback pageUrl="/docs/test-page" pageTitle="Test page" />)

        await user.click(
            screen.getByRole('button', { name: 'This page was not helpful' })
        )
        const comment = screen.getByRole('textbox', {
            name: 'Tell us more (optional)',
        })
        await user.type(comment, 'The example needs more detail.')
        await user.click(screen.getByRole('button', { name: 'Send feedback' }))

        await waitFor(() => expect(global.fetch).toHaveBeenCalledTimes(1))
        expect(global.fetch).toHaveBeenLastCalledWith(
            `/docs/_api/v1/page-feedback/${feedbackId}`,
            expect.objectContaining({
                body: JSON.stringify({
                    pageUrl: '/docs/test-page',
                    pageTitle: 'Test page',
                    reaction: 'thumbsDown',
                    comment: 'The example needs more detail.',
                }),
            })
        )
        expect(
            await screen.findByText('Thanks for your feedback.')
        ).toBeInTheDocument()
    })

    it('keeps an optimistic reaction without showing an error', async () => {
        const user = userEvent.setup()
        jest.mocked(global.fetch).mockResolvedValueOnce({
            ok: false,
            status: 503,
        } as Response)
        render(<PageFeedback pageUrl="/docs/test-page" pageTitle="Test page" />)

        const helpfulButton = screen.getByRole('button', {
            name: 'This page was helpful',
        })
        await user.click(helpfulButton)

        await waitFor(() => expect(global.fetch).toHaveBeenCalledTimes(1))
        expect(helpfulButton).toHaveAttribute('aria-pressed', 'true')
        expect(
            screen.queryByText("We couldn't save your feedback.")
        ).not.toBeInTheDocument()
        expect(
            screen.queryByRole('button', { name: 'Retry' })
        ).not.toBeInTheDocument()
    })

    it('preserves the reaction and retries a failed comment', async () => {
        const user = userEvent.setup()
        jest.mocked(global.fetch)
            .mockResolvedValueOnce({ ok: false, status: 503 } as Response)
            .mockResolvedValueOnce({ ok: true, status: 204 } as Response)
        render(<PageFeedback pageUrl="/docs/test-page" pageTitle="Test page" />)

        const helpfulButton = screen.getByRole('button', {
            name: 'This page was helpful',
        })
        await user.click(helpfulButton)
        await user.type(
            screen.getByRole('textbox', {
                name: 'Tell us more (optional)',
            }),
            'The example needs more detail.'
        )
        await user.click(screen.getByRole('button', { name: 'Send feedback' }))

        expect(
            await screen.findByText("We couldn't save your feedback.")
        ).toBeInTheDocument()
        expect(helpfulButton).toHaveAttribute('aria-pressed', 'true')

        await user.click(screen.getByRole('button', { name: 'Retry' }))

        await waitFor(() => expect(global.fetch).toHaveBeenCalledTimes(2))
    })

    it('does not record a reaction deselected during the debounce', async () => {
        const user = userEvent.setup()
        render(<PageFeedback pageUrl="/docs/test-page" pageTitle="Test page" />)
        const helpfulButton = screen.getByRole('button', {
            name: 'This page was helpful',
        })

        await user.click(helpfulButton)
        await user.click(helpfulButton)
        await act(
            () =>
                new Promise((resolve) => {
                    window.setTimeout(resolve, 550)
                })
        )

        expect(helpfulButton).toHaveAttribute('aria-pressed', 'false')
        expect(global.fetch).not.toHaveBeenCalled()
    })

    it('deletes a previously recorded reaction when deselected', async () => {
        const user = userEvent.setup()
        render(<PageFeedback pageUrl="/docs/test-page" pageTitle="Test page" />)
        const helpfulButton = screen.getByRole('button', {
            name: 'This page was helpful',
        })

        await user.click(helpfulButton)
        await waitFor(() => expect(global.fetch).toHaveBeenCalledTimes(1))
        await user.click(helpfulButton)

        await waitFor(() => expect(global.fetch).toHaveBeenCalledTimes(2))
        expect(global.fetch).toHaveBeenLastCalledWith(
            `/docs/_api/v1/page-feedback/${feedbackId}`,
            expect.objectContaining({ method: 'DELETE' })
        )
    })
})
