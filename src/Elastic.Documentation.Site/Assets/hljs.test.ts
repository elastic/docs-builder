type HljsModule = typeof import('./hljs')

async function loadModule(): Promise<HljsModule> {
    // Re-import per test so the module-level registration cache and the shared hljs
    // singleton start clean, isolating what each test loads.
    jest.resetModules()
    return await import('./hljs')
}

function setCodeBlocks(...languages: string[]) {
    const blocks = languages
        .map(
            (language) =>
                `<pre><code class="language-${language}">echo hello</code></pre>`
        )
        .join('')
    document.body.innerHTML = `<div id="markdown-content">${blocks}</div>`
}

afterEach(() => {
    document.body.innerHTML = ''
})

describe('initHighlight', () => {
    it('loads only the languages referenced on the page', async () => {
        const { initHighlight, hljs } = await loadModule()
        setCodeBlocks('bash')

        await initHighlight()

        expect(hljs.listLanguages()).toContain('bash')
        expect(hljs.listLanguages()).not.toContain('python')
    })

    it('loads no node_modules language when there are no code blocks', async () => {
        const { initHighlight, hljs } = await loadModule()
        document.body.innerHTML = '<div id="markdown-content"></div>'

        await initHighlight()

        expect(hljs.listLanguages()).not.toContain('bash')
        expect(hljs.listLanguages()).not.toContain('python')
    })

    it('highlights a supported language block', async () => {
        const { initHighlight } = await loadModule()
        setCodeBlocks('bash')

        await initHighlight()

        const block = document.querySelector('#markdown-content pre code')
        expect(block?.getAttribute('data-highlighted')).toBe('yes')
    })

    it('loads a newly encountered language on a later swap', async () => {
        const { initHighlight, hljs } = await loadModule()
        setCodeBlocks('bash')
        await initHighlight()
        expect(hljs.listLanguages()).not.toContain('python')

        // Simulate an htmx swap introducing a language not present initially.
        setCodeBlocks('python')
        await initHighlight()

        expect(hljs.listLanguages()).toContain('python')
    })

    it('resolves aliases to their canonical language', async () => {
        const { initHighlight, hljs } = await loadModule()
        setCodeBlocks('sh')

        await initHighlight()

        expect(hljs.listLanguages()).toContain('shell')
    })

    it('ignores unknown languages without breaking other blocks', async () => {
        const { initHighlight } = await loadModule()
        setCodeBlocks('this-language-does-not-exist', 'bash')

        await expect(initHighlight()).resolves.toBeUndefined()

        const blocks = document.querySelectorAll('#markdown-content pre code')
        const bashBlock = blocks[1]
        expect(bashBlock.getAttribute('data-highlighted')).toBe('yes')
    })
})
