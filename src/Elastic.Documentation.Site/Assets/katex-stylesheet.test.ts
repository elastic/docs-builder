import { loadKatexStylesheet } from './katex-stylesheet'

vi.mock('./katex.css?url', () => ({ default: './katex-test.css' }))

describe('loadKatexStylesheet', () => {
    beforeEach(() => {
        document.head.innerHTML = ''
    })

    it('adds a stylesheet link for the KaTeX CSS and resolves when it loads', async () => {
        const promise = loadKatexStylesheet()
        const link = document.head.querySelector<HTMLLinkElement>(
            'link[data-katex-stylesheet]'
        )

        expect(link?.rel).toBe('stylesheet')
        expect(link?.getAttribute('href')).toBe('./katex-test.css')

        link?.dispatchEvent(new Event('load'))
        await expect(promise).resolves.toBeUndefined()
    })

    it('does not add a second link when called again', async () => {
        void loadKatexStylesheet()
        await loadKatexStylesheet()

        expect(
            document.head.querySelectorAll('link[data-katex-stylesheet]')
        ).toHaveLength(1)
    })
})
