import { renderWithProviders } from '../../test-utils/render'
import { startAskAiStream } from '../shared/askAiStreamClient'
import { AIAnswerPanel } from './AIAnswerPanel'
import type { SearchResultItem } from './useFullPageSearchQuery'
import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'

vi.mock('../shared/askAiStreamClient', () => ({
    startAskAiStream: vi.fn(async ({ callbacks }) => {
        callbacks.onEvent({ type: 'message_chunk', content: 'Use a tenant.' })
        callbacks.onEvent({ type: 'conversation_end' })
    }),
}))

// jsdom has no Web Animations API; the panel reads it when it expands.
Element.prototype.getAnimations ??= () => []

const highlighted: SearchResultItem = {
    type: 'doc',
    url: '/docs/tenant',
    title: 'Search &amp; <mark>analytics</mark> &lt;tenant&gt;',
    description: 'Set the &lt;tenant&gt; &amp; <mark>region</mark>.',
    score: 1,
    parents: [],
}

describe('AIAnswerPanel', () => {
    it('sends and links the plain text of a highlighted result', async () => {
        renderWithProviders(
            <AIAnswerPanel
                query="tenant"
                inputQuery="tenant"
                results={[highlighted]}
                visible
            />
        )

        await waitFor(() => expect(startAskAiStream).toHaveBeenCalled())
        const { message } = vi.mocked(startAskAiStream).mock.calls[0][0]
        expect(message).toContain('Title: Search & analytics <tenant>')
        expect(message).toContain('Content: Set the <tenant> & region.')
        // The sources sit in the expanded answer.
        await userEvent.click(
            (await screen.findByText('read more')).closest('button')!
        )
        expect(
            await screen.findByRole('link', {
                name: 'Search & analytics <tenant>',
            })
        ).toBeTruthy()
    })
})
