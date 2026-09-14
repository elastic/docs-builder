import { MarkdownContent } from './MarkdownContent'
import { EuiProvider } from '@elastic/eui'
import { render, waitFor } from '@testing-library/react'

jest.mock('../../copybutton', () => ({
    initCopyButton: jest.fn(),
}))

const renderMarkdown = (content: string) =>
    render(
        <EuiProvider
            colorMode="light"
            globalStyles={false}
            utilityClasses={false}
        >
            <MarkdownContent content={content} enableCopyButtons={false} />
        </EuiProvider>
    )

describe('MarkdownContent', () => {
    it('highlights alias fences after mount', async () => {
        renderMarkdown('```js\nconst x = 1\n```')

        await waitFor(() => {
            const block = document.querySelector('.markdown-content pre code')
            expect(block?.getAttribute('data-highlighted')).toBe('yes')
            expect(block?.className).toContain('language-javascript')
        })
    })

    it('autodetects unlabeled fences after mount', async () => {
        renderMarkdown('```\nconst x = 1\n```')

        await waitFor(() => {
            const block = document.querySelector('.markdown-content pre code')
            expect(block?.getAttribute('data-highlighted')).toBe('yes')
            expect(block?.querySelector('.hljs-keyword')).not.toBeNull()
        })
    })
})
