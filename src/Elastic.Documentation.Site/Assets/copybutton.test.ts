import { initCopyButton } from './copybutton'

const plain = `
    <div class="highlight"><pre><code>echo hi<span class="code-callout" data-index="1"></span></code></pre></div>
`

const card = `
    <div data-code-card>
        <div data-code-actions></div>
        <div data-code-panel="Console">
            <div class="highlight"><pre><code>GET /</code></pre></div>
        </div>
        <div data-code-panel="Python" hidden>
            <div class="highlight"><pre><code>client.get()</code></pre></div>
        </div>
    </div>
`

describe('initCopyButton', () => {
    afterEach(() => {
        document.body.innerHTML = ''
    })

    it('overlays the button next to the code when there is no card', () => {
        document.body.innerHTML = plain

        initCopyButton()

        const pre = document.querySelector('pre') as HTMLElement
        const button = pre.nextElementSibling
        expect(button?.classList.contains('copybtn')).toBe(true)
        expect(button?.getAttribute('data-clipboard-target')).toBe(`#${pre.id}`)
    })

    it('mounts the button in the card actions slot and follows its panel', () => {
        document.body.innerHTML = card

        initCopyButton()

        const buttons = document.querySelectorAll<HTMLButtonElement>(
            '[data-code-actions] .copybtn'
        )
        expect(buttons).toHaveLength(2)
        expect(buttons[0].dataset.codePanel).toBe('Console')
        expect(buttons[0].hidden).toBe(false)
        expect(buttons[1].dataset.codePanel).toBe('Python')
        expect(buttons[1].hidden).toBe(true)
        expect(document.querySelectorAll('pre + .copybtn')).toHaveLength(0)
    })

    it('does not add a button to the line-number gutter on re-init', () => {
        document.body.innerHTML = `
            <div class="highlight"><div class="code-lines">
                <div class="code-line-gutter">1\n2</div>
                <pre><code>a\nb</code></pre>
            </div></div>
        `

        initCopyButton()
        initCopyButton()

        expect(document.querySelectorAll('.copybtn')).toHaveLength(1)
    })
})
