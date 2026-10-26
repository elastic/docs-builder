import { decorateApiCodeTokens } from './api-code-tokens'

function cardCode(language: string, html: string): HTMLElement {
    document.body.innerHTML = `
		<div class="api-code-card">
			<pre><code class="language-${language} hljs" data-highlighted="yes">${html}</code></pre>
		</div>
	`
    return document.querySelector('code') as HTMLElement
}

function texts(code: HTMLElement, selector: string): string[] {
    return [...code.querySelectorAll(selector)].map(
        (el) => el.textContent ?? ''
    )
}

describe('decorateApiCodeTokens', () => {
    it('splits string quotes into punctuation and keeps the value', () => {
        const code = cardCode(
            'javascript',
            '<span class="hljs-keyword">const</span> response = <span class="hljs-keyword">await</span> client.<span class="hljs-title function_">search</span>({\n' +
                '  <span class="hljs-attr">index</span>: <span class="hljs-string">&quot;my-index-000001&quot;</span>,\n' +
                '  <span class="hljs-string">&quot;user.id&quot;</span>: <span class="hljs-string">&quot;kimchy&quot;</span>,\n' +
                '});'
        )

        decorateApiCodeTokens()

        expect(texts(code, '.hljs-variable')).toEqual(['response', 'client'])
        expect(texts(code, '.hljs-string')).toEqual([
            'my-index-000001',
            'kimchy',
        ])
        expect(texts(code, '.hljs-attr')).toEqual(['index', 'user.id'])
        expect(
            [...code.querySelectorAll('.hljs-punctuation')].filter(
                (el) => el.textContent === '"'
            )
        ).toHaveLength(6)
        expect(code.textContent).toContain('"my-index-000001"')
    })

    it('maps Python kwargs, calls, and quoted keys to the same roles', () => {
        const code = cardCode(
            'python',
            'resp = client.search(\n' +
                '    index=<span class="hljs-string">&quot;my-index-000001&quot;</span>,\n' +
                '    <span class="hljs-keyword">from</span>=<span class="hljs-string">&quot;40&quot;</span>,\n' +
                '    query={\n' +
                '        <span class="hljs-string">&quot;term&quot;</span>: {\n' +
                '            <span class="hljs-string">&quot;user.id&quot;</span>: <span class="hljs-string">&quot;kimchy&quot;</span>\n' +
                '        }\n' +
                '    },\n' +
                ')'
        )

        decorateApiCodeTokens()

        expect(texts(code, '.hljs-variable')).toEqual(['resp', 'client'])
        expect(texts(code, '.hljs-title')).toEqual(['search'])
        expect(texts(code, '.hljs-attr')).toEqual([
            'index',
            'from',
            'query',
            'term',
            'user.id',
        ])
        expect(texts(code, '.hljs-string')).toEqual([
            'my-index-000001',
            '40',
            'kimchy',
        ])
        expect(code.querySelector('.hljs-keyword')).toBeNull()
    })

    it('treats Ruby symbol keys as properties and splits the colon', () => {
        const code = cardCode(
            'ruby',
            'response = client.search(\n' +
                '  <span class="hljs-symbol">index:</span> <span class="hljs-string">&quot;my-index-000001&quot;</span>\n' +
                ')'
        )

        decorateApiCodeTokens()

        expect(texts(code, '.hljs-variable')).toEqual(['response', 'client'])
        expect(texts(code, '.hljs-title')).toEqual(['search'])
        expect(texts(code, '.hljs-attr')).toEqual(['index'])
        expect(texts(code, '.hljs-symbol')).toEqual([])
        expect(texts(code, '.hljs-punctuation')).toContain(':')
    })

    it('marks PHP => quoted keys as properties', () => {
        const code = cardCode(
            'php',
            '<span class="hljs-variable">$resp</span> = <span class="hljs-variable">$client</span>-&gt;<span class="hljs-title function_ invoke__">search</span>([\n' +
                '    <span class="hljs-string">&quot;index&quot;</span> =&gt; <span class="hljs-string">&quot;my-index-000001&quot;</span>,\n' +
                ']);'
        )

        decorateApiCodeTokens()

        expect(texts(code, '.hljs-attr')).toEqual(['index'])
        expect(texts(code, '.hljs-string')).toEqual(['my-index-000001'])
    })

    it('splits JSON attribute quotes used in response bodies', () => {
        const code = cardCode(
            'json',
            '<span class="hljs-punctuation">{</span><span class="hljs-attr">&quot;query&quot;</span><span class="hljs-punctuation">:</span> <span class="hljs-string">&quot;kimchy&quot;</span><span class="hljs-punctuation">}</span>'
        )

        decorateApiCodeTokens()

        expect(texts(code, '.hljs-attr')).toEqual(['query'])
        expect(texts(code, '.hljs-string')).toEqual(['kimchy'])
        expect(
            [...code.querySelectorAll('.hljs-punctuation')].map(
                (el) => el.textContent
            )
        ).toEqual(['{', '"', '"', ':', '"', '"', '}'])
    })

    it('does not decorate unhighlighted blocks', () => {
        document.body.innerHTML = `
			<div class="api-code-card">
				<pre><code class="language-javascript">const response = 1</code></pre>
			</div>
		`

        decorateApiCodeTokens()

        expect(document.querySelector('.hljs-variable')).toBeNull()
    })
})
