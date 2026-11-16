import { initMath } from './math'

vi.mock('./katex-stylesheet', () => ({
    loadKatexStylesheet: () => Promise.resolve(),
}))

const source = String.raw`\text{num\_vectors} \times m`

describe('initMath', () => {
    beforeEach(() => {
        document.body.innerHTML = `<span class="math">${source}</span>`
    })

    it('renders the TeX source as MathML', async () => {
        await initMath()

        const element = document.querySelector('.math')!
        expect(element.getAttribute('data-katex-processed')).toBe('true')
        expect(element.querySelector('mtext')?.textContent).toBe('num_vectors')
    })

    it('renders once when the initial load and htmx:load run it together', async () => {
        await Promise.all([initMath(), initMath()])

        const annotation = document.querySelector('.math annotation')
        expect(annotation?.textContent).toBe(source)
        expect(document.querySelectorAll('.math math')).toHaveLength(1)
    })
})
