import { initCodeLineNumbers } from './code-line-numbers'

function block(code: string, optIn: boolean): string {
    return `
        <div class="highlight-json"${optIn ? ' data-line-numbers' : ''}>
            <div class="highlight"><pre><code>${code}</code></pre></div>
        </div>
    `
}

describe('initCodeLineNumbers', () => {
    afterEach(() => {
        document.body.innerHTML = ''
    })

    it('adds a gutter with one number per line to opted-in blocks', () => {
        document.body.innerHTML = block('{\n  "a": 1\n}\n', true)

        initCodeLineNumbers()

        const gutter = document.querySelector('.code-line-gutter')
        expect(gutter?.textContent).toBe('1\n2\n3')
        expect(gutter?.getAttribute('aria-hidden')).toBe('true')
        const wrapper = document.querySelector('.code-lines')
        expect(wrapper?.children).toHaveLength(2)
        expect(wrapper?.lastElementChild?.querySelector('code')).not.toBeNull()
    })

    it('leaves blocks without data-line-numbers alone', () => {
        document.body.innerHTML = block('a\nb', false)

        initCodeLineNumbers()

        expect(document.querySelector('.code-lines')).toBeNull()
    })

    it('is idempotent across re-initialisation', () => {
        document.body.innerHTML = block('a\nb', true)

        initCodeLineNumbers()
        initCodeLineNumbers()

        expect(document.querySelectorAll('.code-line-gutter')).toHaveLength(1)
        expect(document.querySelectorAll('.code-lines')).toHaveLength(1)
    })

    it('counts an empty block as one line', () => {
        document.body.innerHTML = block('', true)

        initCodeLineNumbers()

        expect(document.querySelector('.code-line-gutter')?.textContent).toBe(
            '1'
        )
    })
})
