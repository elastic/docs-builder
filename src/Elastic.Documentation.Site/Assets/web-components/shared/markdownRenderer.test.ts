import { markdownRenderer } from './markdownRenderer'

describe('markdownRenderer', () => {
    it('wraps and escapes fenced code with its language class', () => {
        const html = markdownRenderer.parse(
            '```jsx\nconst element = <div>test</div>\n```',
            { async: false }
        )

        expect(html).toBe(
            '<div class="highlight"><pre><code class="language-jsx">const element = &lt;div&gt;test&lt;/div&gt;\n</code></pre>\n</div>'
        )
    })
})
