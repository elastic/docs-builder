import { PageFeedback } from './PageFeedback'
import { act, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import * as React from 'react'

const feedbackId = '00000000-0000-4000-8000-000000000001'
const successfulResponse = { ok: true, status: 204 } as Response
const failedResponse = { ok: false, status: 503 } as Response

describe('PageFeedback', () => {
    beforeEach(() => {
        jest.spyOn(crypto, 'randomUUID').mockReturnValue(feedbackId)
        global.fetch = jest.fn().mockResolvedValue(successfulResponse)
    })

    afterEach(() => {
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
        expect(screen.getByRole('radio', { name: /Accurate/ })).toHaveFocus()
        expect(
            screen.queryByRole('radio', { name: /Inaccurate/ })
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
            screen.getByRole('radio', {
                name: /Couldn't find what I needed/,
            })
        ).toBeInTheDocument()
        expect(
            screen.queryByRole('radio', { name: /Solved my problem/ })
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
        await user.click(screen.getByRole('radio', { name: /Accurate/ }))
        await user.click(no)

        expect(yes).toHaveAttribute('aria-pressed', 'false')
        expect(no).toHaveAttribute('aria-pressed', 'true')
        expect(
            screen.getByRole('group', { name: 'What went wrong?' })
        ).toBeInTheDocument()
        expect(
            screen.queryByRole('radio', { name: /Accurate/ })
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

    it('silently retries a failed immediate reaction once', async () => {
        const user = userEvent.setup()
        jest.mocked(global.fetch)
            .mockResolvedValueOnce(failedResponse)
            .mockResolvedValueOnce(successfulResponse)
        render(<PageFeedback pageUrl="/docs/test-page" pageTitle="Test page" />)

        await user.click(
            screen.getByRole('button', {
                name: 'Yes, this page was helpful',
            })
        )

        await waitFor(() => expect(global.fetch).toHaveBeenCalledTimes(2))
        expect(
            screen.queryByText("We couldn't save your feedback.")
        ).not.toBeInTheDocument()
    })

    it('submits a structured reason without requiring details', async () => {
        const user = userEvent.setup()
        render(<PageFeedback pageUrl="/docs/test-page" pageTitle="Test page" />)

        await user.click(
            screen.getByRole('button', {
                name: 'No, this page was not helpful',
            })
        )
        expect(
            screen.queryByRole('textbox', {
                name: 'Tell us more (optional)',
            })
        ).not.toBeInTheDocument()

        await user.click(
            screen.getByRole('radio', {
                name: /Couldn't find what I needed/,
            })
        )
        const commentField = screen.getByRole('textbox', {
            name: 'Tell us more (optional)',
        })
        expect(commentField).toHaveAttribute('maxlength', '2000')
        await user.click(screen.getByRole('button', { name: 'Submit' }))

        await waitFor(() => expect(global.fetch).toHaveBeenCalledTimes(2))
        expect(global.fetch).toHaveBeenLastCalledWith(
            `/docs/_api/v1/page-feedback/${feedbackId}`,
            expect.objectContaining({
                body: JSON.stringify({
                    pageUrl: '/docs/test-page',
                    pageTitle: 'Test page',
                    reaction: 'thumbsDown',
                    reason: 'missingInformation',
                    reasonSetVersion: 1,
                }),
            })
        )
        expect(
            await screen.findByText('Thank you for your feedback.')
        ).toBeInTheDocument()
    })

    it('waits for the immediate reaction before storing richer feedback', async () => {
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
        await user.click(screen.getByRole('radio', { name: /Accurate/ }))
        await user.click(screen.getByRole('button', { name: 'Submit' }))

        expect(global.fetch).toHaveBeenCalledTimes(1)
        await act(async () => resolveInitialSave(successfulResponse))
        await waitFor(() => expect(global.fetch).toHaveBeenCalledTimes(2))
        expect(global.fetch).toHaveBeenLastCalledWith(
            `/docs/_api/v1/page-feedback/${feedbackId}`,
            expect.objectContaining({
                body: expect.stringContaining('"reason":"accurate"'),
            })
        )
    })

    it('preserves the selected reason and details when submission is retried', async () => {
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
        const reason = screen.getByRole('radio', {
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
})
