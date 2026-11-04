import { initCodeLines, splitLines } from './code-lines'

describe('splitLines', () => {
    it('wraps every line and keeps the newline inside it', () => {
        expect(splitLines('a\nb')).toEqual([
            '<span class="code-block__line">a\n</span>',
            '<span class="code-block__line">b</span>',
        ])
    })

    it('closes a token at the end of a line and reopens it on the next', () => {
        const [first, second] = splitLines(
            '<span class="hljs-comment">/* one\ntwo */</span>'
        )
        expect(first).toBe(
            '<span class="code-block__line"><span class="hljs-comment">/* one</span>\n</span>'
        )
        expect(second).toBe(
            '<span class="code-block__line"><span class="hljs-comment">two */</span></span>'
        )
    })

    it('does not add an empty line for a trailing newline', () => {
        expect(splitLines('a\n')).toHaveLength(1)
    })
})

describe('initCodeLines', () => {
    it('counts the lines and leaves the text as it was', () => {
        document.body.innerHTML =
            '<figure class="code-block"><pre><code>one\ntwo\nthree</code></pre></figure>'
        initCodeLines()

        expect(document.querySelector('code')?.dataset.lines).toBe('3')
        expect(document.querySelector('code')?.textContent).toBe(
            'one\ntwo\nthree'
        )
        expect(document.querySelectorAll('.code-block__line')).toHaveLength(3)
    })

    it('numbers a long block and prompts a one-line shell block', () => {
        document.body.innerHTML =
            '<figure class="code-block" data-language="json"><pre><code>1\n2\n3\n4\n5\n6</code></pre></figure>' +
            '<figure class="code-block" data-language="sh"><pre><code>npm ci</code></pre></figure>'
        initCodeLines()

        const [long, shell] =
            document.querySelectorAll<HTMLElement>('.code-block')
        expect(long.dataset.numbered).toBeDefined()
        expect(long.dataset.prompt).toBeUndefined()
        expect(shell.dataset.prompt).toBeDefined()
        expect(shell.dataset.numbered).toBeUndefined()
    })

    it('does not wrap a block twice', () => {
        document.body.innerHTML =
            '<figure class="code-block"><pre><code>one\ntwo</code></pre></figure>'
        initCodeLines()
        initCodeLines()

        expect(document.querySelectorAll('.code-block__line')).toHaveLength(2)
    })
})
