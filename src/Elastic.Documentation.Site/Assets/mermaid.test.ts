import { normalizeToXml, sanitizeSvgNode } from './mermaid'

// ---------------------------------------------------------------------------
// Test helpers
// ---------------------------------------------------------------------------

const SVG_NS = 'http://www.w3.org/2000/svg'

const wrapSvg = (inner: string) => `<svg xmlns="${SVG_NS}">${inner}</svg>`

const createRect = () => ({
    width: 100,
    height: 60,
    top: 0,
    left: 0,
    bottom: 60,
    right: 100,
    x: 0,
    y: 0,
    toJSON: () => ({}),
})

const setVisibleRect = (element: HTMLElement) => {
    Object.defineProperty(element, 'getBoundingClientRect', {
        value: () => createRect(),
    })
    Object.defineProperty(element, 'getClientRects', {
        value: () => [createRect()],
    })
}

class MockIntersectionObserver {
    callback: IntersectionObserverCallback
    observe = jest.fn()
    unobserve = jest.fn()
    disconnect = jest.fn()

    constructor(callback: IntersectionObserverCallback) {
        this.callback = callback
    }
}

const setupMermaid = () => {
    window.mermaid = {
        initialize: jest.fn(),
        render: jest.fn().mockResolvedValue({
            svg: `<svg xmlns="${SVG_NS}"></svg>`,
        }),
    }
}

const setupScriptLoad = () =>
    jest.spyOn(document.head, 'appendChild').mockImplementation((node) => {
        if (node instanceof HTMLScriptElement && node.onload) {
            node.onload(new Event('load'))
        }
        return node
    })

// ---------------------------------------------------------------------------
// normalizeToXml
// ---------------------------------------------------------------------------

describe('normalizeToXml', () => {
    it('converts <br> to self-closing <br/>', () => {
        expect(normalizeToXml('<br>')).toBe('<br/>')
    })

    it('converts <hr> to self-closing <hr/>', () => {
        expect(normalizeToXml('<hr>')).toBe('<hr/>')
    })

    it('preserves attributes when self-closing', () => {
        expect(normalizeToXml('<br class="x">')).toBe('<br class="x"/>')
        expect(normalizeToXml('<img src="a" alt="b">')).toBe(
            '<img src="a" alt="b"/>'
        )
    })

    it('leaves already self-closed elements unchanged', () => {
        expect(normalizeToXml('<br/>')).toBe('<br/>')
        expect(normalizeToXml('<br />')).toBe('<br />')
    })

    it('replaces &nbsp; with numeric entity', () => {
        expect(normalizeToXml('a&nbsp;b')).toBe('a&#160;b')
    })

    it('is case-insensitive', () => {
        expect(normalizeToXml('<BR>')).toBe('<BR/>')
        expect(normalizeToXml('<Hr>')).toBe('<Hr/>')
    })
})

// ---------------------------------------------------------------------------
// sanitizeSvgNode
// ---------------------------------------------------------------------------

describe('sanitizeSvgNode', () => {
    it('removes <script> elements', () => {
        const node = sanitizeSvgNode(
            wrapSvg('<script>alert(1)</script><rect/>')
        ) as Element
        expect(node.querySelector('script')).toBeNull()
        expect(node.querySelector('rect')).not.toBeNull()
    })

    it('removes on* event handler attributes', () => {
        const node = sanitizeSvgNode(
            wrapSvg('<rect onclick="alert(1)"/>')
        ) as Element
        expect(node.querySelector('rect')?.hasAttribute('onclick')).toBe(false)
    })

    it('removes javascript: URLs from href', () => {
        const node = sanitizeSvgNode(
            wrapSvg('<a href="javascript:alert(1)"><text>x</text></a>')
        ) as Element
        expect(node.querySelector('a')?.hasAttribute('href')).toBe(false)
    })

    it('removes javascript: URLs with leading whitespace', () => {
        const node = sanitizeSvgNode(
            wrapSvg('<a href="  javascript:void(0)"><text>x</text></a>')
        ) as Element
        expect(node.querySelector('a')?.hasAttribute('href')).toBe(false)
    })

    it('preserves safe href values', () => {
        const node = sanitizeSvgNode(
            wrapSvg('<a href="https://example.com"><text>x</text></a>')
        ) as Element
        expect(node.querySelector('a')?.getAttribute('href')).toBe(
            'https://example.com'
        )
    })

    it('removes <iframe> elements', () => {
        const node = sanitizeSvgNode(
            wrapSvg(
                '<foreignObject><iframe xmlns="http://www.w3.org/1999/xhtml" src="x"/></foreignObject>'
            )
        ) as Element
        expect(node.querySelector('iframe')).toBeNull()
    })

    it('removes <object> and <embed> elements', () => {
        const node = sanitizeSvgNode(
            wrapSvg('<object data="x"/><embed src="y"/>')
        ) as Element
        expect(node.querySelector('object')).toBeNull()
        expect(node.querySelector('embed')).toBeNull()
    })

    it('preserves foreignObject with text content', () => {
        const node = sanitizeSvgNode(
            wrapSvg(
                '<foreignObject>' +
                    '<div xmlns="http://www.w3.org/1999/xhtml">Hello</div>' +
                    '</foreignObject>'
            )
        ) as Element
        expect(node.querySelector('foreignObject')).not.toBeNull()
        expect(node.textContent).toContain('Hello')
    })

    it('handles <br> inside foreignObject via XML normalization', () => {
        const node = sanitizeSvgNode(
            wrapSvg(
                '<foreignObject>' +
                    '<div xmlns="http://www.w3.org/1999/xhtml">' +
                    '<p>line1<br>line2</p>' +
                    '</div>' +
                    '</foreignObject>'
            )
        ) as Element
        expect(node.querySelector('foreignObject')).not.toBeNull()
        expect(node.textContent).toContain('line1')
        expect(node.textContent).toContain('line2')
    })

    it('returns empty text node for malformed SVG', () => {
        const warnSpy = jest.spyOn(console, 'warn').mockImplementation(() => {})
        const node = sanitizeSvgNode('not valid svg <<<')
        expect(node.nodeType).toBe(Node.TEXT_NODE)
        expect(node.textContent).toBe('')
        warnSpy.mockRestore()
    })
})

// ---------------------------------------------------------------------------
// initMermaid
// ---------------------------------------------------------------------------

describe('initMermaid', () => {
    beforeEach(() => {
        jest.resetModules()
        document.body.innerHTML = ''
        global.IntersectionObserver =
            MockIntersectionObserver as unknown as typeof IntersectionObserver
    })

    afterEach(() => {
        jest.restoreAllMocks()
    })

    it('renders visible mermaid blocks immediately', async () => {
        setupMermaid()
        setupScriptLoad()

        const pre = document.createElement('pre')
        pre.className = 'mermaid'
        pre.textContent = 'flowchart LR\nA-->B'
        setVisibleRect(pre)
        document.body.appendChild(pre)

        const { initMermaid } = await import('./mermaid')
        await initMermaid()

        expect(window.mermaid.render).toHaveBeenCalledTimes(1)
    })

    it('renders mermaid blocks when a tab is activated', async () => {
        setupMermaid()
        setupScriptLoad()

        const tabs = document.createElement('div')
        tabs.className = 'tabs'

        const input = document.createElement('input')
        input.className = 'tabs-input'
        input.type = 'radio'
        input.checked = false

        const label = document.createElement('label')
        label.className = 'tabs-label'
        label.textContent = 'Rendered'

        const panel = document.createElement('div')
        panel.className = 'tabs-content'

        const pre = document.createElement('pre')
        pre.className = 'mermaid'
        pre.textContent = 'flowchart LR\nA-->B'
        setVisibleRect(pre)

        panel.appendChild(pre)
        tabs.append(input, label, panel)
        document.body.appendChild(tabs)

        const { initMermaid } = await import('./mermaid')
        await initMermaid()

        expect(window.mermaid.render).not.toHaveBeenCalled()

        input.checked = true
        input.dispatchEvent(new Event('change', { bubbles: true }))
        await new Promise((resolve) => setTimeout(resolve, 0))

        expect(window.mermaid.render).toHaveBeenCalledTimes(1)
    })

    it('retries rendering when viewBox indicates failed layout', async () => {
        const degenerate = `<svg xmlns="${SVG_NS}" viewBox="-8 -8 16 16"></svg>`
        const normal = `<svg xmlns="${SVG_NS}" viewBox="0 0 200 100"><rect/></svg>`

        window.mermaid = {
            initialize: jest.fn(),
            render: jest
                .fn()
                .mockResolvedValueOnce({ svg: degenerate })
                .mockResolvedValueOnce({ svg: normal }),
        }
        setupScriptLoad()

        jest.spyOn(window, 'requestAnimationFrame').mockImplementation((cb) => {
            cb(0)
            return 0
        })

        const pre = document.createElement('pre')
        pre.className = 'mermaid'
        pre.textContent = 'flowchart LR\nA-->B'
        setVisibleRect(pre)
        document.body.appendChild(pre)

        const { initMermaid } = await import('./mermaid')
        await initMermaid()

        expect(window.mermaid.render).toHaveBeenCalledTimes(2)
    })

    it('does nothing when no mermaid blocks exist', async () => {
        setupMermaid()
        const spy = setupScriptLoad()

        const { initMermaid } = await import('./mermaid')
        await initMermaid()

        expect(spy).not.toHaveBeenCalled()
    })
})
