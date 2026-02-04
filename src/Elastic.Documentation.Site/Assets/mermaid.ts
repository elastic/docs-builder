// Beautiful Mermaid is loaded from local _static/ to avoid client-side CDN calls
// The file is copied from node_modules during build (see package.json copy:mermaid)

// Type declaration for beautiful-mermaid browser global
declare global {
    interface Window {
        __mermaid: {
            renderMermaid: (
                code: string,
                options?: {
                    bg?: string
                    fg?: string
                    font?: string
                    transparent?: boolean
                    line?: string
                    accent?: string
                    muted?: string
                    surface?: string
                    border?: string
                }
            ) => Promise<string>
        }
    }
}

let mermaidLoaded = false
let mermaidLoading: Promise<void> | null = null

// High-contrast theme configuration
// beautiful-mermaid generates CSS variables that don't resolve correctly in all contexts,
// so we resolve them to actual colors during post-processing
const colors = {
    background: '#FFFFFF',
    foreground: '#000000',
    nodeFill: '#F5F5F5',
    nodeStroke: '#000000',
    line: '#000000',
    innerStroke: '#333333',
}

// Map CSS variables to resolved colors
const variableReplacements: Record<string, string> = {
    '--_text': colors.foreground,
    '--_text-sec': colors.foreground,
    '--_text-muted': colors.foreground,
    '--_text-faint': colors.foreground, // "+ ", ": ", "(no attributes)"
    '--_line': colors.line,
    '--_arrow': colors.foreground,
    '--_node-fill': colors.nodeFill,
    '--_node-stroke': colors.nodeStroke,
    '--_inner-stroke': colors.innerStroke,
    '--bg': colors.background,
}

// Zoom configuration
const ZOOM_MIN = 0.5
const ZOOM_MAX = 3
const ZOOM_STEP = 0.25

// SVG icons from @elastic/eui for controls
// These are extracted from EUI icon assets to match the Elastic design system
const icons = {
    // EUI magnifyWithPlus icon
    zoomIn: `<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16" fill="currentColor"><path d="M7 6h2v1H7v2H6V7H4V6h2V4h1v2Z"/><path d="M6.5 1a5.5 5.5 0 0 1 4.729 8.308l3.421 2.933a1 1 0 0 1 .057 1.466l-1 1a1 1 0 0 1-1.466-.057l-2.933-3.42A5.5 5.5 0 1 1 6.5 1Zm4.139 9.12a5.516 5.516 0 0 1-.52.519L13 14l1-1-3.361-2.88ZM6.5 2a4.5 4.5 0 1 0 .314 8.987c.024-.001.047-.004.07-.006.207-.017.41-.048.607-.092l.066-.016a4.41 4.41 0 0 0 .588-.185c.012-.006.026-.01.039-.015.194-.079.382-.171.562-.275l.03-.017a4.52 4.52 0 0 0 1.605-1.605c.006-.01.01-.02.017-.03.104-.18.196-.368.275-.562l.018-.048c.074-.188.134-.38.182-.58l.016-.065a4.49 4.49 0 0 0 .093-.61l.005-.067a4.544 4.544 0 0 0 .007-.545A4.5 4.5 0 0 0 6.5 2Z"/></svg>`,
    // EUI magnifyWithMinus icon
    zoomOut: `<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16" fill="currentColor"><path d="M9 7H4V6h5v1Z"/><path d="M6.5 1a5.5 5.5 0 0 1 4.729 8.308l3.421 2.933a1 1 0 0 1 .057 1.466l-1 1a1 1 0 0 1-1.466-.057l-2.933-3.42A5.5 5.5 0 1 1 6.5 1Zm4.139 9.12a5.516 5.516 0 0 1-.52.519L13 14l1-1-3.361-2.88ZM6.5 2a4.5 4.5 0 1 0 .314 8.987c.024-.001.047-.004.07-.006.207-.017.41-.048.607-.092l.066-.016a4.41 4.41 0 0 0 .588-.185c.012-.006.026-.01.039-.015.194-.079.382-.171.562-.275l.03-.017a4.52 4.52 0 0 0 1.605-1.605c.006-.01.01-.02.017-.03.104-.18.196-.368.275-.562l.018-.048c.074-.188.134-.38.182-.58l.016-.065a4.49 4.49 0 0 0 .093-.61l.005-.067a4.544 4.544 0 0 0 .007-.545A4.5 4.5 0 0 0 6.5 2Z"/></svg>`,
    // EUI refresh icon
    reset: `<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16" fill="currentColor"><path d="M2 8a5.98 5.98 0 0 0 1.757 4.243A5.98 5.98 0 0 0 8 14v1a6.98 6.98 0 0 1-4.95-2.05A6.98 6.98 0 0 1 1 8c0-1.79.683-3.58 2.048-4.947l.004-.004.019-.02L3.1 3H1V2h4v4H4V3.525a6.51 6.51 0 0 0-.22.21l-.013.013-.003.002-.007.007A5.98 5.98 0 0 0 2 8Zm10.243-4.243A5.98 5.98 0 0 0 8 2V1a6.98 6.98 0 0 1 4.95 2.05A6.98 6.98 0 0 1 15 8a6.98 6.98 0 0 1-2.047 4.947l-.005.004-.018.02-.03.029H15v1h-4v-4h1v2.475a6.744 6.744 0 0 0 .22-.21l.013-.013.003-.002.007-.007A5.98 5.98 0 0 0 14 8a5.98 5.98 0 0 0-1.757-4.243Z"/></svg>`,
    // EUI fullScreen icon
    fullscreen: `<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16" fill="currentColor"><path d="M13 3v4h-1V4H9V3h4ZM3 3h4v1H4v3H3V3Zm10 10H9v-1h3V9h1v4ZM3 13V9h1v3h3v1H3ZM0 1.994C0 .893.895 0 1.994 0h12.012C15.107 0 16 .895 16 1.994v12.012A1.995 1.995 0 0 1 14.006 16H1.994A1.995 1.995 0 0 1 0 14.006V1.994Zm1 0v12.012c0 .548.446.994.994.994h12.012a.995.995 0 0 0 .994-.994V1.994A.995.995 0 0 0 14.006 1H1.994A.995.995 0 0 0 1 1.994Z"/></svg>`,
    // EUI cross icon
    close: `<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16" fill="currentColor"><path d="M7.293 8 2.646 3.354l.708-.708L8 7.293l4.646-4.647.708.708L8.707 8l4.647 4.646-.707.708L8 8.707l-4.646 4.647-.708-.707L7.293 8Z"/></svg>`,
}

/**
 * State for a single mermaid diagram's zoom/pan
 */
interface DiagramState {
    zoom: number
    panX: number
    panY: number
    isDragging: boolean
    startX: number
    startY: number
}

/**
 * Resolve CSS variables to actual colors in the SVG output
 */
function resolveVariables(svg: string): string {
    let result = svg
    for (const [variable, color] of Object.entries(variableReplacements)) {
        const pattern = new RegExp(
            `(fill|stroke)="var\\(${variable.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')}\\)"`,
            'g'
        )
        result = result.replace(pattern, `$1="${color}"`)
    }
    return result
}

/**
 * Remove Google Fonts @import to avoid external network dependency
 */
function removeGoogleFonts(svg: string): string {
    return svg.replace(
        /@import url\('https:\/\/fonts\.googleapis\.com[^']*'\);\s*/g,
        ''
    )
}

/**
 * Get the base path for _static/ assets by finding main.js script location
 */
function getStaticBasePath(): string {
    // Find the main.js script element to get the correct path prefix
    const scripts = document.querySelectorAll('script[src*="main.js"]')
    for (const script of scripts) {
        const src = script.getAttribute('src')
        if (src) {
            // Extract path up to and including _static/
            const match = src.match(/^(.*\/_static\/)/)
            if (match) {
                return match[1]
            }
        }
    }
    // Fallback for local development
    return '/_static/'
}

/**
 * Lazy-load Beautiful Mermaid from local _static/ only when diagrams exist on the page
 */
async function loadMermaid(): Promise<void> {
    if (mermaidLoaded) return
    if (mermaidLoading) return mermaidLoading

    mermaidLoading = new Promise((resolve, reject) => {
        const script = document.createElement('script')
        script.src = getStaticBasePath() + 'mermaid.min.js'
        script.async = true
        script.onload = () => {
            mermaidLoaded = true
            resolve()
        }
        script.onerror = () =>
            reject(new Error('Failed to load Beautiful Mermaid'))
        document.head.appendChild(script)
    })

    return mermaidLoading
}

/**
 * Create a control button with icon and tooltip
 */
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
 * Apply transform to the rendered element based on state
 */
function applyTransform(rendered: HTMLElement, state: DiagramState): void {
    rendered.style.transform = `scale(${state.zoom}) translate(${state.panX}px, ${state.panY}px)`
}

/**
 * Set up zoom and pan controls for a mermaid container
 */
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

    // Create controls toolbar
    const controls = document.createElement('div')
    controls.className = 'mermaid-controls'

    const zoomInBtn = createControlButton(icons.zoomIn, 'Zoom in', 'mermaid-zoom-in')
    const zoomOutBtn = createControlButton(icons.zoomOut, 'Zoom out', 'mermaid-zoom-out')
    const resetBtn = createControlButton(icons.reset, 'Reset view', 'mermaid-reset')
    const fullscreenBtn = createControlButton(icons.fullscreen, 'View fullscreen', 'mermaid-fullscreen')

    controls.appendChild(zoomInBtn)
    controls.appendChild(zoomOutBtn)
    controls.appendChild(resetBtn)
    controls.appendChild(fullscreenBtn)

    // Insert controls at the beginning of container
    container.insertBefore(controls, container.firstChild)

    // Zoom in handler
    zoomInBtn.addEventListener('click', () => {
        if (state.zoom < ZOOM_MAX) {
            state.zoom = Math.min(ZOOM_MAX, state.zoom + ZOOM_STEP)
            applyTransform(rendered, state)
        }
    })

    // Zoom out handler
    zoomOutBtn.addEventListener('click', () => {
        if (state.zoom > ZOOM_MIN) {
            state.zoom = Math.max(ZOOM_MIN, state.zoom - ZOOM_STEP)
            applyTransform(rendered, state)
        }
    })

    // Reset handler
    resetBtn.addEventListener('click', () => {
        state.zoom = 1
        state.panX = 0
        state.panY = 0
        applyTransform(rendered, state)
    })

    // Fullscreen handler
    fullscreenBtn.addEventListener('click', () => {
        openFullscreenModal(svgContent)
    })

    // Pan with mouse drag
    viewport.addEventListener('mousedown', (e: MouseEvent) => {
        if (e.button !== 0) return // Only left click
        state.isDragging = true
        state.startX = e.clientX - state.panX * state.zoom
        state.startY = e.clientY - state.panY * state.zoom
        viewport.classList.add('is-dragging')
        e.preventDefault()
    })

    document.addEventListener('mousemove', (e: MouseEvent) => {
        if (!state.isDragging) return
        state.panX = (e.clientX - state.startX) / state.zoom
        state.panY = (e.clientY - state.startY) / state.zoom
        applyTransform(rendered, state)
    })

    document.addEventListener('mouseup', () => {
        if (state.isDragging) {
            state.isDragging = false
            viewport.classList.remove('is-dragging')
        }
    })

    // Zoom with wheel (requires Ctrl/Cmd)
    viewport.addEventListener('wheel', (e: WheelEvent) => {
        if (!e.ctrlKey && !e.metaKey) return
        e.preventDefault()

        const delta = e.deltaY > 0 ? -ZOOM_STEP : ZOOM_STEP
        const newZoom = Math.max(ZOOM_MIN, Math.min(ZOOM_MAX, state.zoom + delta))

        if (newZoom !== state.zoom) {
            state.zoom = newZoom
            applyTransform(rendered, state)
        }
    })
}

/**
 * Calculate scale to fit diagram in viewport while filling at least 80% of space
 */
function calculateFitScale(
    svgWidth: number,
    svgHeight: number,
    viewportWidth: number,
    viewportHeight: number
): number {
    // Calculate scale to fit within viewport (with some padding)
    const padding = 80 // pixels of padding
    const availableWidth = viewportWidth - padding
    const availableHeight = viewportHeight - padding

    const scaleX = availableWidth / svgWidth
    const scaleY = availableHeight / svgHeight

    // Use the smaller scale to ensure it fits, but at least 1x
    return Math.max(1, Math.min(scaleX, scaleY))
}

/**
 * Open fullscreen modal with the diagram
 */
function openFullscreenModal(svgContent: string): void {
    // Create modal elements
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
    rendered.innerHTML = svgContent

    viewport.appendChild(rendered)
    modalContent.appendChild(closeBtn)
    modalContent.appendChild(viewport)
    modal.appendChild(modalContent)

    // State for modal zoom/pan - initial zoom will be calculated after mount
    const state: DiagramState = {
        zoom: 1,
        panX: 0,
        panY: 0,
        isDragging: false,
        startX: 0,
        startY: 0,
    }

    // Store initial zoom for reset
    let initialZoom = 1

    // Create modal controls
    const controls = document.createElement('div')
    controls.className = 'mermaid-modal-controls'

    const zoomInBtn = createControlButton(icons.zoomIn, 'Zoom in', 'mermaid-zoom-in')
    const zoomOutBtn = createControlButton(icons.zoomOut, 'Zoom out', 'mermaid-zoom-out')
    const resetBtn = createControlButton(icons.reset, 'Reset view', 'mermaid-reset')

    controls.appendChild(zoomInBtn)
    controls.appendChild(zoomOutBtn)
    controls.appendChild(resetBtn)
    modalContent.appendChild(controls)

    // Modal zoom handlers
    zoomInBtn.addEventListener('click', () => {
        if (state.zoom < ZOOM_MAX) {
            state.zoom = Math.min(ZOOM_MAX, state.zoom + ZOOM_STEP)
            applyTransform(rendered, state)
        }
    })

    zoomOutBtn.addEventListener('click', () => {
        if (state.zoom > ZOOM_MIN) {
            state.zoom = Math.max(ZOOM_MIN, state.zoom - ZOOM_STEP)
            applyTransform(rendered, state)
        }
    })

    resetBtn.addEventListener('click', () => {
        state.zoom = initialZoom
        state.panX = 0
        state.panY = 0
        applyTransform(rendered, state)
    })

    // Modal pan handlers
    viewport.addEventListener('mousedown', (e: MouseEvent) => {
        if (e.button !== 0) return
        state.isDragging = true
        state.startX = e.clientX - state.panX * state.zoom
        state.startY = e.clientY - state.panY * state.zoom
        viewport.classList.add('is-dragging')
        e.preventDefault()
    })

    const onMouseMove = (e: MouseEvent) => {
        if (!state.isDragging) return
        state.panX = (e.clientX - state.startX) / state.zoom
        state.panY = (e.clientY - state.startY) / state.zoom
        applyTransform(rendered, state)
    }

    const onMouseUp = () => {
        if (state.isDragging) {
            state.isDragging = false
            viewport.classList.remove('is-dragging')
        }
    }

    document.addEventListener('mousemove', onMouseMove)
    document.addEventListener('mouseup', onMouseUp)

    // Modal wheel zoom
    viewport.addEventListener('wheel', (e: WheelEvent) => {
        if (!e.ctrlKey && !e.metaKey) return
        e.preventDefault()

        const delta = e.deltaY > 0 ? -ZOOM_STEP : ZOOM_STEP
        const newZoom = Math.max(ZOOM_MIN, Math.min(ZOOM_MAX, state.zoom + delta))

        if (newZoom !== state.zoom) {
            state.zoom = newZoom
            applyTransform(rendered, state)
        }
    })

    // Close modal function
    const closeModal = () => {
        document.removeEventListener('mousemove', onMouseMove)
        document.removeEventListener('mouseup', onMouseUp)
        document.body.style.overflow = ''
        modal.remove()
    }

    // Close on button click
    closeBtn.addEventListener('click', closeModal)

    // Close on backdrop click
    modal.addEventListener('click', (e: MouseEvent) => {
        if (e.target === modal) {
            closeModal()
        }
    })

    // Close on Escape key
    const onKeyDown = (e: KeyboardEvent) => {
        if (e.key === 'Escape') {
            closeModal()
            document.removeEventListener('keydown', onKeyDown)
        }
    }
    document.addEventListener('keydown', onKeyDown)

    // Prevent body scroll
    document.body.style.overflow = 'hidden'

    // Add modal to document
    document.body.appendChild(modal)

    // Calculate and apply initial zoom to fit diagram in viewport
    requestAnimationFrame(() => {
        const svg = rendered.querySelector('svg')
        if (svg) {
            const svgRect = svg.getBoundingClientRect()
            const viewportRect = viewport.getBoundingClientRect()

            initialZoom = calculateFitScale(
                svgRect.width,
                svgRect.height,
                viewportRect.width,
                viewportRect.height
            )

            state.zoom = initialZoom
            applyTransform(rendered, state)
        }
    })
}

/**
 * Initialize Mermaid diagram rendering for elements with class 'mermaid'
 */
export async function initMermaid() {
    const mermaidElements = document.querySelectorAll(
        'pre.mermaid:not([data-mermaid-processed])'
    )

    if (mermaidElements.length === 0) {
        return
    }

    try {
        // Lazy-load Beautiful Mermaid only when diagrams exist
        await loadMermaid()

        // Render each diagram individually
        for (let i = 0; i < mermaidElements.length; i++) {
            const element = mermaidElements[i]
            const content = element.textContent?.trim()

            if (!content) continue

            // Mark as processed to prevent double rendering
            element.setAttribute('data-mermaid-processed', 'true')

            try {
                // Render the diagram using Beautiful Mermaid
                let svg = await window.__mermaid.renderMermaid(content)

                // Post-process the SVG
                svg = resolveVariables(svg)
                svg = removeGoogleFonts(svg)

                // Create container structure with controls
                const container = document.createElement('div')
                container.className = 'mermaid-container'

                const viewport = document.createElement('div')
                viewport.className = 'mermaid-viewport'

                const rendered = document.createElement('div')
                rendered.className = 'mermaid-rendered'
                rendered.innerHTML = svg

                viewport.appendChild(rendered)
                container.appendChild(viewport)

                // Set up interactive controls
                setupControls(container, viewport, rendered, svg)

                // Replace the pre element with the new container
                element.replaceWith(container)
            } catch (err) {
                console.warn('Mermaid rendering error for diagram:', err)
                // Keep the original content as fallback
                element.classList.add('mermaid-error')
            }
        }
    } catch (error) {
        console.warn('Mermaid initialization error:', error)
    }
}
