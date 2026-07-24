type HljsModule = typeof import('./hljs')

async function loadModule(): Promise<HljsModule> {
    // Re-import per test so the module-level registration cache and the shared hljs
    // singleton start clean, isolating what each test loads.
    jest.resetModules()
    return await import('./hljs')
}

function setCodeBlocks(...languages: (string | null)[]) {
    const blocks = languages
        .map((language) => {
            if (language === null) {
                return '<pre><code>const x = 1</code></pre>'
            }
            return `<pre><code class="language-${language}">echo hello</code></pre>`
        })
        .join('')
    document.body.innerHTML = `<div id="markdown-content">${blocks}</div>`
}

afterEach(() => {
    document.body.innerHTML = ''
})

describe('initHighlight', () => {
    it('loads all curated languages when any code block is present', async () => {
        const { initHighlight, hljs } = await loadModule()
        setCodeBlocks('bash')

        await initHighlight()

        expect(hljs.listLanguages()).toContain('bash')
        expect(hljs.listLanguages()).toContain('python')
        expect(hljs.listLanguages()).toContain('javascript')
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

    it('highlights alias language-js via javascript aliases', async () => {
        const { initHighlight } = await loadModule()
        setCodeBlocks('js')

        await initHighlight()

        const block = document.querySelector('#markdown-content pre code')
        expect(block?.getAttribute('data-highlighted')).toBe('yes')
        expect(block?.className).toContain('language-javascript')
    })

    it('highlights alias language-ts via typescript aliases', async () => {
        const { initHighlight } = await loadModule()
        setCodeBlocks('ts')

        await initHighlight()

        const block = document.querySelector('#markdown-content pre code')
        expect(block?.getAttribute('data-highlighted')).toBe('yes')
        expect(block?.className).toContain('language-typescript')
    })

    it('highlights alias language-jsx via javascript aliases', async () => {
        const { initHighlight } = await loadModule()
        setCodeBlocks('jsx')

        await initHighlight()

        const block = document.querySelector('#markdown-content pre code')
        expect(block?.getAttribute('data-highlighted')).toBe('yes')
        expect(block?.className).toContain('language-javascript')
    })

    it('autodetects an unlabeled code block', async () => {
        const { initHighlight } = await loadModule()
        setCodeBlocks(null)

        await initHighlight()

        const block = document.querySelector('#markdown-content pre code')
        expect(block?.getAttribute('data-highlighted')).toBe('yes')
        expect(block?.querySelector('.hljs-keyword')).not.toBeNull()
    })

    it('resolves sh alias to shell', async () => {
        const { initHighlight, hljs } = await loadModule()
        setCodeBlocks('sh')

        await initHighlight()

        expect(hljs.listLanguages()).toContain('shell')
        const block = document.querySelector('#markdown-content pre code')
        expect(block?.getAttribute('data-highlighted')).toBe('yes')
        expect(block?.className).toContain('language-shell')
    })

    it('does not re-highlight a block when invoked concurrently', async () => {
        const { initHighlight, hljs } = await loadModule()
        setCodeBlocks('bash')
        const spy = jest.spyOn(hljs, 'highlightElement')

        await Promise.all([initHighlight(), initHighlight()])

        const block = document.querySelector('#markdown-content pre code')
        const callsForBlock = spy.mock.calls.filter(
            (call) => call[0] === block
        ).length
        expect(callsForBlock).toBe(1)
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

describe('highlightCodeBlocks', () => {
    it('scopes highlighting to the supplied root', async () => {
        const { highlightCodeBlocks } = await loadModule()
        document.body.innerHTML = `
            <div id="inside"><pre><code class="language-bash">echo inside</code></pre></div>
            <div id="outside"><pre><code class="language-bash">echo outside</code></pre></div>
        `

        const inside = document.querySelector('#inside')!
        await highlightCodeBlocks(inside)

        expect(
            inside.querySelector('code')?.getAttribute('data-highlighted')
        ).toBe('yes')
        expect(
            document
                .querySelector('#outside code')
                ?.getAttribute('data-highlighted')
        ).not.toBe('yes')
    })
})

describe('toLanguageFn', () => {
    // Parcel's dynamic import() resolves a language module to the LanguageFn directly,
    // while Babel/Jest wrap it as { default: fn }. Both must yield the function; a
    // regression here silently breaks highlighting only in the Parcel production build.
    it('returns the function when the module is the function itself (Parcel)', async () => {
        const { toLanguageFn } = await loadModule()
        const fn = () => ({ contains: [] })

        expect(toLanguageFn(fn)).toBe(fn)
    })

    it('returns the default export when wrapped in a namespace (Babel/Jest)', async () => {
        const { toLanguageFn } = await loadModule()
        const fn = () => ({ contains: [] })

        expect(toLanguageFn({ default: fn })).toBe(fn)
    })
})
