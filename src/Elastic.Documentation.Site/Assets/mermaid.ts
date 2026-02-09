// Mermaid is loaded from local _static/ to avoid client-side CDN calls.
// The file is copied from node_modules during build (see package.json copy:mermaid).

declare global {
    interface Window {
        mermaid: {
            initialize: (config: Record<string, unknown>) => void
            render: (id: string, code: string) => Promise<{ svg: string }>
        }
    }
}

// ---------------------------------------------------------------------------
// State & types
// ---------------------------------------------------------------------------

interface DiagramState {
    zoom: number
    panX: number
    panY: number
    isDragging: boolean
    startX: number
    startY: number
}

let mermaidLoaded = false
let mermaidLoading: Promise<void> | null = null
let mermaidDiagramIndex = 0
let tabListenerAttached = false

// ---------------------------------------------------------------------------
// Constants
// ---------------------------------------------------------------------------

const ZOOM_MIN = 0.5
const ZOOM_MAX = 3
const ZOOM_STEP = 0.25

/** Selector for unprocessed Mermaid code blocks. */
const MERMAID_SELECTOR = 'pre.mermaid:not([data-mermaid-processed])'

/**
 * HTML void elements that must be self-closed for strict XML parsing.
 * Mermaid emits `<br>` inside `<foreignObject>` text labels; the
 * `image/svg+xml` parser requires `<br/>`.
 */
const VOID_ELEMENT_RE =
    /<(br|hr|img|input|wbr|area|base|col|embed|link|meta|source|track)(\s[^>]*?)?\s*(?<!\/)>/gi

/** Attributes that can carry URLs — potential `javascript:` XSS vectors. */
const URL_ATTRS = new Set([
    'href',
    'xlink:href',
    'src',
    'data',
    'formaction',
    'action',
    'poster',
])

/** Elements that should never appear inside diagram SVG. */
const DANGEROUS_ELEMENTS = ['script', 'iframe', 'object', 'embed', 'form']

// SVG icons extracted from @elastic/eui to match the Elastic design system.
const icons = {
    zoomIn: `<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16" fill="currentColor"><path d="M7 6h2v1H7v2H6V7H4V6h2V4h1v2Z"/><path d="M6.5 1a5.5 5.5 0 0 1 4.729 8.308l3.421 2.933a1 1 0 0 1 .057 1.466l-1 1a1 1 0 0 1-1.466-.057l-2.933-3.42A5.5 5.5 0 1 1 6.5 1Zm4.139 9.12a5.516 5.516 0 0 1-.52.519L13 14l1-1-3.361-2.88ZM6.5 2a4.5 4.5 0 1 0 .314 8.987c.024-.001.047-.004.07-.006.207-.017.41-.048.607-.092l.066-.016a4.41 4.41 0 0 0 .588-.185c.012-.006.026-.01.039-.015.194-.079.382-.171.562-.275l.03-.017a4.52 4.52 0 0 0 1.605-1.605c.006-.01.01-.02.017-.03.104-.18.196-.368.275-.562l.018-.048c.074-.188.134-.38.182-.58l.016-.065a4.49 4.49 0 0 0 .093-.61l.005-.067a4.544 4.544 0 0 0 .007-.545A4.5 4.5 0 0 0 6.5 2Z"/></svg>`,
    zoomOut: `<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16" fill="currentColor"><path d="M9 7H4V6h5v1Z"/><path d="M6.5 1a5.5 5.5 0 0 1 4.729 8.308l3.421 2.933a1 1 0 0 1 .057 1.466l-1 1a1 1 0 0 1-1.466-.057l-2.933-3.42A5.5 5.5 0 1 1 6.5 1Zm4.139 9.12a5.516 5.516 0 0 1-.52.519L13 14l1-1-3.361-2.88ZM6.5 2a4.5 4.5 0 1 0 .314 8.987c.024-.001.047-.004.07-.006.207-.017.41-.048.607-.092l.066-.016a4.41 4.41 0 0 0 .588-.185c.012-.006.026-.01.039-.015.194-.079.382-.171.562-.275l.03-.017a4.52 4.52 0 0 0 1.605-1.605c.006-.01.01-.02.017-.03.104-.18.196-.368.275-.562l.018-.048c.074-.188.134-.38.182-.58l.016-.065a4.49 4.49 0 0 0 .093-.61l.005-.067a4.544 4.544 0 0 0 .007-.545A4.5 4.5 0 0 0 6.5 2Z"/></svg>`,
    reset: `<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16" fill="currentColor"><path d="M2 8a5.98 5.98 0 0 0 1.757 4.243A5.98 5.98 0 0 0 8 14v1a6.98 6.98 0 0 1-4.95-2.05A6.98 6.98 0 0 1 1 8c0-1.79.683-3.58 2.048-4.947l.004-.004.019-.02L3.1 3H1V2h4v4H4V3.525a6.51 6.51 0 0 0-.22.21l-.013.013-.003.002-.007.007A5.98 5.98 0 0 0 2 8Zm10.243-4.243A5.98 5.98 0 0 0 8 2V1a6.98 6.98 0 0 1 4.95 2.05A6.98 6.98 0 0 1 15 8a6.98 6.98 0 0 1-2.047 4.947l-.005.004-.018.02-.03.029H15v1h-4v-4h1v2.475a6.744 6.744 0 0 0 .22-.21l.013-.013.003-.002.007-.007A5.98 5.98 0 0 0 14 8a5.98 5.98 0 0 0-1.757-4.243Z"/></svg>`,
    fullscreen: `<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16" fill="currentColor"><path d="M13 3v4h-1V4H9V3h4ZM3 3h4v1H4v3H3V3Zm10 10H9v-1h3V9h1v4ZM3 13V9h1v3h3v1H3ZM0 1.994C0 .893.895 0 1.994 0h12.012C15.107 0 16 .895 16 1.994v12.012A1.995 1.995 0 0 1 14.006 16H1.994A1.995 1.995 0 0 1 0 14.006V1.994Zm1 0v12.012c0 .548.446.994.994.994h12.012a.995.995 0 0 0 .994-.994V1.994A.995.995 0 0 0 14.006 1H1.994A.995.995 0 0 0 1 1.994Z"/></svg>`,
    close: `<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16" fill="currentColor"><path d="M7.293 8 2.646 3.354l.708-.708L8 7.293l4.646-4.647.708.708L8.707 8l4.647 4.646-.707.708L8 8.707l-4.646 4.647-.708-.707L7.293 8Z"/></svg>`,
}

// ---------------------------------------------------------------------------
// SVG sanitization
// ---------------------------------------------------------------------------

/**
 * Normalize HTML-style void elements to self-closing XML form so that
 * `DOMParser` can parse the SVG with the strict `image/svg+xml` parser.
 */
export function normalizeToXml(svg: string): string {
    return svg.replace(VOID_ELEMENT_RE, '<$1$2/>').replace(/&nbsp;/g, '&#160;')
}

/**
 * Parse an SVG string into a sanitized DOM node.
 *
 * Parses as `image/svg+xml` (after normalizing HTML void elements) to
 * avoid the CodeQL "DOM text reinterpreted as HTML" pattern. The sanitizer
 * strips dangerous elements (`<script>`, `<iframe>`, etc.), `on*` event
 * handler attributes, and `javascript:` URLs.
 */
export function sanitizeSvgNode(svg: string): Node {
    const doc = new DOMParser().parseFromString(
        normalizeToXml(svg),
        'image/svg+xml'
    )

    // The XML parser inserts <parsererror> on malformed input
    if (doc.querySelector('parsererror')) {
        console.warn('Mermaid SVG failed XML parsing; diagram may not render.')
        return document.createTextNode('')
    }

    const root = doc.documentElement

    for (const tag of DANGEROUS_ELEMENTS) {
        for (const el of root.querySelectorAll(tag)) {
            el.remove()
        }
    }

    for (const el of root.querySelectorAll('*')) {
        for (const attr of [...el.attributes]) {
            const name = attr.name.toLowerCase()
            if (name.startsWith('on')) {
                el.removeAttribute(attr.name)
            } else if (
                URL_ATTRS.has(name) &&
                /^\s*javascript\s*:/i.test(attr.value)
            ) {
                el.removeAttribute(attr.name)
            }
        }
    }

    return document.importNode(root, true)
}

// ---------------------------------------------------------------------------
// Asset loading
// ---------------------------------------------------------------------------

function getStaticBasePath(): string {
    for (const script of document.querySelectorAll('script[src*="main.js"]')) {
        const src = script.getAttribute('src')
        if (src) {
            const match = src.match(/^(.*\/_static\/)/)
            if (match) return match[1]
        }
    }
    return '/_static/'
}

async function waitForFonts(): Promise<void> {
    if (!document.fonts?.ready) return
    try {
        await document.fonts.ready
    } catch {
        // Ignore font loading failures
    }
}

async function loadMermaid(): Promise<void> {
    if (mermaidLoaded) return
    if (mermaidLoading) return mermaidLoading

    mermaidLoading = new Promise((resolve, reject) => {
        const script = document.createElement('script')
        script.src = getStaticBasePath() + 'mermaid.min.js'
        script.async = true
        script.onload = () => {
            mermaidLoaded = true
            window.mermaid.initialize({ startOnLoad: false, theme: 'default' })
            resolve()
        }
        script.onerror = () => reject(new Error('Failed to load Mermaid'))
        document.head.appendChild(script)
    })

    return mermaidLoading
}

// ---------------------------------------------------------------------------
// Tab helpers
// ---------------------------------------------------------------------------

/**
 * Walk backwards from a `.tabs-content` panel to find its associated radio
 * input. Tolerates extra elements between input / label / panel.
 */
function getTabInputForElement(element: HTMLElement): HTMLInputElement | null {
    const panel = element.closest('.tabs-content')
    if (!panel) return null

    let sibling = panel.previousElementSibling
    while (sibling) {
        if (
            sibling instanceof HTMLInputElement &&
            sibling.classList.contains('tabs-input')
        ) {
            return sibling
        }
        // Another panel means we've passed the boundary of this tab item
        if (sibling.classList.contains('tabs-content')) break
        sibling = sibling.previousElementSibling
    }

    return null
}

/**
 * Walk forwards from a radio input to find its `.tabs-content` panel,
 * stopping at the next input boundary.
 */
function getTabPanelForInput(input: HTMLInputElement): HTMLElement | null {
    let sibling = input.nextElementSibling
    while (sibling) {
        if (
            sibling instanceof HTMLElement &&
            sibling.classList.contains('tabs-content')
        ) {
            return sibling
        }
        if (
            sibling instanceof HTMLInputElement &&
            sibling.classList.contains('tabs-input')
        ) {
            break
        }
        sibling = sibling.nextElementSibling
    }

    return null
}

function isElementVisible(element: HTMLElement): boolean {
    const tabInput = getTabInputForElement(element)
    if (tabInput && !tabInput.checked) return false

    const rect = element.getBoundingClientRect()
    if (rect.width === 0 || rect.height === 0) return false

    return element.getClientRects().length > 0
}

function attachTabChangeListener(): void {
    if (tabListenerAttached) return
    tabListenerAttached = true

    document.addEventListener('change', (event) => {
        const target = event.target
        if (
            !(target instanceof HTMLInputElement) ||
            !target.classList.contains('tabs-input') ||
            !target.checked
        ) {
            return
        }

        const panel = getTabPanelForInput(target)
        if (!panel) return

        for (const node of panel.querySelectorAll(MERMAID_SELECTOR)) {
            void renderMermaidElement(node as HTMLElement)
        }
    })
}

// ---------------------------------------------------------------------------
// Rendering
// ---------------------------------------------------------------------------

/**
 * Render a Mermaid code block. If the first render returns the degenerate
 * `viewBox="-8 -8 16 16"` — Mermaid's fallback when it cannot compute
 * layout dimensions (common when fonts haven't loaded yet) — wait for
 * fonts and a repaint, then retry once.
 */
async function renderMermaidDiagram(
    id: string,
    content: string
): Promise<string> {
    const { svg } = await window.mermaid.render(id, content)
    if (!/viewBox="-8 -8 16 16"/.test(svg)) return svg

    await waitForFonts()
    await new Promise(requestAnimationFrame)

    const retry = await window.mermaid.render(id, content)
    return retry.svg
}

async function renderMermaidElement(element: HTMLElement): Promise<void> {
    const content = element.textContent?.trim()
    if (!content) return

    element.setAttribute('data-mermaid-processed', 'true')

    try {
        const diagramId = `mermaid-diagram-${mermaidDiagramIndex++}`
        const svg = await renderMermaidDiagram(diagramId, content)

        const container = document.createElement('div')
        container.className = 'mermaid-container'

        const viewport = document.createElement('div')
        viewport.className = 'mermaid-viewport'

        const rendered = document.createElement('div')
        rendered.className = 'mermaid-rendered'
        rendered.appendChild(sanitizeSvgNode(svg))

        viewport.appendChild(rendered)
        container.appendChild(viewport)

        setupControls(container, viewport, rendered, svg)
        element.replaceWith(container)
    } catch (err) {
        console.warn('Mermaid rendering error for diagram:', err)
        element.classList.add('mermaid-error')
    }
}

// ---------------------------------------------------------------------------
// Zoom / pan infrastructure
// ---------------------------------------------------------------------------

function clampZoom(value: number): number {
    return Math.max(ZOOM_MIN, Math.min(ZOOM_MAX, value))
}

function applyTransform(rendered: HTMLElement, state: DiagramState): void {
    rendered.style.transform = `scale(${state.zoom}) translate(${state.panX}px, ${state.panY}px)`
}

function createControlButton(
    icon: string,
    tooltip: string,
    className: string
): HTMLButtonElement {
    const button = document.createElement('button')
    button.type = 'button'
    button.className = `mermaid-btn ${className}`
    button.setAttribute('aria-label', tooltip)
    button.setAttribute('title', tooltip)
    button.innerHTML = icon
    return button
}

/**
 * Attach mouse-drag panning and Ctrl/Cmd+wheel zooming to a viewport.
 * Document-level listeners are only active while dragging to avoid
 * per-diagram overhead. Returns a cleanup function.
 */
function attachZoomPan(
    viewport: HTMLElement,
    rendered: HTMLElement,
    state: DiagramState
): () => void {
    const ac = new AbortController()
    const opts: AddEventListenerOptions = { signal: ac.signal }
    let dragAc: AbortController | null = null

    viewport.addEventListener(
        'mousedown',
        (e: MouseEvent) => {
            if (e.button !== 0) return
            e.preventDefault()

            state.isDragging = true
            state.startX = e.clientX - state.panX * state.zoom
            state.startY = e.clientY - state.panY * state.zoom
            viewport.classList.add('is-dragging')

            // Attach document listeners only for the duration of this drag
            dragAc = new AbortController()
            const dragOpts: AddEventListenerOptions = {
                signal: dragAc.signal,
            }

            document.addEventListener(
                'mousemove',
                (ev: MouseEvent) => {
                    state.panX = (ev.clientX - state.startX) / state.zoom
                    state.panY = (ev.clientY - state.startY) / state.zoom
                    applyTransform(rendered, state)
                },
                dragOpts
            )

            document.addEventListener(
                'mouseup',
                () => {
                    state.isDragging = false
                    viewport.classList.remove('is-dragging')
                    dragAc?.abort()
                    dragAc = null
                },
                dragOpts
            )
        },
        opts
    )

    viewport.addEventListener(
        'wheel',
        (e: WheelEvent) => {
            if (!e.ctrlKey && !e.metaKey) return
            e.preventDefault()
            state.zoom = clampZoom(
                state.zoom + (e.deltaY > 0 ? -ZOOM_STEP : ZOOM_STEP)
            )
            applyTransform(rendered, state)
        },
        { ...opts, passive: false }
    )

    return () => {
        ac.abort()
        dragAc?.abort()
    }
}

/**
 * Create zoom-in, zoom-out, and reset buttons wired to the given state.
 */
function createZoomButtons(
    rendered: HTMLElement,
    state: DiagramState,
    resetZoom: () => number
): HTMLButtonElement[] {
    const zoomInBtn = createControlButton(
        icons.zoomIn,
        'Zoom in',
        'mermaid-zoom-in'
    )
    const zoomOutBtn = createControlButton(
        icons.zoomOut,
        'Zoom out',
        'mermaid-zoom-out'
    )
    const resetBtn = createControlButton(
        icons.reset,
        'Reset view',
        'mermaid-reset'
    )

    zoomInBtn.addEventListener('click', () => {
        state.zoom = clampZoom(state.zoom + ZOOM_STEP)
        applyTransform(rendered, state)
    })

    zoomOutBtn.addEventListener('click', () => {
        state.zoom = clampZoom(state.zoom - ZOOM_STEP)
        applyTransform(rendered, state)
    })

    resetBtn.addEventListener('click', () => {
        state.zoom = resetZoom()
        state.panX = 0
        state.panY = 0
        applyTransform(rendered, state)
    })

    return [zoomInBtn, zoomOutBtn, resetBtn]
}

// ---------------------------------------------------------------------------
// Inline controls
// ---------------------------------------------------------------------------

function setupControls(
    container: HTMLElement,
    viewport: HTMLElement,
    rendered: HTMLElement,
    svgContent: string
): void {
    const state: DiagramState = {
        zoom: 1,
        panX: 0,
        panY: 0,
        isDragging: false,
        startX: 0,
        startY: 0,
    }

    const controls = document.createElement('div')
    controls.className = 'mermaid-controls'

    for (const btn of createZoomButtons(rendered, state, () => 1)) {
        controls.appendChild(btn)
    }

    const fullscreenBtn = createControlButton(
        icons.fullscreen,
        'View fullscreen',
        'mermaid-fullscreen'
    )
    fullscreenBtn.addEventListener('click', () =>
        openFullscreenModal(svgContent)
    )
    controls.appendChild(fullscreenBtn)

    container.insertBefore(controls, container.firstChild)

    attachZoomPan(viewport, rendered, state)
}

// ---------------------------------------------------------------------------
// Fullscreen modal
// ---------------------------------------------------------------------------

function calculateFitScale(
    svgWidth: number,
    svgHeight: number,
    viewportWidth: number,
    viewportHeight: number
): number {
    const padding = 80
    const scaleX = (viewportWidth - padding) / svgWidth
    const scaleY = (viewportHeight - padding) / svgHeight
    return Math.max(1, Math.min(scaleX, scaleY))
}

function openFullscreenModal(svgContent: string): void {
    const modal = document.createElement('div')
    modal.className = 'mermaid-modal'

    const modalContent = document.createElement('div')
    modalContent.className = 'mermaid-modal-content'

    const closeBtn = document.createElement('button')
    closeBtn.type = 'button'
    closeBtn.className = 'mermaid-modal-close'
    closeBtn.setAttribute('aria-label', 'Close fullscreen')
    closeBtn.innerHTML = icons.close

    const viewport = document.createElement('div')
    viewport.className = 'mermaid-modal-viewport'

    const rendered = document.createElement('div')
    rendered.className = 'mermaid-rendered'
    rendered.appendChild(sanitizeSvgNode(svgContent))

    viewport.appendChild(rendered)
    modalContent.appendChild(closeBtn)
    modalContent.appendChild(viewport)
    modal.appendChild(modalContent)

    const state: DiagramState = {
        zoom: 1,
        panX: 0,
        panY: 0,
        isDragging: false,
        startX: 0,
        startY: 0,
    }

    let initialZoom = 1

    const controlsDiv = document.createElement('div')
    controlsDiv.className = 'mermaid-modal-controls'

    for (const btn of createZoomButtons(rendered, state, () => initialZoom)) {
        controlsDiv.appendChild(btn)
    }
    modalContent.appendChild(controlsDiv)

    const cleanupZoomPan = attachZoomPan(viewport, rendered, state)

    // Single AbortController for all modal-scoped listeners, ensuring
    // everything — including the Escape keydown handler — is cleaned up
    // regardless of how the modal is closed.
    const modalAc = new AbortController()
    const modalOpts: AddEventListenerOptions = { signal: modalAc.signal }

    const closeModal = () => {
        cleanupZoomPan()
        modalAc.abort()
        document.body.style.overflow = ''
        modal.remove()
    }

    closeBtn.addEventListener('click', closeModal, modalOpts)

    modal.addEventListener(
        'click',
        (e: MouseEvent) => {
            if (e.target === modal) closeModal()
        },
        modalOpts
    )

    document.addEventListener(
        'keydown',
        (e: KeyboardEvent) => {
            if (e.key === 'Escape') closeModal()
        },
        modalOpts
    )

    document.body.style.overflow = 'hidden'
    document.body.appendChild(modal)

    // Calculate fit-to-viewport zoom after the modal is in the DOM
    requestAnimationFrame(() => {
        const svg = rendered.querySelector('svg')
        if (svg) {
            const svgRect = svg.getBoundingClientRect()
            const vpRect = viewport.getBoundingClientRect()
            initialZoom = calculateFitScale(
                svgRect.width,
                svgRect.height,
                vpRect.width,
                vpRect.height
            )
            state.zoom = initialZoom
            applyTransform(rendered, state)
        }
    })
}

// ---------------------------------------------------------------------------
// Entry point
// ---------------------------------------------------------------------------

export async function initMermaid() {
    const mermaidElements = document.querySelectorAll(MERMAID_SELECTOR)
    if (mermaidElements.length === 0) return

    try {
        await loadMermaid()
        await waitForFonts()
        attachTabChangeListener()

        const observer = new IntersectionObserver((entries, instance) => {
            for (const entry of entries) {
                if (!entry.isIntersecting) continue
                const target = entry.target as HTMLElement
                instance.unobserve(target)
                void renderMermaidElement(target)
            }
        })

        for (let i = 0; i < mermaidElements.length; i++) {
            const element = mermaidElements[i] as HTMLElement
            if (isElementVisible(element)) {
                await renderMermaidElement(element)
            } else {
                observer.observe(element)
            }
        }
    } catch (error) {
        console.warn('Mermaid initialization error:', error)
    }
}
